const fs = require('fs');

let c = fs.readFileSync('src/TradeFlow.Infrastructure/Services/ExcelPricingService.cs', 'utf8');

// 1. Update ExtractImagesByRow to accept targetRows and become async
c = c.replace(
    'private Dictionary<int, string> ExtractImagesByRow(Stream stream)',
    'private async Task<Dictionary<int, string>> ExtractImagesByRowAsync(Stream stream, HashSet<int>? targetRows = null)'
);

// Replace async calls inside ExtractImagesByRow
c = c.replace(
    'var storageRef = _fileStorage.SaveFileAsync(ms, $"row_{excelRow}{ext}", contentType, "pricing").GetAwaiter().GetResult();',
    'if (targetRows != null && !targetRows.Contains(excelRow)) continue;\n                    var storageRef = await _fileStorage.SaveFileAsync(ms, $"row_{excelRow}{ext}", contentType, "pricing");'
);

// 2. Rewrite AnalyzeAndDryRunAsync
const oldAnalyze = `        // 1. Save original file as temporary upload directly from the incoming stream
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
        {`;

const newAnalyze = `        onProgress?.Invoke("1/6 Đang lưu file gốc vào storage...");
        // 1. Save original file as temporary upload directly from the incoming stream
        result.TempFileReference = await _fileStorage.SaveFileAsync(fileStream, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "pricing_originals", cancellationToken);

        onProgress?.Invoke("2/6 Đang mở file...");
        // 2. Open the saved file as a seekable FileStream
        using var diskStream = await _fileStorage.GetFileAsync(result.TempFileReference, cancellationToken);
        if (diskStream == null)
        {
            result.Errors.Add("Không thể đọc file tạm sau khi lưu.");
            return result;
        }

        onProgress?.Invoke("3/6 Đang phân tích cấu trúc Excel (Bước này có thể mất vài phút với file lớn)...");
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

        onProgress?.Invoke("4/6 Đang trích xuất dữ liệu sản phẩm...");
        int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow;
        var validRows = new HashSet<int>();
        for (int r = headerRow + 1; r <= lastRow; r++)
        {
            var testRow = worksheet.Row(r);
            if (!testRow.IsEmpty()) {
                validRows.Add(r);
            }
        }

        onProgress?.Invoke($"5/6 Đang trích xuất hình ảnh cho {validRows.Count} sản phẩm...");
        diskStream.Position = 0;
        var rowImageMap = await ExtractImagesByRowAsync(diskStream, validRows);
        result.ImagesFound = rowImageMap.Count;

        onProgress?.Invoke("6/6 Đang đối chiếu danh mục...");
        // 7. Load all existing products from DB for matching
        var existingProducts = await _context.Products
            .AsNoTracking()
            .Select(p => new { p.Id, p.Code, p.NewCode, p.LegacyCode, p.Name })
            .ToListAsync(cancellationToken);

        int currentOrder = 1;
        string currentGroup = "";

        for (int r = headerRow + 1; r <= lastRow; r++)
        {`;

c = c.replace(oldAnalyze, newAnalyze);

fs.writeFileSync('src/TradeFlow.Infrastructure/Services/ExcelPricingService.cs', c, 'utf8');
