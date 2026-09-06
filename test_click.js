const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({ headless: 'new', args: ['--ignore-certificate-errors'] });
    const page = await browser.newPage();
    const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
    
    try {
        await page.goto('https://localhost:7185/Account/Login', { waitUntil: 'domcontentloaded' });
        await page.type('input[name="Input.Username"]', 'admin');
        await page.type('input[name="Input.Password"]', 'tradecore123');
        
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'domcontentloaded' }),
            page.click('button[type="submit"]')
        ]);
        
        await wait(2000);
        
        console.log(await page.evaluate(() => document.querySelector('.tf-sidebar-inner').innerHTML));
        
    } catch (e) {
        console.error("Error:", e);
    } finally {
        await browser.close();
    }
})();
