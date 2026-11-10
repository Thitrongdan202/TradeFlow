const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox'] });
    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'admin');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    await new Promise(r => setTimeout(r, 1000));

    await page.evaluate(() => {
        document.documentElement.setAttribute('data-bs-theme', 'dark');
        document.documentElement.setAttribute('data-theme', 'dark');
        document.documentElement.classList.add('dark');
    });

    const bellBtn = await page.$('#tf-notification-menu-btn');
    await bellBtn.click();
    await new Promise(r => setTimeout(r, 600));

    const res = await page.evaluate(() => {
        const btn = document.querySelector('.tf-btn-approve');
        if (!btn) return 'not found';
        const cs = window.getComputedStyle(btn);
        
        const allRules = [];
        for (const sheet of document.styleSheets) {
            try {
                for (const rule of sheet.cssRules) {
                    if (rule.selectorText && btn.matches(rule.selectorText)) {
                        allRules.push({
                            selector: rule.selectorText,
                            cssText: rule.cssText
                        });
                    }
                }
            } catch(e) {}
        }
        return {
            computedBg: cs.backgroundColor,
            matchingRules: allRules.filter(r => r.cssText.includes('background') || r.cssText.includes('color'))
        };
    });
    console.log(JSON.stringify(res, null, 2));
    await browser.close();
})();
