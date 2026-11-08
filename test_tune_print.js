const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    let html = fs.readFileSync(path.join(ARTIFACT_DIR, 'captured_print_iframe.html'), 'utf-8');

    // 1. Move Dieu 2 from Page 1 to Page 2
    const d2Start = html.indexOf('<!-- Điều 2 -->');
    const p1FooterStart = html.indexOf('<!-- Page 1 Running Footer');

    if (d2Start !== -1 && p1FooterStart !== -1) {
        const d2Block = html.substring(d2Start, p1FooterStart);
        // remove from page 1
        html = html.substring(0, d2Start) + html.substring(p1FooterStart);

        // insert at start of page 2
        const p2Marker = 'class="bg-white tf-document-paper a4-contract-sheet tf-a4-page tf-a4-page-2';
        const p2DivClose = html.indexOf('>', html.indexOf(p2Marker)) + 1;
        html = html.substring(0, p2DivClose) + '\n' + d2Block + '\n' + html.substring(p2DivClose);
        console.log('Successfully moved Dieu 2 to Page 2');
    }

    // 2. Adjust padding and margins
    html = html.replace(/padding:\s*20mm 20mm 20mm 30mm;/g, 'padding: 15mm 20mm 15mm 30mm;');
    html = html.replace(/class="tf-doc-national-header text-center mb-4"/g, 'class="tf-doc-national-header text-center mb-1"');
    html = html.replace(/class="text-center my-4"/g, 'class="text-center my-1"');

    fs.writeFileSync(path.join(ARTIFACT_DIR, 'test_final_tuned.html'), html, 'utf-8');

    const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox', '--disable-setuid-sandbox'] });
    const page = await browser.newPage();
    await page.setViewport({ width: 1200, height: 1600 });
    await page.setContent(html, { waitUntil: 'load' });

    // Check page heights
    const metrics = await page.evaluate(() => {
        const pages = Array.from(document.querySelectorAll('.tf-a4-page'));
        return pages.map((p, idx) => ({
            index: idx + 1,
            scrollHeight: p.scrollHeight,
            offsetHeight: p.offsetHeight,
            fitsA4: p.scrollHeight <= 1123
        }));
    });
    console.log('Tuned Metrics:', JSON.stringify(metrics, null, 2));

    await page.emulateMediaType('print');
    const pdfPath = path.join(ARTIFACT_DIR, 'verified_3page_chrome_print.pdf');
    await page.pdf({
        path: pdfPath,
        format: 'A4',
        printBackground: true,
        margin: { top: 0, bottom: 0, left: 0, right: 0 }
    });
    console.log('Generated verified_3page_chrome_print.pdf');

    await browser.close();
})();
