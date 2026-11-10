const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    // Step 1: Login
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

    // Set LIGHT MODE
    await page.setCookie(
        { name: 'tf_theme', value: 'light', domain: '127.0.0.1', path: '/' },
        { name: 'tradeflow_theme', value: 'light', domain: '127.0.0.1', path: '/' }
    );
    await page.goto('http://127.0.0.1:5169/', { waitUntil: 'networkidle2' });
    await page.waitForSelector('.tf-chart-svg');

    await page.evaluate(() => {
        document.documentElement.classList.remove('dark');
        document.documentElement.setAttribute('data-bs-theme', 'light');
        document.documentElement.setAttribute('data-theme', 'light');
        localStorage.setItem('tradeflow_theme', 'light');
        localStorage.setItem('tf_theme', 'light');
    });
    await new Promise(r => setTimeout(r, 400));

    // Capture Light Mode 30 days
    let chartCard = await page.$('.tf-dash-chart-card');
    if (chartCard) {
        await chartCard.screenshot({ path: path.join(ARTIFACT_DIR, 'dashboard_chart_30d_light.png') });
    }
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'dashboard_overview_light.png') });

    // Switch to 7 days
    const pills = await page.$$('.tf-dash-pill');
    await pills[2].click(); // 7 ngày qua
    await new Promise(r => setTimeout(r, 450));

    chartCard = await page.$('.tf-dash-chart-card');
    if (chartCard) {
        await chartCard.screenshot({ path: path.join(ARTIFACT_DIR, 'dashboard_chart_7d_light.png') });
    }

    // Now switch to DARK MODE
    await page.setCookie(
        { name: 'tf_theme', value: 'dark', domain: '127.0.0.1', path: '/' },
        { name: 'tradeflow_theme', value: 'dark', domain: '127.0.0.1', path: '/' }
    );
    await page.evaluate(() => {
        document.documentElement.classList.add('dark');
        document.documentElement.setAttribute('data-bs-theme', 'dark');
        document.documentElement.setAttribute('data-theme', 'dark');
        localStorage.setItem('tradeflow_theme', 'dark');
        localStorage.setItem('tf_theme', 'dark');
    });
    await new Promise(r => setTimeout(r, 400));

    chartCard = await page.$('.tf-dash-chart-card');
    if (chartCard) {
        await chartCard.screenshot({ path: path.join(ARTIFACT_DIR, 'dashboard_chart_7d_dark.png') });
    }
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'dashboard_overview_dark.png') });

    console.log('All visual screenshots saved successfully!');
    await browser.close();
})();
