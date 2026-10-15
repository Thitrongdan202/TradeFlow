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
        const emptyRightSpace = cRect ? Math.max(0, Math.round(viewportWidth - cRect.right)) : 0;
        const docScrollWidth = document.documentElement.scrollWidth;
        const hasHorizontalScrollbar = docScrollWidth > viewportWidth;

        return {
            label,
            viewportWidth,
            docScrollWidth,
            hasHorizontalScrollbar,
            sidebar: sRect ? { width: Math.round(sRect.width), right: Math.round(sRect.right) } : null,
            mainWrapper: wRect ? { width: Math.round(wRect.width), left: Math.round(wRect.left), right: Math.round(wRect.right) } : null,
            topbar: tRect ? { width: Math.round(tRect.width), right: Math.round(tRect.right) } : null,
            content: cRect ? {
                width: Math.round(cRect.width),
                left: Math.round(cRect.left),
                right: Math.round(cRect.right),
                computedMaxWidth: window.getComputedStyle(content).maxWidth
            } : null,
            emptyRightSpace
        };
    }, label);
}

async function setSidebarState(page, shouldBeCollapsed) {
    const isCurrentlyCollapsed = await page.evaluate(() => {
        const sidebar = document.querySelector('.tf-sidebar');
        return sidebar ? sidebar.classList.contains('is-collapsed') : false;
    });

    if (isCurrentlyCollapsed !== shouldBeCollapsed) {
        await page.click('.tf-sidebar-toggle');
        await sleep(350); // wait for 0.22s transition to finish
    }
}

