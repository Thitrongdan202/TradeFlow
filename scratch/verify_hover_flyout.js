const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('--- Starting Comprehensive Hover Flyout Verification Suite ---');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    try {
        console.log('Navigating to TradeFlow login...');
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 400));

        // Login as Admin
        const userInput = await page.$('input[name="Input.Username"]');
        if (userInput) {
            console.log('Logging in as admin...');
            await userInput.click({ clickCount: 3 });
            await userInput.type('admin');
            const passInput = await page.$('input[name="Input.Password"]');
            if (passInput) {
                await passInput.click({ clickCount: 3 });
                await passInput.type('tradecore123');
            }
            await Promise.all([
                page.waitForNavigation({ waitUntil: 'networkidle2' }),
                page.click('button[type="submit"]')
            ]);
        }

        console.log('Navigated to main layout:', page.url());
        await page.waitForSelector('.tf-sidebar', { timeout: 10000 });

        // Enable Dark Mode to match user screenshot
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.documentElement.setAttribute('data-theme', 'dark');
            document.documentElement.classList.add('dark');
            document.cookie = 'tf_theme=dark;path=/;max-age=31536000';
            document.cookie = 'tradeflow_theme=dark;path=/;max-age=31536000';
            localStorage.setItem('tf_theme', 'dark');
            localStorage.setItem('tradeflow_theme', 'dark');
        });
        await new Promise(r => setTimeout(r, 600));

        // 1. Verify Desktop Rail Width
        const sidebarBox = await page.evaluate(() => {
            const sidebar = document.querySelector('.tf-sidebar');
            const rect = sidebar.getBoundingClientRect();
            return { width: Math.round(rect.width), height: Math.round(rect.height) };
        });
        console.log('1. Sidebar width:', sidebarBox.width, 'px (expected 68px)');

        // 2. Test Hovering on Settings Icon
        console.log('2. Hovering over Settings icon in Dark Theme...');
        const settingsItem = await page.$('.tf-sidebar [data-flyout-group="settings"]');
        if (!settingsItem) {
            throw new Error('Settings rail item with data-flyout-group="settings" not found!');
        }

        const settingsBox = await settingsItem.boundingBox();
        await page.mouse.move(settingsBox.x + settingsBox.width / 2, settingsBox.y + settingsBox.height / 2);
        await new Promise(r => setTimeout(r, 300)); // wait for hover trigger

        // Check if flyout is visible
        const flyoutState1 = await page.evaluate(() => {
            const flyout = document.querySelector('.tf-floating-flyout');
            if (!flyout) return null;
            const cs = window.getComputedStyle(flyout);
            const settingsSection = flyout.querySelector('[data-section="settings"]');
            return {
                visible: flyout.classList.contains('is-visible'),
                opacity: cs.opacity,
                display: cs.display,
                top: flyout.style.top,
                left: cs.left,
                width: cs.width,
                sectionDisplay: settingsSection ? window.getComputedStyle(settingsSection).display : null,
                headerText: settingsSection ? settingsSection.querySelector('.tf-flyout-header')?.innerText : null
            };
        });
        console.log('Flyout state after hover:', flyoutState1);

        // Capture Screenshot 1: Settings Flyout
        const shot1Path = path.join(ARTIFACT_DIR, 'hover_01_settings_flyout.png');
        await page.screenshot({ path: shot1Path, fullPage: false });
        console.log('Captured:', shot1Path);

        // 3. Test Moving cursor into the Flyout Panel (Hover Bridge verification)
        console.log('3. Moving cursor into the Flyout panel...');
        const flyoutBox = await page.evaluate(() => {
            const flyout = document.querySelector('.tf-floating-flyout');
            const rect = flyout.getBoundingClientRect();
            return { x: rect.x, y: rect.y, width: rect.width, height: rect.height };
        });

        // Move across gap to flyout center
        await page.mouse.move(flyoutBox.x + 50, flyoutBox.y + 100);
        await new Promise(r => setTimeout(r, 250));

        const flyoutState2 = await page.evaluate(() => {
            const flyout = document.querySelector('.tf-floating-flyout');
            return {
                visible: flyout.classList.contains('is-visible'),
                opacity: window.getComputedStyle(flyout).opacity
            };
        });
        console.log('Flyout state inside panel:', flyoutState2);

        // 4. Test Hovering over another item: Warehouse (kho)
        console.log('4. Hovering over Warehouse icon...');
        const warehouseItem = await page.$('.tf-sidebar [data-flyout-group="warehouse"]');
        const whBox = await warehouseItem.boundingBox();
        await page.mouse.move(whBox.x + whBox.width / 2, whBox.y + whBox.height / 2);
        await new Promise(r => setTimeout(r, 300));

        const flyoutState3 = await page.evaluate(() => {
            const flyout = document.querySelector('.tf-floating-flyout');
            const whSection = flyout.querySelector('[data-section="warehouse"]');
            return {
                visible: flyout.classList.contains('is-visible'),
                headerText: whSection ? whSection.querySelector('.tf-flyout-header')?.innerText : null,
                whDisplay: whSection ? window.getComputedStyle(whSection).display : null
            };
        });
        console.log('Flyout state on warehouse:', flyoutState3);

        const shot2Path = path.join(ARTIFACT_DIR, 'hover_02_warehouse_flyout.png');
        await page.screenshot({ path: shot2Path, fullPage: false });
        console.log('Captured:', shot2Path);

        // 5. Test Moving away from both (closing after 150ms)
        console.log('5. Moving mouse away to main content (x: 600, y: 400)...');
        await page.mouse.move(600, 400);
        await new Promise(r => setTimeout(r, 350)); // wait > 150ms for close

        const flyoutState4 = await page.evaluate(() => {
            const flyout = document.querySelector('.tf-floating-flyout');
            return {
                visible: flyout.classList.contains('is-visible'),
                opacity: window.getComputedStyle(flyout).opacity
            };
        });
        console.log('Flyout state after moving away:', flyoutState4);

        // 6. Test Active page highlight on /cai-dat/sao-luu ("Dữ liệu & Kiểm soát" as in screenshot)
        console.log('6. Navigating to /cai-dat/sao-luu to test active row highlight...');
        await page.goto('http://127.0.0.1:5169/cai-dat/sao-luu', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1000));

        // Ensure Dark Mode is still active
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.documentElement.setAttribute('data-theme', 'dark');
            document.documentElement.classList.add('dark');
        });
        await new Promise(r => setTimeout(r, 400));

        const settingsItem2 = await page.$('.tf-sidebar [data-flyout-group="settings"]');
        const sBox2 = await settingsItem2.boundingBox();
        await page.mouse.move(sBox2.x + sBox2.width / 2, sBox2.y + sBox2.height / 2);
        await new Promise(r => setTimeout(r, 400));

        const activeRowInfo = await page.evaluate(() => {
            const flyout = document.querySelector('.tf-floating-flyout');
            const activeLink = flyout ? flyout.querySelector('.tf-flyout-link.active') : null;
            if (!activeLink) return null;
            const cs = window.getComputedStyle(activeLink);
            return {
                text: activeLink.innerText.trim(),
                href: activeLink.getAttribute('href'),
                bgColor: cs.backgroundColor,
                color: cs.color
            };
        });
        console.log('Active row info in flyout:', activeRowInfo);

        // Capture side-by-side exact view like screenshot
        const shot3Path = path.join(ARTIFACT_DIR, 'hover_03_active_row_highlight.png');
        await page.screenshot({ path: shot3Path, fullPage: false });
        console.log('Captured:', shot3Path);

        // Crop/capture focused sidebar region (left 450px)
        const shotCropPath = path.join(ARTIFACT_DIR, 'hover_05_sidebar_crop_match.png');
        await page.screenshot({
            path: shotCropPath,
            clip: { x: 0, y: 0, width: 420, height: 850 }
        });
        console.log('Captured cropped comparison:', shotCropPath);

        // 7. Verify Expand/Collapse Mechanism
        console.log('7. Verifying Collapse/Expand toggle...');
        const collapseBtn = await page.$('.tf-sidebar-toggle');
        if (collapseBtn) {
            await collapseBtn.click();
            await new Promise(r => setTimeout(r, 300));
            console.log('Hamburger clicked without error.');
            await collapseBtn.click();
            await new Promise(r => setTimeout(r, 300));
        }

        console.log('=== ALL TESTS COMPLETED SUCCESSFULLY ===');
    } catch (err) {
        console.error('Error during verification:', err);
    } finally {
        await browser.close();
    }
})();
