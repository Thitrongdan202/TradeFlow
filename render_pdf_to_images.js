const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1200, height: 1600 });

    const pdfPath = path.join(ARTIFACT_DIR, 'verified_backend_direct_export.pdf');
    const fileUrl = 'file:///' + pdfPath.replace(/\\/g, '/');

    console.log('Loading PDF via Chrome PDF viewer:', fileUrl);
    await page.goto(fileUrl, { waitUntil: 'networkidle0' });
    await new Promise(r => setTimeout(r, 2000));

    const shotPath = path.join(ARTIFACT_DIR, 'verified_backend_pdf_viewer.png');
    await page.screenshot({ path: shotPath, fullPage: false });
    console.log('Saved PDF viewer screenshot to:', shotPath);

    await browser.close();
})();
