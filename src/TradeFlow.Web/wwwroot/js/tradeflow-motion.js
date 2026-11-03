/**
 * TradeFlow Enterprise Motion Design System
 * 
 * Provides coordinated, performance-first UI transitions using GSAP:
 * - Modal & Drawer coordinated entrance/exit with backdrop blending
 * - Dropdown & Popover scale/fade transitions
 * - Interactive Card subtle elevation
 * - Page content smooth entrance
 * - Strict prefers-reduced-motion compliance and resource cleanup
 */

(function (root, factory) {
    if (typeof define === 'function' && define.amd) {
        define([], factory);
    } else if (typeof module === 'object' && module.exports) {
        module.exports = factory();
    } else {
        root.TradeFlowMotion = factory();
    }
}(typeof self !== 'undefined' ? self : this, function () {
    'use strict';

    const prefersReducedMotionQuery = typeof window !== 'undefined' && window.matchMedia
        ? window.matchMedia('(prefers-reduced-motion: reduce)')
        : { matches: false };

    function isReducedMotion() {
        return prefersReducedMotionQuery.matches;
    }

    function getGsap() {
        return typeof window !== 'undefined' ? window.gsap : null;
    }

    let scrollLockCount = 0;
    let previousBodyOverflow = '';

    function lockScroll() {
        if (typeof document === 'undefined') return;
        scrollLockCount++;
        if (scrollLockCount === 1) {
            previousBodyOverflow = document.body.style.overflow;
            document.body.style.overflow = 'hidden';
            document.body.classList.add('tf-modal-open');
            document.documentElement.classList.add('tf-modal-open');
        }
    }

    function unlockScroll(force) {
        if (typeof document === 'undefined') return;
        if (force) {
            scrollLockCount = 0;
        } else {
            scrollLockCount = Math.max(0, scrollLockCount - 1);
        }
        // Safeguard: Check if any active modal backdrop or dialog actually remains in DOM
        const hasActiveModal = !!document.querySelector('.tf-modal-overlay:not(.is-closing), .modal.show:not(.is-closing), [class*="-modal-backdrop"]:not(.is-closing), .tf-gallery-modal-backdrop:not(.is-closing), .voucher-overlay:not(.is-closing)');
        if (scrollLockCount === 0 || !hasActiveModal) {
            scrollLockCount = 0;
            document.body.style.overflow = previousBodyOverflow || '';
            document.body.classList.remove('tf-modal-open');
            document.documentElement.classList.remove('tf-modal-open');
        }
    }

    /**
     * Animate a modal dialog entrance
     * @param {HTMLElement|string} dialogEl - The modal dialog element
     * @param {HTMLElement|string} backdropEl - The backdrop overlay element
     * @param {Function} [onComplete] - Callback on animation completion
     * @returns {Promise<void>}
     */
    function animateModalEnter(dialogEl, backdropEl, onComplete) {
        return new Promise((resolve) => {
            lockScroll();
            const dialog = typeof dialogEl === 'string' ? document.querySelector(dialogEl) : dialogEl;
            const backdrop = typeof backdropEl === 'string' ? document.querySelector(backdropEl) : backdropEl;
            const gsap = getGsap();

            const finish = () => {
                if (dialog && gsap) {
                    gsap.set(dialog, { clearProps: 'transform' });
                }
                if (onComplete) onComplete();
                resolve();
            };

            if (isReducedMotion() || !gsap || !dialog) {
                if (dialog) { dialog.style.opacity = '1'; dialog.style.transform = 'none'; }
                if (backdrop) { backdrop.style.opacity = '1'; }
                finish();
                return;
            }

            const tl = gsap.timeline({
                onComplete: finish,
                defaults: { overwrite: 'auto' }
            });

            if (backdrop) {
                tl.fromTo(backdrop, { opacity: 0 }, { opacity: 1, duration: 0.16, ease: 'power2.out' }, 0);
            }

            tl.fromTo(dialog, { opacity: 0, scale: 0.975, y: 8 }, { opacity: 1, scale: 1, y: 0, duration: 0.19, ease: 'power3.out' }, 0.01);

            dialog._tfModalTimeline = tl;
        });
    }

    /**
     * Animate a modal dialog exit
     * @param {HTMLElement|string} dialogEl - The modal dialog element
     * @param {HTMLElement|string} backdropEl - The backdrop overlay element
     * @param {Function} [onComplete] - Callback when exit animation finishes
     * @returns {Promise<void>}
     */
    function animateModalExit(dialogEl, backdropEl, onComplete) {
        return new Promise((resolve) => {
            const dialog = typeof dialogEl === 'string' ? document.querySelector(dialogEl) : dialogEl;
            const backdrop = typeof backdropEl === 'string' ? document.querySelector(backdropEl) : backdropEl;
            const gsap = getGsap();

            const finish = () => {
                unlockScroll();
                if (onComplete) onComplete();
                resolve();
            };

            if (isReducedMotion() || !gsap || !dialog) {
                finish();
                return;
            }

            const tl = gsap.timeline({
                onComplete: finish,
                defaults: { overwrite: 'auto' }
            });

            tl.to(dialog, { opacity: 0, scale: 0.975, y: 6, duration: 0.15, ease: 'power2.in' }, 0);

            if (backdrop) {
                tl.to(backdrop, { opacity: 0, duration: 0.15, ease: 'power2.in' }, 0.01);
            }
        });
    }

    /**
     * Animate dropdown or popover reveal
     * @param {HTMLElement|string} menuEl 
     */
    function animateDropdown(menuEl) {
        const menu = typeof menuEl === 'string' ? document.querySelector(menuEl) : menuEl;
        const gsap = getGsap();
        if (isReducedMotion() || !gsap || !menu) return;

        gsap.killTweensOf(menu);
        gsap.fromTo(menu,
            { opacity: 0, y: -5, scaleY: 0.97 },
            { opacity: 1, y: 0, scaleY: 1, transformOrigin: 'top right', duration: 0.18, ease: 'power2.out' }
        );
    }

    /**
     * Animate page content container on view load/navigation
     * @param {HTMLElement|string} contentEl 
     */
    function animatePageContent(contentEl) {
        const content = typeof contentEl === 'string' ? document.querySelector(contentEl) : contentEl;
        const gsap = getGsap();
        if (isReducedMotion() || !gsap || !content) return;

        gsap.killTweensOf(content);
        gsap.fromTo(content,
            { opacity: 1, y: 4 },
            { opacity: 1, y: 0, duration: 0.22, ease: 'power2.out', clearProps: 'transform' }
        );
    }

    /**
     * Animate tab or section content transition (fade & slide)
     * @param {HTMLElement|string} contentEl 
     */
    function animateTabContent(contentEl) {
        const el = typeof contentEl === 'string' ? document.querySelector(contentEl) : contentEl;
        const gsap = getGsap();
        if (isReducedMotion() || !gsap || !el) return;

        gsap.killTweensOf(el);
        gsap.fromTo(el,
            { opacity: 0.2, y: 6 },
            { opacity: 1, y: 0, duration: 0.16, ease: 'power2.out', clearProps: 'transform' }
        );
    }

    /**
     * Animate animated form mode switching (e.g. Direct Create vs Request Approval)
     * @param {HTMLElement|string} contentEl 
     */
    function animateFormSwitch(contentEl) {
        const el = typeof contentEl === 'string' ? document.querySelector(contentEl) : contentEl;
        const gsap = getGsap();
        if (!el) return;
        if (isReducedMotion() || !gsap) {
            el.style.opacity = '1';
            return;
        }

        gsap.killTweensOf(el);
        gsap.fromTo(el,
            { opacity: 0.4, y: 8 },
            { opacity: 1, y: 0, duration: 0.2, ease: 'power2.out', clearProps: 'transform' }
        );
    }

    /**
     * Get scrollTop of a scroll container
     * @param {HTMLElement|string} selector 
     * @returns {number}
     */
    function getElementScrollTop(selector) {
        const el = typeof selector === 'string' ? document.querySelector(selector) : selector;
        return el ? el.scrollTop : 0;
    }

    /**
     * Set scrollTop of a scroll container safely
     * @param {HTMLElement|string} selector 
     * @param {number} top 
     */
    function setElementScrollTop(selector, top) {
        const el = typeof selector === 'string' ? document.querySelector(selector) : selector;
        if (el) {
            el.scrollTop = top;
        }
    }

    /**
     * Programmatic or event-driven hover animation for cards
     * @param {HTMLElement|string} cardEl 
     * @param {boolean} isHover 
     * @param {number} [liftPx=3] 
     */
    function animateCardHover(cardEl, isHover, liftPx = 3) {
        const el = typeof cardEl === 'string' ? document.querySelector(cardEl) : cardEl;
        const gsap = getGsap();
        if (isReducedMotion() || !gsap || !el) return;

        if (isHover) {
            gsap.to(el, { y: -liftPx, duration: 0.18, ease: 'power2.out', overwrite: 'auto' });
        } else {
            gsap.to(el, { y: 0, duration: 0.2, ease: 'power2.out', overwrite: 'auto' });
        }
    }

    /**
     * Register interactive hover response on card elements
     * @param {HTMLElement} cardEl 
     * @param {number} [liftPx=2] 
     */
    function registerInteractiveCard(cardEl, liftPx = 2) {
        if (!cardEl || cardEl._tfCardInteractive) return;
        cardEl._tfCardInteractive = true;

        const gsap = getGsap();
        if (!gsap || isReducedMotion()) return;

        const onEnter = () => {
            gsap.to(cardEl, { y: -liftPx, duration: 0.16, ease: 'power2.out', overwrite: 'auto' });
        };

        const onLeave = () => {
            gsap.to(cardEl, { y: 0, duration: 0.18, ease: 'power2.out', overwrite: 'auto' });
        };

        cardEl.addEventListener('pointerenter', onEnter, { passive: true });
        cardEl.addEventListener('pointerleave', onLeave, { passive: true });

        cardEl._tfCardCleanup = () => {
            cardEl.removeEventListener('pointerenter', onEnter);
            cardEl.removeEventListener('pointerleave', onLeave);
            gsap.killTweensOf(cardEl);
            gsap.set(cardEl, { clearProps: 'y' });
            cardEl._tfCardInteractive = false;
        };
    }

    /**
     * Animate Dashboard components entrance with staggered reveal
     * @param {HTMLElement|string} containerEl 
     */
    function animateDashboardEntrance(containerEl) {
        const el = typeof containerEl === 'string' ? document.querySelector(containerEl) : containerEl;
        const gsap = getGsap();
        if (isReducedMotion() || !gsap || !el) return null;

        return gsap.context(() => {
            // Stagger KPI Cards
            gsap.fromTo('.tf-kpi-card',
                { opacity: 0, y: 12 },
                { opacity: 1, y: 0, duration: 0.38, stagger: 0.05, ease: 'power2.out', clearProps: 'transform' }
            );

            // Stagger Main Sections
            gsap.fromTo('.tf-dash-section',
                { opacity: 0, y: 15 },
                { opacity: 1, y: 0, duration: 0.42, stagger: 0.08, delay: 0.12, ease: 'power2.out', clearProps: 'transform' }
            );

            // Animate SVG Chart Path if present
            const linePath = el.querySelector('.tf-chart-line-path');
            if (linePath && typeof linePath.getTotalLength === 'function') {
                try {
                    const len = linePath.getTotalLength();
                    gsap.fromTo(linePath,
                        { strokeDasharray: len, strokeDashoffset: len },
                        { strokeDashoffset: 0, duration: 0.75, delay: 0.2, ease: 'power2.out' }
                    );
                } catch (e) {
                    // Fallback
                }
            }
        }, el);
    }

    /**
     * Generalized enterprise module entrance with coordinated stagger
     * Mirrors the smooth Dashboard entrance experience across Contracts, Employees, etc.
     * @param {HTMLElement|string} containerEl 
     * @param {Object} [options]
     */
    function animateModuleEntrance(containerEl, options = {}) {
        const el = typeof containerEl === 'string' ? document.querySelector(containerEl) : containerEl;
        const gsap = getGsap();
        if (isReducedMotion() || !gsap || !el) return null;

        const kpiSelector = options.kpiSelector || '.tf-kpi-card';
        const sectionSelector = options.sectionSelector || '.tf-card, .tf-filter-card, .tf-employee-sidebar, .tf-employee-main, .tf-table-container, .tf-segmented-tabs, .tf-contracts-tab-content';
        const kpiDuration = options.kpiDuration || 0.36;
        const sectionDuration = options.sectionDuration || 0.40;

        return gsap.context(() => {
            const kpiCards = el.querySelectorAll(kpiSelector);
            if (kpiCards.length > 0) {
                gsap.fromTo(kpiCards,
                    { opacity: 0, y: 12 },
                    { opacity: 1, y: 0, duration: kpiDuration, stagger: 0.05, ease: 'power2.out', clearProps: 'transform' }
                );
            }

            const sections = el.querySelectorAll(sectionSelector);
            if (sections.length > 0) {
                gsap.fromTo(sections,
                    { opacity: 0, y: 14 },
                    { opacity: 1, y: 0, duration: sectionDuration, stagger: 0.07, delay: 0.08, ease: 'power2.out', clearProps: 'transform' }
                );
            }
        }, el);
    }

    /**
     * Cross-fade transition when toggling tabs or applying heavy view changes
     * @param {HTMLElement|string} contentEl 
     */
    function animateSectionTransition(contentEl) {
        const el = typeof contentEl === 'string' ? document.querySelector(contentEl) : contentEl;
        const gsap = getGsap();
        if (isReducedMotion() || !gsap || !el) {
            if (el) el.style.opacity = '1';
            return;
        }

        gsap.killTweensOf(el);
        gsap.fromTo(el,
            { opacity: 0.4, y: 6 },
            { opacity: 1, y: 0, duration: 0.2, ease: 'power2.out', clearProps: 'transform' }
        );
    }

    /**
     * Animate Toast notification entrance
     * @param {HTMLElement|string} toastEl 
     */
    function animateToastEntrance(toastEl) {
        const el = typeof toastEl === 'string' ? document.querySelector(toastEl) : toastEl;
        const gsap = getGsap();
        if (isReducedMotion() || !gsap || !el) return;

        gsap.fromTo(el,
            { opacity: 0, y: 20, scale: 0.95 },
            { opacity: 1, y: 0, scale: 1, duration: 0.28, ease: 'power3.out', clearProps: 'transform' }
        );
    }

    /**
     * Animate Toast notification exit
     * @param {HTMLElement|string} toastEl 
     * @param {Function} [onComplete]
     */
    function animateToastExit(toastEl, onComplete) {
        const el = typeof toastEl === 'string' ? document.querySelector(toastEl) : toastEl;
        const gsap = getGsap();
        if (isReducedMotion() || !gsap || !el) {
            if (onComplete) onComplete();
            return;
        }

        gsap.to(el, {
            opacity: 0,
            y: 12,
            scale: 0.96,
            duration: 0.2,
            ease: 'power2.in',
            onComplete: () => {
                if (onComplete) onComplete();
            }
        });
    }

    /**
     * Safely cleanup all GSAP tweens on an element
     * @param {HTMLElement} el 
     */
    function cleanupElement(el) {
        if (!el) return;
        const gsap = getGsap();
        if (el._tfCardCleanup) {
            el._tfCardCleanup();
        }
        if (gsap) {
            gsap.killTweensOf(el);
            gsap.set(el, { clearProps: 'all' });
        }
    }

    // Auto-initialize standard transitions on page load and Blazor navigation
    function initGlobalTransitions() {
        const mainContent = document.querySelector('.tf-content');
        if (mainContent && !mainContent._tfAnimated) {
            mainContent._tfAnimated = true;
            animatePageContent(mainContent);
        }

        // Initialize any interactive cards & stat widgets
        const interactiveCards = document.querySelectorAll('.tf-card-interactive, .tf-stat-card');
        interactiveCards.forEach(c => registerInteractiveCard(c, 2));
    }

    if (typeof window !== 'undefined') {
    // Auto-detect modal removal and unfreeze sidebar/body scroll immediately
    function setupModalDomObserver() {
        if (typeof MutationObserver === 'undefined' || typeof document === 'undefined') return;
        const observer = new MutationObserver(() => {
            if (document.body && (document.body.classList.contains('tf-modal-open') || scrollLockCount > 0)) {
                const hasActiveModal = !!document.querySelector('.tf-modal-overlay:not(.is-closing), .modal.show:not(.is-closing), [class*="-modal-backdrop"]:not(.is-closing), .tf-gallery-modal-backdrop:not(.is-closing), .voucher-overlay:not(.is-closing)');
                if (!hasActiveModal) {
                    unlockScroll(true);
                }
            }
        });
        if (document.body) {
            observer.observe(document.body, { childList: true, subtree: true });
        } else {
            document.addEventListener('DOMContentLoaded', () => {
                if (document.body) observer.observe(document.body, { childList: true, subtree: true });
            });
        }
    }
    setupModalDomObserver();

        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', initGlobalTransitions);
        } else {
            initGlobalTransitions();
        }

        // Global ESC handler for open modals & action menus
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' || e.key === 'Esc') {
                const activeBackdrop = document.querySelector('.tf-table-act-backdrop, .tf-act-dropdown-backdrop');
                if (activeBackdrop) {
                    activeBackdrop.click();
                    return;
                }
                const activeInspectClose = document.querySelector('.tf-inspect-modal-content .btn-close, .tf-inspect-modal-dialog .btn-close');
                if (activeInspectClose) {
                    activeInspectClose.click();
                    return;
                }
                const activeModalClose = document.querySelector('.tf-contract-modal-header .tf-modal-close-btn, .modal.show .tf-modal-close-btn, .modal.show .btn-close, .tf-preview-modal-dialog .tf-modal-close-btn, .tf-modal-backdrop .tf-modal-close-btn, .tf-modal-overlay .tf-modal-close-btn');
                if (activeModalClose) {
                    activeModalClose.click();
                }
            }
        });

        if (window.Blazor && window.Blazor.addEventListener) {
            window.Blazor.addEventListener('enhancedload', function () {
                const mainContent = document.querySelector('.tf-content');
                if (mainContent) {
                    animatePageContent(mainContent);
                }
            });
        }
    }


    /**
     * Animate table row selection highlight
     * @param {HTMLElement|string} rowEl 
     * @param {boolean} isSelected 
     */
    function animateTableRowSelect(rowEl, isSelected) {
        const el = typeof rowEl === 'string' ? document.querySelector(rowEl) : rowEl;
        const gsap = getGsap();
        if (isReducedMotion() || !gsap || !el) return;

        if (isSelected) {
            gsap.fromTo(el,
                { backgroundColor: 'rgba(37, 99, 235, 0.16)' },
                { backgroundColor: 'var(--tf-table-row-selected, rgba(37, 99, 235, 0.08))', duration: 0.18, ease: 'power2.out', clearProps: 'backgroundColor' }
            );
        }
    }

    /**
     * Animate bulk action bar entrance or exit
     * @param {HTMLElement|string} barEl 
     * @param {boolean} show 
     * @param {Function} [onComplete]
     */
    function animateBulkBar(barEl, show, onComplete) {
        const el = typeof barEl === 'string' ? document.querySelector(barEl) : barEl;
        const gsap = getGsap();
        if (isReducedMotion() || !gsap || !el) {
            if (onComplete) onComplete();
            return;
        }

        if (show) {
            gsap.fromTo(el,
                { opacity: 0, y: -8 },
                { opacity: 1, y: 0, duration: 0.16, ease: 'power2.out', clearProps: 'transform', onComplete }
            );
        } else {
            gsap.to(el, {
                opacity: 0,
                y: -6,
                duration: 0.14,
                ease: 'power2.in',
                onComplete
            });
        }
    }

    /**
     * Subtle numeric KPI ticker
     * @param {HTMLElement|string} element 
     * @param {number} start 
     * @param {number} end 
     * @param {number} [duration=0.5] 
     * @param {Function} [formatFn]
     */
    function animateCounter(element, start, end, duration = 0.5, formatFn = null) {
        const el = typeof element === 'string' ? document.querySelector(element) : element;
        const gsap = getGsap();
        if (isReducedMotion() || !gsap || !el) {
            if (el) el.textContent = formatFn ? formatFn(end) : end;
            return;
        }

        const obj = { val: start };
        gsap.to(obj, {
            val: end,
            duration: duration,
            ease: 'power1.out',
            onUpdate: function () {
                const currentVal = Math.round(obj.val);
                el.textContent = formatFn ? formatFn(currentVal) : currentVal;
            }
        });
    }

    /**
     * Restore focus to an element by selector or reference
     * @param {HTMLElement|string} selectorOrEl 
     */
    function restoreFocus(selectorOrEl) {
        if (typeof document === 'undefined') return;
        try {
            const el = typeof selectorOrEl === 'string' ? document.querySelector(selectorOrEl) : selectorOrEl;
            if (el && typeof el.focus === 'function') {
                el.focus();
            }
        } catch (e) { }
    }

    const api = {
        isReducedMotion,
        lockScroll,
        unlockScroll,
        restoreFocus,
        animateModalEnter,
        animateModalExit,
        animateDropdown,
        animatePageContent,
        animateTabContent,
        animateFormSwitch,
        getElementScrollTop,
        setElementScrollTop,
        animateCardHover,
        registerInteractiveCard,
        animateDashboardEntrance,
        animateModuleEntrance,
        animateSectionTransition,
        animateToastEntrance,
        animateToastExit,
        animateTableRowSelect,
        animateBulkBar,
        animateCounter,
        cleanupElement,
        initGlobalTransitions
    };

    if (typeof window !== 'undefined') {
        window.TradeFlowMotion = api;
        window.tradeflowMotion = api;
    }

    return api;
}));
