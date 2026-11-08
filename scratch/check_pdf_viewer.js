const puppeteer = require('puppeteer');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox'] });
    const page = await browser.newPage();
    await page.setViewport({ width: 1200, height: 1600 });

    const pdfPath = path.join(ARTIFACT_DIR, 'verified_clean_print.pdf');
    const fileUrl = 'file:///' + pdfPath.replace(/\\/g, '/');

    console.log('Loading clean print PDF in viewer:', fileUrl);
    await page.goto(fileUrl, { waitUntil: 'networkidle0' });
    await new Promise(r => setTimeout(r, 2000));

    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'verified_clean_print_viewer.png') });
    console.log('Saved verified_clean_print_viewer.png');

    await browser.close();
})();
