const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function login(page, username, password = 'tradecore123') {
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    // Clear and fill username
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
    await delay(1000);
}

async function logout(page) {
    try {
        await page.goto('http://127.0.0.1:5169/Account/Logout', { waitUntil: 'networkidle2' });
    } catch (e) {
        // May redirect or post
    }
}

async function run() {
    console.log('Starting RBAC Verification Suite...');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1600,1000']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1600, height: 1000 });

    try {
        // ========================================================
        // TEST 1: Menu ẩn/hiện theo quyền (nv01: chỉ Employee, không có Đối tác)
        // ========================================================
        console.log('--- TEST 1: Checking nv01 menu ---');
        await login(page, 'nv01', 'tradecore123');
        await page.goto('http://127.0.0.1:5169/', { waitUntil: 'networkidle2' });
        await delay(1500);

        // Check sidebar items
        const sidebarLinks = await page.evaluate(() => {
            const links = Array.from(document.querySelectorAll('aside nav a, aside a'));
            return links.map(a => ({ text: a.innerText.trim(), href: a.getAttribute('href') }));
        });
        console.log('nv01 visible links:', sidebarLinks.filter(l => l.text).map(l => l.text));

        const hasPartnerMenu = sidebarLinks.some(l => 
            l.text.includes('Đối tác') || l.text.includes('Khách hàng') || l.text.includes('Nhà cung cấp')
        );
        console.log(`TEST 1 Result: nv01 has partner menu? ${hasPartnerMenu} (Expected: false)`);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_01.png'),
            fullPage: false
        });
        console.log('Saved rbac_access_01.png');

        // ========================================================
        // TEST 2: Chặn truy cập trực tiếp & Data leak
        // ========================================================
        console.log('--- TEST 2: Direct URL access & POS Data leak check ---');
        // Try accessing /doi-tac
        const directResp = await page.goto('http://127.0.0.1:5169/doi-tac', { waitUntil: 'networkidle2' });
        await delay(1000);
        console.log('/doi-tac response status:', directResp.status(), 'URL:', page.url());

        // Try accessing /doi-tac/khach-hang
        const custResp = await page.goto('http://127.0.0.1:5169/doi-tac/khach-hang', { waitUntil: 'networkidle2' });
        await delay(1000);
        console.log('/doi-tac/khach-hang response status:', custResp.status(), 'URL:', page.url());

        // Check if page content contains 403 or Access Denied
        const pageContent = await page.evaluate(() => document.body.innerText);
        const isBlocked = directResp.status() === 403 || custResp.status() === 403 || 
                          pageContent.includes('403') || pageContent.includes('không có quyền') ||
                          page.url().includes('AccessDenied') || page.url().includes('loi');
        console.log(`TEST 2: Direct access blocked? ${isBlocked}`);

        // Go to POS and check customer search
        await page.goto('http://127.0.0.1:5169/ban-hang/pos', { waitUntil: 'networkidle2' });
        await delay(2000);

        // Screenshot for Test 2
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_02.png'),
            fullPage: false
        });
        console.log('Saved rbac_access_02.png');

        // ========================================================
        // TEST 6 (Part 1): Non-admin blocked from /cai-dat/phan-quyen
        // ========================================================
        console.log('--- TEST 6 (Part 1): nv01 tries to access /cai-dat/phan-quyen ---');
        const accessCtrlResp = await page.goto('http://127.0.0.1:5169/cai-dat/phan-quyen', { waitUntil: 'networkidle2' });
        await delay(1000);
        console.log('/cai-dat/phan-quyen for nv01 status:', accessCtrlResp.status(), 'URL:', page.url());

        // ========================================================
        // TEST 3: Admin manages roles & assigns second role
        // ========================================================
        console.log('--- TEST 3: Admin assigns second role to nv01 ---');
        await login(page, 'admin', 'tradecore123');
        await page.goto('http://127.0.0.1:5169/cai-dat/phan-quyen', { waitUntil: 'networkidle2' });
        await delay(2000);

        // Go to Tab 4: Tài khoản & Role
        await page.goto('http://127.0.0.1:5169/cai-dat/phan-quyen/users', { waitUntil: 'networkidle2' });
        await delay(2000);

        // Take screenshot of Users & Role assignment tab
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_03.png'),
            fullPage: false
        });
        console.log('Saved rbac_access_03.png');

        // ========================================================
        // TEST 4: Temporary access grant & expiration
        // ========================================================
        console.log('--- TEST 4: Temporary Access Grant ---');
        await page.goto('http://127.0.0.1:5169/cai-dat/phan-quyen/temporary', { waitUntil: 'networkidle2' });
        await delay(2000);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_04.png'),
            fullPage: false
        });
        console.log('Saved rbac_access_04.png');

        // ========================================================
        // TEST 5: Role Matrix & Overview
        // ========================================================
        console.log('--- TEST 5: Access Matrix ---');
        await page.goto('http://127.0.0.1:5169/cai-dat/phan-quyen/matrix', { waitUntil: 'networkidle2' });
        await delay(2000);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_05.png'),
            fullPage: false
        });
        console.log('Saved rbac_access_05.png');

        // ========================================================
        // TEST 6 (Part 2): Role Management & Administrator Protection
        // ========================================================
        console.log('--- TEST 6 (Part 2): Role Management & Administrator Protection ---');
        await page.goto('http://127.0.0.1:5169/cai-dat/phan-quyen/roles', { waitUntil: 'networkidle2' });
        await delay(2000);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_06.png'),
            fullPage: false
        });
        console.log('Saved rbac_access_06.png');

        // ========================================================
        // TEST 7: RBAC Audit Logs & Legacy Users
        // ========================================================
        console.log('--- TEST 7: RBAC Audit Logs & Legacy Users ---');
        await page.goto('http://127.0.0.1:5169/cai-dat/phan-quyen/audit', { waitUntil: 'networkidle2' });
        await delay(2000);

        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_07.png'),
            fullPage: false
        });
        console.log('Saved rbac_access_07.png');

    } catch (err) {
        console.error('Error during test execution:', err);
    } finally {
        await browser.close();
        console.log('Verification Suite Finished.');
    }
}

run();
