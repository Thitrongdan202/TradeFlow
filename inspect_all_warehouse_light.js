const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({ headless: 'new' });
    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    await page.goto('http://localhost:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.waitForSelector('#login-username');
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle2' }), page.click('button[type="submit"]')]);

    async function setTheme(theme) {
        await page.evaluate((t) => {
            document.documentElement.setAttribute('data-bs-theme', t);
            document.documentElement.setAttribute('data-theme', t);
            if (t === 'dark') {
                document.body.classList.add('theme-dark');
                document.body.classList.remove('theme-light');
            } else {
                document.body.classList.add('theme-light');
                document.body.classList.remove('theme-dark');
            }
        }, theme);
        await new Promise(r => setTimeout(r, 400));
    }

    const urls = [
        { name: 'Quarantine', url: '/kho/hang-loi' },
        { name: 'Stocktaking', url: '/kho/kiem-kho' },
        { name: 'StockList', url: '/kho/ton-kho' },
        { name: 'Vouchers', url: '/kho/phieu-kho' },
        { name: 'Returns', url: '/kho/hang-tra' }
    ];

    const results = {};

    for (const item of urls) {
        await page.goto(`http://localhost:5169${item.url}`, { waitUntil: 'networkidle2' });
        await setTheme('light');
        await new Promise(r => setTimeout(r, 600));

        const pageData = await page.evaluate(() => {
            // Check Warning Banner if present
            const banner = document.querySelector('.tf-quarantine-warning-banner, .tw-bg-amber-50');
            let bannerInfo = null;
            if (banner) {
                const title = banner.querySelector('.tf-banner-title, .tw-font-bold');
                const body = banner.querySelector('.tf-banner-text, div > div > span:last-child');
                bannerInfo = {
                    bg: window.getComputedStyle(banner).backgroundColor,
                    border: window.getComputedStyle(banner).borderColor,
                    text: window.getComputedStyle(banner).color,
                    boldText: title ? window.getComputedStyle(title).color : null,
                    bodyText: body ? window.getComputedStyle(body).color : null
                };
            }

            // Segmented Tabs
            const tabs = Array.from(document.querySelectorAll('.tf-segmented-tab')).map(tab => {
                const badge = tab.querySelector('span:last-child');
                return {
                    text: tab.innerText.split('\n')[0].trim(),
                    isActive: tab.classList.contains('is-active'),
                    btnColor: window.getComputedStyle(tab).color,
                    btnBg: window.getComputedStyle(tab).backgroundColor,
                    badgeText: badge ? badge.innerText.trim() : null,
                    badgeColor: badge ? window.getComputedStyle(badge).color : null,
                    badgeBg: badge ? window.getComputedStyle(badge).backgroundColor : null
                };
            });

            return { bannerInfo, tabs };
        });

        results[item.name] = pageData;
    }

    console.log('--- LIGHT MODE INSPECTION RESULTS ---');
    console.log(JSON.stringify(results, null, 2));
    await browser.close();
})();
