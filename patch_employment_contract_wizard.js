const fs = require('fs');
const file = 'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web/Components/Pages/Contracts/EmploymentContractFormModal.razor';
let content = fs.readFileSync(file, 'utf8');

// 1. Fix broken SVG paths
content = content.replace(
    /<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8z"\/>/g,
    '<path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"/>'
);

// 2. Fix @key="clause.Id" to @key="clause"
content = content.replace(
    '<div class="tf-form-section p-3 mb-0" @key="clause.Id">',
    '<div class="tf-form-section p-3 mb-0" @key="clause">'
);

// 3. Update footer actions
const footerRegex = /<!-- Footer Actions -->[\s\S]*?<div class="modal-footer tf-contract-modal-footer[^>]*>[\s\S]*?<\/div>\s*<\/div>\s*(?=@\* Unsaved Changes)/;

const newFooter = `<!-- Footer Actions (Step-by-Step Wizard Navigation) -->
                <div class="modal-footer tf-contract-modal-footer px-4 py-3 d-flex justify-content-between flex-shrink-0">
                    <button type="button" class="btn btn-outline-secondary btn-sm px-3 rounded-2" @onclick="RequestCloseModal" disabled="@isSubmitting">
                        Đóng
                    </button>
                    <div class="d-flex align-items-center gap-2">
                        @if (activeTab == "info")
                        {
                            <button type="button" class="btn btn-primary btn-sm px-4 fw-semibold rounded-2 shadow-2xs d-inline-flex align-items-center gap-1.5" @onclick="GoNextStep">
                                <span>Tiếp tục: Tiền lương &amp; Chế độ</span>
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/>
                                </svg>
                            </button>
                            <button type="button" class="btn btn-outline-primary btn-sm px-3 rounded-2" @onclick="HandleSubmitAsync" disabled="@isSubmitting">
                                <span>Lưu bản nháp</span>
                            </button>
                        }
                        else if (activeTab == "remuneration")
                        {
                            <button type="button" class="btn btn-outline-secondary btn-sm px-3 rounded-2 d-inline-flex align-items-center gap-1.5" @onclick="GoPrevStep">
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <line x1="19" y1="12" x2="5" y2="12"/><polyline points="12 19 5 12 12 5"/>
                                </svg>
                                <span>Quay lại: Thông tin chung</span>
                            </button>
                            <button type="button" class="btn btn-primary btn-sm px-4 fw-semibold rounded-2 shadow-2xs d-inline-flex align-items-center gap-1.5" @onclick="GoNextStep">
                                <span>Tiếp tục: Soạn điều khoản</span>
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/>
                                </svg>
                            </button>
                            <button type="button" class="btn btn-outline-primary btn-sm px-3 rounded-2" @onclick="HandleSubmitAsync" disabled="@isSubmitting">
                                <span>Lưu bản nháp</span>
                            </button>
                        }
                        else if (activeTab == "clauses")
                        {
                            <button type="button" class="btn btn-outline-secondary btn-sm px-3 rounded-2 d-inline-flex align-items-center gap-1.5" @onclick="GoPrevStep">
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <line x1="19" y1="12" x2="5" y2="12"/><polyline points="12 19 5 12 12 5"/>
                                </svg>
                                <span>Quay lại: Tiền lương</span>
                            </button>
                            <button type="button" class="btn btn-primary btn-sm px-4 fw-semibold rounded-2 shadow-2xs d-inline-flex align-items-center gap-1.5" @onclick="GoNextStep">
                                <span>Xem trước văn bản A4</span>
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"/><circle cx="12" cy="12" r="3"/>
                                </svg>
                            </button>
                            <button type="button" class="btn btn-outline-primary btn-sm px-3 rounded-2" @onclick="HandleSubmitAsync" disabled="@isSubmitting">
                                <span>Lưu bản nháp</span>
                            </button>
                        }
                        else
                        {
                            <button type="button" class="btn btn-outline-secondary btn-sm px-3 rounded-2 d-inline-flex align-items-center gap-1.5" @onclick="GoPrevStep">
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                    <line x1="19" y1="12" x2="5" y2="12"/><polyline points="12 19 5 12 12 5"/>
                                </svg>
                                <span>Quay lại chỉnh sửa</span>
                            </button>
                            <button type="button" class="btn btn-primary btn-sm px-4 fw-semibold rounded-2 shadow-2xs" @onclick="HandleSubmitAsync" disabled="@isSubmitting">
                                @if (isSubmitting)
                                {
                                    <span class="spinner-border spinner-border-sm me-1" role="status"></span>
                                    <span>Đang lưu...</span>
                                }
                                else
                                {
                                    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="me-1.5">
                                        <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"/>
                                        <polyline points="17 21 17 13 7 13 7 21"/>
                                        <polyline points="7 3 7 8 15 8"/>
                                    </svg>
                                    <span>@(IsEditMode ? "Cập nhật hợp đồng" : "Lưu bản nháp hợp đồng")</span>
                                }
                            </button>
                        }
                    </div>
                </div>`;

if (footerRegex.test(content)) {
    content = content.replace(footerRegex, newFooter + '\r\n\r\n');
    console.log('Replaced footer actions with step-by-step navigation');
} else {
    console.log('footerRegex did not match');
}

// 4. Add Step Navigation helper methods and update SwitchModalTab
const switchTarget = `    private void SwitchModalTab(string tab)
    {
        if (activeTab == tab) return;
        activeTab = tab;
    }`;

const switchReplacement = `    private static readonly string[] WizardSteps = { "info", "remuneration", "clauses", "preview" };

    private int GetCurrentStepIndex() => activeTab switch
    {
        "remuneration" => 1,
        "clauses" => 2,
        "preview" => 3,
        _ => 0
    };

    private void GoNextStep()
    {
        var idx = GetCurrentStepIndex();
        if (idx < WizardSteps.Length - 1)
        {
            SwitchModalTab(WizardSteps[idx + 1]);
        }
    }

    private void GoPrevStep()
    {
        var idx = GetCurrentStepIndex();
        if (idx > 0)
        {
            SwitchModalTab(WizardSteps[idx - 1]);
        }
    }

    private void SwitchModalTab(string tab)
    {
        if (activeTab == tab) return;
        errorMessage = string.Empty;
        activeTab = tab;
    }`;

content = content.replace(switchTarget.replace(/\n/g, '\r\n'), switchReplacement.replace(/\n/g, '\r\n'));

fs.writeFileSync(file, content, 'utf8');
console.log('Successfully updated EmploymentContractFormModal.razor!');
