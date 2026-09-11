const fs = require('fs');

const filePath = 'src/TradeFlow.Infrastructure/Services/ExcelPricingService.cs';
let src = fs.readFileSync(filePath, 'utf8');
const originalLength = src.length;

// ── helper: assert a replacement happened exactly once ────────────────────────
function replaceOnce(label, src, oldStr, newStr) {
    const count = src.split(oldStr).length - 1;
    if (count !== 1) {
        console.error(`✗ ${label}: expected 1 match, found ${count}`);
        process.exit(1);
    }
    console.log(`✓ ${label}`);
    return src.replace(oldStr, newStr);
}

// ── 1. ILogger using statement ────────────────────────────────────────────────
if (!src.includes('using Microsoft.Extensions.Logging;')) {
    src = 'using Microsoft.Extensions.Logging;\r\n' + src;
    console.log('✓ Added using Microsoft.Extensions.Logging');
} else {
    console.log('  (ILogger using already present)');
}

// ── 2. ILogger field ──────────────────────────────────────────────────────────
if (!src.includes('ILogger<ExcelPricingService> _logger')) {
    src = replaceOnce(
        'ILogger field',
        src,
        '    private readonly IAuditService _auditService;\r\n',
        '    private readonly IAuditService _auditService;\r\n    private readonly ILogger<ExcelPricingService> _logger;\r\n'
    );

    src = replaceOnce(
        'ILogger constructor param',
        src,
        '        IAuditService auditService)\r\n',
        '        IAuditService auditService,\r\n        ILogger<ExcelPricingService> logger)\r\n'
    );

    src = replaceOnce(
        'ILogger constructor body',
        src,
        '        _auditService = auditService;\r\n',
        '        _auditService = auditService;\r\n        _logger = logger;\r\n'
    );
} else {
    console.log('  (ILogger already present)');
}

// ── 3. Auto-create product: truncate fields + surface DbUpdateException ───────
const OLD_AUTOCREATE =
    'var prodCode = await _codeGenerator.GenerateCodeAsync(SystemCodeConstants.Product, cancellationToken);\r\n' +
    '                var newProd = new Product\r\n' +
    '                {\r\n' +
    '                    Code = prodCode,\r\n' +
    '                    NewCode = item.NewCode.Trim(),\r\n' +
    '                    LegacyCode = item.LegacyCode?.Trim(),\r\n' +
    '                    Name = !string.IsNullOrWhiteSpace(item.ProductInfo) ? item.ProductInfo.Split(\'\\n\')[0].Trim() : item.NewCode.Trim(),\r\n' +
    '                    Description = item.ProductInfo?.Trim(),\r\n' +
    '                    IsActive = true,\r\n' +
    '                    CreatedAt = DateTime.UtcNow,\r\n' +
    '                    CreatedBy = currentUserName\r\n' +
    '                };\r\n' +
    '                _context.Products.Add(newProd);\r\n' +
    '                await _context.SaveChangesAsync(cancellationToken);\r\n' +
    '                productId = newProd.Id;\r\n' +
    '                item.MatchedProductId = newProd.Id;\r\n' +
    '                item.MatchStatus = PriceMatchStatus.Matched;';

const NEW_AUTOCREATE =
    'var prodCode = await _codeGenerator.GenerateCodeAsync(SystemCodeConstants.Product, cancellationToken);\r\n' +
    '\r\n' +
    '                var rawName = !string.IsNullOrWhiteSpace(item.ProductInfo) ? item.ProductInfo.Split(\'\\n\')[0].Trim() : item.NewCode.Trim();\r\n' +
    '                var safeName = rawName.Length > 300 ? rawName.Substring(0, 300) : rawName;\r\n' +
    '                var safeNewCode = item.NewCode.Trim().Length > 50 ? item.NewCode.Trim().Substring(0, 50) : item.NewCode.Trim();\r\n' +
    '                var safeLegacyCode = item.LegacyCode?.Trim();\r\n' +
    '                if (safeLegacyCode != null && safeLegacyCode.Length > 50) safeLegacyCode = safeLegacyCode.Substring(0, 50);\r\n' +
    '                var safeDescription = item.ProductInfo?.Trim();\r\n' +
    '                if (safeDescription != null && safeDescription.Length > 2000) safeDescription = safeDescription.Substring(0, 2000);\r\n' +
    '\r\n' +
    '                var newProd = new Product\r\n' +
    '                {\r\n' +
    '                    Code = prodCode,\r\n' +
    '                    NewCode = safeNewCode,\r\n' +
    '                    LegacyCode = safeLegacyCode,\r\n' +
    '                    Name = safeName,\r\n' +
    '                    Description = safeDescription,\r\n' +
    '                    IsActive = true,\r\n' +
    '                    CreatedAt = DateTime.UtcNow,\r\n' +
    '                    CreatedBy = currentUserName\r\n' +
    '                };\r\n' +
    '                _context.Products.Add(newProd);\r\n' +
    '                try\r\n' +
    '                {\r\n' +
    '                    await _context.SaveChangesAsync(cancellationToken);\r\n' +
    '                }\r\n' +
    '                catch (DbUpdateException ex)\r\n' +
    '                {\r\n' +
    '                    _logger.LogError(ex, "Product save error at row {Row}. Inner: {Inner}", item.SortOrder, ex.InnerException?.Message);\r\n' +
    '                    throw new Exception($"L\\u1ed7i l\\u01b0u s\\u1ea3n ph\\u1ea9m t\\u1ef1 \\u0111\\u1ed9ng d\\u00f2ng {item.SortOrder}: {ex.InnerException?.Message ?? ex.Message}", ex);\r\n' +
    '                }\r\n' +
    '                productId = newProd.Id;\r\n' +
    '                item.MatchedProductId = newProd.Id;\r\n' +
    '                item.MatchStatus = PriceMatchStatus.Matched;';

