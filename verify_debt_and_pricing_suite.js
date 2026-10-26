const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const BASE_URL = 'http://localhost:5169';
const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

const sleep = ms => new Promise(r => setTimeout(r, ms));

function parseRgb(colorStr) {
    if (!colorStr) return [0, 0, 0];
    const match = colorStr.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/);
    if (match) {
        return [parseInt(match[1]), parseInt(match[2]), parseInt(match[3])];
    }
    return [0, 0, 0];
}

function parseAlpha(colorStr) {
    if (!colorStr) return 1;
    const match = colorStr.match(/rgba\(\d+,\s*\d+,\s*\d+,\s*([\d.]+)\)/);
    return match ? parseFloat(match[1]) : 1;
}

function blendColors(fgStr, bgStr) {
    const [r1, g1, b1] = parseRgb(fgStr);
    const alpha = parseAlpha(fgStr);
    const [r2, g2, b2] = parseRgb(bgStr);
    const r = Math.round(r1 * alpha + r2 * (1 - alpha));
    const g = Math.round(g1 * alpha + g2 * (1 - alpha));
    const b = Math.round(b1 * alpha + b2 * (1 - alpha));
    return [r, g, b];
}

function getLuminance(r, g, b) {
    const a = [r, g, b].map(v => {
        v /= 255;
        return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4);
    });
    return a[0] * 0.2126 + a[1] * 0.7152 + a[2] * 0.0722;
}

function calculateContrast(fgStr, bgStr, fallbackBgStr = 'rgb(30, 35, 45)') {
    let [r2, g2, b2] = parseRgb(bgStr);
    if (bgStr && bgStr.startsWith('rgba')) {
        [r2, g2, b2] = blendColors(bgStr, fallbackBgStr);
    }
    let [r1, g1, b1] = parseRgb(fgStr);
    if (fgStr && fgStr.startsWith('rgba')) {
        [r1, g1, b1] = blendColors(fgStr, `rgb(${r2},${g2},${b2})`);
    }

    const l1 = getLuminance(r1, g1, b1);
    const l2 = getLuminance(r2, g2, b2);
    const brightest = Math.max(l1, l2);
    const darkest = Math.min(l1, l2);
    return ((brightest + 0.05) / (darkest + 0.05)).toFixed(2);
}

