const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1600,1000']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1600, height: 1000 });

    // Login
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);

    // Go to /
    await page.goto('http://127.0.0.1:5169/', { waitUntil: 'networkidle2' });
    await new Promise(r => setTimeout(r, 2000));

    // Evaluate dashboard state
    const dashInfo = await page.evaluate(() => {
        const title = document.querySelector('.tf-dash-title')?.innerText;
        const kpiValues = Array.from(document.querySelectorAll('.tf-kpi-value')).map(el => el.innerText.trim());
        const pills = Array.from(document.querySelectorAll('.tf-dash-pill')).map(el => ({
            text: el.innerText.trim(),
            active: el.classList.contains('is-active')
        }));
        const chartTitle = document.querySelector('.tf-dash-card-title')?.innerText;
        const chartSub = document.querySelector('.tf-dash-card-subtitle')?.innerText;
        const axisTexts = Array.from(document.querySelectorAll('.tf-chart-axis-text')).map(el => el.textContent.trim());
        const sumBadge = document.querySelector('.tf-dash-chart-sum-badge')?.innerText;

        return {
            title,
            kpiValues,
            pills,
            chartTitle,
            chartSub,
            axisTexts,
            sumBadge
        };
    });

    console.log('Dashboard Info:', JSON.stringify(dashInfo, null, 2));

    await browser.close();
})();
