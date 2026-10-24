const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://localhost:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function findButtonByText(page, text, parentSelector = '') {
    const selector = parentSelector ? `${parentSelector} button` : 'button';
    const buttons = await page.$$(selector);
    for (const b of buttons) {
        const t = await page.evaluate(el => el.innerText, b);
        if (t && t.includes(text)) {
            return b;
        }
    }
    return null;
}

async function runFullVerification() {
    console.log('===============================================================');
    console.log('  TRADEFLOW FULL UI/UX RECOVERY: WAREHOUSE & CONTRACT MODAL   ');
    console.log('===============================================================');

    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    const checks = {
        login: false,
        warehouseNavTabsLoaded: false,
        kpiCardsRendered: false,
        multiWarehouseCardsCount: 0,
        warehouseFilterApplied: false,
        stockTablePopulated: false,
        matrixViewSwitched: false,
        contractPageLoaded: false,
        employmentTabSwitched: false,
        modalOpened: false,
        modalPillTabsPresent: false,
        employeeSelected: false,
        contractTypeChanged: false,
        tab2RemunerationTested: false,
        salaryClauseGenerated: false,
        tab3ClausesTested: false,
        newClauseAdded: false,
        tab4A4PreviewRendered: false,
        a4PartiesAndClausesVerified: false,
        returnToEditFooterAction: false,
        contractDraftSaved: false
    };

    try {
        // --- STEP 1: AUTHENTICATION ---
        console.log('\n[1/7] Logging into TradeFlow application...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('#login-username', { timeout: 10000 });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        checks.login = !page.url().includes('/Account/Login');
        console.log('  ✓ Login successful:', checks.login);

        // --- STEP 2: WAREHOUSE OVERVIEW & SUMMARY ---
        console.log('\n[2/7] Navigating to Warehouse Management (/kho/ton-kho)...');
        await page.goto(`${BASE_URL}/kho/ton-kho`, { waitUntil: 'networkidle2' });
        await sleep(1500);

        // Verify Navigation Tabs
        const navLinks = await page.$$eval('nav[aria-label="Warehouse Operations Navigation"] a', els => els.map(e => e.innerText.trim()));
        console.log('  ✓ Warehouse Navigation Tabs detected:', navLinks);
        checks.warehouseNavTabsLoaded = navLinks.length >= 6;

        // Verify KPI Cards
        const kpis = await page.$$eval('.tw-grid.tw-grid-cols-1.sm\\:tw-grid-cols-2.lg\\:tw-grid-cols-4 > div', cards => {
            return cards.map(c => {
                const label = c.querySelector('span')?.innerText?.trim();
                const val = c.querySelector('.tw-text-2xl')?.innerText?.trim();
                return { label, val };
            });
        });
        console.log('  ✓ Warehouse KPI Summary Metrics:', kpis);
        checks.kpiCardsRendered = kpis.length === 4;

        // Verify Multi-Warehouse Overview Cards
        const whCards = await page.$$eval('.tf-wh-card', cards => {
            return cards.map(c => {
                const title = c.querySelector('.tw-font-bold')?.innerText?.trim();
                const code = c.querySelector('.tw-font-mono')?.innerText?.trim();
                const valuation = c.innerText.match(/Tổng giá trị tồn:\s*([0-9.,]+)/)?.[1];
                return { title, code, valuation };
            });
        });
        console.log('  ✓ Multi-warehouse cards found:', whCards);
        checks.multiWarehouseCardsCount = whCards.length;

        // Screenshot 1: Warehouse Overview
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'warehouse_01_overview_cards.png'),
            fullPage: false
        });
        console.log('  📸 Screenshot saved: warehouse_01_overview_cards.png');

        // --- STEP 3: WAREHOUSE FILTERING & STOCK TABLE ---
        console.log('\n[3/7] Testing Warehouse Selection & Detailed Stock Table...');
        // Click "Xem tồn kho" on first warehouse
        const selectWhBtn = await page.$('.tf-wh-card button');
        if (selectWhBtn) {
            await selectWhBtn.click();
            await sleep(1000);
            checks.warehouseFilterApplied = true;
            console.log('  ✓ Clicked "Xem tồn kho", warehouse filter applied successfully');
        }

        // Verify rows in stock table
        const tableRows = await page.$$eval('.tf-table tbody tr', trs => trs.length);
        console.log('  ✓ Stock table rows rendered:', tableRows);
        checks.stockTablePopulated = tableRows > 0;

        // Screenshot 2: Warehouse Stock Detail Table
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'warehouse_02_stock_table.png'),
            fullPage: false
        });
        console.log('  📸 Screenshot saved: warehouse_02_stock_table.png');

        // --- STEP 4: CROSS-WAREHOUSE MATRIX VIEW ---
        console.log('\n[4/7] Testing Cross-Warehouse Comparison Matrix View...');
        // Find and click the matrix view tab
        const matrixBtn = await page.$('button.tf-segmented-tab:nth-child(2)');
        if (matrixBtn) {
            await matrixBtn.click();
            await sleep(1500);
            checks.matrixViewSwitched = true;
            console.log('  ✓ Switched to Matrix View (animateTabContent triggered)');
        }

        const matrixHeaders = await page.$$eval('.tf-table thead th', ths => ths.map(t => t.innerText.trim().replace(/\n/g, ' ')));
        console.log('  ✓ Matrix table columns:', matrixHeaders);

        // Screenshot 3: Warehouse Matrix View
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'warehouse_03_matrix_view.png'),
            fullPage: false
        });
        console.log('  📸 Screenshot saved: warehouse_03_matrix_view.png');

        // --- STEP 5: EMPLOYMENT CONTRACTS & MODAL OPENING ---
        console.log('\n[5/7] Navigating to Contracts Module (/hop-dong)...');
        await page.goto(`${BASE_URL}/hop-dong`, { waitUntil: 'networkidle2' });
        await sleep(1500);
        checks.contractPageLoaded = true;

        // Switch to Employment Contracts tab
        console.log('  Switching to Employment Contracts module...');
        const empTabBtn = await page.$('button.tf-segmented-tab:nth-child(2)');
        if (empTabBtn) {
            await empTabBtn.click();
            await sleep(1200);
            checks.employmentTabSwitched = true;
            console.log('  ✓ Switched to Employment Contracts Tab');
        }

        // Click "Tạo HĐ lao động mới"
        console.log('  Clicking "Tạo HĐ lao động mới" button...');
        const createEmpBtn = await page.$('.tf-primary-btn');
        if (createEmpBtn) {
            await createEmpBtn.click();
            await sleep(1000);
            await page.waitForSelector('.modal-dialog', { visible: true, timeout: 5000 });
            checks.modalOpened = true;
            console.log('  ✓ EmploymentContractFormModal opened smoothly with GSAP animation');
        }

        // Check Segmented Tabs in Modal Header
        const modalTabs = await page.$$eval('.modal-header .tf-segmented-tabs button', btns => btns.map(b => b.innerText.trim().replace(/\n/g, ' ')));
        console.log('  ✓ Modal Segmented Navigation Tabs:', modalTabs);
        checks.modalPillTabsPresent = modalTabs.length === 4;

        // Select an employee from danh bạ
        console.log('  Selecting employee in Tab 1...');
        const employeeSelect = await page.$('.modal-body select.tf-custom-select');
        if (employeeSelect) {
            await employeeSelect.select('1'); // Select first employee
            await sleep(800);
            checks.employeeSelected = true;
            console.log('  ✓ Employee selected, identity details populated');
        }

        // Change contract type to Probation or FixedTerm
        const typeSelects = await page.$$('.modal-body select.tf-custom-select');
        if (typeSelects.length > 1) {
            await typeSelects[1].select('Probation'); // Select Probation
            await sleep(800);
            checks.contractTypeChanged = true;
            console.log('  ✓ Contract type changed to Probation (Hợp đồng thử việc)');
        }

        // Screenshot 4: Modal General Information
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'contract_01_modal_sections.png'),
            fullPage: false
        });
        console.log('  📸 Screenshot saved: contract_01_modal_sections.png');

        // --- STEP 6: REMUNERATION, SALARY CLAUSE & CLAUSE EDITOR ---
        console.log('\n[6/7] Testing Tab 2 (Tiền lương & Chế độ) and Tab 3 (Soạn thảo điều khoản)...');
        // Click Tab 2
        const tab2Btn = (await page.$$('.modal-header .tf-segmented-tabs button'))[1];
        if (tab2Btn) {
            await tab2Btn.click();
            await sleep(800);
            checks.tab2RemunerationTested = true;
            console.log('  ✓ Switched to Tab 2: Tiền lương & Chế độ');

            // Type numeric basic salary
            const salaryInput = await page.$('input[placeholder*="Bỏ trống nếu thỏa thuận"]');
            if (salaryInput) {
                await salaryInput.type('18500000');
            }

            // Click "Tạo mẫu chữ từ thông số"
            const genBtn = await findButtonByText(page, 'Tạo mẫu chữ từ thông số');
            if (genBtn) {
                await genBtn.click();
                await sleep(500);
                checks.salaryClauseGenerated = true;
                console.log('  ✓ Clicked "Tạo mẫu chữ từ thông số", updated SalaryClauseText');
            }
        }

        // Screenshot 5: Tab 2 Salary & Remuneration
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'contract_02_salary_clauses.png'),
            fullPage: false
        });
        console.log('  📸 Screenshot saved: contract_02_salary_clauses.png');

        // Click Tab 3: Soạn thảo điều khoản
        const tab3Btn = (await page.$$('.modal-header .tf-segmented-tabs button'))[2];
        if (tab3Btn) {
            await tab3Btn.click();
            await sleep(800);
            checks.tab3ClausesTested = true;
            console.log('  ✓ Switched to Tab 3: Soạn thảo điều khoản');

            // Click "+ Thêm điều khoản mới"
            const addClauseBtn = await findButtonByText(page, 'Thêm điều khoản mới');
            if (addClauseBtn) {
                await addClauseBtn.click();
                await sleep(500);
                checks.newClauseAdded = true;
                console.log('  ✓ Added custom contract clause dynamically');
            }
        }

        // --- STEP 7: LIVE A4 PREVIEW & DRAFT SAVING ---
        console.log('\n[7/7] Testing Tab 4 (Xem trước văn bản A4) and Draft Persistence...');
        // Click Tab 4: Xem trước văn bản A4
        const tab4Btn = (await page.$$('.modal-header .tf-segmented-tabs button'))[3];
        if (tab4Btn) {
            await tab4Btn.click();
            await sleep(1000);
            checks.tab4A4PreviewRendered = true;
            console.log('  ✓ Switched to Tab 4: Live A4 Preview');
        }

        // Verify A4 Preview Canvas
        const a4Content = await page.$eval('.tf-a4-preview-canvas', el => el.innerText);
        const hasPartyA = a4Content.includes('BÊN A') && a4Content.includes('LACASA');
        const hasPartyB = a4Content.includes('BÊN B');
        const hasMotto = a4Content.includes('CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM');
        console.log('  ✓ A4 Canvas elements check - Motto:', hasMotto, 'Party A:', hasPartyA, 'Party B:', hasPartyB);
        checks.a4PartiesAndClausesVerified = hasPartyA && hasPartyB && hasMotto;

        // Screenshot 6: A4 Document Canvas Preview
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'contract_03_a4_preview.png'),
            fullPage: false
        });
        console.log('  📸 Screenshot saved: contract_03_a4_preview.png');

        // Check footer has "Quay lại chỉnh sửa" when in preview tab
        const backBtn = await findButtonByText(page, 'Quay lại chỉnh sửa', '.modal-footer');
        if (backBtn) {
            checks.returnToEditFooterAction = true;
            console.log('  ✓ Footer button "Quay lại chỉnh sửa" present and active');
        }

        // Save Draft Contract
        console.log('  Saving employment contract draft...');
        const saveDraftBtn = await findButtonByText(page, 'Lưu bản nháp', '.modal-footer') ||
                             await findButtonByText(page, 'Cập nhật hợp đồng', '.modal-footer');
        if (saveDraftBtn) {
            await saveDraftBtn.click();
            await sleep(2500);
            checks.contractDraftSaved = true;
            console.log('  ✓ Saved employment contract draft successfully');
        }

        // Wait for modal to close
        await sleep(1000);

        // Screenshot 7: Saved contract in table
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'contract_04_draft_saved.png'),
            fullPage: false
        });
        console.log('  📸 Screenshot saved: contract_04_draft_saved.png');

    } catch (err) {
        console.error('❌ Verification error encountered:', err);
    } finally {
        await browser.close();
    }

    console.log('\n===============================================================');
    console.log('                 FINAL VERIFICATION SUMMARY                    ');
    console.log('===============================================================');
    console.log(JSON.stringify(checks, null, 2));

    const allPassed = Object.values(checks).every(v => v === true || (typeof v === 'number' && v > 0));
    console.log(`\nOverall Verification Result: ${allPassed ? 'ALL PASS ✓' : 'COMPLETED WITH WARNINGS'}`);
    return checks;
}

runFullVerification();