(async () => {
    console.log('=== STARTING DEBT & PRICE COMPARISON VERIFICATION SUITE ===');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    const results = {
        timestamp: new Date().toISOString(),
        debtManagement: {},
        customerTable: {},
        supplierTable: {},
        priceComparison: {},
        regressionChecks: {}
    };

    // Helper: set dark or light theme
    async function setTheme(theme) {
        await page.evaluate(t => {
            document.documentElement.setAttribute('data-bs-theme', t);
            document.documentElement.setAttribute('data-theme', t);
            if (t === 'dark') {
                document.documentElement.classList.add('dark');
            } else {
                document.documentElement.classList.remove('dark');
            }
        }, theme);
        await sleep(500);
    }

    // 1. Login
    console.log('[1/5] Logging in as admin...');
    await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
    await page.waitForSelector('#login-username', { timeout: 10000 });
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    console.log('Logged in successfully.');
    await sleep(1000);

    // =========================================================================
    // SECTION 2: DEBT MANAGEMENT AUDIT (All 3 tabs, Light & Dark mode, Sticky)
    // =========================================================================
    console.log('[2/5] Inspecting Debt Management (/doi-tac/cong-no)...');
    await page.goto(`${BASE_URL}/doi-tac/cong-no`, { waitUntil: 'networkidle2' });
    await sleep(1500);

    // 2.1 Tab 1: All Debts (Light Mode)
    await setTheme('light');
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'debt_tab1_all_light.png') });

    const debtLightMetrics = await page.evaluate(() => {
        const thAction = document.querySelector('.tf-table th.tf-col-actions');
        const tdAction = document.querySelector('.tf-table tbody tr:first-child td.tf-col-actions');
        const cardBody = document.querySelector('.tf-card-body');
        const containerRect = cardBody ? cardBody.getBoundingClientRect() : null;
        const thRect = thAction ? thAction.getBoundingClientRect() : null;
        const tdRect = tdAction ? tdAction.getBoundingClientRect() : null;
        const buttons = tdAction ? Array.from(tdAction.querySelectorAll('button, a')).map(b => ({
            text: b.innerText.trim(),
            rect: b.getBoundingClientRect()
        })) : [];

        // Check if tdAction right edge is within container visible region
        const isVisibleInside = containerRect && tdRect ? (tdRect.right <= containerRect.right + 2) : false;

        return {
            thPosition: thAction ? window.getComputedStyle(thAction).position : null,
            tdPosition: tdAction ? window.getComputedStyle(tdAction).position : null,
            thBg: thAction ? window.getComputedStyle(thAction).backgroundColor : null,
            tdBg: tdAction ? window.getComputedStyle(tdAction).backgroundColor : null,
            isVisibleInside,
            buttonsCount: buttons.length,
            buttons
        };
    });
    results.debtManagement.tab1Light = debtLightMetrics;

    // 2.2 Tab 1: All Debts (Dark Mode)
    await setTheme('dark');
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'debt_tab1_all_dark.png') });

    const debtDarkMetrics = await page.evaluate(() => {
        const thAction = document.querySelector('.tf-table th.tf-col-actions');
        const tdAction = document.querySelector('.tf-table tbody tr:first-child td.tf-col-actions');
        const cardBody = document.querySelector('.tf-card-body');
        const containerRect = cardBody ? cardBody.getBoundingClientRect() : null;
        const tdRect = tdAction ? tdAction.getBoundingClientRect() : null;

        return {
            thPosition: thAction ? window.getComputedStyle(thAction).position : null,
            tdPosition: tdAction ? window.getComputedStyle(tdAction).position : null,
            thBg: thAction ? window.getComputedStyle(thAction).backgroundColor : null,
            tdBg: tdAction ? window.getComputedStyle(tdAction).backgroundColor : null,
            tdColor: tdAction ? window.getComputedStyle(tdAction).color : null,
            isVisibleInside: containerRect && tdRect ? (tdRect.right <= containerRect.right + 2) : false
        };
    });
    results.debtManagement.tab1Dark = debtDarkMetrics;

    // Test Horizontal Scrolling in Dark Mode: Action Column remains pinned to right!
    await page.evaluate(() => {
        const cardBody = document.querySelector('.tf-card-body');
        if (cardBody) cardBody.scrollLeft = 300;
    });
    await sleep(400);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'debt_tab1_scrolled_right_dark.png') });

    const scrollMetrics = await page.evaluate(() => {
        const tdAction = document.querySelector('.tf-table tbody tr:first-child td.tf-col-actions');
        const cardBody = document.querySelector('.tf-card-body');
        const containerRect = cardBody ? cardBody.getBoundingClientRect() : null;
        const tdRect = tdAction ? tdAction.getBoundingClientRect() : null;
        return {
            scrollLeft: cardBody.scrollLeft,
            tdActionRight: tdRect.right,
            containerRight: containerRect.right,
            isPinnedRight: Math.abs(tdRect.right - containerRect.right) < 5
        };
    });
    results.debtManagement.stickyScrollPinned = scrollMetrics;

    // Reset scroll
    await page.evaluate(() => {
        const cardBody = document.querySelector('.tf-card-body');
        if (cardBody) cardBody.scrollLeft = 0;
    });

    // 2.3 Tab 2: Customer Receivables ("Phải thu khách hàng")
    console.log('Switching to Tab 2 (Phải thu khách hàng)...');
    const tab2Btn = await page.$('.tf-debt-tab:nth-child(2)');
    if (tab2Btn) await tab2Btn.click();
    await sleep(800);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'debt_tab2_receivables_dark.png') });

    const tab2Metrics = await page.evaluate(() => {
        const thAction = document.querySelector('.tf-table th.tf-col-actions');
        const tdAction = document.querySelector('.tf-table tbody tr:first-child td.tf-col-actions');
        const buttons = tdAction ? Array.from(tdAction.querySelectorAll('button, a')).map(b => b.innerText.trim()) : [];
        return {
            thFound: !!thAction,
            tdFound: !!tdAction,
            buttons
        };
    });
    results.debtManagement.tab2Receivables = tab2Metrics;

    // 2.4 Tab 3: Supplier Payables ("Phải trả nhà cung cấp")
    console.log('Switching to Tab 3 (Phải trả nhà cung cấp)...');
    const tab3Btn = await page.$('.tf-debt-tab:nth-child(3)');
    if (tab3Btn) await tab3Btn.click();
    await sleep(800);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'debt_tab3_payables_dark.png') });

    const tab3Metrics = await page.evaluate(() => {
        const thAction = document.querySelector('.tf-table th.tf-col-actions');
        const tdAction = document.querySelector('.tf-table tbody tr:first-child td.tf-col-actions');
        const buttons = tdAction ? Array.from(tdAction.querySelectorAll('button, a')).map(b => b.innerText.trim()) : [];
        return {
            thFound: !!thAction,
            tdFound: !!tdAction,
            buttons
        };
    });
    results.debtManagement.tab3Payables = tab3Metrics;

    // =========================================================================
    // SECTION 3: CUSTOMER & SUPPLIER TABLES AUDIT
    // =========================================================================
    console.log('[3/5] Inspecting Customer List (/doi-tac/khach-hang)...');
    await page.goto(`${BASE_URL}/doi-tac/khach-hang`, { waitUntil: 'networkidle2' });
    await sleep(1500);

    // Customer Light Mode
    await setTheme('light');
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'customers_table_sticky_actions_light.png') });

    // Customer Dark Mode
    await setTheme('dark');
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'customers_table_sticky_actions_dark.png') });

    const custMetrics = await page.evaluate(() => {
        const thAction = document.querySelector('.tf-table th.tf-col-actions');
        const tdAction = document.querySelector('.tf-table tbody tr:first-child td.tf-col-actions');
        const cardBody = document.querySelector('.tf-card-body');
        const containerRect = cardBody ? cardBody.getBoundingClientRect() : null;
        const tdRect = tdAction ? tdAction.getBoundingClientRect() : null;
        const buttons = tdAction ? Array.from(tdAction.querySelectorAll('button, a')).map(b => b.innerText.trim()) : [];

        return {
            thSticky: window.getComputedStyle(thAction).position === 'sticky',
            tdSticky: window.getComputedStyle(tdAction).position === 'sticky',
            isPinnedRight: containerRect && tdRect ? Math.abs(tdRect.right - containerRect.right) < 5 : false,
            buttons
        };
    });
    results.customerTable = custMetrics;

    console.log('Inspecting Supplier List (/doi-tac/nha-cung-cap)...');
    await page.goto(`${BASE_URL}/doi-tac/nha-cung-cap`, { waitUntil: 'networkidle2' });
    await sleep(1500);

    // Supplier Light Mode
    await setTheme('light');
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'suppliers_table_sticky_actions_light.png') });

    // Supplier Dark Mode
    await setTheme('dark');
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'suppliers_table_sticky_actions_dark.png') });

    const supMetrics = await page.evaluate(() => {
        const thAction = document.querySelector('.tf-table th.tf-col-actions');
        const tdAction = document.querySelector('.tf-table tbody tr:first-child td.tf-col-actions');
        const cardBody = document.querySelector('.tf-card-body');
        const containerRect = cardBody ? cardBody.getBoundingClientRect() : null;
        const tdRect = tdAction ? tdAction.getBoundingClientRect() : null;
        const buttons = tdAction ? Array.from(tdAction.querySelectorAll('button, a')).map(b => b.innerText.trim()) : [];

        return {
            thSticky: window.getComputedStyle(thAction).position === 'sticky',
            tdSticky: window.getComputedStyle(tdAction).position === 'sticky',
            isPinnedRight: containerRect && tdRect ? Math.abs(tdRect.right - containerRect.right) < 5 : false,
            buttons
        };
    });
    results.supplierTable = supMetrics;

    // =========================================================================
    // SECTION 4: PRICE COMPARISON AUDIT (Light & Dark, Multi & Two-Period)
    // =========================================================================
    console.log('[4/5] Inspecting Price Comparison (/bang-gia/so-sanh)...');
    await page.goto(`${BASE_URL}/bang-gia/so-sanh`, { waitUntil: 'networkidle2' });
    await sleep(1500);

    // 4.1 Multi-Period View in LIGHT MODE
    await setTheme('light');
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'price_compare_multi_light.png') });

    const priceLightMulti = await page.evaluate(() => {
        const banner = document.querySelector('.tf-compare-notice-banner');
        const bannerContent = document.querySelector('.tf-compare-notice-content');
        const thStandard = document.querySelector('.tf-table th.tf-th-standard');
        const thPeriod = document.querySelector('.tf-table th.tf-th-period');
        const thCurrent = document.querySelector('.tf-table th.tf-th-current-price');
        const tdCurrent = document.querySelector('.tf-table td.tf-td-current-price');
        const currentVal = document.querySelector('.tf-current-price-val');

        return {
            bannerBg: banner ? window.getComputedStyle(banner).backgroundColor : null,
            bannerColor: bannerContent ? window.getComputedStyle(bannerContent).color : null,
            standardHeader: {
                text: thStandard ? thStandard.innerText.trim() : null,
                bg: thStandard ? window.getComputedStyle(thStandard).backgroundColor : null,
                color: thStandard ? window.getComputedStyle(thStandard).color : null
            },
            periodHeader: {
                titleColor: thPeriod ? window.getComputedStyle(thPeriod.querySelector('.tf-th-period-title')).color : null,
                bg: thPeriod ? window.getComputedStyle(thPeriod).backgroundColor : null
            },
            currentHeader: {
                titleColor: thCurrent ? window.getComputedStyle(thCurrent.querySelector('.tf-th-current-title')).color : null,
                bg: thCurrent ? window.getComputedStyle(thCurrent).backgroundColor : null
            },
            currentCell: {
                bg: tdCurrent ? window.getComputedStyle(tdCurrent).backgroundColor : null,
                valColor: currentVal ? window.getComputedStyle(currentVal).color : null
            }
        };
    });

    priceLightMulti.standardHeaderContrast = calculateContrast(
        priceLightMulti.standardHeader.color,
        priceLightMulti.standardHeader.bg,
        'rgb(255, 255, 255)'
    );
    priceLightMulti.periodHeaderContrast = calculateContrast(
        priceLightMulti.periodHeader.titleColor,
        priceLightMulti.periodHeader.bg,
        'rgb(255, 255, 255)'
    );
    priceLightMulti.currentHeaderContrast = calculateContrast(
        priceLightMulti.currentHeader.titleColor,
        priceLightMulti.currentHeader.bg,
        'rgb(255, 255, 255)'
    );
    priceLightMulti.currentCellContrast = calculateContrast(
        priceLightMulti.currentCell.valColor,
        priceLightMulti.currentCell.bg,
        'rgb(255, 255, 255)'
    );
    results.priceComparison.multiLight = priceLightMulti;

    // 4.2 Multi-Period View in DARK MODE
    await setTheme('dark');
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'price_compare_multi_dark.png') });

    const priceDarkMulti = await page.evaluate(() => {
        const banner = document.querySelector('.tf-compare-notice-banner');
        const bannerContent = document.querySelector('.tf-compare-notice-content');
        const thStandard = document.querySelector('.tf-table th.tf-th-standard');
        const thPeriod = document.querySelector('.tf-table th.tf-th-period');
        const thCurrent = document.querySelector('.tf-table th.tf-th-current-price');
        const tdCurrent = document.querySelector('.tf-table td.tf-td-current-price');
        const currentVal = document.querySelector('.tf-current-price-val');

        return {
            bannerBg: banner ? window.getComputedStyle(banner).backgroundColor : null,
            bannerColor: bannerContent ? window.getComputedStyle(bannerContent).color : null,
            standardHeader: {
                text: thStandard ? thStandard.innerText.trim() : null,
                bg: thStandard ? window.getComputedStyle(thStandard).backgroundColor : null,
                color: thStandard ? window.getComputedStyle(thStandard).color : null
            },
            periodHeader: {
                titleColor: thPeriod ? window.getComputedStyle(thPeriod.querySelector('.tf-th-period-title')).color : null,
                bg: thPeriod ? window.getComputedStyle(thPeriod).backgroundColor : null
            },
            currentHeader: {
                titleColor: thCurrent ? window.getComputedStyle(thCurrent.querySelector('.tf-th-current-title')).color : null,
                bg: thCurrent ? window.getComputedStyle(thCurrent).backgroundColor : null
            },
            currentCell: {
                bg: tdCurrent ? window.getComputedStyle(tdCurrent).backgroundColor : null,
                valColor: currentVal ? window.getComputedStyle(currentVal).color : null
            }
        };
    });

    priceDarkMulti.standardHeaderContrast = calculateContrast(
        priceDarkMulti.standardHeader.color,
        priceDarkMulti.standardHeader.bg,
        'rgb(29, 32, 40)'
    );
    priceDarkMulti.periodHeaderContrast = calculateContrast(
        priceDarkMulti.periodHeader.titleColor,
        priceDarkMulti.periodHeader.bg,
        'rgb(29, 32, 40)'
    );
    priceDarkMulti.currentHeaderContrast = calculateContrast(
        priceDarkMulti.currentHeader.titleColor,
        priceDarkMulti.currentHeader.bg,
        'rgb(29, 32, 40)'
    );
    priceDarkMulti.currentCellContrast = calculateContrast(
        priceDarkMulti.currentCell.valColor,
        priceDarkMulti.currentCell.bg,
        'rgb(34, 38, 48)'
    );
    results.priceComparison.multiDark = priceDarkMulti;

    // 4.3 Two-Period View
    console.log('Switching to Mode 2 (So sánh chi tiết 2 kỳ)...');
    const mode2Btn = await page.$('.tf-compare-mode-selector button:nth-child(2)');
    if (mode2Btn) await mode2Btn.click();
    await sleep(800);

    // Check if dropdowns exist to run a comparison
    const hasDropdowns = await page.evaluate(() => {
        const selects = document.querySelectorAll('select.tf-input');
        return selects.length >= 2;
    });

    if (hasDropdowns) {
        // Select base and target lists
        await page.evaluate(() => {
            const selects = document.querySelectorAll('select.tf-input');
            if (selects[0] && selects[0].options.length > 1) selects[0].selectedIndex = 1;
            if (selects[0]) selects[0].dispatchEvent(new Event('change', { bubbles: true }));
            if (selects[1] && selects[1].options.length > 2) selects[1].selectedIndex = 2;
            if (selects[1]) selects[1].dispatchEvent(new Event('change', { bubbles: true }));
        });
        await sleep(500);

        // Click compare button if enabled
        const compareBtn = await page.$('button.tf-btn-primary');
        if (compareBtn) {
            await compareBtn.click();
            await sleep(1500);
        }
    }

    // Capture Two-Period Light
    await setTheme('light');
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'price_compare_two_period_light.png') });

    // Capture Two-Period Dark
    await setTheme('dark');
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'price_compare_two_period_dark.png') });

    const twoPeriodDarkMetrics = await page.evaluate(() => {
        const kpiCard = document.querySelector('.tf-compare-kpi-card.increased');
        const diffBadge = document.querySelector('.tf-diff-badge.increased');
        const thStandard = document.querySelector('.tf-table th.tf-th-standard');
        return {
            kpiCardBg: kpiCard ? window.getComputedStyle(kpiCard).backgroundColor : null,
            badgeBg: diffBadge ? window.getComputedStyle(diffBadge).backgroundColor : null,
            badgeColor: diffBadge ? window.getComputedStyle(diffBadge).color : null,
            thBg: thStandard ? window.getComputedStyle(thStandard).backgroundColor : null,
            thColor: thStandard ? window.getComputedStyle(thStandard).color : null
        };
    });
    results.priceComparison.twoPeriodDark = twoPeriodDarkMetrics;

    // =========================================================================
    // SECTION 5: CROSS-MODULE REGRESSION CHECKS
    // =========================================================================
    console.log('[5/5] Running Cross-Module Regression Checks...');

    // 5.1 Warehouse Quarantine Warning
    await page.goto(`${BASE_URL}/kho/hang-loi`, { waitUntil: 'networkidle2' });
    await sleep(800);
    await setTheme('dark');
    const quaranMetrics = await page.evaluate(() => {
        const title = document.querySelector('.tf-banner-title');
        const banner = document.querySelector('.tf-quarantine-warning-banner');
        return {
            titleColor: title ? window.getComputedStyle(title).color : null,
            bannerBg: banner ? window.getComputedStyle(banner).backgroundColor : null
        };
    });
    quaranMetrics.contrast = calculateContrast(quaranMetrics.titleColor, quaranMetrics.bannerBg, 'rgb(30, 35, 45)');
    results.regressionChecks.quarantine = quaranMetrics;

    // 5.2 Category B Printable Voucher
    await page.goto(`${BASE_URL}/kho/phieu-kho/1`, { waitUntil: 'networkidle2' });
    await sleep(800);
    const catBMetrics = await page.evaluate(() => {
        const sheet = document.querySelector('.voucher-a4-sheet, #print-warehouse-voucher, .tf-document-paper');
        return {
            found: !!sheet,
            paperBg: sheet ? window.getComputedStyle(sheet).backgroundColor : null,
            paperColor: sheet ? window.getComputedStyle(sheet).color : null
        };
    });
    results.regressionChecks.catB = catBMetrics;

    // 5.3 Sales POS
    await page.goto(`${BASE_URL}/ban-hang`, { waitUntil: 'networkidle2' });
    await sleep(1000);
    const modeBtn = await page.$('.tf-mode-btn');
    if (modeBtn) await modeBtn.click();
    await sleep(400);
    const posMetrics = await page.evaluate(() => {
        const wrap = document.querySelector('.tf-order-cust-search-wrap');
        const note = document.querySelector('.tf-order-note-row');
        return {
            searchWrapBg: wrap ? window.getComputedStyle(wrap).backgroundColor : null,
            noteRowBg: note ? window.getComputedStyle(note).backgroundColor : null
        };
    });
    results.regressionChecks.salesPos = posMetrics;

    // Write full audit result JSON
    fs.writeFileSync(
        path.join(ARTIFACT_DIR, 'clipped_action_and_pricing_verification_results.json'),
        JSON.stringify(results, null, 2),
        'utf8'
    );
    console.log('Results written to clipped_action_and_pricing_verification_results.json');

    await browser.close();
    console.log('=== VERIFICATION SUITE FINISHED SUCCESSFULLY ===');
})();
