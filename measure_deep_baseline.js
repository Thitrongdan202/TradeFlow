const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACTS_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://127.0.0.1:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('=== STARTING DEEP PERFORMANCE BASELINE MEASUREMENTS ===');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    const baseline = {
        timestamp: new Date().toISOString(),
        hostingModel: "Blazor InteractiveServer (.NET 10 / ASP.NET Core Razor Components)",
        measurements: {},
        findings: []
    };

    try {
        // 1. LOGIN
        console.log('1. Logging in as admin...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        console.log('Logged in successfully.');
        await sleep(1000);

        // 2. EMPLOYEE CREATION POPUP BENCHMARK
        console.log('\n--- 2. Employee Creation Benchmark ---');
        await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('.tf-page-actions', { timeout: 15000 });
        await sleep(800);

        // 2.1 Click-to-visible & Click-to-interactive
        const t0_click = Date.now();
        await page.click('.tf-page-actions button.tf-btn-primary');
        await page.waitForSelector('.employee-modal', { visible: true });
        const t0_visible = Date.now() - t0_click;

        // Interactive check: wait until first input is enabled and focusable
        await page.waitForFunction(() => {
            const input = document.querySelector('input[placeholder*="Nguyễn Văn An"]');
            return input && !input.disabled;
        });
        const t0_interactive = Date.now() - t0_click;
        console.log(`- EmployeeFormModal: Click-to-Visible = ${t0_visible}ms, Click-to-Interactive = ${t0_interactive}ms`);
        baseline.measurements.employeeForm_clickToVisible = t0_visible;
        baseline.measurements.employeeForm_clickToInteractive = t0_interactive;

        // 2.2 Typing responsiveness (measure typing round-trip time)
        const fullNameSelector = 'input[placeholder*="Nguyễn Văn An"]';
        const typeStart = Date.now();
        const testName = "Nguyễn Hoàng Nam " + Date.now().toString().slice(-4);
        for (let char of testName) {
            await page.type(fullNameSelector, char, { delay: 15 });
        }
        const typeDuration = Date.now() - typeStart;
        const avgKeyLatency = typeDuration / testName.length;
        console.log(`- Typing "${testName}": total ${typeDuration}ms, ~${avgKeyLatency.toFixed(1)}ms/key`);
        baseline.measurements.employeeForm_typingAvgKeyMs = parseFloat(avgKeyLatency.toFixed(1));

        // 2.3 Dropdown response (Department change)
        const deptStart = Date.now();
        await page.evaluate(() => {
            const selects = Array.from(document.querySelectorAll('select.tf-input'));
            if (selects.length > 0 && selects[0].options.length > 1) {
                selects[0].selectedIndex = 1;
                selects[0].dispatchEvent(new Event('change', { bubbles: true }));
            }
        });
        await sleep(100);
        const deptDuration = Date.now() - deptStart;
        console.log(`- Department Dropdown change latency: ${deptDuration}ms`);
        baseline.measurements.employeeForm_deptChangeMs = deptDuration;

        // 2.4 Date Input response
        const dateStart = Date.now();
        const dateInput = await page.$('.tf-date-field');
        if (dateInput) {
            await dateInput.click();
            await page.type('.tf-date-field', '15/05/1990', { delay: 10 });
        }
        const dateDuration = Date.now() - dateStart;
        console.log(`- Date Input typing latency: ${dateDuration}ms`);
        baseline.measurements.employeeForm_dateInputMs = dateDuration;

        // 2.5 Scroll FPS & frame stability
        const scrollJank = await page.evaluate(async () => {
            const body = document.querySelector('.employee-modal-body');
            if (!body) return { frames: 0, maxDelta: 0 };
            let frames = 0;
            let lastTime = performance.now();
            let maxDelta = 0;

            for (let i = 0; i < 20; i++) {
                body.scrollTop += 30;
                await new Promise(resolve => requestAnimationFrame(resolve));
                const now = performance.now();
                const delta = now - lastTime;
                if (delta > maxDelta) maxDelta = delta;
                lastTime = now;
                frames++;
            }
            return { frames, maxDelta: parseFloat(maxDelta.toFixed(1)) };
        });
        console.log(`- Scroll Frame Stability: 20 frames, max frame delta = ${scrollJank.maxDelta}ms`);
        baseline.measurements.employeeForm_scrollMaxFrameDelta = scrollJank.maxDelta;

        // 2.6 Switch mode to "Gửi yêu cầu phê duyệt" to test attachment section
        await page.evaluate(() => {
            const labels = Array.from(document.querySelectorAll('.tf-modal-subheader label'));
            const reqLabel = labels.find(l => l.textContent.includes('Gửi yêu cầu'));
            if (reqLabel) {
                reqLabel.click();
                const r = reqLabel.querySelector('input[type="radio"]');
                if (r) {
                    r.checked = true;
                    r.dispatchEvent(new Event('change', { bubbles: true }));
                }
            }
        });
        await sleep(300);

        // Upload / attach demo documents
        const attachStart = Date.now();
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.employee-modal button'));
            const demoBtn = btns.find(b => b.textContent.includes('hồ sơ mẫu'));
            if (demoBtn) demoBtn.click();
        });
        await page.waitForSelector('.tf-dossier-item', { timeout: 5000 });
        const attachDuration = Date.now() - attachStart;
        console.log(`- Demo Dossier Attach latency: ${attachDuration}ms`);
        baseline.measurements.employeeForm_dossierAttachMs = attachDuration;

        // Close modal
        await page.click('.tf-modal-close-btn');
        await sleep(300);
        await page.evaluate(() => {
            const discardBtn = Array.from(document.querySelectorAll('button')).find(b => b.textContent.includes('Hủy thay đổi'));
            if (discardBtn) discardBtn.click();
        });
        await page.waitForFunction(() => !document.querySelector('.employee-modal'), { timeout: 5000 });
        await sleep(400);

        // 3. EMPLOYEE APPROVAL POPUP BENCHMARK
        console.log('\n--- 3. Employee Approval Benchmark ---');
        const apprStart = Date.now();
        await page.click('.tf-page-actions button.tf-btn-outline');
        await page.waitForSelector('.tf-req-filter-bar', { visible: true });
        const apprVisible = Date.now() - apprStart;
        console.log(`- EmployeeRequestModal: Initial display latency = ${apprVisible}ms`);
        baseline.measurements.approvalModal_initialDisplayMs = apprVisible;

        // 3.1 Tab switching responsiveness
        const tabNames = ['Cần bổ sung hồ sơ', 'Đã duyệt', 'Từ chối', 'Tất cả', 'Chờ duyệt'];
        const tabTimings = {};
        for (let tabName of tabNames) {
            const tStart = Date.now();
            await page.evaluate((name) => {
                const tabs = Array.from(document.querySelectorAll('.tf-tab-btn'));
                const target = tabs.find(t => t.textContent.includes(name));
                if (target) target.click();
            }, tabName);
            await sleep(150);
            const tDuration = Date.now() - tStart;
            tabTimings[tabName] = tDuration;
            console.log(`  * Tab "${tabName}" switch latency: ${tDuration}ms`);
        }
        baseline.measurements.approvalModal_tabSwitching = tabTimings;

        // 3.2 Decision dialogs responsiveness (Start review / Request supplement)
        const decisionStart = Date.now();
        await page.evaluate(() => {
            const card = document.querySelector('.tf-req-card');
            if (card) {
                const suppBtn = Array.from(card.querySelectorAll('button')).find(b => b.textContent.includes('Yêu cầu bổ sung') || b.textContent.includes('Từ chối'));
                if (suppBtn) suppBtn.click();
            }
        });
        await page.waitForSelector('.tf-req-actions input', { timeout: 5000 });
        const decisionOpenMs = Date.now() - decisionStart;
        console.log(`- Decision Input box open latency: ${decisionOpenMs}ms`);
        baseline.measurements.approvalModal_decisionInputOpenMs = decisionOpenMs;

        // Close request modal
        await page.click('.tf-modal-close-btn');
        await page.waitForFunction(() => !document.querySelector('.tf-req-filter-bar'), { timeout: 5000 });
        await sleep(500);

        // 4. EMPLOYMENT CONTRACT EDITOR BENCHMARK
        console.log('\n--- 4. Employment Contract Editor Benchmark ---');
        await page.goto(`${BASE_URL}/hop-dong?tab=employment`, { waitUntil: 'networkidle2' });
        await sleep(1000);

        const contractBtn = await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('button'));
            const b = btns.find(el => el.textContent.includes('Tạo HĐ lao động mới'));
            if (b) { b.click(); return true; }
            return false;
        });

        if (contractBtn) {
            const contractOpenStart = Date.now();
            await page.waitForSelector('.tf-contract-modal-header', { visible: true, timeout: 8000 });
            const contractOpenMs = Date.now() - contractOpenStart;
            console.log(`- Employment Contract Editor Modal opened in ${contractOpenMs}ms`);
            baseline.measurements.contractEditor_openMs = contractOpenMs;

            // Switch steps (Tabs: info -> remuneration -> clauses -> preview)
            const stepTimings = {};
            const steps = [
                { id: 'remuneration', name: 'Tiền lương & Chế độ' },
                { id: 'clauses', name: 'Soạn thảo điều khoản' },
                { id: 'preview', name: 'Xem trước A4' },
                { id: 'info', name: 'Thông tin chung' }
            ];

            for (let step of steps) {
                const stepStart = Date.now();
                await page.evaluate((stepId) => {
                    const tabs = Array.from(document.querySelectorAll('.tf-segmented-tab'));
                    const target = tabs.find(t => t.textContent.includes(stepId)) ||
                                   tabs.find(t => t.getAttribute('onclick') && t.getAttribute('onclick').includes(stepId));
                    if (target) target.click();
                }, step.name.slice(0, 10));
                await sleep(200);
                const stepDuration = Date.now() - stepStart;
                stepTimings[step.name] = stepDuration;
                console.log(`  * Step switch to "${step.name}": ${stepDuration}ms`);
            }
            baseline.measurements.contractEditor_stepSwitching = stepTimings;

            // Clause textarea typing responsiveness
            await page.evaluate(() => {
                const tabs = Array.from(document.querySelectorAll('.tf-segmented-tab'));
                const clauseTab = tabs.find(t => t.textContent.includes('điều khoản'));
                if (clauseTab) clauseTab.click();
            });
            await sleep(300);

            const clauseArea = await page.$('textarea.tf-terms-editor');
            if (clauseArea) {
                const clauseTypeStart = Date.now();
                await page.type('textarea.tf-terms-editor', ' Điều khoản bổ sung kiểm thử', { delay: 15 });
                const clauseTypeDuration = Date.now() - clauseTypeStart;
                console.log(`- Clause textarea typing latency: ${clauseTypeDuration}ms`);
                baseline.measurements.contractEditor_clauseTypingMs = clauseTypeDuration;
            }

            // Close contract modal
            await page.click('.tf-modal-close-btn');
            await sleep(400);
        } else {
            console.log('Contract create button not found on /hop-dong');
        }

        // 5. OTHER MODULE POPUPS (Customer create, Product create)
        console.log('\n--- 5. Other Module Popups Benchmark ---');

        // Customer Create Popup
        await page.goto(`${BASE_URL}/doi-tac/khach-hang`, { waitUntil: 'networkidle2' });
        await sleep(800);
        const tCustStart = Date.now();
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('button, a'));
            const b = btns.find(el => el.textContent.includes('Thêm khách hàng'));
            if (b) b.click();
        });
        await page.waitForSelector('.tf-modal, .modal-dialog, div[role="dialog"]', { visible: true, timeout: 5000 });
        const custOpenMs = Date.now() - tCustStart;
        console.log(`- Customer Create Modal opened in ${custOpenMs}ms`);
        baseline.measurements.customerModal_openMs = custOpenMs;

        // Save baseline JSON report
        fs.writeFileSync(
            path.join(ARTIFACTS_DIR, 'deep_performance_baseline.json'),
            JSON.stringify(baseline, null, 2),
            'utf8'
        );
        console.log('\n=== BASELINE BENCHMARK COMPLETED & SAVED ===');

    } catch (err) {
        console.error('Error during baseline measurement:', err);
    } finally {
        await browser.close();
    }
})();
