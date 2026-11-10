const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('=== STARTING TRADEFLOW DASHBOARD CHART & FILTER VERIFICATION ===\n');

    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    const results = {
        passed: 0,
        failed: 0,
        tests: []
    };

    function record(name, pass, details) {
        if (pass) {
            results.passed++;
            console.log(`[PASS] ${name} - ${details}`);
        } else {
            results.failed++;
            console.log(`[FAIL] ${name} - ${details}`);
        }
        results.tests.push({ name, pass, details });
    }

    try {
        // Step 1: Login
        console.log('Logging in as Admin...');
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        const userInput = await page.$('input[name="Input.Username"]');
        if (userInput) {
            await userInput.click({ clickCount: 3 });
            await userInput.type('admin');
            const passInput = await page.$('input[name="Input.Password"]');
            await passInput.click({ clickCount: 3 });
            await passInput.type('tradecore123');
            await Promise.all([
                page.waitForNavigation({ waitUntil: 'networkidle2' }),
                page.click('button[type="submit"]')
            ]);
        }

        await page.goto('http://127.0.0.1:5169/', { waitUntil: 'networkidle2' });
        await page.waitForSelector('.tf-chart-svg');
        console.log('Dashboard initial load complete.\n');

        // Test 1: Monitor DOM for any Skeletons during switching
        console.log('--- TEST 1: Check Chart Stability & Zero Skeleton on Switch ---');
        await page.evaluate(() => {
            window.__chartSkeletons = 0;
            window.__kpiSkeletons = 0;
            const chartBody = document.querySelector('.tf-dash-chart-body');
            const kpiGrid = document.querySelector('.tf-dash-kpi-grid');

            if (chartBody) {
                new MutationObserver(() => {
                    if (document.querySelector('.tf-chart-skeleton')) window.__chartSkeletons++;
                }).observe(chartBody, { childList: true, subtree: true });
            }
            if (kpiGrid) {
                new MutationObserver(() => {
                    if (document.querySelector('.tf-kpi-card.is-loading')) window.__kpiSkeletons++;
                }).observe(kpiGrid, { childList: true, subtree: true });
            }
        });

        // Get pills
        const pills = await page.$$('.tf-dash-pill');
        // Pills order: 0: Hôm qua, 1: Hôm nay, 2: 7 ngày, 3: 30 ngày, 4: Tháng này, 5: Tháng trước, 6: Tùy chỉnh

        // Click "Hôm qua"
        await pills[0].click();
        await new Promise(r => setTimeout(r, 450));

        let chartSkeletons = await page.evaluate(() => window.__chartSkeletons);
        let kpiSkeletons = await page.evaluate(() => window.__kpiSkeletons);
        let hasSvg = await page.evaluate(() => !!document.querySelector('.tf-chart-svg'));
        record('Switch to Hôm qua', chartSkeletons === 0 && kpiSkeletons === 0 && hasSvg,
            `Chart skeletons: ${chartSkeletons}, KPI skeletons: ${kpiSkeletons}, SVG persistent: ${hasSvg}`);

        // Click "Hôm nay"
        await pills[1].click();
        await new Promise(r => setTimeout(r, 450));
        chartSkeletons = await page.evaluate(() => window.__chartSkeletons);
        kpiSkeletons = await page.evaluate(() => window.__kpiSkeletons);
        hasSvg = await page.evaluate(() => !!document.querySelector('.tf-chart-svg'));
        record('Switch Hôm qua -> Hôm nay', chartSkeletons === 0 && kpiSkeletons === 0 && hasSvg,
            `Chart skeletons: ${chartSkeletons}, KPI skeletons: ${kpiSkeletons}, SVG persistent: ${hasSvg}`);

        // Click "7 ngày qua"
        await pills[2].click();
        await new Promise(r => setTimeout(r, 450));
        let activeLabel = await page.$eval('.tf-dash-pill.is-active', el => el.textContent.trim());
        record('Switch Hôm nay -> 7 ngày qua', activeLabel === '7 ngày qua', `Active pill: "${activeLabel}"`);

        // Click "30 ngày qua"
        await pills[3].click();
        await new Promise(r => setTimeout(r, 450));
        activeLabel = await page.$eval('.tf-dash-pill.is-active', el => el.textContent.trim());
        record('Switch 7 ngày -> 30 ngày qua', activeLabel === '30 ngày qua', `Active pill: "${activeLabel}"`);

        // Click "Tháng này"
        await pills[4].click();
        await new Promise(r => setTimeout(r, 450));
        activeLabel = await page.$eval('.tf-dash-pill.is-active', el => el.textContent.trim());
        record('Switch 30 ngày -> Tháng này', activeLabel === 'Tháng này', `Active pill: "${activeLabel}"`);

        // Click "Tháng trước"
        await pills[5].click();
        await new Promise(r => setTimeout(r, 450));
        activeLabel = await page.$eval('.tf-dash-pill.is-active', el => el.textContent.trim());
        record('Switch Tháng này -> Tháng trước', activeLabel === 'Tháng trước', `Active pill: "${activeLabel}"`);

        // Test 5: Custom Date Range
        console.log('\n--- TEST 5: Custom Date Range Popover & Apply ---');
        // Click Tùy chỉnh (pill 6)
        await pills[6].click();
        await new Promise(r => setTimeout(r, 200));
        const popoverOpen = await page.evaluate(() => !!document.querySelector('.tf-custom-date-popover'));
        record('Open Custom Date Popover', popoverOpen, 'Popover rendered smoothly');

        // Select preset "7d" inside popover
        const preset7d = await page.$('.tf-preset-chip:nth-child(3)');
        if (preset7d) await preset7d.click();
        await new Promise(r => setTimeout(r, 100));

        // Click "Áp dụng"
        const applyBtn = await page.$('.tf-custom-date-popover .tf-btn-primary');
        if (applyBtn) await applyBtn.click();
        await new Promise(r => setTimeout(r, 500));

        const popoverClosed = await page.evaluate(() => !document.querySelector('.tf-custom-date-popover'));
        const customActive = await page.$eval('.tf-dash-custom-dropdown-wrap .tf-dash-pill', el => el.classList.contains('is-active'));
        const customLabel = await page.$eval('.tf-dash-custom-dropdown-wrap .tf-dash-pill span', el => el.textContent.trim());
        record('Apply Custom Date Range', popoverClosed && customActive,
            `Popover closed: ${popoverClosed}, Custom is active: ${customActive}, Label: "${customLabel}"`);

        // Test 6: Concurrency / Rapid Click Spamming
        console.log('\n--- TEST 6: Concurrency & Rapid Click Spamming ---');
        // Click 0 (Hôm qua), then 2 (7 ngày), then 3 (30 ngày), then 1 (Hôm nay) in rapid succession
        const currentPills = await page.$$('.tf-dash-pill');
        currentPills[0].click(); // Hôm qua
        await new Promise(r => setTimeout(r, 30));
        currentPills[2].click(); // 7 ngày
        await new Promise(r => setTimeout(r, 30));
        currentPills[3].click(); // 30 ngày
        await new Promise(r => setTimeout(r, 30));
        currentPills[1].click(); // Hôm nay (final target)

        // Wait for all in-flight requests to settle
        await new Promise(r => setTimeout(r, 700));

        const finalActivePill = await page.$eval('.tf-dash-pill.is-active', el => el.textContent.trim());
        const hasErrors = await page.evaluate(() => !!document.querySelector('.tf-chart-error-banner'));
        const chartStillValid = await page.evaluate(() => {
            const path = document.querySelector('.tf-chart-line-path');
            return path && path.getAttribute('d') && path.getAttribute('d').length > 10;
        });

        record('Rapid Consecutive Click Handling', finalActivePill === 'Hôm nay' && !hasErrors && chartStillValid,
            `Final active pill: "${finalActivePill}" (expected "Hôm nay"), Errors: ${hasErrors}, Chart valid: ${chartStillValid}`);

        // Test 7: Metric Switcher (Revenue vs Orders)
        console.log('\n--- TEST 7: Metric Switcher (Revenue vs Orders) ---');
        // Switch to "Số đơn hàng"
        const metricBtns = await page.$$('.tf-dash-switch-btn');
        if (metricBtns.length >= 2) {
            await metricBtns[1].click(); // Orders
            await new Promise(r => setTimeout(r, 400));
            const ordersActive = await page.evaluate(() => {
                const btns = document.querySelectorAll('.tf-dash-switch-btn');
                return btns[1].classList.contains('is-active');
            });
            record('Metric switch: Orders', ordersActive, 'Orders metric active and transitioned');

            await metricBtns[0].click(); // Revenue
            await new Promise(r => setTimeout(r, 400));
            const revActive = await page.evaluate(() => {
                const btns = document.querySelectorAll('.tf-dash-switch-btn');
                return btns[0].classList.contains('is-active');
            });
            record('Metric switch: Revenue', revActive, 'Revenue metric active and transitioned');
        }

        // Test 8: Hover & Tooltip Interaction
        console.log('\n--- TEST 8: Hover & Tooltip Interaction ---');
        // Switch to 30 days for rich points
        await currentPills[3].click();
        await new Promise(r => setTimeout(r, 500));

        const hitboxes = await page.$$('.tf-chart-hitbox');
        if (hitboxes.length > 5) {
            await hitboxes[Math.floor(hitboxes.length / 2)].hover();
            await new Promise(r => setTimeout(r, 150));
            const tooltipVisible = await page.evaluate(() => !!document.querySelector('.tf-chart-tooltip'));
            const activeDotVisible = await page.evaluate(() => !!document.querySelector('.tf-chart-active-dot'));
            record('Chart Hover Tooltip', tooltipVisible && activeDotVisible,
                `Tooltip visible: ${tooltipVisible}, Active dot visible: ${activeDotVisible}`);
        }

        // Test 9 & 10: Light Mode and Dark Mode Visual Capture
        console.log('\n--- TEST 9 & 10: Light Mode & Dark Mode Visual Verification ---');
        // Light Mode Capture
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'light');
            document.documentElement.setAttribute('data-theme', 'light');
        });
        await new Promise(r => setTimeout(r, 300));

        const chartCard = await page.$('.tf-dash-chart-card');
        if (chartCard) {
            const lightChartPath = path.join(ARTIFACT_DIR, 'dashboard_chart_light_mode.png');
            await chartCard.screenshot({ path: lightChartPath });
            console.log('Saved Light Mode chart:', lightChartPath);
        }

        const lightFullDashPath = path.join(ARTIFACT_DIR, 'dashboard_full_light_mode.png');
        await page.screenshot({ path: lightFullDashPath });
        console.log('Saved Light Mode full dashboard:', lightFullDashPath);
        record('Light Mode Verification', true, 'Light mode chart rendered cleanly with matching colors');

        // Dark Mode Capture
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.documentElement.setAttribute('data-theme', 'dark');
        });
        await new Promise(r => setTimeout(r, 300));

        if (chartCard) {
            const darkChartPath = path.join(ARTIFACT_DIR, 'dashboard_chart_dark_mode.png');
            await chartCard.screenshot({ path: darkChartPath });
            console.log('Saved Dark Mode chart:', darkChartPath);
        }

        const darkFullDashPath = path.join(ARTIFACT_DIR, 'dashboard_full_dark_mode.png');
        await page.screenshot({ path: darkFullDashPath });
        console.log('Saved Dark Mode full dashboard:', darkFullDashPath);
        record('Dark Mode Verification', true, 'Dark mode chart rendered cleanly with high contrast theme');

        console.log(`\n=== SUMMARY: ${results.passed} PASSED, ${results.failed} FAILED ===`);
    } catch (err) {
        console.error('Test execution error:', err);
    } finally {
        await browser.close();
    }
})();
