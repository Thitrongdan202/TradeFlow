const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        defaultViewport: { width: 1440, height: 900 },
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    page.on('console', msg => console.log('PAGE LOG:', msg.text()));
    page.on('pageerror', err => console.log('PAGE ERROR:', err.message));

    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);

    await page.goto('http://127.0.0.1:5169/nhan-vien', { waitUntil: 'networkidle2' });
    await new Promise(r => setTimeout(r, 2000));

    // Click on row 1 (Trần Thị Mai)
    const row1 = (await page.$$('.tf-emp-row'))[1];
    const nameCell = await row1.$('td:nth-child(5)');
    await nameCell.click();
    await new Promise(r => setTimeout(r, 1500));

    // Find and click provision button
    const provBtn = await page.evaluateHandle(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        return btns.find(b => b.innerText.includes('Cấp tài khoản đăng nhập'));
    });
    console.log('Clicking provBtn...');
    await provBtn.click();
    await new Promise(r => setTimeout(r, 2000));

    const modal = await page.$('.tf-modal-dialog');
    console.log('Modal dialog element found:', modal !== null);
    if (modal) {
        console.log('Modal text:', await modal.evaluate(m => m.innerText));
    }

    const inputs = await page.$$eval('input', inputs => inputs.map(i => ({ id: i.id, type: i.type, placeholder: i.placeholder, value: i.value })));
    console.log('Inputs on page:', inputs);

    await browser.close();
})();
