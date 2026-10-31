const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900', '--disable-web-security']
    });
    const page = await browser.newPage();
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.waitForSelector('#login-username');
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    console.log('After login URL:', page.url());

    await page.goto('http://127.0.0.1:5169/nhan-vien', { waitUntil: 'networkidle2' });
    console.log('After /nhan-vien URL:', page.url());
    console.log('Title:', await page.title());
    const actions = await page.$('.tf-page-actions');
    console.log('Has .tf-page-actions:', !!actions);
    if (!actions) {
        console.log('Body HTML snippet:', (await page.content()).substring(0, 500));
    }
    await browser.close();
})();
