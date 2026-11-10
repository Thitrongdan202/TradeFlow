import os

path = r'src/TradeFlow.Web/Components/Layout/NavMenu.razor'
with open(path, 'r', encoding='utf-8') as f:
    c = f.read()

target_flyout = """                        @if (Has("Users", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/cai-dat/nguoi-dung" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Người dùng &amp; An ninh</span>
                            </NavLink>
                        }"""

repl_flyout = """                        @if (Has("Users", "View"))
                        {
                            <NavLink class="tf-flyout-link" href="/cai-dat/nguoi-dung" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Người dùng &amp; An ninh</span>
                            </NavLink>
                            <NavLink class="tf-flyout-link" href="/cai-dat/nguoi-dung/bao-mat" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-flyout-dot"></span>
                                <span>Bảo mật &amp; Quản trị ROOT</span>
                            </NavLink>
                        }"""

target_sub = """                    @if (Has("Users", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/cai-dat/nguoi-dung" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Người dùng &amp; An ninh
                            </NavLink>
                        </div>
                    }"""

repl_sub = """                    @if (Has("Users", "View"))
                    {
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/cai-dat/nguoi-dung" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Người dùng &amp; An ninh
                            </NavLink>
                        </div>
                        <div class="tf-nav-sub-item">
                            <NavLink class="tf-nav-sub-link" href="/cai-dat/nguoi-dung/bao-mat" ActiveClass="active" @onclick="HandleLinkClick">
                                <span class="tf-nav-sub-dot"></span>Bảo mật &amp; Quản trị ROOT
                            </NavLink>
                        </div>
                    }"""

c = c.replace(target_flyout, repl_flyout, 1).replace(target_sub, repl_sub, 1)

with open(path, 'w', encoding='utf-8') as f:
    f.write(c)

print('SUCCESS')
