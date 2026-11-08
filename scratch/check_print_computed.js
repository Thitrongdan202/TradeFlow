const puppeteer = require('puppeteer');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('--- TESTING NATIVE WINDOW.PRINT / MEDIA PRINT ---');
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

    // Go to step 4
    await page.evaluate(() => {
        const items = Array.from(document.querySelectorAll('.tf-stepper-item'));
        if (items[3]) items[3].click();
    });
    await new Promise(r => setTimeout(r, 1500));

    // Emulate print media
    await page.emulateMediaType('print');

    // Inspect elements visible in print
    const printDom = await page.evaluate(() => {
        const scrollWrapper = document.querySelector('.tf-preview-viewport-scroll');
        const computedStyle = scrollWrapper ? window.getComputedStyle(scrollWrapper) : null;
        const printContract = document.getElementById('print-employment-contract');
        const printContractStyle = printContract ? window.getComputedStyle(printContract) : null;
        
        return {
            scrollWrapperBg: computedStyle?.backgroundColor,
            scrollWrapperBorder: computedStyle?.border,
            scrollWrapperMinHeight: computedStyle?.minHeight,
            scrollWrapperDisplay: computedStyle?.display,
            printContractDisplay: printContractStyle?.display
        };
    });
    console.log('Computed styles in print media:', printDom);

    await browser.close();
})();
