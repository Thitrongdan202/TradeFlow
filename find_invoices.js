const puppeteer = require('puppeteer');

(async () => {
    const b = await puppeteer.launch({ headless: 'new' });
    const p = await b.newPage();
    await p.goto('http://localhost:5169/Account/Login');
    await p.type('#login-username', 'admin');
    await p.type('#login-password', 'tradecore123');
    await Promise.all([p.waitForNavigation(), p.click('button[type="submit"]')]);
    await p.goto('http://localhost:5169/hoa-don');
    await new Promise(r => setTimeout(r, 2000));
    const links = await p.evaluate(() => Array.from(document.querySelectorAll('a[href*="/hoa-don/chi-tiet/"]')).map(a => a.getAttribute('href')));
    console.log('Invoice links:', links);
    await p.goto('http://localhost:5169/hop-dong');
    await new Promise(r => setTimeout(r, 2000));
    const contractLinks = await p.evaluate(() => Array.from(document.querySelectorAll('a[href*="/hop-dong/"]')).map(a => a.getAttribute('href')));
    console.log('Contract links:', contractLinks);
    await b.close();
})();
