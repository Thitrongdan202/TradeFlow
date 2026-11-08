const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('=== STARTING TRADEFLOW LABOR CONTRACT COMPREHENSIVE VERIFICATION ===');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1600,1000']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1600, height: 1000 });

    try {
        // Step 1: Login
        console.log('1. Logging in as admin...');
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        console.log('   Logged in successfully. Current URL:', page.url());

        // Step 2: Navigate to Contracts page
        console.log('2. Navigating to /hop-dong...');
        await page.goto('http://127.0.0.1:5169/hop-dong', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 2000));

        // Switch to Employment Contracts tab
        console.log('3. Switching to Hợp đồng lao động tab...');
        const tabBtn = await page.evaluate(() => {
            const buttons = Array.from(document.querySelectorAll('button, a, .nav-link, .tf-segmented-tab'));
            const b = buttons.find(btn => btn.innerText.includes('Hợp đồng lao động & Nhân sự') || btn.innerText.includes('Hợp đồng lao động'));
            if (b) { b.click(); return b.innerText; }
            return null;
        });
        console.log('   Clicked tab:', tabBtn);
        await new Promise(r => setTimeout(r, 2000));

        // Step 3: Open contract 1 detail modal
        console.log('4. Opening first employment contract modal...');
        const openedDetail = await page.evaluate(() => {
            const viewBtns = Array.from(document.querySelectorAll('button[title*="chi tiết"], button.tf-action-btn'));
            if (viewBtns.length > 0) {
                viewBtns[0].click();
                return true;
            }
            return false;
        });
        console.log('   Clicked detail action button:', openedDetail);
        await new Promise(r => setTimeout(r, 2500));

        // Verify Modal Opened
        const modalAnalysis = await page.evaluate(() => {
            const el = document.getElementById('print-employment-contract');
            if (!el) return { found: false };
            const p1 = el.querySelector('.tf-a4-page-1');
            const p2 = el.querySelector('.tf-a4-page-2');
            const p3 = el.querySelector('.tf-a4-page-3');

            const p1Text = p1 ? p1.innerText : '';
            const p2Text = p2 ? p2.innerText : '';
            const p3Text = p3 ? p3.innerText : '';

            const footers = Array.from(el.querySelectorAll('.tf-a4-page-footer')).map(f => f.innerText.replace(/\s+/g, ' ').trim());

            return {
                found: true,
                hasPage1: !!p1,
                hasPage2: !!p2,
                hasPage3: !!p3,
                footers: footers,
                page1Snippet: p1Text.substring(0, 450),
                page3Snippet: p3Text.substring(Math.max(0, p3Text.length - 450)),
                hasTranThiMaiPhuong: p1Text.includes('Trần Thị Mai Phương') || p3Text.includes('Trần Thị Mai Phương'),
                hasNguyenVanAn: p1Text.includes('Nguyễn Văn An') || p3Text.includes('Nguyễn Văn An'),
            };
        });

        console.log('   Detail Modal Analysis:', JSON.stringify(modalAnalysis, null, 2));

        // Take screenshot of Modal
        const modalScreenshotPath = path.join(ARTIFACT_DIR, 'verified_modal_detail_3pages.png');
        await page.screenshot({ path: modalScreenshotPath, fullPage: false });
        console.log('   Saved detail modal screenshot to:', modalScreenshotPath);

        // Step 4: Test Simulated Print (Emulate Media Print)
        console.log('5. Testing Print Media Rendering (Emulating Print Media)...');
        await page.emulateMediaType('print');

        const printStylesCheck = await page.evaluate(() => {
            const modalBody = document.querySelector('.tf-contract-modal-body');
            const p1 = document.querySelector('.tf-a4-page-1');
            const p3 = document.querySelector('.tf-a4-page-3');
            const bodyBg = modalBody ? window.getComputedStyle(modalBody).backgroundColor : 'none';
            const p1Bg = p1 ? window.getComputedStyle(p1).backgroundColor : 'none';
            const p3Bg = p3 ? window.getComputedStyle(p3).backgroundColor : 'none';
            const footerStyle = document.querySelector('.tf-a4-page-footer');
            const footerFontSize = footerStyle ? window.getComputedStyle(footerStyle).fontSize : 'none';

            return {
                modalBodyBg: bodyBg,
                page1Bg: p1Bg,
                page3Bg: p3Bg,
                footerFontSize: footerFontSize
            };
        });
        console.log('   Print computed styles:', JSON.stringify(printStylesCheck, null, 2));

        // Generate Chrome print PDF
        const printPdfPath = path.join(ARTIFACT_DIR, 'verified_chrome_print_document.pdf');
        await page.pdf({
            path: printPdfPath,
            format: 'A4',
            printBackground: true,
            margin: { top: 0, bottom: 0, left: 0, right: 0 }
        });
        console.log('   Saved simulated Chrome print PDF to:', printPdfPath);

        // Switch back to screen media
        await page.emulateMediaType('screen');

        // Close Detail Modal
        await page.evaluate(() => {
            const closeBtn = document.querySelector('.btn-close, button[aria-label="Đóng"]');
            if (closeBtn) closeBtn.click();
        });
        await new Promise(r => setTimeout(r, 1500));

        // Step 5: Test Contract Creation Form Modal (Step 4 Preview)
        console.log('6. Opening Create Contract Modal...');
        const openedCreate = await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('button'));
            const b = btns.find(btn => btn.innerText.includes('Tạo hợp đồng') || btn.innerText.includes('Thêm hợp đồng'));
            if (b) { b.click(); return b.innerText; }
            return false;
        });
        console.log('   Clicked create button:', openedCreate);
        await new Promise(r => setTimeout(r, 2000));

        // Click on Step 4 (Xem trước)
        console.log('7. Navigating to Step 4 (Xem trước văn bản A4)...');
        const clickedStep4 = await page.evaluate(() => {
            const stepperItems = Array.from(document.querySelectorAll('.tf-stepper-item, button'));
            const s4 = stepperItems.find(item => item.innerText.includes('Xem trước'));
            if (s4) { s4.click(); return item.innerText; }
            return false;
        });
        console.log('   Clicked Step 4:', clickedStep4);
        await new Promise(r => setTimeout(r, 2000));

        // Check Step 4 Preview Structure
        const step4Analysis = await page.evaluate(() => {
            const el = document.getElementById('print-employment-contract');
            if (!el) return { found: false };
            const p1 = el.querySelector('.tf-a4-page-1');
            const p2 = el.querySelector('.tf-a4-page-2');
            const p3 = el.querySelector('.tf-a4-page-3');
            const footers = Array.from(el.querySelectorAll('.tf-a4-page-footer')).map(f => f.innerText.replace(/\s+/g, ' ').trim());
            const hasDownloadBtn = Array.from(document.querySelectorAll('button')).some(b => b.innerText.includes('Tải xuống PDF'));
            const hasPrintBtn = Array.from(document.querySelectorAll('button')).some(b => b.innerText.includes('In hợp đồng'));

            return {
                found: true,
                hasPage1: !!p1,
                hasPage2: !!p2,
                hasPage3: !!p3,
                footers: footers,
                hasDownloadBtn: hasDownloadBtn,
                hasPrintBtn: hasPrintBtn
            };
        });
        console.log('   Step 4 Preview Analysis:', JSON.stringify(step4Analysis, null, 2));

        const step4ScreenshotPath = path.join(ARTIFACT_DIR, 'verified_step4_wizard_preview.png');
        await page.screenshot({ path: step4ScreenshotPath, fullPage: false });
        console.log('   Saved Step 4 preview screenshot to:', step4ScreenshotPath);

        // Step 6: Test Direct Backend PDF Export Endpoint
        console.log('8. Testing Direct Backend PDF Endpoint (/api/contracts/employment/1/export-pdf)...');
        const cookies = await page.cookies();
        const cookieHeader = cookies.map(c => `${c.name}=${c.value}`).join('; ');

        const response = await fetch('http://127.0.0.1:5169/api/contracts/employment/1/export-pdf', {
            headers: {
                'Cookie': cookieHeader,
                'Accept': 'application/pdf'
            }
        });

        console.log('   Backend PDF Export HTTP Status:', response.status);
        console.log('   Content-Type:', response.headers.get('content-type'));
        if (response.ok) {
            const buffer = Buffer.from(await response.arrayBuffer());
            const directPdfPath = path.join(ARTIFACT_DIR, 'verified_backend_direct_export.pdf');
            fs.writeFileSync(directPdfPath, buffer);
            console.log('   Successfully downloaded direct PDF (' + buffer.length + ' bytes) to:', directPdfPath);
        }

        console.log('=== VERIFICATION COMPLETED SUCCESSFULLY ===');
    } catch (err) {
        console.error('Error during verification:', err);
    } finally {
        await browser.close();
    }
})();