async function run() {
    console.log('=== VERIFYING SIDEBAR COLLAPSE GLOBAL CONTENT WIDTH FIX ===\n');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    const results = [];

    // Login
    await page.setViewport({ width: 1706, height: 900 });
    await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
    await page.waitForSelector('#login-username');
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    await sleep(600);

    // 1. Dashboard at 1706px - Expanded
    await setSidebarState(page, false);
    const dashExp1706 = await inspectLayout(page, 'Dashboard Expanded (1706px)');
    results.push(dashExp1706);
    console.log('1. Dashboard Expanded (1706px):', dashExp1706);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'fixed_01_dashboard_expanded_1706.png') });

    // 2. Dashboard at 1706px - Collapsed
    await setSidebarState(page, true);
    const dashCol1706 = await inspectLayout(page, 'Dashboard Collapsed (1706px)');
    results.push(dashCol1706);
    console.log('2. Dashboard Collapsed (1706px):', dashCol1706);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'fixed_02_dashboard_collapsed_1706.png') });

    // 3. Dashboard at 1920px (Full HD) - Collapsed
    await page.setViewport({ width: 1920, height: 1080 });
    await sleep(300);
    const dashCol1920 = await inspectLayout(page, 'Dashboard Collapsed (1920px Full HD)');
    results.push(dashCol1920);
    console.log('3. Dashboard Collapsed (1920px):', dashCol1920);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'fixed_03_dashboard_collapsed_1920.png') });

    // Reset viewport back to 1706px for representative page tests
    await page.setViewport({ width: 1706, height: 900 });
    await sleep(200);

    // 4. Customers Page (/doi-tac/khach-hang) - Expanded & Collapsed
    await page.goto(`${BASE_URL}/doi-tac/khach-hang`, { waitUntil: 'networkidle2' });
    await sleep(600);
    await setSidebarState(page, false);
    const custExp = await inspectLayout(page, 'Customers Expanded (1706px)');
    results.push(custExp);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'fixed_04_customers_expanded_1706.png') });

    await setSidebarState(page, true);
    const custCol = await inspectLayout(page, 'Customers Collapsed (1706px)');
    results.push(custCol);
    console.log('4. Customers Collapsed (1706px):', custCol);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'fixed_05_customers_collapsed_1706.png') });

    // 5. Products Page (/danh-muc/san-pham) - Collapsed
    await page.goto(`${BASE_URL}/danh-muc/san-pham`, { waitUntil: 'networkidle2' });
    await sleep(600);
    const prodCol = await inspectLayout(page, 'Products Collapsed (1706px)');
    results.push(prodCol);
    console.log('5. Products Collapsed (1706px):', prodCol);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'fixed_06_products_collapsed_1706.png') });

    // 6. Invoices Page (/hoa-don) - Collapsed
    await page.goto(`${BASE_URL}/hoa-don`, { waitUntil: 'networkidle2' });
    await sleep(600);
    const invCol = await inspectLayout(page, 'Invoices Collapsed (1706px)');
    results.push(invCol);
    console.log('6. Invoices Collapsed (1706px):', invCol);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'fixed_07_invoices_collapsed_1706.png') });

    // 7. Contracts Page (/hop-dong) - Collapsed
    await page.goto(`${BASE_URL}/hop-dong`, { waitUntil: 'networkidle2' });
    await sleep(600);
    const contCol = await inspectLayout(page, 'Contracts Collapsed (1706px)');
    results.push(contCol);
    console.log('7. Contracts Collapsed (1706px):', contCol);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'fixed_08_contracts_collapsed_1706.png') });

    // 8. POS Page (/ban-hang) - Collapsed
    await page.goto(`${BASE_URL}/ban-hang`, { waitUntil: 'networkidle2' });
    await sleep(600);
    const posCol = await inspectLayout(page, 'POS Collapsed (1706px)');
    results.push(posCol);
    console.log('8. POS Collapsed (1706px):', posCol);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'fixed_09_pos_collapsed_1706.png') });

    // 9. Debt Page (/doi-tac/cong-no) - Collapsed
    await page.goto(`${BASE_URL}/doi-tac/cong-no`, { waitUntil: 'networkidle2' });
    await sleep(600);
    const debtCol = await inspectLayout(page, 'Debt Collapsed (1706px)');
    results.push(debtCol);
    console.log('9. Debt Collapsed (1706px):', debtCol);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'fixed_10_debt_collapsed_1706.png') });

    // 10. Warehouses Page (/kho/kho-hang) - Collapsed
    await page.goto(`${BASE_URL}/kho/kho-hang`, { waitUntil: 'networkidle2' });
    await sleep(600);
    const whCol = await inspectLayout(page, 'Warehouses Collapsed (1706px)');
    results.push(whCol);
    console.log('10. Warehouses Collapsed (1706px):', whCol);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'fixed_11_warehouses_collapsed_1706.png') });

    // 11. Documents Page (/tai-lieu) - Collapsed
    await page.goto(`${BASE_URL}/tai-lieu`, { waitUntil: 'networkidle2' });
    await sleep(600);
    const docCol = await inspectLayout(page, 'Documents Collapsed (1706px)');
    results.push(docCol);
    console.log('11. Documents Collapsed (1706px):', docCol);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'fixed_12_documents_collapsed_1706.png') });

    // 12. Settings Page (/cai-dat) - Collapsed
    await page.goto(`${BASE_URL}/cai-dat`, { waitUntil: 'networkidle2' });
    await sleep(600);
    const settCol = await inspectLayout(page, 'Settings Collapsed (1706px)');
    results.push(settCol);
    console.log('12. Settings Collapsed (1706px):', settCol);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'fixed_13_settings_collapsed_1706.png') });

    // 13. Rapid Toggling Stress Test (5 cycles)
    console.log('\n--- Running Rapid Toggling Stress Test (5 cycles) ---');
    let toggleFailures = 0;
    for (let i = 1; i <= 5; i++) {
        await page.click('.tf-sidebar-toggle');
        await sleep(250);
        await page.click('.tf-sidebar-toggle');
        await sleep(250);
    }
    // Final check after 5 cycles
    await setSidebarState(page, true);
    await sleep(350);
    const toggleCheck = await inspectLayout(page, 'Rapid Toggling Final Check (Collapsed)');
    console.log('Rapid Toggle Final Result:', toggleCheck);
    if (toggleCheck.emptyRightSpace !== 0 || toggleCheck.hasHorizontalScrollbar) {
        toggleFailures++;
    }

    // 14. Responsive Breakpoint Tests: 1440px, 1366px, 768px (Tablet)
    console.log('\n--- Testing Additional Viewports ---');
    // 1440px
    await page.setViewport({ width: 1440, height: 900 });
    await sleep(300);
    const vp1440 = await inspectLayout(page, 'Viewport 1440px Collapsed');
    results.push(vp1440);
    console.log('Viewport 1440px:', vp1440);

    // 1366px
    await page.setViewport({ width: 1366, height: 768 });
    await sleep(300);
    const vp1366 = await inspectLayout(page, 'Viewport 1366px Collapsed');
    results.push(vp1366);
    console.log('Viewport 1366px:', vp1366);

    // 768px (Tablet drawer mode)
    await page.setViewport({ width: 768, height: 1024 });
    await sleep(300);
    const vp768 = await inspectLayout(page, 'Viewport 768px Tablet Drawer');
    results.push(vp768);
    console.log('Viewport 768px:', vp768);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'fixed_14_tablet_768.png') });

    // Write full results to JSON artifact
    fs.writeFileSync(
        path.join(ARTIFACT_DIR, 'width_fix_verification_results.json'),
        JSON.stringify({ results, toggleFailures }, null, 2)
    );

    console.log('\n=== VERIFICATION COMPLETE: ALL DATA SAVED TO ARTIFACT DIR ===');
    await browser.close();
}

run().catch(err => {
    console.error('Test script error:', err);
    process.exit(1);
});
