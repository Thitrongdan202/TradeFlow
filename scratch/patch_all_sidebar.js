const fs = require('fs');

// 1. PATCH App.razor
console.log('Patching App.razor...');
let appRazor = fs.readFileSync('src/TradeFlow.Web/Components/App.razor', 'utf8');

const flyoutScript = `
        window.tradeflowFlyout = {
            closeTimer: null,
            activeGroup: null,

            init: function () {
                var self = this;
                var sidebar = document.querySelector('.tf-sidebar');
                var flyout = document.querySelector('.tf-floating-flyout');
                if (!sidebar || !flyout) return;

                if (sidebar.__flyoutBound) return;
                sidebar.__flyoutBound = true;

                // Hover on rail items
                sidebar.addEventListener('mouseover', function (e) {
                    var item = e.target.closest('[data-flyout-group]');
                    if (item) {
                        var group = item.getAttribute('data-flyout-group');
                        self.open(group, item);
                    }
                });

                sidebar.addEventListener('mouseout', function (e) {
                    var item = e.target.closest('[data-flyout-group]');
                    if (item) {
                        var related = e.relatedTarget;
                        if (!related || (!related.closest('.tf-floating-flyout') && !related.closest('[data-flyout-group]'))) {
                            self.scheduleClose();
                        }
                    }
                });

                // Hover on flyout panel itself
                flyout.addEventListener('mouseenter', function () {
                    self.cancelClose();
                });

                flyout.addEventListener('mouseleave', function (e) {
                    var related = e.relatedTarget;
                    if (!related || !related.closest('.tf-sidebar')) {
                        self.scheduleClose();
                    }
                });

                // Close on outside click
                document.addEventListener('click', function (e) {
                    if (!e.target.closest('.tf-sidebar') && !e.target.closest('.tf-floating-flyout')) {
                        self.close();
                    }
                });

                // Close immediately when a flyout link is clicked
                flyout.addEventListener('click', function (e) {
                    if (e.target.closest('a')) {
                        self.close();
                    }
                });

                // Touch devices tap support
                sidebar.addEventListener('click', function (e) {
                    var item = e.target.closest('[data-flyout-group]');
                    if (item) {
                        var group = item.getAttribute('data-flyout-group');
                        if (self.activeGroup === group) {
                            // Already open
                        } else {
                            e.preventDefault();
                            self.open(group, item);
                        }
                    }
                });
            },

            open: function (group, itemEl) {
                this.cancelClose();
                var flyout = document.querySelector('.tf-floating-flyout');
                if (!flyout) return;

                // Highlight active hovered rail item
                document.querySelectorAll('.tf-nav-item.is-hovered').forEach(function (el) {
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

                flyout.classList.add('is-visible');
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
                document.querySelectorAll('.tf-nav-item.is-hovered').forEach(function (el) {
                    el.classList.remove('is-hovered');
                });
                this.activeGroup = null;
            }
        };
`;

// Insert window.tradeflowFlyout definition right before window.tradeflow = {
if (!appRazor.includes('window.tradeflowFlyout =')) {
    appRazor = appRazor.replace('window.tradeflow = {', flyoutScript + '\r\n        window.tradeflow = {');
}

// In initSidebar, call window.tradeflowFlyout.init()
if (!appRazor.includes('window.tradeflowFlyout.init()')) {
    appRazor = appRazor.replace("var sidebar = document.querySelector('.tf-sidebar');", "var sidebar = document.querySelector('.tf-sidebar');\r\n                if (window.tradeflowFlyout) window.tradeflowFlyout.init();");
}

// In keydown Escape, call window.tradeflowFlyout.close()
if (!appRazor.includes('window.tradeflowFlyout.close()')) {
    appRazor = appRazor.replace("if (e.key === 'Escape' || e.keyCode === 27) {", "if (e.key === 'Escape' || e.keyCode === 27) {\r\n                if (window.tradeflowFlyout) window.tradeflowFlyout.close();");
}

fs.writeFileSync('src/TradeFlow.Web/Components/App.razor', appRazor, 'utf8');
console.log('App.razor patched successfully.');

// 2. PATCH app.css
console.log('Patching app.css...');
let css = fs.readFileSync('src/TradeFlow.Web/wwwroot/app.css', 'utf8');

// Change line 169: --tf-sidebar-width to 68px
css = css.replace(/--tf-sidebar-width:\s*224px;/, '--tf-sidebar-width:         68px;');

