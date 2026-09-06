const puppeteer = require('puppeteer');

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

    console.log("Filling form...");
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    
    console.log("Clicking submit...");
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle0', timeout: 5000 }).catch(e => console.log("Navigation timeout")),
        page.click('button[type="submit"]')
    ]);

    console.log("Current URL:", page.url());
    
    // Wait for the websocket to settle
    await new Promise(r => setTimeout(r, 2000));

    // Try to open the UserMenu dropdown
    console.log("Clicking user menu...");
    await page.click('#tf-user-menu-btn');
    
    await new Promise(r => setTimeout(r, 1000));

    // Check if error UI exists
    const hasErrorUi = await page.evaluate(() => !!document.getElementById('blazor-error-ui') && window.getComputedStyle(document.getElementById('blazor-error-ui')).display !== 'none');
    console.log("Has blazor-error-ui:", hasErrorUi);
    
    if (hasErrorUi) {
        const errorText = await page.evaluate(() => document.getElementById('blazor-error-ui').innerText);
        console.log("Error text:", errorText);
    }
    
    await browser.close();
    console.log("Done");
})();
