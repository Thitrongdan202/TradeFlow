const puppeteer = require('puppeteer');
const fs = require('fs');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    console.log('=== TEST 1: NV01 (EMPLOYEE) ===');
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'nv01');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    console.log('nv01 logged in successfully. URL:', page.url());

    // Inspect visible sidebar items
    const nv01Sidebar = await page.evaluate(() => {
        return Array.from(document.querySelectorAll('.tf-nav-item')).map(it => {
            const label = it.querySelector('.tf-nav-label')?.textContent.trim();
            const subLinks = Array.from(it.querySelectorAll('.tf-nav-sub-link, .tf-flyout-link')).map(l => l.textContent.trim());
            const hasChevron = !!it.querySelector('.tf-nav-chevron');
            return { label, hasChevron, subLinksCount: subLinks.length, subLinks: subLinks.slice(0, 3) };
        });
    });
    console.log('nv01 visible sidebar items:', JSON.stringify(nv01Sidebar, null, 2));

    // Test POS access for nv01
    await page.goto('http://127.0.0.1:5169/ban-hang', { waitUntil: 'networkidle2' });
    await delay(1000);
    console.log('nv01 on /ban-hang URL:', page.url());

    // Check headers count on /ban-hang
    const headerCount = await page.evaluate(() => document.querySelectorAll('header').length);
    const toolbarCount = await page.evaluate(() => document.querySelectorAll('.tf-sales-toolbar').length);
    console.log('nv01 on /ban-hang: header count in DOM =', headerCount, ', .tf-sales-toolbar count =', toolbarCount);

    await page.screenshot({ path: `${ARTIFACT_DIR}/rbac_01_nv01_sidebar_and_pos.png` });
    console.log('Saved rbac_01_nv01_sidebar_and_pos.png');

    // Logout nv01
    await page.goto('http://127.0.0.1:5169/Account/Logout', { waitUntil: 'networkidle2' });
    const logoutBtn = await page.$('form button[type="submit"]');
    if (logoutBtn) {
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            logoutBtn.click()
        ]);
    }
    console.log('Logged out nv01');

    console.log('\n=== TEST 2: KINHDOANH01 (SALES) ===');
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'kinhdoanh01');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);

    const salesSidebar = await page.evaluate(() => {
        return Array.from(document.querySelectorAll('.tf-nav-item')).map(it => {
            const label = it.querySelector('.tf-nav-label')?.textContent.trim();
            const subLinks = Array.from(it.querySelectorAll('.tf-nav-sub-link')).map(l => l.textContent.trim());
            return { label, subLinks };
        });
    });
    console.log('kinhdoanh01 visible sidebar items:', JSON.stringify(salesSidebar, null, 2));
    await page.screenshot({ path: `${ARTIFACT_DIR}/rbac_02_sales_sidebar.png` });
    console.log('Saved rbac_02_sales_sidebar.png');

    // Logout sales
    await page.goto('http://127.0.0.1:5169/Account/Logout', { waitUntil: 'networkidle2' });
    const logoutBtn2 = await page.$('form button[type="submit"]');
    if (logoutBtn2) {
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            logoutBtn2.click()
        ]);
    }

    console.log('\n=== TEST 3: KHO01 (WAREHOUSE) ===');
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'kho01');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);

    const warehouseSidebar = await page.evaluate(() => {
        return Array.from(document.querySelectorAll('.tf-nav-item')).map(it => {
            const label = it.querySelector('.tf-nav-label')?.textContent.trim();
            const subLinks = Array.from(it.querySelectorAll('.tf-nav-sub-link')).map(l => l.textContent.trim());
            return { label, subLinks };
        });
    });
    console.log('kho01 visible sidebar items:', JSON.stringify(warehouseSidebar, null, 2));
    await page.screenshot({ path: `${ARTIFACT_DIR}/rbac_03_warehouse_sidebar.png` });
    console.log('Saved rbac_03_warehouse_sidebar.png');

    // Logout warehouse
    await page.goto('http://127.0.0.1:5169/Account/Logout', { waitUntil: 'networkidle2' });
    const logoutBtn3 = await page.$('form button[type="submit"]');
    if (logoutBtn3) {
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            logoutBtn3.click()
        ]);
    }

    console.log('\n=== TEST 4: ADMIN (OWNER / ROOT) ===');
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'admin');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    console.log('admin logged in. Testing Role Management at /cai-dat/vai-tro');

    await page.goto('http://127.0.0.1:5169/cai-dat/vai-tro', { waitUntil: 'networkidle2' });
    await delay(1200);

    const rolesTable = await page.evaluate(() => {
        return Array.from(document.querySelectorAll('.tf-table tbody tr')).map(tr => {
            return Array.from(tr.querySelectorAll('td')).map(td => td.innerText.replace(/\n/g, ' ').trim());
        });
    });
    console.log('Roles table:', JSON.stringify(rolesTable, null, 2));
    await page.screenshot({ path: `${ARTIFACT_DIR}/rbac_04_admin_roles_list.png` });
    console.log('Saved rbac_04_admin_roles_list.png');

    // Test editing role / matrix
    const editLink = await page.$('a[href*="/cai-dat/vai-tro/sua/"]');
    if (editLink) {
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            editLink.click()
        ]);
        await delay(1000);
        console.log('Navigated to role edit. URL:', page.url());
        await page.screenshot({ path: `${ARTIFACT_DIR}/rbac_05_admin_role_matrix.png` });
        console.log('Saved rbac_05_admin_role_matrix.png');
    }

    // Test Audit Log
    console.log('\n=== TEST 5: AUDIT LOG AT /cai-dat/nhat-ky-he-thong ===');
    await page.goto('http://127.0.0.1:5169/cai-dat/nhat-ky-he-thong', { waitUntil: 'networkidle2' });
    await delay(1200);

    const kpiValues = await page.evaluate(() => {
        return Array.from(document.querySelectorAll('.tf-card div[style*="font-size:24px"]')).map(el => el.textContent.trim());
    });
    console.log('Audit Log KPIs (Total, Today, Pending, Security):', kpiValues);

    const tabsList = await page.evaluate(() => {
        return Array.from(document.querySelectorAll('button[style*="border-bottom:2px"]')).map(btn => btn.innerText.replace(/\n/g, ' ').trim());
    });
    console.log('Audit Category Tabs:', tabsList);

    await page.screenshot({ path: `${ARTIFACT_DIR}/rbac_06_audit_log_kpi_tabs.png` });
    console.log('Saved rbac_06_audit_log_kpi_tabs.png');

    // Click first row to test Detail Modal
    const firstRow = await page.$('.tf-audit-row');
    if (firstRow) {
        await firstRow.click();
        await delay(800);
        console.log('Clicked audit row, checking modal...');
        await page.screenshot({ path: `${ARTIFACT_DIR}/rbac_07_audit_log_detail_modal.png` });
        console.log('Saved rbac_07_audit_log_detail_modal.png');

        // Close modal
        const closeBtn = await page.$('button[style*="font-size:20px"]');
        if (closeBtn) await closeBtn.click();
        await delay(400);
    }

    // Test Notification Menu
    console.log('\n=== TEST 6: NOTIFICATION MENU ===');
    const notifBtn = await page.$('#tf-notification-menu-btn');
    if (notifBtn) {
        await notifBtn.click();
        await delay(800);
        const notifItems = await page.evaluate(() => {
            return Array.from(document.querySelectorAll('.tf-notif-item')).map(item => {
                const title = item.querySelector('.tf-notif-item-title')?.textContent.trim();
                const desc = item.querySelector('.tf-notif-item-desc')?.textContent.trim();
                return { title, desc };
            });
        });
        console.log('Notifications in menu:', JSON.stringify(notifItems.slice(0, 5), null, 2));
        await page.screenshot({ path: `${ARTIFACT_DIR}/rbac_08_notifications_approvals.png` });
        console.log('Saved rbac_08_notifications_approvals.png');
    }

    await browser.close();
    console.log('\n=== ALL TESTS COMPLETED SUCCESSFULLY! ===');
})();
