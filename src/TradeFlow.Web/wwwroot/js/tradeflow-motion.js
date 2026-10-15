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
        }
    }

    function unlockScroll() {
        if (typeof document === 'undefined') return;
        scrollLockCount = Math.max(0, scrollLockCount - 1);
        if (scrollLockCount === 0) {
            document.body.style.overflow = previousBodyOverflow || '';
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
                tl.fromTo(backdrop, 
                    { opacity: 0 }, 
                    { opacity: 1, duration: 0.2, ease: 'power2.out' }, 
                    0
                );
            }

            tl.fromTo(dialog,
                { opacity: 0, scale: 0.96, y: 10 },
                { opacity: 1, scale: 1, y: 0, duration: 0.24, ease: 'power3.out' },
                0.02
            );

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

            tl.to(dialog, {
                opacity: 0,
                scale: 0.96,
                y: 8,
                duration: 0.16,
                ease: 'power2.in'
            }, 0);

            if (backdrop) {
                tl.to(backdrop, {
                    opacity: 0,
                    duration: 0.18,
                    ease: 'power2.in'
                }, 0.02);
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
            { opacity: 0.88, y: 4 },
            { opacity: 1, y: 0, duration: 0.22, ease: 'power2.out', clearProps: 'transform' }
        );
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

        // Initialize any interactive cards
        const interactiveCards = document.querySelectorAll('.tf-card-interactive');
        interactiveCards.forEach(c => registerInteractiveCard(c, 2));
    }

    if (typeof window !== 'undefined') {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', initGlobalTransitions);
        } else {
            initGlobalTransitions();
        }

        if (window.Blazor && window.Blazor.addEventListener) {
            window.Blazor.addEventListener('enhancedload', function () {
                const mainContent = document.querySelector('.tf-content');
                if (mainContent) {
                    animatePageContent(mainContent);
                }
            });
        }
    }

    return {
        isReducedMotion,
        lockScroll,
        unlockScroll,
        animateModalEnter,
        animateModalExit,
        animateDropdown,
        animatePageContent,
        registerInteractiveCard,
        cleanupElement,
        initGlobalTransitions
    };
}));
