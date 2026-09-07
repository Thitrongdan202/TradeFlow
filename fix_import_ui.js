const fs = require('fs');

let c = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Pricing/Import.razor', 'utf8');

c = c.replace(
    '<span>Đang phân tích dữ liệu & trích xuất hình ảnh...</span>',
    '<span>@(_progressMessage ?? "Đang phân tích dữ liệu...")</span>'
);

const codeVars = `
    private bool _isAnalyzing = false;
    private bool _isCommitting = false;
    private string? _errorMessage;
    private string? _progressMessage;
    private CancellationTokenSource? _cts;
    private ExcelAnalysisResultDto? _analysisResult;
    private ExcelImportCommitRequest _importRequest = new();
`;

c = c.replace(
    /private bool _isAnalyzing = false;[\s\S]*?private ExcelImportCommitRequest _importRequest = new\(\);/,
    codeVars
);

const handleFile = `
    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        _errorMessage = null;
        _progressMessage = "Đang tải file...";
        var file = e.File;
        if (file == null) return;

        if (!file.Name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            _errorMessage = "Vui lòng chọn file định dạng Excel (.xlsx).";
            return;
        }

        _isAnalyzing = true;
        _cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // 5 minute timeout

        try
        {
            // Max 200MB
            using var stream = file.OpenReadStream(maxAllowedSize: 209715200);
            _analysisResult = await ExcelPricingService.AnalyzeAndDryRunAsync(
                stream, 
                file.Name, 
                msg => { _progressMessage = msg; InvokeAsync(StateHasChanged); },
                _cts.Token
            );

            if (_analysisResult.Errors.Any())
            {
                _errorMessage = string.Join(" | ", _analysisResult.Errors);
            }

            // Populate commit request with smart defaults
            _importRequest = new ExcelImportCommitRequest
            {
                Name = !string.IsNullOrWhiteSpace(_analysisResult.QuotationTitle)
                    ? _analysisResult.QuotationTitle
                    : $"BẢNG GIÁ {DateTime.Now.Year} - {Path.GetFileNameWithoutExtension(file.Name)}",
                QuotationNumber = _analysisResult.QuotationNumber,
                QuotationDate = _analysisResult.QuotationDate,
                EffectiveFrom = _analysisResult.EffectiveFrom,
                EffectiveTo = _analysisResult.EffectiveTo,
                Year = _analysisResult.EffectiveFrom?.Year ?? DateTime.Now.Year,
                ProgramTitle = _analysisResult.ProgramTitle,
                PriceCondition = _analysisResult.PriceCondition,
                VatNote = _analysisResult.VatNote,
                TempFileReference = _analysisResult.TempFileReference,
                OriginalFileName = file.Name,
                InitialStatus = PriceListStatus.Active,
                AutoCreateProducts = true,
                Items = _analysisResult.Items
            };
        }
        catch (OperationCanceledException)
        {
            _errorMessage = "Quá trình đọc file Excel đã bị hủy hoặc vượt quá thời gian cho phép (5 phút).";
        }
        catch (Exception ex)
        {
            _errorMessage = $"Lỗi đọc file Excel: {ex.Message}";
        }
        finally
        {
            _isAnalyzing = false;
            _progressMessage = null;
            _cts?.Dispose();
            _cts = null;
        }
    }`;

c = c.replace(
    /private async Task HandleFileSelected[\s\S]*?finally\s*{\s*_isAnalyzing = false;\s*}\s*}/,
    handleFile
);

// We need to support canceling during commit as well? The user says:
// "Add a reasonable server-side timeout/cancellation mechanism so an import cannot hang indefinitely."
// I will also add it to CommitImport
const commitMethod = `
    private async Task CommitImport()
    {
        if (string.IsNullOrWhiteSpace(_importRequest.Name))
        {
            _errorMessage = "Vui lòng nhập tên bảng giá.";
            return;
        }

        _isCommitting = true;
        _errorMessage = null;
        _cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        try
        {
            var user = CurrentUserService.UserName ?? "admin";
            var result = await ExcelPricingService.CommitImportAsync(
                _importRequest, 
                user,
                null,
                _cts.Token
            );
            Nav.NavigateTo($"/bang-gia/chi-tiet/{result.Id}");
        }
        catch (OperationCanceledException)
        {
            _errorMessage = "Quá trình lưu bảng giá đã vượt quá thời gian cho phép.";
            _isCommitting = false;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Lỗi lưu bảng giá: {ex.Message}";
            _isCommitting = false;
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }`;

c = c.replace(
    /private async Task CommitImport\(\)[\s\S]*?_isCommitting = false;\s*}\s*}/,
    commitMethod
);

// Add using System.Threading
if (!c.includes('using System.Threading')) {
    c = c.replace('@using Microsoft.AspNetCore.Authorization', '@using Microsoft.AspNetCore.Authorization\n@using System.Threading');
}

fs.writeFileSync('src/TradeFlow.Web/Components/Pages/Pricing/Import.razor', c, 'utf8');
