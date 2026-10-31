const fs = require('fs');

const path = 'src/TradeFlow.Web/Components/Pages/Sales/Invoices/Detail.razor';
let content = fs.readFileSync(path, 'utf8');

// 1. Update sign modal markup
const signModalTarget = `@if (_showSignModal)
{
    <div style="position:fixed; top:0; left:0; width:100%; height:100%; background:rgba(0,0,0,0.5); z-index:1000; display:flex; align-items:center; justify-content:center;">
        <div style="background:var(--tf-bg-card); color:var(--tf-text-primary); border:1px solid var(--tf-border); padding:24px; border-radius:8px; width:540px; max-width:95%; box-shadow:0 10px 25px rgba(0,0,0,0.3);">
            <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:16px;">
                <h3 style="margin:0; font-size:18px; color:var(--tf-primary);">✍ Ký điện tử hóa đơn</h3>
                <button type="button" class="tf-btn tf-btn-ghost tf-btn-sm" @onclick="() => _showSignModal = false">✕</button>
            </div>
            
            <p style="margin-bottom:16px; font-size:13px; color:var(--tf-text-muted);">
                Xác thực danh tính và ký điện tử mật mã học RSA-SHA256 cho hóa đơn <strong>@_invoice?.InvoiceSeries-@_invoice?.InvoiceNo</strong>.
            </p>

            @if (!string.IsNullOrEmpty(_signModalError))
            {
                <div class="tf-alert tf-alert-danger" style="margin-bottom:16px;">@_signModalError</div>
            }`;

const signModalReplacement = `@if (_showSignModal)
{
    <div style="position:fixed; top:0; left:0; width:100%; height:100%; background:rgba(0,0,0,0.5); z-index:1000; display:flex; align-items:center; justify-content:center;">
        <div style="background:var(--tf-bg-card); color:var(--tf-text-primary); border:1px solid var(--tf-border); padding:24px; border-radius:8px; width:540px; max-width:95%; box-shadow:0 10px 25px rgba(0,0,0,0.3);">
            <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:16px;">
                <h3 style="margin:0; font-size:18px; color:var(--tf-primary);">✍ Ký điện tử hóa đơn</h3>
                <button type="button" class="tf-btn tf-btn-ghost tf-btn-sm" @onclick="() => _showSignModal = false">✕</button>
            </div>
            
            <p style="margin-bottom:16px; font-size:13px; color:var(--tf-text-muted);">
                Xác thực danh tính và ký điện tử mật mã học RSA-SHA256 cho hóa đơn <strong>@_invoice?.InvoiceSeries-@_invoice?.InvoiceNo</strong>.
            </p>

            @if (!string.IsNullOrEmpty(_signModalError))
            {
                <div class="tf-alert tf-alert-danger" style="margin-bottom:16px;">@_signModalError</div>
            }

            @if (_isLoadingSigners)
            {
                <div style="padding:32px 16px; text-align:center;">
                    <div class="tf-spinner" style="margin:0 auto 12px auto; width:30px; height:30px;"></div>
                    <div style="font-size:13px; color:var(--tf-text-secondary);">Đang nạp danh tính người ký điện tử...</div>
                </div>
            }`;

content = content.replace(signModalTarget, signModalReplacement);

// 2. Update verify modal markup
content = content.replace(
  `@if (_showVerifyModal && _verifyResult != null)\r\n{`,
  `@if (_showVerifyModal)\r\n{`
);
content = content.replace(
  `@if (_showVerifyModal && _verifyResult != null)\n{`,
  `@if (_showVerifyModal)\n{`
);

const verifyHeaderTarget = `<button type="button" class="tf-btn tf-btn-ghost tf-btn-sm" @onclick="() => _showVerifyModal = false">✕</button>
            </div>

            <div style="margin-bottom:16px; text-align:center; padding:14px;`;

const verifyHeaderReplacement = `<button type="button" class="tf-btn tf-btn-ghost tf-btn-sm" @onclick="() => _showVerifyModal = false">✕</button>
            </div>

            @if (_isVerifyingModal || _verifyResult == null)
            {
                <div style="padding:36px 16px; text-align:center;">
                    <div class="tf-spinner" style="margin:0 auto 14px auto; width:34px; height:34px;"></div>
                    <div style="font-size:14px; font-weight:500; color:var(--tf-text-primary);">Đang kiểm tra chữ ký điện tử mật mã học...</div>
                    <div style="font-size:12px; color:var(--tf-text-muted); margin-top:4px;">Xác minh cặp khóa RSA, tính toàn vẹn gói tin và chứng thư số</div>
                </div>
            }
            else
            {
            <div style="margin-bottom:16px; text-align:center; padding:14px;`;

