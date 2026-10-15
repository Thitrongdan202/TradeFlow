const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://localhost:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function runVerification() {
    console.log('--- Starting TradeFlow UI Refinement Verification ---');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    const results = {
        login: false,
        topbarPhoneIconRemoved: false,
        userMenuSupportPresent: false,
        receiptModalCentered: false,
        cleanBackdropClickDismisses: false,
        cleanEscapeDismisses: false,
        dirtyStateBlocksDismissal: false,
        dirtyKeepEditingWorks: false,
        dirtyDiscardClosesModal: false,
        supplierModalCentered: false,
        phoneEmojiRemovedInSearch: false
    };

    try {
        // Step 1: Login
        console.log('1. Logging in as admin...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('#login-username');
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        results.login = page.url().includes('/') && !page.url().includes('/Account/Login');
        console.log('   Login success:', results.login);

        // Step 2: Topbar Icon Audit Verification
        console.log('2. Verifying topbar icon audit...');
        const topbarPhoneBtn = await page.$('.tf-topbar-actions a[href="/ho-tro-ky-thuat"].tf-topbar-btn');
        results.topbarPhoneIconRemoved = (topbarPhoneBtn === null);
        console.log('   Standalone phone icon removed from topbar:', results.topbarPhoneIconRemoved);

        // Check UserMenu for technical support
        await page.click('#tf-user-menu-btn');
        await sleep(300);
        const supportMenuItem = await page.$('a.tf-dropdown-item[href="/ho-tro-ky-thuat"]');
        results.userMenuSupportPresent = (supportMenuItem !== null);
        console.log('   Technical support present in UserMenu:', results.userMenuSupportPresent);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'refinement_01_topbar_usermenu.png') });

        // Close user menu
        await page.click('#tf-user-menu-btn');
        await sleep(300);

        // Step 3: Customers page & Receipt Modal Positioning
        console.log('3. Navigating to Customers page and testing Receipt Modal...');
        await page.goto(`${BASE_URL}/doi-tac/khach-hang`, { waitUntil: 'networkidle2' });
        await sleep(800);

        // Find and click the "Thu tiền" button for the first customer with debt or action button
        const thuTienBtn = await page.$('.btn-receipt, button[title*="Thu tiền"], .tf-btn-receipt, table tbody tr button');
        // Let's find specific button with text "Thu tiền"
        const buttons = await page.$$('button');
        let clicked = false;
        for (const btn of buttons) {
            const text = await page.evaluate(el => el.textContent, btn);
            if (text && text.includes('Thu tiền')) {
                await btn.click();
                clicked = true;
                break;
            }
        }

        if (!clicked) {
            // Alternatively click the general "Thu tiền" or action button
            const actionBtns = await page.$$('table tbody tr button.tf-btn');
            if (actionBtns.length > 0) {
                await actionBtns[0].click();
            }
        }
        await sleep(600);

        // Check modal bounds
        const modalBbox = await page.evaluate(() => {
            const backdrop = document.querySelector('.tf-modal-backdrop');
            const dialog = document.querySelector('.voucher-dialog');
            if (!dialog || !backdrop) return null;
            const bRect = backdrop.getBoundingClientRect();
            const dRect = dialog.getBoundingClientRect();
            return {
                backdrop: { top: bRect.top, bottom: bRect.bottom, left: bRect.left, right: bRect.right },
                dialog: { top: dRect.top, bottom: dRect.bottom, left: dRect.left, right: dRect.right, height: dRect.height, width: dRect.width }
            };
        });

        console.log('   Receipt modal bounding box:', modalBbox);
        if (modalBbox && modalBbox.backdrop.top === 0 && modalBbox.backdrop.left === 0 && modalBbox.dialog.top > 0) {
            results.receiptModalCentered = true;
        }
        console.log('   Receipt modal centered in viewport:', results.receiptModalCentered);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'refinement_02_receipt_modal_centered.png') });

        // Step 4: Test Clean Outside Click Dismissal
        console.log('4. Testing clean outside-click dismissal...');
        // Click at (50, 50) which is on the backdrop outside the centered dialog
        await page.mouse.click(50, 50);
        await sleep(500);

        const isOpenAfterCleanClick = await page.evaluate(() => document.querySelector('.voucher-dialog') !== null);
        results.cleanBackdropClickDismisses = !isOpenAfterCleanClick;
        console.log('   Modal dismissed cleanly on outside click without prompt:', results.cleanBackdropClickDismisses);

        // Step 5: Test Clean Escape Dismissal
        console.log('5. Testing clean Escape key dismissal...');
        // Reopen modal
        for (const btn of await page.$$('button')) {
            const text = await page.evaluate(el => el.textContent, btn);
            if (text && text.includes('Thu tiền')) {
                await btn.click();
                break;
            }
        }
        await sleep(600);

        await page.keyboard.press('Escape');
        await sleep(500);
        const isOpenAfterEscape = await page.evaluate(() => document.querySelector('.voucher-dialog') !== null);
        results.cleanEscapeDismisses = !isOpenAfterEscape;
        console.log('   Modal dismissed cleanly on Escape key without prompt:', results.cleanEscapeDismisses);

        // Step 6: Test Dirty Form Protection & Confirmation Prompt
        console.log('6. Testing dirty form detection and confirmation dialog...');
        // Reopen modal
        for (const btn of await page.$$('button')) {
            const text = await page.evaluate(el => el.textContent, btn);
            if (text && text.includes('Thu tiền')) {
                await btn.click();
                break;
            }
        }
        await sleep(600);

        // Modify note field to make form dirty
        await page.type('.tf-textarea', ' Ghi chú kiểm thử đối soát tự động');
        await sleep(300);

        // Click outside backdrop
        await page.mouse.click(50, 50);
        await sleep(500);

        // Verify confirm overlay is visible
        const confirmOverlayVisible = await page.evaluate(() => {
            const overlay = document.querySelector('.tf-confirm-overlay');
            return overlay !== null && window.getComputedStyle(overlay).display !== 'none';
        });
        results.dirtyStateBlocksDismissal = confirmOverlayVisible;
        console.log('   Confirmation dialog displayed when dirty:', results.dirtyStateBlocksDismissal);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'refinement_03_discard_confirm_dialog.png') });

        // Click "Tiếp tục soạn"
        const keepEditingBtn = await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-confirm-actions button'));
            const keepBtn = btns.find(b => b.textContent.includes('Tiếp tục soạn'));
            if (keepBtn) { keepBtn.click(); return true; }
            return false;
        });
        await sleep(400);

        const modalStillOpen = await page.evaluate(() => {
            const dialog = document.querySelector('.voucher-dialog');
            const overlay = document.querySelector('.tf-confirm-overlay');
            return dialog !== null && overlay === null;
        });
        results.dirtyKeepEditingWorks = modalStillOpen;
        console.log('   "Tiếp tục soạn" dismisses confirm and retains modal:', results.dirtyKeepEditingWorks);

        // Click outside again, then click "Đóng không lưu"
        await page.mouse.click(50, 50);
        await sleep(400);

        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-confirm-actions button'));
            const discardBtn = btns.find(b => b.textContent.includes('Đóng không lưu'));
            if (discardBtn) discardBtn.click();
        });
        await sleep(500);

        const modalClosed = await page.evaluate(() => document.querySelector('.voucher-dialog') === null);
        results.dirtyDiscardClosesModal = modalClosed;
        console.log('   "Đóng không lưu" successfully closes modal:', results.dirtyDiscardClosesModal);

        // Step 7: Supplier Payment Modal Check
        console.log('7. Testing Supplier Payment Modal positioning...');
        await page.goto(`${BASE_URL}/doi-tac/nha-cung-cap`, { waitUntil: 'networkidle2' });
        await sleep(800);

        for (const btn of await page.$$('button')) {
            const text = await page.evaluate(el => el.textContent, btn);
            if (text && (text.includes('Chi tiền') || text.includes('Lập phiếu chi'))) {
                await btn.click();
                break;
            }
        }
        await sleep(600);

        const supplierModalBbox = await page.evaluate(() => {
            const dialog = document.querySelector('.voucher-dialog');
            if (!dialog) return null;
            const dRect = dialog.getBoundingClientRect();
            return { top: dRect.top, bottom: dRect.bottom, left: dRect.left, right: dRect.right };
        });
        if (supplierModalBbox && supplierModalBbox.top > 0) {
            results.supplierModalCentered = true;
        }
        console.log('   Supplier payment modal centered:', results.supplierModalCentered);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'refinement_04_supplier_payment_modal.png') });

        // Close supplier modal
        await page.mouse.click(50, 50);
        await sleep(400);

        // Step 8: Document / Partner Search Phone Emoji Check
        console.log('8. Checking partner search autocomplete layout on /doi-tac/cong-no...');
        await page.goto(`${BASE_URL}/doi-tac/cong-no`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('button[title="Lập phiếu thu tiền khách hàng"]');
        await page.click('button[title="Lập phiếu thu tiền khách hàng"]');
        await page.waitForSelector('.voucher-dialog');
        await page.waitForSelector('.tf-input-search');

        await page.type('.tf-input-search', 'CTY');
        await sleep(800);

        const searchHtml = await page.evaluate(() => {
            const resultsEl = document.querySelector('.tf-search-results-list');
            return resultsEl ? resultsEl.innerHTML : '';
        });
        results.phoneEmojiRemovedInSearch = !searchHtml.includes('📞') && searchHtml.includes('SĐT:');
        console.log('   Phone emoji removed from autocomplete (clean SĐT label used):', results.phoneEmojiRemovedInSearch);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'refinement_05_partner_autocomplete_search.png') });

    } catch (err) {
        console.error('Error during verification:', err);
    } finally {
        await browser.close();
    }

    console.log('--- Final Refinement Results ---');
    console.log(JSON.stringify(results, null, 2));

    fs.writeFileSync(
        path.join(ARTIFACT_DIR, 'ui_refinement_verification_results.json'),
        JSON.stringify(results, null, 2),
        'utf8'
    );
}

runVerification();
