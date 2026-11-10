const puppeteer = require('puppeteer');
(async () => {
    const browser = await puppeteer.launch({ headless: 'new', defaultViewport: { width: 1440, height: 900 } });
    const page = await browser.newPage();
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'admin');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([page.waitForNavigation(), page.click('button[type="submit"]')]);
    await new Promise(r => setTimeout(r, 1000));

    // Collapse sidebar
    await page.click('.tf-nav-collapse-btn');
    await new Promise(r => setTimeout(r, 500));

    // Open Settings panel
    console.log('Clicking Settings...');
    await page.click('.tf-nav-item.align-bottom button.tf-nav-link');
    await new Promise(r => setTimeout(r, 600));

    let panelTitle1 = await page.$eval('.tf-contextual-title', el => el.textContent.trim());
    console.log('Panel 1 Title:', panelTitle1);

    // Now click Kho hàng
    console.log('Clicking Kho hàng...');
    const buttons = await page.$$('.tf-nav-item button.tf-nav-link');
    let warehouseBtn = null;
    for (const b of buttons) {
        const title = await page.evaluate(el => el.getAttribute('title'), b);
        if (title && title.includes('Kho hàng')) {
            warehouseBtn = b;
            break;
        }
    }
    console.log('Found warehouseBtn:', !!warehouseBtn);
    if (warehouseBtn) {
        await warehouseBtn.click();
        await new Promise(r => setTimeout(r, 600));

        const panelExists = await page.$('.tf-contextual-panel');
        console.log('Panel exists after switch:', !!panelExists);
        if (panelExists) {
            const panelTitle2 = await page.$eval('.tf-contextual-title', el => el.textContent.trim());
            console.log('Panel 2 Title:', panelTitle2);
        } else {
            console.log('Panel was closed instead of switched!');
        }
    }

    await browser.close();
})();
