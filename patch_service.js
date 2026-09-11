const fs = require('fs');

let filePath = "src/TradeFlow.Infrastructure/Services/ExcelPricingService.cs";
let content = fs.readFileSync(filePath, 'utf8');

// 1. Add ILogger
if (!content.includes("using Microsoft.Extensions.Logging;")) {
    content = "using Microsoft.Extensions.Logging;\n" + content;
}

if (!content.includes("ILogger<ExcelPricingService> _logger")) {
    content = content.replace("private readonly IAuditService _auditService;", "private readonly IAuditService _auditService;\n    private readonly ILogger<ExcelPricingService> _logger;");
    content = content.replace("IAuditService auditService)", "IAuditService auditService,\n        ILogger<ExcelPricingService> logger)");
    content = content.replace("_auditService = auditService;", "_auditService = auditService;\n        _logger = logger;");
}

// 2. AutoCreateProducts Replacement
let autoCreateFind = `var newProd = new Product
                {
                    Code = prodCode,
                    NewCode = item.NewCode.Trim(),
                    LegacyCode = item.LegacyCode?.Trim(),
                    Name = !string.IsNullOrWhiteSpace(item.ProductInfo) ? item.ProductInfo.Split('\\n')[0].Trim() : item.NewCode.Trim(),
                    Description = item.ProductInfo?.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserName
                };
                _context.Products.Add(newProd);
                await _context.SaveChangesAsync(cancellationToken);`;

let autoCreateReplace = `var rawName = !string.IsNullOrWhiteSpace(item.ProductInfo) ? item.ProductInfo.Split('\\n')[0].Trim() : item.NewCode.Trim();
                var safeName = rawName.Length > 300 ? rawName.substring(0, 300) : rawName;
                var safeNewCode = item.NewCode.Trim().Length > 50 ? item.NewCode.Trim().substring(0, 50) : item.NewCode.Trim();
                var safeLegacyCode = item.LegacyCode?.Trim();
                if (safeLegacyCode != null && safeLegacyCode.Length > 50) safeLegacyCode = safeLegacyCode.substring(0, 50);
                var safeDescription = item.ProductInfo?.Trim();
                if (safeDescription != null && safeDescription.Length > 2000) safeDescription = safeDescription.substring(0, 2000);

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
                    _logger.LogError(ex, "Product save error at row {Row}. Inner: {Inner}", item.SortOrder, ex.InnerException?.Message);
                    throw new Exception($"Lỗi lưu sản phẩm tự động dòng {item.SortOrder}: {ex.InnerException?.Message ?? ex.Message}", ex);
                }`;
// Use JS substring equivalent string indexing `[..300]` in C#
autoCreateReplace = autoCreateReplace.replace(/substring\(0, /g, 'Substring(0, ');

content = content.replace(autoCreateFind, autoCreateReplace);

// 3. PriceListItem replacement
let pItemFind = `var pItem = new PriceListItem
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
            };`;

let pItemReplace = `var safePlGroup = item.Group?.Trim();
            if (safePlGroup != null && safePlGroup.Length > 200) safePlGroup = safePlGroup.Substring(0, 200);
            
            var safePlNewCode = string.IsNullOrWhiteSpace(item.NewCode) ? $"ITEM-{item.SortOrder:D4}" : item.NewCode.Trim();
            if (safePlNewCode.Length > 100) safePlNewCode = safePlNewCode.Substring(0, 100);
            
            var safePlLegacyCode = item.LegacyCode?.Trim();
            if (safePlLegacyCode != null && safePlLegacyCode.Length > 100) safePlLegacyCode = safePlLegacyCode.Substring(0, 100);
            
            var safePlInfo = item.ProductInfo?.Trim();
            if (safePlInfo != null && safePlInfo.Length > 2000) safePlInfo = safePlInfo.Substring(0, 2000);

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
                CurrencyCode = string.IsNullOrWhiteSpace(item.CurrencyCode) ? "VND" : item.CurrencyCode.Trim(),
                MatchStatus = item.MatchStatus,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserName
            };`;
content = content.replace(pItemFind, pItemReplace);

// 4. Final Save
let finalSaveFind = `await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync`;
let finalSaveReplace = `try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.LogError(ex, "Database persistence error during price list commit. Inner exception: {Inner}", ex.InnerException?.Message);
            throw new Exception($"Lỗi lưu bảng giá: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
        await _auditService.LogAsync`;
content = content.replace(finalSaveFind, finalSaveReplace);

// 5. Image Extraction Logging
content = content.replace("var rowImageMap = new Dictionary<int, string>();", "var rowImageMap = new Dictionary<int, string>();\n        _logger.LogInformation(\"Starting image extraction for {FileSize} bytes. Target rows: {Count}\", stream.Length, targetRows?.Count ?? 0);");
content = content.replace("var imgEntry = zip.GetEntry(mediaPath);", "_logger.LogInformation(\"Found image {MediaPath} anchored at row {ExcelRow}\", mediaPath, excelRow);\n                    var imgEntry = zip.GetEntry(mediaPath);");

let imgCatch = `catch
        {
            // If image extraction encounters unusual drawing formats, return whatever mapped so far
        }`;
let imgCatchReplace = `catch (Exception ex)
        {
            _logger.LogError(ex, "Error during image extraction. Returning {Count} images mapped so far.", rowImageMap.Count);
        }`;
content = content.replace(imgCatch, imgCatchReplace);

fs.writeFileSync(filePath, content);
console.log("Patched successfully!");
