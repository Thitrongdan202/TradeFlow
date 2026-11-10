const puppeteer = require('puppeteer');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function login(page, username, password = 'tradecore123') {
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    const userInput = await page.$('input[name="Input.Username"]');
    if (userInput) {
        await userInput.click({ clickCount: 3 });
        await userInput.type(username);
    }
    const passInput = await page.$('input[name="Input.Password"]');
    if (passInput) {
        await passInput.click({ clickCount: 3 });
        await passInput.type(password);
    }
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    await delay(1200);
}

async function run() {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1600,1050']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1600, height: 1050 });

    try {
        await login(page, 'admin', 'tradecore123');

        // Capture Tab Matrix
        console.log('Capturing rbac_access_05.png (Matrix)...');
        await page.goto('http://127.0.0.1:5169/cai-dat/phan-quyen/matrix', { waitUntil: 'networkidle2' });
        await delay(2000);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_05.png'),
            fullPage: false
        });

        // Capture Tab Roles
        console.log('Capturing rbac_access_06.png (Roles & Protection)...');
        await page.goto('http://127.0.0.1:5169/cai-dat/phan-quyen/roles', { waitUntil: 'networkidle2' });
        await delay(2000);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_06.png'),
            fullPage: false
        });

        // Capture Tab Audit
        console.log('Capturing rbac_access_07.png (Audit Logs)...');
        await page.goto('http://127.0.0.1:5169/cai-dat/phan-quyen/audit', { waitUntil: 'networkidle2' });
        await delay(2000);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_07.png'),
            fullPage: false
        });

        console.log('All final screenshots captured successfully.');
    } catch (err) {
        console.error('Error:', err);
    } finally {
        await browser.close();
    }
}

run();
