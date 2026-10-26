const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://localhost:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('[AUDIT] Starting Deep Dark Mode Verification Suite...');
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

    // 1. Login
    console.log('[1/10] Logging in as admin...');
    await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
    await page.waitForSelector('#login-username', { timeout: 10000 });
    
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    console.log('Logged in successfully. Current URL:', page.url());
    await sleep(2000);

    // Helper to ensure Dark Mode
    async function setDarkMode(enabled) {
        const theme = enabled ? 'dark' : 'light';
        await page.evaluate((t) => {
            document.documentElement.setAttribute('data-bs-theme', t);
            document.documentElement.setAttribute('data-theme', t);
            localStorage.setItem('tradeflow_theme', t);
            if (window.TradeFlow && window.TradeFlow.Theme) {
                window.TradeFlow.Theme.setTheme(t);
            }
        }, theme);
        await sleep(600);
    }

    const auditResults = {};

    // ==========================================================
    // 1. Warehouse Overview (/kho/ton-kho & /kho-hang)
    // ==========================================================
    console.log('[2/10] Verifying Warehouse Overview in Dark Mode...');
    await page.goto(`${BASE_URL}/kho/ton-kho`, { waitUntil: 'networkidle2' });
    await setDarkMode(true);
    await sleep(1500);

    let whNavColor = await page.evaluate(() => {
        const nav = document.querySelector('.tf-warehouse-nav-container');
        if (!nav) return 'not found';
        const style = window.getComputedStyle(nav);
        return { bg: style.backgroundColor, border: style.borderColor };
    });
    console.log('   Warehouse Nav Styles:', whNavColor);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_01_warehouse_stock_dark.png') });

    await page.goto(`${BASE_URL}/kho-hang`, { waitUntil: 'networkidle2' });
    await setDarkMode(true);
    await sleep(1500);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_01_warehouse_catalogue_dark.png') });

    // ==========================================================
    // 2. Warehouse Voucher View (/kho/phieu-kho/1)
    // ==========================================================
    console.log('[3/10] Verifying Warehouse Voucher Detail & Isolation...');
    await page.goto(`${BASE_URL}/kho/phieu-kho`, { waitUntil: 'networkidle2' });
    await setDarkMode(true);
    await sleep(1500);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_02_warehouse_vouchers_list_dark.png') });

    // Go to first voucher detail
    const firstVoucherLink = await page.$('a[href^="/kho/phieu-kho/"]');
    if (firstVoucherLink) {
        await firstVoucherLink.click();
        await page.waitForNavigation({ waitUntil: 'networkidle2' }).catch(() => {});
        await setDarkMode(true);
        await sleep(1500);

        const voucherPaperEval = await page.evaluate(() => {
            const paper = document.querySelector('.voucher-a4-sheet, #print-warehouse-voucher');
            if (!paper) return null;
            const style = window.getComputedStyle(paper);
            return {
                bg: style.backgroundColor,
                color: style.color
            };
        });
        console.log('   Warehouse Voucher Paper Colors:', voucherPaperEval);
        auditResults.voucherPaper = voucherPaperEval;

        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_02_warehouse_voucher_detail_dark.png') });
    }

    // ==========================================================
    // 3. Debt Management (/doi-tac/cong-no)
    // ==========================================================
    console.log('[4/10] Verifying Debt Management Page in Dark Mode...');
    await page.goto(`${BASE_URL}/doi-tac/cong-no`, { waitUntil: 'networkidle2' });
    await setDarkMode(true);
    await sleep(2000);

    const debtPageEval = await page.evaluate(() => {
        const kpiCard = document.querySelector('.tf-debt-kpi-card');
        const sidebar = document.querySelector('.tf-filter-sidebar');
        const mainCard = document.querySelector('.tf-main-card');
        return {
            kpiBg: kpiCard ? window.getComputedStyle(kpiCard).backgroundColor : null,
            sidebarBg: sidebar ? window.getComputedStyle(sidebar).backgroundColor : null,
            mainCardBg: mainCard ? window.getComputedStyle(mainCard).backgroundColor : null
        };
    });
    console.log('   Debt Page Surfaces:', debtPageEval);
    auditResults.debtPage = debtPageEval;

    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_03_debt_management_dark.png') });

    // ==========================================================
    // 4. Payment/Receipt Modal on Debt Page
    // ==========================================================
    console.log('[5/10] Triggering Payment/Receipt Modal...');
    const receiptBtn = await page.$('.tf-page-actions button, button.tf-btn-primary');
    if (receiptBtn) {
        await receiptBtn.click();
        await sleep(1200);

        const modalEval = await page.evaluate(() => {
            const dialog = document.querySelector('.tf-modal-dialog.voucher-dialog, .tf-modal-dialog');
            const header = document.querySelector('.tf-modal-header');
            const body = document.querySelector('.tf-modal-body');
            return {
                dialogBg: dialog ? window.getComputedStyle(dialog).backgroundColor : null,
                headerBg: header ? window.getComputedStyle(header).backgroundColor : null,
                bodyBg: body ? window.getComputedStyle(body).backgroundColor : null
            };
        });
        console.log('   Payment Modal Colors:', modalEval);
        auditResults.paymentModal = modalEval;

        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_04_payment_receipt_modal_dark.png') });

        // Close modal
        const closeBtn = await page.$('.tf-btn-close-modal, .tf-modal-header button, .tf-modal-footer button');
        if (closeBtn) await closeBtn.click();
        await sleep(500);
    }

    // ==========================================================
    // 5. Contract Management & Template Selector (/hop-dong)
    // ==========================================================
    console.log('[6/10] Verifying Contract Management & Template Gallery...');
    await page.goto(`${BASE_URL}/hop-dong`, { waitUntil: 'networkidle2' });
    await setDarkMode(true);
    await sleep(2000);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_05_contract_list_dark.png') });

    // Open Template Selector Modal
    const templateModalBtn = await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        const btn = btns.find(b => b.textContent && b.textContent.includes('Tạo hợp đồng mới'));
        if (btn) {
            btn.click();
            return true;
        }
        return false;
    });

    if (templateModalBtn) {
        await sleep(1500);
        const templateGalleryEval = await page.evaluate(() => {
            const galleryContent = document.querySelector('.tf-gallery-modal-content');
            const galleryCard = document.querySelector('.tf-gallery-card');
            const miniPaper = document.querySelector('.tf-a4-sheet');
            return {
                modalBg: galleryContent ? window.getComputedStyle(galleryContent).backgroundColor : null,
                cardBg: galleryCard ? window.getComputedStyle(galleryCard).backgroundColor : null,
                miniPaperBg: miniPaper ? window.getComputedStyle(miniPaper).backgroundColor : null
            };
        });
        console.log('   Template Gallery Modal Colors:', templateGalleryEval);
        auditResults.templateGallery = templateGalleryEval;

        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_05_contract_template_selector_dark.png') });

        // Close template modal
        await page.evaluate(() => {
            const closeBtn = document.querySelector('.tf-modal-close-icon, .tf-gallery-modal-header button');
            if (closeBtn) closeBtn.click();
        });
        await sleep(600);
    }

    // ==========================================================
    // 6. Actual Contract Preview (/hop-dong/1 or 2)
    // ==========================================================
    console.log('[7/10] Verifying Actual Contract Preview & Document Paper Isolation...');
    await page.goto(`${BASE_URL}/hop-dong`, { waitUntil: 'networkidle2' });
    await sleep(1000);
    const contractLink = await page.$('a.tf-contract-number');
    let previewUrl = `${BASE_URL}/hop-dong/1`;
    if (contractLink) {
        const href = await page.evaluate(el => el.getAttribute('href'), contractLink);
        if (href) previewUrl = `${BASE_URL}${href}`;
    }
    console.log('   Navigating to contract preview:', previewUrl);

    await page.goto(previewUrl, { waitUntil: 'networkidle2' });
    await setDarkMode(true);
    await sleep(2000);

    const contractPreviewEval = await page.evaluate(() => {
        const paper = document.querySelector('.tf-document-paper, .a4-contract-page');
        const partyBox = document.querySelector('.tf-doc-party-box');
        const summaryBox = document.querySelector('.tf-doc-summary-box');
        const toolbar = document.querySelector('.tf-card');

        return {
            paperBg: paper ? window.getComputedStyle(paper).backgroundColor : null,
            paperColor: paper ? window.getComputedStyle(paper).color : null,
            partyBoxBg: partyBox ? window.getComputedStyle(partyBox).backgroundColor : null,
            partyBoxColor: partyBox ? window.getComputedStyle(partyBox).color : null,
            summaryBoxBg: summaryBox ? window.getComputedStyle(summaryBox).backgroundColor : null,
            summaryBoxColor: summaryBox ? window.getComputedStyle(summaryBox).color : null,
            toolbarBg: toolbar ? window.getComputedStyle(toolbar).backgroundColor : null
        };
    });
    console.log('   Contract Preview Colors:', contractPreviewEval);
    auditResults.contractPreview = contractPreviewEval;

    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_06_contract_detail_preview_dark.png'), fullPage: false });

    // ==========================================================
    // 7. Employee Approval Modal (/nhan-vien)
    // ==========================================================
    console.log('[8/10] Verifying Employee Approval Modal...');
    await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
    await setDarkMode(true);
    await sleep(1500);

    // Look for Employee Approval trigger button
    const approvalBtn = await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        const target = btns.find(b => b.textContent && b.textContent.includes('Duyệt yêu cầu'));
        if (target) {
            target.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
            return true;
        }
        return false;
    });

    if (approvalBtn) {
        await sleep(1500);
        const empModalEval = await page.evaluate(() => {
            const body = document.querySelector('.tf-req-body');
            const card = document.querySelector('.tf-req-card');
            return {
                bodyBg: body ? window.getComputedStyle(body).backgroundColor : null,
                cardBg: card ? window.getComputedStyle(card).backgroundColor : null
            };
        });
        console.log('   Employee Approval Modal Colors:', empModalEval);
        auditResults.employeeModal = empModalEval;

        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_07_employee_approval_modal_dark.png') });

        // Close modal
        await page.evaluate(() => {
            const closeBtn = document.querySelector('.tf-modal-header button, .tf-modal-footer button');
            if (closeBtn) closeBtn.click();
        });
        await sleep(500);
    } else {
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_07_employee_list_dark.png') });
    }

    // ==========================================================
    // 8. Branding & Loading Screen Settings (/cai-dat/thuong-hieu)
    // ==========================================================
    console.log('[9/10] Verifying Branding & Loading Screen Settings...');
    await page.goto(`${BASE_URL}/cai-dat/thuong-hieu`, { waitUntil: 'networkidle2' });
    await setDarkMode(true);
    await sleep(1800);

    const brandEval = await page.evaluate(() => {
        const previewStage = document.querySelector('.tf-preview-stage, .tf-card .tf-card-body > div');
        const cardHeader = document.querySelector('.tf-card-header h3');
        return {
            previewStageBg: previewStage ? window.getComputedStyle(previewStage).backgroundColor : null,
            cardHeaderColor: cardHeader ? window.getComputedStyle(cardHeader).color : null
        };
    });
    console.log('   Brand Settings Colors:', brandEval);
    auditResults.brandSettings = brandEval;

    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_08_brand_settings_dark.png') });

    // ==========================================================
    // 9. Backup & Restore (/cai-dat/sao-luu)
    // ==========================================================
    console.log('[10/10] Verifying Backup & Restore in Dark Mode...');
    await page.goto(`${BASE_URL}/cai-dat/sao-luu`, { waitUntil: 'networkidle2' });
    await setDarkMode(true);
    await sleep(1800);

    const backupEval = await page.evaluate(() => {
        const card = document.querySelector('.tf-card');
        const title = document.querySelector('.tf-page-title');
        const scopeBox = document.querySelector('.tf-backup-scope-info');
        return {
            cardBg: card ? window.getComputedStyle(card).backgroundColor : null,
            titleColor: title ? window.getComputedStyle(title).color : null,
            scopeBg: scopeBox ? window.getComputedStyle(scopeBox).backgroundColor : null
        };
    });
    console.log('   Backup & Restore Colors:', backupEval);
    auditResults.backupSettings = backupEval;

    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_09_backup_restore_export_dark.png') });

    // Switch to Import Tab
    await page.evaluate(() => {
        const tabs = Array.from(document.querySelectorAll('.tf-backup-tab'));
        if (tabs.length > 1) tabs[1].click();
    });
    await sleep(1000);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_09_backup_restore_import_dark.png') });

    // Save JSON results to artifact directory
    fs.writeFileSync(
        path.join(ARTIFACT_DIR, 'dark_mode_deep_audit_verification_results.json'),
        JSON.stringify(auditResults, null, 2),
        'utf-8'
    );

    console.log('All 9 Defect Area Audits and Screenshots successfully completed!');
    await browser.close();
})().catch(err => {
    console.error('Audit suite error:', err);
    process.exit(1);
});
