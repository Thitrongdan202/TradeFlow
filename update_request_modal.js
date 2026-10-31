const fs = require('fs');

const path = 'src/TradeFlow.Web/Components/Pages/Employees/EmployeeRequestModal.razor';
let content = fs.readFileSync(path, 'utf8');

// 1. Highlight target card
content = content.replace(
  '<div class="tf-req-card">',
  '<div id="@($"req-item-{req.Id}")" class="tf-req-card @(req.Id == TargetRequestId ? "highlighted-target" : "")">'
);

// 2. Add Resubmit button in NeedAdditionalDocs status box
const needDocsTarget = `                                else if (req.Status == EmployeeRequestStatus.NeedAdditionalDocs)
                                {
                                    <div class="tf-req-status-box" style="margin-top: 10px; background-color: rgba(2, 132, 199, 0.1); border: 1px solid rgba(2, 132, 199, 0.3); color: var(--tf-text-primary); border-radius: 6px; padding: 8px 12px; font-size: 12.5px;">
                                        Yêu cầu bổ sung hồ sơ bởi <strong>@req.ApprovedBy</strong> vào @req.ApprovedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                                        @if (!string.IsNullOrEmpty(req.ReviewNotes))
                                        {
                                            <span> — Nội dung cần bổ sung: <strong>@req.ReviewNotes</strong></span>
                                        }
                                    </div>
                                }`;

const needDocsReplacement = `                                else if (req.Status == EmployeeRequestStatus.NeedAdditionalDocs)
                                {
                                    <div class="tf-req-status-box" style="margin-top: 10px; background-color: rgba(2, 132, 199, 0.1); border: 1px solid rgba(2, 132, 199, 0.3); color: var(--tf-text-primary); border-radius: 6px; padding: 10px 14px; font-size: 12.5px;">
                                        <div style="display:flex; justify-content:space-between; align-items:flex-start; gap:12px;">
                                            <div>
                                                <div>Yêu cầu bổ sung hồ sơ bởi <strong>@req.ApprovedBy</strong> vào @req.ApprovedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm")</div>
                                                @if (!string.IsNullOrEmpty(req.ReviewNotes))
                                                {
                                                    <div style="margin-top:4px; font-weight:600; color:var(--tf-info);">
                                                        Nội dung cần bổ sung: @req.ReviewNotes
                                                    </div>
                                                }
                                            </div>
                                            <button type="button" class="tf-btn tf-btn-warning tf-btn-sm" style="flex-shrink:0; font-size:12px; padding:4px 10px;" @onclick="() => RequestResubmit(req)">
                                                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right:4px;">
                                                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                                                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                                                </svg>
                                                Bổ sung &amp; Gửi lại
                                            </button>
                                        </div>
                                    </div>
                                }`;

content = content.replace(needDocsTarget, needDocsReplacement);

// 3. Add Clone button in Rejected status box
const rejectedTarget = `                                else if (req.Status == EmployeeRequestStatus.Rejected)
                                {
                                    <div class="tf-req-status-box tf-req-rejected" style="margin-top: 10px;">
                                        Từ chối bởi <strong>@req.ApprovedBy</strong> vào @req.ApprovedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                                        @if (!string.IsNullOrEmpty(req.ReviewNotes))
                                        {
                                            <span> — Lý do: <strong>@req.ReviewNotes</strong></span>
                                        }
                                    </div>
                                }`;

const rejectedReplacement = `                                else if (req.Status == EmployeeRequestStatus.Rejected)
                                {
                                    <div class="tf-req-status-box tf-req-rejected" style="margin-top: 10px; padding:10px 14px;">
                                        <div style="display:flex; justify-content:space-between; align-items:flex-start; gap:12px;">
                                            <div>
                                                <div>Từ chối bởi <strong>@req.ApprovedBy</strong> vào @req.ApprovedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm")</div>
                                                @if (!string.IsNullOrEmpty(req.ReviewNotes))
                                                {
                                                    <div style="margin-top:4px;">Lý do: <strong>@req.ReviewNotes</strong></div>
                                                }
                                            </div>
                                            <button type="button" class="tf-btn tf-btn-outline-primary tf-btn-sm" style="flex-shrink:0; font-size:12px; padding:4px 10px;" @onclick="() => CloneRequest(req)">
                                                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right:4px;">
                                                    <rect x="9" y="9" width="13" height="13" rx="2" ry="2"/>
                                                    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>
                                                </svg>
                                                Tạo yêu cầu mới từ hồ sơ này
                                            </button>
                                        </div>
                                    </div>
                                }`;

content = content.replace(rejectedTarget, rejectedReplacement);

// 4. Update Parameters and Callbacks in @code
const codeTarget = `    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public int? TargetRequestId { get; set; }
    [Parameter] public EventCallback OnRequestsChanged { get; set; }
    [Parameter] public EventCallback OnApproved { get; set; }`;

const codeReplacement = `    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public int? TargetRequestId { get; set; }
    [Parameter] public EventCallback OnRequestsChanged { get; set; }
    [Parameter] public EventCallback OnApproved { get; set; }
    [Parameter] public EventCallback<EmployeeRequestDto> OnResubmitRequested { get; set; }
    [Parameter] public EventCallback<EmployeeRequestDto> OnCloneRequested { get; set; }`;

content = content.replace(codeTarget, codeReplacement);

// 5. Update LoadRequestsAsync to select the status tab for TargetRequestId
const loadTarget = `            _pendingCount = _allRequests.Count(r => r.Status == EmployeeRequestStatus.Pending);
            _needDocsCount = _allRequests.Count(r => r.Status == EmployeeRequestStatus.NeedAdditionalDocs);
            ApplyFilter();`;

const loadReplacement = `            _pendingCount = _allRequests.Count(r => r.Status == EmployeeRequestStatus.Pending);
            _needDocsCount = _allRequests.Count(r => r.Status == EmployeeRequestStatus.NeedAdditionalDocs);

            if (TargetRequestId.HasValue)
            {
                var target = _allRequests.FirstOrDefault(r => r.Id == TargetRequestId.Value);
                if (target != null)
                {
                    _selectedStatus = target.Status;
                }
            }

            ApplyFilter();`;

content = content.replace(loadTarget, loadReplacement);

// 6. Add RequestResubmit and CloneRequest methods
const endCodeTarget = `    private string GetInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "NV";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}`;

const endCodeReplacement = `    private string GetInitials(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "NV";
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private async Task RequestResubmit(EmployeeRequestDto req)
    {
        await Close();
        if (OnResubmitRequested.HasDelegate)
        {
            await OnResubmitRequested.InvokeAsync(req);
        }
    }

    private async Task CloneRequest(EmployeeRequestDto req)
    {
        await Close();
        if (OnCloneRequested.HasDelegate)
        {
            await OnCloneRequested.InvokeAsync(req);
        }
    }
}`;

content = content.replace(endCodeTarget, endCodeReplacement);

fs.writeFileSync(path, content, 'utf8');
console.log('Successfully updated EmployeeRequestModal.razor');
