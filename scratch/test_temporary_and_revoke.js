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
        console.log('--- TEST 4: Grant Temporary Access ---');
        await login(page, 'admin', 'tradecore123');
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
        await delay(1200);

        // Select user and role in modal
        const userVal = await page.evaluate(() => {
            const sel = document.querySelectorAll('.tf-modal select')[0];
            for (let i = 1; i < sel.options.length; i++) {
                if (!sel.options[i].text.includes('[Đã có Role 2:')) {
                    sel.value = sel.options[i].value;
                    sel.dispatchEvent(new Event('change', { bubbles: true }));
                    return sel.options[i].text;
                }
            }
            sel.value = sel.options[1].value;
            sel.dispatchEvent(new Event('change', { bubbles: true }));
            return sel.options[1].text;
        });
        console.log('Selected user for temporary role:', userVal);
        await delay(500);

        const roleVal = await page.evaluate(() => {
            const sel = document.querySelectorAll('.tf-modal select')[1];
            sel.value = sel.options[1].value;
            sel.dispatchEvent(new Event('change', { bubbles: true }));
            return sel.options[1].text;
        });
        console.log('Selected role for temporary role:', roleVal);
        await delay(500);

        // Click preset button "15 phút"
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-modal button'));
            const b15 = btns.find(b => b.innerText.includes('15 phút'));
            if (b15) b15.click();
        });
        await delay(500);

        // Fill reason
        await page.evaluate(() => {
            const ta = document.querySelector('.tf-modal textarea');
            ta.value = 'Hỗ trợ kiểm kê kho khẩn cấp ca tối';
            ta.dispatchEvent(new Event('input', { bubbles: true }));
            ta.dispatchEvent(new Event('change', { bubbles: true }));
        });
        await delay(500);

        // Click submit
        const submitted = await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-modal-footer button'));
            const b = btns.find(btn => btn.innerText.includes('Xác nhận cấp quyền'));
            if (b) {
                b.click();
                return true;
            }
            return false;
        });
        console.log('Submitted temporary grant?', submitted);
        await delay(3000);

        // Screenshot for Test 4: Active temporary role with countdown
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_04.png'),
            fullPage: false
        });
        console.log('Saved rbac_access_04.png');

        // =========================================================================
        // TEST 5: Immediate Revocation
        // =========================================================================
        console.log('--- TEST 5: Immediate Revocation ---');
        // Click "Thu hồi ngay" on the row
        const revoked = await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('tbody button'));
            const b = btns.find(btn => btn.innerText.includes('Thu hồi'));
            if (b) {
                b.click();
                return true;
            }
            return false;
        });
        console.log('Clicked "Thu hồi" button?', revoked);
        await delay(1200);

        // Fill revocation reason modal if opened
        const revokeModal = await page.$('.tf-modal');
        if (revokeModal) {
            console.log('Revocation modal opened.');
            const ta = await page.$('.tf-modal textarea');
            if (ta) {
                await ta.type('Công việc kiểm kê kết thúc sớm trước thời hạn');
                await page.evaluate(() => {
                    const t = document.querySelector('.tf-modal textarea');
                    t.dispatchEvent(new Event('input', { bubbles: true }));
                    t.dispatchEvent(new Event('change', { bubbles: true }));
                });
                await delay(400);
            }
            const confirmRevoke = await page.evaluate(() => {
                const btns = Array.from(document.querySelectorAll('.tf-modal-footer button'));
                const b = btns.find(btn => btn.innerText.includes('Xác nhận thu hồi') || btn.innerText.includes('Thu hồi'));
                if (b) {
                    b.click();
                    return true;
                }
                return false;
            });
            console.log('Confirmed revocation?', confirmRevoke);
            await delay(2500);
        }

        // Take screenshot of Temporary Access tab showing Revoked status
        await page.screenshot({
            path: path.join(ARTIFACT_DIR, 'rbac_access_05.png'),
            fullPage: false
        });
        console.log('Saved rbac_access_05.png');

    } catch (err) {
        console.error('Error:', err);
    } finally {
        await browser.close();
    }
}

run();
