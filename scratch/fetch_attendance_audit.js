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

        console.log('Logged in as admin, visiting /cham-cong?view=audit...');
        await page.goto('http://127.0.0.1:5169/cham-cong?view=audit', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1500));

        const rows = await page.evaluate(() => {
            const trs = Array.from(document.querySelectorAll('.tf-audit-container table.tf-table tbody tr'));
            return trs.map(tr => Array.from(tr.querySelectorAll('td')).map(td => td.innerText.trim()));
        });

        console.log('Attendance audit rows:', rows.length);
        rows.slice(0, 10).forEach((r, idx) => {
            console.log(`[${idx}]`, JSON.stringify(r));
        });

        await browser.close();
    } catch (e) {
        console.error(e);
    }
})();
