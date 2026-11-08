const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('=== EXACT HEIGHT & PAGINATION AUDIT FOR PRINT ===');
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
    await new Promise(r => setTimeout(r, 1000));

    // Switch to Employment Contracts tab
    await page.evaluate(() => {
        const b = Array.from(document.querySelectorAll('button')).find(x => x.innerText.includes('Hợp đồng lao động'));
        if (b) b.click();
    });
    await new Promise(r => setTimeout(r, 1000));

    // Check detail modal first
    console.log('\n--- AUDITING EMPLOYMENT CONTRACT DETAIL MODAL ---');
    const openedDetail = await page.evaluate(() => {
        const viewBtns = Array.from(document.querySelectorAll('button[title*="chi tiết"], button.tf-action-btn'));
        if (viewBtns.length > 0) {
            viewBtns[0].click();
            return true;
        }
        return false;
    });
    console.log('Opened detail modal:', openedDetail);
    await new Promise(r => setTimeout(r, 2000));

    // Measure page heights in Detail Modal
    const detailMetrics = await page.evaluate(() => {
        const pages = Array.from(document.querySelectorAll('#print-employment-contract .tf-a4-page'));
        return pages.map((p, idx) => ({
            pageIndex: idx + 1,
            scrollHeight: p.scrollHeight,
            offsetHeight: p.offsetHeight,
            clientHeight: p.clientHeight,
            maxHeightPxA4: 297 * 3.7795, // 297mm in pixels at 96dpi = ~1122.5px
            classes: p.className
        }));
    });
    console.log('Detail Modal Page Heights (Limit is ~1122.5px for A4):', detailMetrics);

    // Close detail modal
    await page.evaluate(() => {
        const closeBtn = document.querySelector('.modal.show .btn-close, .modal.show button[aria-label="Close"], .modal.show .modal-footer button');
        if (closeBtn) closeBtn.click();
    });
    await new Promise(r => setTimeout(r, 1000));

    // Now check create form modal (Step 4)
    console.log('\n--- AUDITING EMPLOYMENT CONTRACT CREATE/EDIT FORM MODAL ---');
    await page.evaluate(() => {
        const b = Array.from(document.querySelectorAll('button')).find(x => x.innerText.includes('Tạo HĐ lao động mới'));
        if (b) b.click();
    });
    await new Promise(r => setTimeout(r, 1500));

    // Go to step 4
    await page.evaluate(() => {
        const items = Array.from(document.querySelectorAll('.tf-stepper-item'));
        if (items[3]) items[3].click();
    });
    await new Promise(r => setTimeout(r, 1500));

    const formMetrics = await page.evaluate(() => {
        const pages = Array.from(document.querySelectorAll('#print-employment-contract .tf-a4-page'));
        return pages.map((p, idx) => ({
            pageIndex: idx + 1,
            scrollHeight: p.scrollHeight,
            offsetHeight: p.offsetHeight,
            clientHeight: p.clientHeight,
            maxHeightPxA4: 297 * 3.7795,
            classes: p.className
        }));
    });
    console.log('Form Modal Step 4 Page Heights (Limit is ~1122.5px for A4):', formMetrics);

    // Let's print using tradeflow.printDocument and inspect the actual HTML written to the iframe!
    const iframeDetails = await page.evaluate(async () => {
        let capturedHtml = '';
        let capturedTitle = '';
        const origAppend = document.body.appendChild.bind(document.body);
        document.body.appendChild = function(child) {
            if (child.tagName === 'IFRAME') {
                setTimeout(() => {
                    try {
                        const doc = child.contentDocument || child.contentWindow?.document;
                        if (doc) {
                            capturedHtml = doc.documentElement.outerHTML;
                            capturedTitle = doc.title;
                        }
                    } catch (e) {}
                }, 200);
            }
            return origAppend(child);
        };

        // Trigger print
        const printBtn = Array.from(document.querySelectorAll('button')).find(b => b.innerText.includes('In') || b.title?.includes('In'));
        if (printBtn) printBtn.click();

        await new Promise(r => setTimeout(r, 1200));
        return {
            title: capturedTitle,
            hasHtml: !!capturedHtml,
            htmlLength: capturedHtml.length,
            htmlSnippet: capturedHtml.substring(0, 1500)
        };
    });
    console.log('\nIframe Print Inspection:', iframeDetails);

    await browser.close();
})();
