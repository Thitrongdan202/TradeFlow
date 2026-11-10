path = r"C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Web\Components\Layout\MainLayout.razor"

with open(path, "r", encoding="utf-8") as f:
    text = f.read()

# 1. Update Header markup: Remove search and wrap left button in tf-topbar-left, add clock pill in tf-topbar-actions
old_header_start = '<header class="tf-topbar no-print">'
old_header_end = '</header>'

new_header = '''<header class="tf-topbar no-print">
            <div class="tf-topbar-left">
                @* Sidebar toggle (Desktop: collapses/expands, Mobile: opens/closes drawer) *@
                <button class="tf-topbar-btn tf-sidebar-toggle" @onclick="HandleToggleClick" title="@(_sidebarCollapsed ? "Mở rộng thanh menu (Ctrl+B)" : "Thu gọn thanh menu (Ctrl+B)")" aria-label="Menu" aria-expanded="@(!_sidebarCollapsed)">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <line x1="3" y1="6" x2="21" y2="6"/>
                        <line x1="3" y1="12" x2="21" y2="12"/>
                        <line x1="3" y1="18" x2="21" y2="18"/>
                    </svg>
                </button>
            </div>

            @* Right-side topbar actions *@
            <div class="tf-topbar-actions">
                @* Live System Time Pill *@
                <div class="tf-topbar-clock-pill" title="Thời gian hệ thống (GMT+7)">
                    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <circle cx="12" cy="12" r="10"></circle>
                        <polyline points="12 6 12 12 16 14"></polyline>
                    </svg>
                    <span>@_currentTimeFormatted</span>
                </div>

                @* Warehouse / Branch selector pill *@
                <a href="/kho-hang" class="tf-topbar-warehouse-pill" title="Chi nhánh &amp; Kho hàng hiện tại" style="text-decoration:none;">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>
                        <polyline points="9 22 9 12 15 12 15 22"/>
                    </svg>
                    <span>Tất cả các kho</span>
                    <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                        <polyline points="6 9 12 15 18 9"/>
                    </svg>
                </a>

                @* Quick Action: Audit History *@
                <a href="/cai-dat/nhat-ky-he-thong" class="tf-topbar-btn" title="Nhật ký hệ thống &amp; Kiểm toán" aria-label="Nhật ký hệ thống">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <circle cx="12" cy="12" r="10"/>
                        <polyline points="12 6 12 12 16 14"/>
                    </svg>
                </a>

                @* Quick Action: Interactive Notifications Menu *@
                <NotificationMenu />

                <div class="tf-topbar-divider"></div>

                @* Premium Animated Light/Dark Mode Switch *@
                <ThemeToggle />

                @* User menu *@
                <UserMenu />
            </div>
        </header>'''

# Replace header section
h_start = text.find('<header class="tf-topbar no-print">')
h_end = text.find('</header>', h_start) + len('</header>')
if h_start != -1 and h_end != -1:
    text = text[:h_start] + new_header + text[h_end:]
    print("Replaced header section.")

# 2. Add Clock Timer logic in @code
if "_currentTimeFormatted" not in text:
    old_code_decl = "private ErrorBoundary? _contentErrorBoundary;"
    new_code_decl = """private ErrorBoundary? _contentErrorBoundary;
    private string _currentTimeFormatted = DateTime.Now.ToString("HH:mm • dd/MM/yyyy");
    private System.Threading.Timer? _clockTimer;"""
    text = text.replace(old_code_decl, new_code_decl, 1)

    old_init = "Nav.LocationChanged += OnLocationChanged;"
    new_init = """Nav.LocationChanged += OnLocationChanged;
        _clockTimer = new System.Threading.Timer(_ =>
        {
            _currentTimeFormatted = DateTime.Now.ToString("HH:mm • dd/MM/yyyy");
            InvokeAsync(StateHasChanged);
        }, null, 10000, 30000);"""
    text = text.replace(old_init, new_init, 1)

    old_dispose = "Nav.LocationChanged -= OnLocationChanged;"
    new_dispose = """Nav.LocationChanged -= OnLocationChanged;
        _clockTimer?.Dispose();"""
    text = text.replace(old_dispose, new_dispose, 1)
    print("Added clock timer logic.")

with open(path, "w", encoding="utf-8", newline="") as f:
    f.write(text)

print("Updated MainLayout.razor successfully.")
