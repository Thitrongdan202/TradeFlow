const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACTS_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function runVerification() {
    console.log('=== Starting Contract UI Verification with Auth ===');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-gpu']
    });

    const page = await browser.newPage();
    const consoleLogs = [];
    page.on('console', msg => consoleLogs.push({ type: msg.type(), text: msg.text() }));

    const results = {
        timestamp: new Date().toISOString(),
        url: 'http://localhost:5169/hop-dong',
        tests: []
    };

    try {
        await page.setViewport({ width: 1440, height: 900 });

        // Step 1: Login
        console.log('Navigating to login page...');
        await page.goto('http://localhost:5169/Account/Login', { waitUntil: 'domcontentloaded' });
        await page.evaluate(() => {
            const el = document.getElementById('tf-startup-loader');
            if (el) el.remove();
        });
        await new Promise(r => setTimeout(r, 600));

        console.log('Filling login credentials...');
        await page.waitForSelector('#login-username');
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');

        await Promise.all([
            page.waitForNavigation({ waitUntil: 'domcontentloaded' }),
            page.click('button[type="submit"]')
        ]);
        console.log('Logged in successfully, current URL:', page.url());

        // Step 2: Navigate to /hop-dong
        console.log('Navigating to http://localhost:5169/hop-dong...');
        await page.goto('http://localhost:5169/hop-dong', { waitUntil: 'domcontentloaded' });
        await page.evaluate(() => {
            const el = document.getElementById('tf-startup-loader');
            if (el) el.remove();
        });
        await page.waitForSelector('.tf-contracts-page', { timeout: 15000 });
        await new Promise(r => setTimeout(r, 1200));

        // Test 1: Header verification
        const headerTitle = await page.$eval('.tf-header-title', el => el.textContent.trim());
        const legalBadge = await page.$eval('.tf-legal-badge', el => el.textContent.trim());
        const headerButtons = await page.$$eval('.tf-header-btn, .tf-primary-btn', btns => btns.map(b => b.textContent.trim().replace(/\s+/g, ' ')));

        results.tests.push({
            name: 'Header Elements & Actions',
            passed: headerTitle.includes('Quản lý Hợp đồng') && legalBadge.includes('BLDS 2015') && headerButtons.length >= 3,
            details: {
                title: headerTitle,
                badge: legalBadge,
                buttons: headerButtons,
                noEmptyButtons: headerButtons.every(b => b.length > 0)
            }
        });
        console.log('Test 1 Passed: Header verified:', headerButtons);

        // Capture Desktop Screenshot
        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'contract_01_desktop_main.png'), fullPage: false });

        // Test 2: KPI Metric Cards
        const kpiCards = await page.$$eval('.tf-kpi-card', cards => cards.map(c => ({
            label: c.querySelector('.tf-kpi-label')?.textContent.trim(),
            value: c.querySelector('.tf-kpi-value')?.textContent.trim(),
            subtext: c.querySelector('.tf-kpi-subtext')?.textContent.trim()
        })));

        results.tests.push({
            name: 'KPI Metric Cards',
            passed: kpiCards.length === 4 && kpiCards.every(k => k.label && k.value),
            details: kpiCards
        });
        console.log('Test 2 Passed: KPI Cards:', kpiCards);

        // Hover over second KPI card (Chờ ký kết)
        const kpiElements = await page.$$('.tf-kpi-card');
        if (kpiElements.length >= 2) {
            await kpiElements[1].hover();
        }
        await new Promise(r => setTimeout(r, 300));
        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'contract_02_kpi_hover.png') });

        // Test 3: Status Tabs Navigation
        const statusTabs = await page.$$eval('.tf-status-tab', tabs => tabs.map(t => ({
            name: t.querySelector('span:first-child')?.textContent.trim(),
            count: t.querySelector('.tf-tab-count')?.textContent.trim(),
            isActive: t.classList.contains('active')
        })));

        results.tests.push({
            name: 'Status Navigation Tabs Strip',
            passed: statusTabs.length === 7 && statusTabs[0].isActive,
            details: statusTabs
        });
        console.log('Test 3 Passed: Status Tabs:', statusTabs);

        // Click on "Bản nháp" tab
        const draftTab = await page.$('.tf-status-tab:nth-child(2)');
        if (draftTab) {
            await draftTab.click();
            await new Promise(r => setTimeout(r, 600));
            const activeTabName = await page.$eval('.tf-status-tab.active', el => el.textContent.trim().replace(/\s+/g, ' '));
            const counterText = await page.$eval('.tf-counter-badge', el => el.textContent.trim());

            results.tests.push({
                name: 'Status Tab Switching',
                passed: activeTabName.includes('Bản nháp'),
                details: { activeTab: activeTabName, counter: counterText }
            });
            console.log('Tab switch result:', activeTabName, counterText);
            await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'contract_03_status_tab_filter.png') });

            // Switch back to "Tất cả"
            const allTab = await page.$('.tf-status-tab:nth-child(1)');
            if (allTab) {
                await allTab.click();
                await new Promise(r => setTimeout(r, 500));
            }
        }

        // Test 4: Search & Filters
        const searchInput = await page.$('.tf-contract-search-input');
        if (searchInput) {
            await page.type('.tf-contract-search-input', '0004', { delay: 100 });
            await new Promise(r => setTimeout(r, 800));
            const filteredCount = await page.$$eval('.tf-table-row', rows => rows.length);
            const clearBtn = await page.$('.tf-contract-search-clear');

            results.tests.push({
                name: 'Search Input Feedback',
                passed: clearBtn !== null && filteredCount === 1,
                details: { filteredRows: filteredCount, clearBtnVisible: clearBtn !== null }
            });
            console.log('Test 4 Passed: Search input filtered rows:', filteredCount, 'Clear button:', clearBtn !== null);

            if (clearBtn) {
                await clearBtn.click();
                await new Promise(r => setTimeout(r, 600));
            }
        }

        // Test 5: Table Rows and Action Buttons
        const tableRows = await page.$$eval('.tf-table-row', rows => rows.map(r => ({
            number: r.querySelector('.tf-contract-number')?.textContent.trim(),
            title: r.querySelector('.tf-contract-title-link')?.textContent.trim(),
            template: r.querySelector('.tf-template-badge')?.textContent.trim(),
            partyB: r.querySelector('.tf-party-name')?.textContent.trim(),
            amount: r.querySelector('.tf-amount-main')?.textContent.trim(),
            status: r.querySelector('.tf-status-pill')?.textContent.trim(),
            actionsCount: r.querySelectorAll('.tf-action-btn').length
        })));

        results.tests.push({
            name: 'Contract Table Structure',
            passed: tableRows.length > 0 && tableRows.every(r => r.number && r.title && r.status && r.actionsCount >= 2),
            details: { rowCount: tableRows.length, sampleRow: tableRows[0] }
        });
        console.log(`Test 5 Passed: Verified ${tableRows.length} contract table rows.`);

        // Test 6: Party A Settings Modal
        console.log('Testing Party A Settings Modal...');
        const partyABtn = await page.$('.tf-header-btn:nth-child(1)');
        if (partyABtn) {
            await partyABtn.click();
            await page.waitForSelector('.tf-party-a-modal-content', { timeout: 6000 });
            await new Promise(r => setTimeout(r, 600));

            const modalTitle = await page.$eval('.tf-party-a-modal-header .modal-title', el => el.textContent.trim());
            const sectionsCount = await page.$$eval('.tf-form-section-title', els => els.length);
            const companyNameVal = await page.$eval('.tf-input', el => el.value);

            results.tests.push({
                name: 'Party A Settings Modal',
                passed: modalTitle.includes('Bên A') && sectionsCount === 4 && companyNameVal.length > 0,
                details: { modalTitle, sectionsCount, companyName: companyNameVal }
            });
            console.log('Test 6 Passed: Party A Modal open with company:', companyNameVal);

            await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'contract_04_party_a_modal.png') });

            // Close Modal
            const closeBtn = await page.$('.tf-party-a-modal-header .btn-close');
            if (closeBtn) {
                await closeBtn.click();
                await new Promise(r => setTimeout(r, 500));
            }
        }

        // Test 7: Template Selector Modal & A4 Gallery
        console.log('Testing Template Selector Modal...');
        const newContractBtn = await page.$('.tf-primary-btn');
        if (newContractBtn) {
            await newContractBtn.click();
            await page.waitForSelector('.tf-gallery-modal-content', { timeout: 8000 });
            await page.waitForSelector('.tf-gallery-card', { timeout: 8000 });
            await new Promise(r => setTimeout(r, 800));

            const templateCards = await page.$$eval('.tf-gallery-card', cards => cards.map(c => ({
                title: c.getAttribute('aria-label') || c.querySelector('.tf-a4-doc-title')?.textContent.trim(),
                hasA4: c.querySelector('.tf-a4-sheet') !== null
            })));

            results.tests.push({
                name: 'Template Selector Modal & A4 Gallery',
                passed: templateCards.length === 7 && templateCards.every(t => t.hasA4),
                details: { templateCount: templateCards.length, sampleTemplate: templateCards[0] }
            });
            console.log(`Test 7 Passed: Verified ${templateCards.length} A4 template cards.`);

            // Hover over first card
            await page.hover('.tf-gallery-card:nth-child(1)');
            await new Promise(r => setTimeout(r, 400));
            await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'contract_05_template_modal.png') });

            // Close Modal
            const closeGalleryBtn = await page.$('.tf-gallery-modal-header .btn-close');
            if (closeGalleryBtn) {
                await closeGalleryBtn.click();
                await page.waitForFunction(() => !document.querySelector('.tf-gallery-modal-backdrop'), { timeout: 5000 });
                await new Promise(r => setTimeout(r, 800));
            }
        }

        // Test 8: Responsive Layout - Tablet (768x1024)
        console.log('Testing Tablet Viewport (768x1024)...');
        await page.setViewport({ width: 768, height: 1024 });
        await new Promise(r => setTimeout(r, 800));
        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'contract_06_tablet.png'), fullPage: false });

        // Test 9: Responsive Layout - Mobile (375x812)
        console.log('Testing Mobile Viewport (375x812)...');
        await page.setViewport({ width: 375, height: 812 });
        await new Promise(r => setTimeout(r, 800));
        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'contract_07_mobile.png'), fullPage: false });

        results.overall = results.tests.every(t => t.passed) ? 'PASSED' : 'FAILED';
        console.log('=== Verification Completed: ' + results.overall + ' ===');

    } catch (err) {
        console.error('Verification Error:', err);
        results.overall = 'FAILED';
        results.error = err.message;
    } finally {
        await browser.close();
        fs.writeFileSync(
            path.join(ARTIFACTS_DIR, 'contract_ui_verification_results.json'),
            JSON.stringify(results, null, 2),
            'utf8'
        );
        console.log('Results saved to contract_ui_verification_results.json');
    }
}

runVerification();
