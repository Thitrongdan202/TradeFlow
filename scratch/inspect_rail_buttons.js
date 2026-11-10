const puppeteer = require('puppeteer');
(async () => {
    const browser = await puppeteer.launch({ headless: 'new', defaultViewport: { width: 1440, height: 900 } });
    const page = await browser.newPage();
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'admin');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([page.waitForNavigation(), page.click('button[type="submit"]')]);
    await new Promise(r => setTimeout(r, 1000));

    const btns = await page.$$eval('.tf-nav-item button.tf-nav-link', els => els.map(e => ({ title: e.getAttribute('title'), text: e.textContent.trim() })));
    console.log('Buttons:', btns);
    await browser.close();
})();
