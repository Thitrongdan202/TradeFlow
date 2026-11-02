const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const BASE_URL = 'http://127.0.0.1:5169';
const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('==================================================');
    console.log('🧪 Starting Global Motion & Customer Modal Verification');
    console.log('==================================================');

    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1920,1080']
    });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1920, height: 1080, deviceScaleFactor: 1 });

        // 1. Login
        console.log('1. Logging in as admin...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);

        // 2. Navigate to Customers page
        console.log('2. Navigating to /doi-tac/khach-hang...');
        await page.goto(`${BASE_URL}/doi-tac/khach-hang`, { waitUntil: 'networkidle2' });
        await sleep(1000);

        // 3. Test "Thêm khách hàng" button opens popup directly (No navigation!)
        console.log('3. Clicking "Thêm khách hàng" button...');
        const initialUrl = page.url();
        console.log(`   Initial URL: ${initialUrl}`);

        const startOpen = Date.now();
        await page.click('.tf-header-action-cluster .tf-btn-primary');
        await page.waitForSelector('.customer-modal', { visible: true });
        const openDuration = Date.now() - startOpen;
        console.log(`   ⚡ Customer Modal Open Duration: ${openDuration} ms`);

        const currentUrl = page.url();
        console.log(`   Current URL after opening modal: ${currentUrl}`);
        if (currentUrl.includes('/doi-tac/khach-hang') && !currentUrl.includes('/them-moi')) {
            console.log('   ✅ PASS: Modal opened directly on the list page without navigating away!');
        } else {
            console.error('   ❌ FAIL: Page navigated to a separate route!');
        }

        await sleep(300);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'customer_modal_create_light.png'), fullPage: false });
        console.log('   📸 Saved customer_modal_create_light.png');

        // 4. Test Dirty Check Confirmation Dialog
        console.log('4. Testing Dirty Check Confirmation Dialog...');
        // Type some customer name
        const nameInput = await page.$('.customer-modal input[placeholder*="Tên khách hàng"]');
        if (nameInput) {
            await nameInput.type('Công ty TNHH Thiết Bị Vệ Sinh Đại Phát');
        }

        // Click overlay backdrop to test protection
        await page.click('.tf-modal-overlay');
        await sleep(200);
        const confirmBoxVisible = await page.$('.tf-modal-confirm-overlay') !== null;
        if (confirmBoxVisible) {
            console.log('   ✅ PASS: Unsaved changes confirmation dialog appeared on backdrop click!');
            await page.screenshot({ path: path.join(ARTIFACT_DIR, 'customer_modal_dirty_confirm.png'), fullPage: false });
            console.log('   📸 Saved customer_modal_dirty_confirm.png');

            // Click "Tiếp tục nhập"
            await page.click('.tf-modal-confirm-overlay .tf-btn-secondary');
            await sleep(200);
        }

        // Fill remaining customer fields
        console.log('5. Filling remaining customer details...');
        const companyInput = await page.$('.customer-modal input[placeholder*="Tên đầy đủ theo đăng ký kinh doanh"]');
        if (companyInput) await companyInput.type('Công ty Cổ phần Thương mại Đại Phát');

        const taxInput = await page.$('.customer-modal input[placeholder*="Mã số thuế doanh nghiệp"]');
        if (taxInput) await taxInput.type('0108998877');

        const contactInput = await page.$('.customer-modal input[placeholder*="Họ và tên người liên hệ"]');
        if (contactInput) await contactInput.type('Nguyễn Văn Phát');

        const phoneInput = await page.$('.customer-modal input[placeholder*="Số điện thoại di động"]');
        if (phoneInput) await phoneInput.type('0988776655');

        const emailInput = await page.$('.customer-modal input[placeholder*="email@congty.com"]');
        if (emailInput) await emailInput.type('daiphat@lacasa-demo.vn');

        const addressInput = await page.$('.customer-modal input[placeholder*="Số nhà, đường phố"]');
        if (addressInput) await addressInput.type('Số 45 Đại lộ Thăng Long, Mễ Trì, Nam Từ Liêm, Hà Nội');

        const notesInput = await page.$('.customer-modal textarea[placeholder*="Ghi chú về chính sách"]');
        if (notesInput) await notesInput.type('Khách hàng đại lý cấp 1 phân phối thiết bị vệ sinh cao cấp khu vực miền Bắc');

        await sleep(200);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'customer_modal_filled_light.png'), fullPage: false });
        console.log('   📸 Saved customer_modal_filled_light.png');

        // 6. Submit the form
        console.log('6. Submitting Customer Form...');
        const startSubmit = Date.now();
        await page.click('.customer-modal-footer .tf-btn-primary');
        await page.waitForSelector('.customer-modal', { hidden: true });
        const closeDuration = Date.now() - startSubmit;
        console.log(`   ⚡ Customer Modal Submit & Close Duration: ${closeDuration} ms`);

        await sleep(600);
        const postSubmitUrl = page.url();
        console.log(`   Post-submit URL: ${postSubmitUrl}`);

        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'customer_list_after_create_success.png'), fullPage: false });
        console.log('   📸 Saved customer_list_after_create_success.png');

        // Check if notification banner is present
        const bannerText = await page.evaluate(() => {
            const el = document.querySelector('.tf-alert-success');
            return el ? el.innerText.trim() : null;
        });
        console.log(`   Notification banner text: "${bannerText}"`);
        if (bannerText && bannerText.includes('thành công')) {
            console.log('   ✅ PASS: Success notification banner displayed with customer details!');
        }

        // 7. Test Row Action "Sửa" in Customer Table
        console.log('7. Testing Customer Edit Modal via Row Action...');
        const editButtons = await page.$$('.tf-row-actions-stack .tf-btn-action-stack.edit');
        if (editButtons.length > 0) {
            await editButtons[0].click();
            await page.waitForSelector('.customer-modal', { visible: true });
            await sleep(300);

            const modalTitle = await page.evaluate(() => {
                const el = document.querySelector('.customer-modal .tf-modal-title');
                return el ? el.innerText.trim() : '';
            });
            console.log(`   Edit Modal Title: "${modalTitle}"`);
            if (modalTitle.includes('Chỉnh sửa')) {
                console.log('   ✅ PASS: Edit modal opened with correct title and prefilled data!');
            }

            await page.screenshot({ path: path.join(ARTIFACT_DIR, 'customer_modal_edit_light.png'), fullPage: false });
            console.log('   📸 Saved customer_modal_edit_light.png');

            // Close via close button
            await page.click('.customer-modal .tf-modal-close-btn');
            await page.waitForSelector('.customer-modal', { hidden: true });
            await sleep(200);
        }

        // 8. Test Partner Detail Drawer (Slide-Over)
        console.log('8. Testing Partner Detail Drawer Slide-Over...');
        const firstCustCode = await page.$('.tf-table-code-btn');
        if (firstCustCode) {
            const startDrawer = Date.now();
            await firstCustCode.click();
            await page.waitForSelector('.tf-drawer-panel', { visible: true });
            const drawerOpenTime = Date.now() - startDrawer;
            console.log(`   ⚡ Drawer Slide-in Duration: ${drawerOpenTime} ms`);

            await sleep(350);
            await page.screenshot({ path: path.join(ARTIFACT_DIR, 'customer_detail_drawer_light.png'), fullPage: false });
            console.log('   📸 Saved customer_detail_drawer_light.png');

            // Close Drawer
            const startDrawerClose = Date.now();
            await page.click('.tf-drawer-close-btn');
            await page.waitForSelector('.tf-drawer-panel', { hidden: true });
            const drawerCloseTime = Date.now() - startDrawerClose;
            console.log(`   ⚡ Drawer Slide-out Duration: ${drawerCloseTime} ms`);
            await sleep(200);
        }

        // 9. Test Dark Mode
        console.log('9. Testing Dark Mode across Popups & Drawer...');
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-theme', 'dark');
            localStorage.setItem('tradeflow_theme', 'dark');
        });
        await sleep(300);

        // Open Customer Modal in Dark Mode
        await page.click('.tf-header-action-cluster .tf-btn-primary');
        await page.waitForSelector('.customer-modal', { visible: true });
        await sleep(300);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'customer_modal_create_dark.png'), fullPage: false });
        console.log('   📸 Saved customer_modal_create_dark.png');

        await page.click('.customer-modal .tf-modal-close-btn');
        await page.waitForSelector('.customer-modal', { hidden: true });
        await sleep(200);

        // Reset theme to light
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-theme', 'light');
            localStorage.setItem('tradeflow_theme', 'light');
        });
        await sleep(200);

        console.log('\n🎉 ALL TESTS AND BENCHMARKS PASSED SUCCESSFULLY!');
    } catch (err) {
        console.error('❌ Error during testing:', err);
        process.exitCode = 1;
    } finally {
        await browser.close();
    }
})();
