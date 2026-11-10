const puppeteer = require('puppeteer');

(async () => {
    try {
        const browser = await puppeteer.launch({
            headless: 'new',
            args: ['--no-sandbox', '--disable-setuid-sandbox']
        });
        const page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });

        // Login as admin
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        await page.type('input[name="Input.Username"]', 'admin');
        await page.type('input[name="Input.Password"]', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);

        console.log('Logged in as admin, current URL:', page.url());

        // Go to audit log
        await page.goto('http://127.0.0.1:5169/cai-dat/nhat-ky-he-thong', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1500));

        const rows = await page.evaluate(() => {
            const trs = Array.from(document.querySelectorAll('table.tf-table tbody tr'));
            return trs.map(tr => Array.from(tr.querySelectorAll('td')).map(td => td.innerText.trim()));
        });

        console.log('Found', rows.length, 'audit rows:');
        rows.slice(0, 15).forEach((r, idx) => {
            console.log(`[${idx}]`, JSON.stringify(r));
        });

        await browser.close();
    } catch (e) {
        console.error(e);
    }
})();
