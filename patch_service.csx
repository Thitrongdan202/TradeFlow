using System;
using System.IO;
using System.Text.RegularExpressions;

var filePath = "src/TradeFlow.Infrastructure/Services/ExcelPricingService.cs";
var content = File.ReadAllText(filePath);

// 1. Add ILogger
if (!content.Contains("using Microsoft.Extensions.Logging;")) {
    content = "using Microsoft.Extensions.Logging;\n" + content;
}

if (!content.Contains("ILogger<ExcelPricingService> _logger")) {
    content = content.Replace("private readonly IAuditService _auditService;", "private readonly IAuditService _auditService;\n    private readonly ILogger<ExcelPricingService> _logger;");
    content = content.Replace("IAuditService auditService)", "IAuditService auditService,\n        ILogger<ExcelPricingService> logger)");
    content = content.Replace("_auditService = auditService;", "_auditService = auditService;\n        _logger = logger;");
}

// 2. Truncate auto-created product fields and wrap with catch
var findAutoCreate = @"var newProd = new Product
                {
                    Code = prodCode,
                    NewCode = item.NewCode.Trim(),
                    LegacyCode = item.LegacyCode\?\.Trim\(\),
                    Name = !string.IsNullOrWhiteSpace\(item.ProductInfo\) \? item.ProductInfo.Split\('\\n'\)\[0\].Trim\(\) : item.NewCode.Trim\(\),
                    Description = item.ProductInfo\?\.Trim\(\),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserName
                };
                _context.Products.Add\(newProd\);
                await _context.SaveChangesAsync\(cancellationToken\);";

var replacementAutoCreate = @"
                var rawName = !string.IsNullOrWhiteSpace(item.ProductInfo) ? item.ProductInfo.Split('\n')[0].Trim() : item.NewCode.Trim();
                var safeName = rawName.Length > 300 ? rawName[..300] : rawName;
                var safeNewCode = item.NewCode.Trim().Length > 50 ? item.NewCode.Trim()[..50] : item.NewCode.Trim();
                var safeLegacyCode = item.LegacyCode?.Trim();
                if (safeLegacyCode != null && safeLegacyCode.Length > 50) safeLegacyCode = safeLegacyCode[..50];
                var safeDescription = item.ProductInfo?.Trim();
                if (safeDescription != null && safeDescription.Length > 2000) safeDescription = safeDescription[..2000];

                var newProd = new Product
                {
                    Code = prodCode,
                    NewCode = safeNewCode,
                    LegacyCode = safeLegacyCode,
                    Name = safeName,
                    Description = safeDescription,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserName
                };
                _context.Products.Add(newProd);
                try 
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                {
                    _logger.LogError(ex, ""Product save error at row {Row}. Inner: {Inner}"", item.SortOrder, ex.InnerException?.Message);
                    throw new Exception($""Lỗi lưu sản phẩm tự động dòng {item.SortOrder}: {ex.InnerException?.Message ?? ex.Message}"", ex);
                }
";
content = Regex.Replace(content, findAutoCreate, replacementAutoCreate);

// 3. Truncate PriceListItems
var findPItem = @"var pItem = new PriceListItem
            {
                PriceListId = priceList.Id,
                ProductId = productId,
                SortOrder = item.SortOrder,
                Group = item.Group\?\.Trim\(\),
                NewCode = string.IsNullOrWhiteSpace\(item.NewCode\) \? \$\""ITEM-\{item.SortOrder:D4\}\"" : item.NewCode.Trim\(\),
                LegacyCode = item.LegacyCode\?\.Trim\(\),
                ProductInfo = item.ProductInfo\?\.Trim\(\),
                ImageStorageRef = item.ImageStorageRef,
                UnitPrice = item.UnitPrice,
                CurrencyCode = string.IsNullOrWhiteSpace\(item.CurrencyCode\) \? \""VND\"" : item.CurrencyCode.Trim\(\),
                MatchStatus = item.MatchStatus,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserName
            };";

var replacementPItem = @"
            var safePlGroup = item.Group?.Trim();
            if (safePlGroup != null && safePlGroup.Length > 200) safePlGroup = safePlGroup[..200];
            
            var safePlNewCode = string.IsNullOrWhiteSpace(item.NewCode) ? $""ITEM-{item.SortOrder:D4}"" : item.NewCode.Trim();
            if (safePlNewCode.Length > 100) safePlNewCode = safePlNewCode[..100];
            
            var safePlLegacyCode = item.LegacyCode?.Trim();
            if (safePlLegacyCode != null && safePlLegacyCode.Length > 100) safePlLegacyCode = safePlLegacyCode[..100];
            
            var safePlInfo = item.ProductInfo?.Trim();
            if (safePlInfo != null && safePlInfo.Length > 2000) safePlInfo = safePlInfo[..2000];

            var pItem = new PriceListItem
            {
                PriceListId = priceList.Id,
                ProductId = productId,
                SortOrder = item.SortOrder,
                Group = safePlGroup,
                NewCode = safePlNewCode,
                LegacyCode = safePlLegacyCode,
                ProductInfo = safePlInfo,
                ImageStorageRef = item.ImageStorageRef,
                UnitPrice = item.UnitPrice,
                CurrencyCode = string.IsNullOrWhiteSpace(item.CurrencyCode) ? ""VND"" : item.CurrencyCode.Trim(),
                MatchStatus = item.MatchStatus,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserName
            };
";
content = Regex.Replace(content, findPItem, replacementPItem);

// 4. Wrap final SaveChangesAsync
var findFinalSave = @"await _context.SaveChangesAsync\(cancellationToken\);\s*await _auditService.LogAsync";
var replacementFinalSave = @"
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, ""Database persistence error during price list commit. Inner exception: {Inner}"", ex.InnerException?.Message);
            throw new Exception($""Lỗi lưu bảng giá: {ex.InnerException?.Message ?? ex.Message}"", ex);
        }
        await _auditService.LogAsync";
content = Regex.Replace(content, findFinalSave, replacementFinalSave);

// 5. Add Logging to Image Extraction
content = content.Replace("var rowImageMap = new Dictionary<int, string>();", "var rowImageMap = new Dictionary<int, string>();\n        _logger.LogInformation(\"Starting image extraction for {FileSize} bytes. Target rows: {Count}\", stream.Length, targetRows?.Count ?? 0);");
content = content.Replace("var imgEntry = zip.GetEntry(mediaPath);", "_logger.LogInformation(\"Found image {MediaPath} anchored at row {ExcelRow}\", mediaPath, excelRow);\n                    var imgEntry = zip.GetEntry(mediaPath);");

var imgCatch = @"catch
        {
            // If image extraction encounters unusual drawing formats, return whatever mapped so far
        }";
var replacementImgCatch = @"catch (Exception ex)
        {
            _logger.LogError(ex, ""Error during image extraction. Returning {Count} images mapped so far."", rowImageMap.Count);
        }";
content = content.Replace(imgCatch, replacementImgCatch);

File.WriteAllText(filePath, content);
