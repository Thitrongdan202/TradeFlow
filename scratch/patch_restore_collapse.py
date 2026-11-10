import re

print("=== STEP 1: PATCHING app.css ===")
with open('src/TradeFlow.Web/wwwroot/app.css', 'r', encoding='utf-8') as f:
    css = f.read()

# 1. Restore --tf-sidebar-width to 224px
css = re.sub(r'--tf-sidebar-width:\s*68px;', '--tf-sidebar-width:         224px;', css)

# 2. Update media query block in app.css
old_block_pattern = r'@media \(min-width: 1024px\) \{\s*\.tf-sidebar \{\s*width: 68px !important;.*?\n\}'

new_block = """@media (min-width: 1024px) {
  /* ============================================================
     EXPANDED SIDEBAR MODE (Desktop Default / 224px)
     ============================================================ */
  .tf-sidebar {
    width: var(--tf-sidebar-width, 224px);
    overflow: visible;
  }

  .tf-main-wrapper {
    margin-left: var(--tf-sidebar-width, 224px);
  }

  .tf-sidebar:not(.is-collapsed) .tf-brand-wrapper {
    height: var(--tf-topbar-height);
  }

  .tf-sidebar:not(.is-collapsed) .tf-brand-wrapper .tf-brand {
    padding: 0 16px;
    display: flex;
    align-items: center;
    gap: 12px;
    justify-content: flex-start;
  }

  .tf-sidebar:not(.is-collapsed) .tf-brand-logo {
    width: 36px;
    height: 36px;
    margin: 0;
  }

  .tf-sidebar:not(.is-collapsed) .tf-brand-text {
    display: flex !important;
    flex-direction: column;
  }

  .tf-sidebar:not(.is-collapsed) .tf-nav-item {
    margin: 2px 8px;
    position: relative;
  }

  .tf-sidebar:not(.is-collapsed) .tf-nav-link {
    display: flex;
    align-items: center;
    gap: 10px;
    height: 40px;
    padding: 0 12px;
    border-radius: 8px;
    justify-content: flex-start;
    width: 100%;
  }

  .tf-sidebar:not(.is-collapsed) .tf-nav-icon {
    width: 20px;
    height: 20px;
    min-width: 20px;
    min-height: 20px;
    margin: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  .tf-sidebar:not(.is-collapsed) .tf-nav-label {
    display: inline-block !important;
    font-size: 13.5px;
    font-weight: 500;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .tf-sidebar:not(.is-collapsed) .tf-nav-bottom-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 8px 12px !important;
    justify-content: flex-start !important;
  }

  .tf-sidebar:not(.is-collapsed) .tf-nav-bottom-item .tf-nav-label {
    display: inline-block !important;
    font-size: 13px;
    font-weight: 500;
  }

  /* When Expanded, Floating Flyout Panel is NEVER displayed */
  .tf-sidebar:not(.is-collapsed) .tf-floating-flyout,
  .tf-sidebar:not(.is-collapsed) ~ .tf-floating-flyout {
    display: none !important;
    opacity: 0 !important;
    pointer-events: none !important;
  }

  /* ============================================================
     COLLAPSED ICON RAIL MODE (Desktop / 68px)
     ============================================================ */
  .tf-sidebar.is-collapsed {
    width: 68px !important;
    overflow: visible !important;
  }

  .tf-sidebar.is-collapsed .tf-sidebar-inner {
    overflow: visible !important;
  }

  .tf-main-wrapper.is-collapsed {
    margin-left: 68px !important;
  }

  .tf-sidebar.is-collapsed .tf-brand-wrapper {
    height: var(--tf-topbar-height);
  }

  .tf-sidebar.is-collapsed .tf-brand-wrapper .tf-brand {
    padding: 0;
    justify-content: center;
    gap: 0;
  }

  .tf-sidebar.is-collapsed .tf-brand-logo {
    margin: 0 auto;
    width: 36px;
    height: 36px;
  }

  .tf-sidebar.is-collapsed .tf-brand-text {
    display: none !important;
  }

  .tf-sidebar.is-collapsed .tf-nav-label {
    display: none !important;
  }

  .tf-sidebar.is-collapsed .tf-nav-chevron-wrap {
    display: none !important;
  }

  .tf-sidebar.is-collapsed .tf-nav-scroll {
    overflow-y: auto !important;
    overflow-x: hidden !important;
  }

  .tf-sidebar.is-collapsed .tf-nav-item {
    margin: 3px 8px;
    position: relative;
  }

  .tf-sidebar.is-collapsed .tf-nav-link {
    padding: 0;
    justify-content: center;
    gap: 0;
    height: 40px;
    min-height: 40px;
    border-radius: 8px;
    width: 100%;
  }

  .tf-sidebar.is-collapsed .tf-nav-icon {
    width: 20px;
    height: 20px;
    min-width: 20px;
    min-height: 20px;
    margin: 0 auto;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .tf-sidebar.is-collapsed .tf-nav-bottom-item {
    justify-content: center !important;
    padding: 8px 0 !important;
  }

  .tf-sidebar.is-collapsed .tf-nav-bottom-item .tf-nav-label {
    display: none !important;
  }

  .tf-sidebar-backdrop {
    display: none !important;
  }
}"""

