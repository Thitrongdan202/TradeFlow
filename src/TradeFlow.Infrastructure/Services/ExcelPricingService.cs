using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TradeFlow.Application.Common.Constants;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Pricing;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Pricing;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class ExcelPricingService : IExcelPricingService
{
    private readonly TradeFlowDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly ISystemCodeGenerator _codeGenerator;
    private readonly IAuditService _auditService;

    public ExcelPricingService(
        TradeFlowDbContext context,
        IFileStorageService fileStorage,
        ISystemCodeGenerator codeGenerator,
        IAuditService auditService)
    {
        _context = context;
        _fileStorage = fileStorage;
        _codeGenerator = codeGenerator;
        _auditService = auditService;
    }

    public async Task<ExcelAnalysisResultDto> AnalyzeAndDryRunAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var result = new ExcelAnalysisResultDto
        {
            FileName = fileName
        };

        // 1. Save original file as temporary upload directly from the incoming stream
        result.TempFileReference = await _fileStorage.SaveFileAsync(fileStream, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "pricing_originals", cancellationToken);

        // 2. Open the saved file as a seekable FileStream
        using var diskStream = await _fileStorage.GetFileAsync(result.TempFileReference, cancellationToken);
        if (diskStream == null)
        {
            result.Errors.Add("Không thể đọc file tạm sau khi lưu.");
            return result;
        }

        // 3. Extract embedded pictures mapping to row index (1-based Excel row number)
        var rowImageMap = ExtractImagesByRow(diskStream);
        result.ImagesFound = rowImageMap.Count;
        diskStream.Position = 0;

        // 4. Parse workbook structure via ClosedXML
        using var workbook = new XLWorkbook(diskStream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            result.Errors.Add("File Excel không chứa bất kỳ bảng tính (worksheet) nào.");
            return result;
        }

        // 5. Inspect Header & Metadata
        InspectHeader(worksheet, result);

        // 6. Find Table Header Row
        int headerRow = FindHeaderRow(worksheet);
        if (headerRow <= 0)
        {
            result.Errors.Add("Không tìm thấy dòng tiêu đề bảng hàng hóa (cần chứa các cột: MÃ HÀNG, THÔNG TIN SẢN PHẨM, GIÁ...).");
            return result;
        }

        // Determine column indexes
        int colNo = FindColumnByKeywords(worksheet, headerRow, "NO", "STT");
        int colGroup = FindColumnByKeywords(worksheet, headerRow, "NHÓM", "NHOM", "DANH MỤC");
        int colNewCode = FindColumnByKeywords(worksheet, headerRow, "MÃ HÀNG MỚI", "MA HANG MOI", "MÃ MỚI", "MÃ HÀNG");
        int colLegacyCode = FindColumnByKeywords(worksheet, headerRow, "MÃ HÀNG CŨ", "MA HANG CU", "MÃ CŨ");
        int colInfo = FindColumnByKeywords(worksheet, headerRow, "THÔNG TIN SẢN PHẨM", "THONG TIN", "QUY CÁCH", "TÊN SẢN PHẨM");
        int colPrice = FindColumnByKeywords(worksheet, headerRow, "GIÁ ĐẠI LÝ", "GIA DAI LY", "GIÁ", "ĐƠN GIÁ");

        if (colNewCode <= 0 && colInfo <= 0)
        {
            result.Errors.Add("Không xác định được cột Mã hàng mới hoặc Thông tin sản phẩm trong bảng tính.");
            return result;
        }

        // 7. Load all existing products from DB for matching
        var existingProducts = await _context.Products
            .AsNoTracking()
            .Select(p => new { p.Id, p.Code, p.NewCode, p.LegacyCode, p.Name })
            .ToListAsync(cancellationToken);

        int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow;
        int currentOrder = 1;
        string currentGroup = "";

        for (int r = headerRow + 1; r <= lastRow; r++)
        {
            var row = worksheet.Row(r);
            if (row.IsEmpty()) continue;

            string newCodeText = colNewCode > 0 ? row.Cell(colNewCode).GetString().Trim() : "";
            string legacyCodeText = colLegacyCode > 0 ? row.Cell(colLegacyCode).GetString().Trim() : "";
            string infoText = colInfo > 0 ? row.Cell(colInfo).GetString().Trim() : "";
            string groupText = colGroup > 0 ? row.Cell(colGroup).GetString().Trim() : "";

            if (!string.IsNullOrEmpty(groupText))
            {
                currentGroup = groupText;
            }

            // Price parsing
            decimal unitPrice = 0;
            if (colPrice > 0)
            {
                var priceCell = row.Cell(colPrice);
                if (priceCell.TryGetValue<decimal>(out var pVal))
                {
                    unitPrice = pVal;
                }
                else
                {
                    var rawPriceStr = priceCell.GetString().Trim();
                    rawPriceStr = Regex.Replace(rawPriceStr, @"[^\d\.]", "");
                    decimal.TryParse(rawPriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out unitPrice);
                }
            }

            // STT parsing
            int sortNo = currentOrder;
            if (colNo > 0 && row.Cell(colNo).TryGetValue<int>(out var parsedNo))
            {
                sortNo = parsedNo;
            }

            // Skip row if no code, no info, and zero price
            if (string.IsNullOrWhiteSpace(newCodeText) && string.IsNullOrWhiteSpace(infoText) && unitPrice <= 0)
            {
                continue;
            }

            var itemDto = new ExcelParsedItemDto
            {
                RowIndex = r,
                SortOrder = sortNo,
                Group = string.IsNullOrWhiteSpace(groupText) ? currentGroup : groupText,
                NewCode = newCodeText,
                LegacyCode = legacyCodeText,
                ProductInfo = infoText,
                UnitPrice = unitPrice,
                CurrencyCode = "VND",
                MatchStatus = PriceMatchStatus.NewProduct
            };

            // Associate extracted image if available
            if (rowImageMap.TryGetValue(r, out var imgRef))
            {
                itemDto.ImageStorageRef = imgRef;
            }

            // Match against DB products
            var matched = existingProducts.FirstOrDefault(p =>
                (!string.IsNullOrEmpty(p.NewCode) && (p.NewCode.Equals(newCodeText, StringComparison.OrdinalIgnoreCase) || newCodeText.StartsWith(p.NewCode, StringComparison.OrdinalIgnoreCase))) ||
                (!string.IsNullOrEmpty(p.LegacyCode) && !string.IsNullOrEmpty(legacyCodeText) && p.LegacyCode.Equals(legacyCodeText, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.Code) && p.Code.Equals(newCodeText, StringComparison.OrdinalIgnoreCase)));

            if (matched != null)
            {
                itemDto.MatchedProductId = matched.Id;
                itemDto.MatchedProductName = matched.Name;
                itemDto.MatchStatus = PriceMatchStatus.Matched;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(itemDto.NewCode) && string.IsNullOrWhiteSpace(itemDto.LegacyCode))
                {
                    itemDto.MatchStatus = PriceMatchStatus.ReviewRequired;
                    itemDto.ValidationMessages.Add("Thiếu mã sản phẩm định danh (Mã mới / Mã cũ).");
                }
                else
                {
                    itemDto.MatchStatus = PriceMatchStatus.NewProduct;
                }
            }

            if (itemDto.UnitPrice <= 0)
            {
                itemDto.ValidationMessages.Add("Đơn giá chưa được thiết lập hoặc bằng 0.");
            }

            result.Items.Add(itemDto);
            currentOrder++;
        }

        result.TotalRowsFound = result.Items.Count;
        return result;
    }

    public async Task<PriceListDto> CommitImportAsync(ExcelImportCommitRequest request, string currentUserName, CancellationToken cancellationToken = default)
    {
        var priceListCode = await _codeGenerator.GenerateCodeAsync(SystemCodeConstants.PriceList, cancellationToken);

        var priceList = new PriceList
        {
            Code = priceListCode,
            Name = string.IsNullOrWhiteSpace(request.Name) ? "BẢNG GIÁ ĐẠI LÝ" : request.Name.Trim(),
            QuotationNumber = request.QuotationNumber?.Trim(),
            QuotationDate = request.QuotationDate,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Month = request.Month,
            Quarter = request.Quarter,
            Year = request.Year > 0 ? request.Year : DateTime.UtcNow.Year,
            ProgramTitle = request.ProgramTitle?.Trim(),
            PriceCondition = request.PriceCondition?.Trim(),
            VatNote = request.VatNote?.Trim(),
            Status = request.InitialStatus,
            OriginalFileName = request.OriginalFileName,
            OriginalFileStorageRef = request.TempFileReference,
            TotalItems = request.Items.Count,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUserName
        };

        _context.PriceLists.Add(priceList);
        await _context.SaveChangesAsync(cancellationToken);

        // Add Items
        foreach (var item in request.Items)
        {
            int? productId = item.MatchedProductId;

            // Auto create product if missing and requested
            if (!productId.HasValue && request.AutoCreateProducts && !string.IsNullOrWhiteSpace(item.NewCode))
            {
                var prodCode = await _codeGenerator.GenerateCodeAsync(SystemCodeConstants.Product, cancellationToken);
                var newProd = new Product
                {
                    Code = prodCode,
                    NewCode = item.NewCode.Trim(),
                    LegacyCode = item.LegacyCode?.Trim(),
                    Name = !string.IsNullOrWhiteSpace(item.ProductInfo) ? item.ProductInfo.Split('\n')[0].Trim() : item.NewCode.Trim(),
                    Description = item.ProductInfo?.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserName
                };
                _context.Products.Add(newProd);
                await _context.SaveChangesAsync(cancellationToken);
                productId = newProd.Id;
                item.MatchedProductId = newProd.Id;
                item.MatchStatus = PriceMatchStatus.Matched;
            }

            var pItem = new PriceListItem
            {
                PriceListId = priceList.Id,
                ProductId = productId,
                SortOrder = item.SortOrder,
                Group = item.Group?.Trim(),
                NewCode = string.IsNullOrWhiteSpace(item.NewCode) ? $"ITEM-{item.SortOrder:D4}" : item.NewCode.Trim(),
                LegacyCode = item.LegacyCode?.Trim(),
                ProductInfo = item.ProductInfo?.Trim(),
                ImageStorageRef = item.ImageStorageRef,
                UnitPrice = item.UnitPrice,
                CurrencyCode = string.IsNullOrWhiteSpace(item.CurrencyCode) ? "VND" : item.CurrencyCode.Trim(),
                MatchStatus = item.MatchStatus,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserName
            };

            _context.PriceListItems.Add(pItem);

            // If matched product has no images yet and we extracted an image, add it as primary ProductImage
            if (item.MatchedProductId.HasValue && !string.IsNullOrEmpty(item.ImageStorageRef))
            {
                var hasImg = await _context.ProductImages.AnyAsync(pi => pi.ProductId == item.MatchedProductId.Value, cancellationToken);
                if (!hasImg)
                {
                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = item.MatchedProductId.Value,
                        FileName = Path.GetFileName(item.ImageStorageRef),
                        StorageReference = item.ImageStorageRef,
                        ContentType = "image/png",
                        IsPrimary = true,
                        SortOrder = 1,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUserName
                    });
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(AuditEventType.PriceListImported, currentUserName, "PriceList", priceList.Code);

        return new PriceListDto
        {
            Id = priceList.Id,
            Code = priceList.Code,
            Name = priceList.Name,
            QuotationNumber = priceList.QuotationNumber,
            EffectiveFrom = priceList.EffectiveFrom,
            EffectiveTo = priceList.EffectiveTo,
            TotalItems = priceList.TotalItems,
            Status = priceList.Status
        };
    }

    public async Task<byte[]> ExportPriceListAsync(int priceListId, CancellationToken cancellationToken = default)
    {
        var priceList = await _context.PriceLists
            .Include(p => p.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == priceListId, cancellationToken);

        if (priceList == null)
        {
            throw new InvalidOperationException("Không tìm thấy bảng giá.");
        }

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Bảng giá");

        // 1. Company Header (Rows 1-3)
        ws.Cell("A2").Value = "TỔNG KHO PHÂN PHỐI TRADEFLOW";
        ws.Cell("A2").Style.Font.Bold = true;
        ws.Cell("A2").Style.Font.FontSize = 14;
        ws.Cell("A2").Style.Font.FontColor = XLColor.DarkBrown;

        ws.Cell("A3").Value = "Địa chỉ: Hệ thống kho TradeFlow toàn quốc — Hotline: 1900 1234";
        ws.Cell("A3").Style.Font.Italic = true;
        ws.Cell("A3").Style.Font.FontSize = 10;

        // Title Top Right
        ws.Cell("F2").Value = priceList.Name.ToUpper();
        ws.Cell("F2").Style.Font.Bold = true;
        ws.Cell("F2").Style.Font.FontSize = 15;
        ws.Cell("F2").Style.Font.FontColor = XLColor.DarkOrange;

        ws.Cell("F3").Value = $"Số Báo Giá: {priceList.QuotationNumber ?? priceList.Code} | Ngày: {(priceList.QuotationDate ?? priceList.CreatedAt).ToString("dd/MM/yyyy")}";
        ws.Cell("F3").Style.Font.FontSize = 10;

        // 2. Program Subheader (Rows 5-8)
        ws.Cell("A5").Value = priceList.ProgramTitle ?? "CHƯƠNG TRÌNH BÁO GIÁ ĐẠI LÝ";
        ws.Cell("A5").Style.Font.Bold = true;
        ws.Cell("A5").Style.Font.FontColor = XLColor.Firebrick;
        ws.Cell("A5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range("A5:G5").Merge();

        ws.Cell("A6").Value = $"- Thời gian áp dụng: từ ngày {(priceList.EffectiveFrom?.ToString("dd/MM/yyyy") ?? "áp dụng")} đến ngày {(priceList.EffectiveTo?.ToString("dd/MM/yyyy") ?? "khi có thông báo mới")}";
        ws.Cell("A6").Style.Font.Bold = true;

        ws.Cell("A7").Value = $"- Điều kiện giá: {priceList.PriceCondition ?? "Giá giao tại kho"} | {priceList.VatNote ?? "Chưa bao gồm VAT"}";
        ws.Cell("A7").Style.Font.FontColor = XLColor.Firebrick;

        // 3. Table Header (Row 9)
        int headerRow = 9;
        string[] headers = { "STT", "NHÓM", "MÃ HÀNG MỚI", "MÃ HÀNG CŨ", "THÔNG TIN SẢN PHẨM", "GIÁ ĐẠI LÝ (VNĐ)", "GHI CHÚ" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(184, 83, 20); // Amber/Orange Theme
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        ws.Row(headerRow).Height = 28;

        // 4. Data Rows
        int currentRow = headerRow + 1;
        foreach (var item in priceList.Items)
        {
            ws.Cell(currentRow, 1).Value = item.SortOrder;
            ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 2).Value = item.Group ?? "";
            ws.Cell(currentRow, 2).Style.Font.Bold = true;

            ws.Cell(currentRow, 3).Value = item.NewCode;
            ws.Cell(currentRow, 3).Style.Font.Bold = true;
            ws.Cell(currentRow, 3).Style.Font.FontColor = XLColor.Red;

            ws.Cell(currentRow, 4).Value = item.LegacyCode ?? "";

            ws.Cell(currentRow, 5).Value = item.ProductInfo ?? "";
            ws.Cell(currentRow, 5).Style.Alignment.WrapText = true;

            ws.Cell(currentRow, 6).Value = item.UnitPrice;
            ws.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0";
            ws.Cell(currentRow, 6).Style.Font.Bold = true;
            ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Cell(currentRow, 7).Value = item.Note ?? "";

            // Borders
            ws.Range(currentRow, 1, currentRow, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(currentRow, 1, currentRow, 7).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            ws.Range(currentRow, 1, currentRow, 7).Style.Border.OutsideBorderColor = XLColor.LightGray;
            ws.Range(currentRow, 1, currentRow, 7).Style.Border.InsideBorderColor = XLColor.LightGray;

            currentRow++;
        }

        // Column widths
        ws.Column(1).Width = 8;
        ws.Column(2).Width = 18;
        ws.Column(3).Width = 18;
        ws.Column(4).Width = 16;
        ws.Column(5).Width = 35;
        ws.Column(6).Width = 20;
        ws.Column(7).Width = 16;

        using var outMs = new MemoryStream();
        workbook.SaveAs(outMs);
        return outMs.ToArray();
    }

    #region Helper Methods

    private static void InspectHeader(IXLWorksheet ws, ExcelAnalysisResultDto result)
    {
        // Scan first 10 rows for metadata
        for (int r = 1; r <= 10; r++)
        {
            for (int c = 1; c <= 10; c++)
            {
                var text = ws.Cell(r, c).GetString().Trim();
                if (string.IsNullOrEmpty(text)) continue;

                if (text.Contains("TỔNG KHO", StringComparison.OrdinalIgnoreCase) || text.Contains("CÔNG TY", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(result.CompanyName)) result.CompanyName = text;
                }
                else if (text.Contains("ĐC:", StringComparison.OrdinalIgnoreCase) || text.Contains("Địa chỉ:", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(result.CompanyAddress)) result.CompanyAddress = text;
                }
                else if (text.Contains("Tel:", StringComparison.OrdinalIgnoreCase) || text.Contains("Điện thoại:", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(result.CompanyPhone)) result.CompanyPhone = text;
                }
                else if (text.Contains("BẢNG GIÁ", StringComparison.OrdinalIgnoreCase) || text.Contains("BÁO GIÁ", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(result.QuotationTitle)) result.QuotationTitle = text;
                }
                else if (text.Contains("Số Báo Giá:", StringComparison.OrdinalIgnoreCase) || text.Contains("Số BG:", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(text, @"Số (?:Báo Giá|BG):\s*([^\r\n|]+)", RegexOptions.IgnoreCase);
                    if (match.Success) result.QuotationNumber = match.Groups[1].Value.Trim();
                }
                else if (text.Contains("Ngày Báo Giá:", StringComparison.OrdinalIgnoreCase) || text.Contains("Ngày:", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(text, @"Ngày (?:Báo Giá|BG)?:\s*(\d{1,2}/\d{1,2}/\d{4})", RegexOptions.IgnoreCase);
                    if (match.Success && DateTime.TryParseExact(match.Groups[1].Value, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var quotationDt))
                    {
                        result.QuotationDate = quotationDt;
                    }
                }
                else if (text.Contains("CHƯƠNG TRÌNH", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(result.ProgramTitle)) result.ProgramTitle = text;
                }
                else if (text.Contains("Thời gian áp dụng", StringComparison.OrdinalIgnoreCase))
                {
                    var dateMatches = Regex.Matches(text, @"(\d{1,2}/\d{1,2}/\d{4})");
                    if (dateMatches.Count >= 1 && DateTime.TryParseExact(dateMatches[0].Value, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDt))
                    {
                        result.EffectiveFrom = fromDt;
                    }
                    if (dateMatches.Count >= 2 && DateTime.TryParseExact(dateMatches[1].Value, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDt))
                    {
                        result.EffectiveTo = toDt;
                    }
                }
                else if (text.Contains("Giá bán tại kho", StringComparison.OrdinalIgnoreCase) || text.Contains("Điều kiện:", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(result.PriceCondition)) result.PriceCondition = text;
                }
                else if (text.Contains("VAT", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(result.VatNote)) result.VatNote = text;
                }
            }
        }
    }

    private static int FindHeaderRow(IXLWorksheet ws)
    {
        for (int r = 1; r <= 25; r++)
        {
            int matchCount = 0;
            for (int c = 1; c <= 15; c++)
            {
                var val = ws.Cell(r, c).GetString().ToUpperInvariant().Trim();
                if (val.Contains("NO") || val.Contains("STT") || val.Contains("NHÓM") ||
                    val.Contains("MÃ HÀNG") || val.Contains("HÌNH ẢNH") ||
                    val.Contains("THÔNG TIN") || val.Contains("GIÁ"))
                {
                    matchCount++;
                }
            }
            if (matchCount >= 3) return r;
        }
        return -1;
    }

    private static int FindColumnByKeywords(IXLWorksheet ws, int headerRow, params string[] keywords)
    {
        for (int c = 1; c <= 20; c++)
        {
            var headerText = ws.Cell(headerRow, c).GetString().Trim();
            foreach (var kw in keywords)
            {
                if (headerText.Contains(kw, StringComparison.OrdinalIgnoreCase))
                {
                    return c;
                }
            }
        }
        return -1;
    }

    private Dictionary<int, string> ExtractImagesByRow(Stream stream)
    {
        var rowImageMap = new Dictionary<int, string>();
        try
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

            // Look for drawing files
            var drawingEntries = zip.Entries.Where(e => e.FullName.StartsWith("xl/drawings/drawing") && e.FullName.EndsWith(".xml")).ToList();
            if (!drawingEntries.Any()) return rowImageMap;

            foreach (var drawingEntry in drawingEntries)
            {
                var relPath = $"xl/drawings/_rels/{Path.GetFileName(drawingEntry.FullName)}.rels";
                var relEntry = zip.GetEntry(relPath);
                if (relEntry == null) continue;

                // Load relationships mapping rId -> media file
                var relsDoc = XDocument.Load(relEntry.Open());
                XNamespace relsNs = "http://schemas.openxmlformats.org/package/2006/relationships";
                var idToTarget = relsDoc.Descendants(relsNs + "Relationship")
                    .ToDictionary(
                        r => (string)r.Attribute("Id")!,
                        r => (string)r.Attribute("Target")!
                    );

                // Load drawings XML
                var drawingDoc = XDocument.Load(drawingEntry.Open());
                XNamespace xdr = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";
                XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
                XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

                var anchors = drawingDoc.Descendants(xdr + "twoCellAnchor")
                    .Concat(drawingDoc.Descendants(xdr + "oneCellAnchor"));

                foreach (var anchor in anchors)
                {
                    var from = anchor.Element(xdr + "from");
                    if (from == null) continue;

                    var rowElem = from.Element(xdr + "row");
                    if (rowElem == null || !int.TryParse(rowElem.Value, out var xmlRow)) continue;

                    // xmlRow is 0-indexed, corresponding to 1-indexed Excel row: xmlRow + 1
                    int excelRow = xmlRow + 1;

                    var blip = anchor.Descendants(a + "blip").FirstOrDefault();
                    if (blip == null) continue;

                    var embedId = (string?)blip.Attribute(r + "embed");
                    if (string.IsNullOrEmpty(embedId) || !idToTarget.TryGetValue(embedId, out var targetPath)) continue;

                    // Resolve target path (e.g. "../media/image1.png" -> "xl/media/image1.png")
                    var mediaPath = targetPath.Replace("../", "xl/").Replace('\\', '/');
                    var imgEntry = zip.GetEntry(mediaPath);
                    if (imgEntry == null) continue;

                    using var imgStream = imgEntry.Open();
                    using var ms = new MemoryStream();
                    imgStream.CopyTo(ms);
                    ms.Position = 0;

                    var ext = Path.GetExtension(mediaPath);
                    var contentType = ext.ToLower() switch
                    {
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".png" => "image/png",
                        ".webp" => "image/webp",
                        _ => "application/octet-stream"
                    };

                    var storageRef = _fileStorage.SaveFileAsync(ms, $"row_{excelRow}{ext}", contentType, "pricing").GetAwaiter().GetResult();
                    rowImageMap[excelRow] = storageRef;
                }
            }
        }
        catch
        {
            // If image extraction encounters unusual drawing formats, return whatever mapped so far
        }

        return rowImageMap;
    }

    #endregion
}
