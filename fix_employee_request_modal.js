const fs = require('fs');

const path = 'src/TradeFlow.Web/Components/Pages/Employees/EmployeeRequestModal.razor';
let lines = fs.readFileSync(path, 'utf8').split(/\r?\n/);

// 1. Highlight card
const cardIdx = lines.findIndex(l => l.includes('<div class="tf-req-card">'));
if (cardIdx !== -1) {
  lines[cardIdx] = '                            <div id="@($"req-item-{req.Id}")" class="tf-req-card @(req.Id == TargetRequestId ? "highlighted-target" : "")">';
}

// 2. Add Resubmit button in NeedAdditionalDocs status box
const needDocsIdx = lines.findIndex(l => l.includes('else if (req.Status == EmployeeRequestStatus.NeedAdditionalDocs)'));
if (needDocsIdx !== -1) {
  const boxEndIdx = lines.findIndex((l, idx) => idx > needDocsIdx && l.includes('</div>'));
  lines.splice(boxEndIdx, 1,
    '                                        <div style="display:flex; justify-content:flex-end; margin-top:8px;">',
    '                                            <button type="button" class="tf-btn tf-btn-warning tf-btn-sm" style="font-size:12px; padding:4px 10px;" @onclick="() => RequestResubmit(req)">',
    '                                                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right:4px;">',
    '                                                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>',
    '                                                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>',
    '                                                </svg>',
    '                                                Bổ sung hồ sơ &amp; Gửi lại',
    '                                            </button>',
    '                                        </div>',
    '                                    </div>'
  );
}

// 3. Add Clone button in Rejected status box
const rejIdx = lines.findIndex(l => l.includes('else if (req.Status == EmployeeRequestStatus.Rejected)'));
if (rejIdx !== -1) {
  const boxEndIdx = lines.findIndex((l, idx) => idx > rejIdx && l.includes('</div>'));
  lines.splice(boxEndIdx, 1,
    '                                        <div style="display:flex; justify-content:flex-end; margin-top:8px;">',
    '                                            <button type="button" class="tf-btn tf-btn-outline-primary tf-btn-sm" style="font-size:12px; padding:4px 10px;" @onclick="() => CloneRequest(req)">',
    '                                                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right:4px;">',
    '                                                    <rect x="9" y="9" width="13" height="13" rx="2" ry="2"/>',
    '                                                    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>',
    '                                                </svg>',
    '                                                Tạo yêu cầu mới từ hồ sơ này',
    '                                            </button>',
    '                                        </div>',
    '                                    </div>'
  );
}

// 4. Add Parameters in @code
const onApprovedIdx = lines.findIndex(l => l.includes('[Parameter] public EventCallback OnApproved { get; set; }'));
if (onApprovedIdx !== -1) {
  lines.splice(onApprovedIdx + 1, 0,
    '    [Parameter] public EventCallback<EmployeeRequestDto> OnResubmitRequested { get; set; }',
    '    [Parameter] public EventCallback<EmployeeRequestDto> OnCloneRequested { get; set; }'
  );
}

// 5. Update LoadRequestsAsync to select tab for TargetRequestId
const filterApplyIdx = lines.findIndex(l => l.includes('ApplyFilter();'));
if (filterApplyIdx !== -1) {
  lines.splice(filterApplyIdx, 0,
    '            if (TargetRequestId.HasValue)',
    '            {',
    '                var target = _allRequests.FirstOrDefault(r => r.Id == TargetRequestId.Value);',
    '                if (target != null)',
    '                {',
    '                    _selectedStatus = target.Status;',
    '                }',
    '            }'
  );
}

// 6. Add RequestResubmit & CloneRequest methods at the end of @code
const lastCloseIdx = lines.findLastIndex(l => l.trim() === '}');
if (lastCloseIdx !== -1) {
  lines.splice(lastCloseIdx, 0,
    '',
    '    private async Task RequestResubmit(EmployeeRequestDto req)',
    '    {',
    '        await Close();',
    '        if (OnResubmitRequested.HasDelegate)',
    '        {',
    '            await OnResubmitRequested.InvokeAsync(req);',
    '        }',
    '    }',
    '',
    '    private async Task CloneRequest(EmployeeRequestDto req)',
    '    {',
    '        await Close();',
    '        if (OnCloneRequested.HasDelegate)',
    '        {',
    '            await OnCloneRequested.InvokeAsync(req);',
    '        }',
    '    }'
  );
}

fs.writeFileSync(path, lines.join('\r\n'), 'utf8');
console.log('Successfully fixed EmployeeRequestModal.razor by lines');
