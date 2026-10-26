const puppeteer = require('puppeteer');

(async () => {
    const b = await puppeteer.launch({ headless: 'new' });
    const p = await b.newPage();
    await p.setViewport({ width: 1440, height: 900 });
    
    // Login
    await p.goto('http://localhost:5169/Account/Login');
    await p.type('#login-username', 'admin');
    await p.type('#login-password', 'tradecore123');
    await Promise.all([p.waitForNavigation(), p.click('button[type="submit"]')]);

    // Set theme in localStorage
    await p.evaluate(() => {
        localStorage.setItem('tradeflow_theme', 'dark');
    });

    // Navigate to invoice detail
    await p.goto('http://localhost:5169/hoa-don/chi-tiet/14', { waitUntil: 'networkidle2' });
    
    // Ensure dark mode on documentElement
    await p.evaluate(() => {
        document.documentElement.setAttribute('data-bs-theme', 'dark');
        document.documentElement.setAttribute('data-theme', 'dark');
        if (window.TradeFlow && window.TradeFlow.Theme) {
            window.TradeFlow.Theme.setTheme('dark');
        }
    });
    await new Promise(r => setTimeout(r, 1200));

    const metrics = await p.evaluate(() => {
        const sheet = document.querySelector('.invoice-print-sheet, .tf-document-paper');
        const s = sheet ? window.getComputedStyle(sheet) : null;
        const pageBg = window.getComputedStyle(document.body).backgroundColor;
        const navBg = window.getComputedStyle(document.querySelector('.tf-sidebar') || document.body).backgroundColor;
        return {
            pageBg,
            navBg,
            sheetBg: s ? s.backgroundColor : null,
            sheetColor: s ? s.color : null
        };
    });
    console.log('Document Theme Isolation Metrics:', metrics);

    await p.screenshot({
        path: 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901/targeted_cat_b_invoice_isolated_dark.png'
    });

    // Also test contract
    await p.goto('http://localhost:5169/hop-dong/4', { waitUntil: 'networkidle2' });
    await p.evaluate(() => {
        document.documentElement.setAttribute('data-bs-theme', 'dark');
        document.documentElement.setAttribute('data-theme', 'dark');
        if (window.TradeFlow && window.TradeFlow.Theme) {
            window.TradeFlow.Theme.setTheme('dark');
        }
    });
    await new Promise(r => setTimeout(r, 1200));

    await p.screenshot({
        path: 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901/targeted_cat_b_contract_isolated_dark.png'
    });

    await b.close();
})();
