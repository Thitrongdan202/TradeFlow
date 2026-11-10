import re

with open('src/TradeFlow.Web/Components/App.razor', 'r', encoding='utf-8') as f:
    content = f.read()

pattern = r'window\.tradeflowFlyout = \{.*?\n\s*window\.tradeflow = \{'

replacement = """window.tradeflowFlyout = {
            closeTimer: null,
            activeGroup: null,

            open: function (group, itemEl) {
                this.cancelClose();
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

        // Document-level event delegation for seamless hover flyout
        document.addEventListener('mouseover', function (e) {
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
            // Close when clicking any link inside flyout
            if (e.target.closest('.tf-floating-flyout a')) {
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
            // Click outside
            if (!e.target.closest('.tf-sidebar') && !e.target.closest('.tf-floating-flyout')) {
                window.tradeflowFlyout.close();
            }
        });

        window.tradeflow = {"""

new_content, count = re.subn(pattern, replacement, content, flags=re.DOTALL)
print(f"Substitutions made: {count}")
if count == 1:
    with open('src/TradeFlow.Web/Components/App.razor', 'w', encoding='utf-8') as f:
        f.write(new_content)
    print("Successfully updated App.razor")
else:
    print("Error: Could not find exact pattern in App.razor")
