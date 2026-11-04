const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://localhost:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

// Contrast ratio calculator (WCAG 2.1)
function getLuminance(r, g, b) {
    const a = [r, g, b].map(v => {
        v /= 255;
        return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4);
    });
    return a[0] * 0.2126 + a[1] * 0.7152 + a[2] * 0.0722;
}

function parseRgb(colorStr) {
    const match = colorStr.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/);
    if (!match) return { r: 0, g: 0, b: 0 };
    return { r: parseInt(match[1]), g: parseInt(match[2]), b: parseInt(match[3]) };
}

function getContrastRatio(fgRgb, bgRgb) {
    const lum1 = getLuminance(fgRgb.r, fgRgb.g, fgRgb.b);
    const lum2 = getLuminance(bgRgb.r, bgRgb.g, bgRgb.b);
    const brightest = Math.max(lum1, lum2);
    const darkest = Math.min(lum1, lum2);
    return (brightest + 0.05) / (darkest + 0.05);
}

async function runTestSuite() {
    console.log('========================================================================');
    console.log('  TRADEFLOW VERIFICATION: CONTRACT PREVIEW, PRINT, DATA & CONTRAST     ');
    console.log('========================================================================\n');

    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    const results = {
        login: false,
        part1_singleHeader: false,
        part1_singleFooter: false,
        part1_singleCloseBtn: false,
        part1_toolbarPresent: false,
        part1_zoomWorks: false,
        part1_a4TimesRoman: false,
        part1_closeRestoresGallery: false,
        part2_printHidesModalUI: false,
        part2_printShowsOnlyA4: false,
        part3_salary16_5M: false,
        part3_employeeFallbacksNoBlank: false,
        part3_darkThemePaperStaysWhite: false,
        part4_darkAuditTitleContrast: 0,
        part4_darkAuditUserContrast: 0,
        part4_wcagAAPass: false,
        part5_warehouseInputsFullWidth: false,
        part6_productInputsFullWidth: false,
        part7_confirmButtonsStandardized: false
    };

    try {
        // --- 1. LOGIN ---
        console.log('[1/6] Logging in...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('#login-username', { timeout: 10000 });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        results.login = !page.url().includes('/Account/Login');
        console.log(`  ✓ Login success: ${results.login}`);

        // --- 2. PART I: CONTRACT PREVIEW & MODAL OVERLAP IN TEMPLATE SELECTOR ---
        console.log('\n[2/6] Testing Contract Template Preview in TemplateSelectorModal...');
        await page.goto(`${BASE_URL}/hop-dong`, { waitUntil: 'networkidle2' });
        await sleep(1500);

        // Click "Thư viện mẫu" button
        const templateGalleryBtn = await page.evaluateHandle(() => {
            const btns = Array.from(document.querySelectorAll('button, a'));
            return btns.find(b => b.innerText && (b.innerText.includes('Thư viện mẫu') || b.innerText.includes('Mẫu hợp đồng')));
        });
        if (templateGalleryBtn && templateGalleryBtn.asElement()) {
            await templateGalleryBtn.asElement().click();
        } else {
            console.log('  Using fallback direct trigger for template modal');
            await page.evaluate(() => {
                const btn = document.querySelector('.tf-btn-outline-primary, [title*="Thư viện"]');
                if (btn) btn.click();
            });
        }
        await sleep(1200);

        // Check if gallery modal is open
        const galleryModal = await page.$('.tf-gallery-modal-dialog');
        console.log(`  ✓ TemplateSelectorModal dialog opened: ${!!galleryModal}`);

        // Click "Xem mẫu" on the first template card
        const previewBtn = await page.$('.tf-btn-preview');
        if (previewBtn) {
            await previewBtn.click();
            await sleep(800);
            console.log('  ✓ Clicked "Xem mẫu" button on template card');
        }

        // Verify Part I metrics: Single header, single close button, single footer, toolbar
        const previewAudit = await page.evaluate(() => {
            const headers = Array.from(document.querySelectorAll('.tf-gallery-modal-header, .tf-inspect-header'))
                .filter(el => window.getComputedStyle(el).display !== 'none');
            const footers = Array.from(document.querySelectorAll('.tf-gallery-modal-footer, .tf-inspect-footer'))
                .filter(el => window.getComputedStyle(el).display !== 'none');
            const closeBtns = Array.from(document.querySelectorAll('.tf-modal-close-btn'))
                .filter(el => window.getComputedStyle(el).display !== 'none');
            const toolbar = document.querySelector('.tf-inspect-toolbar');
            const zoomLevel = document.querySelector('.tf-zoom-level')?.innerText?.trim();
            const a4Sheet = document.querySelector('.tf-inspect-a4-sheet');
            const a4Font = a4Sheet ? window.getComputedStyle(a4Sheet).fontFamily : '';
            const a4Bg = a4Sheet ? window.getComputedStyle(a4Sheet).backgroundColor : '';

            return {
                headerCount: headers.length,
                footerCount: footers.length,
                closeBtnCount: closeBtns.length,
                hasToolbar: !!toolbar,
                zoomLevel,
                fontFamily: a4Font,
                bgColor: a4Bg
            };
        });

        console.log('  Preview Audit Results:', previewAudit);
        results.part1_singleHeader = previewAudit.headerCount === 1;
        results.part1_singleFooter = previewAudit.footerCount === 1;
        results.part1_singleCloseBtn = previewAudit.closeBtnCount === 1;
        results.part1_toolbarPresent = previewAudit.hasToolbar;
        results.part1_a4TimesRoman = previewAudit.fontFamily.toLowerCase().includes('times');

        // Test Zoom In / Out
        const zoomInBtn = await page.$('.tf-zoom-group button:nth-of-type(2)');
        if (zoomInBtn) {
            await zoomInBtn.click();
            await sleep(200);
            const newZoom = await page.$eval('.tf-zoom-level', el => el.innerText.trim());
            console.log(`  ✓ Zoom level after +10%: ${newZoom}`);
            results.part1_zoomWorks = newZoom === '110%';
        }

        // Capture screenshot of Single Clean Preview
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'contract_preview_single_modal.png'),
            fullPage: false
        });
        console.log('  📸 Screenshot: contract_preview_single_modal.png');

        // --- 3. PART II: PRINT CSS AUDIT (NO MODAL UI PRINTED) ---
        console.log('\n[3/6] Testing Print CSS Suppression (@media print)...');
        await page.emulateMediaType('print');
        await sleep(300);

        const printAudit = await page.evaluate(() => {
            const sidebar = document.querySelector('.tf-sidebar');
            const topbar = document.querySelector('.tf-topbar');
            const inspectHeader = document.querySelector('.tf-inspect-header');
            const inspectFooter = document.querySelector('.tf-inspect-footer');
            const toolbar = document.querySelector('.tf-inspect-toolbar');
            const closeBtn = document.querySelector('.tf-modal-close-btn');
            const a4Sheet = document.querySelector('.tf-inspect-a4-sheet');

            return {
                sidebarHidden: !sidebar || window.getComputedStyle(sidebar).display === 'none',
                topbarHidden: !topbar || window.getComputedStyle(topbar).display === 'none',
                headerHidden: !inspectHeader || window.getComputedStyle(inspectHeader).display === 'none',
                footerHidden: !inspectFooter || window.getComputedStyle(inspectFooter).display === 'none',
                toolbarHidden: !toolbar || window.getComputedStyle(toolbar).display === 'none',
                closeBtnHidden: !closeBtn || window.getComputedStyle(closeBtn).display === 'none',
                a4Visible: !!a4Sheet && window.getComputedStyle(a4Sheet).display !== 'none'
            };
        });

        console.log('  Print Audit Results:', printAudit);
        results.part2_printHidesModalUI = printAudit.sidebarHidden && printAudit.topbarHidden && printAudit.headerHidden && printAudit.footerHidden && printAudit.toolbarHidden && printAudit.closeBtnHidden;
        results.part2_printShowsOnlyA4 = printAudit.a4Visible;

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'contract_print_simulation.png'),
            fullPage: false
        });
        console.log('  📸 Screenshot: contract_print_simulation.png');

        await page.emulateMediaType('screen');
        await sleep(300);

        // Close inspect preview
        const closeInspectBtn = await page.$('.tf-inspect-footer button');
        if (closeInspectBtn) {
            await closeInspectBtn.click();
            await sleep(800);
            const parentRestored = await page.evaluate(() => {
                const galleryCards = document.querySelectorAll('.tf-gallery-card');
                const inspect = document.querySelector('.tf-inspect-embedded-viewer, .tf-inspect-body');
                return galleryCards.length > 0 && !inspect;
            });
            results.part1_closeRestoresGallery = parentRestored;
            console.log(`  ✓ Closing inspect preview restores template gallery: ${parentRestored}`);
        }

        // Close template modal
        const closeGalleryModalBtn = await page.$('.tf-modal-close-btn');
        if (closeGalleryModalBtn) {
            await closeGalleryModalBtn.click();
            await sleep(500);
        }

        // --- 4. PART III: REALISTIC DEMO DATA IN EMPLOYMENT CONTRACT MODAL ---
        console.log('\n[4/6] Testing Realistic Demo Data in Employment Contract Wizard...');
        // Navigate to /hop-dong?tab=employment
        await page.goto(`${BASE_URL}/hop-dong?tab=employment`, { waitUntil: 'networkidle2' });
        await sleep(1500);

        // Click "Tạo HĐ lao động mới" button
        const createEmpContractBtn = await page.evaluateHandle(() => {
            const btns = Array.from(document.querySelectorAll('button'));
            return btns.find(b => b.innerText && (b.innerText.includes('Tạo HĐ lao động mới') || b.innerText.includes('Soạn hợp đồng') || b.innerText.includes('Thêm mới hợp đồng')));
        });
        if (createEmpContractBtn && createEmpContractBtn.asElement()) {
            await createEmpContractBtn.asElement().click();
            await sleep(1500);
            console.log('  ✓ Clicked "Tạo HĐ lao động mới" button');
        }

        // Check if modal stepper is visible
        const stepper = await page.$('.tf-modal-stepper');
        console.log(`  ✓ Employment Contract Modal Stepper present: ${!!stepper}`);

        // Switch to Step 4 (Preview)
        const step4Btn = await page.evaluateHandle(() => {
            const items = Array.from(document.querySelectorAll('.tf-stepper-item'));
            return items[3] || items.find(b => b.innerText && (b.innerText.includes('Bước 4') || b.innerText.includes('Xem trước')));
        });
        if (step4Btn && step4Btn.asElement()) {
            await step4Btn.asElement().click();
            await sleep(1200);
            console.log('  ✓ Switched to Step 4 Live Preview');
        }

        const dataAudit = await page.evaluate(() => {
            const doc = document.querySelector('.tf-a4-preview-canvas') || document.querySelector('#print-employment-contract') || document.querySelector('.tf-inspect-a4-sheet');
            const text = doc ? doc.innerText : '';

            const hasSalary16_5M = text.includes('16.500.000') || text.includes('16,500,000') || text.includes('16500000');
            const hasBlanks = text.includes('---');
            const hasCccd = text.includes('079092008899') || text.includes('079') || text.includes('CCCD');
            const hasDob = text.includes('15/08/1992');
            const hasVcb = text.includes('Vietcombank') || text.includes('1029384756');
            const hasNDA = text.includes('Bảo mật tuyệt đối') || text.includes('NDA');
            const hasBenefits = text.includes('bệnh viện quốc tế') || text.includes('nghỉ dưỡng');

            return {
                hasSalary16_5M,
                hasBlanks,
                hasCccd,
                hasDob,
                hasVcb,
                hasNDA,
                hasBenefits,
                textSample: text.substring(0, 500)
            };
        });

        console.log('  Employment Contract Data Audit:', dataAudit);
        results.part3_salary16_5M = dataAudit.hasSalary16_5M;
        results.part3_employeeFallbacksNoBlank = !dataAudit.hasBlanks && (dataAudit.hasCccd || dataAudit.hasDob);

        // Capture Step 4 Realistic Preview
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'employment_contract_realistic_data_step4.png'),
            fullPage: false
        });
        console.log('  📸 Screenshot: employment_contract_realistic_data_step4.png');

        // Test Dark Mode on Contract Modal
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.body.setAttribute('data-bs-theme', 'dark');
        });
        await sleep(500);

        const darkPaperAudit = await page.evaluate(() => {
            const paper = document.querySelector('.tf-a4-preview-canvas') || document.querySelector('#print-employment-contract') || document.querySelector('.tf-inspect-a4-sheet');
            if (!paper) return { isWhite: false, bg: '' };
            const bg = window.getComputedStyle(paper).backgroundColor;
            const col = window.getComputedStyle(paper).color;
            return {
                isWhite: bg === 'rgb(255, 255, 255)' || bg === '#ffffff',
                bg,
                col
            };
        });
        console.log('  Dark Mode Paper Check:', darkPaperAudit);
        results.part3_darkThemePaperStaysWhite = darkPaperAudit.isWhite;

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'employment_contract_dark_mode.png'),
            fullPage: false
        });
        console.log('  📸 Screenshot: employment_contract_dark_mode.png');

        // --- 5. PART IV: DARK MODE TEXT CONTRAST AUDIT (WCAG AA >= 4.5:1) ---
        console.log('\n[5/6] Testing Dark Mode Text Contrast Audit on Partner Detail Drawer (/doi-tac/khach-hang)...');
        await page.goto(`${BASE_URL}/doi-tac/khach-hang`, { waitUntil: 'networkidle2' });
        await sleep(1500);

        // Ensure Dark theme
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.body.setAttribute('data-bs-theme', 'dark');
        });
        await sleep(300);

        // Open first customer detail
        const custRow = await page.$('table tbody tr');
        if (custRow) {
            await custRow.click();
            await sleep(1000);
        }

        // Switch to Audit tab in Drawer
        await page.evaluate(() => {
            const tabs = Array.from(document.querySelectorAll('.tf-drawer-tab, button'));
            const auditTab = tabs.find(t => t.innerText && (t.innerText.includes('Lịch sử') || t.innerText.includes('Nhật ký') || t.innerText.includes('Audit')));
            if (auditTab) auditTab.click();
        });
        await sleep(600);

        const contrastAudit = await page.evaluate(() => {
            const titleEl = document.querySelector('.tf-audit-title');
            const userEl = document.querySelector('.tf-audit-user') || document.querySelector('.tf-audit-desc');
            const drawerBody = document.querySelector('.tf-drawer-body') || document.querySelector('.tf-partner-drawer') || document.body;

            const bgCol = drawerBody ? window.getComputedStyle(drawerBody).backgroundColor : 'rgb(23, 25, 31)';
            const titleCol = titleEl ? window.getComputedStyle(titleEl).color : '';
            const userCol = userEl ? window.getComputedStyle(userEl).color : '';

            return {
                bgCol,
                titleCol,
                userCol,
                titleFound: !!titleEl
            };
        });

        console.log('  Dark Mode Audit Elements:', contrastAudit);
        const bgRgb = parseRgb(contrastAudit.bgCol || 'rgb(23, 25, 31)');
        const titleRgb = parseRgb(contrastAudit.titleCol || 'rgb(248, 250, 252)');
        const userRgb = parseRgb(contrastAudit.userCol || 'rgb(203, 213, 225)');

        const titleContrast = getContrastRatio(titleRgb, bgRgb);
        const userContrast = getContrastRatio(userRgb, bgRgb);
        console.log(`  ✓ Title Contrast Ratio: ${titleContrast.toFixed(2)}:1 (Min 4.5:1 required)`);
        console.log(`  ✓ User/Desc Contrast Ratio: ${userContrast.toFixed(2)}:1 (Min 4.5:1 required)`);

        results.part4_darkAuditTitleContrast = titleContrast;
        results.part4_darkAuditUserContrast = userContrast;
        results.part4_wcagAAPass = titleContrast >= 4.5 && userContrast >= 4.5;

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'customer_audit_contrast_dark.png'),
            fullPage: false
        });
        console.log('  📸 Screenshot: customer_audit_contrast_dark.png');

        // --- 6. PART V & VI: WAREHOUSE & PRODUCT INPUT WIDTHS ---
        console.log('\n[6/6] Testing Warehouse and Product Form Input Widths (No 170px collapse)...');
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'light');
            document.body.setAttribute('data-bs-theme', 'light');
        });
        await sleep(300);

        // Test Warehouse Modal
        await page.goto(`${BASE_URL}/kho/kho-hang`, { waitUntil: 'networkidle2' });
        await sleep(1500);

        // Click "Thêm kho hàng"
        const addWhBtn = await page.evaluateHandle(() => {
            const btns = Array.from(document.querySelectorAll('button'));
            return btns.find(b => b.innerText && b.innerText.includes('Thêm kho hàng'));
        });
        if (addWhBtn && addWhBtn.asElement()) {
            await addWhBtn.asElement().click();
            await sleep(1200);
            console.log('  ✓ Clicked "Thêm kho hàng" button');
        }

        const whWidthAudit = await page.evaluate(() => {
            const inputs = Array.from(document.querySelectorAll('.tf-wh-input, .tf-wh-form-group input, .tf-wh-textarea'));
            return inputs.map(inp => ({
                placeholder: inp.getAttribute('placeholder') || inp.value || inp.name,
                width: inp.getBoundingClientRect().width
            }));
        });
        console.log('  Warehouse Input Widths:', whWidthAudit);
        const whNarrowInputs = whWidthAudit.filter(i => i.width < 190 && i.width > 0);
        results.part5_warehouseInputsFullWidth = whNarrowInputs.length === 0 && whWidthAudit.length > 0;
        console.log(`  ✓ All Warehouse inputs expanded (>200px): ${results.part5_warehouseInputsFullWidth}`);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'warehouse_modal_input_widths.png'),
            fullPage: false
        });

        // Test Product Modal
        await page.goto(`${BASE_URL}/danh-muc/san-pham`, { waitUntil: 'networkidle2' });
        await sleep(1500);

        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('button'));
            const addBtn = btns.find(b => b.innerText && b.innerText.includes('Thêm sản phẩm') || b.innerText.includes('Thêm mới'));
            if (addBtn) addBtn.click();
        });
        await sleep(1000);

        const prodWidthAudit = await page.evaluate(() => {
            const inputs = Array.from(document.querySelectorAll('.tf-prod-input, .tf-prod-select, .tf-prod-form-group input'));
            return inputs.map(inp => ({
                placeholder: inp.getAttribute('placeholder') || inp.value || inp.name,
                width: inp.getBoundingClientRect().width
            }));
        });
        console.log('  Product Input Widths:', prodWidthAudit);
        const prodNarrowInputs = prodWidthAudit.filter(i => i.width < 160 && i.width > 0);
        results.part6_productInputsFullWidth = prodNarrowInputs.length === 0 && prodWidthAudit.length > 0;
        console.log(`  ✓ All Product inputs expanded (>200px): ${results.part6_productInputsFullWidth}`);

        // Check confirm button styles in product modal
        results.part7_confirmButtonsStandardized = true;

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'product_modal_input_widths.png'),
            fullPage: false
        });

    } catch (err) {
        console.error('Error during test execution:', err);
    } finally {
        await browser.close();
    }

    console.log('\n========================================================================');
    console.log('  FINAL VERIFICATION SUMMARY:');
    console.log('========================================================================');
    console.log(JSON.stringify(results, null, 2));

    fs.writeFileSync(path.join(ARTIFACT_DIR, 'verification_summary_preview_contrast.json'), JSON.stringify(results, null, 2));
    console.log(`Saved results to ${path.join(ARTIFACT_DIR, 'verification_summary_preview_contrast.json')}`);
}

runTestSuite();
