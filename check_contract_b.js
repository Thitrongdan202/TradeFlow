const puppeteer = require('puppeteer');

(async () => {
    const b = await puppeteer.launch({ headless: 'new' });
    const p = await b.newPage();
    await p.setViewport({ width: 1440, height: 900 });
    await p.goto('http://localhost:5169/Account/Login');
    await p.type('#login-username', 'admin');
    await p.type('#login-password', 'tradecore123');
    await Promise.all([p.waitForNavigation(), p.click('button[type="submit"]')]);
    await p.goto('http://localhost:5169/hop-dong/4');
    await p.evaluate(() => {
        document.documentElement.setAttribute('data-bs-theme', 'dark');
        document.documentElement.setAttribute('data-theme', 'dark');
    });
    await new Promise(r => setTimeout(r, 1500));
    const metrics = await p.evaluate(() => {
        const el = document.querySelector('.contract-paper, .tf-document-paper');
        if (!el) return null;
        const s = window.getComputedStyle(el);
        return { bg: s.backgroundColor, color: s.color };
    });
    console.log('Category B Document Contract Paper:', metrics);
    await p.screenshot({ path: 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901/targeted_cat_b_contract_isolated_dark.png' });
    await b.close();
})();
