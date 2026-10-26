const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const BASE_URL = 'http://localhost:5169';
const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

const sleep = ms => new Promise(r => setTimeout(r, ms));

(async () => {
    console.log('--- STARTING INVOICE REBALANCE VERIFICATION ---');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1920,1080']
    });

    const page = await browser.newPage();
    const auditResults = {};

    async function setTheme(theme) {
        await page.evaluate((t) => {
            document.documentElement.setAttribute('data-bs-theme', t);
            document.documentElement.setAttribute('data-theme', t);
            if (t === 'dark') {
                document.body.classList.add('theme-dark');
                document.body.classList.remove('theme-light');
            } else {
                document.body.classList.add('theme-light');
                document.body.classList.remove('theme-dark');
            }
        }, theme);
        await sleep(500);
    }

    try {
        // 1. Login
        console.log('[1/6] Logging in as admin...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('#login-username', { timeout: 10000 });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        console.log('Logged in successfully.');
        await sleep(1500);

        // 2. Navigate to Invoice Creation at 1920x1080
        console.log('[2/6] Verifying 1920x1080 Wide Desktop Layout...');
        await page.setViewport({ width: 1920, height: 1080 });
        await page.goto(`${BASE_URL}/hoa-don/tao-moi`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(1500);

        const wideMetrics = await page.evaluate(() => {
            const container = document.querySelector('.tf-invoice-create-container');
            const cards = Array.from(document.querySelectorAll('.tf-invoice-card'));
            const configGrid = document.querySelector('.tf-invoice-config-grid');
            const searchToolbars = Array.from(document.querySelectorAll('.tf-search-toolbar'));
            const buyerGrid = document.querySelector('.tf-buyer-grid');
            const itemsTable = document.querySelector('.tf-invoice-items-table');
            const bottomGrid = document.querySelector('.tf-invoice-bottom-grid');
            const notesCard = document.querySelector('.tf-notes-card');
            const summaryCard = document.querySelector('.tf-summary-card');

            const containerRect = container ? container.getBoundingClientRect() : null;
            const card0Rect = cards[0] ? cards[0].getBoundingClientRect() : null;
            const configGridRect = configGrid ? configGrid.getBoundingClientRect() : null;

            return {
                viewport: '1920x1080',
                containerWidth: containerRect ? containerRect.width : 0,
                cardCount: cards.length,
                cardPaddings: cards.map(c => window.getComputedStyle(c).padding),
                configGridColumns: configGrid ? window.getComputedStyle(configGrid).gridTemplateColumns : '',
                configGridWidth: configGridRect ? configGridRect.width : 0,
                configGridSpanRatio: (card0Rect && configGridRect) ? (configGridRect.width / (card0Rect.width - 48)).toFixed(3) : 0,
                searchToolbarWidths: searchToolbars.map(s => s.offsetWidth),
                buyerGridColumns: buyerGrid ? window.getComputedStyle(buyerGrid).gridTemplateColumns : '',
                itemsTableWidth: itemsTable ? itemsTable.offsetWidth : 0,
                bottomGridColumns: bottomGrid ? window.getComputedStyle(bottomGrid).gridTemplateColumns : '',
                notesCardHeight: notesCard ? notesCard.offsetHeight : 0,
                summaryCardHeight: summaryCard ? summaryCard.offsetHeight : 0,
                notesCardPadding: notesCard ? window.getComputedStyle(notesCard).padding : '',
                summaryCardPadding: summaryCard ? window.getComputedStyle(summaryCard).padding : '',
                cardBgDark: cards[0] ? window.getComputedStyle(cards[0]).backgroundColor : '',
                cardBorderDark: cards[0] ? window.getComputedStyle(cards[0]).borderColor : ''
            };
        });
        auditResults.wide_desktop_1920 = wideMetrics;
        console.log('1920x1080 Metrics:', wideMetrics);

        // Capture Wide Desktop Dark
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'invoice_create_wide_desktop_dark.png'),
            fullPage: false
        });

        // Capture Wide Desktop Light
        await setTheme('light');
        await sleep(600);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'invoice_create_wide_desktop_light.png'),
            fullPage: false
        });

        // 3. Test Standard Desktop 1440x900
        console.log('[3/6] Verifying 1440x900 Desktop Layout...');
        await page.setViewport({ width: 1440, height: 900 });
        await setTheme('dark');
        await sleep(800);

        const standardMetrics = await page.evaluate(() => {
            const container = document.querySelector('.tf-invoice-create-container');
            const notesCard = document.querySelector('.tf-notes-card');
            const summaryCard = document.querySelector('.tf-summary-card');
            const configGrid = document.querySelector('.tf-invoice-config-grid');
            const buyerGrid = document.querySelector('.tf-buyer-grid');

            return {
                viewport: '1440x900',
                containerWidth: container ? container.offsetWidth : 0,
                configGridColumns: configGrid ? window.getComputedStyle(configGrid).gridTemplateColumns : '',
                buyerGridColumns: buyerGrid ? window.getComputedStyle(buyerGrid).gridTemplateColumns : '',
                notesCardHeight: notesCard ? notesCard.offsetHeight : 0,
                summaryCardHeight: summaryCard ? summaryCard.offsetHeight : 0,
                heightDifference: (notesCard && summaryCard) ? Math.abs(notesCard.offsetHeight - summaryCard.offsetHeight) : null
            };
        });
        auditResults.standard_desktop_1440 = standardMetrics;
        console.log('1440x900 Metrics:', standardMetrics);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'invoice_rebalanced_1440_dark.png'),
            fullPage: false
        });

        await setTheme('light');
        await sleep(600);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'invoice_rebalanced_1440_light.png'),
            fullPage: false
        });

        // 4. Test Sidebar Collapsed State
        console.log('[4/6] Verifying Sidebar Collapsed State...');
        await setTheme('dark');
        await sleep(600);
        await page.click('.tf-sidebar-toggle');
        await sleep(800);

        const collapsedMetrics = await page.evaluate(() => {
            const container = document.querySelector('.tf-invoice-create-container');
            const content = document.querySelector('.tf-content');
            return {
                containerWidth: container ? container.offsetWidth : 0,
                contentWidth: content ? content.offsetWidth : 0
            };
        });
        auditResults.sidebar_collapsed = collapsedMetrics;
        console.log('Sidebar Collapsed Metrics:', collapsedMetrics);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'invoice_create_sidebar_collapsed_dark.png'),
            fullPage: false
        });

        // Restore sidebar
        await page.click('.tf-sidebar-toggle');
        await sleep(800);

        // 5. Test Medium Viewport (Tablet 1024x768)
        console.log('[5/6] Verifying Tablet / Medium Viewport (1024x768)...');
        await page.setViewport({ width: 1024, height: 768 });
        await sleep(800);

        const mediumMetrics = await page.evaluate(() => {
            const configGrid = document.querySelector('.tf-invoice-config-grid');
            const buyerGrid = document.querySelector('.tf-buyer-grid');
            const bottomGrid = document.querySelector('.tf-invoice-bottom-grid');
            return {
                viewport: '1024x768',
                configGridColumns: configGrid ? window.getComputedStyle(configGrid).gridTemplateColumns : '',
                buyerGridColumns: buyerGrid ? window.getComputedStyle(buyerGrid).gridTemplateColumns : '',
                bottomGridColumns: bottomGrid ? window.getComputedStyle(bottomGrid).gridTemplateColumns : ''
            };
        });
        auditResults.medium_viewport_1024 = mediumMetrics;
        console.log('1024x768 Metrics:', mediumMetrics);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'invoice_create_medium_viewport_dark.png'),
            fullPage: false
        });

        // 6. Interactive Testing: Add Items and Verify Calculations
        console.log('[6/6] Verifying Interactive Item Addition & Calculations...');
        await page.setViewport({ width: 1440, height: 900 });
        await sleep(600);

        // Click "+ Thêm dòng thủ công"
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-btn'));
            const addBtn = btns.find(b => b.innerText && b.innerText.includes('Thêm dòng thủ công'));
            if (addBtn) addBtn.click();
        });
        await sleep(800);

        // Fill item 1 values
        await page.evaluate(() => {
            const table = document.querySelector('.tf-invoice-items-table');
            const rows = table.querySelectorAll('tbody tr');
            if (rows.length > 0) {
                const firstRow = rows[0];
                const inputs = firstRow.querySelectorAll('input');
                if (inputs.length >= 5) {
                    inputs[0].value = 'Bình Ắc quy GS 12V-75Ah Đồng Nai';
                    inputs[0].dispatchEvent(new Event('input', { bubbles: true }));
                    inputs[0].dispatchEvent(new Event('change', { bubbles: true }));

                    inputs[1].value = 'Bình';
                    inputs[1].dispatchEvent(new Event('input', { bubbles: true }));
                    inputs[1].dispatchEvent(new Event('change', { bubbles: true }));

                    inputs[2].value = '2';
                    inputs[2].dispatchEvent(new Event('input', { bubbles: true }));
                    inputs[2].dispatchEvent(new Event('change', { bubbles: true }));

                    inputs[3].value = '1500000';
                    inputs[3].dispatchEvent(new Event('input', { bubbles: true }));
                    inputs[3].dispatchEvent(new Event('change', { bubbles: true }));

                    inputs[4].value = '100000';
                    inputs[4].dispatchEvent(new Event('input', { bubbles: true }));
                    inputs[4].dispatchEvent(new Event('change', { bubbles: true }));
                }
            }
        });
        await sleep(1000);

        // Add a second item using the bottom action button
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-table-bottom-actions button, .tf-btn'));
            const addBtn = btns.find(b => b.innerText && b.innerText.includes('Thêm dòng mới'));
            if (addBtn) addBtn.click();
        });
        await sleep(800);

        // Fill item 2 values
        await page.evaluate(() => {
            const table = document.querySelector('.tf-invoice-items-table');
            const rows = table.querySelectorAll('tbody tr');
            if (rows.length >= 2) {
                const secondRow = rows[1];
                const inputs = secondRow.querySelectorAll('input');
                if (inputs.length >= 5) {
                    inputs[0].value = 'Bộ phụ kiện cọc bình ắc quy chuyên dụng';
                    inputs[0].dispatchEvent(new Event('input', { bubbles: true }));
                    inputs[0].dispatchEvent(new Event('change', { bubbles: true }));

                    inputs[1].value = 'Bộ';
                    inputs[1].dispatchEvent(new Event('input', { bubbles: true }));
                    inputs[1].dispatchEvent(new Event('change', { bubbles: true }));

                    inputs[2].value = '4';
                    inputs[2].dispatchEvent(new Event('input', { bubbles: true }));
                    inputs[2].dispatchEvent(new Event('change', { bubbles: true }));

                    inputs[3].value = '250000';
                    inputs[3].dispatchEvent(new Event('input', { bubbles: true }));
                    inputs[3].dispatchEvent(new Event('change', { bubbles: true }));

                    inputs[4].value = '0';
                    inputs[4].dispatchEvent(new Event('input', { bubbles: true }));
                    inputs[4].dispatchEvent(new Event('change', { bubbles: true }));
                }
            }
        });
        await sleep(1200);

        // Measure item count and summary totals
        const populatedMetrics = await page.evaluate(() => {
            const rows = document.querySelectorAll('.tf-invoice-items-table tbody tr');
            const summaryRows = Array.from(document.querySelectorAll('.tf-summary-row, .tf-summary-total-row'));
            const summaryText = summaryRows.map(r => r.innerText.replace(/\s+/g, ' ').trim());
            const badge = document.querySelector('.tf-badge-info');

            return {
                rowCount: rows.length,
                badgeText: badge ? badge.innerText : 'none',
                summaryValues: summaryText
            };
        });
        auditResults.interactive_invoice_items = populatedMetrics;
        console.log('Populated Invoice Metrics:', populatedMetrics);

        // Screenshot with items in Dark Mode
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'invoice_create_with_items_dark.png'),
            fullPage: false
        });

        // Screenshot with items in Light Mode
        await setTheme('light');
        await sleep(600);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'invoice_create_with_items_light.png'),
            fullPage: false
        });

        // Write audit results
        const jsonPath = path.join(ARTIFACT_DIR, 'invoice_rebalance_verification_results.json');
        fs.writeFileSync(jsonPath, JSON.stringify(auditResults, null, 2), 'utf-8');
        console.log(`Audit results successfully written to ${jsonPath}`);

    } catch (err) {
        console.error('VERIFICATION ERROR:', err);
    } finally {
        await browser.close();
        console.log('--- INVOICE REBALANCE VERIFICATION FINISHED ---');
    }
})();
