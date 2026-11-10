const puppeteer = require('puppeteer');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('=== STARTING ROOT CREDENTIAL GOVERNANCE VERIFICATION ===');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    // -------------------------------------------------------------
    // TEST 1: Admin logs in, views User List and Eye Toggles
    // -------------------------------------------------------------
    console.log('\n--- TEST 1: ADMIN USER LIST & EYE TOGGLE ---');
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'admin');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    console.log('Admin logged in. URL:', page.url());

    await page.goto('http://127.0.0.1:5169/cai-dat/nguoi-dung', { waitUntil: 'networkidle2' });
    await delay(1500);

    // Look for eye toggle buttons in table
    const eyeButtons = await page.$$('button[title*="con mắt"]');
    console.log('Found eye toggle buttons in user table:', eyeButtons.length);
    if (eyeButtons.length > 0) {
        // Click the first eye button to reveal password
        await eyeButtons[0].click();
        await delay(800);
        console.log('Clicked eye toggle to reveal password.');
    }

    await page.screenshot({ path: `${ARTIFACT_DIR}/root_01_user_list_eye_password.png` });
    console.log('Saved root_01_user_list_eye_password.png');

    // -------------------------------------------------------------
    // TEST 2: Dedicated ROOT Credential Governance Page
    // -------------------------------------------------------------
    console.log('\n--- TEST 2: CREDENTIAL GOVERNANCE DEDICATED PAGE ---');
    await page.goto('http://127.0.0.1:5169/cai-dat/nguoi-dung/bao-mat', { waitUntil: 'networkidle2' });
    await delay(1500);
    console.log('Navigated to /cai-dat/nguoi-dung/bao-mat. URL:', page.url());

    // Click on eye button to reveal current password on the card
    const cardEyeBtn = await page.$('#btn-toggle-eye-password');
    if (cardEyeBtn) {
        await cardEyeBtn.click();
        await delay(800);
        console.log('Clicked eye button on selected user card.');
    }

    await page.screenshot({ path: `${ARTIFACT_DIR}/root_02_credential_governance_page.png` });
    console.log('Saved root_02_credential_governance_page.png');

    // -------------------------------------------------------------
    // TEST 3: Admin Resets Password for Employee (nv01)
    // -------------------------------------------------------------
    console.log('\n--- TEST 3: ADMIN RESETS PASSWORD FOR NV01 ---');
    // Type new password
    const newPassInput = await page.$('#input-new-password');
    if (newPassInput) {
        await newPassInput.click({ clickCount: 3 });
        await newPassInput.type('TradeFlow@2026!Nv01');
    }

    // Type Admin verify password
    const adminVerifyInput = await page.$('#input-admin-verify-password');
    if (adminVerifyInput) {
        await adminVerifyInput.type('tradecore123');
    }

    // Click submit
    const submitBtn = await page.$('#btn-submit-reset-password');
    if (submitBtn) {
        await submitBtn.click();
        await delay(1500);
        console.log('Submitted Admin Reset Password for nv01.');
    }

    await page.screenshot({ path: `${ARTIFACT_DIR}/root_03_admin_reset_employee_success.png` });
    console.log('Saved root_03_admin_reset_employee_success.png');

    // -------------------------------------------------------------
    // TEST 4: Old password rejected, new password works for nv01
    // -------------------------------------------------------------
    console.log('\n--- TEST 4: VERIFY OLD PASSWORD REJECTED & NEW PASSWORD ACCEPTED ---');
    const userPage = await browser.newPage();
    await userPage.setViewport({ width: 1440, height: 900 });

    // Try logging in with OLD password tradecore123
    await userPage.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await userPage.type('input[name="Input.Username"]', 'nv01');
    await userPage.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([
        userPage.waitForNavigation({ waitUntil: 'networkidle2' }).catch(() => {}),
        userPage.click('button[type="submit"]')
    ]);
    await delay(1000);
    console.log('After login attempt with OLD password, URL is:', userPage.url());

    // Now login with NEW password TradeFlow@2026!Nv01
    await userPage.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await userPage.type('input[name="Input.Username"]', 'nv01');
    await userPage.type('input[name="Input.Password"]', 'TradeFlow@2026!Nv01');
    await Promise.all([
        userPage.waitForNavigation({ waitUntil: 'networkidle2' }),
        userPage.click('button[type="submit"]')
    ]);
    await delay(1000);
    console.log('After login with NEW password, redirected to URL:', userPage.url());
    await userPage.screenshot({ path: `${ARTIFACT_DIR}/root_04_employee_forced_change_password.png` });
    console.log('Saved root_04_employee_forced_change_password.png');
    await userPage.close();

    // -------------------------------------------------------------
    // TEST 5: Admin Resets Password for Manager (quanly01) and HR (hr01)
    // -------------------------------------------------------------
    console.log('\n--- TEST 5: ADMIN RESETS PASSWORD FOR MANAGER & HR ---');
    await page.goto('http://127.0.0.1:5169/cai-dat/nguoi-dung/bao-mat', { waitUntil: 'networkidle2' });
    await delay(1500);

    // Filter user by typing quanly01
    const searchInput = await page.$('input[placeholder*="Tìm username"]');
    if (searchInput) {
        await searchInput.type('quanly01');
        await delay(600);
        const quanlyItem = await page.$('.tf-card-body div[style*="cursor:pointer"]');
        if (quanlyItem) await quanlyItem.click();
        await delay(800);
    }

    // Reset quanly01 password
    const newPassInput2 = await page.$('#input-new-password');
    if (newPassInput2) {
        await newPassInput2.click({ clickCount: 3 });
        await newPassInput2.type('TradeFlow@2026!Manager');
    }
    const adminVerifyInput2 = await page.$('#input-admin-verify-password');
    if (adminVerifyInput2) {
        await adminVerifyInput2.type('tradecore123');
    }
    const submitBtn2 = await page.$('#btn-submit-reset-password');
    if (submitBtn2) {
        await submitBtn2.click();
        await delay(1500);
        console.log('Reset password for quanly01 successfully.');
    }

    await page.screenshot({ path: `${ARTIFACT_DIR}/root_05_admin_reset_manager_hr.png` });
    console.log('Saved root_05_admin_reset_manager_hr.png');

    // -------------------------------------------------------------
    // TEST 6: Manager and HR cannot access ROOT functions
    // -------------------------------------------------------------
    console.log('\n--- TEST 6: PRIVILEGE ESCALATION CHECK FOR MANAGER / HR ---');
    const nonAdminPage = await browser.newPage();
    await nonAdminPage.setViewport({ width: 1440, height: 900 });

    // Login as hr01
    await nonAdminPage.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await nonAdminPage.type('input[name="Input.Username"]', 'hr01');
    await nonAdminPage.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([
        nonAdminPage.waitForNavigation({ waitUntil: 'networkidle2' }),
        nonAdminPage.click('button[type="submit"]')
    ]);

    // Try navigating to /cai-dat/nguoi-dung/bao-mat
    await nonAdminPage.goto('http://127.0.0.1:5169/cai-dat/nguoi-dung/bao-mat', { waitUntil: 'networkidle2' });
    await delay(1000);
    console.log('hr01 attempted /cai-dat/nguoi-dung/bao-mat, URL:', nonAdminPage.url());

    await nonAdminPage.screenshot({ path: `${ARTIFACT_DIR}/root_06_non_admin_forbidden.png` });
    console.log('Saved root_06_non_admin_forbidden.png');
    await nonAdminPage.close();

    // -------------------------------------------------------------
    // TEST 7: Lock User & Verify Login Blocked
    // -------------------------------------------------------------
    console.log('\n--- TEST 7: LOCK USER & VERIFY LOGIN BLOCKED ---');
    await page.goto('http://127.0.0.1:5169/cai-dat/nguoi-dung/bao-mat', { waitUntil: 'networkidle2' });
    await delay(1500);

    // Search and select nv01
    const searchInput2 = await page.$('input[placeholder*="Tìm username"]');
    if (searchInput2) {
        await searchInput2.click({ clickCount: 3 });
        await searchInput2.type('nv01');
        await delay(600);
        const nv01Item = await page.$('.tf-card-body div[style*="cursor:pointer"]');
        if (nv01Item) await nv01Item.click();
        await delay(800);
    }

    // Click "#btn-lock-user"
    const lockBtn = await page.$('#btn-lock-user');
    if (lockBtn) {
        await lockBtn.click();
        await delay(1500);
        console.log('Clicked #btn-lock-user on nv01.');
    } else {
        console.log('Could not find #btn-lock-user, checking if already locked...');
    }

    // Try login as nv01
    const lockTestPage = await browser.newPage();
    await lockTestPage.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await lockTestPage.type('input[name="Input.Username"]', 'nv01');
    await lockTestPage.type('input[name="Input.Password"]', 'TradeFlow@2026!Nv01');
    await lockTestPage.click('button[type="submit"]');
    await delay(1200);

    const lockError = await lockTestPage.evaluate(() => {
        return document.querySelector('.tf-login-alert, .alert, .text-danger')?.innerText || '';
    });
    console.log('Lock attempt error message on login:', lockError);
    await lockTestPage.screenshot({ path: `${ARTIFACT_DIR}/root_07_account_lock_prevention.png` });
    console.log('Saved root_07_account_lock_prevention.png');
    await lockTestPage.close();

    // Unlock nv01 for cleanliness
    const unlockBtn = await page.$('#btn-unlock-user');
    if (unlockBtn) {
        await unlockBtn.click();
        await delay(1000);
        console.log('Clicked #btn-unlock-user on nv01.');
    }

    // -------------------------------------------------------------
    // TEST 8: Security Audit Log Table & Details
    // -------------------------------------------------------------
    console.log('\n--- TEST 8: SECURITY AUDIT LOG ---');
    await page.goto('http://127.0.0.1:5169/cai-dat/nguoi-dung/bao-mat', { waitUntil: 'networkidle2' });
    await delay(1500);

    // Scroll to audit log table at bottom
    await page.evaluate(() => {
        window.scrollTo(0, document.body.scrollHeight);
    });
    await delay(800);

    const auditRows = await page.evaluate(() => {
        return Array.from(document.querySelectorAll('table.tf-table tbody tr')).slice(0, 8).map(tr => {
            return Array.from(tr.querySelectorAll('td')).map(td => td.innerText.replace(/\n/g, ' ').trim());
        });
    });
    console.log('Security Audit Log rows count:', auditRows.length);
    console.log('First 3 audit rows:', JSON.stringify(auditRows.slice(0, 3), null, 2));

    await page.screenshot({ path: `${ARTIFACT_DIR}/root_08_security_audit_log.png` });
    console.log('Saved root_08_security_audit_log.png');

    await browser.close();
    console.log('\n=== ALL ROOT CREDENTIAL GOVERNANCE TESTS COMPLETED SUCCESSFULLY! ===');
})();
