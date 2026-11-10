const puppeteer = require('puppeteer');
const path = require('path');

const ARTIFACTS_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function sleep(ms) {
    return new Promise(r => setTimeout(r, ms));
}

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900, deviceScaleFactor: 1 });

        console.log('--- 1. Login as admin (Owner / Admin) ---');
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        await page.type('input[name="Input.Username"]', 'admin');
        await page.type('input[name="Input.Password"]', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        await sleep(1500);

        // Ensure light mode first
        await page.evaluate(() => {
            document.documentElement.removeAttribute('data-bs-theme');
            localStorage.setItem('tradeflow-theme', 'light');
        });
        await sleep(500);

        // Screenshot 1: Header with Owner/Admin (Light Mode, sidebar open)
        console.log('Capturing header_01_owner_admin_light.png...');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_01_owner_admin_light.png`,
            clip: { x: 0, y: 0, width: 1440, height: 180 }
        });

        // Screenshot 6: Full view showing no search bar and clean balanced layout
        console.log('Capturing header_06_no_global_search_layout.png...');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_06_no_global_search_layout.png`,
            fullPage: false
        });

        // Click user menu button to open dropdown
        console.log('Opening Owner/Admin dropdown...');
        await page.click('#tf-user-menu-btn');
        await sleep(600);

        // Screenshot 2: Owner/Admin dropdown open
        console.log('Capturing header_02_owner_admin_dropdown.png...');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_02_owner_admin_dropdown.png`,
            clip: { x: 1000, y: 0, width: 440, height: 460 }
        });

        // Toggle Dark Mode
        console.log('Switching to Dark Mode...');
        await page.click('.tf-dropdown-backdrop'); // close dropdown
        await sleep(400);

        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            localStorage.setItem('tradeflow-theme', 'dark');
        });
        await sleep(600);

        // Screenshot 7: Header with Owner/Admin in Dark Mode
        console.log('Capturing header_07_dark_mode_owner.png...');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_07_dark_mode_owner.png`,
            clip: { x: 0, y: 0, width: 1440, height: 180 }
        });

        // Screenshot 8: Owner/Admin dropdown in Dark Mode
        console.log('Opening Owner/Admin dropdown in Dark Mode...');
        await page.click('#tf-user-menu-btn');
        await sleep(600);
        console.log('Capturing header_08_dark_mode_dropdown.png...');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_08_dark_mode_dropdown.png`,
            clip: { x: 1000, y: 0, width: 440, height: 460 }
        });
        await page.click('.tf-dropdown-backdrop');
        await sleep(400);

        // Screenshot 10: Sidebar collapsed in Dark Mode
        console.log('Collapsing sidebar in Dark Mode...');
        await page.click('.tf-sidebar-toggle');
        await sleep(600);
        console.log('Capturing header_10_sidebar_collapsed_dark.png...');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_10_sidebar_collapsed_dark.png`,
            clip: { x: 0, y: 0, width: 1440, height: 180 }
        });

        // Switch back to Light Mode
        await page.evaluate(() => {
            document.documentElement.removeAttribute('data-bs-theme');
            localStorage.setItem('tradeflow-theme', 'light');
        });
        await sleep(600);

        // Screenshot 9: Sidebar collapsed in Light Mode
        console.log('Capturing header_09_sidebar_collapsed_light.png...');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_09_sidebar_collapsed_light.png`,
            clip: { x: 0, y: 0, width: 1440, height: 180 }
        });

        // Expand sidebar back
        await page.click('.tf-sidebar-toggle');
        await sleep(600);

        // Navigate to Audit Log: /cai-dat/nhat-ky-he-thong
        console.log('Navigating to /cai-dat/nhat-ky-he-thong...');
        await page.goto('http://127.0.0.1:5169/cai-dat/nhat-ky-he-thong', { waitUntil: 'networkidle2' });
        await sleep(1500);

        // Screenshot 5: Audit Log with clean UTF-8 Vietnamese
        console.log('Capturing header_05_audit_log_vietnamese.png...');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_05_audit_log_vietnamese.png`,
            fullPage: false
        });

        // Check audit log row contents
        const auditRows = await page.evaluate(() => {
            const trs = Array.from(document.querySelectorAll('table.tf-table tbody tr'));
            return trs.slice(0, 8).map(tr => Array.from(tr.querySelectorAll('td')).map(td => td.innerText.trim()));
        });
        console.log('Audit Log sample rows:');
        auditRows.forEach((r, idx) => console.log(`Row [${idx}]:`, JSON.stringify(r)));

        // Navigate to Products or Employees to verify module search preserved
        console.log('Navigating to /danh-muc/san-pham to verify module search...');
        await page.goto('http://127.0.0.1:5169/danh-muc/san-pham', { waitUntil: 'networkidle2' });
        await sleep(1500);

        // Screenshot 11: Module search preserved
        console.log('Capturing header_11_module_search_preserved.png...');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_11_module_search_preserved.png`,
            fullPage: false
        });

        // Logout and Login as Employee nv01
        console.log('--- 2. Logout and Login as Employee (nv01) ---');
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        await page.type('input[name="Input.Username"]', 'nv01');
        await page.type('input[name="Input.Password"]', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        await sleep(1500);

        // Screenshot 3: Header with Regular Employee
        console.log('Capturing header_03_employee_light.png...');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_03_employee_light.png`,
            clip: { x: 0, y: 0, width: 1440, height: 180 }
        });

        // Screenshot 4: Employee dropdown open
        console.log('Opening Employee dropdown...');
        await page.click('#tf-user-menu-btn');
        await sleep(600);
        console.log('Capturing header_04_employee_dropdown.png...');
        await page.screenshot({
            path: `${ARTIFACTS_DIR}/header_04_employee_dropdown.png`,
            clip: { x: 1000, y: 0, width: 440, height: 460 }
        });

        console.log('All screenshots captured successfully!');
    } catch (err) {
        console.error('Error during capture:', err);
    } finally {
        await browser.close();
    }
})();
