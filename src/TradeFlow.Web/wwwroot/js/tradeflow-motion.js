/**
 * TradeFlow Enterprise Global Animation System
 * 
 * Coordinated, high-performance UI motion combining:
 * 1. MOTION ENGINE (Owner: Component enter/exit, modals, dropdowns, popovers, tabs, toasts, button feedback, hover)
 * 2. GSAP ENGINE   (Owner: Multi-stage sequence timelines, dashboard chart intro, SVG animations, counters)
 * 3. CSS LAYER     (Owner: Micro-transitions, focus states, theme colors, reduced-motion fallbacks)
 * 
 * Key Architectural Invariants:
 * - Single Owner per DOM element property at any time
 * - Zero collision between Motion and GSAP (clean takeover & property cleanup)
 * - Strict prefers-reduced-motion compliance (instantaneous resting state, 0s duration)
 * - Memory leak prevention (timeline cleanup, context reversion, observer teardown)
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

    // =========================================================================
    // 1. ANIMATION DESIGN SYSTEM TOKENS & PRESETS
    // =========================================================================

    const TOKENS = {
        duration: {
            hover: 0.15,            // 150ms
            button: 0.12,           // 120ms
            dropdown: 0.18,         // 180ms
            modalEnter: 0.24,       // 240ms
            modalExit: 0.16,        // 160ms
            tab: 0.18,              // 180ms
            toastEnter: 0.22,       // 220ms
            toastExit: 0.18,        // 180ms
            pageContent: 0.22,      // 220ms
            dashboardSection: 0.42, // 420ms
            dashboardKpi: 0.36,     // 360ms
            chartDraw: 0.75         // 750ms
        },
        spring: {
            snappy: { type: 'spring', stiffness: 420, damping: 32, mass: 0.8 },
            modal: { type: 'spring', stiffness: 360, damping: 28, mass: 0.9 },
            gentle: { type: 'spring', stiffness: 280, damping: 24, mass: 1 },
            button: { type: 'spring', stiffness: 500, damping: 30 }
        },
        easing: {
            easeOutCubic: [0.215, 0.61, 0.355, 1],
            easeInQuad: [0.55, 0.085, 0.68, 0.53],
            easeOutQuad: [0.25, 0.46, 0.45, 0.94],
            easeInOut: [0.42, 0, 0.58, 1]
        }
    };

    // =========================================================================
    // 2. RUNTIME RESOLUTION & ACCESSIBILITY (A11Y)
    // =========================================================================

    const prefersReducedMotionQuery = typeof window !== 'undefined' && window.matchMedia
        ? window.matchMedia('(prefers-reduced-motion: reduce)')
        : { matches: false };

    function isReducedMotion() {
        return prefersReducedMotionQuery.matches;
    }

    function getMotion() {
        return typeof window !== 'undefined' ? (window.Motion || null) : null;
    }

    function getGsap() {
        return typeof window !== 'undefined' ? (window.gsap || null) : null;
    }

    // =========================================================================
    // 3. ANTI-COLLISION & DOM OWNERSHIP REGISTRY
    // =========================================================================

    /**
     * Claim exclusive animation ownership on an element.
     * Prevents Motion and GSAP from simultaneously mutating the same DOM element.
     * @param {HTMLElement} el
     * @param {'motion'|'gsap'} newOwner
     */
    function claimOwnership(el, newOwner) {
        if (!el || typeof el.getAttribute !== 'function') return;
        const currentOwner = el._tfOwner;
        if (currentOwner === newOwner) return;

        // If switching from GSAP to Motion, kill GSAP tweens and reset inline styles
        if (currentOwner === 'gsap') {
            const gsap = getGsap();
            if (gsap) {
                gsap.killTweensOf(el);
            }
        }

        // If switching from Motion to GSAP, stop any active Motion animation
        if (currentOwner === 'motion' && el._tfMotionAnim) {
            try {
                el._tfMotionAnim.stop();
            } catch (e) { }
            el._tfMotionAnim = null;
        }

        el._tfOwner = newOwner;
        el.setAttribute('data-tf-owner', newOwner);
    }

    /**
     * Release ownership and cleanup active animations on an element
     * @param {HTMLElement} el
     */
    function releaseOwnership(el) {
        if (!el) return;
        if (el._tfMotionAnim) {
            try {
                el._tfMotionAnim.stop();
            } catch (e) { }
            el._tfMotionAnim = null;
        }
        const gsap = getGsap();
        if (gsap && el._tfOwner === 'gsap') {
            gsap.killTweensOf(el);
        }
        el._tfOwner = null;
        if (typeof el.removeAttribute === 'function') {
            el.removeAttribute('data-tf-owner');
        }
    }

    // =========================================================================
    // 4. SCROLL LOCK & VIEWPORT SAFEGUARD
    // =========================================================================

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

        const hasActiveModal = !!document.querySelector(
            '.tf-modal-overlay:not(.is-closing), .modal.show:not(.is-closing), [class*="-modal-backdrop"]:not(.is-closing), .tf-gallery-modal-backdrop:not(.is-closing), .voucher-overlay:not(.is-closing)'
        );

        if (scrollLockCount === 0 || !hasActiveModal) {
            scrollLockCount = 0;
            document.body.style.overflow = previousBodyOverflow || '';
            document.body.classList.remove('tf-modal-open');
            document.documentElement.classList.remove('tf-modal-open');
        }
    }

    function restoreFocus(selectorOrEl) {
        if (typeof document === 'undefined') return;
        try {
            const el = typeof selectorOrEl === 'string' ? document.querySelector(selectorOrEl) : selectorOrEl;
            if (el && typeof el.focus === 'function') {
                el.focus();
            }
        } catch (e) { }
    }

    // =========================================================================
    // 5. MOTION ENGINE DOMAIN: MODALS, DROPDOWNS, TABS, TOASTS, FEEDBACK
    // =========================================================================

    /**
     * Animate a modal dialog entrance with Motion (or GSAP fallback).
     * Disables conflicting CSS keyframes during transition for zero jitter.
     * @param {HTMLElement|string} dialogEl
     * @param {HTMLElement|string} backdropEl
     * @param {Function} [onComplete]
     * @returns {Promise<void>}
     */
    function animateModalEnter(dialogEl, backdropEl, onComplete) {
        return new Promise((resolve) => {
            lockScroll();
            const dialog = typeof dialogEl === 'string' ? document.querySelector(dialogEl) : dialogEl;
            const backdrop = typeof backdropEl === 'string' ? document.querySelector(backdropEl) : backdropEl;
            const Motion = getMotion();
            const gsap = getGsap();

            const finish = () => {
                if (dialog) {
                    dialog.style.transform = '';
                    dialog.style.opacity = '1';
                }
                if (backdrop) {
                    backdrop.style.opacity = '1';
                }
                if (onComplete) onComplete();
                resolve();
            };

            if (isReducedMotion() || (!Motion && !gsap) || !dialog) {
                if (dialog) {
                    dialog.style.opacity = '1';
                    dialog.style.transform = 'none';
                    dialog.style.animation = 'none';
                }
                if (backdrop) {
                    backdrop.style.opacity = '1';
                    backdrop.style.animation = 'none';
                }
                finish();
                return;
            }

            // Suppress CSS keyframe to avoid conflict
            dialog.style.animation = 'none';
            if (backdrop) backdrop.style.animation = 'none';

            if (Motion && typeof Motion.animate === 'function') {
                claimOwnership(dialog, 'motion');
                if (backdrop) claimOwnership(backdrop, 'motion');

                if (backdrop) {
                    Motion.animate(backdrop, { opacity: [0, 1] }, {
                        duration: TOKENS.duration.modalEnter * 0.85,
                        easing: TOKENS.easing.easeOutCubic
                    });
                }

                const anim = Motion.animate(
                    dialog,
                    {
                        opacity: [0, 1],
                        scale: [0.975, 1],
                        y: [8, 0]
                    },
                    {
                        duration: TOKENS.duration.modalEnter,
                        easing: TOKENS.easing.easeOutCubic
                    }
                );

                dialog._tfMotionAnim = anim;
                anim.then(finish).catch(finish);
            } else if (gsap) {
                // High-fidelity GSAP fallback
                claimOwnership(dialog, 'gsap');
                if (backdrop) claimOwnership(backdrop, 'gsap');

                const tl = gsap.timeline({ onComplete: finish, defaults: { overwrite: 'auto' } });
                if (backdrop) {
                    tl.fromTo(backdrop, { opacity: 0 }, { opacity: 1, duration: 0.16, ease: 'power2.out' }, 0);
                }
                tl.fromTo(dialog,
                    { opacity: 0, scale: 0.975, y: 8 },
                    { opacity: 1, scale: 1, y: 0, duration: 0.20, ease: 'power3.out' },
                    0.01
                );
            }
        });
    }

    /**
     * Animate a modal dialog exit with Motion (or GSAP fallback).
     * @param {HTMLElement|string} dialogEl
     * @param {HTMLElement|string} backdropEl
     * @param {Function} [onComplete]
     * @returns {Promise<void>}
     */
    function animateModalExit(dialogEl, backdropEl, onComplete) {
        return new Promise((resolve) => {
            const dialog = typeof dialogEl === 'string' ? document.querySelector(dialogEl) : dialogEl;
            const backdrop = typeof backdropEl === 'string' ? document.querySelector(backdropEl) : backdropEl;
            const Motion = getMotion();
            const gsap = getGsap();

            const finish = () => {
                unlockScroll();
                if (dialog) releaseOwnership(dialog);
                if (backdrop) releaseOwnership(backdrop);
                if (onComplete) onComplete();
                resolve();
            };

            if (isReducedMotion() || (!Motion && !gsap) || !dialog) {
                finish();
                return;
            }

            dialog.style.animation = 'none';
            if (backdrop) backdrop.style.animation = 'none';

            if (Motion && typeof Motion.animate === 'function') {
                claimOwnership(dialog, 'motion');
                if (backdrop) claimOwnership(backdrop, 'motion');

                if (backdrop) {
                    Motion.animate(backdrop, { opacity: [1, 0] }, {
                        duration: TOKENS.duration.modalExit,
                        easing: TOKENS.easing.easeInQuad
                    });
                }

                const anim = Motion.animate(
                    dialog,
                    {
                        opacity: [1, 0],
                        scale: [1, 0.975],
                        y: [0, 6]
                    },
                    {
                        duration: TOKENS.duration.modalExit,
                        easing: TOKENS.easing.easeInQuad
                    }
                );

                dialog._tfMotionAnim = anim;
                anim.then(finish).catch(finish);
            } else if (gsap) {
                claimOwnership(dialog, 'gsap');
                if (backdrop) claimOwnership(backdrop, 'gsap');

                const tl = gsap.timeline({ onComplete: finish, defaults: { overwrite: 'auto' } });
                tl.to(dialog, { opacity: 0, scale: 0.975, y: 6, duration: 0.15, ease: 'power2.in' }, 0);
                if (backdrop) {
                    tl.to(backdrop, { opacity: 0, duration: 0.15, ease: 'power2.in' }, 0.01);
                }
            }
        });
    }

    /**
     * Animate dropdown or popover reveal with Motion
     * @param {HTMLElement|string} menuEl
     */
    function animateDropdown(menuEl) {
        const menu = typeof menuEl === 'string' ? document.querySelector(menuEl) : menuEl;
        if (!menu) return;

        if (isReducedMotion()) {
            menu.style.opacity = '1';
            menu.style.transform = 'none';
            return;
        }

        const Motion = getMotion();
        const gsap = getGsap();

        if (Motion && typeof Motion.animate === 'function') {
            claimOwnership(menu, 'motion');
            menu.style.transformOrigin = 'top right';
            const anim = Motion.animate(
                menu,
                {
                    opacity: [0, 1],
                    scaleY: [0.96, 1],
                    y: [-4, 0]
                },
                {
                    duration: TOKENS.duration.dropdown,
                    easing: TOKENS.easing.easeOutCubic
                }
            );
            menu._tfMotionAnim = anim;
            anim.then(() => {
                menu.style.transform = '';
            });
        } else if (gsap) {
            claimOwnership(menu, 'gsap');
            gsap.fromTo(menu,
                { opacity: 0, y: -4, scaleY: 0.96 },
                { opacity: 1, y: 0, scaleY: 1, transformOrigin: 'top right', duration: 0.18, ease: 'power2.out', clearProps: 'transform' }
            );
        }
    }

    /**
     * Animate tab or section content transition (fade & subtle slide)
     * @param {HTMLElement|string} contentEl
     */
    function animateTabContent(contentEl) {
        const el = typeof contentEl === 'string' ? document.querySelector(contentEl) : contentEl;
        if (!el) return;

        if (isReducedMotion()) {
            el.style.opacity = '1';
            el.style.transform = 'none';
            return;
        }

        const Motion = getMotion();
        const gsap = getGsap();

        if (Motion && typeof Motion.animate === 'function') {
            claimOwnership(el, 'motion');
            const anim = Motion.animate(
                el,
                {
                    opacity: [0.35, 1],
                    y: [6, 0]
                },
                {
                    duration: TOKENS.duration.tab,
                    easing: TOKENS.easing.easeOutCubic
                }
            );
            el._tfMotionAnim = anim;
            anim.then(() => {
                el.style.transform = '';
            });
        } else if (gsap) {
            claimOwnership(el, 'gsap');
            gsap.fromTo(el,
                { opacity: 0.35, y: 6 },
                { opacity: 1, y: 0, duration: 0.16, ease: 'power2.out', clearProps: 'transform' }
            );
        }
    }

    /**
     * Animate form mode switching (e.g. Direct Create vs Request Approval)
     * @param {HTMLElement|string} contentEl
     */
    function animateFormSwitch(contentEl) {
        const el = typeof contentEl === 'string' ? document.querySelector(contentEl) : contentEl;
        if (!el) return;

        if (isReducedMotion()) {
            el.style.opacity = '1';
            el.style.transform = 'none';
            return;
        }

        const Motion = getMotion();
        const gsap = getGsap();

        if (Motion && typeof Motion.animate === 'function') {
            claimOwnership(el, 'motion');
            const anim = Motion.animate(
                el,
                {
                    opacity: [0.4, 1],
                    y: [8, 0]
                },
                {
                    duration: TOKENS.duration.tab * 1.1,
                    easing: TOKENS.easing.easeOutCubic
                }
            );
            el._tfMotionAnim = anim;
            anim.then(() => {
                el.style.transform = '';
            });
        } else if (gsap) {
            claimOwnership(el, 'gsap');
            gsap.fromTo(el,
                { opacity: 0.4, y: 8 },
                { opacity: 1, y: 0, duration: 0.20, ease: 'power2.out', clearProps: 'transform' }
            );
        }
    }

    /**
     * Animate page content container on view load or navigation
     * @param {HTMLElement|string} contentEl
     */
    function animatePageContent(contentEl) {
        const content = typeof contentEl === 'string' ? document.querySelector(contentEl) : contentEl;
        if (!content) return;

        if (isReducedMotion()) {
            content.style.opacity = '1';
            content.style.transform = 'none';
            return;
        }

        const Motion = getMotion();
        const gsap = getGsap();

        if (Motion && typeof Motion.animate === 'function') {
            claimOwnership(content, 'motion');
            const anim = Motion.animate(
                content,
                {
                    opacity: [0.75, 1],
                    y: [4, 0]
                },
                {
                    duration: TOKENS.duration.pageContent,
                    easing: TOKENS.easing.easeOutCubic
                }
            );
            content._tfMotionAnim = anim;
            anim.then(() => {
                content.style.transform = '';
            });
        } else if (gsap) {
            claimOwnership(content, 'gsap');
            gsap.fromTo(content,
                { opacity: 0.75, y: 4 },
                { opacity: 1, y: 0, duration: 0.22, ease: 'power2.out', clearProps: 'transform' }
            );
        }
    }

    /**
     * Cross-fade transition when toggling tabs or applying heavy view changes
     * @param {HTMLElement|string} contentEl
     */
    function animateSectionTransition(contentEl) {
        animateTabContent(contentEl);
    }

    /**
     * Animate Toast notification entrance with Motion
     * @param {HTMLElement|string} toastEl
     */
    function animateToastEntrance(toastEl) {
        const el = typeof toastEl === 'string' ? document.querySelector(toastEl) : toastEl;
        if (!el) return;

        if (isReducedMotion()) {
            el.style.opacity = '1';
            el.style.transform = 'none';
            return;
        }

        const Motion = getMotion();
        const gsap = getGsap();

        if (Motion && typeof Motion.animate === 'function') {
            claimOwnership(el, 'motion');
            const anim = Motion.animate(
                el,
                {
                    opacity: [0, 1],
                    y: [20, 0],
                    scale: [0.95, 1]
                },
                {
                    duration: TOKENS.duration.toastEnter,
                    easing: TOKENS.easing.easeOutCubic
                }
            );
            el._tfMotionAnim = anim;
            anim.then(() => {
                el.style.transform = '';
            });
        } else if (gsap) {
            claimOwnership(el, 'gsap');
            gsap.fromTo(el,
                { opacity: 0, y: 20, scale: 0.95 },
                { opacity: 1, y: 0, scale: 1, duration: 0.26, ease: 'power3.out', clearProps: 'transform' }
            );
        }
    }

    /**
     * Animate Toast notification exit with Motion
     * @param {HTMLElement|string} toastEl
     * @param {Function} [onComplete]
     */
    function animateToastExit(toastEl, onComplete) {
        const el = typeof toastEl === 'string' ? document.querySelector(toastEl) : toastEl;
        if (!el) {
            if (onComplete) onComplete();
            return;
        }

        if (isReducedMotion()) {
            if (onComplete) onComplete();
            return;
        }

        const Motion = getMotion();
        const gsap = getGsap();

        if (Motion && typeof Motion.animate === 'function') {
            claimOwnership(el, 'motion');
            const anim = Motion.animate(
                el,
                {
                    opacity: [1, 0],
                    y: [0, 12],
                    scale: [1, 0.96]
                },
                {
                    duration: TOKENS.duration.toastExit,
                    easing: TOKENS.easing.easeInQuad
                }
            );
            el._tfMotionAnim = anim;
            anim.then(() => {
                releaseOwnership(el);
                if (onComplete) onComplete();
            });
        } else if (gsap) {
            claimOwnership(el, 'gsap');
            gsap.to(el, {
                opacity: 0,
                y: 12,
                scale: 0.96,
                duration: 0.18,
                ease: 'power2.in',
                onComplete: () => {
                    releaseOwnership(el);
                    if (onComplete) onComplete();
                }
            });
        } else {
            if (onComplete) onComplete();
        }
    }

    /**
     * Register micro-feedback on interactive buttons (tap/click micro-scale)
     * @param {HTMLElement|string} btnSelectorOrEl
     */
    function registerButtonFeedback(btnSelectorOrEl) {
        const el = typeof btnSelectorOrEl === 'string' ? document.querySelector(btnSelectorOrEl) : btnSelectorOrEl;
        if (!el || el._tfBtnFeedback) return;
        el._tfBtnFeedback = true;

        if (isReducedMotion()) return;

        const onDown = () => {
            if (el.disabled || el.classList.contains('disabled')) return;
            const Motion = getMotion();
            if (Motion && typeof Motion.animate === 'function') {
                claimOwnership(el, 'motion');
                Motion.animate(el, { scale: 0.975 }, { duration: 0.08, easing: 'easeOut' });
            }
        };

        const onUp = () => {
            const Motion = getMotion();
            if (Motion && typeof Motion.animate === 'function') {
                Motion.animate(el, { scale: 1 }, { duration: 0.12, easing: 'easeOut' });
            }
        };

        el.addEventListener('pointerdown', onDown, { passive: true });
        el.addEventListener('pointerup', onUp, { passive: true });
        el.addEventListener('pointercancel', onUp, { passive: true });
        el.addEventListener('pointerleave', onUp, { passive: true });
    }

    /**
     * Programmatic or event-driven hover animation for cards
     * @param {HTMLElement|string} cardEl
     * @param {boolean} isHover
     * @param {number} [liftPx=2]
     */
    function animateCardHover(cardEl, isHover, liftPx = 2) {
        const el = typeof cardEl === 'string' ? document.querySelector(cardEl) : cardEl;
        if (!el || isReducedMotion()) return;

        const Motion = getMotion();
        const gsap = getGsap();

        if (Motion && typeof Motion.animate === 'function') {
            claimOwnership(el, 'motion');
            Motion.animate(el, { y: isHover ? -liftPx : 0 }, {
                duration: TOKENS.duration.hover,
                easing: TOKENS.easing.easeOutCubic
            });
        } else if (gsap) {
            claimOwnership(el, 'gsap');
            gsap.to(el, {
                y: isHover ? -liftPx : 0,
                duration: 0.18,
                ease: 'power2.out',
                overwrite: 'auto'
            });
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

        if (isReducedMotion()) return;

        const onEnter = () => animateCardHover(cardEl, true, liftPx);
        const onLeave = () => animateCardHover(cardEl, false, liftPx);

        cardEl.addEventListener('pointerenter', onEnter, { passive: true });
        cardEl.addEventListener('pointerleave', onLeave, { passive: true });

        cardEl._tfCardCleanup = () => {
            cardEl.removeEventListener('pointerenter', onEnter);
            cardEl.removeEventListener('pointerleave', onLeave);
            releaseOwnership(cardEl);
            cardEl.style.transform = '';
            cardEl._tfCardInteractive = false;
        };
    }

    /**
     * Animate table row selection highlight
     * @param {HTMLElement|string} rowEl
     * @param {boolean} isSelected
     */
    function animateTableRowSelect(rowEl, isSelected) {
        const el = typeof rowEl === 'string' ? document.querySelector(rowEl) : rowEl;
        if (!el || isReducedMotion()) return;

        const Motion = getMotion();
        if (isSelected && Motion && typeof Motion.animate === 'function') {
            Motion.animate(el, {
                backgroundColor: ['rgba(37, 99, 235, 0.16)', 'var(--tf-table-row-selected, rgba(37, 99, 235, 0.08))']
            }, {
                duration: 0.20,
                easing: 'easeOut'
            });
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
        if (!el) {
            if (onComplete) onComplete();
            return;
        }

        if (isReducedMotion()) {
            el.style.opacity = show ? '1' : '0';
            el.style.transform = 'none';
            if (onComplete) onComplete();
            return;
        }

        const Motion = getMotion();
        if (Motion && typeof Motion.animate === 'function') {
            claimOwnership(el, 'motion');
            const anim = Motion.animate(
                el,
                {
                    opacity: show ? [0, 1] : [1, 0],
                    y: show ? [-8, 0] : [0, -6]
                },
                {
                    duration: show ? 0.16 : 0.14,
                    easing: show ? TOKENS.easing.easeOutCubic : TOKENS.easing.easeInQuad
                }
            );
            anim.then(() => {
                if (show) el.style.transform = '';
                if (onComplete) onComplete();
            });
        } else {
            if (onComplete) onComplete();
        }
    }

    // =========================================================================
    // 6. GSAP ENGINE DOMAIN: DASHBOARD SEQUENCE, SVG CHARTS, COUNTERS
    // =========================================================================

    /**
     * Animate Dashboard components entrance with coordinated GSAP timeline
     * Staggers KPI cards, main sections, activity feed, and draws SVG chart paths.
     * @param {HTMLElement|string} containerEl
     * @returns {Object|null} GSAP context
     */
    function animateDashboardEntrance(containerEl) {
        const el = typeof containerEl === 'string' ? document.querySelector(containerEl) : containerEl;
        const gsap = getGsap();
        if (isReducedMotion() || !gsap || !el) return null;

        // Prevent repeated animation when returning to dashboard in same session
        if (el._tfDashboardAnimated) return null;
        el._tfDashboardAnimated = true;

        return gsap.context(() => {
            const tl = gsap.timeline({ defaults: { overwrite: 'auto' } });

            // 1. Stagger KPI Cards
            const kpiCards = el.querySelectorAll('.tf-kpi-card');
            if (kpiCards.length > 0) {
                kpiCards.forEach(c => claimOwnership(c, 'gsap'));
                tl.fromTo(kpiCards,
                    { opacity: 0, y: 12 },
                    {
                        opacity: 1,
                        y: 0,
                        duration: TOKENS.duration.dashboardKpi,
                        stagger: 0.05,
                        ease: 'power2.out',
                        clearProps: 'transform'
                    },
                    0
                );
            }

            // 2. Stagger Main Sections
            const sections = el.querySelectorAll('.tf-dash-section, .tf-card');
            if (sections.length > 0) {
                sections.forEach(s => claimOwnership(s, 'gsap'));
                tl.fromTo(sections,
                    { opacity: 0, y: 14 },
                    {
                        opacity: 1,
                        y: 0,
                        duration: TOKENS.duration.dashboardSection,
                        stagger: 0.07,
                        ease: 'power2.out',
                        clearProps: 'transform'
                    },
                    0.08
                );
            }

            // 3. Stagger Recent Activity items if present
            const activityItems = el.querySelectorAll('.tf-activity-item, .tf-dash-activity-item');
            if (activityItems.length > 0) {
                activityItems.forEach(a => claimOwnership(a, 'gsap'));
                tl.fromTo(activityItems,
                    { opacity: 0, x: -8 },
                    {
                        opacity: 1,
                        x: 0,
                        duration: 0.32,
                        stagger: 0.04,
                        ease: 'power2.out',
                        clearProps: 'transform'
                    },
                    0.15
                );
            }

            // 4. Draw SVG Chart Line Path
            const linePath = el.querySelector('.tf-chart-line-path');
            if (linePath && typeof linePath.getTotalLength === 'function') {
                try {
                    const len = linePath.getTotalLength();
                    tl.fromTo(linePath,
                        { strokeDasharray: len, strokeDashoffset: len },
                        {
                            strokeDashoffset: 0,
                            duration: TOKENS.duration.chartDraw,
                            ease: 'power2.out'
                        },
                        0.18
                    );
                } catch (e) { }
            }

            // 5. Fade in SVG Chart Area Path
            const areaPath = el.querySelector('.tf-chart-area-path');
            if (areaPath) {
                tl.fromTo(areaPath,
                    { opacity: 0 },
                    { opacity: 1, duration: 0.50, ease: 'power1.out' },
                    0.35
                );
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

        const kpiSelector = options.kpiSelector || '.tf-kpi-card, .tf-filter-card';
        const sectionSelector = options.sectionSelector || '.tf-card, .tf-employee-sidebar, .tf-employee-main, .tf-table-container, .tf-segmented-tabs, .tf-contracts-tab-content';
        const kpiDuration = options.kpiDuration || TOKENS.duration.dashboardKpi;
        const sectionDuration = options.sectionDuration || TOKENS.duration.dashboardSection;

        return gsap.context(() => {
            const tl = gsap.timeline({ defaults: { overwrite: 'auto' } });

            const kpiCards = el.querySelectorAll(kpiSelector);
            if (kpiCards.length > 0) {
                kpiCards.forEach(c => claimOwnership(c, 'gsap'));
                tl.fromTo(kpiCards,
                    { opacity: 0, y: 12 },
                    { opacity: 1, y: 0, duration: kpiDuration, stagger: 0.05, ease: 'power2.out', clearProps: 'transform' },
                    0
                );
            }

            const sections = el.querySelectorAll(sectionSelector);
            if (sections.length > 0) {
                sections.forEach(s => claimOwnership(s, 'gsap'));
                tl.fromTo(sections,
                    { opacity: 0, y: 14 },
                    { opacity: 1, y: 0, duration: sectionDuration, stagger: 0.07, ease: 'power2.out', clearProps: 'transform' },
                    0.06
                );
            }
        }, el);
    }

    /**
     * Subtle numeric KPI ticker using GSAP
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
     * Safely cleanup all GSAP tweens and Motion animations on an element
     * @param {HTMLElement} el
     */
    function cleanupElement(el) {
        if (!el) return;
        if (el._tfCardCleanup) {
            el._tfCardCleanup();
        }
        releaseOwnership(el);
        const gsap = getGsap();
        if (gsap) {
            gsap.killTweensOf(el);
            gsap.set(el, { clearProps: 'all' });
        }
    }

    // =========================================================================
    // 7. GLOBAL INITIALIZATION & BLIND AUTO-CLEANUP
    // =========================================================================

    function getElementScrollTop(selector) {
        const el = typeof selector === 'string' ? document.querySelector(selector) : selector;
        return el ? el.scrollTop : 0;
    }

    function setElementScrollTop(selector, top) {
        const el = typeof selector === 'string' ? document.querySelector(selector) : selector;
        if (el) el.scrollTop = top;
    }

    function initGlobalTransitions() {
        const mainContent = document.querySelector('.tf-content');
        if (mainContent && !mainContent._tfAnimated) {
            mainContent._tfAnimated = true;
            animatePageContent(mainContent);
        }

        // Initialize any interactive cards & stat widgets
        const interactiveCards = document.querySelectorAll('.tf-card-interactive, .tf-stat-card');
        interactiveCards.forEach(c => registerInteractiveCard(c, 2));

        // Initialize button micro-feedback
        const interactiveButtons = document.querySelectorAll('.tf-btn:not(.tf-btn-feedback-registered), .btn:not(.tf-btn-feedback-registered)');
        interactiveButtons.forEach(b => {
            b.classList.add('tf-btn-feedback-registered');
            registerButtonFeedback(b);
        });
    }

    if (typeof window !== 'undefined') {
        // Auto-detect modal removal and unfreeze sidebar/body scroll immediately
        function setupModalDomObserver() {
            if (typeof MutationObserver === 'undefined' || typeof document === 'undefined') return;
            const observer = new MutationObserver(() => {
                if (document.body && (document.body.classList.contains('tf-modal-open') || scrollLockCount > 0)) {
                    const hasActiveModal = !!document.querySelector(
                        '.tf-modal-overlay:not(.is-closing), .modal.show:not(.is-closing), [class*="-modal-backdrop"]:not(.is-closing), .tf-gallery-modal-backdrop:not(.is-closing), .voucher-overlay:not(.is-closing)'
                    );
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
                const activeModalClose = document.querySelector(
                    '.tf-contract-modal-header .tf-modal-close-btn, .modal.show .tf-modal-close-btn, .modal.show .btn-close, .tf-preview-modal-dialog .tf-modal-close-btn, .tf-modal-backdrop .tf-modal-close-btn, .tf-modal-overlay .tf-modal-close-btn'
                );
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
                initGlobalTransitions();
            });
        }
    }

    const api = {
        // Tokens
        Tokens: TOKENS,

        // A11y & Helpers
        isReducedMotion,
        lockScroll,
        unlockScroll,
        restoreFocus,
        getElementScrollTop,
        setElementScrollTop,
        claimOwnership,
        releaseOwnership,
        cleanupElement,
        initGlobalTransitions,

        // Motion Engine Domain
        animateModalEnter,
        animateModalExit,
        animateDropdown,
        animateTabContent,
        animateFormSwitch,
        animatePageContent,
        animateSectionTransition,
        animateToastEntrance,
        animateToastExit,
        registerButtonFeedback,
        animateCardHover,
        registerInteractiveCard,
        animateTableRowSelect,
        animateBulkBar,

        // GSAP Engine Domain
        animateDashboardEntrance,
        animateModuleEntrance,
        animateCounter
    };

    if (typeof window !== 'undefined') {
        window.TradeFlowMotion = api;
        window.tradeflowMotion = api;
    }

    return api;
}));
