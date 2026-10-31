const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({ headless: 'new' });
    const page = await browser.newPage();
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
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
    const text = await page.evaluate(() => document.body.innerText.slice(0, 500));
    console.log('Body snippet:', text);
    const html = await page.evaluate(() => document.body.innerHTML.slice(0, 500));
    console.log('HTML snippet:', html);

    await browser.close();
})();
