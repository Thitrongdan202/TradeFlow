import os

# 1. Update app.css
css_path = r"C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Web\wwwroot\app.css"
with open(css_path, "r", encoding="utf-8") as f:
    css_content = f.read()

# Replace rule that hid flyout when expanded
old_rule = """  /* When Expanded, Floating Flyout Panel is NEVER displayed */
  .tf-sidebar:not(.is-collapsed) .tf-floating-flyout,
  .tf-sidebar:not(.is-collapsed) ~ .tf-floating-flyout {
    display: none !important;
    opacity: 0 !important;
    pointer-events: none !important;
  }"""

new_rule = """  /* When Expanded, Floating Flyout Panel positions next to the expanded sidebar (224px) */
  .tf-sidebar:not(.is-collapsed) .tf-floating-flyout {
    left: 224px;
  }"""

# Normalize CRLF for replacement
css_normalized = css_content.replace("\r\n", "\n")
old_rule_norm = old_rule.replace("\r\n", "\n")
new_rule_norm = new_rule.replace("\r\n", "\n")

if old_rule_norm in css_normalized:
    css_normalized = css_normalized.replace(old_rule_norm, new_rule_norm)
    print("Replaced old_rule in app.css")
else:
    print("WARNING: old_rule not found in app.css")

# Update .tf-floating-flyout default styles
css_normalized = css_normalized.replace("left: 76px;", "left: 68px;")
css_normalized = css_normalized.replace("width: 275px;", "width: 270px;")
css_normalized = css_normalized.replace("border-radius: 12px;", "border-radius: 14px;")
css_normalized = css_normalized.replace("background-color: #222630;\n  border-color: #383E4B;", "background-color: #1e2330;\n  border-color: rgba(255, 255, 255, 0.08);")
css_normalized = css_normalized.replace("color: #60a5fa !important;\n  border-color: rgba(96, 165, 250, 0.25);", "color: #93c5fd !important;\n  border-color: rgba(96, 165, 250, 0.25);")

with open(css_path, "w", encoding="utf-8") as f:
    f.write(css_normalized.replace("\n", "\r\n"))
print("Saved app.css")

# 2. Update App.razor
razor_path = r"C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Web\Components\App.razor"
with open(razor_path, "r", encoding="utf-8") as f:
    razor_content = f.read()

razor_norm = razor_content.replace("\r\n", "\n")

start_marker = "window.tradeflowFlyout = {"
end_marker = "// Early and DOMContentLoaded initialization"

start_idx = razor_norm.find(start_marker)
end_idx = razor_norm.find(end_marker, start_idx)

if start_idx != -1 and end_idx != -1:
    new_flyout_code = """window.tradeflowFlyout = {
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
        });

        """
    razor_norm = razor_norm[:start_idx] + new_flyout_code + razor_norm[end_idx:]
    with open(razor_path, "w", encoding="utf-8") as f:
        f.write(razor_norm.replace("\n", "\r\n"))
    print("Saved App.razor")
else:
    print("ERROR: Markers not found in App.razor", start_idx, end_idx)
