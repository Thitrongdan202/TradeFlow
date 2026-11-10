const puppeteer = require('puppeteer');

async function loginUser(browser, username, password) {
    const page = await browser.newPage();
    try {
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        const userInput = await page.$('input[name="Input.Username"]');
        if (userInput) {
            await userInput.click({ clickCount: 3 });
            await userInput.type(username);
        }
        const passInput = await page.$('input[name="Input.Password"]');
        if (passInput) {
            await passInput.click({ clickCount: 3 });
            await passInput.type(password);
        }
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        const url = page.url();
        const success = !url.includes('/Account/Login');
        console.log(`User [${username}] login success: ${success} (URL: ${url})`);
        return success;
    } catch (e) {
        console.error(`User [${username}] error:`, e.message);
        return false;
    } finally {
        await page.close();
    }
}

async function run() {
    const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox'] });
    const users = ['admin', 'quanly01', 'ketoan01', 'kho01', 'kinhdoanh01'];
    let allOk = true;
    for (const u of users) {
        const ok = await loginUser(browser, u, 'tradecore123');
        if (!ok) allOk = false;
    }
    console.log(`All legacy users authenticated cleanly: ${allOk}`);
    await browser.close();
}

run();
