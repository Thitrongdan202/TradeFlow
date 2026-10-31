const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACTS_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log("=== STARTING TRADEFLOW POPUP & SCROLL PERFORMANCE BENCHMARK ===");
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage', '--window-size=1600,1000']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1600, height: 1000 });

    const consoleLogs = [];
    page.on('console', msg => consoleLogs.push(`[${msg.type()}] ${msg.text()}`));
    page.on('pageerror', err => consoleLogs.push(`[PAGE ERROR] ${err.message}`));

    // 1. Sign In
    console.log("1. Navigating to Login...");
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle0' });
    await page.waitForSelector('#login-username', { visible: true });
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle0' }),
        page.click('button[type="submit"]')
    ]);
    console.log("Logged in successfully. Current URL:", page.url());

    const results = {
        benchmarkGallery: {},
        employeeRequestModal: {},
        employeeFormModal: {},
        tabSwitching: {},
        scrollPerformance: {}
    };

    // 2. Measure Benchmark Reference: Contract Template Library Popup
    console.log("\n2. Measuring Benchmark: Contract Template Library (/hop-dong)...");
    await page.goto('http://127.0.0.1:5169/hop-dong', { waitUntil: 'networkidle0' });
    await new Promise(r => setTimeout(r, 1000));

    // Look for button that opens TemplateSelectorModal
    const openLibSelector = await page.evaluate(() => {
        const buttons = Array.from(document.querySelectorAll('button'));
        const btn = buttons.find(b => b.innerText.includes('Thư viện') || b.innerText.includes('Mẫu hợp đồng') || b.innerText.includes('Tạo hợp đồng'));
        if (btn) {
            btn.setAttribute('id', 'test-btn-open-template-gallery');
            return true;
        }
        return false;
    });

    if (openLibSelector) {
        // Cold Open
        const t0 = Date.now();
        await page.click('#test-btn-open-template-gallery');
        await page.waitForSelector('.tf-gallery-modal-dialog, .modal.show', { visible: true });
        const coldOpenTime = Date.now() - t0;
        console.log(`Benchmark Contract Template Gallery Cold Open: ${coldOpenTime} ms`);
        results.benchmarkGallery.coldOpenMs = coldOpenTime;

        // Close
        await page.click('.btn-close, .tf-gallery-modal-footer button');
        await new Promise(r => setTimeout(r, 300));

        // Warm Open
        const t1 = Date.now();
        await page.click('#test-btn-open-template-gallery');
        await page.waitForSelector('.tf-gallery-modal-dialog, .modal.show', { visible: true });
        const warmOpenTime = Date.now() - t1;
        console.log(`Benchmark Contract Template Gallery Warm Open: ${warmOpenTime} ms`);
        results.benchmarkGallery.warmOpenMs = warmOpenTime;

        // Close again
        await page.click('.btn-close, .tf-gallery-modal-footer button');
        await new Promise(r => setTimeout(r, 300));
    }

    // 3. Measure Employee Management Popups (/nhan-vien)
    console.log("\n3. Navigating to Employees page (/nhan-vien)...");
    await page.goto('http://127.0.0.1:5169/nhan-vien', { waitUntil: 'networkidle0' });
    await new Promise(r => setTimeout(r, 1000));

    // Identify Open Approval Modal button and Open Form Modal button
    await page.evaluate(() => {
        const buttons = Array.from(document.querySelectorAll('button'));
        const reviewBtn = buttons.find(b => b.innerText.includes('Xét duyệt') || b.innerText.includes('Yêu cầu'));
        if (reviewBtn) reviewBtn.setAttribute('id', 'test-btn-open-approval-modal');

        const addBtn = buttons.find(b => b.innerText.includes('Thêm nhân viên'));
        if (addBtn) addBtn.setAttribute('id', 'test-btn-open-add-employee-modal');
    });

    // 3A. Employee Approval Modal: Cold vs Warm Open
    console.log("3A. Testing Employee Approval Modal (EmployeeRequestModal)...");
    const tApprovalCold0 = Date.now();
    await page.click('#test-btn-open-approval-modal');
    await page.waitForSelector('.tf-req-card, .tf-req-body, .tf-modal-title', { visible: true });
    const approvalColdMs = Date.now() - tApprovalCold0;
    console.log(`Employee Approval Modal Cold Open: ${approvalColdMs} ms`);
    results.employeeRequestModal.coldOpenMs = approvalColdMs;

    // Test Tab Switching Latency
    console.log("Testing Tab Switching latency inside Employee Approval Modal...");
    const tabs = ['Chờ duyệt', 'Cần bổ sung hồ sơ', 'Đã duyệt', 'Từ chối', 'Tất cả'];
    const tabTimings = {};

    for (const tabName of tabs) {
        const switchMs = await page.evaluate(async (name) => {
            const btns = Array.from(document.querySelectorAll('.tf-req-filter-bar button.tf-tab-btn'));
            const targetBtn = btns.find(b => b.innerText.includes(name));
            if (!targetBtn) return -1;
            const start = performance.now();
            targetBtn.click();
            // Wait for next render frame
            await new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)));
            return Math.round(performance.now() - start);
        }, tabName);
        tabTimings[tabName] = switchMs;
        console.log(`  Tab [${tabName}] switch render latency: ${switchMs} ms`);
        await new Promise(r => setTimeout(r, 150));
    }
    results.tabSwitching = tabTimings;

    // Test Scrolling in Employee Approval Modal
    console.log("Testing Scroll Smoothness in Employee Approval Modal (20 scroll cycles)...");
    const approvalScrollStats = await page.evaluate(async () => {
        const body = document.querySelector('.tf-req-body');
        if (!body) return { error: 'tf-req-body not found' };

        const fpsSamples = [];
        let totalScrollDistance = 0;
        let droppedFrames = 0;

        for (let i = 0; i < 20; i++) {
            const targetScroll = i % 2 === 0 ? 500 : 0;
            const startScroll = body.scrollTop;
            const delta = targetScroll - startScroll;
            const steps = 15;
            
            for (let step = 1; step <= steps; step++) {
                const frameStart = performance.now();
                body.scrollTop = startScroll + (delta * (step / steps));
                totalScrollDistance += Math.abs(delta / steps);
                await new Promise(r => requestAnimationFrame(r));
                const frameDuration = performance.now() - frameStart;
                if (frameDuration > 16.7) droppedFrames++;
                fpsSamples.push(frameDuration);
            }
        }

        const avgFrameMs = fpsSamples.reduce((a, b) => a + b, 0) / fpsSamples.length;
        return {
            totalSamples: fpsSamples.length,
            avgFrameDurationMs: Math.round(avgFrameMs * 100) / 100,
            estimatedFps: Math.round(1000 / avgFrameMs),
            droppedFramesCount: droppedFrames,
            totalScrollPx: Math.round(totalScrollDistance)
        };
    });
    console.log("Employee Approval Modal Scroll Stats:", approvalScrollStats);
    results.scrollPerformance.approvalModal = approvalScrollStats;

    // Screenshot of Approval Modal
    await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'employee_approval_modal_optimized.png') });

    // Close Approval Modal
    await page.click('.tf-modal-header button.tf-modal-close-btn:last-child');
    await new Promise(r => setTimeout(r, 300));

    // Warm Open of Approval Modal
    const tApprovalWarm0 = Date.now();
    await page.click('#test-btn-open-approval-modal');
    await page.waitForSelector('.tf-req-card, .tf-req-body', { visible: true });
    const approvalWarmMs = Date.now() - tApprovalWarm0;
    console.log(`Employee Approval Modal Warm Open: ${approvalWarmMs} ms`);
    results.employeeRequestModal.warmOpenMs = approvalWarmMs;

    // Close again
    await page.click('.tf-modal-header button.tf-modal-close-btn:last-child');
    await new Promise(r => setTimeout(r, 300));

    // 3B. Employee Form Modal: Cold vs Warm Open & Scroll
    console.log("\n3B. Testing Employee Creation Modal (EmployeeFormModal)...");
    await page.evaluate(() => {
        const buttons = Array.from(document.querySelectorAll('button'));
        const addBtn = buttons.find(b => (b.innerText.includes('Nhân viên') || b.innerText.includes('Thêm')) && b.classList.contains('tf-btn-primary'));
        if (addBtn) addBtn.setAttribute('id', 'test-btn-open-add-employee-modal');
    });

    const tFormCold0 = Date.now();
    await page.click('#test-btn-open-add-employee-modal');
    await page.waitForSelector('.employee-modal-body, input[placeholder="NV00001"]', { visible: true });
    const formColdMs = Date.now() - tFormCold0;
    console.log(`Employee Form Modal Cold Open: ${formColdMs} ms`);
    results.employeeFormModal.coldOpenMs = formColdMs;

    // Test Scrolling in Employee Form Modal
    console.log("Testing Scroll Smoothness in Employee Form Modal (20 scroll cycles)...");
    const formScrollStats = await page.evaluate(async () => {
        const body = document.querySelector('.employee-modal-body');
        if (!body) return { error: 'employee-modal-body not found' };

        const fpsSamples = [];
        let totalScrollDistance = 0;
        let droppedFrames = 0;

        for (let i = 0; i < 20; i++) {
            const targetScroll = i % 2 === 0 ? 600 : 0;
            const startScroll = body.scrollTop;
            const delta = targetScroll - startScroll;
            const steps = 15;
            
            for (let step = 1; step <= steps; step++) {
                const frameStart = performance.now();
                body.scrollTop = startScroll + (delta * (step / steps));
                totalScrollDistance += Math.abs(delta / steps);
                await new Promise(r => requestAnimationFrame(r));
                const frameDuration = performance.now() - frameStart;
                if (frameDuration > 16.7) droppedFrames++;
                fpsSamples.push(frameDuration);
            }
        }

        const avgFrameMs = fpsSamples.reduce((a, b) => a + b, 0) / fpsSamples.length;
        return {
            totalSamples: fpsSamples.length,
            avgFrameDurationMs: Math.round(avgFrameMs * 100) / 100,
            estimatedFps: Math.round(1000 / avgFrameMs),
            droppedFramesCount: droppedFrames,
            totalScrollPx: Math.round(totalScrollDistance)
        };
    });
    console.log("Employee Form Modal Scroll Stats:", formScrollStats);
    results.scrollPerformance.formModal = formScrollStats;

    // Screenshot of Employee Form Modal
    await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'employee_form_modal_optimized.png') });

    // Close Form Modal
    await page.click('.employee-modal-header button.tf-modal-close-btn');
    await new Promise(r => setTimeout(r, 300));

    // Warm Open Form Modal
    const tFormWarm0 = Date.now();
    await page.click('#test-btn-open-add-employee-modal');
    await page.waitForSelector('.employee-modal-body, input[placeholder="NV00001"]', { visible: true });
    const formWarmMs = Date.now() - tFormWarm0;
    console.log(`Employee Form Modal Warm Open: ${formWarmMs} ms`);
    results.employeeFormModal.warmOpenMs = formWarmMs;

    // Close Form Modal
    await page.click('.employee-modal-header button.tf-modal-close-btn');
    await new Promise(r => setTimeout(r, 300));

    // 4. Save metrics JSON
    fs.writeFileSync(path.join(ARTIFACTS_DIR, 'popup_scroll_performance_results.json'), JSON.stringify(results, null, 2), 'utf8');
    console.log("\nBenchmark complete! Results saved to popup_scroll_performance_results.json");

    await browser.close();
})();

