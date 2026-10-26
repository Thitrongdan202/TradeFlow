const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const BASE_URL = 'http://localhost:5169';
const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

const sleep = ms => new Promise(r => setTimeout(r, ms));

(async () => {
    console.log('--- STARTING INITIAL REPRODUCTION INSPECTION ---');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    // Login
    console.log('Logging in...');
    await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
    await page.waitForSelector('#login-username', { timeout: 10000 });
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    console.log('Logged in successfully.');

    // 1. Debt Management (/doi-tac/cong-no) - Light Mode
    console.log('Navigating to Debt Management (/doi-tac/cong-no)...');
    await page.goto(`${BASE_URL}/doi-tac/cong-no`, { waitUntil: 'networkidle2' });
    await sleep(1500);

    // Capture screenshot of Debt Page Light Mode
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'repro_debt_light_initial.png'), fullPage: false });

    // Inspect Table & Action column dimensions
    const debtMetrics = await page.evaluate(() => {
        const table = document.querySelector('.tf-table');
        const cardBody = document.querySelector('.tf-card-body');
        const mainPanel = document.querySelector('.tf-table-main-panel');
        const splitLayout = document.querySelector('.tf-split-layout');
        const lastTh = document.querySelector('.tf-table th:last-child');
        const firstRowLastTd = document.querySelector('.tf-table tbody tr:first-child td:last-child');
        const actionButtons = firstRowLastTd ? Array.from(firstRowLastTd.querySelectorAll('button, a')).map(b => ({
            text: b.innerText.trim(),
            rect: b.getBoundingClientRect()
        })) : [];

        return {
            tableWidth: table ? table.offsetWidth : null,
            tableScrollWidth: table ? table.scrollWidth : null,
            cardBodyWidth: cardBody ? cardBody.offsetWidth : null,
            cardBodyScrollWidth: cardBody ? cardBody.scrollWidth : null,
            cardBodyOverflowX: cardBody ? window.getComputedStyle(cardBody).overflowX : null,
            mainPanelWidth: mainPanel ? mainPanel.offsetWidth : null,
            splitLayoutWidth: splitLayout ? splitLayout.offsetWidth : null,
            lastThRect: lastTh ? lastTh.getBoundingClientRect() : null,
            lastTdRect: firstRowLastTd ? firstRowLastTd.getBoundingClientRect() : null,
            cardBodyRect: cardBody ? cardBody.getBoundingClientRect() : null,
            actionButtons
        };
    });
    console.log('Debt Metrics:', JSON.stringify(debtMetrics, null, 2));

    // 2. Customer List (/doi-tac/khach-hang)
    console.log('Navigating to Customer List (/doi-tac/khach-hang)...');
    await page.goto(`${BASE_URL}/doi-tac/khach-hang`, { waitUntil: 'networkidle2' });
    await sleep(1500);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'repro_customer_light_initial.png'), fullPage: false });

    const custMetrics = await page.evaluate(() => {
        const table = document.querySelector('.tf-table');
        const cardBody = document.querySelector('.tf-card-body');
        const firstRowLastTd = document.querySelector('.tf-table tbody tr:first-child td:last-child');
        const actionButtons = firstRowLastTd ? Array.from(firstRowLastTd.querySelectorAll('button, a')).map(b => ({
            text: b.innerText.trim(),
            rect: b.getBoundingClientRect()
        })) : [];

        return {
            tableWidth: table ? table.offsetWidth : null,
            cardBodyWidth: cardBody ? cardBody.offsetWidth : null,
            cardBodyScrollWidth: cardBody ? cardBody.scrollWidth : null,
            cardBodyRect: cardBody ? cardBody.getBoundingClientRect() : null,
            lastTdRect: firstRowLastTd ? firstRowLastTd.getBoundingClientRect() : null,
            actionButtons
        };
    });
    console.log('Customer Metrics:', JSON.stringify(custMetrics, null, 2));

    // 3. Price Comparison (/bang-gia/so-sanh) - Light Mode
    console.log('Navigating to Price Comparison (/bang-gia/so-sanh)...');
    await page.goto(`${BASE_URL}/bang-gia/so-sanh`, { waitUntil: 'networkidle2' });
    await sleep(1500);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'repro_price_compare_light_initial.png'), fullPage: false });

    const priceCompareMetrics = await page.evaluate(() => {
        const headers = Array.from(document.querySelectorAll('.tf-table th')).map(th => ({
            text: th.innerText.trim().replace(/\n/g, ' '),
            bg: window.getComputedStyle(th).backgroundColor,
            color: window.getComputedStyle(th).color
        }));
        return { headers };
    });
    console.log('Price Compare Headers (Light Mode):', JSON.stringify(priceCompareMetrics, null, 2));

    await browser.close();
    console.log('Inspection complete.');
})();
