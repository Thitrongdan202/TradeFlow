const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('=== Starting Deep Performance & Dark Mode Verification ===');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    const results = {
        timestamp: new Date().toISOString(),
        contractModal: {},
        employeeModal: {},
        metrics: {}
    };

    // 1. Login
    console.log('Logging in...');
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle2' }), page.click('button[type="submit"]')]);
    console.log('Login successful.');

    // 2. Navigate to Employment Contracts
    console.log('Navigating to /hop-dong?tab=employment...');
    await page.goto('http://127.0.0.1:5169/hop-dong?tab=employment', { waitUntil: 'networkidle2' });

    // Enable Dark Mode
    await page.evaluate(() => {
        document.documentElement.setAttribute('data-bs-theme', 'dark');
        document.documentElement.setAttribute('data-theme', 'dark');
        localStorage.setItem('tf-theme', 'dark');
    });
    await new Promise(r => setTimeout(r, 600));

    // Open Contract Modal
    console.log('Opening Employment Contract Modal...');
    let t0 = Date.now();
    await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        const btn = btns.find(b => b.textContent.includes('Tạo HĐ lao động mới'));
        if (btn) btn.click();
    });
    await page.waitForSelector('.modal.show, .tf-contract-modal-header', { visible: true });
    let t1 = Date.now();
    results.contractModal.openTimeMs = t1 - t0;
    console.log(`Contract Modal opened in ${t1 - t0}ms`);
    await new Promise(r => setTimeout(r, 500));

    // Inspect Dark Mode Colors on Step 1
    const step1Styles = await page.evaluate(() => {
        const modalHeader = document.querySelector('.modal-header');
        const modalBody = document.querySelector('.modal-body');
        const modalTitle = document.querySelector('.modal-title');
        const formSection = document.querySelector('.tf-form-section');
        const label = document.querySelector('.tf-form-section label');
        const input = document.querySelector('.tf-form-section input.tf-input, .tf-form-section input.tf-custom-input');

        return {
            headerBg: modalHeader ? window.getComputedStyle(modalHeader).backgroundColor : null,
            bodyBg: modalBody ? window.getComputedStyle(modalBody).backgroundColor : null,
            titleColor: modalTitle ? window.getComputedStyle(modalTitle).color : null,
            cardBg: formSection ? window.getComputedStyle(formSection).backgroundColor : null,
            cardBorder: formSection ? window.getComputedStyle(formSection).borderColor : null,
            labelColor: label ? window.getComputedStyle(label).color : null,
            inputBg: input ? window.getComputedStyle(input).backgroundColor : null,
            inputColor: input ? window.getComputedStyle(input).color : null
        };
    });
    console.log('Step 1 Dark Styles:', step1Styles);
    results.contractModal.step1Styles = step1Styles;

    // Capture Step 1 Dark
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'verify_contract_step1_dark.png') });
    console.log('✓ Captured verify_contract_step1_dark.png');

    // Benchmark Tab Switching Times
    console.log('Benchmarking tab switching speeds...');
    const tabTimes = [];

    // Switch to Tab 2
    t0 = Date.now();
    await page.evaluate(() => {
        const tabs = document.querySelectorAll('.modal .tf-segmented-tab');
        if (tabs[1]) tabs[1].click();
    });
    await page.waitForFunction(() => {
        const el = document.getElementById('emp-tab-remuneration');
        return el && window.getComputedStyle(el).display === 'block';
    });
    t1 = Date.now();
    tabTimes.push({ from: 'Step 1', to: 'Step 2', timeMs: t1 - t0 });
    console.log(`Step 1 -> Step 2: ${t1 - t0}ms`);
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'verify_contract_step2_dark.png') });
    console.log('✓ Captured verify_contract_step2_dark.png');

    // Switch to Tab 3
    t0 = Date.now();
    await page.evaluate(() => {
        const tabs = document.querySelectorAll('.modal .tf-segmented-tab');
        if (tabs[2]) tabs[2].click();
    });
    await page.waitForFunction(() => {
        const el = document.getElementById('emp-tab-clauses');
        return el && window.getComputedStyle(el).display === 'block';
    });
    t1 = Date.now();
    tabTimes.push({ from: 'Step 2', to: 'Step 3', timeMs: t1 - t0 });
    console.log(`Step 2 -> Step 3: ${t1 - t0}ms`);

    // Verify clause editing in Step 3
    console.log('Testing clause editing in Step 3...');
    const clauseCardsCount = await page.evaluate(() => {
        return document.querySelectorAll('#emp-tab-clauses .tf-form-section').length;
    });
    console.log(`Found ${clauseCardsCount} clause sections in Step 3.`);

    // Type into first clause title
    await page.type('#emp-tab-clauses input[type="text"]:nth-of-type(2)', ' [ĐÃ KIỂM TRA]');
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'verify_contract_step3_dark.png') });
    console.log('✓ Captured verify_contract_step3_dark.png');

    // Switch to Tab 4
    t0 = Date.now();
    await page.evaluate(() => {
        const tabs = document.querySelectorAll('.modal .tf-segmented-tab');
        if (tabs[3]) tabs[3].click();
    });
    await page.waitForFunction(() => {
        const el = document.getElementById('emp-tab-preview');
        return el && window.getComputedStyle(el).display === 'block';
    });
    t1 = Date.now();
    tabTimes.push({ from: 'Step 3', to: 'Step 4', timeMs: t1 - t0 });
    console.log(`Step 3 -> Step 4: ${t1 - t0}ms`);

    // Inspect Step 4 Preview Canvas Styles (Must be pure white paper)
    const previewStyles = await page.evaluate(() => {
        const canvas = document.querySelector('.tf-a4-preview-canvas');
        if (!canvas) return null;
        return {
            canvasBg: window.getComputedStyle(canvas).backgroundColor,
            canvasColor: window.getComputedStyle(canvas).color,
            canvasBorder: window.getComputedStyle(canvas).borderColor
        };
    });
    console.log('Step 4 Preview Canvas Styles:', previewStyles);
    results.contractModal.previewStyles = previewStyles;
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'verify_contract_step4_dark.png') });
    console.log('✓ Captured verify_contract_step4_dark.png');

    // Switch back to Tab 1
    t0 = Date.now();
    await page.evaluate(() => {
        const tabs = document.querySelectorAll('.modal .tf-segmented-tab');
        if (tabs[0]) tabs[0].click();
    });
    await page.waitForFunction(() => {
        const el = document.getElementById('emp-tab-info');
        return el && window.getComputedStyle(el).display === 'block';
    });
    t1 = Date.now();
    tabTimes.push({ from: 'Step 4', to: 'Step 1', timeMs: t1 - t0 });
    console.log(`Step 4 -> Step 1: ${t1 - t0}ms`);

    const avgSwitchTime = tabTimes.reduce((acc, c) => acc + c.timeMs, 0) / tabTimes.length;
    results.metrics.postFixTabSwitchAvgMs = avgSwitchTime;
    results.metrics.tabTimes = tabTimes;
    console.log(`Average Tab Switch Time: ${avgSwitchTime.toFixed(1)}ms`);

    // Responsive Viewports
    console.log('Checking responsive viewports for Contract Editor...');
    await page.setViewport({ width: 1366, height: 768 });
    await new Promise(r => setTimeout(r, 400));
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'verify_contract_1366x768.png') });

    await page.setViewport({ width: 1920, height: 1080 });
    await new Promise(r => setTimeout(r, 400));
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'verify_contract_1920x1080.png') });

    // Reset viewport
    await page.setViewport({ width: 1440, height: 900 });

    // Check Light Mode for Contract Editor
    console.log('Checking Light Mode consistency for Contract Editor...');
    await page.evaluate(() => {
        document.documentElement.setAttribute('data-bs-theme', 'light');
        document.documentElement.setAttribute('data-theme', 'light');
    });
    await new Promise(r => setTimeout(r, 500));
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'verify_contract_step1_light.png') });

    // Click Tab 4 in Light Mode
    await page.evaluate(() => {
        const tabs = document.querySelectorAll('.modal .tf-segmented-tab');
        if (tabs[3]) tabs[3].click();
    });
    await new Promise(r => setTimeout(r, 400));
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'verify_contract_step4_light.png') });
    console.log('✓ Captured verify_contract_step1_light.png and verify_contract_step4_light.png');

    // Close Contract Modal
    await page.evaluate(() => {
        const closeBtn = document.querySelector('.tf-modal-close-btn');
        if (closeBtn) closeBtn.click();
    });
    await new Promise(r => setTimeout(r, 500));
    // If discard dialog appears, confirm discard
    await page.evaluate(() => {
        const discardBtn = Array.from(document.querySelectorAll('button')).find(b => b.textContent.includes('Hủy thay đổi'));
        if (discardBtn) discardBtn.click();
    });
    await new Promise(r => setTimeout(r, 500));

    // 3. Employee Modal Verification (/nhan-vien)
    console.log('Navigating to /nhan-vien for Employee Modal verification...');
    await page.goto('http://127.0.0.1:5169/nhan-vien', { waitUntil: 'networkidle2' });

    // Set back to Dark Mode
    await page.evaluate(() => {
        document.documentElement.setAttribute('data-bs-theme', 'dark');
        document.documentElement.setAttribute('data-theme', 'dark');
    });
    await new Promise(r => setTimeout(r, 800));

    // Open Employee Modal
    console.log('Opening Employee Creation Modal via .tf-page-actions button.tf-btn-primary...');
    t0 = Date.now();
    await page.click('.tf-page-actions button.tf-btn-primary');
    await page.waitForSelector('.employee-modal, .tf-modal', { timeout: 8000 });
    t1 = Date.now();
    console.log(`✓ Employee modal opened in ${t1 - t0}ms`);
    results.employeeModal.openTimeMs = t1 - t0;
    await new Promise(r => setTimeout(r, 600));

    // Inspect Employee Modal Dark Mode
    const empModalStyles = await page.evaluate(() => {
        const modalHeader = document.querySelector('.employee-modal-header, .tf-modal-header');
        const modalBody = document.querySelector('.tf-modal-body, .employee-modal');
        const input = document.querySelector('.employee-modal input.tf-input, .employee-modal input.tf-custom-input, .employee-modal input.form-control');
        return {
            headerBg: modalHeader ? window.getComputedStyle(modalHeader).backgroundColor : null,
            bodyBg: modalBody ? window.getComputedStyle(modalBody).backgroundColor : null,
            inputBg: input ? window.getComputedStyle(input).backgroundColor : null,
            inputColor: input ? window.getComputedStyle(input).color : null
        };
    });
    console.log('Employee Modal Styles:', empModalStyles);
    results.employeeModal.styles = empModalStyles;

    // Test typing and field responsiveness in Employee Modal
    console.log('Measuring typing response in employee modal...');
    t0 = Date.now();
    await page.type('input[placeholder*="Nguyễn Văn An"]', 'Nguyễn Thái Bình', { delay: 10 });
    t1 = Date.now();
    console.log(`✓ Typed name in ${t1 - t0}ms`);
    results.metrics.employeeInputTypingMs = t1 - t0;

    // Test mode switch
    t0 = Date.now();
    await page.evaluate(() => {
        const radios = Array.from(document.querySelectorAll('input[type="radio"][name="empCreateMode"]'));
        if (radios.length > 1) radios[1].click();
    });
    await new Promise(r => setTimeout(r, 300));
    t1 = Date.now();
    console.log(`✓ Switched mode to approval in ${t1 - t0}ms`);
    results.metrics.employeeModeSwitchMs = t1 - t0;

    // Capture Employee Modal Dark
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'verify_employee_modal_dark.png') });
    console.log('✓ Captured verify_employee_modal_dark.png');

    // Test Light Mode for Employee Modal
    await page.evaluate(() => {
        document.documentElement.setAttribute('data-bs-theme', 'light');
        document.documentElement.setAttribute('data-theme', 'light');
    });
    await new Promise(r => setTimeout(r, 500));
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'verify_employee_modal_light.png') });
    console.log('✓ Captured verify_employee_modal_light.png');

    // Write results JSON
    fs.writeFileSync(path.join(ARTIFACT_DIR, 'verification_metrics.json'), JSON.stringify(results, null, 2));
    console.log('✓ Written verification_metrics.json');

    await browser.close();
    console.log('=== Deep Performance & Dark Mode Verification Complete! ===');
})();
