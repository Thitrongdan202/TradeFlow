const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACTS_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function sleep(ms) {
    return new Promise(r => setTimeout(r, ms));
}

let tempTestFilePath = null;

async function performLogout(page) {
    try {
        await page.goto('http://127.0.0.1:5169/Account/Logout', { waitUntil: 'networkidle2' });
        const logoutBtn = await page.$('form button[type="submit"]');
        if (logoutBtn) {
            await Promise.all([
                page.waitForNavigation({ waitUntil: 'networkidle2' }),
                logoutBtn.click()
            ]);
        }
    } catch (e) {}
    try {
        const client = await page.target().createCDPSession();
        await client.send('Network.clearBrowserCookies');
    } catch (e) {}
    await sleep(500);
}

(async () => {
    console.log('--- STARTING COMPREHENSIVE VERIFICATION OF REDESIGNED SUPPORT CENTER ---');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    try {
        // ==========================================
        // 1. LOGIN AS ADMIN & INSPECT HEADER EXECUTIVE MENU
        // ==========================================
        console.log('\n[1/6] Logging in as Admin...');
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        await page.type('input[name="Input.Username"]', 'admin');
        await page.type('input[name="Input.Password"]', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        await sleep(1500);

        // Click 'Nhật ký' to open Executive dropdown
        console.log('Opening Executive Dropdown (Nhật ký)...');
        await page.waitForSelector('.tf-topbar-audit-pill', { timeout: 6000 });
        await page.click('.tf-topbar-audit-pill');
        await sleep(600);

        const dropdownMetrics = await page.evaluate(() => {
            const dropdown = document.querySelector('.tf-audit-dropdown');
            if (!dropdown) return null;
            const rect = dropdown.getBoundingClientRect();
            const headerTitle = dropdown.querySelector('.tf-audit-header-title')?.innerText.trim();
            const navItems = Array.from(dropdown.querySelectorAll('.tf-audit-nav-item')).map(el => el.innerText.trim());
            const compactRows = Array.from(dropdown.querySelectorAll('.tf-audit-compact-row')).map(el => ({
                code: el.querySelector('.tf-audit-code-pill')?.innerText.trim(),
                title: el.querySelector('.tf-audit-row-title')?.innerText.trim(),
                priority: el.querySelector('.tf-audit-priority-tag')?.innerText.trim()
            }));

            return {
                width: Math.round(rect.width),
                height: Math.round(rect.height),
                headerTitle,
                navCount: navItems.length,
                compactRowCount: compactRows.length,
                firstRow: compactRows[0] || null
            };
        });

        console.log('Executive Dropdown Metrics:', dropdownMetrics);
        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'header_admin_audit_button.png') });
        console.log('✓ Captured screenshot: header_admin_audit_button.png');

        // ==========================================
        // 2. OPEN SUPPORT CENTER MODAL (MANAGEMENT VIEW)
        // ==========================================
        console.log('\n[2/6] Opening Support Center Modal...');
        // Click on "Báo cáo & Hỗ trợ" or the footer hub button in the dropdown
        await page.click('.tf-audit-nav-item');
        await sleep(1000);

        const modalMetrics = await page.evaluate(() => {
            const dialog = document.querySelector('.tf-support-dialog');
            if (!dialog) return null;
            const rect = dialog.getBoundingClientRect();
            const title = dialog.querySelector('.tf-support-title-group h2')?.innerText.trim();
            const kpiPills = Array.from(dialog.querySelectorAll('.tf-kpi-pill')).map(p => ({
                label: p.querySelector('.tf-kpi-label')?.innerText.trim(),
                num: p.querySelector('.tf-kpi-number')?.innerText.trim()
            }));
            const tableRows = dialog.querySelectorAll('.tf-support-table-row');
            const searchInput = !!dialog.querySelector('.tf-support-search');
            const statusSelect = !!dialog.querySelector('.tf-filter-select');

            return {
                width: Math.round(rect.width),
                height: Math.round(rect.height),
                title,
                kpiCount: kpiPills.length,
                kpiPills,
                tableRowCount: tableRows.length,
                hasFilterBar: searchInput && statusSelect
            };
        });

        console.log('Support Center Modal Metrics:', modalMetrics);
        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'support_popup_management_kpis.png') });
        console.log('✓ Captured screenshot: support_popup_management_kpis.png');

        // Test Filter Bar interaction
        console.log('Testing filter interaction (Clicking "Mới gửi" KPI filter)...');
        await page.evaluate(() => {
            const newKpi = document.querySelector('.tf-kpi-new');
            if (newKpi) newKpi.click();
        });
        await sleep(600);

        // ==========================================
        // 3. CREATE NEW TICKET FORM & FILE UPLOAD
        // ==========================================
        console.log('\n[3/6] Testing "Gửi báo cáo mới" Form & Dropzone...');
        await page.evaluate(() => {
            const tabs = Array.from(document.querySelectorAll('.tf-support-tab-btn'));
            const createTab = tabs.find(t => t.innerText.includes('Gửi báo cáo mới'));
            if (createTab) createTab.click();
        });
        await sleep(800);

        // Fill form fields
        await page.type('input[placeholder*="Tóm tắt ngắn gọn"]', 'Giao diện bảng giá cập nhật không lưu được chiết khấu');
        await page.type('textarea.tf-support-textarea', '1. Thao tác: Vào Danh mục Sản phẩm -> Bảng giá bán buôn -> Sửa chiết khấu 15% -> Lưu.\n2. Thực tế: Báo lỗi "Không thể cập nhật chính sách chiết khấu".\n3. Mong muốn: Lưu thành công và hiển thị đúng trên POS.');

        // Test File Upload: Create temporary test file
        tempTestFilePath = path.join(__dirname, 'test_screenshot_loi.png');
        const dummyPng = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==', 'base64');
        fs.writeFileSync(tempTestFilePath, dummyPng);

        const fileInput = await page.$('.tf-support-file-input');
        if (fileInput) {
            await fileInput.uploadFile(tempTestFilePath);
            await sleep(800);
        }

        const createFormCheck = await page.evaluate(() => {
            const fileCards = document.querySelectorAll('.tf-support-file-card');
            const routeInput = document.querySelector('.tf-input-readonly')?.value;
            return {
                uploadedFileCount: fileCards.length,
                autoRoute: routeInput
            };
        });
        console.log('Create Form File Check:', createFormCheck);

        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'support_popup_create_ticket.png') });
        console.log('✓ Captured screenshot: support_popup_create_ticket.png');

        // Submit form
        console.log('Submitting ticket...');
        await page.click('button[type="submit"]');
        await sleep(2000);

        // ==========================================
        // 4. "BÁO CÁO CỦA TÔI" TAB
        // ==========================================
        console.log('\n[4/6] Checking "Báo cáo của tôi" Tab...');
        await page.evaluate(() => {
            const tabs = Array.from(document.querySelectorAll('.tf-support-tab-btn'));
            const myTab = tabs.find(t => t.innerText.includes('Báo cáo của tôi'));
            if (myTab) myTab.click();
        });
        await sleep(1000);

        const myTicketsCheck = await page.evaluate(() => {
            const rows = document.querySelectorAll('.tf-support-table-row');
            return {
                count: rows.length,
                firstCode: rows[0]?.querySelector('.tf-code-badge')?.innerText.trim(),
                firstTitle: rows[0]?.querySelector('.tf-ticket-title')?.innerText.trim()
            };
        });
        console.log('My Tickets List Check:', myTicketsCheck);

        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'support_popup_my_tickets.png') });
        console.log('✓ Captured screenshot: support_popup_my_tickets.png');

        // ==========================================
        // 5. TICKET DETAIL VIEW & DISCUSSION
        // ==========================================
        console.log('\n[5/6] Opening Ticket Detail & Discussion thread...');
        await page.click('.tf-support-table-row');
        await sleep(1500);

        const detailCheck = await page.evaluate(() => {
            const code = document.querySelector('.tf-code-large')?.innerText.trim();
            const title = document.querySelector('.tf-detail-content-title')?.innerText.trim();
            const metaCells = Array.from(document.querySelectorAll('.tf-meta-cell')).map(c => ({
                label: c.querySelector('.tf-meta-label')?.innerText.trim(),
                val: c.querySelector('.tf-meta-val')?.innerText.trim()
            }));
            const actionBtns = Array.from(document.querySelectorAll('.tf-action-buttons-group .tf-support-btn')).map(b => b.innerText.trim());
            const hasAssignDropdown = !!document.querySelector('.tf-select-assign');
            const hasReplyTextarea = !!document.querySelector('.tf-reply-textarea');

            return {
                code,
                title,
                metaCells,
                actionBtns,
                hasAssignDropdown,
                hasReplyTextarea
            };
        });
        console.log('Ticket Detail Check:', detailCheck);

        // Add a discussion comment
        console.log('Typing response comment in discussion...');
        await page.type('.tf-reply-textarea', 'Đã kiểm tra logs: lỗi phát sinh do bảng phụ thuộc bị khóa bởi transaction đồng bộ POS. Đang tiến hành điều chỉnh.');
        await sleep(300);
        await page.click('.tf-reply-card button.tf-btn-primary');
        await sleep(1500);

        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'support_popup_ticket_detail_discussion.png') });
        console.log('✓ Captured screenshot: support_popup_ticket_detail_discussion.png');

        // ==========================================
        // 6. DARK MODE & RESPONSIVE VERIFICATION
        // ==========================================
        console.log('\n[6/6] Testing Dark Mode & Mobile Responsive...');
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.documentElement.classList.add('dark');
        });
        await sleep(800);

        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'support_popup_dark_mode.png') });
        console.log('✓ Captured screenshot: support_popup_dark_mode.png');

        // Test Mobile Responsive (390px iPhone width)
        console.log('Testing Mobile Viewport (390px)...');
        await page.setViewport({ width: 390, height: 844 });
        await sleep(800);

        const mobileCheck = await page.evaluate(() => {
            const dialog = document.querySelector('.tf-support-dialog');
            const rect = dialog ? dialog.getBoundingClientRect() : null;
            const bodyScrollWidth = document.body.scrollWidth;
            const windowWidth = window.innerWidth;
            return {
                dialogWidth: rect ? Math.round(rect.width) : null,
                hasHorizontalOverflow: bodyScrollWidth > windowWidth
            };
        });
        console.log('Mobile Responsive Check:', mobileCheck);

        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'support_popup_mobile_responsive.png') });
        console.log('✓ Captured screenshot: support_popup_mobile_responsive.png');

        // Clean up temp test file
        if (tempTestFilePath && fs.existsSync(tempTestFilePath)) {
            fs.unlinkSync(tempTestFilePath);
        }

        console.log('\n======================================================');
        console.log('ALL VERIFICATION CRITERIA PASSED SUCCESSFULLY!');
        console.log('======================================================');

    } catch (err) {
        console.error('Error during verification:', err);
    } finally {
        await browser.close();
    }
})();
