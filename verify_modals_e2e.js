const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const BASE_URL = 'http://127.0.0.1:5169';
const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('🚀 Starting Modal Verification & Visual Capture...');

    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1920,1080']
    });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1920, height: 1080, deviceScaleFactor: 1 });

        // 1. Login
        console.log('1. Logging in...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);

        // 2. Navigate to Employees page
        console.log('2. Navigating to /nhan-vien...');
        await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
        await sleep(1000);

        // 3. Open Add Employee Modal
        console.log('3. Opening Add Employee Modal...');
        await page.click('.tf-page-actions .tf-btn-primary');
        await page.waitForSelector('.employee-modal', { visible: true });
        await sleep(300);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'modal_add_employee_light.png'), fullPage: false });
        console.log('Saved modal_add_employee_light.png');

        // Switch to "Gửi yêu cầu phê duyệt nhân sự" mode
        console.log('3b. Switching form mode to approval proposal...');
        await page.click('.tf-mode-tab:nth-child(2)');
        await sleep(250);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'modal_add_employee_approval_mode.png'), fullPage: false });
        console.log('Saved modal_add_employee_approval_mode.png');

        // Test closing Add Employee Modal
        console.log('3c. Closing Add Employee Modal...');
        await page.click('.employee-modal .tf-modal-close-btn');
        await sleep(50); // capture during exit animation
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'modal_add_employee_closing_anim.png'), fullPage: false });
        await page.waitForSelector('.employee-modal', { hidden: true });
        await sleep(200);

        // 4. Open Request Review Modal
        console.log('4. Opening Request Review Modal...');
        await page.click('.tf-page-actions .tf-btn-outline');
        await page.waitForSelector('.tf-req-body', { visible: true });
        await sleep(300);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'modal_request_review_light.png'), fullPage: false });
        console.log('Saved modal_request_review_light.png');

        // Switch tabs in review modal
        console.log('4b. Switching review tab to "Cần bổ sung hồ sơ"...');
        const tabs = await page.$$('.tf-req-filter-bar .tf-tab-btn');
        if (tabs.length >= 2) {
            await tabs[1].click();
            await sleep(250);
            await page.screenshot({ path: path.join(ARTIFACT_DIR, 'modal_request_review_need_docs_tab.png'), fullPage: false });
            console.log('Saved modal_request_review_need_docs_tab.png');
        }

        // 5. Test Dark Mode
        console.log('5. Toggling Dark Mode...');
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-theme', 'dark');
            localStorage.setItem('tradeflow_theme', 'dark');
        });
        await sleep(300);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'modal_request_review_dark.png'), fullPage: false });
        console.log('Saved modal_request_review_dark.png');

        // Close Request Modal with Escape
        console.log('5b. Closing Request Modal with Escape...');
        await page.keyboard.press('Escape');
        await page.waitForSelector('.tf-req-body', { hidden: true });
        await sleep(200);

        // Open Add Employee Modal in Dark Mode
        console.log('5c. Opening Add Employee Modal in Dark Mode...');
        await page.click('.tf-page-actions .tf-btn-primary');
        await page.waitForSelector('.employee-modal', { visible: true });
        await sleep(300);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'modal_add_employee_dark.png'), fullPage: false });
        console.log('Saved modal_add_employee_dark.png');

        // Close modal
        await page.click('.employee-modal .tf-modal-close-btn');
        await page.waitForSelector('.employee-modal', { hidden: true });
        await sleep(200);

        // Reset theme to light
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-theme', 'light');
            localStorage.setItem('tradeflow_theme', 'light');
        });
        await sleep(200);

        console.log('🎉 Verification complete!');
    } catch (err) {
        console.error('Error during verification:', err);
        process.exitCode = 1;
    } finally {
        await browser.close();
    }
})();
