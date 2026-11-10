const puppeteer = require('puppeteer');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('Testing Lock & Unlock for nv01...');
    const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox', '--window-size=1440,900'] });
    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'admin');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle2' }), page.click('button[type="submit"]')]);

    // Go to dedicated page
    await page.goto('http://127.0.0.1:5169/cai-dat/nguoi-dung/bao-mat', { waitUntil: 'networkidle2' });
    await delay(2000);

    const selectedName = await page.evaluate(() => document.querySelector('.tf-card-body strong[style*="monospace"]')?.innerText || 'None');
    console.log('Currently selected user:', selectedName);

    // Click lock button
    const lockBtn = await page.$('#btn-lock-user');
    console.log('lockBtn found:', !!lockBtn);
    if (lockBtn) {
        await lockBtn.click();
        await delay(2000);
        console.log('Clicked #btn-lock-user');
    }

    // Try login as nv01 in second page
    const lockPage = await browser.newPage();
    await lockPage.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await lockPage.type('input[name="Input.Username"]', 'nv01');
    await lockPage.type('input[name="Input.Password"]', 'TradeFlow@2026!Nv01');
    await lockPage.click('button[type="submit"]');
    await delay(1200);

    const lockErrorMsg = await lockPage.evaluate(() => {
        return document.querySelector('.tf-login-alert, .alert, .text-danger, .validation-message')?.innerText || '';
    });
    console.log('Lock error message on login attempt:', lockErrorMsg);
    await lockPage.screenshot({ path: `${ARTIFACT_DIR}/root_07_account_lock_prevention.png` });
    console.log('Saved root_07_account_lock_prevention.png');
    await lockPage.close();

    // Now unlock nv01
    const unlockBtn = await page.$('#btn-unlock-user');
    console.log('unlockBtn found:', !!unlockBtn);
    if (unlockBtn) {
        await unlockBtn.click();
        await delay(1500);
        console.log('Clicked #btn-unlock-user');
    }

    // Now capture root_08_security_audit_log.png
    await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
    await delay(1000);
    const audits = await page.evaluate(() => Array.from(document.querySelectorAll('table.tf-table tbody tr')).slice(0, 5).map(r => r.innerText));
    console.log('Audits on page:', audits);
    await page.screenshot({ path: `${ARTIFACT_DIR}/root_08_security_audit_log.png` });
    console.log('Saved root_08_security_audit_log.png');

    await browser.close();
    console.log('DONE!');
})();
