path_razor = r"C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Web\Components\Layout\UserMenu.razor"
path_css = r"C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Web\Components\Layout\UserMenu.razor.css"

with open(path_razor, "r", encoding="utf-8") as f:
    text_razor = f.read()

# Update button markup in UserMenu.razor
old_btn_start = '<button type="button"'
old_btn_end = '</button>'

b_start = text_razor.find('<button type="button"')
b_end = text_razor.find('</button>', b_start) + len('</button>')

new_button = '''<button type="button"
                    class="tf-user-btn @(_isAdmin ? "tf-admin-user-btn" : "")"
                    aria-label="Tài khoản cá nhân"
                    aria-expanded="@_isOpen"
                    aria-haspopup="true"
                    id="tf-user-menu-btn"
                    @onclick="ToggleDropdown">
                @if (_isAdmin)
                {
                    <div class="tf-avatar tf-admin-avatar" title="Quản trị tối cao (System Root)">
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                            <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
                            <polyline points="9 12 11 14 15 10"/>
                        </svg>
                    </div>
                    <div class="tf-admin-chip-content">
                        <span class="tf-user-name tf-admin-name">Owner / Admin</span>
                        <span class="tf-admin-root-pill">ROOT</span>
                    </div>
                }
                else
                {
                    <div class="tf-avatar">@GetInitials(_displayName)</div>
                    <div class="tf-user-info">
                        <span class="tf-user-name">@TruncateName(_displayName)</span>
                        <span class="tf-user-role">@TruncateName(_role)</span>
                    </div>
                }
                <svg class="tf-user-chevron @(_isOpen ? "expanded" : "")" width="13" height="13" viewBox="0 0 24 24" fill="none"
                     stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                    <polyline points="6 9 12 15 18 9"/>
                </svg>
            </button>'''

if b_start != -1 and b_end != -1:
    text_razor = text_razor[:b_start] + new_button + text_razor[b_end:]

# Also update the dropdown header for Owner/Admin
old_admin_header = '''                    @if (_isAdmin)
                    {
                        <div class="tf-user-menu-header tf-admin-dropdown-header">
                            <div class="tf-avatar tf-avatar-lg tf-admin-avatar-lg">
                                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                                    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
                                    <polyline points="9 12 11 14 15 10"/>
                                </svg>
                            </div>
                            <div style="overflow:hidden;">
                                <div class="tf-font-semibold tf-admin-header-title">
                                    <span>OWNER / ADMIN</span>
                                    <span class="tf-admin-badge-hdr">System Owner</span>
                                </div>
                                <div class="tf-text-xs tf-text-muted">Toàn quyền quản trị hệ thống</div>
                            </div>
                        </div>'''

new_admin_header = '''                    @if (_isAdmin)
                    {
                        <div class="tf-user-menu-header tf-admin-dropdown-header">
                            <div class="tf-avatar tf-admin-avatar-lg">
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                                    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
                                    <polyline points="9 12 11 14 15 10"/>
                                </svg>
                            </div>
                            <div style="overflow:hidden;">
                                <div class="tf-admin-header-title">
                                    <span style="font-weight:600; font-size:13.5px; color:var(--tf-text-primary);">Owner / Admin</span>
                                    <span class="tf-admin-root-pill" style="font-size:9.5px; padding:1px 5px;">ROOT</span>
                                </div>
                                <div class="tf-text-xs tf-text-muted" style="margin-top:2px;">Toàn quyền quản trị hệ thống</div>
                            </div>
                        </div>'''

if old_admin_header in text_razor:
    text_razor = text_razor.replace(old_admin_header, new_admin_header)

with open(path_razor, "w", encoding="utf-8", newline="") as f:
    f.write(text_razor)

print("Updated UserMenu.razor successfully.")

