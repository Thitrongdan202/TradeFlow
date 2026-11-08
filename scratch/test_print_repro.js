const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('--- REPRODUCING PRINT ISSUE ---');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1600,1000']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1600, height: 1000 });

    // Login
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);

    // Go to /hop-dong
    await page.goto('http://127.0.0.1:5169/hop-dong', { waitUntil: 'networkidle2' });
    await new Promise(r => setTimeout(r, 1500));

    // Switch to Employment Contracts tab
    await page.evaluate(() => {
        const buttons = Array.from(document.querySelectorAll('button, a, .nav-link, .tf-segmented-tab'));
        const b = buttons.find(btn => btn.innerText.includes('Hợp đồng lao động'));
        if (b) b.click();
    });
    await new Promise(r => setTimeout(r, 2000));

    // Find and click "Tạo HĐ lao động mới"
    const clicked = await page.evaluate(() => {
        const btns = Array.from(document.querySelectorAll('button'));
        const btn = btns.find(b => b.innerText.includes('Tạo HĐ lao động mới'));
        if (btn) {
            btn.click();
            return true;
        }
        return false;
    });
    console.log('Tạo HĐ lao động mới clicked:', clicked);
    await new Promise(r => setTimeout(r, 2000));

    // Go to Preview Step (4)
    const step4 = await page.evaluate(() => {
        const stepBtns = Array.from(document.querySelectorAll('.tf-stepper-item, button'));
        const prevStep = stepBtns.find(b => b.innerText.includes('Xem trước') || b.innerText.includes('4'));
        if (prevStep) {
            prevStep.click();
            return true;
        }
        return false;
    });
    console.log('Step 4 clicked:', step4);
    await new Promise(r => setTimeout(r, 2000));

    // Take screenshot of step 4 preview in modal
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'modal_step4_preview.png'), fullPage: false });

    // Inspect preview DOM
    const previewInfo = await page.evaluate(() => {
        const printContainer = document.getElementById('print-employment-contract');
        if (!printContainer) return { error: 'print-employment-contract not found' };
        
        const pages = Array.from(printContainer.querySelectorAll('.tf-a4-page'));
        const footers = Array.from(printContainer.querySelectorAll('.tf-a4-page-footer'));
        
        return {
            pageCount: pages.length,
            footerTexts: footers.map(f => f.innerText.replace(/\n/g, ' ')),
            sigBlockExists: !!printContainer.querySelector('.tf-signature-block')
        };
    });
    console.log('Preview Info in Create Form Modal:', previewInfo);

    // Now test what happens when we call tradeflow.printDocument
    // or test printing via window.print / iframe print!
    const printIframeCheck = await page.evaluate(async () => {
        let createdIframe = null;
        const origAppend = document.body.appendChild.bind(document.body);
        document.body.appendChild = function(child) {
            if (child.tagName === 'IFRAME' && child.className.includes('tf-print-sandboxed-iframe')) {
                createdIframe = child;
            }
            return origAppend(child);
        };

        // Click Print button
        const printBtn = Array.from(document.querySelectorAll('button')).find(b => b.innerText.includes('In') || b.title?.includes('In'));
        if (printBtn) {
            printBtn.click();
        }

        await new Promise(r => setTimeout(r, 1000));

        if (createdIframe && createdIframe.contentDocument) {
            const doc = createdIframe.contentDocument;
            const ifrPages = Array.from(doc.querySelectorAll('.tf-a4-page'));
            const ifrFooters = Array.from(doc.querySelectorAll('.tf-a4-page-footer'));
            const ifrBody = doc.body.innerHTML;
            return {
                iframeFound: true,
                pageCount: ifrPages.length,
                footers: ifrFooters.map(f => f.innerText.replace(/\n/g, ' ')),
                bodyLength: ifrBody.length,
                classes: Array.from(doc.body.classList)
            };
        }
        return { iframeFound: false };
    });
    console.log('Print Iframe Check:', printIframeCheck);

    await browser.close();
})();