// Find the block we previously added and update it
const targetStart = '/* ============================================================\r\n   TRADEFLOW CONTEXTUAL NAVIGATION PANEL & ICON RAIL ARCHITECTURE';
const targetStartUnix = '/* ============================================================\n   TRADEFLOW CONTEXTUAL NAVIGATION PANEL & ICON RAIL ARCHITECTURE';

const targetEnd = '/* ============================================================\r\n\r\n   SIDEBAR BOTTOM PINNED DOCK ("Bảng điều khiển")';
const targetEndUnix = '/* ============================================================\n\n   SIDEBAR BOTTOM PINNED DOCK ("Bảng điều khiển")';

let isCrlf = css.includes('\r\n');
let sIdx = css.indexOf(targetStart);
if (sIdx === -1) sIdx = css.indexOf(targetStartUnix);

let eIdx = css.indexOf(targetEnd);
if (eIdx === -1) eIdx = css.indexOf(targetEndUnix);

console.log('css sIdx:', sIdx, 'eIdx:', eIdx);
if (sIdx === -1 || eIdx === -1) {
  console.error('Target not found in app.css!');
  process.exit(1);
}

const newCssBlock = `/* ============================================================
   TRADEFLOW COMPACT ICON RAIL & FLOATING HOVER FLYOUT ARCHITECTURE
   - Compact vertical icon rail always active on desktop (68px)
   - Zero-gap seamless hover bridge between rail and flyout panel
   - 150ms grace timeout for rock-solid flicker-free navigation
   - Smooth in-place group switching without stacked panels
   ============================================================ */

.tf-sidebar {
  z-index: 1045 !important;
}

@media (min-width: 1024px) {
  .tf-sidebar {
    width: 68px !important;
    overflow: visible !important;
  }

  .tf-sidebar-inner {
    overflow: visible !important;
  }

  .tf-main-wrapper {
    margin-left: 68px !important;
  }

  .tf-brand-wrapper {
    height: var(--tf-topbar-height);
  }

  .tf-brand-wrapper .tf-brand {
    padding: 0;
    justify-content: center;
    gap: 0;
  }

  .tf-brand-logo {
    margin: 0 auto;
    width: 36px;
    height: 36px;
  }

  .tf-brand-text {
    display: none !important;
  }

  .tf-nav-label {
    display: none !important;
  }

  .tf-nav-chevron-wrap {
    display: none !important;
  }

  .tf-nav-scroll {
    overflow-y: auto !important;
    overflow-x: hidden !important;
  }

  .tf-nav-item {
    margin: 3px 8px;
    position: relative;
  }

  .tf-nav-link {
    padding: 0;
    justify-content: center;
    gap: 0;
    height: 40px;
    min-height: 40px;
    border-radius: 8px;
    width: 100%;
  }

  .tf-nav-icon {
    width: 20px;
    height: 20px;
    min-width: 20px;
    min-height: 20px;
    margin: 0 auto;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .tf-nav-bottom-item {
    justify-content: center !important;
    padding: 8px 0 !important;
  }

  .tf-nav-bottom-item .tf-nav-label {
    display: none !important;
  }

  .tf-sidebar-backdrop {
    display: none !important;
  }
}

/* --- Rail Item Active & Hover Indicator --- */
.tf-nav-item.is-hovered > .tf-nav-link,
.tf-nav-item.is-active > .tf-nav-link {
  background-color: var(--tf-sidebar-item-active, #eff6ff) !important;
  color: var(--tf-sidebar-text-active, #2563eb) !important;
}

[data-bs-theme="dark"] .tf-nav-item.is-hovered > .tf-nav-link,
[data-theme="dark"] .tf-nav-item.is-hovered > .tf-nav-link,
[data-bs-theme="dark"] .tf-nav-item.is-active > .tf-nav-link,
[data-theme="dark"] .tf-nav-item.is-active > .tf-nav-link {
  background-color: rgba(37, 99, 235, 0.2) !important;
  color: #60a5fa !important;
}

.tf-nav-item.is-hovered > .tf-nav-link::before,
.tf-nav-item.is-active > .tf-nav-link::before {
  transform: translateY(-50%) scaleY(1);
  opacity: 1;
}

/* ============================================================
   FLOATING FLYOUT PANEL STYLING
   ============================================================ */

.tf-floating-flyout {
  display: none;
  position: fixed;
  left: 76px;
  width: 275px;
  background-color: var(--tf-bg-card, #ffffff);
  border: 1px solid var(--tf-border, #e2e8f0);
  border-radius: 12px;
  box-shadow: 0 16px 36px -4px rgba(16, 24, 40, 0.18), 0 4px 12px -2px rgba(16, 24, 40, 0.06);
  z-index: 1060;
  padding: 14px 10px 12px 10px;
  pointer-events: auto;
  opacity: 0;
  transform: translateX(-4px);
  transition: opacity 0.14s cubic-bezier(0.16, 1, 0.3, 1), transform 0.14s cubic-bezier(0.16, 1, 0.3, 1);
  box-sizing: border-box;
}

.tf-floating-flyout.is-visible {
  display: block;
  opacity: 1;
  transform: translateX(0);
}

/* Seamless invisible hover bridge between rail and flyout (eliminates gap) */
.tf-floating-flyout::before {
  content: '';
  position: absolute;
  top: -12px;
  bottom: -12px;
  left: -20px;
  width: 25px;
  background: transparent;
  pointer-events: auto;
}

[data-bs-theme="dark"] .tf-floating-flyout,
[data-theme="dark"] .tf-floating-flyout {
  background-color: #222630;
  border-color: #383E4B;
  box-shadow: 0 20px 44px -4px rgba(0, 0, 0, 0.55), 0 0 0 1px rgba(255, 255, 255, 0.08);
}

.tf-flyout-header {
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--tf-text-muted, #94a3b8);
  padding: 0 8px 8px 8px;
  border-bottom: 1px solid var(--tf-border, #e2e8f0);
  margin-bottom: 6px;
  user-select: none;
}

[data-bs-theme="dark"] .tf-flyout-header,
[data-theme="dark"] .tf-flyout-header {
  color: #929BAC;
  border-bottom-color: rgba(255, 255, 255, 0.08);
}

.tf-flyout-body {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.tf-flyout-link {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 7px 10px;
  border-radius: 8px;
  color: var(--tf-text-secondary, #334155);
  font-size: 13px;
  font-weight: 500;
  text-decoration: none;
  transition: all 0.12s ease;
  cursor: pointer;
  border: 1px solid transparent;
  user-select: none;
}

[data-bs-theme="dark"] .tf-flyout-link {
  color: #B4BBC8;
}

.tf-flyout-link:hover {
  background-color: var(--tf-sidebar-item-hover, #F7F8FA);
  color: var(--tf-text-primary, #0f172a);
  text-decoration: none;
}

[data-bs-theme="dark"] .tf-flyout-link:hover {
  background-color: rgba(255, 255, 255, 0.06);
  color: #ffffff;
}

.tf-flyout-dot {
  width: 5px;
  height: 5px;
  border-radius: 50%;
  background-color: #94a3b8;
  flex-shrink: 0;
  transition: all 0.12s ease;
}

.tf-flyout-link:hover .tf-flyout-dot {
  background-color: var(--tf-text-primary, #0f172a);
}

[data-bs-theme="dark"] .tf-flyout-link:hover .tf-flyout-dot {
  background-color: #ffffff;
}

/* Active Page in Flyout — Matching the user's screenshot! */
.tf-flyout-link.active {
  background-color: var(--tf-sidebar-item-active, #eff6ff) !important;
  color: var(--tf-sidebar-text-active, #2563eb) !important;
  font-weight: 600 !important;
  border-color: rgba(37, 99, 235, 0.15);
}

.tf-flyout-link.active .tf-flyout-dot {
  background-color: #2563eb;
  transform: scale(1.3);
  box-shadow: 0 0 6px rgba(37, 99, 235, 0.4);
}

[data-bs-theme="dark"] .tf-flyout-link.active {
  background-color: rgba(37, 99, 235, 0.22) !important;
  color: #60a5fa !important;
  border-color: rgba(96, 165, 250, 0.25);
}

[data-bs-theme="dark"] .tf-flyout-link.active .tf-flyout-dot {
  background-color: #3b82f6;
  box-shadow: 0 0 8px rgba(59, 130, 246, 0.8);
}

/* Special color for 'Vai trò & Phân quyền' link as in screenshot */
.tf-flyout-link.tf-link-accent {
  color: var(--tf-primary, #2563eb);
}
[data-bs-theme="dark"] .tf-flyout-link.tf-link-accent {
  color: #60a5fa;
}

/* On mobile screen (< 1024px), sidebar operates as drawer */
@media (max-width: 1023px) {
  .tf-floating-flyout {
    display: none !important;
  }
}

`;

const formattedReplacement = isCrlf ? newCssBlock.replace(/\n/g, '\r\n') : newCssBlock;
css = css.substring(0, sIdx) + formattedReplacement + css.substring(eIdx);
fs.writeFileSync('src/TradeFlow.Web/wwwroot/app.css', css, 'utf8');
console.log('app.css patched successfully.');
