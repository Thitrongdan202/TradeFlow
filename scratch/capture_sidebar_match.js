const puppeteer = require('puppeteer');
const fs = require('fs');

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

    // Navigate to /cai-dat/sao-luu
    await page.goto('http://127.0.0.1:5169/cai-dat/sao-luu', { waitUntil: 'networkidle2' });
    await new Promise(r => setTimeout(r, 1000));

    // Ensure dark theme
    await page.evaluate(() => {
        document.documentElement.setAttribute('data-bs-theme', 'dark');
        document.documentElement.setAttribute('data-theme', 'dark');
        document.documentElement.classList.add('dark');
        document.cookie = 'tf_theme=dark;path=/;max-age=31536000';
        document.cookie = 'tradeflow_theme=dark;path=/;max-age=31536000';
    });

    // Ensure collapsed
    await page.evaluate(() => {
        const sidebar = document.querySelector('.tf-sidebar');
        const main = document.querySelector('.tf-main-wrapper');
        if (sidebar && !sidebar.classList.contains('is-collapsed')) {
            sidebar.classList.add('is-collapsed');
            if (main) main.classList.add('is-collapsed');
        }
    });

    await new Promise(r => setTimeout(r, 500));

    // Hover settings icon
    const settingsItem = await page.$('.tf-sidebar [data-flyout-group="settings"]');
    if (settingsItem) {
        const box = await settingsItem.boundingBox();
        await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
    }
    await new Promise(r => setTimeout(r, 600));

    // Capture screenshot of sidebar + flyout
    await page.screenshot({
        path: 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901/test_sidebar_match_v2.png',
        clip: { x: 0, y: 0, width: 420, height: 700 }
    });

    console.log('Screenshot v2 taken successfully!');
    await browser.close();
})();
