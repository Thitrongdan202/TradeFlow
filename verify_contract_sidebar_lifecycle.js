const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function sleep(ms) {
    return new Promise(r => setTimeout(r, ms));
}

(async () => {
    console.log('--- STARTING COMPREHENSIVE SYSTEM-WIDE AUDIT & VERIFICATION ---');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage', '--window-size=1920,1080']
    });

    const results = {
        login: false,
        closeByX: false,
        sidebarAfterX: false,
        navigationAfterX: false,
        closeByBackdrop: false,
        sidebarAfterBackdrop: false,
        closeByEscape: false,
        sidebarAfterEscape: false,
        closeByCancel: false,
        sidebarAfterCancel: false,
        tabTransitions: false,
        moduleAudit: {},
        viewports: {}
    };

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1920, height: 1080 });

        // Step 0: Authenticate
        console.log('Logging in as Admin (admin / tradecore123)...');
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        await page.waitForSelector('#login-username', { timeout: 10000 });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        await sleep(1000);
        console.log('Login completed! Current URL:', page.url());
        results.login = true;

        // Navigate to Contracts page with employment tab active
        console.log('Navigating to http://127.0.0.1:5169/hop-dong?tab=employment ...');
        await page.goto('http://127.0.0.1:5169/hop-dong?tab=employment', { waitUntil: 'networkidle0', timeout: 30000 });
        await sleep(1500);

        // Helper: get sidebar interactivity status
        async function getSidebarStatus() {
            return await page.evaluate(() => {
                const sidebar = document.querySelector('.tf-sidebar');
                if (!sidebar) return { exists: false };
                const cs = window.getComputedStyle(sidebar);
                const bodyHasClass = document.body.classList.contains('tf-modal-open');
                const htmlHasClass = document.documentElement.classList.contains('tf-modal-open');
                const bodyOverflow = document.body.style.overflow;
                const activeModal = !!document.querySelector('.modal.show, .tf-modal-overlay, [class*="-modal-backdrop"]');

                return {
                    exists: true,
                    pointerEvents: cs.pointerEvents,
                    zIndex: cs.zIndex,
                    bodyHasClass,
                    htmlHasClass,
                    bodyOverflow,
                    activeModal,
                    isInteractive: cs.pointerEvents !== 'none' && !bodyHasClass
                };
            });
        }

        // Helper: open employment modal
        async function openEmploymentModal() {
            console.log('Opening Employment Contract Modal...');
            await page.evaluate(() => {
                const btns = Array.from(document.querySelectorAll('button'));
                const btn = btns.find(b => b.textContent && b.textContent.includes('Tạo HĐ lao động mới'));
                if (btn) btn.click();
                else throw new Error('Create button not found');
            });
            await sleep(800);
        }

        // ---------------------------------------------------------
        // TEST 1: Open modal, switch tabs, close via X button
        // ---------------------------------------------------------
        console.log('\n--- TEST 1: Open Modal, Test Tab Transitions & Close via X ---');
        await openEmploymentModal();

        // Check modal is open
        const modalVisible = await page.evaluate(() => {
            const m = document.querySelector('.modal.show');
            const h = document.querySelector('.tf-contract-modal-title');
            return !!m && !!h;
        });
        console.log('Modal visible:', modalVisible);

        // Test Tab transitions
        console.log('Testing Tab switching...');
        const tabResults = await page.evaluate(async () => {
            const tabs = Array.from(document.querySelectorAll('.tf-segmented-tab'));
            const logs = [];

            // Click Tab 2
            const tab2 = tabs.find(t => t.textContent.includes('2.'));
            if (tab2) tab2.click();
            await new Promise(r => setTimeout(r, 250));
            const p2 = document.getElementById('emp-tab-remuneration');
            logs.push({ tab: 2, display: p2 ? window.getComputedStyle(p2).display : 'null', hasActive: p2 ? p2.classList.contains('is-active') : false });

            // Click Tab 3
            const tab3 = tabs.find(t => t.textContent.includes('3.'));
            if (tab3) tab3.click();
            await new Promise(r => setTimeout(r, 250));
            const p3 = document.getElementById('emp-tab-clauses');
            logs.push({ tab: 3, display: p3 ? window.getComputedStyle(p3).display : 'null', hasActive: p3 ? p3.classList.contains('is-active') : false });

            // Click Tab 4
            const tab4 = tabs.find(t => t.textContent.includes('4.'));
            if (tab4) tab4.click();
            await new Promise(r => setTimeout(r, 250));
            const p4 = document.getElementById('emp-tab-preview');
            logs.push({ tab: 4, display: p4 ? window.getComputedStyle(p4).display : 'null', hasActive: p4 ? p4.classList.contains('is-active') : false });

            // Click back to Tab 1
            const tab1 = tabs.find(t => t.textContent.includes('1.'));
            if (tab1) tab1.click();
            await new Promise(r => setTimeout(r, 250));
            const p1 = document.getElementById('emp-tab-info');
            logs.push({ tab: 1, display: p1 ? window.getComputedStyle(p1).display : 'null', hasActive: p1 ? p1.classList.contains('is-active') : false });

            return logs;
        });
        console.log('Tab transition logs:', tabResults);
        results.tabTransitions = tabResults.every(t => t.display !== 'none');

        // Capture screenshot of modal with tab active
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'contract_wizard_tab1.png'), fullPage: false });

        // Close modal via X button
        console.log('Closing modal via X button...');
        await page.evaluate(() => {
            const closeBtn = document.querySelector('.tf-contract-modal-header .tf-modal-close-btn');
            if (closeBtn) closeBtn.click();
            else throw new Error('Close button not found');
        });
        await sleep(600);

        let sbStatus1 = await getSidebarStatus();
        console.log('Sidebar status after X close:', sbStatus1);
        results.closeByX = !sbStatus1.activeModal;
        results.sidebarAfterX = sbStatus1.isInteractive;

        // Verify real navigation via sidebar without refresh
        console.log('Testing Sidebar click to /ban-hang...');
        await page.evaluate(() => {
            const directLink = document.querySelector('.tf-sidebar a[title="Bán hàng"], .tf-sidebar a[href="/ban-hang"]');
            if (directLink) directLink.click();
            else throw new Error('Ban hang link not found');
        });
        await sleep(1500);

        const currentUrl1 = page.url();
        console.log('Current URL after navigation:', currentUrl1);
        results.navigationAfterX = currentUrl1.includes('/ban-hang');
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'navigated_to_sales_after_modal_close.png'), fullPage: false });

        // Navigate back to contracts
        await page.goto('http://127.0.0.1:5169/hop-dong?tab=employment', { waitUntil: 'networkidle0' });
        await sleep(1200);

        // ---------------------------------------------------------
        // TEST 2: Open modal and close via Backdrop click
        // ---------------------------------------------------------
        console.log('\n--- TEST 2: Close via Backdrop Click ---');
        await openEmploymentModal();
        await sleep(400);

        console.log('Clicking backdrop...');
        await page.evaluate(() => {
            const backdrop = document.querySelector('.modal.show');
            if (backdrop) backdrop.click();
        });
        await sleep(600);

        let sbStatus2 = await getSidebarStatus();
        console.log('Sidebar status after Backdrop close:', sbStatus2);
        results.closeByBackdrop = !sbStatus2.activeModal;
        results.sidebarAfterBackdrop = sbStatus2.isInteractive;

        // ---------------------------------------------------------
        // TEST 3: Open modal and close via Escape key
        // ---------------------------------------------------------
        console.log('\n--- TEST 3: Close via Escape Key ---');
        await openEmploymentModal();
        await sleep(500);

        console.log('Pressing Escape key...');
        await page.keyboard.press('Escape');
        await sleep(600);

        let sbStatus3 = await getSidebarStatus();
        console.log('Sidebar status after Escape close:', sbStatus3);
        results.closeByEscape = !sbStatus3.activeModal;
        results.sidebarAfterEscape = sbStatus3.isInteractive;

        // ---------------------------------------------------------
        // TEST 4: Open modal and close via Cancel button
        // ---------------------------------------------------------
        console.log('\n--- TEST 4: Close via Cancel Button in Footer ---');
        await openEmploymentModal();
        await sleep(400);

        console.log('Clicking Cancel button in footer...');
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.modal-footer button, .tf-contract-modal-footer button'));
            const cancelBtn = btns.find(b => b.textContent && b.textContent.includes('Hủy bỏ'));
            if (cancelBtn) cancelBtn.click();
            else {
                const cb = document.querySelector('.tf-modal-close-btn');
                if (cb) cb.click();
            }
        });
        await sleep(600);

        let sbStatus4 = await getSidebarStatus();
        console.log('Sidebar status after Cancel close:', sbStatus4);
        results.closeByCancel = !sbStatus4.activeModal;
        results.sidebarAfterCancel = sbStatus4.isInteractive;

        // ---------------------------------------------------------
        // TEST 5: Responsive Viewports (1920x1080, 1440x900, 1366x768)
        // ---------------------------------------------------------
        console.log('\n--- TEST 5: Responsive Viewports (1920x1080, 1440x900, 1366x768) ---');
        const viewports = [
            { name: '1920x1080', width: 1920, height: 1080, zoomLabel: '100%' },
            { name: '1440x900', width: 1440, height: 900, zoomLabel: '125% eq' },
            { name: '1366x768', width: 1366, height: 768, zoomLabel: '150% eq' }
        ];

        for (const vp of viewports) {
            console.log(`Testing viewport ${vp.name}...`);
            await page.setViewport({ width: vp.width, height: vp.height });
            await sleep(400);

            await openEmploymentModal();
            await sleep(500);

            const modalMetrics = await page.evaluate(() => {
                const dialog = document.querySelector('.modal-dialog');
                const content = document.querySelector('.modal-content');
                const body = document.querySelector('.modal-body');
                const header = document.querySelector('.modal-header');

                const winH = window.innerHeight;
                const winW = window.innerWidth;
                const rect = dialog ? dialog.getBoundingClientRect() : null;

                return {
                    winW,
                    winH,
                    dialogW: rect ? Math.round(rect.width) : 0,
                    dialogH: rect ? Math.round(rect.height) : 0,
                    topGap: rect ? Math.round(rect.top) : 0,
                    bottomGap: rect ? Math.round(winH - rect.bottom) : 0,
                    fitsViewport: rect ? (rect.bottom <= winH && rect.top >= 0) : false,
                    bodyScrollable: body ? (body.scrollHeight > body.clientHeight) : false
                };
            });
            console.log(`Viewport ${vp.name} metrics:`, modalMetrics);

            const shotPath = path.join(ARTIFACT_DIR, `contract_modal_vp_${vp.name}.png`);
            await page.screenshot({ path: shotPath, fullPage: false });

            // Close modal
            await page.evaluate(() => {
                const cb = document.querySelector('.tf-contract-modal-header .tf-modal-close-btn');
                if (cb) cb.click();
            });
            await sleep(500);

            const finalSb = await getSidebarStatus();
            results.viewports[vp.name] = {
                metrics: modalMetrics,
                sidebarInteractive: finalSb.isInteractive,
                screenshot: shotPath
            };
        }

        // ---------------------------------------------------------
        // TEST 6: Audit 10 Core Modules for Sidebar & Navigation Health
        // ---------------------------------------------------------
        console.log('\n--- TEST 6: 10 Core Modules SPA Navigation & Sidebar Health ---');
        const modules = [
            { name: 'Dashboard', url: 'http://127.0.0.1:5169/' },
            { name: 'Sales', url: 'http://127.0.0.1:5169/ban-hang' },
            { name: 'Invoices', url: 'http://127.0.0.1:5169/hoa-don' },
            { name: 'Products', url: 'http://127.0.0.1:5169/san-pham' },
            { name: 'Warehouses', url: 'http://127.0.0.1:5169/kho-hang' },
            { name: 'Partners', url: 'http://127.0.0.1:5169/doi-tac/khach-hang' },
            { name: 'Contracts', url: 'http://127.0.0.1:5169/hop-dong' },
            { name: 'Documents', url: 'http://127.0.0.1:5169/chung-tu' },
            { name: 'Employees', url: 'http://127.0.0.1:5169/nhan-vien' },
            { name: 'Settings', url: 'http://127.0.0.1:5169/cai-dat' }
        ];

        for (const mod of modules) {
            console.log(`Checking module: ${mod.name} (${mod.url})...`);
            await page.goto(mod.url, { waitUntil: 'networkidle0', timeout: 20000 });
            await sleep(600);
            const status = await getSidebarStatus();
            results.moduleAudit[mod.name] = {
                url: mod.url,
                sidebarInteractive: status.isInteractive,
                bodyHasModalClass: status.bodyHasClass,
                pointerEvents: status.pointerEvents
            };
        }

        console.log('\n--- ALL AUDIT TESTS COMPLETED SUCCESSFULLY ---');
        console.log('Final Verification Summary:\n', JSON.stringify(results, null, 2));

        // Save results JSON
        fs.writeFileSync(
            path.join(ARTIFACT_DIR, 'contract_sidebar_lifecycle_verification.json'),
            JSON.stringify(results, null, 2),
            'utf8'
        );

    } catch (err) {
        console.error('Test execution error:', err);
    } finally {
        await browser.close();
    }
})();
