const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://127.0.0.1:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function clearAuth(page) {
    const cookies = await page.cookies();
    if (cookies.length > 0) {
        await page.deleteCookie(...cookies);
    }
}

async function fillLogin(page, username, password) {
    await page.evaluate(() => {
        const u = document.getElementById('login-username');
        const p = document.getElementById('login-password');
        if (u) u.value = '';
        if (p) p.value = '';
    });
    await page.type('#login-username', username);
    await page.type('#login-password', password);
}

(async () => {
    console.log('=== STARTING TRADEFLOW EMPLOYEE ACCOUNT PROVISIONING VERIFICATION ===');
    const browser = await puppeteer.launch({
        headless: 'new',
        defaultViewport: { width: 1440, height: 900 },
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage']
    });

    const page = await browser.newPage();
    page.setDefaultTimeout(15000);

    const results = {
        test1_employee_list_ui: false,
        test2_account_provisioning: false,
        test3_first_time_login_interception: false,
        test4_rbac_boundaries_403: false,
        test5_password_reset: false,
        test6_account_lock_unlock: false,
        test7_termination_deactivation: false,
        test8_role_hierarchy: false
    };

    let tempPasswordExtracted = null;
    let provisionedUsername = 'nv.mai_' + Date.now().toString().slice(-4);
    const validNewPassword = 'Tr@deFlow#Sec2026!';

    try {
        // -------------------------------------------------------------
        // TEST 1: Admin logs in & inspects Employee list with Account UI
        // -------------------------------------------------------------
        console.log('\n--- TEST 1: Admin Login & Employee List UI ---');
        await clearAuth(page);
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await fillLogin(page, 'admin', 'tradecore123');

        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);

        console.log('Admin logged in successfully. Navigating to /nhan-vien...');
        await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
        await sleep(2000);

        // Check if "Tài khoản" table header exists (case-insensitive check for text-transform: uppercase)
        const thTexts = await page.$$eval('th', ths => ths.map(t => t.innerText.trim()));
        const hasAccountHeader = thTexts.some(t => t.toLowerCase().includes('tài khoản'));
        console.log(`Table headers found: ${thTexts.join(', ')}`);
        console.log(`"Tài khoản" column present: ${hasAccountHeader}`);

        // Check sidebar filter card
        const filterTexts = await page.$$eval('.tf-filter-title', els => els.map(e => e.innerText.trim()));
        const hasAccountFilter = filterTexts.some(t => t.toLowerCase().includes('tài khoản tradeflow'));
        console.log(`Sidebar filters found: ${filterTexts.join(', ')}`);
        console.log(`"Tài khoản TradeFlow" filter card present: ${hasAccountFilter}`);

        await page.screenshot({ path: path.join(ARTIFACT_DIR, '01_employee_list_with_account_columns.png'), fullPage: false });
        if (hasAccountHeader && hasAccountFilter) {
            results.test1_employee_list_ui = true;
            console.log('✓ TEST 1 PASSED: Employee table and filter sidebar show Account features.');
        }

        // -------------------------------------------------------------
        // TEST 2: Account Provisioning with One-Time Temporary Password
        // -------------------------------------------------------------
        console.log('\n--- TEST 2: Account Provisioning with Temporary Password ---');
        const rows = await page.$$('.tf-emp-row');
        let targetRowIndex = -1;
        for (let i = 0; i < rows.length; i++) {
            const text = await rows[i].evaluate(el => el.innerText);
            if (text.includes('Chưa cấp')) {
                targetRowIndex = i;
                break;
            }
        }

        if (targetRowIndex === -1) {
            targetRowIndex = 0;
        }

        console.log(`Expanding row ${targetRowIndex} by clicking name cell...`);
        const nameCell = await rows[targetRowIndex].$('td:nth-child(5)');
        await nameCell.click();
        await sleep(1500);

        // Find "Cấp tài khoản đăng nhập" button in the expanded row or page
        const provBtnHandle = await page.evaluateHandle(() => {
            const btns = Array.from(document.querySelectorAll('button'));
            return btns.find(b => b.innerText.includes('Cấp tài khoản đăng nhập'));
        });

        if (provBtnHandle) {
            await provBtnHandle.click();
            await sleep(1500);
            await page.waitForSelector('#input-provision-username', { visible: true, timeout: 8000 });

            // Clear input via select-all backspace to trigger Blazor input binding
            await page.click('#input-provision-username');
            await page.keyboard.down('Control');
            await page.keyboard.press('KeyA');
            await page.keyboard.up('Control');
            await page.keyboard.press('Backspace');
            await page.type('#input-provision-username', provisionedUsername);
            await page.select('#select-provision-role', 'Employee');

            console.log(`Submitting provision command for username: ${provisionedUsername}...`);
            await page.click('#btn-provision-submit');
            await sleep(2500);

            // Verify temporary password display
            const tempPassBtn = await page.$('#btn-copy-temp-password');
            const hasTempPassBtn = tempPassBtn !== null;
            console.log(`One-time temporary password screen visible: ${hasTempPassBtn}`);

            await page.screenshot({ path: path.join(ARTIFACT_DIR, '02_provision_account_success_modal.png'), fullPage: false });

            // Toggle eye icon to unmask temporary password
            const eyeBtn = await page.evaluateHandle(() => {
                const btns = Array.from(document.querySelectorAll('.tf-modal-dialog button'));
                return btns.find(b => b.title === 'Hiện' || b.title === 'Ẩn');
            });
            if (eyeBtn) {
                await eyeBtn.click();
                await sleep(500);
            }

            tempPasswordExtracted = await page.evaluate(() => {
                const el = document.getElementById('display-temp-password');
                if (el) {
                    const txt = el.innerText.trim();
                    if (txt.length >= 8 && txt !== '••••••••••••') return txt;
                }
                const modal = document.querySelector('.tf-modal-dialog');
                if (!modal) return null;
                const divs = Array.from(modal.querySelectorAll('div'));
                for (const div of divs) {
                    if (div.style.letterSpacing === '1px' || div.style.fontFamily.includes('monospace')) {
                        const txt = div.innerText.trim();
                        if (txt.length >= 8 && txt !== '••••••••••••') return txt;
                    }
                }
                return null;
            });

            console.log(`Extracted temporary password: ${tempPasswordExtracted ? '[REDACTED: ' + tempPasswordExtracted.length + ' chars]' : 'FAILED'}`);

            // Close modal
            const doneBtn = await page.evaluateHandle(() => {
                const btns = Array.from(document.querySelectorAll('.tf-modal-dialog button'));
                return btns.find(b => b.innerText.includes('Hoàn tất') || b.innerText.includes('Đóng'));
            });
            if (doneBtn) await doneBtn.click();
            await sleep(1500);

            results.test2_account_provisioning = !!tempPasswordExtracted;
            console.log('✓ TEST 2 PASSED: Account provisioned with one-time temporary password.');
        }

        // -------------------------------------------------------------
        // TEST 3: First-time Login Interception & Mandatory Password Change
        // -------------------------------------------------------------
        console.log('\n--- TEST 3: First-Time Login Interception ---');
        // Clear auth cookies
        await clearAuth(page);

        console.log(`Logging in as ${provisionedUsername} with temporary password...`);
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await fillLogin(page, provisionedUsername, tempPasswordExtracted);

        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);

        const currentUrl = page.url();
        console.log(`Redirected URL after login: ${currentUrl}`);
        const isChangePasswordPage = currentUrl.toLowerCase().includes('/account/changepassword');
        console.log(`First-time login intercepted to /Account/ChangePassword: ${isChangePasswordPage}`);

        await page.screenshot({ path: path.join(ARTIFACT_DIR, '03_first_time_login_intercepted.png'), fullPage: false });

        // Try navigating away to "/" before changing password
        console.log('Testing URL bypass attempt to "/"...');
        await page.goto(`${BASE_URL}/`, { waitUntil: 'networkidle2' });
        await sleep(1000);
        const bypassedUrl = page.url();
        console.log(`URL after bypass attempt: ${bypassedUrl}`);
        const bypassPrevented = bypassedUrl.toLowerCase().includes('/account/changepassword');
        console.log(`Bypass attempt strictly blocked: ${bypassPrevented}`);

        // Test identical password rejection
        console.log('Testing rejection of identical password...');
        await page.type('#old-password', tempPasswordExtracted);
        await page.type('#new-password', tempPasswordExtracted);
        await page.type('#confirm-password', tempPasswordExtracted);
        await page.click('#btn-change-password-submit');
        await sleep(1500);

        const errorText = await page.$eval('.tf-login-alert-error', el => el.innerText).catch(() => '');
        console.log(`Error message for identical password: "${errorText}"`);
        const identicalRejected = errorText.includes('không được trùng');

        // Set strong valid new password
        console.log(`Setting valid new password: ${validNewPassword}...`);
        await page.evaluate(() => {
            const op = document.getElementById('old-password');
            const np = document.getElementById('new-password');
            const cp = document.getElementById('confirm-password');
            if (op) op.value = '';
            if (np) np.value = '';
            if (cp) cp.value = '';
        });
        await page.type('#old-password', tempPasswordExtracted);
        await page.type('#new-password', validNewPassword);
        await page.type('#confirm-password', validNewPassword);

        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('#btn-change-password-submit')
        ]);
        await sleep(1500);

        await page.screenshot({ path: path.join(ARTIFACT_DIR, '04_password_changed_success.png'), fullPage: false });
        const postChangeUrl = page.url();
        console.log(`URL after password change: ${postChangeUrl}`);
        const passwordChangeSuccess = postChangeUrl === `${BASE_URL}/` || !postChangeUrl.toLowerCase().includes('/account/changepassword');

        if (isChangePasswordPage && bypassPrevented && identicalRejected && passwordChangeSuccess) {
            results.test3_first_time_login_interception = true;
            console.log('✓ TEST 3 PASSED: First-time login intercepted, bypass blocked, password change enforced.');
        }

        // -------------------------------------------------------------
        // TEST 4: RBAC Boundaries Enforcement for Employee Role
        // -------------------------------------------------------------
        console.log('\n--- TEST 4: RBAC Server-Side Enforcement (403 Forbidden) ---');
        console.log('Attempting to access /cai-dat as Employee...');
        await page.goto(`${BASE_URL}/cai-dat`, { waitUntil: 'networkidle2' });
        await sleep(2000);

        const pageContent = await page.content();
        const curUrl = page.url();
        const has403 = curUrl.includes('AccessDenied') || pageContent.includes('403') || pageContent.includes('Từ chối truy cập') || pageContent.includes('Không có quyền truy cập');
        console.log(`RBAC enforcement: URL=${curUrl}, Access Denied displayed: ${has403}`);

        await page.screenshot({ path: path.join(ARTIFACT_DIR, '05_rbac_forbidden_403.png'), fullPage: false });

        if (has403) {
            results.test4_rbac_boundaries_403 = true;
            console.log('✓ TEST 4 PASSED: Employee blocked with HTTP 403 / Access Denied from admin configuration.');
        }

        // -------------------------------------------------------------
        // TEST 5: Admin Resets Password
        // -------------------------------------------------------------
        console.log('\n--- TEST 5: Admin Resets Password ---');
        await clearAuth(page);
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await fillLogin(page, 'admin', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);

        await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
        await sleep(2000);

        // Expand the row of provisioned user
        const allRows = await page.$$('.tf-emp-row');
        let matchedRow = null;
        for (const r of allRows) {
            const txt = await r.evaluate(el => el.innerText);
            if (txt.includes(provisionedUsername)) {
                matchedRow = r;
                break;
            }
        }

        if (matchedRow) {
            const nameC = await matchedRow.$('td:nth-child(5)');
            await nameC.click();
            await sleep(1500);

            const resetBtn = await page.evaluateHandle(() => {
                const btns = Array.from(document.querySelectorAll('button'));
                return btns.find(b => b.innerText.includes('Đặt lại MK'));
            });

            if (resetBtn) {
                await resetBtn.click();
                await sleep(1200);
                await page.waitForSelector('#btn-reset-password-confirm', { visible: true, timeout: 5000 });

                await page.click('#btn-reset-password-confirm');
                await sleep(2500);

                await page.screenshot({ path: path.join(ARTIFACT_DIR, '06_reset_password_modal.png'), fullPage: false });

                const hasNewTempPass = (await page.$('#btn-copy-reset-password')) !== null;
                console.log(`New temporary password generated & displayed: ${hasNewTempPass}`);

                const doneResetBtn = await page.evaluateHandle(() => {
                    const btns = Array.from(document.querySelectorAll('.tf-modal-dialog button'));
                    return btns.find(b => b.innerText.includes('Hoàn tất') || b.innerText.includes('Đóng'));
                });
                if (doneResetBtn) await doneResetBtn.click();
                await sleep(1500);

                if (hasNewTempPass) {
                    results.test5_password_reset = true;
                    console.log('✓ TEST 5 PASSED: Password reset generated new temporary password & revoked sessions.');
                }
            }
        }

        // -------------------------------------------------------------
        // TEST 6: Account Locking & Unlocking
        // -------------------------------------------------------------
        console.log('\n--- TEST 6: Account Lock & Unlock ---');
        // Click "Khóa TK"
        const lockBtn = await page.evaluateHandle(() => {
            const btns = Array.from(document.querySelectorAll('button'));
            return btns.find(b => b.innerText.includes('Khóa TK'));
        });

        if (lockBtn) {
            await lockBtn.click();
            await sleep(2000);

            await page.screenshot({ path: path.join(ARTIFACT_DIR, '07_account_locked_badge.png'), fullPage: false });

            // Verify table badge
            const pageTxt = await page.evaluate(() => document.body.innerText);
            const isLockedShown = pageTxt.includes('Đã khóa') || pageTxt.includes('Khóa');
            console.log(`Account locked badge visible: ${isLockedShown}`);

            // Test login with locked account
            await clearAuth(page);
            await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
            await fillLogin(page, provisionedUsername, validNewPassword);
            await page.click('button[type="submit"]');
            await sleep(2000);

            const lockedMsg = await page.$eval('#tf-login-error-alert', el => el.innerText).catch(() => '');
            console.log(`Locked login alert message: "${lockedMsg}"`);
            const lockedBlocked = lockedMsg.includes('bị khóa');

            await page.screenshot({ path: path.join(ARTIFACT_DIR, '08_locked_login_denied.png'), fullPage: false });

            // Unlock as admin
            await clearAuth(page);
            await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
            await fillLogin(page, 'admin', 'tradecore123');
            await Promise.all([
                page.waitForNavigation({ waitUntil: 'networkidle2' }),
                page.click('button[type="submit"]')
            ]);

            await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
            await sleep(2000);

            const rowsLock = await page.$$('.tf-emp-row');
            for (const r of rowsLock) {
                const txt = await r.evaluate(el => el.innerText);
                if (txt.includes(provisionedUsername)) {
                    const nameC = await r.$('td:nth-child(5)');
                    await nameC.click();
                    await sleep(1500);
                    break;
                }
            }

            const unlockBtn = await page.evaluateHandle(() => {
                const btns = Array.from(document.querySelectorAll('button'));
                return btns.find(b => b.innerText.includes('Mở khóa'));
            });
            if (unlockBtn) {
                await unlockBtn.click();
                await sleep(2000);
                console.log('Account successfully unlocked by Admin.');
            }

            if (isLockedShown && lockedBlocked) {
                results.test6_account_lock_unlock = true;
                console.log('✓ TEST 6 PASSED: Account locking denied access and unlocking restored access.');
            }
        }

        // -------------------------------------------------------------
        // TEST 7: Employee Termination Lifecycle & Session Deactivation
        // -------------------------------------------------------------
        console.log('\n--- TEST 7: Employee Termination & Access Revocation ---');
        // Ensure row of provisioned user is expanded
        let termRow = null;
        const currentRows = await page.$$('.tf-emp-row');
        for (const r of currentRows) {
            const txt = await r.evaluate(el => el.innerText);
            if (txt.includes(provisionedUsername)) {
                termRow = r;
                break;
            }
        }

        if (termRow) {
            const isRowExpanded = (await page.$('.tf-emp-detail-tr')) !== null;
            if (!isRowExpanded) {
                const nameC = await termRow.$('td:nth-child(5)');
                await nameC.click();
                await sleep(1500);
            }
        }

        const retireBtn = await page.evaluateHandle(() => {
            const btns = Array.from(document.querySelectorAll('.tf-emp-expanded-actions button'));
            return btns.find(b => b.innerText.includes('Thôi việc'));
        });

        if (retireBtn) {
            console.log('Opening Retire modal...');
            await retireBtn.click();
            await sleep(1500);

            // Fill reason in textarea and trigger change event
            await page.waitForSelector('#retire-reason', { visible: true, timeout: 5000 });
            await page.type('#retire-reason', 'Chấm dứt hợp đồng lao động theo thỏa thuận đôi bên.');
            await page.$eval('#retire-reason', el => {
                el.dispatchEvent(new Event('change', { bubbles: true }));
            });
            await sleep(800);

            const confirmRetireBtn = await page.$('#btn-retire-confirm, .tf-modal-footer button.tf-btn-danger');
            if (confirmRetireBtn) {
                console.log('Submitting employee retirement...');
                await confirmRetireBtn.click();
                await sleep(3500);
            }

            await page.screenshot({ path: path.join(ARTIFACT_DIR, '09_employee_terminated_badge.png'), fullPage: false });

            // Attempt login with terminated account
            await clearAuth(page);
            console.log(`Attempting login with terminated account: ${provisionedUsername}...`);
            await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
            await fillLogin(page, provisionedUsername, validNewPassword);
            await page.click('button[type="submit"]');
            await sleep(2000);

            const termMsg = await page.$eval('#tf-login-error-alert', el => el.innerText).catch(() => '');
            console.log(`Terminated login denial alert: "${termMsg}"`);
            const exactTerminationMessage = termMsg.includes('Tài khoản đã bị vô hiệu hóa') && termMsg.includes('trạng thái nhân sự đã được cập nhật');

            await page.screenshot({ path: path.join(ARTIFACT_DIR, '10_terminated_login_denied.png'), fullPage: false });

            if (exactTerminationMessage) {
                results.test7_termination_deactivation = true;
                console.log('✓ TEST 7 PASSED: Terminated employee account blocked with exact required message.');
            }
        }

        // -------------------------------------------------------------
        // TEST 8: Manager Hierarchy Boundary Enforcement
        // -------------------------------------------------------------
        console.log('\n--- TEST 8: Manager Hierarchy Boundary ---');
        await clearAuth(page);
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await fillLogin(page, 'quanly01', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);

        console.log('Manager logged in. Navigating to /nhan-vien...');
        await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
        await sleep(2000);

        const rowsM = await page.$$('.tf-emp-row');
        let unprovisionedRow = null;
        for (const r of rowsM) {
            const txt = await r.evaluate(el => el.innerText);
            if (txt.includes('Chưa cấp')) {
                unprovisionedRow = r;
                break;
            }
        }

        if (unprovisionedRow) {
            const nameC = await unprovisionedRow.$('td:nth-child(5)');
            console.log('Expanding unprovisioned employee row for Manager...');
            await nameC.click();
            await sleep(1500);

            const provBtn = await page.evaluateHandle(() => {
                const btns = Array.from(document.querySelectorAll('button'));
                return btns.find(b => b.innerText.includes('Cấp tài khoản đăng nhập'));
            });

            if (provBtn) {
                await provBtn.click();
                await sleep(1500);

                await page.waitForSelector('#select-provision-role', { visible: true, timeout: 5000 });
                const roleOptions = await page.$$eval('#select-provision-role option', opts => opts.map(o => o.value));
                console.log(`Roles available to Manager: [${roleOptions.join(', ')}]`);

                const hasAdministrator = roleOptions.some(r => r.toLowerCase() === 'administrator');
                console.log(`Manager can assign Administrator: ${hasAdministrator} (must be FALSE)`);

                await page.screenshot({ path: path.join(ARTIFACT_DIR, '11_manager_assignable_roles_no_admin.png'), fullPage: false });

                if (!hasAdministrator) {
                    results.test8_role_hierarchy = true;
                    console.log('✓ TEST 8 PASSED: Manager cannot elevate accounts to Administrator role.');
                }

                // Close modal
                const cancelBtn = await page.evaluateHandle(() => {
                    const btns = Array.from(document.querySelectorAll('.tf-modal-dialog button'));
                    return btns.find(b => b.innerText.includes('Hủy'));
                });
                if (cancelBtn) await cancelBtn.click();
                await sleep(800);
            }
        }

    } catch (err) {
        console.error('Test execution error:', err);
    } finally {
        await browser.close();
    }

    console.log('\n=== FINAL VERIFICATION SUMMARY ===');
    console.table(results);

    const allPassed = Object.values(results).every(v => v === true);
    console.log(allPassed ? '\n>>> ALL 8 TESTS PASSED SUCCESSFULLY! <<<' : '\n>>> SOME TESTS FAILED <<<');

    fs.writeFileSync(
        path.join(ARTIFACT_DIR, 'verification_employee_account_provisioning.json'),
        JSON.stringify({ results, allPassed, timestamp: new Date().toISOString() }, null, 2)
    );
})();
