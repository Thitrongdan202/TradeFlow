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
    await page.evaluate(() => {
        const b = Array.from(document.querySelectorAll('button')).find(x => x.innerText.includes('Tạo HĐ lao động mới'));
        if (b) b.click();
    });
    await new Promise(r => setTimeout(r, 1500));
    const stepper = await page.evaluate(() => {
        const items = Array.from(document.querySelectorAll('.tf-stepper-item'));
        return items.map(i => ({ text: i.innerText.trim(), class: i.className }));
    });
    console.log('Stepper items:', stepper);

    // Click the 4th stepper item
    await page.evaluate(() => {
        const items = Array.from(document.querySelectorAll('.tf-stepper-item'));
        if (items[3]) items[3].click();
    });
    await new Promise(r => setTimeout(r, 1500));

    const step4Dom = await page.evaluate(() => {
        const modal = document.querySelector('.modal.show, [role="dialog"], .tf-modal-dialog');
        const ids = Array.from(document.querySelectorAll('[id]')).map(x => x.id);
        const canvases = Array.from(document.querySelectorAll('.tf-a4-preview-canvas, .tf-a4-page'));
        return {
            modalFound: !!modal,
            modalId: modal?.id,
            hasPrintEmpContract: ids.includes('print-employment-contract'),
            allCanvasClasses: canvases.map(c => c.className),
            footers: Array.from(document.querySelectorAll('.tf-a4-page-footer')).map(f => f.innerText.replace(/\n/g, ' '))
        };
    });
    console.log('Step 4 DOM info:', step4Dom);

    await browser.close();
})();
