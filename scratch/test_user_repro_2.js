const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('=== TESTING STEP 4 PREVIEW IN CREATE MODAL ===');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1600,1000']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1600, height: 1000 });

    // 1. Login
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);

    // 2. Go to /hop-dong
    await page.goto('http://127.0.0.1:5169/hop-dong', { waitUntil: 'networkidle2' });
    await new Promise(r => setTimeout(r, 2000));

    // 3. Switch tab to Hợp đồng lao động
    await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button, a, .nav-link, .tf-segmented-tab'));
        const b = btns.find(x => x.innerText.includes('Hợp đồng lao động'));
        if (b) b.click();
    });
    await new Promise(r => setTimeout(r, 2000));

    // 4. Click "Tạo HĐ lao động mới"
    await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        const b = btns.find(x => x.innerText.includes('Tạo HĐ lao động mới'));
        if (b) b.click();
    });
    await new Promise(r => setTimeout(r, 2500));

    // Click step 4 inside .tf-modal-stepper
    const step4Clicked = await page.evaluate(() => {
        const stepper = document.querySelector('.tf-modal-stepper');
        if (!stepper) return false;
        const items = Array.from(stepper.querySelectorAll('.tf-stepper-item'));
        if (items.length >= 4) {
            items[3].click();
            return true;
        }
        return false;
    });
    console.log('Step 4 clicked inside stepper:', step4Clicked);
    await new Promise(r => setTimeout(r, 2500));

    // Check preview in modal
    const modalCheck = await page.evaluate(() => {
        const el = document.getElementById('print-employment-contract');
        if (!el) return { error: 'print-employment-contract not found' };
        const pages = Array.from(el.querySelectorAll('.tf-a4-page'));
        const footers = Array.from(el.querySelectorAll('.tf-a4-page-footer')).map(f => f.innerText.replace(/\s+/g, ' '));
        const sig = el.querySelector('.tf-signature-block');
        return {
            pageCount: pages.length,
            footers: footers,
            sigExists: !!sig,
            sigText: sig ? sig.innerText.replace(/\s+/g, ' ') : null
        };
    });
    console.log('Modal Step 4 Check:', modalCheck);

    // Save screenshot of modal
    const modalShot = path.join(ARTIFACT_DIR, 'repro_create_modal_step4.png');
    await page.screenshot({ path: modalShot });
    console.log('Saved screenshot to:', modalShot);

    // Test printDocument capture
    const printHtml = await page.evaluate(async () => {
        let html = '';
        const origAppend = document.body.appendChild.bind(document.body);
        document.body.appendChild = function(node) {
            if (node.tagName === 'IFRAME') {
                setTimeout(() => {
                    try {
                        if (node.contentDocument && node.contentDocument.documentElement) {
                            html = node.contentDocument.documentElement.outerHTML;
                        }
                    } catch(e) {}
                }, 100);
            }
            return origAppend(node);
        };

        const printBtn = Array.from(document.querySelectorAll('button')).find(b => b.innerText.includes('In hợp đồng') || (b.innerText.includes('In') && b.closest('.modal')));
        if (printBtn) {
            printBtn.click();
        }
        await new Promise(r => setTimeout(r, 600));
        return html;
    });

    console.log('Print HTML captured, length:', printHtml.length);
    if (printHtml.length > 0) {
        fs.writeFileSync(path.join(ARTIFACT_DIR, 'repro_create_print_iframe.html'), printHtml, 'utf-8');
        
        // Render this iframe HTML using page.pdf
        const printPage = await browser.newPage();
        await printPage.setContent(printHtml, { waitUntil: 'load' });
        await printPage.emulateMediaType('print');
        
        const pdfFile = path.join(ARTIFACT_DIR, 'repro_create_chrome_print.pdf');
        await printPage.pdf({
            path: pdfFile,
            format: 'A4',
            printBackground: true,
            margin: { top: 0, bottom: 0, left: 0, right: 0 }
        });
        console.log('Saved repro_create_chrome_print.pdf');
    }

    await browser.close();
    console.log('=== COMPLETED ===');
})();
