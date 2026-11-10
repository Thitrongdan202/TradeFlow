const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--window-size=1440,900']
    });
    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'admin');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);

    await page.goto('http://127.0.0.1:5169/ban-hang', { waitUntil: 'networkidle2' });
    await page.screenshot({ path: 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901/test_admin_pos.png' });

    const headers = await page.evaluate(() => {
        return Array.from(document.querySelectorAll('header')).map(h => ({
            tag: h.tagName,
            cls: h.className,
            text: h.innerText.substring(0, 100).replace(/\n/g, ' '),
            rect: h.getBoundingClientRect()
        }));
    });
    console.log('Headers on /ban-hang:', JSON.stringify(headers, null, 2));

    await browser.close();
})();
