const puppeteer = require('puppeteer');

const ARTIFACTS_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function sleep(ms) {
    return new Promise(r => setTimeout(r, ms));
}

async function setTheme(page, theme) {
    await page.evaluate((th) => {
        document.cookie = `tf_theme=${th}; path=/; max-age=31536000`;
        document.cookie = `tradeflow_theme=${th}; path=/; max-age=31536000`;
        localStorage.setItem('tradeflow-theme', th);
        localStorage.setItem('tf_theme', th);
        if (th === 'dark') {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.documentElement.setAttribute('data-theme', 'dark');
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.setAttribute('data-bs-theme', 'light');
            document.documentElement.setAttribute('data-theme', 'light');
            document.documentElement.classList.remove('dark');
        }
    }, theme);
    await sleep(400);
}

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900, deviceScaleFactor: 1 });

        // Set light theme initially
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        await setTheme(page, 'light');

        console.log('--- 1. Login as admin (Owner / Admin) ---');
        await page.type('input[name="Input.Username"]', 'admin');
        await page.type('input[name="Input.Password"]', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        await sleep(1000);
        await setTheme(page, 'light');
        await page.reload({ waitUntil: 'networkidle2' });
        await sleep(1200);

        // 1. Header with Owner/Admin (Light Mode, sidebar open)
        console.log('1. header_01_owner_admin_light.png');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_01_owner_admin_light.png`,
            clip: { x: 0, y: 0, width: 1440, height: 180 }
        });

        // 6. Header without global search - full layout
        console.log('6. header_06_no_global_search_layout.png');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_06_no_global_search_layout.png`,
            fullPage: false
        });

        // 2. Owner/Admin Dropdown in Light Mode
        console.log('2. header_02_owner_admin_dropdown.png');
        await page.click('#tf-user-menu-btn');
        await sleep(600);
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_02_owner_admin_dropdown.png`,
            clip: { x: 1000, y: 0, width: 440, height: 460 }
        });
        await page.click('.tf-dropdown-backdrop');
        await sleep(400);

        // 9. Sidebar Collapsed in Light Mode
        console.log('9. header_09_sidebar_collapsed_light.png');
        await page.click('.tf-sidebar-toggle');
        await sleep(600);
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_09_sidebar_collapsed_light.png`,
            clip: { x: 0, y: 0, width: 1440, height: 180 }
        });

        // Expand sidebar back
        await page.click('.tf-sidebar-toggle');
        await sleep(600);

        // 7. Dark Mode - Toggle theme
        console.log('7. Switching to Dark Mode...');
        await setTheme(page, 'dark');
        await page.reload({ waitUntil: 'networkidle2' });
        await sleep(1200);

        console.log('Capturing header_07_dark_mode_owner.png');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_07_dark_mode_owner.png`,
            clip: { x: 0, y: 0, width: 1440, height: 180 }
        });

        // 8. Dark Mode Dropdown
        console.log('8. header_08_dark_mode_dropdown.png');
        await page.click('#tf-user-menu-btn');
        await sleep(600);
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_08_dark_mode_dropdown.png`,
            clip: { x: 1000, y: 0, width: 440, height: 460 }
        });
        await page.click('.tf-dropdown-backdrop');
        await sleep(400);

        // 10. Sidebar Collapsed in Dark Mode
        console.log('10. header_10_sidebar_collapsed_dark.png');
        await page.click('.tf-sidebar-toggle');
        await sleep(600);
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_10_sidebar_collapsed_dark.png`,
            clip: { x: 0, y: 0, width: 1440, height: 180 }
        });
        await page.click('.tf-sidebar-toggle');
        await sleep(600);

        // Switch back to Light Mode for Audit Log
        await setTheme(page, 'light');

        // 5. Audit Log in Light Mode
        console.log('5. Navigating to /cai-dat/nhat-ky-he-thong...');
        await page.goto('http://127.0.0.1:5169/cai-dat/nhat-ky-he-thong', { waitUntil: 'networkidle2' });
        await sleep(1500);

        console.log('Capturing header_05_audit_log_vietnamese.png');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_05_audit_log_vietnamese.png`,
            fullPage: false
        });

        // Also capture Audit Log in Dark Mode
        await setTheme(page, 'dark');
        await page.reload({ waitUntil: 'networkidle2' });
        await sleep(1200);
        console.log('Capturing header_05_audit_log_dark.png');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_05_audit_log_dark.png`,
            fullPage: false
        });

        // 11. Module search preserved
        await setTheme(page, 'light');
        console.log('11. Navigating to /danh-muc/san-pham to verify module search...');
        await page.goto('http://127.0.0.1:5169/danh-muc/san-pham', { waitUntil: 'networkidle2' });
        await sleep(1500);
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_11_module_search_preserved.png`,
            fullPage: false
        });

        // 3 & 4. Logout and Login as Employee nv01
        console.log('--- 2. Login as Employee nv01 ---');
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        await setTheme(page, 'light');
        await page.type('input[name="Input.Username"]', 'nv01');
        await page.type('input[name="Input.Password"]', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        await sleep(1200);

        console.log('3. header_03_employee_light.png');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_03_employee_light.png`,
            clip: { x: 0, y: 0, width: 1440, height: 180 }
        });

        console.log('4. header_04_employee_dropdown.png');
        await page.click('#tf-user-menu-btn');
        await sleep(600);
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_04_employee_dropdown.png`,
            clip: { x: 1000, y: 0, width: 440, height: 460 }
        });

        console.log('All verification captures completed successfully!');
    } catch (e) {
        console.error('Error during capture:', e);
    } finally {
        await browser.close();
    }
})();
