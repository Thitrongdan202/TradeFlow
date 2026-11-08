const puppeteer = require('puppeteer');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox', '--disable-setuid-sandbox'] });
    const page = await browser.newPage();
    await page.setViewport({ width: 1200, height: 1600 });
    const htmlPath = path.join(ARTIFACT_DIR, 'test_tuned_iframe.html');
    await page.goto('file:///' + htmlPath.replace(/\\/g, '/'), { waitUntil: 'load' });

    const childHeights = await page.evaluate(() => {
        const p1 = document.querySelector('.tf-a4-page-1');
        const children = Array.from(p1.children);
        return {
            pageComputed: {
                minHeight: window.getComputedStyle(p1).minHeight,
                height: window.getComputedStyle(p1).height,
                padding: window.getComputedStyle(p1).padding,
            },
            children: children.map(c => ({
                tag: c.tagName,
                className: c.className,
                textSnippet: c.innerText.substring(0, 35),
                offsetHeight: c.offsetHeight,
                marginTop: window.getComputedStyle(c).marginTop,
                marginBottom: window.getComputedStyle(c).marginBottom,
            }))
        };
    });

    console.log(JSON.stringify(childHeights, null, 2));
    await browser.close();
})();
