const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--window-size=1440,900']
    });
    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    // Login as nv01
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'nv01');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    console.log('nv01 logged in. Current URL:', page.url());

    // Check sidebar visible items for nv01
    const sidebarItems = await page.evaluate(() => {
        return Array.from(document.querySelectorAll('.tf-nav-item')).map(it => {
            const label = it.querySelector('.tf-nav-label')?.textContent.trim();
            const links = Array.from(it.querySelectorAll('.tf-nav-sub-link, .tf-flyout-link')).map(l => l.textContent.trim());
            return { label, links };
        });
    });
    console.log('Sidebar items visible to nv01:', JSON.stringify(sidebarItems, null, 2));

    // Try going to /ban-hang
    await page.goto('http://127.0.0.1:5169/ban-hang', { waitUntil: 'networkidle2' });
    console.log('nv01 URL on /ban-hang:', page.url());
    const bodyText = await page.evaluate(() => document.body.innerText.substring(0, 400));
    console.log('nv01 text on /ban-hang:\n', bodyText);

    // Take screenshot of nv01 on /ban-hang
    await page.screenshot({ path: 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901/test_nv01_pos.png' });

    await browser.close();
})();
