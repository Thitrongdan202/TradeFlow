const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    await page.setContent(`
        <html>
        <head>
            <style>
                .test-path {
                    transition: d 0.35s cubic-bezier(0.16, 1, 0.3, 1);
                }
            </style>
        </head>
        <body>
            <svg viewBox="0 0 800 240" width="800" height="240">
                <path id="p" class="test-path" d="M 55 180 C 400 180, 400 80, 785 80" stroke="blue" fill="none" />
            </svg>
        </body>
        </html>
    `);

    const initialD = await page.$eval('#p', el => el.getAttribute('d'));
    console.log('Initial D:', initialD);

    // Change D to same command structure
    await page.$eval('#p', el => el.setAttribute('d', 'M 55 50 C 400 50, 400 150, 785 150'));

    // Check computed value at 100ms
    await new Promise(r => setTimeout(r, 100));
    const midD = await page.$eval('#p', el => window.getComputedStyle(el).d);
    console.log('Mid computed d:', midD);

    // Check at 400ms
    await new Promise(r => setTimeout(r, 300));
    const endD = await page.$eval('#p', el => window.getComputedStyle(el).d);
    console.log('End computed d:', endD);

    await browser.close();
})();
