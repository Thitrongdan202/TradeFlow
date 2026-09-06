const puppeteer = require('puppeteer');
const fs = require('fs');

(async () => {
    console.log("Launching browser...");
    const browser = await puppeteer.launch({ 
        headless: true, 
        ignoreHTTPSErrors: true,
        args: ['--ignore-certificate-errors'] 
    });
    const page = await browser.newPage();
    
    page.on('console', msg => console.log('PAGE LOG:', msg.text()));
    page.on('pageerror', error => console.log('PAGE ERROR:', error.message));

    console.log("Navigating to Login...");
    await page.goto('https://localhost:7185/Account/Login', { waitUntil: 'networkidle0' });

    // Let's dump the HTML here
    const html = await page.content();
    fs.writeFileSync('dom_dump.html', html);

    try {
        console.log("Filling form...");
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        
        console.log("Clicking submit...");
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle0', timeout: 5000 }).catch(e => console.log("Navigation timeout")),
            page.click('button[type="submit"]')
        ]);

        console.log("Current URL:", page.url());
    } catch (e) {
        console.log("Error:", e.message);
    }
    
    await browser.close();
    console.log("Done");
})();
