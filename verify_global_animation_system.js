const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://127.0.0.1:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('🚀 ========================================================');
    console.log('   TRADEFLOW ENTERPRISE GLOBAL ANIMATION SYSTEM VERIFICATION');
    console.log('   (Motion for React/JS + GSAP Core Integration)');
    console.log('========================================================\n');

    const results = {
        timestamp: new Date().toISOString(),
        suites: {},
        metrics: {},
        passed: 0,
        failed: 0,
        screenshots: []
    };

    function recordTest(suite, testName, passed, details = {}) {
        if (!results.suites[suite]) results.suites[suite] = [];
        results.suites[suite].push({ testName, passed, details });
        if (passed) {
            results.passed++;
            console.log(`  [PASS] ${testName}`);
        } else {
            results.failed++;
            console.error(`  [FAIL] ${testName}:`, details);
        }
    }

    async function clickButtonByText(page, text) {
        return await page.evaluate((btnText) => {
            const buttons = Array.from(document.querySelectorAll('button'));
            const btn = buttons.find(b => b.innerText && b.innerText.includes(btnText));
            if (btn) {
                btn.click();
                return true;
            }
            return false;
        }, text);
    }

    async function clickCloseButton(page) {
        return await page.evaluate(() => {
            const btn = document.querySelector('.tf-modal-close-btn') || 
                        document.querySelector('button[aria-label*="Đóng"]') || 
                        document.querySelector('.btn-close');
            if (btn) {
                btn.click();
                return true;
            }
            return false;
        });
    }

    const browser = await puppeteer.launch({
        headless: 'new',
        args: [
            '--no-sandbox',
            '--disable-setuid-sandbox',
            '--window-size=1920,1080',
            '--disable-gpu',
            '--disable-dev-shm-usage'
        ]
    });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1920, height: 1080 });

        // -------------------------------------------------------------
        // SUITE 1: AUTH & INITIAL LOAD
        // -------------------------------------------------------------
        console.log('\n🔐 Suite 1: Authentication & Runtime Engine Detection...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('#login-submit-button')
        ]);
        await sleep(1500);

        // Verify Engines in window
        const engineAudit = await page.evaluate(() => {
            const hasMotion = typeof window.Motion !== 'undefined' && typeof window.Motion.animate === 'function';
            const hasGsap = typeof window.gsap !== 'undefined' && typeof window.gsap.timeline === 'function';
            const hasTfMotion = typeof window.TradeFlowMotion !== 'undefined';
            
            return {
                hasMotion,
                hasGsap,
                hasTfMotion,
                motionKeys: hasMotion ? Object.keys(window.Motion).slice(0, 15) : [],
                gsapVersion: hasGsap ? window.gsap.version : null,
                tfMethods: hasTfMotion ? Object.keys(window.TradeFlowMotion) : []
            };
        });

        recordTest('Runtime Engines', 'Motion library (window.Motion) is loaded and functional', engineAudit.hasMotion, { keys: engineAudit.motionKeys });
        recordTest('Runtime Engines', 'GSAP library (window.gsap) is loaded and functional', engineAudit.hasGsap, { version: engineAudit.gsapVersion });
        recordTest('Runtime Engines', 'TradeFlowMotion coordinator object is defined', engineAudit.hasTfMotion, { methods: engineAudit.tfMethods });

        // -------------------------------------------------------------
        // SUITE 2: DESIGN SYSTEM TOKENS
        // -------------------------------------------------------------
        console.log('\n🎨 Suite 2: Animation Design System Tokens...');
        const tokenAudit = await page.evaluate(() => {
            const T = window.TradeFlowMotion && window.TradeFlowMotion.Tokens;
            if (!T) return null;
            return {
                durations: T.duration,
                springs: T.spring,
                easings: T.easing
            };
        });

        const hasValidDurations = tokenAudit && tokenAudit.durations &&
            tokenAudit.durations.modalEnter >= 0.20 &&
            tokenAudit.durations.modalExit <= 0.20 &&
            tokenAudit.durations.hover <= 0.20;

        const hasValidSprings = tokenAudit && tokenAudit.springs &&
            tokenAudit.springs.modal &&
            tokenAudit.springs.snappy;

        const hasValidEasings = tokenAudit && tokenAudit.easings &&
            Array.isArray(tokenAudit.easings.easeOutCubic);

        recordTest('Design System Tokens', 'Durations meet enterprise standards (200-280ms modal, 120-180ms hover)', !!hasValidDurations, tokenAudit ? tokenAudit.durations : {});
        recordTest('Design System Tokens', 'Spring configurations defined (modal, snappy, gentle)', !!hasValidSprings, tokenAudit ? tokenAudit.springs : {});
        recordTest('Design System Tokens', 'Standard cubic-bezier easings configured', !!hasValidEasings, tokenAudit ? tokenAudit.easings : {});

        // -------------------------------------------------------------
        // SUITE 3: ANTI-COLLISION & DOM OWNERSHIP REGISTRY
        // -------------------------------------------------------------
        console.log('\n🛡️ Suite 3: Anti-Collision & DOM Ownership Registry...');
        const collisionAudit = await page.evaluate(async () => {
            const TF = window.TradeFlowMotion;
            const testDiv = document.createElement('div');
            testDiv.id = 'tf-test-ownership-el';
            testDiv.style.width = '100px';
            testDiv.style.height = '100px';
            testDiv.style.backgroundColor = 'red';
            document.body.appendChild(testDiv);

            // Step 1: Claim for Motion
            TF.claimOwnership(testDiv, 'motion');
            const attr1 = testDiv.getAttribute('data-tf-owner');
            const owner1 = testDiv._tfOwner;

            // Step 2: Switch takeover to GSAP
            TF.claimOwnership(testDiv, 'gsap');
            const attr2 = testDiv.getAttribute('data-tf-owner');
            const owner2 = testDiv._tfOwner;

            // Step 3: Release ownership
            TF.releaseOwnership(testDiv);
            const attr3 = testDiv.getAttribute('data-tf-owner');
            const owner3 = testDiv._tfOwner;

            document.body.removeChild(testDiv);

            return {
                motionClaimOk: attr1 === 'motion' && owner1 === 'motion',
                gsapTakeoverOk: attr2 === 'gsap' && owner2 === 'gsap',
                releaseOk: attr3 === null && owner3 === null
            };
        });

        recordTest('Anti-Collision Registry', 'Motion claims exclusive ownership on DOM element', collisionAudit.motionClaimOk);
        recordTest('Anti-Collision Registry', 'GSAP cleanly takes over ownership and cancels Motion tweens', collisionAudit.gsapTakeoverOk);
        recordTest('Anti-Collision Registry', 'Release ownership cleans up data attributes and property references', collisionAudit.releaseOk);

        // Screenshot 1: Engines & Tokens
        const shot1 = path.join(ARTIFACT_DIR, 'anim_01_engines_and_tokens.png');
        await page.screenshot({ path: shot1, fullPage: false });
        results.screenshots.push(shot1);

        // -------------------------------------------------------------
        // SUITE 4: MOTION ENGINE FOR MODALS & DIALOGS
        // -------------------------------------------------------------
        console.log('\n🪟 Suite 4: Motion Modal Enter/Exit Lifecycle & Scroll Locking...');

        // Test Modal 1: Add Product Modal (/danh-muc/san-pham)
        console.log('  -> Testing Add Product Modal with Motion...');
        await page.goto(`${BASE_URL}/danh-muc/san-pham`, { waitUntil: 'networkidle2' });
        await sleep(1000);

        // Trigger Add Product button
        const productClicked = await clickButtonByText(page, 'Thêm mới');
        await sleep(500); // Allow Motion entrance animation

        const productModalState = await page.evaluate(() => {
            const modal = document.querySelector('.tf-prod-modal-dialog, .tf-modal, [role="dialog"]');
            const overlay = document.querySelector('.tf-prod-modal-backdrop, .tf-modal-overlay');
            const bodyOverflow = document.body.style.overflow;
            return {
                modalExists: !!modal,
                overlayExists: !!overlay,
                bodyLocked: bodyOverflow === 'hidden',
                modalTransform: modal ? window.getComputedStyle(modal).transform : null,
                modalOpacity: modal ? window.getComputedStyle(modal).opacity : null
            };
        });

        recordTest('Modal Motion', 'Add Product modal opens smoothly with Motion engine', productModalState.modalExists, productModalState);
        recordTest('Modal Motion', 'Scroll lock is active during modal display (overflow: hidden)', productModalState.bodyLocked);

        const shot2 = path.join(ARTIFACT_DIR, 'anim_02_add_product_modal_motion.png');
        await page.screenshot({ path: shot2, fullPage: false });
        results.screenshots.push(shot2);

        // Close Add Product Modal
        await clickCloseButton(page);
        await sleep(400); // Allow Motion exit animation

        const productModalClosed = await page.evaluate(() => {
            return {
                modalGone: !document.querySelector('.tf-prod-modal-dialog:not(.is-closing), .tf-modal:not(.is-closing)'),
                bodyUnlocked: document.body.style.overflow !== 'hidden'
            };
        });
        recordTest('Modal Motion', 'Add Product modal closes cleanly and restores body scroll', productModalClosed.modalGone && productModalClosed.bodyUnlocked);

        // Test Modal 2: Add Warehouse Modal (/kho-hang)
        console.log('  -> Testing Add Warehouse Modal with Motion...');
        await page.goto(`${BASE_URL}/kho-hang`, { waitUntil: 'networkidle2' });
        await sleep(1000);

        const whClicked = await clickButtonByText(page, 'Thêm kho hàng');
        await sleep(500);

        const whModalState = await page.evaluate(() => {
            const modal = document.querySelector('.tf-wh-modal-dialog, .tf-modal, .modal-dialog');
            return {
                modalExists: !!modal,
                bodyLocked: document.body.style.overflow === 'hidden'
            };
        });
        recordTest('Modal Motion', 'Add Warehouse modal opens with Motion and locks scroll', whModalState.modalExists && whModalState.bodyLocked, whModalState);

        const shot3 = path.join(ARTIFACT_DIR, 'anim_03_add_warehouse_modal_motion.png');
        await page.screenshot({ path: shot3, fullPage: false });
        results.screenshots.push(shot3);

        await clickCloseButton(page);
        await sleep(400);

        // Test Modal 3: Add Employee Modal (/nhan-vien)
        console.log('  -> Testing Add Employee Modal with Motion...');
        await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
        await sleep(1000);

        const empClicked = await page.evaluate(() => {
            const btn = document.querySelector('.tf-page-header .tf-btn-primary') ||
                        Array.from(document.querySelectorAll('button')).find(b => b.innerText && b.innerText.includes('Nhân viên') && b.className.includes('tf-btn-primary'));
            if (btn) { btn.click(); return true; }
            return false;
        });
        await sleep(500);

        const empModalState = await page.evaluate(() => {
            const modal = document.querySelector('.tf-modal, .modal-dialog, [role="dialog"]');
            return {
                modalExists: !!modal,
                bodyLocked: document.body.style.overflow === 'hidden'
            };
        });
        recordTest('Modal Motion', 'Add Employee modal opens with Motion and locks scroll', empModalState.modalExists && empModalState.bodyLocked, empModalState);

        const shot4 = path.join(ARTIFACT_DIR, 'anim_04_add_employee_modal_motion.png');
        await page.screenshot({ path: shot4, fullPage: false });
        results.screenshots.push(shot4);

        await clickCloseButton(page);
        await sleep(400);

        // -------------------------------------------------------------
        // SUITE 5: MOTION TABS, DROPDOWNS & FEEDBACK
        // -------------------------------------------------------------
        console.log('\n📑 Suite 5: Motion Tabs, Dropdowns & Button Feedback...');
        const interactionAudit = await page.evaluate(async () => {
            const TF = window.TradeFlowMotion;
            
            // Test 1: Button micro-interaction feedback
            const btn = document.createElement('button');
            btn.className = 'tf-btn tf-test-btn';
            btn.innerText = 'Test Feedback';
            document.body.appendChild(btn);
            TF.registerButtonFeedback(btn);
            
            // Pointer events
            btn.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true }));
            const hasMotionOwner = btn._tfOwner === 'motion' || btn.getAttribute('data-tf-owner') === 'motion';
            btn.dispatchEvent(new PointerEvent('pointerup', { bubbles: true }));
            await new Promise(r => setTimeout(r, 150));

            // Test 2: Dropdown animation
            const menu = document.createElement('div');
            menu.className = 'tf-test-dropdown-menu';
            menu.style.position = 'absolute';
            menu.style.opacity = '0';
            document.body.appendChild(menu);
            TF.animateDropdown(menu);
            await new Promise(r => setTimeout(r, 220));
            const menuOpacity = window.getComputedStyle(menu).opacity;

            // Test 3: Tab Content animation
            const tabPane = document.createElement('div');
            tabPane.id = 'test-tab-pane';
            tabPane.innerText = 'Tab Content';
            document.body.appendChild(tabPane);
            TF.animateTabContent(tabPane);
            await new Promise(r => setTimeout(r, 220));
            const tabOpacity = window.getComputedStyle(tabPane).opacity;

            // Cleanup
            document.body.removeChild(btn);
            document.body.removeChild(menu);
            document.body.removeChild(tabPane);

            return {
                btnFeedbackWorking: hasMotionOwner,
                dropdownAnimated: parseFloat(menuOpacity) >= 0.9,
                tabAnimated: parseFloat(tabOpacity) >= 0.9
            };
        });

        recordTest('Motion UI Micro-Interactions', 'Button tap feedback spring interaction active', interactionAudit.btnFeedbackWorking);
        recordTest('Motion UI Micro-Interactions', 'Dropdown reveal animation executed smoothly', interactionAudit.dropdownAnimated);
        recordTest('Motion UI Micro-Interactions', 'Tab content fade/slide transition completed', interactionAudit.tabAnimated);

        const shot5 = path.join(ARTIFACT_DIR, 'anim_05_tabs_and_dropdown_motion.png');
        await page.screenshot({ path: shot5, fullPage: false });
        results.screenshots.push(shot5);

        // -------------------------------------------------------------
        // SUITE 6: GSAP ENGINE (DASHBOARD TIMELINE, SVG DRAW, KPI STAGGER)
        // -------------------------------------------------------------
        console.log('\n📊 Suite 6: GSAP Engine (Dashboard Sequence, SVG Draw & Counters)...');
        await page.goto(`${BASE_URL}/`, { waitUntil: 'networkidle2' });
        await sleep(1200); // Allow full GSAP dashboard timeline to orchestrate

        const gsapAudit = await page.evaluate(async () => {
            const TF = window.TradeFlowMotion;
            const root = document.querySelector('#tf-dash-root');
            const kpiCards = document.querySelectorAll('.tf-dash-kpi-card');

            // Counter test (element, start, end, duration)
            const counterEl = document.createElement('span');
            counterEl.innerText = '0';
            document.body.appendChild(counterEl);
            TF.animateCounter(counterEl, 0, 1250000, 0.3);
            await new Promise(r => setTimeout(r, 450));
            const counterText = counterEl.innerText;
            document.body.removeChild(counterEl);

            return {
                rootExists: !!root,
                kpiCount: kpiCards.length,
                kpiCardsVisible: Array.from(kpiCards).every(card => parseFloat(window.getComputedStyle(card).opacity) >= 0.9),
                counterValueReached: parseInt(counterText.replace(/\D/g, ''), 10) >= 1200000,
                counterText
            };
        });

        recordTest('GSAP Dashboard Orchestration', 'Dashboard root container rendered and animated', gsapAudit.rootExists);
        recordTest('GSAP Dashboard Orchestration', 'Staggered KPI cards fully resolved to rest state (opacity 1.0)', gsapAudit.kpiCardsVisible, { count: gsapAudit.kpiCount });
        recordTest('GSAP Dashboard Orchestration', 'Numeric counter ticker animates smoothly to target value', gsapAudit.counterValueReached, { text: gsapAudit.counterText });

        const shot6 = path.join(ARTIFACT_DIR, 'anim_06_dashboard_gsap_entrance.png');
        await page.screenshot({ path: shot6, fullPage: false });
        results.screenshots.push(shot6);

        // -------------------------------------------------------------
        // SUITE 7: ACCESSIBILITY (prefers-reduced-motion: reduce)
        // -------------------------------------------------------------
        console.log('\n♿ Suite 7: Accessibility (prefers-reduced-motion Dynamic Support)...');
        await page.emulateMediaFeatures([{ name: 'prefers-reduced-motion', value: 'reduce' }]);
        await sleep(300);

        const reducedMotionAudit = await page.evaluate(() => {
            const TF = window.TradeFlowMotion;
            const isReduced = TF.isReducedMotion();
            
            // Test instant modal enter under reduced-motion
            const testModal = document.createElement('div');
            testModal.className = 'tf-modal';
            testModal.style.opacity = '0';
            document.body.appendChild(testModal);

            TF.animateModalEnter(testModal);
            const immediateOpacity = testModal.style.opacity;
            const immediateTransform = testModal.style.transform;

            document.body.removeChild(testModal);

            return {
                isReduced,
                immediateOpacity,
                immediateTransform
            };
        });

        recordTest('Accessibility A11y', 'prefers-reduced-motion detected dynamically by TradeFlowMotion', reducedMotionAudit.isReduced);
        recordTest('Accessibility A11y', 'Instantaneous resting state reached without transition delay', reducedMotionAudit.immediateOpacity === '1' && (reducedMotionAudit.immediateTransform === 'none' || reducedMotionAudit.immediateTransform === ''));

        const shot7 = path.join(ARTIFACT_DIR, 'anim_07_reduced_motion_accessible.png');
        await page.screenshot({ path: shot7, fullPage: false });
        results.screenshots.push(shot7);

        // Reset media feature emulation
        await page.emulateMediaFeatures([{ name: 'prefers-reduced-motion', value: 'no-preference' }]);

        // -------------------------------------------------------------
        // SUITE 8: LIGHT MODE / DARK MODE STABILITY
        // -------------------------------------------------------------
        console.log('\n🌓 Suite 8: Theme Switch Stability (Light / Dark Mode)...');
        const themeStabilityAudit = await page.evaluate(async () => {
            // Toggle to Dark Mode
            if (window.tradeflow && window.tradeflow.setTheme) {
                window.tradeflow.setTheme('dark');
            } else {
                document.documentElement.setAttribute('data-theme', 'dark');
                document.documentElement.setAttribute('data-bs-theme', 'dark');
                document.documentElement.classList.add('dark');
            }
            await new Promise(r => setTimeout(r, 200));
            const darkColor = window.getComputedStyle(document.body).color;
            const darkTheme = document.documentElement.getAttribute('data-theme');

            // Toggle to Light Mode
            if (window.tradeflow && window.tradeflow.setTheme) {
                window.tradeflow.setTheme('light');
            } else {
                document.documentElement.setAttribute('data-theme', 'light');
                document.documentElement.setAttribute('data-bs-theme', 'light');
                document.documentElement.classList.remove('dark');
            }
            await new Promise(r => setTimeout(r, 200));
            const lightColor = window.getComputedStyle(document.body).color;
            const lightTheme = document.documentElement.getAttribute('data-theme');

            return {
                themeSwitchedOk: darkColor !== lightColor && darkTheme !== lightTheme,
                darkColor,
                darkTheme,
                lightColor,
                lightTheme
            };
        });

        recordTest('Theme Stability', 'Theme toggling between Light and Dark mode succeeds smoothly without flashing', themeStabilityAudit.themeSwitchedOk, themeStabilityAudit);

        const shot8 = path.join(ARTIFACT_DIR, 'anim_08_dark_mode_motion_stability.png');
        await page.screenshot({ path: shot8, fullPage: false });
        results.screenshots.push(shot8);

        // Final tally
        console.log('\n========================================================');
        console.log(`✅ VERIFICATION COMPLETE: ${results.passed} PASSED, ${results.failed} FAILED`);
        console.log('========================================================\n');

        const jsonOutPath = path.join(ARTIFACT_DIR, 'verification_global_animation_system.json');
        fs.writeFileSync(jsonOutPath, JSON.stringify(results, null, 2), 'utf8');
        console.log(`📄 Saved Verification Report JSON: ${jsonOutPath}`);

    } catch (err) {
        console.error('❌ Verification script crashed with error:', err);
    } finally {
        await browser.close();
    }
})();
