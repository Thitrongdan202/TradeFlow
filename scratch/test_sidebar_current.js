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

    // Inspect the sidebar menu items
    const navItems = await page.evaluate(() => {
        const buttons = Array.from(document.querySelectorAll('.tf-sidebar .tf-nav-link'));
        return buttons.map(b => ({
            text: b.innerText.trim(),
            hasSub: !!b.parentElement.querySelector('.tf-nav-sub'),
            hasFlyout: !!b.parentElement.querySelector('.tf-nav-flyout'),
            subExpanded: b.parentElement.querySelector('.tf-nav-sub')?.classList.contains('expanded'),
            subComputedDisplay: b.parentElement.querySelector('.tf-nav-sub') ? window.getComputedStyle(b.parentElement.querySelector('.tf-nav-sub')).display : null,
            subComputedHeight: b.parentElement.querySelector('.tf-nav-sub') ? window.getComputedStyle(b.parentElement.querySelector('.tf-nav-sub')).height : null
        }));
    });

    console.log('Sidebar nav items status:');
    console.log(JSON.stringify(navItems, null, 2));

    await browser.close();
})();
