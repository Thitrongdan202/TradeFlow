const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({ headless: 'new', args: ['--ignore-certificate-errors'] });
    const page = await browser.newPage();
    try {
        await page.goto('https://localhost:7185/Account/Login', { waitUntil: 'domcontentloaded' });
        await page.type('input[name="Input.Username"]', 'admin');
        await page.type('input[name="Input.Password"]', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'domcontentloaded' }),
            page.click('button[type="submit"]')
        ]);
        await new Promise(r => setTimeout(r, 2000));
        
        const isErrorVisible = await page.evaluate(() => {
            const el = document.getElementById('blazor-error-ui');
            return el && window.getComputedStyle(el).display !== 'none';
        });
        
        console.log('Error UI visible:', isErrorVisible);
    } finally {
        await browser.close();
    }
})();
