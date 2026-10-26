const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://localhost:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('[VERIFY] Starting Targeted UI Repair & Dark Mode Regression Audit...');
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

    // Helper to ensure theme
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

    const results = {};

    try {
        // Step 1: Login
        console.log('[1/7] Logging in as admin...');
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
        // AREA 1: Stock Ledger (/kho/bien-dong)
        // =================================================================
        console.log('[2/7] Verifying Area 1: Stock Ledger (/kho/bien-dong)...');
        await page.goto(`${BASE_URL}/kho/bien-dong`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(1500);

        // Capture initial dark view
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'targeted_area1_stock_ledger_dark.png'),
            fullPage: false
        });

        // Evaluate ledger metrics
        const ledgerMetrics = await page.evaluate(() => {
            const wrap = document.querySelector('.tf-ledger-scroll-wrap');
            const table = document.querySelector('.tf-ledger-table');
            const notesTh = document.querySelector('.tf-th-notes');
            const notesCells = document.querySelectorAll('.tf-ledger-notes-cell');
            const viewportOverflow = document.documentElement.scrollWidth > window.innerWidth;

            let scrollMax = 0;
            if (wrap) {
                wrap.scrollLeft = wrap.scrollWidth;
                scrollMax = wrap.scrollLeft;
            }

            return {
                wrapFound: !!wrap,
                tableFound: !!table,
                tableWidth: table ? table.offsetWidth : 0,
                tableMinStyle: table ? window.getComputedStyle(table).minWidth : '',
                wrapScrollWidth: wrap ? wrap.scrollWidth : 0,
                wrapClientWidth: wrap ? wrap.clientWidth : 0,
                maxScrollLeft: scrollMax,
                notesThWidth: notesTh ? notesTh.offsetWidth : 0,
                firstNotesCellWidth: notesCells.length > 0 ? notesCells[0].offsetWidth : 0,
                viewportOverflow: viewportOverflow,
                thBg: notesTh ? window.getComputedStyle(notesTh).backgroundColor : '',
                thColor: notesTh ? window.getComputedStyle(notesTh).color : ''
            };
        });
        results.area1_ledger = ledgerMetrics;
        console.log('Area 1 Metrics:', ledgerMetrics);

        // Capture scrolled-right state showing full notes column
        await sleep(500);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'targeted_area1_stock_ledger_scrolled_dark.png'),
            fullPage: false
        });

        // Light mode capture
        await setTheme('light');
        await sleep(800);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'targeted_area1_stock_ledger_light.png'),
            fullPage: false
        });

        // =================================================================
        // AREA 2: Invoice Creation Form (/hoa-don/tao-moi)
        // =================================================================
        console.log('[3/7] Verifying Area 2: Invoice Creation Form (/hoa-don/tao-moi)...');
        await page.goto(`${BASE_URL}/hoa-don/tao-moi`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(1500);

        const invoiceMetrics = await page.evaluate(() => {
            const container = document.querySelector('.tf-invoice-create-container');
            const configRow = document.querySelector('.tf-invoice-config-row');
            const buyerGrid = document.querySelector('.tf-buyer-grid');
            const summaryRow = document.querySelector('.tf-invoice-bottom-summary');

            return {
                containerFound: !!container,
                containerWidth: container ? container.offsetWidth : 0,
                containerMaxWidth: container ? window.getComputedStyle(container).maxWidth : '',
                configRowFound: !!configRow,
                buyerGridColumns: buyerGrid ? window.getComputedStyle(buyerGrid).gridTemplateColumns : '',
                summaryRowFound: !!summaryRow,
                cardBg: container ? window.getComputedStyle(container.querySelector('.tf-card') || container).backgroundColor : ''
            };
        });
        results.area2_invoice = invoiceMetrics;
        console.log('Area 2 Metrics:', invoiceMetrics);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'targeted_area2_invoice_create_dark.png'),
            fullPage: false
        });

        await setTheme('light');
        await sleep(800);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'targeted_area2_invoice_create_light.png'),
            fullPage: false
        });

        // =================================================================
        // AREA 3, 4, 5: Sales/POS Modes & Cash Chips (/ban-hang)
        // =================================================================
        console.log('[4/7] Verifying Area 3, 4, 5: Sales/POS (/ban-hang)...');
        await page.goto(`${BASE_URL}/ban-hang`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(2000);

        // Click first product card if available to add item to order
        try {
            await page.waitForSelector('.tf-pos-grid-card, .tf-product-card, .pos-product-item', { timeout: 3000 });
            await page.click('.tf-pos-grid-card, .tf-product-card, .pos-product-item');
            await sleep(800);
        } catch (e) {
            console.log('Could not click product card, proceeding with layout inspection...');
        }

        // Open checkout modal or ensure payment area visible if needed
        try {
            const payBtn = await page.$('.tf-btn-pay, .btn-checkout, .tf-order-action-pay, button:has-text("Thanh toán")');
            if (payBtn) {
                await payBtn.click();
                await sleep(1000);
            }
        } catch (e) {}

        // Area 3: Inspect Cash Chips, F4, Chọn
        const posArea3Metrics = await page.evaluate(() => {
            const cashChips = Array.from(document.querySelectorAll('.tf-cash-chip')).map(el => {
                const s = window.getComputedStyle(el);
                return { text: el.innerText.trim(), bg: s.backgroundColor, color: s.color, border: s.borderColor };
            });

            const f4Kbds = Array.from(document.querySelectorAll('.tf-cust-kbd, .tf-order-cust-kbd')).map(el => {
                const s = window.getComputedStyle(el);
                return { text: el.innerText.trim(), bg: s.backgroundColor, color: s.color, border: s.borderColor };
            });

            const promoBtns = Array.from(document.querySelectorAll('.tf-promo-btn-hint')).map(el => {
                const s = window.getComputedStyle(el);
                return { text: el.innerText.trim(), bg: s.backgroundColor, color: s.color };
            });

            return { cashChips, f4Kbds, promoBtns };
        });
        results.area3_pos_controls = posArea3Metrics;
        console.log('Area 3 POS Controls Metrics:', posArea3Metrics);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'targeted_area3_pos_cash_chips_dark.png'),
            fullPage: false
        });

        await setTheme('light');
        await sleep(800);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'targeted_area3_pos_cash_chips_light.png'),
            fullPage: false
        });

        // Area 4: POS Mode 2 (Bán thường / Regular Sale)
        console.log('[5/7] Verifying Area 4: POS Regular Sale Mode...');
        await setTheme('dark');
        await sleep(800);

        // Click mode switchers: usually 3 tabs/buttons for Bán nhanh (Quick), Bán thường (Regular), Giao hàng (Delivery)
        const regularSwitched = await page.evaluate(() => {
            // Find switcher buttons
            const btns = Array.from(document.querySelectorAll('.tf-pos-mode-btn, .tf-view-switch-btn, button'));
            const regBtn = btns.find(b => b.innerText && b.innerText.includes('Bán thường'));
            if (regBtn) {
                regBtn.click();
                return true;
            }
            return false;
        });
        console.log('Switched to Regular Sale mode:', regularSwitched);
        await sleep(1500);

        const area4Metrics = await page.evaluate(() => {
            const catalog = document.querySelector('.tf-catalog-panel');
            const toolbar = document.querySelector('.tf-catalog-toolbar-pos');
            const order = document.querySelector('.tf-order-panel');
            return {
                catalogBg: catalog ? window.getComputedStyle(catalog).backgroundColor : 'not found',
                toolbarBg: toolbar ? window.getComputedStyle(toolbar).backgroundColor : 'not found',
                orderBg: order ? window.getComputedStyle(order).backgroundColor : 'not found'
            };
        });
        results.area4_regular_sale = area4Metrics;
        console.log('Area 4 Metrics:', area4Metrics);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'targeted_area4_pos_regular_sale_dark.png'),
            fullPage: false
        });

        await setTheme('light');
        await sleep(800);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'targeted_area4_pos_regular_sale_light.png'),
            fullPage: false
        });

        // Area 5: POS Mode 3 (Giao hàng / Delivery Sale)
        console.log('[6/7] Verifying Area 5: POS Delivery Sale Mode...');
        await setTheme('dark');
        await sleep(800);

        const deliverySwitched = await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-pos-mode-btn, .tf-view-switch-btn, button'));
            const delBtn = btns.find(b => b.innerText && b.innerText.includes('Giao hàng'));
            if (delBtn) {
                delBtn.click();
                return true;
            }
            return false;
        });
        console.log('Switched to Delivery Sale mode:', deliverySwitched);
        await sleep(1500);

        const area5Metrics = await page.evaluate(() => {
            const inputs = Array.from(document.querySelectorAll('.tf-input-control')).map(inp => {
                const s = window.getComputedStyle(inp);
                return { bg: s.backgroundColor, color: s.color, border: s.borderColor };
            });
            const carrierCard = document.querySelector('.tf-carrier-option-card, .tf-carrier-item');
            return {
                inputCount: inputs.length,
                inputSample: inputs.length > 0 ? inputs[0] : null,
                carrierCardBg: carrierCard ? window.getComputedStyle(carrierCard).backgroundColor : 'not found'
            };
        });
        results.area5_delivery_sale = area5Metrics;
        console.log('Area 5 Metrics:', area5Metrics);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'targeted_area5_pos_delivery_sale_dark.png'),
            fullPage: false
        });

        await setTheme('light');
        await sleep(800);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'targeted_area5_pos_delivery_sale_light.png'),
            fullPage: false
        });

        // =================================================================
        // AREA 6: Dashboard Notifications (/ Home)
        // =================================================================
        console.log('[7/7] Verifying Area 6: Dashboard Notifications (/)...');
        await page.goto(`${BASE_URL}/`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(1500);

        // Scroll to notifications container
        await page.evaluate(() => {
            const list = document.querySelector('.tf-notifications-list');
            if (list) {
                list.scrollIntoView({ behavior: 'instant', block: 'center' });
            }
        });
        await sleep(800);

        const area6Metrics = await page.evaluate(() => {
            const notifHeader = document.querySelector('.tf-notif-header');
            const notifTitle = document.querySelector('.tf-notification-title');
            const notifBadge = document.querySelector('.tf-notif-badge');
            const isApproval = document.querySelector('.is-approval-card');

            return {
                headerDisplay: notifHeader ? window.getComputedStyle(notifHeader).display : 'none',
                headerAlign: notifHeader ? window.getComputedStyle(notifHeader).alignItems : '',
                titleWidth: notifTitle ? notifTitle.offsetWidth : 0,
                titleColor: notifTitle ? window.getComputedStyle(notifTitle).color : '',
                badgeFound: !!notifBadge,
                badgeBg: notifBadge ? window.getComputedStyle(notifBadge).backgroundColor : '',
                badgeColor: notifBadge ? window.getComputedStyle(notifBadge).color : '',
                isApprovalCardBg: isApproval ? window.getComputedStyle(isApproval).backgroundColor : ''
            };
        });
        results.area6_dashboard_notifs = area6Metrics;
        console.log('Area 6 Metrics:', area6Metrics);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'targeted_area6_dashboard_notifications_dark.png'),
            fullPage: false
        });

        await setTheme('light');
        await sleep(800);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'targeted_area6_dashboard_notifications_light.png'),
            fullPage: false
        });

        // =================================================================
        // AREA 7: Category B Document Paper Isolation
        // =================================================================
        console.log('[BONUS] Verifying Category B Document Paper Isolation (/hoa-don/chi-tiet/15)...');
        try {
            await page.goto(`${BASE_URL}/hoa-don/chi-tiet/15`, { waitUntil: 'networkidle2' });
            await setTheme('dark');
            await sleep(1500);

            const docMetrics = await page.evaluate(() => {
                const sheet = document.querySelector('.tf-invoice-sheet, .contract-paper, .print-document, .tf-paper');
                if (!sheet) return { found: false };
                const s = window.getComputedStyle(sheet);
                return {
                    found: true,
                    paperBg: s.backgroundColor,
                    paperColor: s.color
                };
            });
            results.cat_b_isolation = docMetrics;
            console.log('Category B Document Metrics:', docMetrics);

            await page.screenshot({
                path: path.join(ARTIFACT_DIR, 'targeted_cat_b_invoice_isolated_dark.png'),
                fullPage: false
            });
        } catch (e) {
            console.log('Document detail page verification notice:', e.message);
        }

        console.log('\n[SUMMARY OF VERIFICATION METRICS]');
        console.log(JSON.stringify(results, null, 2));

        fs.writeFileSync(
            path.join(ARTIFACT_DIR, 'targeted_verification_results.json'),
            JSON.stringify(results, null, 2)
        );

    } catch (err) {
        console.error('Audit encountered error:', err);
    } finally {
        await browser.close();
        console.log('[VERIFY] Audit finished.');
    }
})();
