const puppeteer = require('puppeteer');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1200, height: 1600 });

    const pdfPath = path.join(ARTIFACT_DIR, 'test_current_print_with_headers.pdf');
    const fileUrl = 'file:///' + pdfPath.replace(/\\/g, '/');

    console.log('Loading PDF:', fileUrl);
    await page.goto(fileUrl, { waitUntil: 'networkidle0' });
    await new Promise(r => setTimeout(r, 2000));

    // Scroll to see pages
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'current_pdf_page1.png') });

    // Scroll down to see subsequent pages
    await page.evaluate(() => {
        const embed = document.querySelector('embed');
        if (embed) embed.scrollIntoView();
        window.scrollBy(0, 1000);
    });
    await new Promise(r => setTimeout(r, 1000));
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'current_pdf_page_scroll.png') });

    await browser.close();
    console.log('Done capturing PDF viewer');
})();