css, c = re.subn(old_block_pattern, new_block, css, flags=re.DOTALL)
print(f"app.css media query replacement count: {c}")

# Ensure topbar and nav-bottom z-index
if '.tf-nav-bottom {' in css:
    css = css.replace('.tf-nav-bottom {', '.tf-nav-bottom {\n  z-index: 1070 !important;\n  position: relative;')

with open('src/TradeFlow.Web/wwwroot/app.css', 'w', encoding='utf-8') as f:
    f.write(css)
print("app.css written successfully.")

print("\n=== STEP 2: PATCHING App.razor ===")
with open('src/TradeFlow.Web/Components/App.razor', 'r', encoding='utf-8') as f:
    app_code = f.read()

# Replace tradeflowFlyout and tradeflow definition cleanly
flyout_pattern = r'window\.tradeflowFlyout = \{.*?\n\s*window\.tradeflow = \{'

flyout_replacement = """window.tradeflowFlyout = {
            closeTimer: null,
            activeGroup: null,

            open: function (group, itemEl) {
                this.cancelClose();
                var sidebar = document.querySelector('.tf-sidebar');
                // ONLY open flyout when sidebar is collapsed!
                if (!sidebar || !sidebar.classList.contains('is-collapsed')) {
                    return;
                }

                var flyout = document.querySelector('.tf-floating-flyout');
                if (!flyout) return;

                // Highlight active hovered rail item
                document.querySelectorAll('.tf-sidebar .tf-nav-item.is-hovered').forEach(function (el) {
                    el.classList.remove('is-hovered');
                });
                if (itemEl) {
                    itemEl.classList.add('is-hovered');
                }

                // Show corresponding section
                var sections = flyout.querySelectorAll('.tf-flyout-section');
                var found = false;
                sections.forEach(function (sec) {
                    if (sec.getAttribute('data-section') === group) {
                        sec.style.display = 'block';
                        found = true;
                    } else {
                        sec.style.display = 'none';
                    }
                });

                if (!found) {
                    this.close();
                    return;
                }

                flyout.classList.add('is-visible');

                // Position flyout vertically relative to itemEl
                if (itemEl) {
                    var rect = itemEl.getBoundingClientRect();
                    var flyoutHeight = flyout.offsetHeight || 340;
                    var vh = window.innerHeight;
                    var top = rect.top;
                    if (top + flyoutHeight > vh - 12) {
                        top = Math.max(12, vh - flyoutHeight - 12);
                    }
                    flyout.style.top = top + 'px';
                }

                this.activeGroup = group;
            },

            scheduleClose: function () {
                this.cancelClose();
                var self = this;
                this.closeTimer = setTimeout(function () {
                    self.close();
                }, 150);
            },

            cancelClose: function () {
                if (this.closeTimer) {
                    clearTimeout(this.closeTimer);
                    this.closeTimer = null;
                }
            },

            close: function () {
                this.cancelClose();
                var flyout = document.querySelector('.tf-floating-flyout');
                if (flyout) {
                    flyout.classList.remove('is-visible');
                }
                document.querySelectorAll('.tf-sidebar .tf-nav-item.is-hovered').forEach(function (el) {
                    el.classList.remove('is-hovered');
                });
                this.activeGroup = null;
            }
        };

        // Document-level event delegation for hover flyout
        document.addEventListener('mouseover', function (e) {
            var sidebar = document.querySelector('.tf-sidebar');
            // If sidebar is expanded, do NOT trigger flyout!
            if (!sidebar || !sidebar.classList.contains('is-collapsed')) {
                return;
            }

            var item = e.target.closest('.tf-sidebar [data-flyout-group]');
            if (item) {
                var group = item.getAttribute('data-flyout-group');
                window.tradeflowFlyout.open(group, item);
                return;
            }
            if (e.target.closest('.tf-floating-flyout')) {
                window.tradeflowFlyout.cancelClose();
                return;
            }
        });

        document.addEventListener('mouseout', function (e) {
            var fromItem = e.target.closest('.tf-sidebar [data-flyout-group]');
            var fromFlyout = e.target.closest('.tf-floating-flyout');
            if (fromItem || fromFlyout) {
                var to = e.relatedTarget;
                if (to && (to.closest('.tf-floating-flyout') || to.closest('.tf-sidebar [data-flyout-group]'))) {
                    return;
                }
                window.tradeflowFlyout.scheduleClose();
            }
        });

        document.addEventListener('click', function (e) {
            // Close flyout immediately if clicking any link inside flyout
            if (e.target.closest('.tf-floating-flyout a')) {
                window.tradeflowFlyout.close();
                return;
            }

            // Close flyout immediately if clicking toggle buttons
            if (e.target.closest('.tf-sidebar-toggle, .tf-nav-collapse-btn')) {
                window.tradeflowFlyout.close();
                return;
            }

            // Touch tap on mobile
            var item = e.target.closest('.tf-sidebar [data-flyout-group]');
            if (item && window.innerWidth < 1024) {
                var group = item.getAttribute('data-flyout-group');
                if (window.tradeflowFlyout.activeGroup === group) {
                    window.tradeflowFlyout.close();
                } else {
                    e.preventDefault();
                    window.tradeflowFlyout.open(group, item);
                }
                return;
            }

            // Click outside closes flyout
            if (!e.target.closest('.tf-sidebar') && !e.target.closest('.tf-floating-flyout')) {
                window.tradeflowFlyout.close();
            }
        });

        window.tradeflow = {"""

