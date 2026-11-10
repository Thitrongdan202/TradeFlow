const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    const userInput = await page.$('input[name="Input.Username"]');
    if (userInput) {
        await userInput.click({ clickCount: 3 });
        await userInput.type('admin');
        const passInput = await page.$('input[name="Input.Password"]');
        await passInput.click({ clickCount: 3 });
        await passInput.type('tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
    }

    await page.goto('http://127.0.0.1:5169/', { waitUntil: 'networkidle2' });
    await page.waitForSelector('.tf-chart-svg');

    // Monitor KPI and Top Products
    await page.evaluate(() => {
        window.__kpiMutations = [];
        const kpiGrid = document.querySelector('.tf-dash-kpi-grid');
        if (kpiGrid) {
            new MutationObserver(() => {
                const hasKpiLoading = !!kpiGrid.querySelector('.is-loading');
                window.__kpiMutations.push({ time: Date.now(), hasKpiLoading });
            }).observe(kpiGrid, { childList: true, subtree: true });
        }
    });

    const buttons = await page.$$('.tf-dash-pill');
    await buttons[0].click(); // "Hôm qua"
    await new Promise(r => setTimeout(r, 600));

    const kpiEvents = await page.evaluate(() => window.__kpiMutations);
    console.log('KPI mutation events recorded:', kpiEvents.length);
    kpiEvents.forEach(e => {
        console.log(`- hasKpiLoading: ${e.hasKpiLoading}`);
    });

    await browser.close();
})();
