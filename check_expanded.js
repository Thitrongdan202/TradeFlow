const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({ headless: 'new', args: ['--ignore-certificate-errors'] });
    const page = await browser.newPage();
    const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
    
    try {
        await page.goto('https://localhost:7185/Account/Login', { waitUntil: 'domcontentloaded' });
        await page.type('input[name="Input.Username"]', 'admin');
        await page.type('input[name="Input.Password"]', 'tradecore123');
        
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'domcontentloaded' }),
            page.click('button[type="submit"]')
        ]);
        
        await wait(2000);
        
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('button'));
            const dmBtn = btns.find(b => b.textContent.includes('Danh mục'));
            if (dmBtn) dmBtn.click();
        });
        
        await wait(1000);
        
        const isDmExpanded = await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('button'));
            const dmBtn = btns.find(b => b.textContent.includes('Danh mục'));
            return dmBtn && dmBtn.nextElementSibling && dmBtn.nextElementSibling.classList.contains('expanded');
        });
        console.log("Is Danh mục expanded?", isDmExpanded);
        
        // Also check if Cài đặt click works
        await page.evaluate(() => {
            const links = Array.from(document.querySelectorAll('a'));
            const cBtn = links.find(b => b.textContent.includes('Cài đặt'));
            if (cBtn) cBtn.click();
        });
        await wait(2000);
        console.log("Current URL after clicking Cài đặt:", page.url());
        
    } finally {
        await browser.close();
    }
})();
