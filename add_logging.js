const fs = require('fs');
let file = 'src/TradeFlow.Infrastructure/Services/ExcelPricingService.cs';
let content = fs.readFileSync(file, 'utf8');

// Add using Microsoft.Extensions.Logging;
if (!content.includes('using Microsoft.Extensions.Logging;')) {
    content = 'using Microsoft.Extensions.Logging;\n' + content;
}

// Add _logger field
content = content.replace(
    'private readonly IAuditService _auditService;',
    'private readonly IAuditService _auditService;\n    private readonly ILogger<ExcelPricingService> _logger;'
);

// Update constructor
content = content.replace(
    'IAuditService auditService)',
    'IAuditService auditService,\n        ILogger<ExcelPricingService> logger)'
);
content = content.replace(
    '_auditService = auditService;',
    '_auditService = auditService;\n        _logger = logger;'
);

// Add logging to ExtractImagesByRowAsync
content = content.replace(
    'var rowImageMap = new Dictionary<int, string>();',
    'var rowImageMap = new Dictionary<int, string>();\n        _logger.LogInformation("Starting image extraction for {FileSize} bytes. Target rows: {Count}", stream.Length, targetRows?.Count ?? 0);'
);

content = content.replace(
    'var imgEntry = zip.GetEntry(mediaPath);',
    '_logger.LogInformation("Found image {MediaPath} anchored at row {ExcelRow}", mediaPath, excelRow);\n                    var imgEntry = zip.GetEntry(mediaPath);'
);

content = content.replace(
    'catch',
    'catch (Exception ex)\n        {\n            _logger.LogError(ex, "Error during image extraction");\n        }'
);

content = content.replace(
    '// If image extraction encounters unusual drawing formats, return whatever mapped so far',
    ''
);

// Add logging for persistence
content = content.replace(
    'catch (DbUpdateException ex)',
    'catch (DbUpdateException ex)\n        {\n            _logger.LogError(ex, "Database persistence error during price list commit. Inner exception: {Inner}", ex.InnerException?.Message);\n            throw new Exception($"Lỗi lưu dữ liệu: {ex.InnerException?.Message ?? ex.Message}", ex);\n        }'
);

content = content.replace(
    'throw new Exception($"Lỗi lưu sản phẩm tự động dòng {item.SortOrder}: {ex.InnerException?.Message ?? ex.Message}", ex);',
    '_logger.LogError(ex, "Product save error at row {Row}. Inner: {Inner}", item.SortOrder, ex.InnerException?.Message);\n                    throw new Exception($"Lỗi lưu sản phẩm tự động dòng {item.SortOrder}: {ex.InnerException?.Message ?? ex.Message}", ex);'
);

fs.writeFileSync(file, content, 'utf8');
console.log('Done modifying ExcelPricingService.cs');
