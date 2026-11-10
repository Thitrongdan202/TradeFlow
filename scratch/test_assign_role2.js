const puppeteer = require('puppeteer');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function login(page, username, password = 'tradecore123') {
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
    await delay(1200);
}

async function run() {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1600,1050']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1600, height: 1050 });

    try {
        console.log('--- TEST 3: Admin Assigns Secondary Role (Accountant) to nv01 ---');
        await login(page, 'admin', 'tradecore123');
        await page.goto('http://127.0.0.1:5169/cai-dat/phan-quyen/accounts', { waitUntil: 'networkidle2' });
        await delay(2000);

        // Click "+ Gán Role 2" for nv01
        const clicked = await page.evaluate(() => {
            const rows = Array.from(document.querySelectorAll('tbody tr'));
            for (const r of rows) {
                if (r.innerText.includes('nv01')) {
                    const btns = Array.from(r.querySelectorAll('button'));
                    const b = btns.find(btn => btn.innerText.includes('Role 2') || btn.innerText.includes('Gán'));
                    if (b) {
                        b.click();
                        return true;
                    }
                }
            }
            return false;
        });
        console.log('Clicked "+ Gán Role 2" button for nv01?', clicked);
        await delay(1200);

        // Select Accountant in modal
        const modalFound = await page.$('.tf-modal');
        if (modalFound) {
            console.log('Modal opened!');
            const select = await page.$('.tf-modal select');
            if (select) {
                const options = await page.evaluate(sel => {
                    return Array.from(sel.options).map(o => ({ val: o.value, text: o.text }));
                }, select);
                console.log('Options found:', options);
                const acctOption = options.find(o => o.text.includes('Accountant') || o.text.includes('Kế toán'));
                if (acctOption) {
                    await page.evaluate((val) => {
                        const sel = document.querySelector('.tf-modal select');
                        sel.value = val;
                        sel.dispatchEvent(new Event('change', { bubbles: true }));
                    }, acctOption.val);
                    await delay(600);
                }
            }

            // Click submit button "Gán vai trò"
            const saved = await page.evaluate(() => {
                const btns = Array.from(document.querySelectorAll('.tf-modal-footer button'));
                const b = btns.find(btn => btn.innerText.includes('Gán vai trò') || btn.innerText.includes('Lưu') || btn.innerText.includes('Xác nhận'));
                if (b) {
                    b.click();
                    return true;
                }
                return false;
            });
            console.log('Clicked save button?', saved);
            await delay(3000);
        }

        // Take screenshot of Accounts tab showing nv01 with 2 roles (Employee + Accountant)
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_03.png'),
            fullPage: false
        });
        console.log('Updated rbac_access_03.png');

        // Now test nv01 login to verify Partner menu is now visible
        console.log('Verifying nv01 now sees Partner menu...');
        await login(page, 'nv01', 'tradecore123');
        await page.goto('http://127.0.0.1:5169/', { waitUntil: 'networkidle2' });
        await delay(2000);

        const nv01NewLinks = await page.evaluate(() => {
            return Array.from(document.querySelectorAll('aside nav a, aside a'))
                .map(a => a.innerText.trim())
                .filter(t => t.length > 0);
        });
        console.log('nv01 links after second role assigned:', nv01NewLinks);
        const hasPartnersNow = nv01NewLinks.some(t => t.includes('Đối tác') || t.includes('Khách hàng'));
        console.log('Partner menu visible now for nv01?', hasPartnersNow);

        // Access /doi-tac/khach-hang directly
        await page.goto('http://127.0.0.1:5169/doi-tac/khach-hang', { waitUntil: 'networkidle2' });
        await delay(2000);
        console.log('nv01 access /doi-tac/khach-hang URL:', page.url());

    } catch (err) {
        console.error('Error in test:', err);
    } finally {
        await browser.close();
    }
}

run();
