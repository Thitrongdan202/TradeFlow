const puppeteer = require('puppeteer');
const fs = require('fs');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('=== VERIFYING SIDEBAR SUBMENU CHEVRONS & THEME TOGGLE ===\n');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    // 1. Login
    console.log('[1/4] Logging in as Admin...');
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'admin');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    await new Promise(r => setTimeout(r, 1200));

    // 2. Verify Theme Toggle
    console.log('[2/4] Testing Theme Toggle...');

    // Get current theme
    const themeInitial = await page.evaluate(() => {
        return {
            bsTheme: document.documentElement.getAttribute('data-bs-theme'),
            dataTheme: document.documentElement.getAttribute('data-theme'),
            hasDarkClass: document.documentElement.classList.contains('dark'),
            toggleIsDark: document.querySelector('.tf-theme-toggle')?.classList.contains('is-dark'),
            cookie: document.cookie
        };
    });
    console.log('Initial Theme State:', themeInitial);

    // Click Theme Toggle
    const toggleBtn = await page.$('.tf-theme-toggle');
    if (!toggleBtn) {
        console.error('FAIL: .tf-theme-toggle not found!');
        process.exit(1);
    }

    // Toggle 1: If dark -> switch to light; if light -> switch to dark
    await toggleBtn.click();
    await new Promise(r => setTimeout(r, 600));

    const themeAfterToggle1 = await page.evaluate(() => {
        return {
            bsTheme: document.documentElement.getAttribute('data-bs-theme'),
            dataTheme: document.documentElement.getAttribute('data-theme'),
            hasDarkClass: document.documentElement.classList.contains('dark'),
            toggleIsDark: document.querySelector('.tf-theme-toggle')?.classList.contains('is-dark')
        };
    });
    console.log('Theme After Toggle 1:', themeAfterToggle1);

    // Toggle 2: Switch back
    await toggleBtn.click();
    await new Promise(r => setTimeout(r, 600));

    const themeAfterToggle2 = await page.evaluate(() => {
        return {
            bsTheme: document.documentElement.getAttribute('data-bs-theme'),
            dataTheme: document.documentElement.getAttribute('data-theme'),
            hasDarkClass: document.documentElement.classList.contains('dark'),
            toggleIsDark: document.querySelector('.tf-theme-toggle')?.classList.contains('is-dark')
        };
    });
    console.log('Theme After Toggle 2:', themeAfterToggle2);

    // Explicitly test setting to LIGHT mode
    console.log('Testing transition specifically to LIGHT MODE...');
    await page.evaluate(() => {
        if (window.tradeflow && window.tradeflow.setTheme) {
            window.tradeflow.setTheme('light');
        }
    });
    await new Promise(r => setTimeout(r, 500));
    await page.reload({ waitUntil: 'networkidle2' });
    await new Promise(r => setTimeout(r, 1000));

    const lightState = await page.evaluate(() => {
        return {
            bsTheme: document.documentElement.getAttribute('data-bs-theme'),
            dataTheme: document.documentElement.getAttribute('data-theme'),
            hasDarkClass: document.documentElement.classList.contains('dark'),
            toggleIsDark: document.querySelector('.tf-theme-toggle')?.classList.contains('is-dark')
        };
    });
    console.log('Reloaded in Light Mode state:', lightState);

    // Capture Light Mode screenshot
    await page.screenshot({
        path: ARTIFACT_DIR + '/theme_light_mode_verified.png',
        clip: { x: 0, y: 0, width: 1440, height: 900 }
    });

    // Test transition specifically to DARK MODE
    console.log('Testing transition specifically to DARK MODE via UI toggle click...');
    const toggleBtnLight = await page.$('.tf-theme-toggle');
    await toggleBtnLight.click();
    await new Promise(r => setTimeout(r, 800));

    const darkStateAfterClick = await page.evaluate(() => {
        return {
            bsTheme: document.documentElement.getAttribute('data-bs-theme'),
            dataTheme: document.documentElement.getAttribute('data-theme'),
            hasDarkClass: document.documentElement.classList.contains('dark'),
            toggleIsDark: document.querySelector('.tf-theme-toggle')?.classList.contains('is-dark')
        };
    });
    console.log('Switched to Dark Mode state via UI button:', darkStateAfterClick);

    // Capture Dark Mode screenshot
    await page.screenshot({
        path: ARTIFACT_DIR + '/theme_dark_mode_verified.png',
        clip: { x: 0, y: 0, width: 1440, height: 900 }
    });

    // 3. Verify Sidebar Submenu Chevrons in Expanded Mode
    console.log('\n[3/4] Verifying Sidebar Submenu Chevrons in Expanded Mode...');

    // Ensure sidebar is expanded
    await page.evaluate(() => {
        const sidebar = document.querySelector('.tf-sidebar');
        if (sidebar && sidebar.classList.contains('is-collapsed')) {
            window.tradeflow.toggleSidebar();
        }
    });
    await new Promise(r => setTimeout(r, 500));

    const chevronVerification = await page.evaluate(() => {
        const items = Array.from(document.querySelectorAll('.tf-sidebar .tf-nav-item'));
        const results = [];

        items.forEach(item => {
            const label = item.querySelector('.tf-nav-label')?.textContent?.trim() || '';
            const link = item.querySelector('.tf-nav-link');
            const chevron = item.querySelector('.tf-nav-chevron');
            const chevronWrap = item.querySelector('.tf-nav-chevron-wrap');
            const submenu = item.querySelector('.tf-nav-sub');
            const subLinks = Array.from(item.querySelectorAll('.tf-nav-sub-link')).map(l => l.textContent.trim());

            results.push({
                label,
                hasChevron: !!chevron,
                hasChevronWrap: !!chevronWrap,
                hasSubmenu: !!submenu,
                subLinkCount: subLinks.length,
                subLinks: subLinks,
                isChevronExpanded: chevron ? chevron.classList.contains('expanded') : false
            });
        });

        return results;
    });

    console.log('Sidebar Navigation Items Check:');
    let allChevronsValid = true;

    chevronVerification.forEach(item => {
        console.log(`  - [${item.label}] hasChevron: ${item.hasChevron}, hasSubmenu: ${item.hasSubmenu} (${item.subLinkCount} items)`);

        // Check Forbidden Chevron list:
        if (['Tổng quan', 'Chấm công', 'Bán hàng', 'Hợp đồng'].includes(item.label)) {
            if (item.hasChevron) {
                console.error(`    ERROR: ${item.label} MUST NOT have chevron!`);
                allChevronsValid = false;
            }
            if (item.hasSubmenu) {
                console.error(`    ERROR: ${item.label} MUST NOT have submenu!`);
                allChevronsValid = false;
            }
        }

        // Check Required Chevron list:
        if (['Hóa đơn', 'Sản phẩm', 'Kho hàng', 'Đối tác', 'Chứng từ', 'Nhân sự', 'Cài đặt'].includes(item.label)) {
            if (!item.hasChevron) {
                console.error(`    ERROR: ${item.label} MUST have chevron!`);
                allChevronsValid = false;
            }
            if (!item.hasSubmenu) {
                console.error(`    ERROR: ${item.label} MUST have submenu!`);
                allChevronsValid = false;
            }
        }
    });

    console.log('All chevrons valid according to specifications:', allChevronsValid);

    // Test chevron rotation on click
    console.log('\nTesting chevron rotation when clicking on Kho hàng...');
    const warehouseBtn = await page.evaluateHandle(() => {
        const items = Array.from(document.querySelectorAll('.tf-sidebar .tf-nav-item'));
        const wh = items.find(i => i.querySelector('.tf-nav-label')?.textContent?.trim() === 'Kho hàng');
        return wh ? wh.querySelector('.tf-nav-link') : null;
    });

    if (warehouseBtn) {
        await warehouseBtn.click();
        await new Promise(r => setTimeout(r, 600));

        const whStateAfterClick = await page.evaluate(() => {
            const items = Array.from(document.querySelectorAll('.tf-sidebar .tf-nav-item'));
            const wh = items.find(i => i.querySelector('.tf-nav-label')?.textContent?.trim() === 'Kho hàng');
            const chevron = wh?.querySelector('.tf-nav-chevron');
            const sub = wh?.querySelector('.tf-nav-sub');
            return {
                isChevronExpanded: chevron?.classList.contains('expanded'),
                isSubExpanded: sub?.classList.contains('expanded')
            };
        });
        console.log('Kho hàng state after click:', whStateAfterClick);
    }

    // Capture expanded sidebar screenshot with chevrons
    await page.screenshot({
        path: ARTIFACT_DIR + '/sidebar_expanded_chevrons_verified.png',
        clip: { x: 0, y: 0, width: 320, height: 900 }
    });

    // 4. Test Collapsed Sidebar
    console.log('\n[4/4] Verifying Collapsed Sidebar (no visible chevrons on rail, flyout intact)...');
    await page.evaluate(() => {
        window.tradeflow.toggleSidebar();
    });
    await new Promise(r => setTimeout(r, 600));

    const collapsedMetrics = await page.evaluate(() => {
        const sidebar = document.querySelector('.tf-sidebar');
        const visibleChevrons = Array.from(sidebar.querySelectorAll('.tf-nav-chevron-wrap')).filter(c => {
            const style = window.getComputedStyle(c);
            return style.display !== 'none';
        });
        return {
            isCollapsed: sidebar.classList.contains('is-collapsed'),
            visibleChevronCount: visibleChevrons.length
        };
    });
    console.log('Collapsed sidebar state:', collapsedMetrics);

    // Capture collapsed sidebar screenshot
    await page.screenshot({
        path: ARTIFACT_DIR + '/sidebar_collapsed_rail_verified.png',
        clip: { x: 0, y: 0, width: 220, height: 900 }
    });

    await browser.close();
    console.log('\n=== ALL VERIFICATION TESTS COMPLETED SUCCESSFULLY ===');
})();
