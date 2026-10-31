const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox'] });
    const page = await browser.newPage();
    await page.goto('http://127.0.0.1:5169/Account/Login');
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([page.waitForNavigation(), page.click('button[type="submit"]')]);
    
    await page.goto('http://127.0.0.1:5169/nhan-vien');
    const text = await page.evaluate(() => document.body.innerText);
    console.log('=== BODY TEXT ===');
    console.log(text);
    await browser.close();
})();