src = replaceOnce('Auto-create product truncation + error surfacing', src, OLD_AUTOCREATE, NEW_AUTOCREATE);

// ── 4. PriceListItem: truncate fields ─────────────────────────────────────────
const OLD_PITEM =
    '            var pItem = new PriceListItem\r\n' +
    '            {\r\n' +
    '                PriceListId = priceList.Id,\r\n' +
    '                ProductId = productId,\r\n' +
    '                SortOrder = item.SortOrder,\r\n' +
    '                Group = item.Group?.Trim(),\r\n' +
    '                NewCode = string.IsNullOrWhiteSpace(item.NewCode) ? $"ITEM-{item.SortOrder:D4}" : item.NewCode.Trim(),\r\n' +
    '                LegacyCode = item.LegacyCode?.Trim(),\r\n' +
    '                ProductInfo = item.ProductInfo?.Trim(),\r\n' +
    '                ImageStorageRef = item.ImageStorageRef,\r\n' +
    '                UnitPrice = item.UnitPrice,\r\n' +
    '                CurrencyCode = string.IsNullOrWhiteSpace(item.CurrencyCode) ? "VND" : item.CurrencyCode.Trim(),\r\n' +
    '                MatchStatus = item.MatchStatus,\r\n' +
    '                CreatedAt = DateTime.UtcNow,\r\n' +
    '                CreatedBy = currentUserName\r\n' +
    '            };';

const NEW_PITEM =
    '            var safePlGroup = item.Group?.Trim();\r\n' +
    '            if (safePlGroup != null && safePlGroup.Length > 200) safePlGroup = safePlGroup.Substring(0, 200);\r\n' +
    '\r\n' +
    '            var safePlNewCode = string.IsNullOrWhiteSpace(item.NewCode) ? $"ITEM-{item.SortOrder:D4}" : item.NewCode.Trim();\r\n' +
    '            if (safePlNewCode.Length > 100) safePlNewCode = safePlNewCode.Substring(0, 100);\r\n' +
    '\r\n' +
    '            var safePlLegacyCode = item.LegacyCode?.Trim();\r\n' +
    '            if (safePlLegacyCode != null && safePlLegacyCode.Length > 100) safePlLegacyCode = safePlLegacyCode.Substring(0, 100);\r\n' +
    '\r\n' +
    '            var safePlInfo = item.ProductInfo?.Trim();\r\n' +
    '            if (safePlInfo != null && safePlInfo.Length > 2000) safePlInfo = safePlInfo.Substring(0, 2000);\r\n' +
    '\r\n' +
    '            var pItem = new PriceListItem\r\n' +
    '            {\r\n' +
    '                PriceListId = priceList.Id,\r\n' +
    '                ProductId = productId,\r\n' +
    '                SortOrder = item.SortOrder,\r\n' +
    '                Group = safePlGroup,\r\n' +
    '                NewCode = safePlNewCode,\r\n' +
    '                LegacyCode = safePlLegacyCode,\r\n' +
    '                ProductInfo = safePlInfo,\r\n' +
    '                ImageStorageRef = item.ImageStorageRef,\r\n' +
    '                UnitPrice = item.UnitPrice,\r\n' +
    '                CurrencyCode = string.IsNullOrWhiteSpace(item.CurrencyCode) ? "VND" : item.CurrencyCode.Trim(),\r\n' +
    '                MatchStatus = item.MatchStatus,\r\n' +
    '                CreatedAt = DateTime.UtcNow,\r\n' +
    '                CreatedBy = currentUserName\r\n' +
    '            };';

