const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const BASE_URL = 'http://localhost:5169';
const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

const sleep = ms => new Promise(r => setTimeout(r, ms));

// Helper: Relative luminance and contrast ratio according to WCAG 2.1
function parseRgb(colorStr) {
    if (!colorStr) return [0, 0, 0];
    const match = colorStr.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/);
    if (match) {
        return [parseInt(match[1]), parseInt(match[2]), parseInt(match[3])];
    }
    return [0, 0, 0];
}

function getLuminance(r, g, b) {
    const a = [r, g, b].map(v => {
        v /= 255;
        return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4);
    });
    return a[0] * 0.2126 + a[1] * 0.7152 + a[2] * 0.0722;
}

function calculateContrast(fgStr, bgStr) {
    const [r1, g1, b1] = parseRgb(fgStr);
    const [r2, g2, b2] = parseRgb(bgStr);
    const l1 = getLuminance(r1, g1, b1);
    const l2 = getLuminance(r2, g2, b2);
    const brightest = Math.max(l1, l2);
    const darkest = Math.min(l1, l2);
    return ((brightest + 0.05) / (darkest + 0.05)).toFixed(2);
}

(async () => {
    console.log('--- STARTING WAREHOUSE CONTRAST & READABILITY AUDIT SUITE ---');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });
    const auditResults = {
        timestamp: new Date().toISOString(),
        screens: {},
        crossModuleRegression: {}
    };

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
        await sleep(1500);

        // 2. Quarantine Screen: Warning Alert & Status Tabs
        console.log('[2/5] Inspecting Quarantine Screen (/kho/hang-loi)...');
        await page.goto(`${BASE_URL}/kho/hang-loi`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(1000);

        const quarantineMetrics = await page.evaluate(() => {
            const banner = document.querySelector('.tf-quarantine-warning-banner');
            const title = banner ? banner.querySelector('.tf-banner-title') : null;
            const body = banner ? banner.querySelector('.tf-banner-text') : null;
            const icon = banner ? banner.querySelector('svg') : null;

            const bannerBg = banner ? window.getComputedStyle(banner).backgroundColor : '';
            const bannerBorder = banner ? window.getComputedStyle(banner).borderColor : '';
            const titleColor = title ? window.getComputedStyle(title).color : '';
            const bodyColor = body ? window.getComputedStyle(body).color : '';
            const iconColor = icon ? window.getComputedStyle(icon).color : '';

            const navLinks = Array.from(document.querySelectorAll('.tf-warehouse-nav-container .tf-nav-tab')).map(a => ({
                text: a.innerText.trim(),
                isActive: a.classList.contains('is-active'),
                color: window.getComputedStyle(a).color,
                bg: window.getComputedStyle(a).backgroundColor
            }));

            const segTabs = Array.from(document.querySelectorAll('.tf-segmented-tab')).map(t => {
                const badge = t.querySelector('span:last-child');
                return {
                    label: t.innerText.split('\n')[0].trim(),
                    isActive: t.classList.contains('is-active'),
                    btnColor: window.getComputedStyle(t).color,
                    btnBg: window.getComputedStyle(t).backgroundColor,
                    badgeText: badge ? badge.innerText.trim() : '',
                    badgeColor: badge ? window.getComputedStyle(badge).color : '',
                    badgeBg: badge ? window.getComputedStyle(badge).backgroundColor : ''
                };
            });

            return {
                banner: { bannerBg, bannerBorder, titleColor, bodyColor, iconColor },
                navLinks,
                segTabs
            };
        });

        // Compute contrast for Quarantine elements
        quarantineMetrics.banner.titleContrast = calculateContrast(quarantineMetrics.banner.titleColor, '#27231c');
        quarantineMetrics.banner.bodyContrast = calculateContrast(quarantineMetrics.banner.bodyColor, '#27231c');
        quarantineMetrics.banner.iconContrast = calculateContrast(quarantineMetrics.banner.iconColor, '#27231c');

        quarantineMetrics.segTabs.forEach(t => {
            t.btnContrast = calculateContrast(t.btnColor, t.isActive ? 'rgb(37, 43, 54)' : 'rgb(23, 25, 31)');
            t.badgeContrast = calculateContrast(t.badgeColor, t.badgeBg);
        });

        auditResults.screens.quarantine = quarantineMetrics;
        console.log('Quarantine Metrics (Dark Mode):', quarantineMetrics.banner);

        // Screenshot Quarantine Dark
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'warehouse_quarantine_alert_and_tabs_dark.png'),
            fullPage: false
        });

        // Screenshot Quarantine Light
        await setTheme('light');
        await sleep(600);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'warehouse_quarantine_alert_and_tabs_light.png'),
            fullPage: false
        });

        // 3. Stocktaking Screen (/kho/kiem-kho)
        console.log('[3/5] Inspecting Stocktaking Screen (/kho/kiem-kho)...');
        await page.goto(`${BASE_URL}/kho/kiem-kho`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(1000);

        const stocktakeMetrics = await page.evaluate(() => {
            const segTabs = Array.from(document.querySelectorAll('.tf-segmented-tab')).map(t => {
                const badge = t.querySelector('span:last-child');
                return {
                    label: t.innerText.split('\n')[0].trim(),
                    isActive: t.classList.contains('is-active'),
                    btnColor: window.getComputedStyle(t).color,
                    btnBg: window.getComputedStyle(t).backgroundColor,
                    badgeText: badge ? badge.innerText.trim() : '',
                    badgeColor: badge ? window.getComputedStyle(badge).color : '',
                    badgeBg: badge ? window.getComputedStyle(badge).backgroundColor : ''
                };
            });
            return { segTabs };
        });

        stocktakeMetrics.segTabs.forEach(t => {
            t.btnContrast = calculateContrast(t.btnColor, t.isActive ? 'rgb(37, 43, 54)' : 'rgb(23, 25, 31)');
            t.badgeContrast = calculateContrast(t.badgeColor, t.badgeBg);
        });
        auditResults.screens.stocktake = stocktakeMetrics;

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'warehouse_stocktake_status_tabs_dark.png'),
            fullPage: false
        });

        await setTheme('light');
        await sleep(600);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'warehouse_stocktake_status_tabs_light.png'),
            fullPage: false
        });

        // 4. Inspect Stock List & Matrix Tabs (/kho/ton-kho)
        console.log('[4/5] Inspecting Stock List & Other Screens...');
        await page.goto(`${BASE_URL}/kho/ton-kho`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(1000);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'warehouse_stock_matrix_tabs_dark.png'),
            fullPage: false
        });

        // Voucher List Tabs (/kho/phieu-kho)
        await page.goto(`${BASE_URL}/kho/phieu-kho`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(800);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'warehouse_voucher_type_tabs_dark.png'),
            fullPage: false
        });

        // Returns List Tabs (/kho/hang-tra)
        await page.goto(`${BASE_URL}/kho/hang-tra`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(800);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'warehouse_returns_status_tabs_dark.png'),
            fullPage: false
        });

        // Movement Ledger (/kho/bien-dong)
        await page.goto(`${BASE_URL}/kho/bien-dong`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(800);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'warehouse_movement_ledger_dark.png'),
            fullPage: false
        });

        // Check Category B Voucher Printable Document (/kho/phieu-kho/1)
        await page.goto(`${BASE_URL}/kho/phieu-kho/1`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(1000);

        const voucherPrintMetrics = await page.evaluate(() => {
            const paper = document.querySelector('.tf-document-paper, .voucher-a4-sheet');
            return {
                found: !!paper,
                paperBg: paper ? window.getComputedStyle(paper).backgroundColor : '',
                paperColor: paper ? window.getComputedStyle(paper).color : ''
            };
        });
        auditResults.screens.voucherPrintCategoryB = voucherPrintMetrics;

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'warehouse_cat_b_voucher_print_dark.png'),
            fullPage: false
        });

        // 5. Cross-Module Regression Audit
        console.log('[5/5] Performing Cross-Module Regression Audit...');
        // A. Sales/POS
        await page.goto(`${BASE_URL}/ban-hang`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(1500);
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-mode-btn, .tf-pos-mode-btn, button'));
            const regBtn = btns.find(b => b.innerText && b.innerText.includes('Bán thường'));
            if (regBtn) regBtn.click();
        });
        await sleep(1200);
        const salesMetrics = await page.evaluate(() => {
            const searchWrap = document.querySelector('.tf-order-cust-search-wrap');
            const noteRow = document.querySelector('.tf-order-note-input-row');
            return {
                searchWrapBg: searchWrap ? window.getComputedStyle(searchWrap).backgroundColor : '',
                noteRowBg: noteRow ? window.getComputedStyle(noteRow).backgroundColor : ''
            };
        });
        auditResults.crossModuleRegression.salesPos = salesMetrics;

        // B. Invoice Creation Form
        await page.goto(`${BASE_URL}/hoa-don/tao-moi`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(1000);
        const invoiceMetrics = await page.evaluate(() => {
            const configGrid = document.querySelector('.tf-invoice-config-grid');
            const notesCard = document.querySelector('.tf-notes-card');
            const summaryCard = document.querySelector('.tf-summary-card');
            return {
                configGridFound: !!configGrid,
                heightDifference: (notesCard && summaryCard) ? Math.abs(notesCard.offsetHeight - summaryCard.offsetHeight) : 0
            };
        });
        auditResults.crossModuleRegression.invoiceCreate = invoiceMetrics;

        // Write audit results
        const jsonPath = path.join(ARTIFACT_DIR, 'warehouse_contrast_verification_results.json');
        fs.writeFileSync(jsonPath, JSON.stringify(auditResults, null, 2), 'utf-8');
        console.log(`Audit results successfully written to ${jsonPath}`);

    } catch (err) {
        console.error('VERIFICATION ERROR:', err);
    } finally {
        await browser.close();
        console.log('--- WAREHOUSE CONTRAST & READABILITY AUDIT SUITE FINISHED ---');
    }
})();
