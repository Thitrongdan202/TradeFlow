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

    // Detail Modal
    await page.evaluate(() => {
        const viewBtns = Array.from(document.querySelectorAll('button[title*="chi tiết"], button.tf-action-btn'));
        if (viewBtns.length > 0) viewBtns[0].click();
    });
    await new Promise(r => setTimeout(r, 1500));

    const detailHeights = await page.evaluate(() => {
        const pages = Array.from(document.querySelectorAll('#print-employment-contract .tf-a4-page'));
        return pages.map((p, i) => {
            // Measure natural content height
            const clone = p.cloneNode(true);
            clone.style.height = 'auto';
            clone.style.minHeight = '0';
            clone.style.maxHeight = 'none';
            document.body.appendChild(clone);
            const contentHeight = clone.scrollHeight;
            clone.remove();
            return {
                page: i + 1,
                contentHeightPx: contentHeight,
                a4LimitPx: 297 * 3.779527, // 1122.5px
                headroomPx: (297 * 3.779527) - contentHeight
            };
        });
    });
    console.log('Detail Modal Natural Content Heights:', detailHeights);

    // Close detail modal
    await page.evaluate(() => {
        const btn = document.querySelector('.modal.show .btn-close, .modal.show .modal-footer button');
        if (btn) btn.click();
    });
    await new Promise(r => setTimeout(r, 1000));

    // Open Form Modal Step 4
    await page.evaluate(() => {
        const b = Array.from(document.querySelectorAll('button')).find(x => x.innerText.includes('Tạo HĐ lao động mới'));
        if (b) b.click();
    });
    await new Promise(r => setTimeout(r, 1500));

    await page.evaluate(() => {
        const items = Array.from(document.querySelectorAll('.tf-stepper-item'));
        if (items[3]) items[3].click();
    });
    await new Promise(r => setTimeout(r, 1500));

    const formHeights = await page.evaluate(() => {
        const pages = Array.from(document.querySelectorAll('#print-employment-contract .tf-a4-page'));
        return pages.map((p, i) => {
            const clone = p.cloneNode(true);
            clone.style.height = 'auto';
            clone.style.minHeight = '0';
            clone.style.maxHeight = 'none';
            document.body.appendChild(clone);
            const contentHeight = clone.scrollHeight;
            clone.remove();
            return {
                page: i + 1,
                contentHeightPx: contentHeight,
                a4LimitPx: 297 * 3.779527,
                headroomPx: (297 * 3.779527) - contentHeight
            };
        });
    });
    console.log('Form Modal Step 4 Natural Content Heights:', formHeights);

    await browser.close();
})();
