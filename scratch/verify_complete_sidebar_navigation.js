const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('=== RUNNING COMPREHENSIVE RECOVERY & HOVER NAVIGATION TEST SUITE ===');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    // Login as Admin
    console.log('[1/7] Logging in as Admin...');
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'admin');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    await new Promise(r => setTimeout(r, 1000));

    const modules = [
        { group: 'attendance', label: 'Chấm công', minSubmenus: 5 },
        { group: 'sales', label: 'Bán hàng', minSubmenus: 3 },
        { group: 'invoices', label: 'Hóa đơn', minSubmenus: 3 },
        { group: 'catalog', label: 'Sản phẩm', minSubmenus: 3 },
        { group: 'warehouse', label: 'Kho hàng', minSubmenus: 7 },
        { group: 'partners', label: 'Đối tác', minSubmenus: 3 },
        { group: 'contracts', label: 'Hợp đồng', minSubmenus: 3 },
        { group: 'documents', label: 'Chứng từ', minSubmenus: 2 },
        { group: 'employees', label: 'Nhân sự', minSubmenus: 2 },
        { group: 'settings', label: 'Cài đặt', minSubmenus: 9 }
    ];

    // TEST 1: Collapsed Mode Hover Flyout Verification for ALL modules
    console.log('[2/7] Testing Collapsed Mode Hover Flyout for all modules...');
    await page.evaluate(() => {
        const sidebar = document.querySelector('.tf-sidebar');
        const main = document.querySelector('.tf-main-wrapper');
        if (sidebar && !sidebar.classList.contains('is-collapsed')) {
            sidebar.classList.add('is-collapsed');
            if (main) main.classList.add('is-collapsed');
        }
        document.documentElement.setAttribute('data-bs-theme', 'dark');
        document.documentElement.setAttribute('data-theme', 'dark');
        document.documentElement.classList.add('dark');
    });
    await new Promise(r => setTimeout(r, 400));

    let collapsedPassCount = 0;
    for (const mod of modules) {
        const item = await page.$(`.tf-sidebar [data-flyout-group="${mod.group}"]`);
        if (!item) {
            console.error(`  FAIL: Item not found for group: ${mod.group}`);
            continue;
        }

        const box = await item.boundingBox();
        await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
        await new Promise(r => setTimeout(r, 300));

        const flyoutState = await page.evaluate((grp) => {
            const flyout = document.querySelector('.tf-floating-flyout');
            if (!flyout) return { isVisible: false, subCount: 0 };
            const isVisible = flyout.classList.contains('is-visible');
            const section = flyout.querySelector(`.tf-flyout-section[data-section="${grp}"]`);
            const isSectionShown = section && window.getComputedStyle(section).display !== 'none';
            const links = section ? Array.from(section.querySelectorAll('.tf-flyout-link')) : [];
            const rect = flyout.getBoundingClientRect();
            return {
                isVisible: isVisible && isSectionShown,
                subCount: links.length,
                subTexts: links.map(l => l.innerText.trim()),
                left: rect.left,
                top: rect.top,
                bottom: rect.bottom,
                windowHeight: window.innerHeight
            };
        }, mod.group);

        if (flyoutState.isVisible && flyoutState.subCount >= mod.minSubmenus) {
            console.log(`  ✓ Collapsed Hover [${mod.label}] -> Flyout visible, ${flyoutState.subCount} submenus, left: ${flyoutState.left}px, top: ${flyoutState.top}px`);
            collapsedPassCount++;
        } else {
            console.error(`  FAIL: Collapsed Hover [${mod.label}] -> visible=${flyoutState.isVisible}, subs=${flyoutState.subCount}`);
        }
    }

    // Capture Collapsed Dark Screenshots
    console.log('[3/7] Capturing Collapsed Dark Screenshots...');
    // Settings Flyout
    const settingsItem = await page.$('.tf-sidebar [data-flyout-group="settings"]');
    if (settingsItem) {
        const box = await settingsItem.boundingBox();
        await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
        await new Promise(r => setTimeout(r, 400));
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'sidebar_recovery_collapsed_dark_settings.png'),
            clip: { x: 0, y: 0, width: 450, height: 750 }
        });
    }

    // Warehouse Flyout
    const warehouseItem = await page.$('.tf-sidebar [data-flyout-group="warehouse"]');
    if (warehouseItem) {
        const box = await warehouseItem.boundingBox();
        await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
        await new Promise(r => setTimeout(r, 400));
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'sidebar_recovery_collapsed_dark_warehouse.png'),
            clip: { x: 0, y: 0, width: 450, height: 750 }
        });
    }

    // TEST 2: Expanded Mode Verification
    console.log('[4/7] Testing Expanded Mode Hover Flyout for all modules...');
    // Expand sidebar
    await page.evaluate(() => {
        const sidebar = document.querySelector('.tf-sidebar');
        const main = document.querySelector('.tf-main-wrapper');
        if (sidebar && sidebar.classList.contains('is-collapsed')) {
            sidebar.classList.remove('is-collapsed');
            if (main) main.classList.remove('is-collapsed');
        }
    });
    await new Promise(r => setTimeout(r, 500));

    let expandedPassCount = 0;
    for (const mod of modules) {
        const item = await page.$(`.tf-sidebar [data-flyout-group="${mod.group}"]`);
        if (!item) {
            console.error(`  FAIL: Expanded item not found for group: ${mod.group}`);
            continue;
        }

        const box = await item.boundingBox();
        await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
        await new Promise(r => setTimeout(r, 300));

        const flyoutState = await page.evaluate((grp) => {
            const flyout = document.querySelector('.tf-floating-flyout');
            if (!flyout) return { isVisible: false, subCount: 0 };
            const isVisible = flyout.classList.contains('is-visible');
            const section = flyout.querySelector(`.tf-flyout-section[data-section="${grp}"]`);
            const isSectionShown = section && window.getComputedStyle(section).display !== 'none';
            const links = section ? Array.from(section.querySelectorAll('.tf-flyout-link')) : [];
            const rect = flyout.getBoundingClientRect();
            return {
                isVisible: isVisible && isSectionShown,
                subCount: links.length,
                subTexts: links.map(l => l.innerText.trim()),
                left: rect.left,
                top: rect.top,
                bottom: rect.bottom,
                windowHeight: window.innerHeight
            };
        }, mod.group);

        if (flyoutState.isVisible && flyoutState.subCount >= mod.minSubmenus && flyoutState.left >= 220) {
            console.log(`  ✓ Expanded Hover [${mod.label}] -> Flyout visible at left: ${flyoutState.left}px, ${flyoutState.subCount} submenus`);
            expandedPassCount++;
        } else {
            console.error(`  FAIL: Expanded Hover [${mod.label}] -> visible=${flyoutState.isVisible}, subs=${flyoutState.subCount}, left=${flyoutState.left}`);
        }
    }

    // Capture Expanded Dark Screenshots
    console.log('[5/7] Capturing Expanded Dark Screenshots...');
    // Move away to capture clean expanded sidebar
    await page.mouse.move(700, 300);
    await new Promise(r => setTimeout(r, 400));
    await page.screenshot({
        path: path.join(ARTIFACT_DIR, 'sidebar_recovery_expanded_dark_clean.png'),
        clip: { x: 0, y: 0, width: 350, height: 750 }
    });

    // Hover Settings in Expanded
    const expSettingsItem = await page.$('.tf-sidebar [data-flyout-group="settings"]');
    if (expSettingsItem) {
        const box = await expSettingsItem.boundingBox();
        await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
        await new Promise(r => setTimeout(r, 400));
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'sidebar_recovery_expanded_dark_settings.png'),
            clip: { x: 0, y: 0, width: 550, height: 750 }
        });
    }

    // TEST 3: Light Mode Testing & Screenshots
    console.log('[6/7] Testing Light Mode...');
    await page.evaluate(() => {
        document.documentElement.setAttribute('data-bs-theme', 'light');
        document.documentElement.setAttribute('data-theme', 'light');
        document.documentElement.classList.remove('dark');
        document.cookie = 'tf_theme=light;path=/;max-age=31536000';
        document.cookie = 'tradeflow_theme=light;path=/;max-age=31536000';
    });
    await new Promise(r => setTimeout(r, 500));

    // Hover Warehouse in Expanded Light
    const expWarehouseItem = await page.$('.tf-sidebar [data-flyout-group="warehouse"]');
    if (expWarehouseItem) {
        const box = await expWarehouseItem.boundingBox();
        await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
        await new Promise(r => setTimeout(r, 400));
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'sidebar_recovery_expanded_light_warehouse.png'),
            clip: { x: 0, y: 0, width: 550, height: 750 }
        });
    }

    // Collapse to test Collapsed Light Settings
    await page.evaluate(() => {
        const sidebar = document.querySelector('.tf-sidebar');
        const main = document.querySelector('.tf-main-wrapper');
        if (sidebar) sidebar.classList.add('is-collapsed');
        if (main) main.classList.add('is-collapsed');
    });
    await new Promise(r => setTimeout(r, 400));

    const lightSettingsItem = await page.$('.tf-sidebar [data-flyout-group="settings"]');
    if (lightSettingsItem) {
        const box = await lightSettingsItem.boundingBox();
        await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
        await new Promise(r => setTimeout(r, 400));
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'sidebar_recovery_collapsed_light_settings.png'),
            clip: { x: 0, y: 0, width: 450, height: 750 }
        });
    }

    // TEST 4: Navigation through Flyout Link Click
    console.log('[7/7] Testing navigation via Flyout link click...');
    const linkClicked = await page.evaluate(() => {
        const flyout = document.querySelector('.tf-floating-flyout');
        const rbacLink = flyout ? flyout.querySelector('a[href="/cai-dat/phan-quyen"]') : null;
        if (rbacLink) {
            rbacLink.click();
            return true;
        }
        return false;
    });

    if (linkClicked) {
        await new Promise(r => setTimeout(r, 1200));
        const currentUrl = page.url();
        console.log(`  ✓ Submenu Link Click Navigation URL: ${currentUrl}`);
    }

    console.log('\n================ SUMMARY ================');
    console.log(`Collapsed Hover Tests Passed: ${collapsedPassCount}/${modules.length}`);
    console.log(`Expanded Hover Tests Passed:  ${expandedPassCount}/${modules.length}`);
    console.log('=========================================');

    await browser.close();
})();
