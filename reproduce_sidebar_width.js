const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://localhost:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function inspectLayout(page, label) {
    return await page.evaluate((label) => {
        const sidebar = document.querySelector('.tf-sidebar');
        const mainWrapper = document.querySelector('.tf-main-wrapper');
        const topbar = document.querySelector('.tf-topbar');
        const content = document.querySelector('.tf-content');

        const sRect = sidebar ? sidebar.getBoundingClientRect() : null;
        const wRect = mainWrapper ? mainWrapper.getBoundingClientRect() : null;
        const tRect = topbar ? topbar.getBoundingClientRect() : null;
        const cRect = content ? content.getBoundingClientRect() : null;

        const viewportWidth = window.innerWidth;
        const emptyRightSpace = cRect ? Math.max(0, viewportWidth - cRect.right) : 0;

        return {
            label,
            viewportWidth,
            sidebar: sRect ? { width: sRect.width, right: sRect.right } : null,
            mainWrapper: wRect ? { width: wRect.width, left: wRect.left, right: wRect.right } : null,
            topbar: tRect ? { width: tRect.width, right: tRect.right } : null,
            content: cRect ? {
                width: cRect.width,
                left: cRect.left,
                right: cRect.right,
                computedMaxWidth: window.getComputedStyle(content).maxWidth
            } : null,
            emptyRightSpace
        };
    }, label);
}

async function run() {
    console.log('--- Inspecting Sidebar Collapse Layout Defect ---');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    // Test at 1706x900 as specified in prompt
    await page.setViewport({ width: 1706, height: 900 });

    // Login
    await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
    await page.waitForSelector('#login-username');
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);

    await sleep(500);

    // 1. Dashboard Expanded
    const expandedStats = await inspectLayout(page, 'Dashboard Expanded (1706px)');
    console.log('1. Dashboard Expanded:', JSON.stringify(expandedStats, null, 2));
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'reproduce_01_dashboard_expanded.png') });

    // 2. Collapse Sidebar
    console.log('2. Clicking sidebar toggle to collapse...');
    await page.click('.tf-sidebar-toggle');
    await sleep(400); // Wait for transition

    const collapsedStats = await inspectLayout(page, 'Dashboard Collapsed (1706px)');
    console.log('2. Dashboard Collapsed:', JSON.stringify(collapsedStats, null, 2));
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'reproduce_02_dashboard_collapsed.png') });

    // 3. Customers Page Collapsed
    await page.goto(`${BASE_URL}/doi-tac/khach-hang`, { waitUntil: 'networkidle2' });
    await sleep(600);
    const customersStats = await inspectLayout(page, 'Customers Collapsed (1706px)');
    console.log('3. Customers Collapsed:', JSON.stringify(customersStats, null, 2));
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'reproduce_03_customers_collapsed.png') });

    // 4. Products Page Collapsed
    await page.goto(`${BASE_URL}/danh-muc/san-pham`, { waitUntil: 'networkidle2' });
    await sleep(600);
    const productsStats = await inspectLayout(page, 'Products Collapsed (1706px)');
    console.log('4. Products Collapsed:', JSON.stringify(productsStats, null, 2));
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'reproduce_04_products_collapsed.png') });

    await browser.close();
}

run();
