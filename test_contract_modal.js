const puppeteer = require('puppeteer');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox', '--window-size=1440,900'] });
    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([page.waitForNavigation(), page.click('button[type="submit"]')]);

    await page.goto('http://127.0.0.1:5169/hop-dong?tab=employment', { waitUntil: 'networkidle2' });
    await page.evaluate(() => {
        document.documentElement.setAttribute('data-bs-theme', 'dark');
        document.documentElement.setAttribute('data-theme', 'dark');
    });
    await new Promise(r => setTimeout(r, 1000));

    // Click "Tạo HĐ lao động mới"
    await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        const btn = btns.find(b => b.textContent.includes('Tạo HĐ lao động mới'));
        if (btn) btn.click();
    });
    await new Promise(r => setTimeout(r, 1500));

    // Capture Step 1 in Dark Mode
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'baseline_contract_step1_dark.png') });
    console.log('✓ Captured baseline_contract_step1_dark.png');

    // Click Step 2 tab in modal
    await page.evaluate(() => {
        const modalTabs = Array.from(document.querySelectorAll('.modal .tf-segmented-tab'));
        if (modalTabs.length > 1) modalTabs[1].click();
    });
    await new Promise(r => setTimeout(r, 1000));
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'baseline_contract_step2_dark.png') });
    console.log('✓ Captured baseline_contract_step2_dark.png');

    // Click Step 3 tab in modal
    await page.evaluate(() => {
        const modalTabs = Array.from(document.querySelectorAll('.modal .tf-segmented-tab'));
        if (modalTabs.length > 2) modalTabs[2].click();
    });
    await new Promise(r => setTimeout(r, 1000));
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'baseline_contract_step3_dark.png') });
    console.log('✓ Captured baseline_contract_step3_dark.png');

    // Click Step 4 tab in modal
    await page.evaluate(() => {
        const modalTabs = Array.from(document.querySelectorAll('.modal .tf-segmented-tab'));
        if (modalTabs.length > 3) modalTabs[3].click();
    });
    await new Promise(r => setTimeout(r, 1000));
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'baseline_contract_step4_dark.png') });
    console.log('✓ Captured baseline_contract_step4_dark.png');

    await browser.close();
})();
