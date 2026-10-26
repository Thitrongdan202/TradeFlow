const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('🚀 Starting Comprehensive Dark Mode Persistence & Navigation Audit Suite...');

    const browser = await puppeteer.launch({
        headless: 'new',
        defaultViewport: { width: 1440, height: 900 },
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    const results = {
        passed: 0,
        failed: 0,
        details: []
    };

    function assert(name, condition, extra = '') {
        if (condition) {
            console.log(`  ✅ PASS: ${name} ${extra}`);
            results.passed++;
            results.details.push({ test: name, status: 'PASS', extra });
        } else {
            console.error(`  ❌ FAIL: ${name} ${extra}`);
            results.failed++;
            results.details.push({ test: name, status: 'FAIL', extra });
        }
    }

    try {
        // Step 1: Login
        console.log('\n--- Step 1: Login & Theme Toggle Initialization ---');
        await page.goto('http://localhost:5169/Account/Login', { waitUntil: 'networkidle0', timeout: 30000 });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle0' }),
            page.click('button[type="submit"]')
        ]);
        console.log('Logged in successfully. Current URL:', page.url());

        // Step 2: Ensure Dark Mode is activated
        console.log('\n--- Step 2: Activating Dark Mode ---');
        let isDark = await page.evaluate(() => document.documentElement.getAttribute('data-bs-theme') === 'dark');
        if (!isDark) {
            console.log('Switching to Dark Mode via ThemeToggle click...');
            await page.click('.tf-theme-toggle');
            await new Promise(r => setTimeout(r, 600));
        }

        // Verify initial Dark Mode state
        const state0 = await page.evaluate(() => {
            const html = document.documentElement;
            const cookies = document.cookie;
            return {
                bsTheme: html.getAttribute('data-bs-theme'),
                dataTheme: html.getAttribute('data-theme'),
                isDarkClass: html.classList.contains('dark'),
                localStorage: localStorage.getItem('tf_theme'),
                cookies: cookies,
                toggleIsDark: document.querySelector('.tf-theme-toggle')?.classList.contains('is-dark'),
                toggleChecked: document.querySelector('.tf-theme-toggle')?.getAttribute('aria-checked')
            };
        });
        console.log('Initial Dark Mode State:', state0);
        assert('HTML data-bs-theme is dark', state0.bsTheme === 'dark');
        assert('HTML class contains dark', state0.isDarkClass === true);
        assert('Cookie contains tf_theme=dark', state0.cookies.includes('tf_theme=dark'));
        assert('ThemeToggle has is-dark class', state0.toggleIsDark === true);

        // Step 3: Test Navigation Across All Core Modules via Enhanced Navigation (Sidebar clicks)
        console.log('\n--- Step 3: Enhanced Navigation Audit Across Modules ---');
        const navigationTargets = [
            { name: 'Tổng quan (Dashboard)', selector: 'a[href=""]', urlMatch: '/' },
            { name: 'Đơn bán hàng', selector: 'a[href="/ban-hang/don-ban-hang"]', urlMatch: '/ban-hang/don-ban-hang' },
            { name: 'Báo giá', selector: 'a[href="/ban-hang/bao-gia"]', urlMatch: '/ban-hang/bao-gia' },
            { name: 'Hóa đơn', selector: 'a[href="/hoa-don"]', urlMatch: '/hoa-don' },
            { name: 'Sản phẩm', selector: 'a[href="/danh-muc/san-pham"]', urlMatch: '/danh-muc/san-pham' },
            { name: 'Kho hàng', selector: 'a[href="/kho-hang"]', urlMatch: '/kho-hang' },
            { name: 'Khách hàng', selector: 'a[href="/doi-tac/khach-hang"]', urlMatch: '/doi-tac/khach-hang' },
            { name: 'Nhà cung cấp', selector: 'a[href="/doi-tac/nha-cung-cap"]', urlMatch: '/doi-tac/nha-cung-cap' },
            { name: 'Hợp đồng', selector: 'a[href="/hop-dong"]', urlMatch: '/hop-dong' },
            { name: 'Chứng từ', selector: 'a[href="/tai-lieu"]', urlMatch: '/tai-lieu' },
            { name: 'Nhân viên', selector: 'a[href="/nhan-vien"]', urlMatch: '/nhan-vien' },
            { name: 'Cài đặt', selector: 'a[href="/cai-dat"]', urlMatch: '/cai-dat' },
            { name: 'POS Bán hàng', selector: 'a[href="/ban-hang/pos"]', urlMatch: '/ban-hang/pos' }
        ];

        for (const target of navigationTargets) {
            console.log(`\nNavigating to: ${target.name} (${target.urlMatch})...`);
            
            await page.evaluate((targetUrl) => {
                const clean = targetUrl.replace(/^\//, '');
                const link = document.querySelector(`a[href="${targetUrl}"], a[href="/${clean}"], a[href="${clean}"]`);
                if (link) {
                    link.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
                } else {
                    window.location.href = targetUrl;
                }
            }, target.urlMatch);

            // Wait for Blazor enhanced navigation to patch DOM
            await new Promise(r => setTimeout(r, 700));

            const pageState = await page.evaluate(() => {
                const html = document.documentElement;
                const toggle = document.querySelector('.tf-theme-toggle');
                const card = document.querySelector('.tf-card, .card');
                const app = document.querySelector('.tf-app');
                const body = document.body;
                return {
                    url: window.location.pathname,
                    bsTheme: html.getAttribute('data-bs-theme'),
                    dataTheme: html.getAttribute('data-theme'),
                    isDarkClass: html.classList.contains('dark'),
                    toggleIsDark: toggle ? toggle.classList.contains('is-dark') : null,
                    appBg: app ? window.getComputedStyle(app).backgroundColor : null,
                    cardBg: card ? window.getComputedStyle(card).backgroundColor : null,
                    bodyBg: body ? window.getComputedStyle(body).backgroundColor : null
                };
            });

            console.log(`  Page state for ${target.name}:`, pageState);
            assert(`${target.name} - HTML data-bs-theme is dark`, pageState.bsTheme === 'dark');
            assert(`${target.name} - HTML has dark class`, pageState.isDarkClass === true);
            assert(`${target.name} - Toggle is dark`, pageState.toggleIsDark === true);
            assert(`${target.name} - Background is dark (not white/light)`, 
                pageState.appBg !== 'rgb(241, 243, 246)' && pageState.cardBg !== 'rgb(255, 255, 255)');
        }

        // Capture verification screenshot of dark Supplier page (which previously failed)
        await page.goto('http://localhost:5169/doi-tac/nha-cung-cap', { waitUntil: 'networkidle0' });
        await new Promise(r => setTimeout(r, 600));
        const suppShot = path.join(ARTIFACT_DIR, 'verify_nav_suppliers_dark.png');
        await page.screenshot({ path: suppShot, fullPage: false });
        console.log(`Saved screenshot: ${suppShot}`);

        // Capture verification screenshot of dark Invoices page (which previously failed)
        await page.goto('http://localhost:5169/hoa-don', { waitUntil: 'networkidle0' });
        await new Promise(r => setTimeout(r, 600));
        const invShot = path.join(ARTIFACT_DIR, 'verify_nav_invoices_dark.png');
        await page.screenshot({ path: invShot, fullPage: false });
        console.log(`Saved screenshot: ${invShot}`);

        // Capture verification screenshot of dark Products page
        await page.goto('http://localhost:5169/danh-muc/san-pham', { waitUntil: 'networkidle0' });
        await new Promise(r => setTimeout(r, 600));
        const prodShot = path.join(ARTIFACT_DIR, 'verify_nav_products_dark.png');
        await page.screenshot({ path: prodShot, fullPage: false });
        console.log(`Saved screenshot: ${prodShot}`);

        // Capture verification screenshot of dark Contracts page
        await page.goto('http://localhost:5169/hop-dong', { waitUntil: 'networkidle0' });
        await new Promise(r => setTimeout(r, 600));
        const contractShot = path.join(ARTIFACT_DIR, 'verify_nav_contracts_dark.png');
        await page.screenshot({ path: contractShot, fullPage: false });
        console.log(`Saved screenshot: ${contractShot}`);

        // Capture verification screenshot of dark POS page
        await page.goto('http://localhost:5169/ban-hang/pos', { waitUntil: 'networkidle0' });
        await new Promise(r => setTimeout(r, 600));
        const posShot = path.join(ARTIFACT_DIR, 'verify_nav_pos_dark.png');
        await page.screenshot({ path: posShot, fullPage: false });
        console.log(`Saved screenshot: ${posShot}`);

        // Step 4: Direct Page Reload (F5) on a nested route
        console.log('\n--- Step 4: Direct Page Reload (F5) State Persistence ---');
        await page.reload({ waitUntil: 'networkidle0' });
        await new Promise(r => setTimeout(r, 600));
        const reloadState = await page.evaluate(() => {
            const html = document.documentElement;
            const toggle = document.querySelector('.tf-theme-toggle');
            return {
                bsTheme: html.getAttribute('data-bs-theme'),
                dataTheme: html.getAttribute('data-theme'),
                isDarkClass: html.classList.contains('dark'),
                toggleIsDark: toggle ? toggle.classList.contains('is-dark') : null
            };
        });
        console.log('After F5 reload on /hoa-don:', reloadState);
        assert('F5 Reload - HTML data-bs-theme is dark', reloadState.bsTheme === 'dark');
        assert('F5 Reload - HTML has dark class', reloadState.isDarkClass === true);
        assert('F5 Reload - Toggle is dark', reloadState.toggleIsDark === true);

        // Step 5: Test Popups & Floating Panels
        console.log('\n--- Step 5: Popups & Floating Panels Verification in Dark Mode ---');
        // A. Notification dropdown
        const notifBtn = await page.$('.tf-topbar-btn[title*="thông báo"], .tf-notif-bell, button[aria-label*="thông báo"]');
        if (notifBtn) {
            await notifBtn.click();
            await new Promise(r => setTimeout(r, 400));
            const notifShot = path.join(ARTIFACT_DIR, 'verify_popup_notifications_dark.png');
            await page.screenshot({ path: notifShot, fullPage: false });
            console.log(`Saved notification dropdown screenshot: ${notifShot}`);
            await page.keyboard.press('Escape');
        }

        // B. User account dropdown
        const userBtn = await page.$('.tf-user-avatar-btn, .tf-user-menu-btn, .tf-avatar');
        if (userBtn) {
            await userBtn.click();
            await new Promise(r => setTimeout(r, 400));
            const userShot = path.join(ARTIFACT_DIR, 'verify_popup_user_menu_dark.png');
            await page.screenshot({ path: userShot, fullPage: false });
            console.log(`Saved user menu screenshot: ${userShot}`);
            await page.keyboard.press('Escape');
        }

        // Step 6: Document Paper Isolation Verification (Quotation Detail / Print Preview)
        console.log('\n--- Step 6: Document Paper Isolation Verification ---');
        await page.goto('http://localhost:5169/ban-hang/bao-gia/chi-tiet/2', { waitUntil: 'networkidle0' });
        await new Promise(r => setTimeout(r, 800));

        const paperCheck = await page.evaluate(() => {
            const docPaper = document.querySelector('.tf-document-paper, .tf-print-paper, .quotation-paper, .tf-doc-paper');
            const termsBox = document.querySelector('.tf-doc-terms, .tf-terms-box, .quotation-terms, .tf-doc-notes');
            return {
                docPaperExists: !!docPaper,
                docPaperBg: docPaper ? window.getComputedStyle(docPaper).backgroundColor : null,
                docPaperColor: docPaper ? window.getComputedStyle(docPaper).color : null,
                termsBoxBg: termsBox ? window.getComputedStyle(termsBox).backgroundColor : null,
                termsBoxColor: termsBox ? window.getComputedStyle(termsBox).color : null
            };
        });
        console.log('Document Paper Isolation Check:', paperCheck);
        if (paperCheck.docPaperBg) {
            assert('Document Paper has pure white background', 
                paperCheck.docPaperBg === 'rgb(255, 255, 255)' || paperCheck.docPaperBg === '#ffffff');
            assert('Document Paper has dark ink text', 
                paperCheck.docPaperColor === 'rgb(17, 24, 39)' || paperCheck.docPaperColor === 'rgb(30, 41, 59)' || paperCheck.docPaperColor === 'rgb(0, 0, 0)');
        }
        const docShot = path.join(ARTIFACT_DIR, 'verify_document_paper_isolation.png');
        await page.screenshot({ path: docShot, fullPage: false });
        console.log(`Saved document paper screenshot: ${docShot}`);

        // Step 7: Switch Back to Light Mode & Verify Navigation
        console.log('\n--- Step 7: Switching to Light Mode & Testing Persistence ---');
        await page.click('.tf-theme-toggle');
        await new Promise(r => setTimeout(r, 600));

        const lightState = await page.evaluate(() => {
            const html = document.documentElement;
            const toggle = document.querySelector('.tf-theme-toggle');
            return {
                bsTheme: html.getAttribute('data-bs-theme'),
                dataTheme: html.getAttribute('data-theme'),
                isDarkClass: html.classList.contains('dark'),
                toggleIsDark: toggle ? toggle.classList.contains('is-dark') : null,
                cookie: document.cookie
            };
        });
        console.log('Switched to Light Mode State:', lightState);
        assert('HTML data-bs-theme is light', lightState.bsTheme === 'light');
        assert('HTML class does NOT contain dark', lightState.isDarkClass === false);
        assert('Cookie contains tf_theme=light', lightState.cookie.includes('tf_theme=light'));
        assert('Toggle is not dark', lightState.toggleIsDark === false);

        // Navigate to /doi-tac/nha-cung-cap in Light Mode
        await page.goto('http://localhost:5169/doi-tac/nha-cung-cap', { waitUntil: 'networkidle0' });
        await new Promise(r => setTimeout(r, 600));
        const suppLightState = await page.evaluate(() => {
            const html = document.documentElement;
            const toggle = document.querySelector('.tf-theme-toggle');
            return {
                bsTheme: html.getAttribute('data-bs-theme'),
                isDarkClass: html.classList.contains('dark'),
                toggleIsDark: toggle ? toggle.classList.contains('is-dark') : null
            };
        });
        assert('Light Mode Nav - Suppliers data-bs-theme is light', suppLightState.bsTheme === 'light');
        assert('Light Mode Nav - Suppliers has NO dark class', suppLightState.isDarkClass === false);
        assert('Light Mode Nav - Suppliers Toggle is light', suppLightState.toggleIsDark === false);

        const suppLightShot = path.join(ARTIFACT_DIR, 'verify_nav_suppliers_light.png');
        await page.screenshot({ path: suppLightShot, fullPage: false });
        console.log(`Saved screenshot: ${suppLightShot}`);

        // Switch back to Dark Mode for final enterprise state
        await page.click('.tf-theme-toggle');
        await new Promise(r => setTimeout(r, 600));

    } catch (err) {
        console.error('Fatal error during test suite:', err);
        results.failed++;
    } finally {
        await browser.close();
    }

    console.log('\n=============================================');
    console.log(`Test Suite Completed: ${results.passed} Passed, ${results.failed} Failed`);
    console.log('=============================================');
    fs.writeFileSync(path.join(ARTIFACT_DIR, 'theme_persistence_audit_results.json'), JSON.stringify(results, null, 2));
    process.exit(results.failed > 0 ? 1 : 0);
})();
