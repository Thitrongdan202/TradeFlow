const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('🚀 Starting Document Center & Modal System Visual Verification...');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1920,1080']
    });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1920, height: 1080 });

        // 1. Sign In
        console.log('🔑 Signing in to TradeFlow...');
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        console.log('✅ Logged in successfully. Current URL:', page.url());

        // 2. Navigate to Document Center (/tai-lieu)
        console.log('📄 Navigating to /tai-lieu...');
        await page.goto('http://127.0.0.1:5169/tai-lieu', { waitUntil: 'networkidle2' });
        await sleep(2000);

        // Ensure Light Mode first
        const isDarkInitial = await page.evaluate(() => document.documentElement.getAttribute('data-bs-theme') === 'dark' || document.documentElement.classList.contains('dark'));
        if (isDarkInitial) {
            await page.click('.tf-theme-toggle');
            await sleep(800);
        }

        // Capture Light Mode Document Center
        const lightDocPath = path.join(ARTIFACT_DIR, 'document_center_redesigned_light.png');
        await page.screenshot({ path: lightDocPath });
        console.log('📸 Captured:', lightDocPath);

        // Switch to Dark Mode
        console.log('🌙 Switching to Dark Mode...');
        await page.click('.tf-theme-toggle');
        await sleep(800);

        // Capture Dark Mode Document Center
        const darkDocPath = path.join(ARTIFACT_DIR, 'document_center_redesigned_dark.png');
        await page.screenshot({ path: darkDocPath });
        console.log('📸 Captured:', darkDocPath);

        // 3. Test In-Modal Document Preview
        console.log('🔍 Testing In-Modal Document Preview...');
        const previewBtns = await page.$$('.tf-doc-btn-preview');
        if (previewBtns.length > 0) {
            await previewBtns[0].click();
            await sleep(1200);
            const previewModalPath = path.join(ARTIFACT_DIR, 'document_preview_modal_dark.png');
            await page.screenshot({ path: previewModalPath });
            console.log('📸 Captured Document Preview Modal:', previewModalPath);

            // Close Preview Modal
            const closeBtn = await page.$('.tf-preview-modal-dialog .tf-modal-close-btn');
            if (closeBtn) {
                await closeBtn.click();
                await sleep(600);
            }
        } else {
            console.log('⚠️ No preview button found in table');
        }

        // 4. Test Upload Modal in Dark Mode
        console.log('📤 Testing Upload Modal in Dark Mode...');
        const uploadBtns = await page.$$('button.tf-btn-primary');
        for (const b of uploadBtns) {
            const txt = await page.evaluate(el => el.textContent, b);
            if (txt.includes('Tải lên tài liệu')) {
                await b.click();
                await sleep(1000);
                const uploadModalPath = path.join(ARTIFACT_DIR, 'document_upload_modal_dark.png');
                await page.screenshot({ path: uploadModalPath });
                console.log('📸 Captured Upload Modal:', uploadModalPath);

                const closeUploadBtn = await page.$('.tf-upload-dialog .tf-modal-close-btn');
                if (closeUploadBtn) {
                    await closeUploadBtn.click();
                    await sleep(600);
                }
                break;
            }
        }

        // 5. Test "Lập phiếu thu" Modal in Dark Mode (Receipt Voucher)
        console.log('💰 Testing Receipt Voucher Modal in Dark Mode...');
        const outlineBtns = await page.$$('button.tf-btn-outline-primary');
        for (const b of outlineBtns) {
            const txt = await page.evaluate(el => el.textContent, b);
            if (txt.includes('Lập phiếu thu')) {
                await b.click();
                await sleep(1200);

                // Check computed background colors of search box & inputs
                const bgInfo = await page.evaluate(() => {
                    const searchBox = document.querySelector('.tf-partner-search-box');
                    const searchInput = document.querySelector('.tf-partner-search-box input');
                    const textarea = document.querySelector('.tf-textarea');
                    const dialog = document.querySelector('.voucher-dialog');
                    return {
                        dialogBg: dialog ? window.getComputedStyle(dialog).backgroundColor : null,
                        searchBoxBg: searchBox ? window.getComputedStyle(searchBox).backgroundColor : null,
                        searchInputBg: searchInput ? window.getComputedStyle(searchInput).backgroundColor : null,
                        textareaBg: textarea ? window.getComputedStyle(textarea).backgroundColor : null
                    };
                });
                console.log('🎨 Receipt Modal Dark Mode Computed Colors:', bgInfo);

                const receiptModalPath = path.join(ARTIFACT_DIR, 'receipt_voucher_modal_fixed_dark.png');
                await page.screenshot({ path: receiptModalPath });
                console.log('📸 Captured Fixed Receipt Modal:', receiptModalPath);

                // Close modal
                const closeBtn = await page.$('.voucher-dialog .tf-modal-close-btn');
                if (closeBtn) {
                    await closeBtn.click();
                    await sleep(600);
                }
                break;
            }
        }

        // 6. Test "Lập phiếu chi" Modal in Dark Mode (Payment Voucher)
        console.log('💸 Testing Payment Voucher Modal in Dark Mode...');
        const secOutlineBtns = await page.$$('button.tf-btn-outline-secondary');
        for (const b of secOutlineBtns) {
            const txt = await page.evaluate(el => el.textContent, b);
            if (txt.includes('Lập phiếu chi')) {
                await b.click();
                await sleep(1200);

                const bgInfo = await page.evaluate(() => {
                    const searchBox = document.querySelector('.tf-partner-search-box');
                    const searchInput = document.querySelector('.tf-partner-search-box input');
                    const textarea = document.querySelector('.tf-textarea');
                    const dialog = document.querySelector('.voucher-dialog');
                    return {
                        dialogBg: dialog ? window.getComputedStyle(dialog).backgroundColor : null,
                        searchBoxBg: searchBox ? window.getComputedStyle(searchBox).backgroundColor : null,
                        searchInputBg: searchInput ? window.getComputedStyle(searchInput).backgroundColor : null,
                        textareaBg: textarea ? window.getComputedStyle(textarea).backgroundColor : null
                    };
                });
                console.log('🎨 Payment Modal Dark Mode Computed Colors:', bgInfo);

                const paymentModalPath = path.join(ARTIFACT_DIR, 'payment_voucher_modal_fixed_dark.png');
                await page.screenshot({ path: paymentModalPath });
                console.log('📸 Captured Fixed Payment Modal:', paymentModalPath);

                // Close modal
                const closeBtn = await page.$('.voucher-dialog .tf-modal-close-btn');
                if (closeBtn) {
                    await closeBtn.click();
                    await sleep(600);
                }
                break;
            }
        }

        // 7. Verify Employee Modals
        console.log('👥 Navigating to /nhan-vien to verify Employee Modals...');
        await page.goto('http://127.0.0.1:5169/nhan-vien', { waitUntil: 'networkidle2' });
        await sleep(1500);

        // Open Add Employee Modal
        const empBtns = await page.$$('button.tf-btn-primary');
        for (const b of empBtns) {
            const txt = await page.evaluate(el => el.textContent, b);
            if (txt.includes('Nhân viên')) {
                await b.click();
                await sleep(1000);
                const empAddPath = path.join(ARTIFACT_DIR, 'employee_add_modal_polished_dark.png');
                await page.screenshot({ path: empAddPath });
                console.log('📸 Captured Polished Employee Add Modal:', empAddPath);

                const closeBtn = await page.$('.employee-modal .tf-modal-close-btn');
                if (closeBtn) {
                    await closeBtn.click();
                    await sleep(600);
                }
                break;
            }
        }

        // Open Review Requests Modal
        const reviewBtns = await page.$$('button.tf-btn-primary');
        for (const b of reviewBtns) {
            const txt = await page.evaluate(el => el.textContent, b);
            if (txt.includes('Duyệt yêu cầu')) {
                await b.click();
                await sleep(1200);
                const empReqPath = path.join(ARTIFACT_DIR, 'employee_review_modal_polished_dark.png');
                await page.screenshot({ path: empReqPath });
                console.log('📸 Captured Polished Employee Review Modal:', empReqPath);

                const closeBtn = await page.$('.tf-modal-header .tf-modal-close-btn');
                if (closeBtn) {
                    await closeBtn.click();
                    await sleep(600);
                }
                break;
            }
        }

        // 8. Test Responsive Viewports for Document Center
        console.log('📱 Testing Responsive Viewports for Document Center...');
        await page.goto('http://127.0.0.1:5169/tai-lieu', { waitUntil: 'networkidle2' });
        await sleep(1000);

        const viewports = [
            { width: 1440, height: 900, name: 'document_center_1440_dark.png' },
            { width: 1366, height: 768, name: 'document_center_1366_dark.png' },
            { width: 768, height: 1024, name: 'document_center_768_tablet_dark.png' },
            { width: 390, height: 844, name: 'document_center_390_mobile_dark.png' }
        ];

        for (const vp of viewports) {
            await page.setViewport({ width: vp.width, height: vp.height });
            await sleep(600);
            const vpPath = path.join(ARTIFACT_DIR, vp.name);
            await page.screenshot({ path: vpPath });
            console.log(`📸 Captured Viewport ${vp.width}x${vp.height}:`, vpPath);
        }

        console.log('🎉 ALL VISUAL VERIFICATIONS COMPLETED SUCCESSFULLY!');
    } catch (err) {
        console.error('❌ Verification Error:', err);
    } finally {
        await browser.close();
    }
})();
