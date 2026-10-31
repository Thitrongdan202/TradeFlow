const fs = require('fs');

const path = 'src/TradeFlow.Web/Components/Layout/NotificationMenu.razor';
let content = fs.readFileSync(path, 'utf8');

const targetElseTime = `                                    else
                                    {
                                        <div class="tf-notif-item-time">@n.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")</div>
                                    }`;

const replacementElseTime = `                                    else
                                    {
                                        <div style="display:flex; justify-content:space-between; align-items:center; margin-top:8px;" @onclick:stopPropagation="true">
                                            <div class="tf-notif-item-time">@n.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")</div>
                                            @if (!string.IsNullOrEmpty(n.ActionButtonText) && !string.IsNullOrEmpty(n.TargetUrl))
                                            {
                                                <a href="@n.TargetUrl" class="tf-btn-action-pill @(n.Type == "warning" ? "warning" : n.Type == "error" ? "danger" : n.Type == "success" ? "success" : "")" @onclick="CloseDropdown">
                                                    @n.ActionButtonText &rarr;
                                                </a>
                                            }
                                        </div>
                                    }`;

content = content.replace(targetElseTime, replacementElseTime);

// Also update the approval detail link text if ActionButtonText is set
const targetApprLink = `<a href="@n.TargetUrl" class="tf-notif-detail-link" @onclick="CloseDropdown">
                                                    Chi tiết &rarr;
                                                </a>`;

const replacementApprLink = `<a href="@n.TargetUrl" class="tf-notif-detail-link" @onclick="CloseDropdown">
                                                    @(n.ActionButtonText ?? "Chi tiết") &rarr;
                                                </a>`;

content = content.replace(targetApprLink, replacementApprLink);

fs.writeFileSync(path, content, 'utf8');
console.log('Successfully updated NotificationMenu.razor');
