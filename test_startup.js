const puppeteer = require('puppeteer');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function testCleanLaunch() {
    console.log('Testing clean first launch...');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1400,900']
    });

    const context = await browser.createBrowserContext();
    const page = await context.newPage();
    await page.setViewport({ width: 1400, height: 900 });

    const logs = [];
    const errors = [];
    page.on('console', msg => logs.push(`[${msg.type()}] ${msg.text()}`));
    page.on('pageerror', err => errors.push(err.toString()));

    try {
        console.log('Navigating to http://localhost:5169/ on fresh browser context...');
        const response = await page.goto('http://localhost:5169/', { waitUntil: 'domcontentloaded' });
        console.log('Response status:', response.status());

        // Snapshot immediately at 300ms
        await new Promise(r => setTimeout(r, 300));
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'startup_01_300ms.png') });

        // Snapshot at 1500ms
        await new Promise(r => setTimeout(r, 1200));
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'startup_02_1500ms.png') });

        // Snapshot at 4000ms (after the 3500ms timeout)
        await new Promise(r => setTimeout(r, 2500));
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'startup_03_4000ms.png') });

        console.log('Console logs:', logs);
        console.log('Page errors:', errors);

        const loaderInfo = await page.evaluate(() => {
            const l = document.getElementById('tf-startup-loader');
            if (!l) return { exists: false };
            const style = window.getComputedStyle(l);
            const errBox = document.getElementById('tf-startup-error-box');
            return {
                exists: true,
                display: style.display,
                opacity: style.opacity,
                visibility: style.visibility,
                className: l.className,
                errorBoxDisplay: errBox ? window.getComputedStyle(errBox).display : null,
                normalContentDisplay: document.getElementById('tf-startup-normal-content') ? window.getComputedStyle(document.getElementById('tf-startup-normal-content')).display : null
            };
        });
        console.log('Loader Info after 4s:', loaderInfo);

        const currentUrl = page.url();
        console.log('Current URL after 4s:', currentUrl);

    } catch (err) {
        console.error('Error during clean launch test:', err);
    } finally {
        await browser.close();
    }
}

testCleanLaunch();