src = replaceOnce('PriceListItem field truncation', src, OLD_PITEM, NEW_PITEM);

// ── 5. Wrap the final batch SaveChangesAsync ──────────────────────────────────
const OLD_FINAL_SAVE =
    '        await _context.SaveChangesAsync(cancellationToken);\r\n' +
    '        await _auditService.LogAsync(AuditEventType.PriceListImported, currentUserName, "PriceList", priceList.Code);';

const NEW_FINAL_SAVE =
    '        try\r\n' +
    '        {\r\n' +
    '            await _context.SaveChangesAsync(cancellationToken);\r\n' +
    '        }\r\n' +
    '        catch (DbUpdateException ex)\r\n' +
    '        {\r\n' +
    '            _logger.LogError(ex, "Database persistence error during price list commit. Inner exception: {Inner}", ex.InnerException?.Message);\r\n' +
    '            throw new Exception($"L\\u1ed7i l\\u01b0u b\\u1ea3ng gi\\u00e1: {ex.InnerException?.Message ?? ex.Message}", ex);\r\n' +
    '        }\r\n' +
    '        await _auditService.LogAsync(AuditEventType.PriceListImported, currentUserName, "PriceList", priceList.Code);';

src = replaceOnce('Final SaveChangesAsync error surfacing', src, OLD_FINAL_SAVE, NEW_FINAL_SAVE);

// ── 6. Replace ExtractImagesByRowAsync with logger + keep-largest ─────────────
// Build the exact original from the captured JSON output
const OLD_EXTRACT =
    '    private async Task<Dictionary<int, string>> ExtractImagesByRowAsync(Stream stream, HashSet<int>? targetRows = null)\r\n' +
    '    {\r\n' +
    '        var rowImageMap = new Dictionary<int, string>();\r\n' +
    '        try\r\n' +
    '        {\r\n' +
    '            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);\r\n' +
    '\r\n' +
    '            // Look for drawing files\r\n' +
    '            var drawingEntries = zip.Entries.Where(e => e.FullName.StartsWith("xl/drawings/drawing") && e.FullName.EndsWith(".xml")).ToList();\r\n' +
    '            if (!drawingEntries.Any()) return rowImageMap;\r\n' +
    '\r\n' +
    '            foreach (var drawingEntry in drawingEntries)\r\n' +
    '            {\r\n' +
    '                var relPath = $"xl/drawings/_rels/{Path.GetFileName(drawingEntry.FullName)}.rels";\r\n' +
    '                var relEntry = zip.GetEntry(relPath);\r\n' +
    '                if (relEntry == null) continue;\r\n' +
    '\r\n' +
    '                // Load relationships mapping rId -> media file\r\n' +
    '                var relsDoc = XDocument.Load(relEntry.Open());\r\n' +
    '                XNamespace relsNs = "http://schemas.openxmlformats.org/package/2006/relationships";\r\n' +
    '                var idToTarget = relsDoc.Descendants(relsNs + "Relationship")\r\n' +
    '                    .ToDictionary(\r\n' +
    '                        r => (string)r.Attribute("Id")!,\r\n' +
    '                        r => (string)r.Attribute("Target")!\r\n' +
    '                    );\r\n' +
    '\r\n' +
    '                // Load drawings XML\r\n' +
    '                var drawingDoc = XDocument.Load(drawingEntry.Open());\r\n' +
    '                XNamespace xdr = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";\r\n' +
    '                XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";\r\n' +
    '                XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";\r\n' +
    '\r\n' +
    '                var anchors = drawingDoc.Descendants(xdr + "twoCellAnchor")\r\n' +
    '                    .Concat(drawingDoc.Descendants(xdr + "oneCellAnchor"));\r\n' +
    '\r\n' +
    '                foreach (var anchor in anchors)\r\n' +
    '                {\r\n' +
    '                    var from = anchor.Element(xdr + "from");\r\n' +
    '                    if (from == null) continue;\r\n' +
    '\r\n' +
    '                    var rowElem = from.Element(xdr + "row");\r\n' +
    '                    if (rowElem == null || !int.TryParse(rowElem.Value, out var xmlRow)) continue;\r\n' +
    '\r\n' +
    '                    // xmlRow is 0-indexed, corresponding to 1-indexed Excel row: xmlRow + 1\r\n' +
    '                    int excelRow = xmlRow + 1;\r\n' +
    '\r\n' +
    '                    var blip = anchor.Descendants(a + "blip").FirstOrDefault();\r\n' +
    '                    if (blip == null) continue;\r\n' +
    '\r\n' +
    '                    var embedId = (string?)blip.Attribute(r + "embed");\r\n' +
    '                    if (string.IsNullOrEmpty(embedId) || !idToTarget.TryGetValue(embedId, out var targetPath)) continue;\r\n' +
    '\r\n' +
    '                    // Resolve target path (e.g. "../media/image1.png" -> "xl/media/image1.png")\r\n' +
    "                    var mediaPath = targetPath.Replace(\"../\", \"xl/\").Replace('\\\\', '/');\r\n" +
    '                    var imgEntry = zip.GetEntry(mediaPath);\r\n' +
    '                    if (imgEntry == null) continue;\r\n' +
    '\r\n' +
    '                    using var imgStream = imgEntry.Open();\r\n' +
    '                    using var ms = new MemoryStream();\r\n' +
    '                    imgStream.CopyTo(ms);\r\n' +
    '                    ms.Position = 0;\r\n' +
    '\r\n' +
    '                    var ext = Path.GetExtension(mediaPath);\r\n' +
    '                    var contentType = ext.ToLower() switch\r\n' +
    '                    {\r\n' +
    '                        ".jpg" or ".jpeg" => "image/jpeg",\r\n' +
    '                        ".png" => "image/png",\r\n' +
    '                        ".webp" => "image/webp",\r\n' +
    '                        _ => "application/octet-stream"\r\n' +
    '                    };\r\n' +
    '\r\n' +
    '                    if (targetRows != null && !targetRows.Contains(excelRow)) continue;\r\n' +
    '                    var storageRef = await _fileStorage.SaveFileAsync(ms, $"row_{excelRow}{ext}", contentType, "pricing");\r\n' +
    '                    rowImageMap[excelRow] = storageRef;\r\n' +
    '                }\r\n' +
    '            }\r\n' +
    '        }\r\n' +
    '        catch\r\n' +
    '        {\r\n' +
    '            // If image extraction encounters unusual drawing formats, return whatever mapped so far\r\n' +
    '        }\r\n' +
    '\r\n' +
    '        return rowImageMap;\r\n' +
    '    }\r\n';