content = content.replace(verifyHeaderTarget, verifyHeaderReplacement);

// Add closing } for the else block before the footer in verify modal
const verifyCloseTarget = `            <div style="display:flex; justify-content:flex-end;">
                <button type="button" class="tf-btn tf-btn-secondary" @onclick="() => _showVerifyModal = false">Đóng</button>
            </div>
        </div>
    </div>
}`;

const verifyCloseReplacement = `            }
            <div style="display:flex; justify-content:flex-end; margin-top:16px;">
                <button type="button" class="tf-btn tf-btn-secondary" @onclick="() => _showVerifyModal = false">Đóng</button>
            </div>
        </div>
    </div>
}`;

content = content.replace(verifyCloseTarget, verifyCloseReplacement);

// 3. Update C# methods in Detail.razor
const varTarget = `    private bool _showSignModal = false;
    private bool _showVerifyModal = false;`;

const varReplacement = `    private bool _showSignModal = false;
    private bool _showVerifyModal = false;
    private bool _isLoadingSigners = false;
    private bool _isVerifyingModal = false;`;

content = content.replace(varTarget, varReplacement);

const openSignTarget = `    private async Task OpenSignModal()
    {
        _signModalError = null;
        _signerPin = "";
        try
        {
            var all = await SecurityService.GetAllSignersAsync();
            _availableSigners = all.Where(s => s.Status == SignerStatus.Active && s.HasCredential && !s.IsExpired).ToList();
            if (_availableSigners.Count == 1)
            {
                _selectedSignerId = _availableSigners[0].Id;
            }
            else if (_availableSigners.Count > 1)
            {
                _selectedSignerId = _availableSigners[0].Id;
            }
            else
            {
                _selectedSignerId = 0;
            }
            _showSignModal = true;
        }
        catch (Exception ex)
        {
            _errorMessage = "Lỗi khi lấy danh sách người ký: " + ex.Message;
        }
    }`;

const openSignReplacement = `    private async Task OpenSignModal()
    {
        _signModalError = null;
        _signerPin = "";
        _showSignModal = true;
        _isLoadingSigners = true;
        StateHasChanged();

        try
        {
            var all = await SecurityService.GetAllSignersAsync();
            _availableSigners = all.Where(s => s.Status == SignerStatus.Active && s.HasCredential && !s.IsExpired).ToList();
            if (_availableSigners.Count >= 1)
            {
                _selectedSignerId = _availableSigners[0].Id;
            }
            else
            {
                _selectedSignerId = 0;
            }
        }
        catch (Exception ex)
        {
            _signModalError = "Lỗi khi lấy danh sách người ký: " + ex.Message;
        }
        finally
        {
            _isLoadingSigners = false;
            StateHasChanged();
        }
    }`;

content = content.replace(openSignTarget, openSignReplacement);

const verifyTarget = `    private async Task VerifySignature()
    {
        _isProcessing = true;
        _isVerifying = true;
        _errorMessage = null;
        try
        {
            _verifyResult = await InvoiceService.VerifyInvoiceSignatureAsync(Id);
            _showVerifyModal = true;
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = "Lỗi khi kiểm tra chữ ký: " + ex.Message;
        }
        finally
        {
            _isProcessing = false;
            _isVerifying = false;
        }
    }`;

const verifyReplacement = `    private async Task VerifySignature()
    {
        _isProcessing = true;
        _isVerifying = true;
        _showVerifyModal = true;
        _isVerifyingModal = true;
        _verifyResult = null;
        _errorMessage = null;
        StateHasChanged();

        try
        {
            _verifyResult = await InvoiceService.VerifyInvoiceSignatureAsync(Id);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _verifyResult = new DigitalSignatureVerificationResult
            {
                IsValid = false,
                Status = DigitalSignatureStatus.Tampered,
                Message = "Lỗi khi kiểm tra chữ ký: " + ex.Message
            };
        }
        finally
        {
            _isProcessing = false;
            _isVerifying = false;
            _isVerifyingModal = false;
            StateHasChanged();
        }
    }`;

content = content.replace(verifyTarget, verifyReplacement);

fs.writeFileSync(path, content, 'utf8');
console.log('Successfully updated Detail.razor');
