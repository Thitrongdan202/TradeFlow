const fs = require('fs');

let c = fs.readFileSync('src/TradeFlow.Infrastructure/Services/ExcelPricingService.cs', 'utf8');

// 1. Update AnalyzeAndDryRunAsync signature
c = c.replace(
    'public async Task<ExcelAnalysisResultDto> AnalyzeAndDryRunAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)',
    'public async Task<ExcelAnalysisResultDto> AnalyzeAndDryRunAsync(Stream fileStream, string fileName, Action<string>? onProgress = null, CancellationToken cancellationToken = default)'
);

// 2. Update CommitImportAsync signature
c = c.replace(
    'public async Task<PriceListDto> CommitImportAsync(ExcelImportCommitRequest request, string currentUserName, CancellationToken cancellationToken = default)',
    'public async Task<PriceListDto> CommitImportAsync(ExcelImportCommitRequest request, string currentUserName, Action<string>? onProgress = null, CancellationToken cancellationToken = default)'
);

// 3. Update ExportPriceListAsync signature
c = c.replace(
    'public async Task<byte[]> ExportPriceListAsync(int priceListId, CancellationToken cancellationToken = default)',
    'public async Task<byte[]> ExportPriceListAsync(int priceListId, Action<string>? onProgress = null, CancellationToken cancellationToken = default)'
);

fs.writeFileSync('src/TradeFlow.Infrastructure/Services/ExcelPricingService.cs', c, 'utf8');
