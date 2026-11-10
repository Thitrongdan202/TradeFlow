const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox'] });
    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'admin');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    await new Promise(r => setTimeout(r, 1000));

    await page.goto('http://127.0.0.1:5169/cai-dat/sao-luu', { waitUntil: 'networkidle2' });
    await new Promise(r => setTimeout(r, 1000));

    const metrics = await page.evaluate(() => {
        const sidebar = document.querySelector('.tf-sidebar');
        if (sidebar) sidebar.classList.add('is-collapsed');
        const settingsItem = document.querySelector('.tf-sidebar [data-flyout-group="settings"]');
        window.tradeflowFlyout.open('settings', settingsItem);
        const flyout = document.querySelector('.tf-floating-flyout');
        const fRect = flyout.getBoundingClientRect();
        const sRect = settingsItem.getBoundingClientRect();
        return {
            windowHeight: window.innerHeight,
            settingsTop: sRect.top,
            settingsBottom: sRect.bottom,
            flyoutTop: fRect.top,
            flyoutBottom: fRect.bottom,
            flyoutHeight: fRect.height,
            flyoutStyleTop: flyout.style.top
        };
    });
    console.log(metrics);
    await browser.close();
})();
