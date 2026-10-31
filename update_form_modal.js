const fs = require('fs');

const path = 'src/TradeFlow.Web/Components/Pages/Employees/EmployeeFormModal.razor';
let content = fs.readFileSync(path, 'utf8');

// 1. Add parameters in @code
const paramTarget = `    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<EmployeeDto?> OnEmployeeSaved { get; set; }
    [Parameter] public EventCallback<EmployeeDto> OnEmployeeCreated { get; set; }`;

const paramReplacement = `    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<EmployeeDto?> OnEmployeeSaved { get; set; }
    [Parameter] public EventCallback<EmployeeDto> OnEmployeeCreated { get; set; }
    [Parameter] public EmployeeRequestDto? PreloadedRequest { get; set; }
    [Parameter] public bool IsResubmitMode { get; set; }
    [Parameter] public bool IsCloneMode { get; set; }`;

content = content.replace(paramTarget, paramReplacement);

// 2. Add notice banners right after header
const bannerTarget = `            @* Fixed Mode Selector Subheader *@
            <AuthorizeView Policy="Permission:Employees:Create">`;

const bannerReplacement = `            @if (IsResubmitMode && PreloadedRequest != null)
            {
                <div style="background-color: rgba(217, 119, 6, 0.1); border-bottom: 1px solid rgba(217, 119, 6, 0.3); padding: 10px 20px; font-size: 13px; color: var(--tf-text-primary); display: flex; align-items: flex-start; gap: 8px;">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#d97706" stroke-width="2" style="flex-shrink:0; margin-top:2px;">
                        <circle cx="12" cy="12" r="10"/>
                        <line x1="12" y1="8" x2="12" y2="12"/>
                        <line x1="12" y1="16" x2="12.01" y2="16"/>
                    </svg>
                    <div>
                        <div><strong>Hồ sơ đang yêu cầu bổ sung thông tin hoặc tài liệu đính kèm:</strong></div>
                        <div style="margin-top:2px; color:#b45309; font-weight:600;">@PreloadedRequest.ReviewNotes</div>
                    </div>
                </div>
            }
            else if (IsCloneMode && PreloadedRequest != null)
            {
                <div style="background-color: rgba(2, 132, 199, 0.1); border-bottom: 1px solid rgba(2, 132, 199, 0.3); padding: 10px 20px; font-size: 13px; color: var(--tf-text-primary); display: flex; align-items: flex-start; gap: 8px;">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#0284c7" stroke-width="2" style="flex-shrink:0; margin-top:2px;">
                        <circle cx="12" cy="12" r="10"/>
                        <line x1="12" y1="16" x2="12" y2="12"/>
                        <line x1="12" y1="8" x2="12.01" y2="8"/>
                    </svg>
                    <div>
                        <div><strong>Tạo yêu cầu mới từ hồ sơ bị từ chối (@(PreloadedRequest.Code ?? $"YC-{PreloadedRequest.Id:D4}")):</strong></div>
                        <div style="margin-top:2px; color:var(--tf-text-muted); font-size:12px;">Thông tin ứng viên đã được tự động điền sẵn. Bạn có thể cập nhật và đính kèm thêm hồ sơ phù hợp.</div>
                    </div>
                </div>
            }

            @* Fixed Mode Selector Subheader *@
            <AuthorizeView Policy="Permission:Employees:Create">`;

content = content.replace(bannerTarget, bannerReplacement);

// 3. Update title
content = content.replace(
  `@(_isDirectCreate ? "Thêm nhân viên mới" : "Gửi yêu cầu phê duyệt nhân sự")`,
  `@(IsResubmitMode ? "Bổ sung hồ sơ & Gửi lại duyệt" : IsCloneMode ? "Tạo yêu cầu nhân sự mới từ hồ sơ cũ" : _isDirectCreate ? "Thêm nhân viên mới" : "Gửi yêu cầu phê duyệt nhân sự")`
);

// 4. Update submit button text
const submitBtnTarget = `<button type="submit" class="tf-btn tf-btn-primary" disabled="@_isSubmitting">
                    @(_isSubmitting ? "Đang lưu..." : (_isDirectCreate ? "Lưu nhân viên" : "Gửi yêu cầu phê duyệt"))
                </button>`;

const submitBtnReplacement = `<button type="submit" class="tf-btn tf-btn-primary" disabled="@_isSubmitting">
                    @if (_isSubmitting)
                    {
                        <span>Đang lưu...</span>
                    }
                    else if (IsResubmitMode)
                    {
                        <span>Gửi lại để duyệt</span>
                    }
                    else if (IsCloneMode)
                    {
                        <span>Gửi yêu cầu phê duyệt mới</span>
                    }
                    else if (_isDirectCreate)
                    {
                        <span>Lưu nhân viên</span>
                    }
                    else
                    {
                        <span>Gửi yêu cầu phê duyệt</span>
                    }
                </button>`;

content = content.replace(submitBtnTarget, submitBtnReplacement);