# Update UserMenu.razor.css
new_css = '''/* UserMenu scoped styles */

.tf-user-dropdown-container {
    position: relative;
}

.tf-user-btn {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    height: 38px;
    padding: 3px 8px;
    border-radius: var(--tf-radius-md, 8px);
    border: none;
    background: transparent;
    cursor: pointer;
    color: var(--tf-text-primary);
    transition: background-color var(--tf-transition, 0.15s ease);
}

.tf-user-btn:hover {
    background-color: var(--tf-gray-100, #f1f5f9);
}

[data-bs-theme="dark"] .tf-user-btn:hover {
    background-color: #292E39;
}

.tf-user-name {
    font-size: var(--tf-text-sm, 13px);
    font-weight: 500;
    color: var(--tf-text-primary);
    max-width: 140px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    line-height: 1.25;
}

.tf-user-role {
    font-size: 11px;
    color: var(--tf-text-muted, #64748b);
    line-height: 1.2;
    white-space: nowrap;
}

.tf-user-info {
    display: flex;
    flex-direction: column;
    text-align: left;
    justify-content: center;
}

/* Owner / Admin Single Row Chip */
.tf-admin-chip-content {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    line-height: 1;
}

.tf-admin-name {
    font-size: 13px;
    font-weight: 600;
    color: var(--tf-text-primary);
    letter-spacing: -0.1px;
}

.tf-admin-root-pill {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 9.5px;
    font-weight: 700;
    line-height: 1;
    padding: 2px 5px;
    border-radius: 4px;
    letter-spacing: 0.4px;
    text-transform: uppercase;
    background: rgba(2, 132, 199, 0.08);
    color: #0284c7;
    border: 1px solid rgba(2, 132, 199, 0.22);
}

[data-bs-theme="dark"] .tf-admin-root-pill {
    background: rgba(56, 189, 248, 0.12);
    color: #38bdf8;
    border-color: rgba(56, 189, 248, 0.28);
}

/* Avatar styling */
.tf-avatar {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    background: linear-gradient(135deg, var(--tf-primary, #0284c7) 0%, #6366f1 100%);
    color: #ffffff;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-size: 12px;
    font-weight: 600;
    flex-shrink: 0;
}

.tf-admin-avatar {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    background: #0284c7;
    color: #ffffff;
    border: 1px solid rgba(2, 132, 199, 0.3);
    box-shadow: none;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
}

[data-bs-theme="dark"] .tf-admin-avatar {
    background: #0f172a;
    color: #38bdf8;
    border-color: rgba(56, 189, 248, 0.3);
}

.tf-admin-avatar-lg {
    width: 38px;
    height: 38px;
    border-radius: 50%;
    background: #0284c7;
    color: #ffffff;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
}

[data-bs-theme="dark"] .tf-admin-avatar-lg {
    background: #0f172a;
    color: #38bdf8;
    border: 1px solid rgba(56, 189, 248, 0.3);
}

.tf-admin-header-title {
    display: flex;
    align-items: center;
    gap: 6px;
    white-space: nowrap;
}

.tf-user-chevron {
    color: var(--tf-text-muted, #94a3b8);
    transition: transform 0.2s ease;
    flex-shrink: 0;
}

.tf-user-chevron.expanded {
    transform: rotate(180deg);
}

.tf-user-menu {
    right: 0;
    min-width: 250px;
    max-width: calc(100vw - 32px);
    top: calc(100% + 8px);
    background: var(--tf-surface, #ffffff);
    border: 1px solid var(--tf-border, #e2e8f0);
    border-radius: 8px;
    box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1);
    padding: 6px 0;
    animation: tf-dropdown-fade 0.15s ease-out;
}

[data-bs-theme="dark"] .tf-user-menu {
    background-color: #1D2028;
    border-color: #383E4B;
    box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.45);
}

@keyframes tf-dropdown-fade {
    from {
        opacity: 0;
        transform: translateY(-4px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

.tf-user-menu-header {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 16px;
}

.tf-avatar-lg {
    width: 38px;
    height: 38px;
    font-size: 13px;
    font-weight: 600;
    flex-shrink: 0;
    border-radius: 50%;
    background: var(--tf-primary-light, #e0f2fe);
    color: var(--tf-primary, #0284c7);
    display: flex;
    align-items: center;
    justify-content: center;
}

.tf-user-btn:focus-visible {
    outline: 2px solid var(--tf-primary, #0284c7);
    outline-offset: 2px;
}

.tf-dropdown-item:focus-visible {
    outline: 2px solid var(--tf-primary, #0284c7);
    outline-offset: -2px;
    background: var(--tf-gray-100, #f1f5f9);
}
'''

with open(path_css, "w", encoding="utf-8", newline="") as f:
    f.write(new_css)

print("Updated UserMenu.razor.css successfully.")