app_code, count1 = re.subn(flyout_pattern, flyout_replacement, app_code, flags=re.DOTALL)
print(f"App.razor flyout replacement count: {count1}")

# Update setSidebarCollapsed and toggleSidebar in App.razor
sidebar_funcs_pattern = r'setSidebarCollapsed:\s*function\s*\(val\)\s*\{.*?\n\s*toggleSidebar:\s*function\s*\(\)\s*\{.*?\n\s*initSidebar:\s*function'

sidebar_funcs_replacement = """setSidebarCollapsed: function (val) {
                try {
                    localStorage.setItem('tf_sidebar_collapsed', val ? 'true' : 'false');
                } catch (e) { }

                if (window.tradeflowFlyout) window.tradeflowFlyout.close();

                var sidebar = document.querySelector('.tf-sidebar');
                var wrapper = document.querySelector('.tf-main-wrapper');
                var app = document.querySelector('.tf-app');

                if (sidebar) {
                    if (val) {
                        sidebar.classList.add('is-collapsed');
                    } else {
                        sidebar.classList.remove('is-collapsed');
                    }
                    sidebar.classList.add('is-animating-horizontal');
                    setTimeout(function () {
                        sidebar.classList.remove('is-animating-horizontal');
                    }, 260);
                }

                if (wrapper) {
                    if (val) {
                        wrapper.classList.add('is-collapsed');
                    } else {
                        wrapper.classList.remove('is-collapsed');
                    }
                }

                if (app) {
                    if (val) {
                        app.classList.add('is-sidebar-collapsed');
                    } else {
                        app.classList.remove('is-sidebar-collapsed');
                    }
                }

                var titleText = val ? 'Mở rộng thanh menu (Ctrl+B)' : 'Thu gọn thanh menu (Ctrl+B)';
                var hamburgerBtn = document.querySelector('.tf-sidebar-toggle');
                if (hamburgerBtn) {
                    hamburgerBtn.setAttribute('aria-expanded', val ? 'false' : 'true');
                    hamburgerBtn.setAttribute('title', titleText);
                }
                var bottomBtn = document.querySelector('.tf-nav-collapse-btn');
                if (bottomBtn) {
                    bottomBtn.setAttribute('aria-expanded', val ? 'false' : 'true');
                    bottomBtn.setAttribute('title', titleText);
                }
            },

            toggleSidebar: function () {
                var isMob = window.innerWidth < 1024;
                if (isMob) {
                    var sidebar = document.querySelector('.tf-sidebar');
                    var backdrop = document.querySelector('.tf-sidebar-backdrop');
                    if (sidebar) {
                        var isOpen = sidebar.classList.toggle('open');
                        if (backdrop) {
                            if (isOpen) backdrop.classList.add('active');
                            else backdrop.classList.remove('active');
                        }
                    }
                } else {
                    var sidebar = document.querySelector('.tf-sidebar');
                    if (!sidebar) return;
                    var isCurrentlyCollapsed = sidebar.classList.contains('is-collapsed');
                    this.setSidebarCollapsed(!isCurrentlyCollapsed);
                }
            },

            initSidebar: function"""

app_code, count2 = re.subn(sidebar_funcs_pattern, sidebar_funcs_replacement, app_code, flags=re.DOTALL)
print(f"App.razor sidebar funcs replacement count: {count2}")

with open('src/TradeFlow.Web/Components/App.razor', 'w', encoding='utf-8') as f:
    f.write(app_code)
print("App.razor written successfully.")
