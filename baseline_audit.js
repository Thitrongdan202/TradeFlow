const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://127.0.0.1:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('=== Starting Baseline Performance & Dark Mode Inspection ===');
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
        // Step 1: Login
        console.log('[1/4] Logging in...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('#login-username', { timeout: 10000 });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        console.log('✓ Logged in.');

        // Step 2: Navigate to Employment Contracts
        console.log('[2/4] Navigating to /hop-dong?tab=employment...');
        await page.goto(`${BASE_URL}/hop-dong?tab=employment`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(1500);

        // Click "Tạo HĐ lao động mới" button
        console.log('Opening Employment Contract Editor modal...');
        const t0_open = Date.now();
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('button'));
            const btn = btns.find(b => b.textContent.includes('Tạo HĐ lao động mới'));
            if (btn) btn.click();
        });
        await page.waitForSelector('.modal.show, .tf-segmented-tabs', { timeout: 5000 });
        const openTime = Date.now() - t0_open;
        console.log(`✓ Contract modal opened in ${openTime}ms`);
        await sleep(800);

        // Capture Step 1 (Info & Party B) in Dark Mode
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'baseline_contract_step1_dark.png'),
            fullPage: false
        });
        console.log('✓ Captured baseline_contract_step1_dark.png');

        // Measure Step 1 -> Step 2
        console.log('Switching to Step 2 (Tiền lương & Chế độ)...');
        const t0_step2 = Date.now();
        await page.evaluate(() => {
            const tabs = Array.from(document.querySelectorAll('.tf-segmented-tab'));
            if (tabs.length > 1) tabs[1].click();
        });
        await sleep(500);
        const step2Time = Date.now() - t0_step2;
        console.log(`✓ Step 2 active in ${step2Time}ms`);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'baseline_contract_step2_dark.png'),
            fullPage: false
        });
        console.log('✓ Captured baseline_contract_step2_dark.png');

        // Measure Step 2 -> Step 3
        console.log('Switching to Step 3 (Soạn thảo điều khoản)...');
        const t0_step3 = Date.now();
        await page.evaluate(() => {
            const tabs = Array.from(document.querySelectorAll('.tf-segmented-tab'));
            if (tabs.length > 2) tabs[2].click();
        });
        await sleep(500);
        const step3Time = Date.now() - t0_step3;
        console.log(`✓ Step 3 active in ${step3Time}ms`);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'baseline_contract_step3_dark.png'),
            fullPage: false
        });
        console.log('✓ Captured baseline_contract_step3_dark.png');

        // Measure Step 3 -> Step 4
        console.log('Switching to Step 4 (Xem trước văn bản A4)...');
        const t0_step4 = Date.now();
        await page.evaluate(() => {
            const tabs = Array.from(document.querySelectorAll('.tf-segmented-tab'));
            if (tabs.length > 3) tabs[3].click();
        });
        await sleep(500);
        const step4Time = Date.now() - t0_step4;
        console.log(`✓ Step 4 active in ${step4Time}ms`);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'baseline_contract_step4_dark.png'),
            fullPage: false
        });
        console.log('✓ Captured baseline_contract_step4_dark.png');

        // Close modal
        await page.evaluate(() => {
            const closeBtn = document.querySelector('.tf-modal-close-btn');
            if (closeBtn) closeBtn.click();
        });
        await sleep(800);

        // Step 3: Inspect Employee Creation Modal
        console.log('[3/4] Navigating to /nhan-vien for Employee Modal baseline...');
        await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
        await setTheme('dark');
        await sleep(1200);

        const t0_emp = Date.now();
        await page.click('.tf-page-actions button.tf-btn-primary');
        await page.waitForSelector('.employee-modal, .tf-modal', { timeout: 5000 });
        const empOpenTime = Date.now() - t0_emp;
        console.log(`✓ Employee modal opened in ${empOpenTime}ms`);
        await sleep(600);

        // Test typing in employee modal
        console.log('Measuring typing response in employee modal...');
        const t0_type = Date.now();
        await page.type('input[placeholder*="Nguyễn Văn An"]', 'Nguyễn Thái Bình', { delay: 10 });
        const typeTime = Date.now() - t0_type;
        console.log(`✓ Typed name in ${typeTime}ms`);

        // Test mode switch
        const t0_mode = Date.now();
        await page.evaluate(() => {
            const radios = Array.from(document.querySelectorAll('input[type="radio"][name="empCreateMode"]'));
            if (radios.length > 1) radios[1].click();
        });
        await sleep(400);
        const modeTime = Date.now() - t0_mode;
        console.log(`✓ Switched mode to approval in ${modeTime}ms`);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'baseline_employee_modal_dark.png'),
            fullPage: false
        });
        console.log('✓ Captured baseline_employee_modal_dark.png');

        const metrics = {
            contractModalOpenTimeMs: openTime,
            contractStep2TimeMs: step2Time,
            contractStep3TimeMs: step3Time,
            contractStep4TimeMs: step4Time,
            employeeModalOpenTimeMs: empOpenTime,
            employeeTypingTimeMs: typeTime,
            employeeModeSwitchTimeMs: modeTime
        };

        fs.writeFileSync(path.join(ARTIFACT_DIR, 'baseline_metrics.json'), JSON.stringify(metrics, null, 2));
        console.log('Baseline metrics saved:', metrics);

    } catch (err) {
        console.error('Error during baseline inspection:', err);
    } finally {
        await browser.close();
    }
})();
