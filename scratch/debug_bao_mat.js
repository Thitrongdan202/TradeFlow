const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox'] });
    const page = await browser.newPage();
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'admin');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle2' }), page.click('button[type="submit"]')]);
    
    await page.goto('http://127.0.0.1:5169/cai-dat/nguoi-dung/bao-mat', { waitUntil: 'networkidle2' });
    const alert = await page.evaluate(() => document.querySelector('.tf-alert')?.innerText || 'No alert');
    console.log('Page Alert:', alert);
    
    const selectedUserName = await page.evaluate(() => document.querySelector('.tf-card-body strong[style*="monospace"]')?.innerText || 'None');
    console.log('Selected user on page:', selectedUserName);

    const audits = await page.evaluate(() => Array.from(document.querySelectorAll('table.tf-table tbody tr')).map(r => r.innerText));
    console.log('Audits count:', audits.length);
    console.log('Audit rows sample:', audits.slice(0, 5));

    await browser.close();
})();
