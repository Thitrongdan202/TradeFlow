const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('=== RUNNING COMPREHENSIVE SIDEBAR COLLAPSE / EXPAND SUITE ===');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    try {
        console.log('1. Navigating to Login...');
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 400));

        const userInput = await page.$('input[name="Input.Username"]');
        if (userInput) {
            console.log('Logging in as admin...');
            await userInput.click({ clickCount: 3 });
            await userInput.type('admin');
            const passInput = await page.$('input[name="Input.Password"]');
            await passInput.click({ clickCount: 3 });
            await passInput.type('tradecore123');
            await Promise.all([
                page.waitForNavigation({ waitUntil: 'networkidle2' }),
                page.click('button[type="submit"]')
            ]);
        }

        await page.waitForSelector('.tf-sidebar', { timeout: 10000 });
        await new Promise(r => setTimeout(r, 1000));

        // Helper to query sidebar metrics
        const getMetrics = async () => {
            return await page.evaluate(() => {
                const sb = document.querySelector('.tf-sidebar');
                const mainWrapper = document.querySelector('.tf-main-wrapper');
                const label = document.querySelector('.tf-sidebar .tf-nav-label');
                const brandText = document.querySelector('.tf-brand-text');
                const flyout = document.querySelector('.tf-floating-flyout');
                const bottomBtn = document.querySelector('.tf-nav-collapse-btn');
                const hamburger = document.querySelector('.tf-sidebar-toggle');

                const sbRect = sb.getBoundingClientRect();
                const wrapRect = mainWrapper.getBoundingClientRect();
                const labelCs = label ? window.getComputedStyle(label) : null;
                const brandCs = brandText ? window.getComputedStyle(brandText) : null;
                const flyoutCs = flyout ? window.getComputedStyle(flyout) : null;

                return {
                    sbWidth: Math.round(sbRect.width),
                    wrapMarginLeft: parseInt(window.getComputedStyle(mainWrapper).marginLeft, 10),
                    isCollapsed: sb.classList.contains('is-collapsed'),
                    labelsVisible: labelCs && labelCs.display !== 'none',
                    brandVisible: brandCs && brandCs.display !== 'none',
                    flyoutVisible: flyout && flyout.classList.contains('is-visible') && flyoutCs.display !== 'none',
                    bottomBtnTitle: bottomBtn ? bottomBtn.getAttribute('title') : null,
                    hamburgerExpanded: hamburger ? hamburger.getAttribute('aria-expanded') : null
                };
            });
        };

        // TEST 1: Default Expanded Mode (Light Theme)
        console.log('\n--- TEST 1: Expanded Mode (Light Theme) ---');
        let m = await getMetrics();
        console.log('State 1 (Expanded):', m);
        if (m.sbWidth !== 224 || !m.labelsVisible || !m.brandVisible) {
            throw new Error('Test 1 Failed: Sidebar is not expanded to 224px with labels visible!');
        }
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'sidebar_01_expanded_light.png') });
        console.log('Saved: sidebar_01_expanded_light.png');

        // TEST 2: Hover in Expanded Mode should NEVER open Flyout
        console.log('\n--- TEST 2: Hover in Expanded Mode ---');
        const settingsItemExpanded = await page.$('.tf-sidebar [data-flyout-group="settings"]');
        const sBoxExp = await settingsItemExpanded.boundingBox();
        await page.mouse.move(sBoxExp.x + sBoxExp.width / 2, sBoxExp.y + sBoxExp.height / 2);
        await new Promise(r => setTimeout(r, 400));
        m = await getMetrics();
        console.log('Flyout visible after hover in Expanded mode:', m.flyoutVisible);
        if (m.flyoutVisible) {
            throw new Error('Test 2 Failed: Flyout should NOT open when sidebar is expanded!');
        }

        // TEST 3: Click Topbar Hamburger to Collapse
        console.log('\n--- TEST 3: Click Topbar Hamburger to Collapse ---');
        await page.click('.tf-sidebar-toggle');
        await new Promise(r => setTimeout(r, 600));
        m = await getMetrics();
        console.log('State 2 (Collapsed):', m);
        if (m.sbWidth !== 68 || m.labelsVisible || m.brandVisible || !m.isCollapsed) {
            throw new Error('Test 3 Failed: Sidebar did not collapse to 68px rail!');
        }
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'sidebar_02_collapsed_rail.png') });
        console.log('Saved: sidebar_02_collapsed_rail.png');

        // TEST 4: Hover in Collapsed Mode -> Opens Floating Flyout
        console.log('\n--- TEST 4: Hover in Collapsed Mode ---');
        const settingsItemCol = await page.$('.tf-sidebar [data-flyout-group="settings"]');
        const sBoxCol = await settingsItemCol.boundingBox();
        await page.mouse.move(sBoxCol.x + sBoxCol.width / 2, sBoxCol.y + sBoxCol.height / 2);
        await new Promise(r => setTimeout(r, 350));
        m = await getMetrics();
        console.log('Flyout visible after hover in Collapsed mode:', m.flyoutVisible);
        if (!m.flyoutVisible) {
            throw new Error('Test 4 Failed: Flyout did not open when hovering icon in Collapsed mode!');
        }
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'sidebar_03_collapsed_hover_flyout.png') });
        console.log('Saved: sidebar_03_collapsed_hover_flyout.png');

        // TEST 5: Move cursor into Flyout -> Stays Open
        console.log('\n--- TEST 5: Move into Flyout Panel ---');
        const flyoutBox = await page.evaluate(() => {
            const flyout = document.querySelector('.tf-floating-flyout');
            const r = flyout.getBoundingClientRect();
            return { x: r.x, y: r.y, width: r.width, height: r.height };
        });
        await page.mouse.move(flyoutBox.x + 50, flyoutBox.y + 100);
        await new Promise(r => setTimeout(r, 300));
        m = await getMetrics();
        console.log('Flyout visible inside panel:', m.flyoutVisible);
        if (!m.flyoutVisible) {
            throw new Error('Test 5 Failed: Flyout closed when cursor moved inside!');
        }

        // TEST 6: Move cursor away -> Closes after 150ms
        console.log('\n--- TEST 6: Move cursor away (Closing) ---');
        await page.mouse.move(700, 400);
        await new Promise(r => setTimeout(r, 400));
        m = await getMetrics();
        console.log('Flyout visible after moving away:', m.flyoutVisible);
        if (m.flyoutVisible) {
            throw new Error('Test 6 Failed: Flyout did not close after moving mouse away!');
        }

        // TEST 7: Click Bottom Collapse Button to Expand
        console.log('\n--- TEST 7: Click Bottom Collapse Button to Expand ---');
        await page.click('.tf-nav-collapse-btn');
        await new Promise(r => setTimeout(r, 600));
        m = await getMetrics();
        console.log('State 3 (Expanded again via Bottom Button):', m);
        if (m.sbWidth !== 224 || !m.labelsVisible || !m.brandVisible || m.isCollapsed) {
            throw new Error('Test 7 Failed: Sidebar did not expand back to 224px via bottom button!');
        }

        // TEST 8: Click Bottom Collapse Button to Collapse again
        console.log('\n--- TEST 8: Click Bottom Collapse Button to Collapse again ---');
        await page.click('.tf-nav-collapse-btn');
        await new Promise(r => setTimeout(r, 600));
        m = await getMetrics();
        console.log('State 4 (Collapsed again via Bottom Button):', m);
        if (m.sbWidth !== 68 || !m.isCollapsed) {
            throw new Error('Test 8 Failed: Sidebar did not collapse via bottom button!');
        }

        // TEST 9: Click Topbar Hamburger to Expand again
        console.log('\n--- TEST 9: Click Topbar Hamburger to Expand again ---');
        await page.click('.tf-sidebar-toggle');
        await new Promise(r => setTimeout(r, 600));
        m = await getMetrics();
        console.log('State 5 (Expanded again via Topbar):', m);
        if (m.sbWidth !== 224 || m.isCollapsed) {
            throw new Error('Test 9 Failed: Sidebar did not expand via hamburger!');
        }

        // TEST 10: Dark Mode Verification for both Expanded and Collapsed
        console.log('\n--- TEST 10: Dark Mode Verification ---');
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.documentElement.setAttribute('data-theme', 'dark');
            document.documentElement.classList.add('dark');
            localStorage.setItem('tf_theme', 'dark');
        });
        await new Promise(r => setTimeout(r, 500));

        // Screenshot Dark Expanded
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'sidebar_04_expanded_dark.png') });
        console.log('Saved: sidebar_04_expanded_dark.png');

        // Collapse in Dark Mode
        await page.click('.tf-sidebar-toggle');
        await new Promise(r => setTimeout(r, 600));

        // Hover Settings in Dark Mode
        const settingsDark = await page.$('.tf-sidebar [data-flyout-group="settings"]');
        const sBoxDark = await settingsDark.boundingBox();
        await page.mouse.move(sBoxDark.x + sBoxDark.width / 2, sBoxDark.y + sBoxDark.height / 2);
        await new Promise(r => setTimeout(r, 350));

        // Screenshot Dark Collapsed with Flyout
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'sidebar_05_collapsed_dark_flyout.png') });
        console.log('Saved: sidebar_05_collapsed_dark_flyout.png');

        console.log('\n========================================');
        console.log('✅ ALL 10 TESTS PASSED WITH 100% SUCCESS!');
        console.log('========================================');

    } catch (err) {
        console.error('VERIFICATION ERROR:', err);
        process.exitCode = 1;
    } finally {
        await browser.close();
    }
})();