const NEW_EXTRACT =
    '    private async Task<Dictionary<int, string>> ExtractImagesByRowAsync(Stream stream, HashSet<int>? targetRows = null)\r\n' +
    '    {\r\n' +
    '        var rowImageMap = new Dictionary<int, string>();\r\n' +
    '        var rowImageSizes = new Dictionary<int, long>(); // tracks byte size to keep largest image per row\r\n' +
    '        _logger.LogInformation("Starting image extraction for {FileSize} bytes. Target rows: {Count}", stream.Length, targetRows?.Count ?? 0);\r\n' +
    '        try\r\n' +
    '        {\r\n' +
    '            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);\r\n' +
    '\r\n' +
    '            // Look for drawing files\r\n' +
    '            var drawingEntries = zip.Entries.Where(e => e.FullName.StartsWith("xl/drawings/drawing") && e.FullName.EndsWith(".xml")).ToList();\r\n' +
    '            if (!drawingEntries.Any()) return rowImageMap;\r\n' +
    '\r\n' +
    '            foreach (var drawingEntry in drawingEntries)\r\n' +
    '            {\r\n' +
    '                var relPath = $"xl/drawings/_rels/{Path.GetFileName(drawingEntry.FullName)}.rels";\r\n' +
    '                var relEntry = zip.GetEntry(relPath);\r\n' +
    '                if (relEntry == null) continue;\r\n' +
    '\r\n' +
    '                // Load relationships mapping rId -> media file\r\n' +
    '                var relsDoc = XDocument.Load(relEntry.Open());\r\n' +
    '                XNamespace relsNs = "http://schemas.openxmlformats.org/package/2006/relationships";\r\n' +
    '                var idToTarget = relsDoc.Descendants(relsNs + "Relationship")\r\n' +
    '                    .ToDictionary(\r\n' +
    '                        r => (string)r.Attribute("Id")!,\r\n' +
    '                        r => (string)r.Attribute("Target")!\r\n' +
    '                    );\r\n' +
    '\r\n' +
    '                // Load drawings XML\r\n' +
    '                var drawingDoc = XDocument.Load(drawingEntry.Open());\r\n' +
    '                XNamespace xdr = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";\r\n' +
    '                XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";\r\n' +
    '                XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";\r\n' +
    '\r\n' +
    '                var anchors = drawingDoc.Descendants(xdr + "twoCellAnchor")\r\n' +
    '                    .Concat(drawingDoc.Descendants(xdr + "oneCellAnchor"));\r\n' +
    '\r\n' +
    '                foreach (var anchor in anchors)\r\n' +
    '                {\r\n' +
    '                    var from = anchor.Element(xdr + "from");\r\n' +
    '                    if (from == null) continue;\r\n' +
    '\r\n' +
    '                    var rowElem = from.Element(xdr + "row");\r\n' +
    '                    if (rowElem == null || !int.TryParse(rowElem.Value, out var xmlRow)) continue;\r\n' +
    '\r\n' +
    '                    // xmlRow is 0-indexed, corresponding to 1-indexed Excel row: xmlRow + 1\r\n' +
    '                    int excelRow = xmlRow + 1;\r\n' +
    '\r\n' +
    '                    var blip = anchor.Descendants(a + "blip").FirstOrDefault();\r\n' +
    '                    if (blip == null) continue;\r\n' +
    '\r\n' +
    '                    var embedId = (string?)blip.Attribute(r + "embed");\r\n' +
    '                    if (string.IsNullOrEmpty(embedId) || !idToTarget.TryGetValue(embedId, out var targetPath)) continue;\r\n' +
    '\r\n' +
    '                    // Resolve target path (e.g. "../media/image1.png" -> "xl/media/image1.png")\r\n' +
    "                    var mediaPath = targetPath.Replace(\"../\", \"xl/\").Replace('\\\\', '/');\r\n" +
    '                    _logger.LogInformation("Found image {MediaPath} anchored at row {ExcelRow}", mediaPath, excelRow);\r\n' +
    '                    var imgEntry = zip.GetEntry(mediaPath);\r\n' +
    '                    if (imgEntry == null) continue;\r\n' +
    '\r\n' +
    '                    using var imgStream = imgEntry.Open();\r\n' +
    '                    using var ms = new MemoryStream();\r\n' +
    '                    imgStream.CopyTo(ms);\r\n' +
    '                    ms.Position = 0;\r\n' +
    '\r\n' +
    '                    var ext = Path.GetExtension(mediaPath);\r\n' +
    '                    var contentType = ext.ToLower() switch\r\n' +
    '                    {\r\n' +
    '                        ".jpg" or ".jpeg" => "image/jpeg",\r\n' +
    '                        ".png" => "image/png",\r\n' +
    '                        ".webp" => "image/webp",\r\n' +
    '                        _ => "application/octet-stream"\r\n' +
    '                    };\r\n' +
    '\r\n' +
    '                    if (targetRows != null && !targetRows.Contains(excelRow)) continue;\r\n' +
    '\r\n' +
    '                    // If multiple images are anchored on the same row, keep the largest (most likely the product image)\r\n' +
    '                    var imageSize = ms.Length;\r\n' +
    '                    if (rowImageSizes.TryGetValue(excelRow, out var existingSize) && existingSize >= imageSize)\r\n' +
    '                    {\r\n' +
    '                        _logger.LogInformation("Skipping smaller duplicate image for row {Row} ({NewSize} < {OldSize})", excelRow, imageSize, existingSize);\r\n' +
    '                        continue;\r\n' +
    '                    }\r\n' +
    '\r\n' +
    '                    var storageRef = await _fileStorage.SaveFileAsync(ms, $"row_{excelRow}{ext}", contentType, "pricing");\r\n' +
    '                    rowImageMap[excelRow] = storageRef;\r\n' +
    '                    rowImageSizes[excelRow] = imageSize;\r\n' +
    '                }\r\n' +
    '            }\r\n' +
    '        }\r\n' +
    '        catch (Exception ex)\r\n' +
    '        {\r\n' +
    '            _logger.LogError(ex, "Error during image extraction. Returning {Count} images mapped so far.", rowImageMap.Count);\r\n' +
    '        }\r\n' +
    '\r\n' +
    '        return rowImageMap;\r\n' +
    '    }\r\n';

src = replaceOnce('ExtractImagesByRowAsync (logger + keep-largest, correct Dictionary<int,string>)', src, OLD_EXTRACT, NEW_EXTRACT);

fs.writeFileSync(filePath, src, 'utf8');
console.log(`\nDone. File size: ${originalLength} → ${src.length} bytes`);
