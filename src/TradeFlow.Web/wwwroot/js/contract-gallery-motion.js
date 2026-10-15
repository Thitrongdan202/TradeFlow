/**
 * TradeFlow Contract Template Gallery — High-Performance Zero-Lag Interaction
 *
 * Architecture:
 * - Layer A: Immediate, 0ms latency pointer tracking via gsap.quickSetter() on an isolated overlay.
 *            Translates a GPU-composited radial highlight without cascading style invalidations.
 * - Layer B: Stationary A4 Document: No 3D tilt, no text warping or subpixel font blur.
 * - Layer C: Discrete elevation transitions: Restrained lift (y: -3px) on pointerenter (150ms)
 *            and return to rest on pointerleave (180ms), with 100% GPU-composited shadow fade.
 */

const prefersReducedMotionQuery = typeof window !== 'undefined' && window.matchMedia ? window.matchMedia('(prefers-reduced-motion: reduce)') : { matches: false };
const finePointerQuery = typeof window !== 'undefined' && window.matchMedia ? window.matchMedia('(hover: hover) and (pointer: fine)') : { matches: true };

function isMotionEligible() {
    return !prefersReducedMotionQuery.matches && finePointerQuery.matches && typeof window !== 'undefined' && typeof window.gsap !== 'undefined';
}

export function initGalleryMotion(container) {
    if (!container) return;
    setupCardsInContainer(container);
}

export function refreshGalleryMotion(container) {
    if (!container) return;
    setupCardsInContainer(container);
}

export function disposeGalleryMotion(container) {
    if (!container) return;
    const cards = container.querySelectorAll('.tf-gallery-card');
    cards.forEach(cleanupCard);
}

// Global fallback registration for standard script tags
if (typeof window !== 'undefined') {
    window.TradeFlowGalleryMotion = {
        initGalleryMotion,
        refreshGalleryMotion,
        disposeGalleryMotion
    };
}

function setupCardsInContainer(container) {
    if (!isMotionEligible()) {
        return;
    }

    const cards = container.querySelectorAll('.tf-gallery-card');
    cards.forEach(setupCardMotion);
}

function setupCardMotion(card) {
    if (card._tfMotionInitialized) return;
    card._tfMotionInitialized = true;

    const gsap = window.gsap;
    if (!gsap) return;

    // Stationary card base transform
    gsap.set(card, {
        y: 0,
        transformPerspective: 0,
        rotationX: 0,
        rotationY: 0
    });

    // Find the isolated cursor highlight spot
    const spot = card.querySelector('.tf-highlight-spot');
    let setSpotX = null;
    let setSpotY = null;

    if (spot) {
        // gsap.quickSetter directly updates transform matrix with 0ms delay and zero interpolation chase
        setSpotX = gsap.quickSetter(spot, "x", "px");
        setSpotY = gsap.quickSetter(spot, "y", "px");
    }

    // Scroll parent tracking for rect cache invalidation
    const scrollParent = getScrollParent(card);
    const onScroll = () => {
        card._cachedRect = null;
    };

    const onPointerEnter = (e) => {
        if (!isMotionEligible()) return;
        card._cachedRect = card.getBoundingClientRect();
        card.classList.add('is-hovered');

        // Discrete elevation tween: 150ms natural lift
        gsap.to(card, {
            y: -3,
            duration: 0.15,
            ease: "power2.out",
            overwrite: "auto"
        });

        // Initialize spot position immediately on pointer entry
        if (setSpotX && setSpotY) {
            setSpotX(e.clientX - card._cachedRect.left);
            setSpotY(e.clientY - card._cachedRect.top);
        }

        if (scrollParent) {
            scrollParent.addEventListener('scroll', onScroll, { passive: true });
        }
        window.addEventListener('scroll', onScroll, { passive: true });
    };

    const onPointerMove = (e) => {
        if (!isMotionEligible()) return;
        let rect = card._cachedRect;
        if (!rect) {
            rect = card._cachedRect = card.getBoundingClientRect();
        }

        // Direct 1:1 cursor highlight tracking (0ms delay, no interpolation lag)
        if (setSpotX && setSpotY) {
            setSpotX(e.clientX - rect.left);
            setSpotY(e.clientY - rect.top);
        }
    };

    const onPointerLeave = (e) => {
        card._cachedRect = null;
        card.classList.remove('is-hovered');

        if (scrollParent) {
            scrollParent.removeEventListener('scroll', onScroll);
        }
        window.removeEventListener('scroll', onScroll);

        // Discrete return tween: 180ms smooth settle to resting elevation
        gsap.to(card, {
            y: 0,
            duration: 0.18,
            ease: "power2.out",
            overwrite: "auto"
        });
    };

    card.addEventListener('pointerenter', onPointerEnter, { passive: true });
    card.addEventListener('pointermove', onPointerMove, { passive: true });
    card.addEventListener('pointerleave', onPointerLeave, { passive: true });

    card._tfCleanups = [
        () => card.removeEventListener('pointerenter', onPointerEnter),
        () => card.removeEventListener('pointermove', onPointerMove),
        () => card.removeEventListener('pointerleave', onPointerLeave),
        () => {
            if (scrollParent) scrollParent.removeEventListener('scroll', onScroll);
            window.removeEventListener('scroll', onScroll);
        }
    ];
}

function cleanupCard(card) {
    if (card._tfCleanups) {
        card._tfCleanups.forEach(fn => fn());
        card._tfCleanups = null;
    }
    const spot = card.querySelector('.tf-highlight-spot');
    if (window.gsap) {
        window.gsap.killTweensOf(card);
        window.gsap.set(card, { clearProps: "all" });
        if (spot) {
            window.gsap.killTweensOf(spot);
            window.gsap.set(spot, { clearProps: "all" });
        }
    }
    card.classList.remove('is-hovered');
    card._tfMotionInitialized = false;
    card._cachedRect = null;
}

function getScrollParent(element) {
    let parent = element.parentElement;
    while (parent && parent !== document.body) {
        const style = window.getComputedStyle(parent);
        const overflowY = style.overflowY;
        if (overflowY === 'auto' || overflowY === 'scroll') {
            return parent;
        }
        parent = parent.parentElement;
    }
    return null;
}
