const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const BASE_URL = 'http://127.0.0.1:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function runBenchmark(phaseName) {
    console.log(`\n==================================================`);
    console.log(`🧪 Running Modal Performance Benchmark: ${phaseName}`);
    console.log(`==================================================`);

    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1920,1080']
    });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1920, height: 1080 });

        // Login
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);

        // Navigate to Employees page
        await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
        await sleep(1000);

        // Benchmark 1: Add Employee Modal Open/Close (10 iterations)
        console.log('⚡ Benchmark 1: Add Employee Modal Open/Close...');
        const addModalMetrics = [];
        for (let i = 0; i < 5; i++) {
            const startOpen = Date.now();
            await page.click('.tf-page-actions .tf-btn-primary');
            await page.waitForSelector('.employee-modal', { visible: true });
            const openDuration = Date.now() - startOpen;

            // Switch mode twice
            const startMode = Date.now();
            await page.click('.tf-mode-tab:nth-child(2)');
            await sleep(180);
            await page.click('.tf-mode-tab:nth-child(1)');
            await sleep(180);
            const modeDuration = Date.now() - startMode;

            // Close modal
            const startClose = Date.now();
            await page.click('.employee-modal .tf-modal-close-btn');
            await page.waitForSelector('.employee-modal', { hidden: true });
            const closeDuration = Date.now() - startClose;

            addModalMetrics.push({ openDuration, modeDuration, closeDuration });
            await sleep(200);
        }

        // Benchmark 2: Employee Request Review Modal Open/Close & Tab Switching (5 iterations)
        console.log('⚡ Benchmark 2: Employee Request Review Modal & Tab Switching...');
        const reqModalMetrics = [];
        for (let i = 0; i < 5; i++) {
            const startOpen = Date.now();
            await page.click('.tf-page-actions .tf-btn-outline');
            await page.waitForSelector('.tf-req-body', { visible: true });
            const openDuration = Date.now() - startOpen;

            // Tab switching
            const startTabs = Date.now();
            const tabs = await page.$$('.tf-req-filter-bar .tf-tab-btn');
            if (tabs.length >= 3) {
                await tabs[1].click(); // Cần bổ sung hồ sơ
                await sleep(160);
                await tabs[2].click(); // Đã duyệt
                await sleep(160);
                await tabs[0].click(); // Chờ duyệt
                await sleep(160);
            }
            const tabDuration = Date.now() - startTabs;

            // Close modal via Escape
            const startClose = Date.now();
            await page.keyboard.press('Escape');
            await page.waitForSelector('.tf-req-body', { hidden: true });
            const closeDuration = Date.now() - startClose;

            reqModalMetrics.push({ openDuration, tabDuration, closeDuration });
            await sleep(200);
        }

        // Summary Calculations
        const avg = arr => (arr.reduce((a, b) => a + b, 0) / arr.length).toFixed(1);

        const addOpenAvg = avg(addModalMetrics.map(m => m.openDuration));
        const addCloseAvg = avg(addModalMetrics.map(m => m.closeDuration));
        const modeSwitchAvg = avg(addModalMetrics.map(m => m.modeDuration / 2));

        const reqOpenAvg = avg(reqModalMetrics.map(m => m.openDuration));
        const reqCloseAvg = avg(reqModalMetrics.map(m => m.closeDuration));
        const tabSwitchAvg = avg(reqModalMetrics.map(m => m.tabDuration / 3));

        const result = {
            phase: phaseName,
            addModal: {
                openAvgMs: parseFloat(addOpenAvg),
                closeAvgMs: parseFloat(addCloseAvg),
                modeSwitchAvgMs: parseFloat(modeSwitchAvg)
            },
            reqModal: {
                openAvgMs: parseFloat(reqOpenAvg),
                closeAvgMs: parseFloat(reqCloseAvg),
                tabSwitchAvgMs: parseFloat(tabSwitchAvg)
            }
        };

        console.log('\n📊 RESULTS SUMMARY:');
        console.log(`- Add Employee Modal Open: ${addOpenAvg} ms`);
        console.log(`- Add Employee Modal Close: ${addCloseAvg} ms`);
        console.log(`- Form Mode Switch: ${modeSwitchAvg} ms`);
        console.log(`- Request Review Modal Open: ${reqOpenAvg} ms`);
        console.log(`- Request Review Modal Close: ${reqCloseAvg} ms`);
        console.log(`- Request Tab Switch: ${tabSwitchAvg} ms`);

        return result;
    } finally {
        await browser.close();
    }
}

if (require.main === module) {
    const phase = process.argv[2] || 'Baseline';
    runBenchmark(phase).then(res => {
        fs.writeFileSync(path.join(__dirname, `modal_perf_${phase.toLowerCase()}.json`), JSON.stringify(res, null, 2));
    }).catch(err => {
        console.error('Benchmark failed:', err);
        process.exitCode = 1;
    });
}
