const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox'] });
    const page = await browser.newPage();
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    await page.goto('http://127.0.0.1:5169/hop-dong', { waitUntil: 'networkidle2' });
    await new Promise(r => setTimeout(r, 1000));
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

    // Inspect clauses in Step 3
    const clauseItems = await page.evaluate(() => {
        // Switch to step 3 (Điều khoản)
        const items = Array.from(document.querySelectorAll('.tf-stepper-item'));
        if (items[2]) items[2].click();
    });
    await new Promise(r => setTimeout(r, 1000));

    // Now switch to Step 4 and measure each clause DOM node
    await page.evaluate(() => {
        const items = Array.from(document.querySelectorAll('.tf-stepper-item'));
        if (items[3]) items[3].click();
    });
    await new Promise(r => setTimeout(r, 1500));

    const clauseMeasurements = await page.evaluate(() => {
        const clauses = Array.from(document.querySelectorAll('#print-employment-contract .clause-heading'));
        return clauses.map(c => {
            const parent = c.closest('.mb-3') || c.parentElement;
            return {
                title: c.innerText.trim(),
                heightPx: parent.scrollHeight
            };
        });
    });
    console.log('Clause measurements:', clauseMeasurements);

    await browser.close();
})();