// 5. Update OnParametersSetAsync to preload
const onParamTarget = `        if (IsOpen && !_wasOpen)
        {
            _wasOpen = true;
            _model = new FormModel
            {
                StartDate = DateTime.Today,
                WorkingBranch = "Chi nhánh trung tâm",
                PayrollBranch = "Chi nhánh trung tâm",
                Gender = "Nam"
            };
            _errorMessage = null;
            _isSubmitting = false;
            _showDirtyConfirm = false;
            _pendingAttachments.Clear();
            _attachmentDescription = null;

            await LoadDataAsync();
        }`;

const onParamReplacement = `        if (IsOpen && !_wasOpen)
        {
            _wasOpen = true;
            _errorMessage = null;
            _isSubmitting = false;
            _showDirtyConfirm = false;
            _pendingAttachments.Clear();
            _attachmentDescription = null;

            if (PreloadedRequest != null)
            {
                _isDirectCreate = false;
                _model = new FormModel
                {
                    FullName = PreloadedRequest.FullName,
                    Code = PreloadedRequest.Code,
                    AttendanceCode = PreloadedRequest.AttendanceCode,
                    NationalId = PreloadedRequest.NationalId,
                    DateOfBirth = PreloadedRequest.DateOfBirth,
                    Gender = PreloadedRequest.Gender ?? "Nam",
                    Phone = PreloadedRequest.Phone,
                    Email = PreloadedRequest.Email,
                    Address = PreloadedRequest.Address,
                    DepartmentId = PreloadedRequest.DepartmentId,
                    PositionId = PreloadedRequest.PositionId,
                    WorkingBranch = PreloadedRequest.WorkingBranch ?? "Chi nhánh trung tâm",
                    PayrollBranch = PreloadedRequest.PayrollBranch ?? "Chi nhánh trung tâm",
                    StartDate = PreloadedRequest.StartDate ?? DateTime.Today,
                    BankInformation = PreloadedRequest.BankInformation,
                    Notes = IsCloneMode
                        ? $"Tạo lại từ yêu cầu bị từ chối [{(string.IsNullOrWhiteSpace(PreloadedRequest.Code) ? $"YC-{PreloadedRequest.Id:D4}" : PreloadedRequest.Code)}]: {PreloadedRequest.ReviewNotes}"
                        : PreloadedRequest.Notes,
                    UserId = PreloadedRequest.ProposedUserId
                };
            }
            else
            {
                _model = new FormModel
                {
                    StartDate = DateTime.Today,
                    WorkingBranch = "Chi nhánh trung tâm",
                    PayrollBranch = "Chi nhánh trung tâm",
                    Gender = "Nam"
                };
            }

            await LoadDataAsync();
        }`;

content = content.replace(onParamTarget, onParamReplacement);

// 6. Update HandleSubmit for IsResubmitMode
const submitHandleTarget = `            if (_isDirectCreate)
            {`;

const submitHandleReplacement = `            if (IsResubmitMode && PreloadedRequest != null)
            {
                var resubmitCmd = new ResubmitEmployeeRequestCommand
                {
                    RequestId = PreloadedRequest.Id,
                    FullName = _model.FullName.Trim(),
                    Code = _model.Code?.Trim(),
                    AttendanceCode = _model.AttendanceCode?.Trim(),
                    Phone = _model.Phone?.Trim(),
                    Email = _model.Email?.Trim(),
                    NationalId = _model.NationalId?.Trim(),
                    DateOfBirth = _model.DateOfBirth,
                    Gender = _model.Gender?.Trim(),
                    Address = _model.Address?.Trim(),
                    DepartmentId = _model.DepartmentId,
                    PositionId = _model.PositionId,
                    WorkingBranch = _model.WorkingBranch?.Trim(),
                    PayrollBranch = _model.PayrollBranch?.Trim(),
                    StartDate = _model.StartDate,
                    BankInformation = _model.BankInformation?.Trim(),
                    Notes = _model.Notes?.Trim(),
                    ProposedUserId = !string.IsNullOrWhiteSpace(_model.UserId) ? _model.UserId.Trim() : null,
                    ResubmitNotes = "Đã cập nhật thông tin và bổ sung hồ sơ theo yêu cầu."
                };

                await EmployeeService.ResubmitEmployeeRequestAsync(resubmitCmd, username);

                if (_pendingAttachments.Any())
                {
                    var categories = await DocumentService.GetCategoriesAsync();

                    foreach (var att in _pendingAttachments)
                    {
                        var matchedCat = categories.FirstOrDefault(c => c.Name.Equals(att.Category, StringComparison.OrdinalIgnoreCase));
                        int? catId = matchedCat?.Id;

                        using var fileStream = new MemoryStream(att.Bytes);
                        await DocumentService.UploadDocumentAsync(
                            fileStream,
                            att.FileName,
                            att.ContentType,
                            att.Bytes.Length,
                            "EmployeeRequest",
                            PreloadedRequest.Id,
                            catId,
                            att.Description);
                    }
                }

                if (OnEmployeeSaved.HasDelegate)
                {
                    await OnEmployeeSaved.InvokeAsync(null);
                }
            }
            else if (_isDirectCreate)
            {`;

content = content.replace(submitHandleTarget, submitHandleReplacement);

fs.writeFileSync(path, content, 'utf8');
console.log('Successfully updated EmployeeFormModal.razor');
