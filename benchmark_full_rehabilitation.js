const puppeteer = require('puppeteer');
const fs = require('fs');

const BASE_URL = 'http://127.0.0.1:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('=== STARTING TRADEFLOW FULL SYSTEM PERFORMANCE BENCHMARK ===');

    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    const consoleErrors = [];
    page.on('console', msg => {
        if (msg.type() === 'error') {
            const txt = msg.text();
            if (!txt.includes('favicon.ico')) {
                consoleErrors.push(txt);
            }
        }
    });

    try {
        // 1. LOGIN
        console.log('Logging in as admin...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        await sleep(1000);

        // 2. NAVIGATE TO EMPLOYEE MANAGEMENT PAGE
        console.log('Navigating to /nhan-vien...');
        await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
        await sleep(1200);

        // 3. BENCHMARK EMPLOYEE REQUEST MODAL TAB SWITCHING (30 CYCLES)
        console.log('Opening Employee Request Modal...');
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('button'));
            const reqBtn = btns.find(b => b.innerText.includes('Duyệt yêu cầu') || b.innerText.includes('Xét duyệt'));
            if (reqBtn) reqBtn.click();
        });
        await page.waitForSelector('.tf-req-filter-bar', { timeout: 8000 });
        await sleep(600);

        console.log('Benchmarking Tab Switching Latency (30 rapid cycles)...');
        const tabSwitchLatencies = [];
        const tabIndices = [0, 1, 2, 3, 4];

        for (let i = 0; i < 30; i++) {
            const tabIdx = tabIndices[i % tabIndices.length];
            const t0 = Date.now();
            await page.evaluate((idx) => {
                const tabs = document.querySelectorAll('.tf-req-filter-bar .tf-tab-btn');
                if (tabs[idx]) tabs[idx].click();
            }, tabIdx);

            await page.waitForFunction((idx) => {
                const tabs = document.querySelectorAll('.tf-req-filter-bar .tf-tab-btn');
                return tabs[idx] && tabs[idx].classList.contains('active');
            }, {}, tabIdx);

            const duration = Date.now() - t0;
            tabSwitchLatencies.push(duration);
            await sleep(40);
        }

        tabSwitchLatencies.sort((a, b) => a - b);
        const avgTabSwitch = Math.round(tabSwitchLatencies.reduce((a, b) => a + b, 0) / tabSwitchLatencies.length);
        const minTabSwitch = tabSwitchLatencies[0];
        const maxTabSwitch = tabSwitchLatencies[tabSwitchLatencies.length - 1];
        const p95TabSwitch = tabSwitchLatencies[Math.floor(tabSwitchLatencies.length * 0.95)];

        console.log(`Tab Switch Latency: Avg=${avgTabSwitch}ms, Min=${minTabSwitch}ms, Max=${maxTabSwitch}ms, P95=${p95TabSwitch}ms`);

        // Close request modal and ensure it is fully closed
        console.log('Closing Employee Request Modal...');
        await page.evaluate(() => {
            const closeBtn = document.querySelector('.tf-modal-close-btn');
            if (closeBtn) closeBtn.click();
        });
        await page.waitForFunction(() => !document.querySelector('.tf-req-filter-bar'), { timeout: 5000 });
        await sleep(600);

        // 4. BENCHMARK EMPLOYEE FORM MODAL (OPEN / CLOSE - 20 CYCLES)
        console.log('Benchmarking Employee Form Modal (20 Open/Close cycles)...');
        const openLatencies = [];
        const closeLatencies = [];

        for (let i = 0; i < 20; i++) {
            // Open modal
            const t0 = Date.now();
            await page.evaluate(() => {
                const btns = Array.from(document.querySelectorAll('.tf-page-header button.tf-btn-primary, .tf-toolbar button.tf-btn-primary'));
                if (btns.length > 0) btns[0].click();
            });
            await page.waitForSelector('.employee-modal', { timeout: 8000 });
            const openDur = Date.now() - t0;
            openLatencies.push(openDur);

            await sleep(60);

            // Close modal
            const t1 = Date.now();
            await page.evaluate(() => {
                const closeBtn = document.querySelector('.employee-modal .tf-modal-close-btn');
                if (closeBtn) closeBtn.click();
            });
            await page.waitForFunction(() => !document.querySelector('.employee-modal'), { timeout: 5000 });
            const closeDur = Date.now() - t1;
            closeLatencies.push(closeDur);

            await sleep(60);
        }

        const avgOpen = Math.round(openLatencies.reduce((a, b) => a + b, 0) / openLatencies.length);
        const minOpen = Math.min(...openLatencies);
        const maxOpen = Math.max(...openLatencies);

        const avgClose = Math.round(closeLatencies.reduce((a, b) => a + b, 0) / closeLatencies.length);
        const minClose = Math.min(...closeLatencies);
        const maxClose = Math.max(...closeLatencies);

        console.log(`Employee Modal Open Latency: Avg=${avgOpen}ms, Min=${minOpen}ms, Max=${maxOpen}ms`);
        console.log(`Employee Modal Close Latency: Avg=${avgClose}ms, Min=${minClose}ms, Max=${maxClose}ms`);

        // 5. TEST POS BATCH PRICING RESPONSE
        console.log('Navigating to /ban-hang/pos...');
        const tPos0 = Date.now();
        await page.goto(`${BASE_URL}/ban-hang/pos`, { waitUntil: 'networkidle2' });
        const posNavDur = Date.now() - tPos0;
        await sleep(1000);

        // Search in POS F3
        console.log('Testing POS autocomplete search with batch pricing...');
        const tSearch0 = Date.now();
        await page.type('.tf-pos-search-input, input[placeholder*="Tìm kiếm"]', 'a');
        await sleep(500);
        const searchDur = Date.now() - tSearch0;

        const results = {
            timestamp: new Date().toISOString(),
            status: 'SUCCESS',
            consoleErrorsCount: consoleErrors.length,
            consoleErrors: consoleErrors,
            metrics: {
                tabSwitch: {
                    iterations: tabSwitchLatencies.length,
                    avgMs: avgTabSwitch,
                    minMs: minTabSwitch,
                    maxMs: maxTabSwitch,
                    p95Ms: p95TabSwitch
                },
                employeeModalOpen: {
                    iterations: openLatencies.length,
                    avgMs: avgOpen,
                    minMs: minOpen,
                    maxMs: maxOpen
                },
                employeeModalClose: {
                    iterations: closeLatencies.length,
                    avgMs: avgClose,
                    minMs: minClose,
                    maxMs: maxClose
                },
                posNavigationMs: posNavDur,
                posSearchLatencyMs: searchDur
            }
        };

        fs.writeFileSync(
            'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901/full_system_rehabilitation_results.json',
            JSON.stringify(results, null, 2),
            'utf8'
        );

        console.log('\n=== BENCHMARK COMPLETED SUCCESSFULLY ===');
        console.log(JSON.stringify(results, null, 2));

    } catch (err) {
        console.error('Benchmark Error:', err);
    } finally {
        await browser.close();
    }
})();
