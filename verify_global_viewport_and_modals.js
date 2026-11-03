const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://127.0.0.1:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('[VERIFY] Starting Global Viewport, Responsive Layout & Modal System Repair Audit...');

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
    const results = {
        unauthenticatedLanding: {},
        employeeModal: {},
        contractGallery: {},
        viewportCentering: {}
    };

    try {
        // =================================================================
        // TEST 1: Unauthenticated Landing Screen at "/"
        // =================================================================
        console.log('[1/5] Testing Unauthenticated Landing Page at "/"...');
        await page.setViewport({ width: 1440, height: 900 });
        await page.goto(`${BASE_URL}/`, { waitUntil: 'networkidle2' });
        await sleep(1000);

        // Check for .tf-welcome-card
        const welcomeCardHandle = await page.$('.tf-welcome-card');
        if (welcomeCardHandle) {
            const cardBox = await welcomeCardHandle.boundingBox();
            const contentBox = await page.evaluate(() => {
                const content = document.querySelector('.tf-content');
                if (!content) return null;
                const r = content.getBoundingClientRect();
                return { x: r.x, y: r.y, width: r.width, height: r.height };
            });

            const leftInContent = cardBox.x - (contentBox ? contentBox.x : 0);
            const rightInContent = (contentBox ? contentBox.width : 1440) - (leftInContent + cardBox.width);
            const isHorizontallyCentered = Math.abs(leftInContent - rightInContent) < 30;

            console.log(`Welcome Card: width=${cardBox.width.toFixed(1)}, leftInContent=${leftInContent.toFixed(1)}, rightInContent=${rightInContent.toFixed(1)}, top=${cardBox.y.toFixed(1)}`);
            console.log(`Horizontally Centered in Content Area: ${isHorizontallyCentered ? 'PASS' : 'FAIL'} (delta: ${Math.abs(leftInContent - rightInContent).toFixed(1)}px)`);

            results.unauthenticatedLanding = {
                found: true,
                cardBox,
                contentBox,
                leftInContent,
                rightInContent,
                isHorizontallyCentered,
                top: cardBox.y
            };

            await page.screenshot({ path: path.join(ARTIFACT_DIR, 'unauthenticated_landing_1440.png') });

            // Test in Dark Mode
            await page.evaluate(() => {
                document.documentElement.setAttribute('data-bs-theme', 'dark');
                document.documentElement.setAttribute('data-theme', 'dark');
            });
            await sleep(500);
            await page.screenshot({ path: path.join(ARTIFACT_DIR, 'unauthenticated_landing_dark.png') });
            console.log('Saved unauthenticated landing screenshots in light & dark mode.');

            // Switch back to light mode
            await page.evaluate(() => {
                document.documentElement.setAttribute('data-bs-theme', 'light');
                document.documentElement.setAttribute('data-theme', 'light');
            });
        } else {
            console.error('FAIL: .tf-welcome-card not found!');
            results.unauthenticatedLanding.found = false;
        }

        // =================================================================
        // STEP 2: Authenticate as Admin
        // =================================================================
        console.log('[2/5] Logging in as Admin...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('#login-username', { timeout: 10000 });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        console.log('Logged in successfully. Navigated to:', page.url());
        await sleep(2000);

        // =================================================================
        // TEST 3: Employee Modal at Standard, 125%, and 150% Zoom
        // =================================================================
        console.log('[3/5] Testing Employee Modal across resolutions and zooms...');
        await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('.tf-page-actions, .tf-page-header-actions', { timeout: 10000 });
        await sleep(1500); // Allow SignalR to fully bind

        // Click "Nhân viên" primary button
        const clicked = await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-page-actions button.tf-btn-primary, button.tf-btn-primary'));
            const btn = btns.find(b => b.innerText && (b.innerText.includes('Nhân viên') || b.innerText.includes('Thêm')));
            if (btn) {
                btn.click();
                return true;
            }
            return false;
        });

        console.log('Employee modal button clicked:', clicked);
        await sleep(1500);

        // Wait for modal body
        await page.waitForSelector('.employee-modal-body, .employee-modal', { visible: true, timeout: 8000 });

        // 3.1 Standard 1920x1080
        await page.setViewport({ width: 1920, height: 1080 });
        await sleep(600);
        let empBox = await page.evaluate(() => {
            const el = document.querySelector('.employee-modal');
            if (!el) return null;
            const r = el.getBoundingClientRect();
            return { x: r.x, y: r.y, width: r.width, height: r.height, top: r.top, bottom: r.bottom };
        });
        console.log(`Employee Modal @ 1920x1080: top=${empBox.top.toFixed(1)}, bottom=${empBox.bottom.toFixed(1)}, height=${empBox.height.toFixed(1)}`);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'employee_modal_1920x1080.png') });
        results.employeeModal['1920x1080'] = empBox;

        // 3.2 Laptop 1366x768
        await page.setViewport({ width: 1366, height: 768 });
        await sleep(600);
        empBox = await page.evaluate(() => {
            const el = document.querySelector('.employee-modal');
            if (!el) return null;
            const r = el.getBoundingClientRect();
            return { x: r.x, y: r.y, width: r.width, height: r.height, top: r.top, bottom: r.bottom };
        });
        console.log(`Employee Modal @ 1366x768: top=${empBox.top.toFixed(1)}, bottom=${empBox.bottom.toFixed(1)}, height=${empBox.height.toFixed(1)}`);
        const isNegative1366 = empBox.top < 0;
        console.log(`Safe centering check @ 1366x768: ${!isNegative1366 ? 'PASS (top >= 0)' : 'FAIL (negative coordinate clipping!)'}`);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'employee_modal_1366x768.png') });
        results.employeeModal['1366x768'] = empBox;

        // 3.3 Zoom 125% (Viewport 1093 x 614, deviceScaleFactor 1.25)
        await page.setViewport({ width: 1093, height: 614, deviceScaleFactor: 1.25 });
        await sleep(600);
        empBox = await page.evaluate(() => {
            const el = document.querySelector('.employee-modal');
            if (!el) return null;
            const r = el.getBoundingClientRect();
            return { x: r.x, y: r.y, width: r.width, height: r.height, top: r.top, bottom: r.bottom };
        });
        console.log(`Employee Modal @ Zoom 125%: top=${empBox.top.toFixed(1)}, height=${empBox.height.toFixed(1)}`);
        const isNegative125 = empBox.top < 0;
        console.log(`Safe centering check @ Zoom 125%: ${!isNegative125 ? 'PASS (top >= 0)' : 'FAIL'}`);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'employee_modal_zoom125.png') });
        results.employeeModal['zoom125'] = empBox;

        // 3.4 Zoom 150% (Viewport 960 x 540, deviceScaleFactor 1.5)
        await page.setViewport({ width: 960, height: 540, deviceScaleFactor: 1.5 });
        await sleep(600);
        empBox = await page.evaluate(() => {
            const el = document.querySelector('.employee-modal');
            if (!el) return null;
            const r = el.getBoundingClientRect();
            const basicGrid = document.querySelector('.tf-emp-grid-basic');
            const grid3 = document.querySelector('.tf-emp-grid-3');
            return {
                x: r.x, y: r.y, width: r.width, height: r.height, top: r.top, bottom: r.bottom,
                basicGridCols: basicGrid ? window.getComputedStyle(basicGrid).gridTemplateColumns : null,
                grid3Cols: grid3 ? window.getComputedStyle(grid3).gridTemplateColumns : null
            };
        });
        console.log(`Employee Modal @ Zoom 150%: top=${empBox.top.toFixed(1)}, height=${empBox.height.toFixed(1)}`);
        console.log(`Responsive grid reflow: basicGridCols=${empBox.basicGridCols}, grid3Cols=${empBox.grid3Cols}`);
        const isNegative150 = empBox.top < 0;
        console.log(`Safe centering check @ Zoom 150%: ${!isNegative150 ? 'PASS (top >= 0)' : 'FAIL'}`);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'employee_modal_zoom150.png') });
        results.employeeModal['zoom150'] = empBox;

        // Close modal
        await page.click('.tf-modal-close-btn');
        await sleep(600);

        // =================================================================
        // TEST 4: Contract Template Gallery Page & Modal
        // =================================================================
        console.log('[4/5] Testing Contract Template Gallery Page...');
        await page.setViewport({ width: 1440, height: 900, deviceScaleFactor: 1.0 });
        await page.goto(`${BASE_URL}/hop-dong/thu-vien-mau`, { waitUntil: 'networkidle2' });
        await sleep(1500);

        // Check category pills wrapping
        const pillCheck = await page.evaluate(() => {
            const catBar = document.querySelector('.tf-category-bar');
            if (!catBar) return null;
            return {
                flexWrap: window.getComputedStyle(catBar).flexWrap,
                scrollWidth: catBar.scrollWidth,
                clientWidth: catBar.clientWidth,
                hasOverflowScroll: catBar.scrollWidth > catBar.clientWidth
            };
        });
        console.log('Category Bar Check:', pillCheck);
        results.contractGallery.pillCheck = pillCheck;

        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'contract_gallery_page_verified.png') });

        // Now test the TemplateSelectorModal on /hop-dong
        console.log('Testing Template Selector Modal on /hop-dong...');
        await page.goto(`${BASE_URL}/hop-dong`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('.tf-page-actions, .tf-page-header-actions, button.tf-primary-btn', { timeout: 10000 });
        await sleep(1500);

        const templateModalClicked = await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('button.tf-primary-btn, button, a'));
            const btn = btns.find(b => b.textContent && b.textContent.includes('Tạo hợp đồng mới'));
            if (btn) {
                btn.click();
                return true;
            }
            return false;
        });

        console.log('Template Selector button clicked:', templateModalClicked);
        if (templateModalClicked) {
            await sleep(1500);

            const modalDialog = await page.$('.tf-gallery-modal-dialog, .modal-dialog');
            if (modalDialog) {
                const dialogBox = await modalDialog.boundingBox();
                console.log(`Template Selector Modal: width=${dialogBox.width.toFixed(1)}, height=${dialogBox.height.toFixed(1)}, top=${dialogBox.y.toFixed(1)}`);
                await page.screenshot({ path: path.join(ARTIFACT_DIR, 'template_selector_modal_verified.png') });
                results.contractGallery.modalBox = dialogBox;
            }
        }

        // Write verification results to JSON
        fs.writeFileSync(
            path.join(ARTIFACT_DIR, 'global_viewport_verification_results.json'),
            JSON.stringify(results, null, 2),
            'utf8'
        );
        console.log('[5/5] Verification completed successfully! All results and screenshots saved.');

    } catch (err) {
        console.error('Error during verification:', err);
    } finally {
        await browser.close();
    }
})();
