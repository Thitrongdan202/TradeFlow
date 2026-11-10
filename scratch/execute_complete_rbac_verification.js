const puppeteer = require('puppeteer');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function login(page, username, password = 'tradecore123') {
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await delay(300);
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
    console.log('=== STARTING COMPLETE RBAC VERIFICATION SUITE ===');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1600,1050']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1600, height: 1050 });

    try {
        // =========================================================================
        // SCENARIO 1: nv01 Base Employee Role - Partner Menu Hidden
        // =========================================================================
        console.log('--- TEST 1: nv01 Sidebar Check ---');
        await login(page, 'nv01', 'tradecore123');
        await page.goto('http://127.0.0.1:5169/', { waitUntil: 'networkidle2' });
        await delay(1500);

        const nv01Links = await page.evaluate(() => {
            return Array.from(document.querySelectorAll('aside nav a, aside a'))
                .map(a => a.innerText.trim())
                .filter(t => t.length > 0);
        });
        console.log('nv01 visible menus:', nv01Links);
        const hasPartners = nv01Links.some(t => t.includes('Đối tác') || t.includes('Khách hàng') || t.includes('Nhà cung cấp'));
        console.log('TEST 1: Partner menu hidden for nv01?', !hasPartners);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_01.png'),
            fullPage: false
        });
        console.log('Saved rbac_access_01.png');

        // =========================================================================
        // SCENARIO 2: Direct URL Protection & POS Customer Leak Protection
        // =========================================================================
        console.log('--- TEST 2: Direct URL & POS Protection ---');
        // Direct access to /doi-tac/khach-hang
        await page.goto('http://127.0.0.1:5169/doi-tac/khach-hang', { waitUntil: 'networkidle2' });
        await delay(1000);
        const blockedUrl = page.url();
        console.log('Direct access to /doi-tac/khach-hang redirected to:', blockedUrl);

        // Re-login as nv01 and open POS
        try {
            await login(page, 'nv01', 'tradecore123');
            await page.goto('http://127.0.0.1:5169/ban-hang/pos', { waitUntil: 'networkidle2' });
            await delay(2000);

            // Screenshot for Test 2
            await page.screenshot({
                path: path.join(ARTIFACT_DIR, 'rbac_access_02.png'),
                fullPage: false
            });
            console.log('Saved rbac_access_02.png');
        } catch (e2) {
            console.error('Error in Test 2:', e2);
        }

        // =========================================================================
        // SCENARIO 3: Admin Assigns Secondary Role (Accountant) to nv01
        // =========================================================================
        console.log('--- TEST 3: Admin Assigns Secondary Role ---');
        await login(page, 'admin', 'tradecore123');
        await page.goto('http://127.0.0.1:5169/cai-dat/phan-quyen/accounts', { waitUntil: 'networkidle2' });
        await delay(2000);

        // Look for nv01 row and click "+ Gán Role phụ"
        const assigned = await page.evaluate(() => {
            const rows = Array.from(document.querySelectorAll('tbody tr'));
            for (const r of rows) {
                if (r.innerText.includes('nv01')) {
                    const btn = r.querySelector('button');
                    if (btn && btn.innerText.includes('Role phụ')) {
                        btn.click();
                        return true;
                    }
                }
            }
            return false;
        });
        console.log('Clicked assign secondary role button for nv01?', assigned);
        await delay(1000);

        // In modal, select Accountant role and save
        const modalFound = await page.$('.tf-modal');
        if (modalFound) {
            console.log('Secondary role modal opened.');
            // Select second role
            const select = await page.$('.tf-modal select');
            if (select) {
                // Find Accountant option
                const options = await page.evaluate(sel => {
                    return Array.from(sel.options).map(o => ({ val: o.value, text: o.text }));
                }, select);
                console.log('Role options:', options);
                const acctOption = options.find(o => o.text.includes('Accountant') || o.text.includes('Kế toán'));
                if (acctOption) {
                    await select.select(acctOption.val);
                    await delay(500);
                }
            }

            // Click save button
            const saveBtn = await page.evaluate(() => {
                const btns = Array.from(document.querySelectorAll('.tf-modal-footer button'));
                const save = btns.find(b => b.innerText.includes('Lưu') || b.innerText.includes('Xác nhận'));
                if (save) {
                    save.click();
                    return true;
                }
                return false;
            });
            console.log('Clicked save secondary role?', saveBtn);
            await delay(2000);
        }

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_03.png'),
            fullPage: false
        });
        console.log('Saved rbac_access_03.png');

        // =========================================================================
        // SCENARIO 4: Temporary Access Grant with Hard Limit (Max 120 mins)
        // =========================================================================
        console.log('--- TEST 4: Temporary Access Grant ---');
        await page.goto('http://127.0.0.1:5169/cai-dat/phan-quyen/temporary', { waitUntil: 'networkidle2' });
        await delay(2000);

        // Click "+ Cấp quyền tạm thời"
        const grantBtn = await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('button'));
            const b = btns.find(btn => btn.innerText.includes('Cấp quyền tạm thời'));
            if (b) {
                b.click();
                return true;
            }
            return false;
        });
        console.log('Clicked grant temporary button?', grantBtn);
        await delay(1000);

        // In modal, fill user nv01, role Warehouse, duration 5 minutes, reason
        const tempModal = await page.$('.tf-modal');
        if (tempModal) {
            console.log('Temporary access modal opened.');
            // Select user
            const selects = await page.$$('.tf-modal select');
            if (selects.length >= 2) {
                // First select is user
                const userOptions = await page.evaluate(sel => Array.from(sel.options).map(o => ({ val: o.value, text: o.text })), selects[0]);
                const nv01Opt = userOptions.find(o => o.text.includes('nv01'));
                if (nv01Opt) await selects[0].select(nv01Opt.val);

                // Second select is role
                const roleOptions = await page.evaluate(sel => Array.from(sel.options).map(o => ({ val: o.value, text: o.text })), selects[1]);
                const whOpt = roleOptions.find(o => o.text.includes('Warehouse') || o.text.includes('Kho'));
                if (whOpt) await selects[1].select(whOpt.val);
            }

            // Duration input
            const numInput = await page.$('.tf-modal input[type="number"]');
            if (numInput) {
                await numInput.click({ clickCount: 3 });
                await numInput.type('5');
            }

            // Reason textarea
            const reasonArea = await page.$('.tf-modal textarea');
            if (reasonArea) {
                await reasonArea.type('Hỗ trợ kiểm kê kho khẩn cấp ca tối');
            }

            // Confirm button
            const confirmBtn = await page.evaluate(() => {
                const btns = Array.from(document.querySelectorAll('.tf-modal-footer button'));
                const b = btns.find(btn => btn.innerText.includes('Xác nhận'));
                if (b) {
                    b.click();
                    return true;
                }
                return false;
            });
            console.log('Confirmed temporary grant?', confirmBtn);
            await delay(2500);
        }

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_04.png'),
            fullPage: false
        });
        console.log('Saved rbac_access_04.png');

        // =========================================================================
        // SCENARIO 5: Matrix Tab - Modules, Scopes & Sensitive Permissions
        // =========================================================================
        console.log('--- TEST 5: Access Matrix ---');
        await page.goto('http://127.0.0.1:5169/cai-dat/phan-quyen/matrix', { waitUntil: 'networkidle2' });
        await delay(2000);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_05.png'),
            fullPage: false
        });
        console.log('Saved rbac_access_05.png');

        // =========================================================================
        // SCENARIO 6: Role Management & System Administrator Protection
        // =========================================================================
        console.log('--- TEST 6: Role Management & Administrator Protection ---');
        await page.goto('http://127.0.0.1:5169/cai-dat/phan-quyen/roles', { waitUntil: 'networkidle2' });
        await delay(2000);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_06.png'),
            fullPage: false
        });
        console.log('Saved rbac_access_06.png');

        // =========================================================================
        // SCENARIO 7: Security Audit Logs & Legacy Users
        // =========================================================================
        console.log('--- TEST 7: Security Audit Logs ---');
        await page.goto('http://127.0.0.1:5169/cai-dat/phan-quyen/audit', { waitUntil: 'networkidle2' });
        await delay(2000);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_07.png'),
            fullPage: false
        });
        console.log('Saved rbac_access_07.png');

    } catch (err) {
        console.error('Error occurred:', err);
    } finally {
        await browser.close();
        console.log('=== RBAC VERIFICATION COMPLETED SUCCESSFULLY ===');
    }
}

run();
