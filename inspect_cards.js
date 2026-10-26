const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({ headless: 'new' });
    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });
    await page.goto('http://localhost:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.waitForSelector('#login-username', { timeout: 10000 });
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle2' }), page.click('button[type="submit"]')]);
    await page.goto('http://localhost:5169/hoa-don/tao-moi', { waitUntil: 'networkidle2' });
    await new Promise(r => setTimeout(r, 1200));

    const cardStyles = await page.evaluate(() => {
        const cards = Array.from(document.querySelectorAll('.tf-invoice-create-container .tf-card'));
        return cards.map((c, i) => {
            const cs = window.getComputedStyle(c);
            const header = c.querySelector('.tf-card-section-header');
            return {
                index: i,
                className: c.className,
                padding: cs.padding,
                width: c.offsetWidth,
                height: c.offsetHeight,
                headerPadding: header ? window.getComputedStyle(header).padding : 'no header',
                headerMarginBottom: header ? window.getComputedStyle(header).marginBottom : 'no header',
                children: Array.from(c.children).map(ch => ({
                    tag: ch.tagName,
                    class: ch.className,
                    width: ch.offsetWidth,
                    height: ch.offsetHeight,
                    margin: window.getComputedStyle(ch).margin,
                    padding: window.getComputedStyle(ch).padding
                }))
            };
        });
    });
    console.log(JSON.stringify(cardStyles, null, 2));
    await browser.close();
})();
