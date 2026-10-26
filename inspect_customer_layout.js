const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const BASE_URL = 'http://localhost:5169';
const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

const sleep = ms => new Promise(r => setTimeout(r, ms));

(async () => {
    console.log('=== INSPECTING CUSTOMER PAGE LAYOUT & CLIPPING ===');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    // Login
    await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);

    // Go to Customer list
    await page.goto(`${BASE_URL}/doi-tac/khach-hang`, { waitUntil: 'networkidle2' });
    await sleep(1500);

    const diagnostics = await page.evaluate(() => {
        const toObj = r => r ? { x: Math.round(r.x), y: Math.round(r.y), w: Math.round(r.width), h: Math.round(r.height), left: Math.round(r.left), right: Math.round(r.right) } : null;

        const pageContainer = document.querySelector('.tf-page-container');
        const pageHeader = document.querySelector('.tf-page-header');
        const title = document.querySelector('.tf-page-title');
        const kpiGrid = document.querySelector('.tf-partner-metrics-grid');
        const firstKpi = document.querySelector('.tf-partner-metric-card');
        const splitLayout = document.querySelector('.tf-split-layout');
        const sidebar = document.querySelector('.tf-filter-sidebar');
        const mainPanel = document.querySelector('.tf-table-main-panel');
        const cardBody = document.querySelector('.tf-table-main-panel .tf-card-body');
        const table = document.querySelector('.tf-table');
        const ths = Array.from(document.querySelectorAll('.tf-table th')).map(th => ({
            text: th.innerText.trim(),
            rect: toObj(th.getBoundingClientRect()),
            minWidth: window.getComputedStyle(th).minWidth,
            width: window.getComputedStyle(th).width
        }));

        const trFirst = document.querySelector('.tf-table tbody tr:first-child');
        const tds = trFirst ? Array.from(trFirst.querySelectorAll('td')).map(td => ({
            text: td.innerText.trim().replace(/\n/g, ' '),
            rect: toObj(td.getBoundingClientRect())
        })) : [];

        return {
            pageContainerRect: toObj(pageContainer ? pageContainer.getBoundingClientRect() : null),
            pageHeaderRect: toObj(pageHeader ? pageHeader.getBoundingClientRect() : null),
            titleRect: toObj(title ? title.getBoundingClientRect() : null),
            kpiGridRect: toObj(kpiGrid ? kpiGrid.getBoundingClientRect() : null),
            firstKpiRect: toObj(firstKpi ? firstKpi.getBoundingClientRect() : null),
            sidebarRect: toObj(sidebar ? sidebar.getBoundingClientRect() : null),
            mainPanelRect: toObj(mainPanel ? mainPanel.getBoundingClientRect() : null),
            cardBodyRect: toObj(cardBody ? cardBody.getBoundingClientRect() : null),
            tableRect: toObj(table ? table.getBoundingClientRect() : null),
            scrollLeft: cardBody ? cardBody.scrollLeft : null,
            scrollWidth: cardBody ? cardBody.scrollWidth : null,
            clientWidth: cardBody ? cardBody.clientWidth : null,
            splitLayoutCols: splitLayout ? window.getComputedStyle(splitLayout).gridTemplateColumns : null,
            ths,
            tds
        };
    });

    console.log('Customer Layout Diagnostics:', JSON.stringify(diagnostics, null, 2));

    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'customer_layout_diagnostics.png') });

    await browser.close();
})();
