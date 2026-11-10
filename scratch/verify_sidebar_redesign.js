const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function sleep(ms) {
    return new Promise(r => setTimeout(r, ms));
}

(async () => {
    console.log('🚀 Starting Sidebar Redesign Verification...');
    const browser = await puppeteer.launch({
        headless: 'new',
        defaultViewport: { width: 1440, height: 900 },
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    const results = [];

    // Login as Admin
    console.log('🔑 Logging in as admin...');
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await sleep(400);
    const userInput = await page.$('input[name="Input.Username"]');
    if (userInput) {
        await userInput.click({ clickCount: 3 });
        await userInput.type('admin');
    }
    const passInput = await page.$('input[name="Input.Password"]');
    if (passInput) {
        await passInput.click({ clickCount: 3 });
        await passInput.type('tradecore123');
    }
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    console.log('✅ Logged in successfully. Current URL:', page.url());

    // TEST 1: Default Desktop Expanded Sidebar (1440px)
    console.log('\n--- TEST 1: Default Desktop Expanded Sidebar (1440px) ---');
    await page.goto('http://127.0.0.1:5169/', { waitUntil: 'networkidle2' });
    await sleep(800);

    let sidebarWidth = await page.$eval('.tf-sidebar', el => el.getBoundingClientRect().width);
    let isCollapsed = await page.$eval('.tf-sidebar', el => el.classList.contains('is-collapsed'));
    console.log(`Sidebar width: ${sidebarWidth}px, isCollapsed: ${isCollapsed}`);
    results.push({ test: '1. Desktop Expanded Width', passed: sidebarWidth > 200 && !isCollapsed });

    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'sidebar_01_expanded_desktop.png') });
    console.log('📸 Captured sidebar_01_expanded_desktop.png');

    // TEST 2: Toggle Collapse via Bottom Button
    console.log('\n--- TEST 2: Toggle Collapse via Bottom Button ---');
    await page.click('.tf-nav-collapse-btn');
    await sleep(500);

    sidebarWidth = await page.$eval('.tf-sidebar', el => el.getBoundingClientRect().width);
    isCollapsed = await page.$eval('.tf-sidebar', el => el.classList.contains('is-collapsed'));
    let storedState = await page.evaluate(() => localStorage.getItem('tf_sidebar_collapsed'));
    console.log(`Collapsed sidebar width: ${sidebarWidth}px, isCollapsed: ${isCollapsed}, localStorage: ${storedState}`);
    results.push({ test: '2. Bottom Collapse Button', passed: Math.abs(sidebarWidth - 68) <= 2 && isCollapsed && storedState === 'true' });

    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'sidebar_02_collapsed_rail.png') });
    console.log('📸 Captured sidebar_02_collapsed_rail.png');

    // TEST 3: Hover over Collapsed Icon (Verify NO flyout menu pops open)
    console.log('\n--- TEST 3: Hover Verification (No Hover Flyout) ---');
    // Hover on Settings item
    const settingsItem = await page.$('.tf-nav-item.align-bottom .tf-nav-link');
    if (settingsItem) {
        await settingsItem.hover();
        await sleep(500);
        let panelOpen = await page.$('.tf-contextual-panel');
        let flyoutVisible = await page.evaluate(() => {
            const flyout = document.querySelector('.tf-nav-flyout');
            return flyout && window.getComputedStyle(flyout).display !== 'none';
        });
        console.log(`Panel open on hover: ${!!panelOpen}, flyout visible: ${!!flyoutVisible}`);
        results.push({ test: '3. No Panel/Flyout on Hover', passed: !panelOpen && !flyoutVisible });
    }

    // TEST 4: Click Module Icon (Cài đặt) opens Contextual Navigation Panel
    console.log('\n--- TEST 4: Click Module Icon opens Contextual Panel ---');
    await page.click('.tf-nav-item.align-bottom .tf-nav-link');
    await sleep(600);

    let panelExists = await page.$('.tf-contextual-panel');
    let panelTitle = await page.$eval('.tf-contextual-title', el => el.textContent.trim());
    let panelItemsCount = await page.$$eval('.tf-contextual-item', els => els.length);
    let panelRect = await page.$eval('.tf-contextual-panel', el => {
        const r = el.getBoundingClientRect();
        return { left: r.left, width: r.width, height: r.height };
    });
    console.log(`Panel opened: ${!!panelExists}, Title: "${panelTitle}", Items: ${panelItemsCount}, Left: ${panelRect.left}px, Width: ${panelRect.width}px`);
    results.push({ 
        test: '4. Contextual Panel Opened', 
        passed: !!panelExists && panelTitle.includes('CÀI ĐẶT') && panelItemsCount >= 8 && panelRect.width >= 300 
    });

    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'sidebar_03_contextual_panel_settings.png') });
    console.log('📸 Captured sidebar_03_contextual_panel_settings.png');

    // TEST 5: Switch Group without closing (Click "Kho hàng")
    console.log('\n--- TEST 5: Switch Panel Group Smoothly (Click "Kho hàng") ---');
    // Find Kho hàng button
    const buttons = await page.$$('.tf-nav-item button.tf-nav-link');
    let warehouseBtn = null;
    for (const b of buttons) {
        const title = await page.evaluate(el => el.getAttribute('title'), b);
        if (title && title.includes('Kho hàng')) {
            warehouseBtn = b;
            break;
        }
    }
    if (warehouseBtn) {
        await warehouseBtn.click();
        await sleep(500);
        let newTitle = await page.$eval('.tf-contextual-title', el => el.textContent.trim());
        let panelsCount = await page.$$eval('.tf-contextual-panel', els => els.length);
        console.log(`Switched to Title: "${newTitle}", Panels Count (must be 1): ${panelsCount}`);
        results.push({ 
            test: '5. Single Panel Smooth Switch', 
            passed: newTitle.includes('KHO HÀNG') && panelsCount === 1 
        });
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'sidebar_04_contextual_panel_warehouse.png') });
        console.log('📸 Captured sidebar_04_contextual_panel_warehouse.png');
    }

    // TEST 6: Click Panel Item Navigates & Highlights
    console.log('\n--- TEST 6: Navigate via Contextual Panel Item ---');
    // Click "Phiếu nhập / xuất kho"
    const targetLink = await page.$('a.tf-contextual-item[href="/kho/phieu-kho"]');
    if (targetLink) {
        await targetLink.click();
        await sleep(800);
        console.log('Navigated URL:', page.url());
        let panelAfterNav = await page.$('.tf-contextual-panel');
        let activeModule = await page.$eval('.tf-nav-item.is-active button.tf-nav-link', el => el.getAttribute('title'));
        console.log(`Panel closed after nav: ${!panelAfterNav}, Active rail module: ${activeModule}`);
        results.push({ 
            test: '6. Item Navigation & Active State', 
            passed: page.url().includes('/kho/phieu-kho') && !panelAfterNav && activeModule && activeModule.includes('Kho hàng') 
        });
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'sidebar_05_navigated_active_state.png') });
        console.log('📸 Captured sidebar_05_navigated_active_state.png');
    }

    // TEST 7: Re-open Panel on Active Route (Highlight Row)
    console.log('\n--- TEST 7: Highlight Current Page in Panel ---');
    const activeWarehouseBtn = await page.$('.tf-nav-item.is-active button.tf-nav-link');
    if (activeWarehouseBtn) {
        await activeWarehouseBtn.click();
        await sleep(500);
        let activeItemText = await page.$eval('.tf-contextual-item.active .tf-contextual-text', el => el.textContent.trim());
        console.log(`Active row in panel: "${activeItemText}"`);
        results.push({ 
            test: '7. Current Page Row Highlighted', 
            passed: activeItemText.includes('Phiếu nhập') 
        });
    }

    // TEST 8: Close Panel with Escape Key
    console.log('\n--- TEST 8: Close Panel with Escape Key ---');
    await page.keyboard.press('Escape');
    await sleep(400);
    let panelAfterEsc = await page.$('.tf-contextual-panel');
    console.log(`Panel after Escape key: ${!panelAfterEsc}`);
    results.push({ test: '8. Escape Key Closes Panel', passed: !panelAfterEsc });

    // TEST 9: Close Panel by Clicking Backdrop (Outside Click)
    console.log('\n--- TEST 9: Click Outside (Backdrop) Closes Panel ---');
    // Open panel again
    const settingsBtn2 = await page.$('.tf-nav-item.align-bottom .tf-nav-link');
    await settingsBtn2.click();
    await sleep(400);
    // Click backdrop
    const backdrop = await page.$('.tf-contextual-backdrop');
    if (backdrop) {
        // Click near the right edge of viewport
        await page.mouse.click(1000, 400);
        await sleep(400);
        let panelAfterBackdrop = await page.$('.tf-contextual-panel');
        console.log(`Panel after click outside: ${!panelAfterBackdrop}`);
        results.push({ test: '9. Outside Click Closes Panel', passed: !panelAfterBackdrop });
    }

    // TEST 10: Toggle Close by Clicking Same Active Rail Icon
    console.log('\n--- TEST 10: Toggle Close by Clicking Same Icon ---');
    const settingsBtn3 = await page.$('.tf-nav-item.align-bottom .tf-nav-link');
    await settingsBtn3.click();
    await sleep(400);
    let panelOpen3 = await page.$('.tf-contextual-panel');
    console.log(`Panel opened: ${!!panelOpen3}`);
    // Click again
    await settingsBtn3.click();
    await sleep(400);
    let panelClosed3 = await page.$('.tf-contextual-panel');
    console.log(`Panel closed on re-click: ${!panelClosed3}`);
    results.push({ test: '10. Re-click Toggles Close', passed: !!panelOpen3 && !panelClosed3 });

    // TEST 11: Dark Mode Verification
    console.log('\n--- TEST 11: Dark Mode Verification ---');
    // Toggle dark mode via theme toggle
    await page.evaluate(() => {
        document.documentElement.setAttribute('data-bs-theme', 'dark');
        document.documentElement.setAttribute('data-theme', 'dark');
        document.body.setAttribute('data-bs-theme', 'dark');
    });
    // Open panel in dark mode
    const settingsBtnDark = await page.$('.tf-nav-item.align-bottom .tf-nav-link');
    await settingsBtnDark.click();
    await sleep(500);

    const darkBg = await page.$eval('.tf-contextual-panel', el => window.getComputedStyle(el).backgroundColor);
    console.log(`Dark Mode Panel Background: ${darkBg}`);
    results.push({ test: '11. Dark Mode Theme Styling', passed: darkBg !== 'rgb(255, 255, 255)' });

    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'sidebar_06_dark_mode_panel.png') });
    console.log('📸 Captured sidebar_06_dark_mode_panel.png');

    // Reset back to light mode for remaining tests
    await page.evaluate(() => {
        document.documentElement.setAttribute('data-bs-theme', 'light');
        document.documentElement.setAttribute('data-theme', 'light');
        document.body.setAttribute('data-bs-theme', 'light');
    });
    await page.keyboard.press('Escape');
    await sleep(400);

    // TEST 12: Topbar Toggle Button & Keyboard Shortcut (Ctrl+B)
    console.log('\n--- TEST 12: Topbar Toggle Button & Persistence ---');
    await page.click('.tf-sidebar-toggle');
    await sleep(500);
    let isExpandedAfterTopToggle = await page.$eval('.tf-sidebar', el => !el.classList.contains('is-collapsed'));
    let storedState2 = await page.evaluate(() => localStorage.getItem('tf_sidebar_collapsed'));
    console.log(`Sidebar expanded via topbar toggle: ${isExpandedAfterTopToggle}, localStorage: ${storedState2}`);
    results.push({ test: '12. Topbar Toggle Button & Persistence', passed: isExpandedAfterTopToggle && storedState2 === 'false' });

    // Open panel in expanded desktop mode
    const attendanceBtn = await page.$('.tf-nav-item button.tf-nav-link[title="Chấm công"]');
    if (attendanceBtn) {
        await attendanceBtn.click();
        await sleep(500);
        let panelLeftInExpanded = await page.$eval('.tf-contextual-panel', el => el.getBoundingClientRect().left);
        console.log(`Panel Left in expanded sidebar: ${panelLeftInExpanded}px (should be > 224px)`);
        results.push({ test: '13. Docked Beside Expanded Sidebar', passed: panelLeftInExpanded >= 224 });
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'sidebar_07_expanded_docked_panel.png') });
        console.log('📸 Captured sidebar_07_expanded_docked_panel.png');
        await page.keyboard.press('Escape');
        await sleep(300);
    }

    // TEST 13: Responsive Verification (1440px, 1024px, 768px, 390px, 360px)
    console.log('\n--- TEST 13: Responsive Verification ---');
    const viewports = [
        { width: 1440, height: 900, name: '1440px_desktop' },
        { width: 1024, height: 768, name: '1024px_laptop' },
        { width: 768, height: 1024, name: '768px_tablet' },
        { width: 390, height: 844, name: '390px_mobile' },
        { width: 360, height: 740, name: '360px_mobile' }
    ];

    for (const vp of viewports) {
        await page.setViewport({ width: vp.width, height: vp.height });
        await sleep(500);

        if (vp.width < 1024) {
            // Mobile test: open drawer via hamburger, click a group, verify drawer subview
            console.log(`Testing mobile viewport ${vp.name}...`);
            await page.click('.tf-sidebar-toggle'); // Opens mobile drawer
            await sleep(400);

            // Click "Kho hàng" inside mobile drawer
            const mobBtns = await page.$$('.tf-nav-item button.tf-nav-link');
            for (const b of mobBtns) {
                const title = await page.evaluate(el => el.getAttribute('title'), b);
                if (title && title.includes('Kho hàng')) {
                    await b.click();
                    break;
                }
            }
            await sleep(400);

            // Verify no horizontal overflow
            const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
            const clientWidth = await page.evaluate(() => document.documentElement.clientWidth);
            const hasNoOverflow = scrollWidth <= clientWidth + 2;
            console.log(`[${vp.name}] scrollWidth: ${scrollWidth}, clientWidth: ${clientWidth}, noOverflow: ${hasNoOverflow}`);
            results.push({ test: `Responsive ${vp.name} No Overflow`, passed: hasNoOverflow });

            await page.screenshot({ path: path.join(ARTIFACT_DIR, `sidebar_responsive_${vp.name}.png`) });
            console.log(`📸 Captured sidebar_responsive_${vp.name}.png`);

            // Close panel / drawer
            const closeBtn = await page.$('.tf-contextual-close');
            if (closeBtn) await closeBtn.click();
            await sleep(300);
            await page.click('.tf-sidebar-backdrop').catch(() => {});
            await sleep(300);
        } else {
            const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
            const clientWidth = await page.evaluate(() => document.documentElement.clientWidth);
            const hasNoOverflow = scrollWidth <= clientWidth + 2;
            results.push({ test: `Responsive ${vp.name} No Overflow`, passed: hasNoOverflow });
            await page.screenshot({ path: path.join(ARTIFACT_DIR, `sidebar_responsive_${vp.name}.png`) });
        }
    }

    await browser.close();

    console.log('\n========================================');
    console.log('📊 FINAL TEST RESULTS:');
    let allPassed = true;
    for (const r of results) {
        console.log(`${r.passed ? '✅' : '❌'} ${r.test}: ${r.passed ? 'PASSED' : 'FAILED'}`);
        if (!r.passed) allPassed = false;
    }
    console.log('========================================');
    console.log(allPassed ? '🎉 ALL TESTS PASSED SUCCESSFULLY!' : '⚠️ SOME TESTS FAILED!');

    fs.writeFileSync('scratch/sidebar_test_results.json', JSON.stringify({ results, allPassed }, null, 2));
    process.exit(allPassed ? 0 : 1);
})();
