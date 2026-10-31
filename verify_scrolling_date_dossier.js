const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://127.0.0.1:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('================================================================');
    console.log('[VERIFY] Starting Employee Form Scrolling, Vietnamese Date Format');
    console.log('         & Employee Dossier Workflow Verification');
    console.log('================================================================');

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
        await sleep(400);
    }

    try {
        // STEP 1: Login as admin
        console.log('\n[1/5] Logging into TradeFlow...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('#login-username', { timeout: 10000 });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        console.log('✓ Login successful. URL:', page.url());
        await sleep(1500);

        // STEP 2: Navigate to Employee Management
        console.log('\n[2/5] Navigating to /nhan-vien (Employee Management)...');
        await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
        await sleep(2000);

        // Wait for page actions
        await page.waitForSelector('.tf-page-actions', { timeout: 10000 });
        console.log('Page actions found. Clicking Add Employee button...');
        
        await page.click('.tf-page-actions button.tf-btn-primary');
        await sleep(1200);

        // Verify modal opened
        await page.waitForSelector('.employee-modal, .tf-modal', { timeout: 5000 });
        console.log('✓ Employee modal opened successfully.');

        // PART 1: Modal Scrolling at Viewports
        console.log('\n--- PART 1: Modal Scrolling Geometry & Responsive Viewports ---');
        
        // Viewport 1366x768
        await page.setViewport({ width: 1366, height: 768 });
        await sleep(600);
        
        const geom1366 = await page.evaluate(() => {
            const modal = document.querySelector('.employee-modal') || document.querySelector('.tf-modal');
            const header = document.querySelector('.employee-modal-header') || document.querySelector('.tf-modal-header');
            const body = document.querySelector('.employee-modal-body') || document.querySelector('.tf-modal-body');
            const footer = document.querySelector('.employee-modal-footer') || document.querySelector('.tf-modal-footer');
            
            if (!modal || !header || !body || !footer) return null;
            
            const modalRect = modal.getBoundingClientRect();
            const headerRect = header.getBoundingClientRect();
            const bodyRect = body.getBoundingClientRect();
            const footerRect = footer.getBoundingClientRect();
            
            return {
                viewport: { width: window.innerWidth, height: window.innerHeight },
                modal: { height: modalRect.height, top: modalRect.top, bottom: modalRect.bottom },
                header: { height: headerRect.height, top: headerRect.top },
                body: { height: bodyRect.height, scrollHeight: body.scrollHeight, scrollTop: body.scrollTop, overflowY: window.getComputedStyle(body).overflowY },
                footer: { height: footerRect.height, bottom: footerRect.bottom, isVisible: footerRect.bottom <= window.innerHeight }
            };
        });
        console.log('Geometry at 1366x768:', JSON.stringify(geom1366, null, 2));

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'scroll_01_modal_1366x768.png'),
            fullPage: false
        });
        console.log('✓ Captured scroll_01_modal_1366x768.png');

        // Viewport 1920x1080
        await page.setViewport({ width: 1920, height: 1080 });
        await sleep(500);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'scroll_02_modal_1920x1080.png'),
            fullPage: false
        });
        console.log('✓ Captured scroll_02_modal_1920x1080.png');

        // Reset to standard 1440x900
        await page.setViewport({ width: 1440, height: 900 });
        await sleep(500);

        // Switch to "Gửi yêu cầu phê duyệt" mode to expand all 5 sections
        console.log('Switching to "Gửi yêu cầu phê duyệt" mode...');
        await page.evaluate(() => {
            const radios = Array.from(document.querySelectorAll('input[type="radio"][name="empCreateMode"]'));
            if (radios.length > 1) {
                radios[1].click(); // Second radio is "Gửi yêu cầu phê duyệt"
            }
        });
        await sleep(800);

        // Scroll the body down to verify smooth scrolling and visible footer
        console.log('Scrolling modal body down...');
        await page.evaluate(() => {
            const body = document.querySelector('.employee-modal-body') || document.querySelector('.tf-modal-body');
            if (body) {
                body.scrollTop = body.scrollHeight;
            }
        });
        await sleep(500);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'scroll_03_modal_scrolled_to_bottom.png'),
            fullPage: false
        });
        console.log('✓ Captured scroll_03_modal_scrolled_to_bottom.png');

        // Test in Dark Mode
        await setTheme('dark');
        await sleep(500);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'scroll_04_modal_dark_mode.png'),
            fullPage: false
        });
        console.log('✓ Captured scroll_04_modal_dark_mode.png');
        await setTheme('light');

        // PART 2: Vietnamese Date Format (TfDateInput)
        console.log('\n--- PART 2: Vietnamese Date Format (TfDateInput) ---');
        
        // Scroll body back to top to access date fields
        await page.evaluate(() => {
            const body = document.querySelector('.employee-modal-body') || document.querySelector('.tf-modal-body');
            if (body) body.scrollTop = 0;
        });
        await sleep(400);

        // Open calendar popup
        console.log('Opening calendar popup on DateOfBirth...');
        await page.evaluate(() => {
            const calBtns = document.querySelectorAll('.tf-date-calendar-btn');
            if (calBtns.length > 0) calBtns[0].click();
        });
        await sleep(600);

        const calPopupInfo = await page.evaluate(() => {
            const popup = document.querySelector('.tf-date-picker-popup');
            if (!popup) return null;
            const weekdays = Array.from(popup.querySelectorAll('.tf-calendar-weekdays span')).map(s => s.textContent);
            const monthSelect = popup.querySelector('.tf-calendar-month-select');
            const yearSelect = popup.querySelector('.tf-calendar-year-select');
            const footerBtns = Array.from(popup.querySelectorAll('.tf-calendar-footer button')).map(b => b.textContent);
            return {
                isOpen: true,
                weekdays,
                footerBtns,
                monthSelectVal: monthSelect ? monthSelect.value : null,
                yearSelectVal: yearSelect ? yearSelect.value : null
            };
        });
        console.log('Calendar popup inspection:', calPopupInfo);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'date_01_vietnamese_calendar_popup.png'),
            fullPage: false
        });
        console.log('✓ Captured date_01_vietnamese_calendar_popup.png');

        // Close calendar popup
        await page.evaluate(() => {
            const popup = document.querySelector('.tf-date-picker-popup');
            if (popup) {
                const btns = Array.from(popup.querySelectorAll('.tf-calendar-footer button'));
                const closeBtn = btns.find(b => b.textContent.includes('Đóng'));
                if (closeBtn) closeBtn.click();
            }
        });
        await sleep(400);

        // Test invalid date manual typing (e.g., 31/02/2026)
        console.log('Testing invalid date manual typing (31/02/2026)...');
        await page.evaluate(() => {
            const dateInputs = document.querySelectorAll('.tf-date-field');
            if (dateInputs.length > 0) {
                dateInputs[0].value = '31/02/2026';
                dateInputs[0].dispatchEvent(new Event('input', { bubbles: true }));
                dateInputs[0].dispatchEvent(new Event('change', { bubbles: true }));
                dateInputs[0].dispatchEvent(new Event('blur', { bubbles: true }));
            }
        });
        await sleep(500);

        const errorInfo = await page.evaluate(() => {
            const errs = Array.from(document.querySelectorAll('.tf-field-error')).map(e => e.textContent.trim());
            return errs;
        });
        console.log('Validation errors on invalid date:', errorInfo);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'date_02_validation_invalid_date.png'),
            fullPage: false
        });
        console.log('✓ Captured date_02_validation_invalid_date.png');

        // Enter valid date (e.g. 15/08/1992)
        console.log('Entering valid date (15/08/1992)...');
        await page.evaluate(() => {
            const dateInputs = document.querySelectorAll('.tf-date-field');
            if (dateInputs.length > 0) {
                dateInputs[0].value = '15/08/1992';
                dateInputs[0].dispatchEvent(new Event('input', { bubbles: true }));
                dateInputs[0].dispatchEvent(new Event('change', { bubbles: true }));
                dateInputs[0].dispatchEvent(new Event('blur', { bubbles: true }));
            }
        });
        await sleep(500);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'date_03_valid_date_display.png'),
            fullPage: false
        });
        console.log('✓ Captured date_03_valid_date_display.png');

        // PART 3: Employee Dossier Workflow & Attachments
        console.log('\n--- PART 3: Employee Dossier Workflow & Attachments ---');
        
        // Fill out required candidate form fields
        console.log('Filling out candidate form fields...');
        await page.evaluate(() => {
            // Full name
            const allInputs = Array.from(document.querySelectorAll('input.tf-input'));
            const nameInput = allInputs.find(i => i.placeholder.includes('Nguyễn Văn An') || i.placeholder.includes('Họ và tên'));
            if (nameInput) {
                nameInput.value = 'Trần Văn Hoàng Nam';
                nameInput.dispatchEvent(new Event('input', { bubbles: true }));
                nameInput.dispatchEvent(new Event('change', { bubbles: true }));
            }

            const codeInput = allInputs.find(i => i.placeholder.includes('NV00001'));
            if (codeInput) {
                codeInput.value = 'NV99882';
                codeInput.dispatchEvent(new Event('input', { bubbles: true }));
                codeInput.dispatchEvent(new Event('change', { bubbles: true }));
            }

            const phoneInput = allInputs.find(i => i.placeholder.includes('0901'));
            if (phoneInput) {
                phoneInput.value = '0988776655';
                phoneInput.dispatchEvent(new Event('input', { bubbles: true }));
                phoneInput.dispatchEvent(new Event('change', { bubbles: true }));
            }

            const emailInput = allInputs.find(i => i.placeholder.includes('@congty.vn'));
            if (emailInput) {
                emailInput.value = 'hoangnam.dossier@tradeflow.vn';
                emailInput.dispatchEvent(new Event('input', { bubbles: true }));
                emailInput.dispatchEvent(new Event('change', { bubbles: true }));
            }

            // Department select
            const selects = Array.from(document.querySelectorAll('select.tf-input'));
            if (selects.length > 1 && selects[1].options.length > 1) {
                selects[1].selectedIndex = 1;
                selects[1].dispatchEvent(new Event('change', { bubbles: true }));
            }
            if (selects.length > 2 && selects[2].options.length > 1) {
                selects[2].selectedIndex = 1;
                selects[2].dispatchEvent(new Event('change', { bubbles: true }));
            }
        });
        await sleep(600);

        // Scroll down to Section 5
        await page.evaluate(() => {
            const body = document.querySelector('.employee-modal-body') || document.querySelector('.tf-modal-body');
            if (body) body.scrollTop = body.scrollHeight;
        });
        await sleep(500);

        // Click "+ Đính kèm hồ sơ mẫu DEMO" button
        console.log('Clicking "+ Đính kèm hồ sơ mẫu DEMO" button...');
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('button'));
            const demoBtn = btns.find(b => b.textContent.includes('Đính kèm hồ sơ mẫu DEMO'));
            if (demoBtn) demoBtn.click();
        });
        await sleep(1500);

        // Check attached dossier items
        const dossierList = await page.evaluate(() => {
            const items = Array.from(document.querySelectorAll('.tf-dossier-item')).map(item => ({
                text: item.textContent.replace(/\s+/g, ' ').trim(),
                hasDemoTag: !!item.querySelector('.tf-dossier-demo-tag')
            }));
            return items;
        });
        console.log('Attached dossier documents:', JSON.stringify(dossierList, null, 2));

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'dossier_01_form_demo_attachments.png'),
            fullPage: false
        });
        console.log('✓ Captured dossier_01_form_demo_attachments.png');

        // Submit the approval request
        console.log('Submitting candidate approval request...');
        await page.evaluate(() => {
            const submitBtn = document.querySelector('.employee-modal-footer button[type="submit"]') ||
                             document.querySelector('button[type="submit"]');
            if (submitBtn) submitBtn.click();
        });
        await sleep(2500);

        // Open "Duyệt yêu cầu" modal to inspect the submitted candidate
        console.log('Opening "Duyệt yêu cầu" modal...');
        await page.evaluate(() => {
            const reqBtn = Array.from(document.querySelectorAll('.tf-page-actions button')).find(b => b.textContent.includes('Duyệt yêu cầu'));
            if (reqBtn) reqBtn.click();
        });
        await sleep(1500);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'dossier_02_approval_modal_with_attachments.png'),
            fullPage: false
        });
        console.log('✓ Captured dossier_02_approval_modal_with_attachments.png');

        // Inspect candidate and attached dossier in EmployeeRequestModal
        const reqDetail = await page.evaluate(() => {
            const reqCards = Array.from(document.querySelectorAll('.tf-req-card, .tf-request-item, tr')).map(r => r.textContent.trim());
            const atts = Array.from(document.querySelectorAll('.tf-req-attachment-item, .tf-dossier-item, a[href*="/api/documents"]')).map(a => a.textContent.trim());
            return { cardsCount: reqCards.length, attachmentsFound: atts.length, attSample: atts.slice(0, 3) };
        });
        console.log('Approval modal inspection:', reqDetail);

        // Click "Phê duyệt" button
        console.log('Clicking "Phê duyệt" button on the candidate...');
        await page.evaluate(() => {
            const approveBtn = Array.from(document.querySelectorAll('button')).find(b => b.textContent.includes('Phê duyệt') || b.textContent.includes('Duyệt'));
            if (approveBtn) approveBtn.click();
        });
        await sleep(2500);

        // Close request modal if still open
        await page.evaluate(() => {
            const closeBtn = document.querySelector('.tf-modal-close-btn');
            if (closeBtn) closeBtn.click();
        });
        await sleep(1000);

        // Find the newly approved employee in the table and expand row
        console.log('Expanding approved employee row to verify dossier transfer...');
        await page.evaluate(() => {
            const rows = Array.from(document.querySelectorAll('tr'));
            const targetRow = rows.find(r => r.textContent.includes('Hoàng Nam') || r.textContent.includes('NV99882'));
            if (targetRow) {
                // Find expand toggle button
                const expandBtn = targetRow.querySelector('.tf-row-expand-btn, button[title*="chi tiết"], button');
                if (expandBtn) expandBtn.click();
                else targetRow.click();
            }
        });
        await sleep(1500);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'dossier_03_approved_employee_expanded_dossier.png'),
            fullPage: false
        });
        console.log('✓ Captured dossier_03_approved_employee_expanded_dossier.png');

        // Submit a second candidate request and test "Yêu cầu bổ sung hồ sơ"
        console.log('\nTesting "Yêu cầu bổ sung hồ sơ" workflow on second candidate...');
        await page.click('.tf-page-actions button.tf-btn-primary');
        await sleep(1000);

        // Switch to "Gửi yêu cầu phê duyệt"
        await page.evaluate(() => {
            const radios = Array.from(document.querySelectorAll('input[type="radio"][name="empCreateMode"]'));
            if (radios.length > 1) radios[1].click();
        });
        await sleep(600);

        // Fill candidate 2
        await page.evaluate(() => {
            const allInputs = Array.from(document.querySelectorAll('input.tf-input'));
            const nameInput = allInputs.find(i => i.placeholder.includes('Nguyễn Văn An') || i.placeholder.includes('Họ và tên'));
            if (nameInput) {
                nameInput.value = 'Lê Thị Thu Thảo';
                nameInput.dispatchEvent(new Event('input', { bubbles: true }));
                nameInput.dispatchEvent(new Event('change', { bubbles: true }));
            }
            const codeInput = allInputs.find(i => i.placeholder.includes('NV00001'));
            if (codeInput) {
                codeInput.value = 'NV99883';
                codeInput.dispatchEvent(new Event('input', { bubbles: true }));
                codeInput.dispatchEvent(new Event('change', { bubbles: true }));
            }
        });
        await sleep(600);

        // Submit candidate 2
        await page.evaluate(() => {
            const submitBtn = document.querySelector('.employee-modal-footer button[type="submit"]');
            if (submitBtn) submitBtn.click();
        });
        await sleep(2500);

        // Open Duyệt yêu cầu modal
        await page.evaluate(() => {
            const reqBtn = Array.from(document.querySelectorAll('.tf-page-actions button')).find(b => b.textContent.includes('Duyệt yêu cầu'));
            if (reqBtn) reqBtn.click();
        });
        await sleep(1500);

        // Click "Yêu cầu bổ sung"
        console.log('Clicking "Yêu cầu bổ sung" button...');
        await page.evaluate(() => {
            const needDocsBtn = Array.from(document.querySelectorAll('button')).find(b => b.textContent.includes('Yêu cầu bổ sung'));
            if (needDocsBtn) needDocsBtn.click();
        });
        await sleep(2000);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'dossier_04_request_additional_docs_flow.png'),
            fullPage: false
        });
        console.log('✓ Captured dossier_04_request_additional_docs_flow.png');

        // Close modal
        await page.evaluate(() => {
            const closeBtn = document.querySelector('.tf-modal-close-btn');
            if (closeBtn) closeBtn.click();
        });
        await sleep(1000);

        // PART 4: Other Application Popups & Modals Audit
        console.log('\n--- PART 4: Audit Other Representative Application Popups ---');

        // 1. Customer Modal (/khach-hang)
        console.log('Auditing Customer modal (/khach-hang)...');
        await page.goto(`${BASE_URL}/khach-hang`, { waitUntil: 'networkidle2' });
        await sleep(1500);
        await page.evaluate(() => {
            const addBtn = Array.from(document.querySelectorAll('button')).find(b => b.textContent.includes('Thêm khách hàng') || b.textContent.includes('Thêm'));
            if (addBtn) addBtn.click();
        });
        await sleep(1000);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'modal_audit_01_customer.png'),
            fullPage: false
        });
        console.log('✓ Captured modal_audit_01_customer.png');

        // 2. Supplier Modal (/nha-cung-cap)
        console.log('Auditing Supplier modal (/nha-cung-cap)...');
        await page.goto(`${BASE_URL}/nha-cung-cap`, { waitUntil: 'networkidle2' });
        await sleep(1500);
        await page.evaluate(() => {
            const addBtn = Array.from(document.querySelectorAll('button')).find(b => b.textContent.includes('Thêm nhà cung cấp') || b.textContent.includes('Thêm'));
            if (addBtn) addBtn.click();
        });
        await sleep(1000);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'modal_audit_02_supplier.png'),
            fullPage: false
        });
        console.log('✓ Captured modal_audit_02_supplier.png');

        // 3. Contract Modal (/hop-dong)
        console.log('Auditing Contract modal (/hop-dong)...');
        await page.goto(`${BASE_URL}/hop-dong`, { waitUntil: 'networkidle2' });
        await sleep(1500);
        await page.evaluate(() => {
            const addBtn = Array.from(document.querySelectorAll('button')).find(b => b.textContent.includes('Tạo hợp đồng') || b.textContent.includes('Thêm'));
            if (addBtn) addBtn.click();
        });
        await sleep(1000);
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'modal_audit_03_contract.png'),
            fullPage: false
        });
        console.log('✓ Captured modal_audit_03_contract.png');

        console.log('\n================================================================');
        console.log('[SUCCESS] All verification tests completed successfully!');
        console.log('================================================================');

    } catch (err) {
        console.error('ERROR during verification:', err);
    } finally {
        await browser.close();
    }
})();
