const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('--- GENERATING PDF VIA PUPPETEER TO REPRODUCE EXACT PRINT PAGES ---');
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

    // Click "Tạo HĐ lao động mới"
    await page.evaluate(() => {
        const b = Array.from(document.querySelectorAll('button')).find(x => x.innerText.includes('Tạo HĐ lao động mới'));
        if (b) b.click();
    });
    await new Promise(r => setTimeout(r, 1500));

    // Click Step 4
    await page.evaluate(() => {
        const items = Array.from(document.querySelectorAll('.tf-stepper-item'));
        if (items[3]) items[3].click();
    });
    await new Promise(r => setTimeout(r, 1500));

    // Emulate print media
    await page.emulateMediaType('print');

    // Generate PDF with Chrome header/footer like the user has
    await page.pdf({
        path: path.join(ARTIFACT_DIR, 'test_current_print_with_headers.pdf'),
        format: 'A4',
        printBackground: true,
        displayHeaderFooter: true,
        headerTemplate: '<div style="font-size: 8px; width: 100%; display: flex; justify-content: space-between; padding: 0 10mm;"><span class="date"></span><span class="title"></span><span class="pageNumber"></span>/<span class="totalPages"></span></div>',
        footerTemplate: '<div style="font-size: 8px; width: 100%; display: flex; justify-content: space-between; padding: 0 10mm;"><span class="url"></span><span class="pageNumber"></span>/<span class="totalPages"></span></div>',
        margin: { top: '10mm', bottom: '10mm', left: '10mm', right: '10mm' }
    });

    console.log('Saved test_current_print_with_headers.pdf');

    // Also test without browser header/footer (margin: 0)
    await page.pdf({
        path: path.join(ARTIFACT_DIR, 'test_current_print_margin0.pdf'),
        format: 'A4',
        printBackground: true,
        displayHeaderFooter: false,
        margin: { top: '0', bottom: '0', left: '0', right: '0' }
    });

    console.log('Saved test_current_print_margin0.pdf');

    await browser.close();
})();
