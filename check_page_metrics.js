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
    // 210mm x 297mm at 96 DPI is 794 x 1123 px
    await page.setViewport({ width: 1200, height: 1600 });

    const htmlPath = path.join(ARTIFACT_DIR, 'captured_print_iframe.html');
    const fileUrl = 'file:///' + htmlPath.replace(/\\/g, '/');
    await page.goto(fileUrl, { waitUntil: 'load' });

    // Inspect the heights and margins of each page
    const pageMetrics = await page.evaluate(() => {
        const pages = Array.from(document.querySelectorAll('.tf-a4-page'));
        return pages.map((p, idx) => ({
            index: idx + 1,
            className: p.className,
            scrollHeight: p.scrollHeight,
            offsetHeight: p.offsetHeight,
            clientHeight: p.clientHeight,
            stylePadding: p.style.padding,
            textLength: p.innerText.length,
            footerText: p.querySelector('.tf-a4-page-footer') ? p.querySelector('.tf-a4-page-footer').innerText : 'no footer'
        }));
    });

    console.log('Page Metrics:', JSON.stringify(pageMetrics, null, 2));

    await browser.close();
})();
