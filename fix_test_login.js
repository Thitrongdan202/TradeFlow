const fs = require('fs');

const path = 'test_popups_and_hr_workflow.js';
let content = fs.readFileSync(path, 'utf8');

// Replace login block
const oldLogin = `        // 1. LOGIN
        console.log('1. Logging in as admin...');
        await page.goto(\`\${BASE_URL}/dang-nhap\`, { waitUntil: 'networkidle0', timeout: 30000 });
        
        await page.waitForSelector('input[name="Input.UserName"], #username, input[type="text"]');
        const userInput = await page.$('input[name="Input.UserName"]') || await page.$('#username') || await page.$('input[type="text"]');
        const passInput = await page.$('input[name="Input.Password"]') || await page.$('#password') || await page.$('input[type="password"]');
        
        await userInput.type('admin');
        await passInput.type('tradecore123');

        const submitBtn = await page.$('button[type="submit"]');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle0' }),
            submitBtn.click()
        ]);
        console.log('Logged in successfully. Current URL:', page.url());`;

const newLogin = `        // 1. LOGIN
        console.log('1. Logging in as admin...');
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        await page.waitForSelector('#login-username', { timeout: 15000 });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        console.log('Logged in successfully. Current URL:', page.url());`;

content = content.replace(oldLogin, newLogin);
fs.writeFileSync(path, content, 'utf8');
console.log('Updated test_popups_and_hr_workflow.js login');
