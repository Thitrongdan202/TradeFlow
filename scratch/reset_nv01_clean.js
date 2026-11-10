const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox'] });
    const page = await browser.newPage();
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'admin');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle2' }), page.click('button[type="submit"]')]);

    // Go to dedicated page
    await page.goto('http://127.0.0.1:5169/cai-dat/nguoi-dung/bao-mat', { waitUntil: 'networkidle2' });
    await new Promise(r => setTimeout(r, 2000));

    // Reset nv01 to tradecore123 with uncheck temporary password
    const tempCheck = await page.$('input[type="checkbox"]');
    if (tempCheck) {
        const isChecked = await (await tempCheck.getProperty('checked')).jsonValue();
        if (isChecked) await tempCheck.click();
    }

    const newPassInput = await page.$('#input-new-password');
    if (newPassInput) {
        await newPassInput.click({ clickCount: 3 });
        await newPassInput.type('tradecore123');
    }
    const adminVerifyInput = await page.$('#input-admin-verify-password');
    if (adminVerifyInput) {
        await adminVerifyInput.type('tradecore123');
    }
    const submitBtn = await page.$('#btn-submit-reset-password');
    if (submitBtn) {
        await submitBtn.click();
        await new Promise(r => setTimeout(r, 2000));
        console.log('Reset nv01 back to tradecore123 with MustChangePassword=false');
    }

    await browser.close();
})();
