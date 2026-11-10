const puppeteer = require('puppeteer');
const path = require('path');

const ARTIFACTS_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    // Login as Admin
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'admin');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);

    // Go to /cai-dat/sao-luu
    await page.goto('http://127.0.0.1:5169/cai-dat/sao-luu', { waitUntil: 'networkidle2' });
    await new Promise(r => setTimeout(r, 1000));

    // Collapse sidebar
    await page.evaluate(() => {
        // Trigger collapse if not collapsed
        const collapseBtn = document.querySelector('.tf-nav-collapse-btn');
        if (collapseBtn && !document.querySelector('.tf-sidebar.collapsed')) {
            collapseBtn.click();
        }
        // Set dark mode to match user reference image
        document.documentElement.setAttribute('data-bs-theme', 'dark');
        document.documentElement.setAttribute('data-theme', 'dark');
        document.documentElement.classList.add('dark');
    });
    await new Promise(r => setTimeout(r, 800));

    // Hover over the settings item (data-flyout-group="settings")
    const settingsItem = await page.$('.tf-nav-item[data-flyout-group="settings"]');
    if (settingsItem) {
        await settingsItem.hover();
        await new Promise(r => setTimeout(r, 600));
    }

    // Capture screenshot of sidebar + flyout
    await page.screenshot({
        path: path.join(ARTIFACTS_DIR, 'current_settings_flyout_match.png'),
        clip: { x: 0, y: 0, width: 420, height: 800 }
    });
    console.log('✓ Captured current_settings_flyout_match.png');

    await browser.close();
})();
