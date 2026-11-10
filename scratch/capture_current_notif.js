const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

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

    // Click bell icon
    const bellBtn = await page.$('#tf-notification-menu-btn');
    if (bellBtn) {
        await bellBtn.click();
        await new Promise(r => setTimeout(r, 600));

        // Screenshot Notification dropdown Light Mode
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'notification_current_light.png'),
            clip: { x: 750, y: 0, width: 680, height: 700 }
        });

        // Switch to Dark Mode
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.documentElement.setAttribute('data-theme', 'dark');
            document.documentElement.classList.add('dark');
        });
        await new Promise(r => setTimeout(r, 400));

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'notification_current_dark.png'),
            clip: { x: 750, y: 0, width: 680, height: 700 }
        });

        console.log('Current notifications captured!');
    } else {
        console.error('Bell button not found!');
    }

    await browser.close();
})();
