const fs = require('fs');
const path = require('path');

const appCssPath = 'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web/wwwroot/app.css';
let appCss = fs.readFileSync(appCssPath, 'utf8');

// 1. Update line 1665 in app.css (allow flyout in expanded mode)
const target1 = `  /* When Expanded, Floating Flyout Panel is NEVER displayed */
  .tf-sidebar:not(.is-collapsed) .tf-floating-flyout,
  .tf-sidebar:not(.is-collapsed) ~ .tf-floating-flyout {
    display: none !important;
    opacity: 0 !important;
    pointer-events: none !important;
  }`;

const replace1 = `  /* When Expanded, Floating Flyout Panel positions next to the expanded sidebar (224px) */
  .tf-sidebar:not(.is-collapsed) .tf-floating-flyout {
    left: 224px;
  }`;

if (appCss.includes(target1)) {
    appCss = appCss.replace(target1, replace1);
    console.log('Successfully replaced target1 in app.css');
} else {
    console.log('target1 NOT found in app.css');
}

// 2. Update .tf-floating-flyout styles in app.css
const target2 = `.tf-floating-flyout {
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
}`;

const replace2 = `.tf-floating-flyout {
  display: none;
  position: fixed;
  left: 68px;
  width: 270px;
  background-color: var(--tf-bg-card, #ffffff);
  border: 1px solid var(--tf-border, #e2e8f0);
  border-radius: 14px;
  box-shadow: 0 16px 36px -4px rgba(16, 24, 40, 0.18), 0 4px 12px -2px rgba(16, 24, 40, 0.06);
  z-index: 1060;
  padding: 14px 12px 12px 12px;
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

/* Seamless invisible hover bridge between rail/sidebar and flyout (eliminates gap) */
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
  background-color: #1e2330;
  border-color: rgba(255, 255, 255, 0.08);
  box-shadow: 0 20px 44px -4px rgba(0, 0, 0, 0.55), 0 0 0 1px rgba(255, 255, 255, 0.06);
}`;

if (appCss.includes(target2)) {
    appCss = appCss.replace(target2, replace2);
    console.log('Successfully replaced target2 in app.css');
} else {
    console.log('target2 NOT found in app.css');
}

// 3. Update active link color in dark mode
const target3 = `[data-bs-theme="dark"] .tf-flyout-link.active {
  background-color: rgba(37, 99, 235, 0.22) !important;
  color: #60a5fa !important;
  border-color: rgba(96, 165, 250, 0.25);
}`;

const replace3 = `[data-bs-theme="dark"] .tf-flyout-link.active {
  background-color: rgba(37, 99, 235, 0.22) !important;
  color: #93c5fd !important;
  border-color: rgba(96, 165, 250, 0.25);
}`;

if (appCss.includes(target3)) {
    appCss = appCss.replace(target3, replace3);
    console.log('Successfully replaced target3 in app.css');
}

fs.writeFileSync(appCssPath, appCss, 'utf8');
console.log('app.css updated.');

// 4. Update App.razor
const appRazorPath = 'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web/Components/App.razor';
let appRazor = fs.readFileSync(appRazorPath, 'utf8');

const targetFlyoutBlock = `        window.tradeflowFlyout = {
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
            if (item && window.innerWidth >= 1024) {
                var group = item.getAttribute('data-flyout-group');
                if (window.tradeflowFlyout.activeGroup === group) {
                    window.tradeflowFlyout.close();
                } else {
                    window.tradeflowFlyout.open(group, item);
                }
            } else if (!e.target.closest('.tf-floating-flyout')) {
                window.tradeflowFlyout.close();
            }
        });`;

const replaceFlyoutBlock = `        window.tradeflowFlyout = {
            closeTimer: null,
            activeGroup: null,

            open: function (group, itemEl) {
                this.cancelClose();
                var sidebar = document.querySelector('.tf-sidebar');
                if (!sidebar || window.innerWidth < 1024) {
                    return;
                }

                var flyout = document.querySelector('.tf-floating-flyout');
                if (!flyout) return;

                // Highlight active hovered rail / sidebar item
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

                // Dynamic horizontal docking immediately adjacent to sidebar
                var sidebarRect = sidebar.getBoundingClientRect();
                var leftPos = Math.round(sidebarRect.right);
                flyout.style.left = leftPos + 'px';

                // Optimal vertical alignment matching user design:
                if (itemEl) {
                    var rect = itemEl.getBoundingClientRect();
                    var flyoutHeight = flyout.offsetHeight || 380;
                    var vh = window.innerHeight;
                    var top;

                    // If item is in lower half of screen, align bottom of flyout with bottom of item
                    if (rect.top > vh / 2 || rect.top + flyoutHeight > vh - 20) {
                        top = rect.bottom - flyoutHeight;
                        if (top < 12) top = 12;
                    } else {
                        top = rect.top;
                        if (top + flyoutHeight > vh - 12) {
                            top = Math.max(12, vh - flyoutHeight - 12);
                        }
                    }
                    flyout.style.top = Math.round(top) + 'px';
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
            if (!sidebar || window.innerWidth < 1024) {
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
            if (item && window.innerWidth >= 1024) {
                var group = item.getAttribute('data-flyout-group');
                if (window.tradeflowFlyout.activeGroup === group) {
                    window.tradeflowFlyout.close();
                } else {
                    window.tradeflowFlyout.open(group, item);
                }
            } else if (!e.target.closest('.tf-floating-flyout')) {
                window.tradeflowFlyout.close();
            }
        });`;

if (appRazor.includes(targetFlyoutBlock)) {
    appRazor = appRazor.replace(targetFlyoutBlock, replaceFlyoutBlock);
    console.log('Successfully replaced flyout block in App.razor');
} else {
    console.log('targetFlyoutBlock NOT found in App.razor');
}

fs.writeFileSync(appRazorPath, appRazor, 'utf8');
console.log('App.razor updated.');
