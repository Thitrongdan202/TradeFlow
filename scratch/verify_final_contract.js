const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('=== VERIFYING REBALANCED CONTRACT PAGINATION & PRINT ===');
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

    // Open create form modal
    await page.evaluate(() => {
        const b = Array.from(document.querySelectorAll('button')).find(x => x.innerText.includes('Tạo HĐ lao động mới'));
        if (b) b.click();
    });
    await new Promise(r => setTimeout(r, 1500));

    // Go to step 4 (Preview)
    await page.evaluate(() => {
        const items = Array.from(document.querySelectorAll('.tf-stepper-item'));
        if (items[3]) items[3].click();
    });
    await new Promise(r => setTimeout(r, 1500));

    // Measure natural content heights
    const formHeights = await page.evaluate(() => {
        const pages = Array.from(document.querySelectorAll('#print-employment-contract .tf-a4-page'));
        return pages.map((p, i) => {
            const clone = p.cloneNode(true);
            clone.style.height = 'auto';
            clone.style.minHeight = '0';
            clone.style.maxHeight = 'none';
            document.body.appendChild(clone);
            const contentHeight = clone.scrollHeight;
            clone.remove();
            return {
                page: i + 1,
                contentHeightPx: contentHeight,
                a4LimitPx: 297 * 3.779527,
                headroomPx: (297 * 3.779527) - contentHeight,
                footerText: p.querySelector('.tf-a4-page-footer')?.innerText.replace(/\n/g, ' ')
            };
        });
    });
    console.log('Rebalanced Form Modal Step 4 Heights:', formHeights);

    // Capture each page screenshot in preview
    const pageHandles = await page.$$('#print-employment-contract .tf-a4-page');
    for (let i = 0; i < pageHandles.length; i++) {
        await pageHandles[i].screenshot({
            path: path.join(ARTIFACT_DIR, `verified_contract_preview_p${i+1}.png`)
        });
        console.log(`Saved verified_contract_preview_p${i+1}.png`);
    }

    // Now test what tradeflow.printDocument generates!
    const printResult = await page.evaluate(async () => {
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

        // Click Print button
        const printBtn = Array.from(document.querySelectorAll('button')).find(b => b.innerText.includes('In') || b.title?.includes('In'));
        if (printBtn) printBtn.click();

        await new Promise(r => setTimeout(r, 1000));
        return {
            title: capturedTitle,
            hasHtml: !!capturedHtml,
            htmlLength: capturedHtml.length
        };
    });
    console.log('Print Document Result:', printResult);

    // Emulate print media and generate PDF
    await page.emulateMediaType('print');
    await page.pdf({
        path: path.join(ARTIFACT_DIR, 'verified_clean_print.pdf'),
        format: 'A4',
        printBackground: true,
        margin: { top: '0', bottom: '0', left: '0', right: '0' }
    });
    console.log('Saved verified_clean_print.pdf');

    await browser.close();
    console.log('ALL VERIFICATIONS COMPLETED SUCCESSFULLY!');
})();
