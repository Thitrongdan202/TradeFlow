const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('=== TESTING TRADEFLOW.PRINTDOCUMENT SANDBOXED IFRAME ===');
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
    await new Promise(r => setTimeout(r, 1500));

    // Open first employment contract modal
    await page.evaluate(() => {
        const viewBtns = Array.from(document.querySelectorAll('button[title*="chi tiết"], button.tf-action-btn'));
        if (viewBtns.length > 0) viewBtns[0].click();
    });
    await new Promise(r => setTimeout(r, 2000));

    // Inspect iframe generation by calling printDocument
    const iframeHtml = await page.evaluate(async () => {
        let capturedHtml = '';
        const origCreate = document.createElement.bind(document);
        document.createElement = function(tag) {
            const el = origCreate(tag);
            if (tag.toLowerCase() === 'iframe') {
                setTimeout(() => {
                    if (el.contentDocument && el.contentDocument.documentElement) {
                        capturedHtml = el.contentDocument.documentElement.outerHTML;
                    }
                }, 100);
            }
            return el;
        };

        // Click print button
        const printBtn = Array.from(document.querySelectorAll('button')).find(b => b.innerText.trim() === 'In' || b.innerText.includes('In'));
        if (printBtn) printBtn.click();

        await new Promise(r => setTimeout(r, 600));
        return capturedHtml;
    });

    console.log('Captured iframe HTML length:', iframeHtml.length);
    if (iframeHtml.length > 0) {
        fs.writeFileSync(path.join(ARTIFACT_DIR, 'captured_print_iframe.html'), iframeHtml, 'utf-8');
        console.log('Saved captured_print_iframe.html');

        const printTab = await browser.newPage();
        await printTab.setContent(iframeHtml, { waitUntil: 'load' });
        await printTab.emulateMediaType('print');

        const pdfPath = path.join(ARTIFACT_DIR, 'verified_iframe_chrome_print.pdf');
        await printTab.pdf({
            path: pdfPath,
            format: 'A4',
            printBackground: true,
            margin: { top: 0, bottom: 0, left: 0, right: 0 }
        });
        console.log('Saved verified_iframe_chrome_print.pdf');
    }

    await browser.close();
    console.log('=== DONE ===');
})();
