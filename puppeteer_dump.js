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
    
    console.log("Navigating to Login...");
    await page.goto('https://localhost:7185/Account/Login', { waitUntil: 'networkidle0' });

    const html = await page.content();
    fs.writeFileSync('login_page.html', html);
    console.log("Saved login_page.html");

    await browser.close();
    console.log("Done");
})();
