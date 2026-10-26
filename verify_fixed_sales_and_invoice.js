const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://localhost:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('[VERIFY] Starting Sales/POS & Invoice Layout Verification...');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: [
            '--no-sandbox',
            '--disable-setuid-sandbox',
            '--window-size=1440,900',
            '--disable-web-security'
        ]
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    async function setTheme(theme) {
        await page.evaluate((t) => {
            document.documentElement.setAttribute('data-bs-theme', t);
            document.documentElement.setAttribute('data-theme', t);
            localStorage.setItem('tradeflow_theme', t);
            if (window.TradeFlow && window.TradeFlow.Theme) {
                window.TradeFlow.Theme.setTheme(t);
            }
        }, theme);
        await sleep(500);
    }

    const auditResults = {};

    try {
        // 1. Login
        console.log('[1/4] Logging in as admin...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('#login-username', { timeout: 10000 });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        console.log('Logged in successfully. URL:', page.url());
        await sleep(1500);

        // =================================================================
        // PROBLEM B: INVOICE CREATION LAYOUT & POSITIONING
        // =================================================================
        console.log('[2/4] Verifying Problem B: Invoice Creation Layout & Positioning...');
        await page.goto(`${BASE_URL}/hoa-don/tao-moi`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(1500);

        // Measure layout metrics at 1440px desktop
        const invoiceLayoutMetrics = await page.evaluate(() => {
            const container = document.querySelector('.tf-invoice-create-container');
            const mainContent = document.querySelector('.tf-content');
            const header = document.querySelector('.tf-page-header');
            const buyerGrid = document.querySelector('.tf-buyer-grid');
            const bottomGrid = document.querySelector('.tf-invoice-bottom-grid');
            const notesCard = document.querySelector('.tf-notes-card');
            const summaryCard = document.querySelector('.tf-summary-card');

            const containerRect = container ? container.getBoundingClientRect() : null;
            const contentRect = mainContent ? mainContent.getBoundingClientRect() : null;

            // Offset between content padding and container left
            const leftOffset = containerRect && contentRect ? containerRect.left - contentRect.left : null;

            return {
                containerFound: !!container,
                contentWidth: contentRect ? contentRect.width : 0,
                containerWidth: containerRect ? containerRect.width : 0,
                containerLeft: containerRect ? containerRect.left : 0,
                contentLeft: contentRect ? contentRect.left : 0,
                leftRelativeOffset: leftOffset,
                buyerGridColumns: buyerGrid ? window.getComputedStyle(buyerGrid).gridTemplateColumns : '',
                bottomGridColumns: bottomGrid ? window.getComputedStyle(bottomGrid).gridTemplateColumns : '',
                notesCardHeight: notesCard ? notesCard.offsetHeight : 0,
                summaryCardHeight: summaryCard ? summaryCard.offsetHeight : 0,
                containerBg: container ? window.getComputedStyle(container.querySelector('.tf-card') || container).backgroundColor : ''
            };
        });
        auditResults.invoice_layout = invoiceLayoutMetrics;
        console.log('Invoice Layout Metrics (1440px):', invoiceLayoutMetrics);

        // Screenshot 1: Desktop 1440px Dark
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'invoice_create_wide_desktop_dark.png'),
            fullPage: false
        });

        // Screenshot 2: Desktop 1440px Light
        await setTheme('light');
        await sleep(600);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'invoice_create_wide_desktop_light.png'),
            fullPage: false
        });

        // Screenshot 3: Sidebar Collapsed in Dark Mode
        await setTheme('dark');
        await sleep(600);
        await page.click('.tf-sidebar-toggle');
        await sleep(800);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'invoice_create_sidebar_collapsed_dark.png'),
            fullPage: false
        });
        // Restore sidebar
        await page.click('.tf-sidebar-toggle');
        await sleep(800);

        // Screenshot 4: Medium Viewport (1024x768)
        await page.setViewport({ width: 1024, height: 768 });
        await sleep(800);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'invoice_create_medium_viewport_dark.png'),
            fullPage: false
        });
        // Reset viewport back to 1440x900
        await page.setViewport({ width: 1440, height: 900 });
        await sleep(600);

        // =================================================================
        // PROBLEM A: SALES/POS IN DARK MODE (REGULAR & QUICK MODES)
        // =================================================================
        console.log('[3/4] Verifying Problem A: Sales/POS in Dark Mode...');
        await page.goto(`${BASE_URL}/ban-hang`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(2000);

        // Switch to Regular Mode (Bán thường)
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-mode-btn, .tf-pos-mode-btn, button'));
            const regBtn = btns.find(b => b.innerText && b.innerText.includes('Bán thường'));
            if (regBtn) regBtn.click();
        });
        await sleep(1500);

        // Measure Regular Mode Dark Elements
        const regularModeMetrics = await page.evaluate(() => {
            const custSearchWrap = document.querySelector('.tf-order-cust-search-wrap');
            const custInput = document.querySelector('.tf-order-cust-input');
            const addCustBtn = document.querySelector('.tf-btn-add-cust-mini');
            const orderNoteRow = document.querySelector('.tf-order-note-input-row');
            const orderNoteInput = document.querySelector('.tf-order-note-input');
            const catSearch = document.querySelector('.tf-pos-cat-search');

            return {
                custSearchWrapBg: custSearchWrap ? window.getComputedStyle(custSearchWrap).backgroundColor : 'not found',
                custSearchWrapBorder: custSearchWrap ? window.getComputedStyle(custSearchWrap).borderColor : '',
                custInputColor: custInput ? window.getComputedStyle(custInput).color : '',
                addCustBtnBg: addCustBtn ? window.getComputedStyle(addCustBtn).backgroundColor : '',
                addCustBtnColor: addCustBtn ? window.getComputedStyle(addCustBtn).color : '',
                orderNoteRowBg: orderNoteRow ? window.getComputedStyle(orderNoteRow).backgroundColor : 'not found',
                orderNoteRowBorder: orderNoteRow ? window.getComputedStyle(orderNoteRow).borderColor : '',
                orderNoteInputColor: orderNoteInput ? window.getComputedStyle(orderNoteInput).color : '',
                catSearchBg: catSearch ? window.getComputedStyle(catSearch).backgroundColor : 'not found'
            };
        });
        auditResults.sales_regular_mode = regularModeMetrics;
        console.log('Sales Regular Mode Metrics:', regularModeMetrics);

        // Screenshot 5: Sales Regular Mode Dark
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'sales_regular_mode_repaired_dark.png'),
            fullPage: false
        });

        // Screenshot 6: Sales Regular Mode Light
        await setTheme('light');
        await sleep(600);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'sales_regular_mode_repaired_light.png'),
            fullPage: false
        });

        // Screenshot 7: Group Filter Modal in Dark Mode
        await setTheme('dark');
        await sleep(600);
        // Click "Nhóm hàng" button to trigger group filter modal
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-filter-trigger-btn, button'));
            const groupBtn = btns.find(b => b.innerText && b.innerText.includes('Nhóm hàng'));
            if (groupBtn) groupBtn.click();
        });
        await sleep(1000);

        const filterModalMetrics = await page.evaluate(() => {
            const modal = document.querySelector('.tf-modal-dialog');
            const searchWrap = document.querySelector('.tf-filter-search-input-wrap');
            const footer = document.querySelector('.tf-filter-modal-footer');
            return {
                modalFound: !!modal,
                modalBg: modal ? window.getComputedStyle(modal).backgroundColor : 'not found',
                searchWrapBg: searchWrap ? window.getComputedStyle(searchWrap).backgroundColor : 'not found',
                footerBg: footer ? window.getComputedStyle(footer).backgroundColor : 'not found'
            };
        });
        auditResults.filter_modal = filterModalMetrics;
        console.log('Filter Modal Metrics:', filterModalMetrics);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'sales_filter_modal_repaired_dark.png'),
            fullPage: false
        });

        // Close modal
        await page.evaluate(() => {
            const closeBtn = document.querySelector('.tf-modal-close-btn, .tf-btn-secondary');
            if (closeBtn) closeBtn.click();
        });
        await sleep(800);

        // Switch to Quick Mode (Bán nhanh)
        console.log('[4/4] Verifying Quick Mode (Bán nhanh)...');
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-mode-btn, .tf-pos-mode-btn, button'));
            const qBtn = btns.find(b => b.innerText && b.innerText.includes('Bán nhanh'));
            if (qBtn) qBtn.click();
        });
        await sleep(1200);

        const quickModeMetrics = await page.evaluate(() => {
            const notesWrap = document.querySelector('.tf-notes-wrapper');
            const notesInput = document.querySelector('.tf-notes-input');
            const custBox = document.querySelector('.tf-cust-search-box');
            return {
                notesWrapBg: notesWrap ? window.getComputedStyle(notesWrap).backgroundColor : 'not found',
                notesInputColor: notesInput ? window.getComputedStyle(notesInput).color : '',
                custBoxBg: custBox ? window.getComputedStyle(custBox).backgroundColor : 'not found'
            };
        });
        auditResults.sales_quick_mode = quickModeMetrics;
        console.log('Sales Quick Mode Metrics:', quickModeMetrics);

        // Screenshot 8: Sales Quick Mode Dark
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'sales_quick_mode_repaired_dark.png'),
            fullPage: false
        });

        // Screenshot 9: Sales Quick Mode Light
        await setTheme('light');
        await sleep(600);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'sales_quick_mode_repaired_light.png'),
            fullPage: false
        });

        console.log('\n[SUMMARY OF ALL AUDIT METRICS]');
        console.log(JSON.stringify(auditResults, null, 2));

        fs.writeFileSync(
            path.join(ARTIFACT_DIR, 'fixed_sales_and_invoice_results.json'),
            JSON.stringify(auditResults, null, 2)
        );

    } catch (err) {
        console.error('Audit encountered error:', err);
    } finally {
        await browser.close();
        console.log('[VERIFY] Audit suite completed.');
    }
})();
