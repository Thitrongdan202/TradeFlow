const fs = require('fs');

let filePath = "src/TradeFlow.Infrastructure/Services/ExcelPricingService.cs";
let content = fs.readFileSync(filePath, 'utf8');

let findExtraction = `var rowImageMap = new Dictionary<int, string>();
        _logger.LogInformation("Starting image extraction for {FileSize} bytes. Target rows: {Count}", stream.Length, targetRows?.Count ?? 0);`;

let replaceExtraction = `var rowImageMap = new Dictionary<int, (string storageRef, long size)>();
        _logger.LogInformation("Starting image extraction for {FileSize} bytes. Target rows: {Count}", stream.Length, targetRows?.Count ?? 0);`;
content = content.replace(findExtraction, replaceExtraction);

let findSave = `var storageRef = await _fileStorage.SaveFileAsync(ms, $"row_{excelRow}{ext}", contentType, "pricing");
                    rowImageMap[excelRow] = storageRef;`;

let replaceSave = `var storageRef = await _fileStorage.SaveFileAsync(ms, $"row_{excelRow}{ext}", contentType, "pricing");
                    if (!rowImageMap.TryGetValue(excelRow, out var existing) || ms.Length > existing.size)
                    {
                        if (existing.storageRef != null)
                        {
                            _logger.LogInformation("Overwriting existing image for row {Row} because new image is larger ({NewSize} > {OldSize})", excelRow, ms.Length, existing.size);
                        }
                        rowImageMap[excelRow] = (storageRef, ms.Length);
                    }`;
content = content.replace(findSave, replaceSave);

let findUse = `if (rowImageMap.TryGetValue(r, out var imgRef))
            {
                itemDto.ImageStorageRef = imgRef;
            }`;
let replaceUse = `if (rowImageMap.TryGetValue(r, out var imgData))
            {
                itemDto.ImageStorageRef = imgData.storageRef;
            }`;
content = content.replace(findUse, replaceUse);

fs.writeFileSync(filePath, content);
console.log("Patched image map size logic!");
