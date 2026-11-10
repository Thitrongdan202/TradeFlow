const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1600,1000']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1600, height: 1000 });

    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);

    // Navigate to /ban-hang/don-hang to see sales orders list
    await page.goto('http://127.0.0.1:5169/ban-hang/don-hang', { waitUntil: 'networkidle2' });
    await new Promise(r => setTimeout(r, 2000));

    const orders = await page.evaluate(() => {
        const rows = Array.from(document.querySelectorAll('tbody tr'));
        return rows.map(r => {
            const cells = Array.from(r.querySelectorAll('td')).map(c => c.innerText.trim());
            return cells.slice(0, 6).join(' | ');
        });
    });

    console.log('Sales orders in system:', orders);

    await browser.close();
})();
