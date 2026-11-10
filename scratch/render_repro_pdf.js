const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1200, height: 1600 });

    const pdfPath = path.join(ARTIFACT_DIR, 'repro_create_chrome_print.pdf');
    const fileUrl = 'file:///' + pdfPath.replace(/\\/g, '/');

    console.log('Loading PDF:', fileUrl);
    await page.goto(fileUrl, { waitUntil: 'networkidle0' });
    await new Promise(r => setTimeout(r, 2000));

    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'repro_create_pdf_view_p1.png') });

    // Scroll down to see page 2 & 3
    await page.evaluate(() => window.scrollBy(0, 1100));
    await new Promise(r => setTimeout(r, 1000));
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'repro_create_pdf_view_p2.png') });

    await page.evaluate(() => window.scrollBy(0, 1100));
    await new Promise(r => setTimeout(r, 1000));
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'repro_create_pdf_view_p3.png') });

    console.log('Saved PDF viewer screenshots for p1, p2, p3');
    await browser.close();
})();
