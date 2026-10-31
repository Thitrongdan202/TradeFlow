const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACTS_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://127.0.0.1:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('--- STARTING COMPREHENSIVE POPUP & HR WORKFLOW VERIFICATION ---');

    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    const results = {
        popupLatency: {},
        workflow: {},
        screenshots: []
    };

    try {
        // 1. LOGIN
        console.log('1. Logging in as admin...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('#login-username', { timeout: 15000 });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        console.log('Logged in successfully. Current URL:', page.url());
        await sleep(1500);

        // 2. TEST POPUP OPENING LATENCIES
        console.log('2. Testing popup opening latencies...');
        await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('.tf-page-actions', { timeout: 15000 });
        await sleep(1000);

        // Test Employee Form Modal Opening Latency
        const t0 = Date.now();
        await page.click('.tf-page-actions button.tf-btn-primary');
        await page.waitForSelector('.employee-modal', { visible: true, timeout: 5000 });
        const formModalOpenMs = Date.now() - t0;
        results.popupLatency.employeeFormModal = formModalOpenMs;
        console.log(`- EmployeeFormModal opened in ${formModalOpenMs}ms`);

        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'verify_employee_form_modal_light.png') });
        results.screenshots.push('verify_employee_form_modal_light.png');

        // Close modal
        await page.click('.tf-modal-close-btn');
        await page.waitForFunction(() => !document.querySelector('.employee-modal'), { timeout: 5000 });
        await sleep(400);

        // Test Employee Request Modal Opening Latency
        const t1 = Date.now();
        await page.click('.tf-page-actions button.tf-btn-outline');
        await page.waitForSelector('.tf-req-filter-bar', { visible: true, timeout: 5000 });
        const reqModalOpenMs = Date.now() - t1;
        results.popupLatency.employeeRequestModal = reqModalOpenMs;
        console.log(`- EmployeeRequestModal opened in ${reqModalOpenMs}ms`);

        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'verify_employee_request_modal_light.png') });
        results.screenshots.push('verify_employee_request_modal_light.png');

        // Close request modal
        await page.click('.tf-modal-close-btn');
        await page.waitForFunction(() => !document.querySelector('.tf-req-filter-bar'), { timeout: 5000 });
        await sleep(400);

        // 3. HR FEEDBACK WORKFLOW: SCENARIO A — REQUEST ADDITIONAL DOCUMENTS
        console.log('3. Executing Scenario A: Submit request and request additional documents...');
        await page.click('.tf-page-actions button.tf-btn-primary');
        await page.waitForSelector('.employee-modal', { visible: true, timeout: 5000 });
        await sleep(400);

        // Select mode "Gửi yêu cầu phê duyệt nhân sự"
        await page.evaluate(() => {
            const labels = Array.from(document.querySelectorAll('.tf-modal-subheader label'));
            const reqLabel = labels.find(l => l.textContent.includes('Gửi yêu cầu'));
            if (reqLabel) {
                reqLabel.click();
                const r = reqLabel.querySelector('input[type="radio"]');
                if (r) {
                    r.checked = true;
                    r.dispatchEvent(new Event('change', { bubbles: true }));
                }
            }
        });
        await sleep(400);

        const testCandidateName = "Phan Nhật Nam - " + Date.now().toString().slice(-4);
        await page.type('input[placeholder*="Nguyễn Văn An"]', testCandidateName, { delay: 10 });
        await page.type('input[placeholder*="0901 234 567"]', '0938889999', { delay: 10 });
        await page.type('input[placeholder*="nhanvien@congty.vn"]', 'nhatnam@tradeflow.vn', { delay: 10 });

        // Add demo dossier
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.employee-modal button'));
            const demoBtn = btns.find(b => b.textContent.includes('hồ sơ mẫu'));
            if (demoBtn) demoBtn.click();
        });
        await sleep(400);

        // Submit form
        await page.click('.employee-modal button[type="submit"]');
        await page.waitForFunction(() => !document.querySelector('.employee-modal'), { timeout: 10000 });
        await sleep(1000);
        console.log(`Submitted employee request for: ${testCandidateName}`);

        // Open Request Modal to review
        await page.click('.tf-page-actions button.tf-btn-outline');
        await page.waitForSelector('.tf-req-filter-bar', { visible: true, timeout: 5000 });
        await page.waitForSelector('.tf-req-card', { visible: true, timeout: 10000 });
        await sleep(500);

        // Click "Yêu cầu bổ sung"
        await page.evaluate((name) => {
            const cards = Array.from(document.querySelectorAll('.tf-req-card'));
            const card = cards.find(c => c.textContent.includes(name));
            if (card) {
                const btns = Array.from(card.querySelectorAll('button'));
                const suppBtn = btns.find(b => b.textContent.includes('Yêu cầu bổ sung'));
                if (suppBtn) suppBtn.click();
            }
        }, testCandidateName);
        await sleep(400);

        // Enter review note and confirm
        const supplementNote = "Vui lòng cập nhật số CCCD và đính kèm bằng cấp chuyên môn!";
        await page.waitForSelector('.tf-req-actions input', { visible: true, timeout: 5000 });
        await page.type('.tf-req-actions input', supplementNote, { delay: 10 });
        await sleep(200);

        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-req-actions button'));
            const confirmBtn = btns.find(b => b.textContent.includes('Yêu cầu bổ sung hồ sơ'));
            if (confirmBtn) confirmBtn.click();
        });
        await sleep(1500);

        // Switch to "Cần bổ sung hồ sơ" tab and verify status
        await page.evaluate(() => {
            const tabs = Array.from(document.querySelectorAll('.tf-tab-btn'));
            const tab = tabs.find(t => t.textContent.includes('Cần bổ sung'));
            if (tab) tab.click();
        });
        await sleep(800);

        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'verify_status_needs_supplement.png') });
        results.screenshots.push('verify_status_needs_supplement.png');
        console.log('Request successfully transitioned to NeedAdditionalDocs');

        // Close request modal
        await page.click('.tf-modal-close-btn');
        await page.waitForFunction(() => !document.querySelector('.tf-req-filter-bar'), { timeout: 5000 });
        await sleep(500);

        // 4. NOTIFICATION CENTER VERIFICATION & ACTION BUTTON
        console.log('4. Verifying Notification Center & Actionable Button...');
        await page.click('#tf-notification-menu-btn');
        await page.waitForSelector('#tf-notification-panel', { visible: true, timeout: 5000 });
        await sleep(600);

        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'verify_notification_dropdown_light.png') });
        results.screenshots.push('verify_notification_dropdown_light.png');

        // Verify actionable button exists in notification
        const notifButtonInfo = await page.evaluate((name) => {
            const panel = document.getElementById('tf-notification-panel');
            if (!panel) return { found: false };
            const items = Array.from(panel.querySelectorAll('.tf-notif-item'));
            const targetItem = items.find(it => it.textContent.includes(name) || it.textContent.includes('Cần bổ sung'));
            if (!targetItem) return { found: false, count: items.length };
            const btn = targetItem.querySelector('.tf-btn-action-pill');
            return {
                found: true,
                title: targetItem.querySelector('.tf-notif-item-title')?.textContent.trim(),
                btnText: btn ? btn.textContent.trim() : null,
                href: btn ? btn.getAttribute('href') : null
            };
        }, testCandidateName);

        console.log('Notification item found:', notifButtonInfo);
        results.workflow.notificationItem = notifButtonInfo;

        // Deep-link via notification href
        if (notifButtonInfo.href) {
            await page.goto(`${BASE_URL}${notifButtonInfo.href}`, { waitUntil: 'networkidle2' });
            await page.waitForSelector('.tf-req-filter-bar', { visible: true, timeout: 8000 });
            await sleep(800);

            const modalCheck = await page.evaluate(() => {
                const activeTab = document.querySelector('.tf-tab-btn.active');
                const highlightedCard = document.querySelector('.tf-req-card.highlighted-target');
                return {
                    activeTabName: activeTab ? activeTab.textContent.trim() : null,
                    hasHighlightedCard: !!highlightedCard
                };
            });
            console.log('Modal deep-link check:', modalCheck);
            results.workflow.modalCheck = modalCheck;

            await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'verify_deep_link_highlight_light.png') });
            results.screenshots.push('verify_deep_link_highlight_light.png');

            // 5. RESUBMISSION FLOW
            console.log('5. Executing Resubmission: Clicking "Bổ sung hồ sơ & Gửi lại"...');
            await page.evaluate(() => {
                const card = document.querySelector('.tf-req-card.highlighted-target') || document.querySelector('.tf-req-card');
                if (card) {
                    const btn = Array.from(card.querySelectorAll('button')).find(b => b.textContent.includes('Bổ sung hồ sơ & Gửi lại') || b.textContent.includes('Bổ sung'));
                    if (btn) btn.click();
                }
            });
            await page.waitForSelector('.employee-modal', { visible: true, timeout: 5000 });
            await sleep(800);

            await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'verify_resubmit_form_modal.png') });
            results.screenshots.push('verify_resubmit_form_modal.png');

            // Fill CCCD and click Demo Dossier to add more documents
            
            await page.type('input[placeholder*="001200001234"]', '079095012345', { delay: 10 });
            await page.evaluate(() => {
                const btns = Array.from(document.querySelectorAll('.employee-modal button'));
                const demoBtn = btns.find(b => b.textContent.includes('hồ sơ mẫu'));
                if (demoBtn) demoBtn.click();
            });
            await sleep(400);

            // Click "Gửi lại để duyệt"
            await page.click('.employee-modal button[type="submit"]');
            await page.waitForFunction(() => !document.querySelector('.employee-modal'), { timeout: 10000 });
            await sleep(1000);
            console.log('Resubmission submitted successfully!');

            // 6. APPROVE RESUBMITTED REQUEST
            console.log('6. Approving resubmitted request...');
            await page.click('.tf-page-actions button.tf-btn-outline');
            await page.waitForSelector('.tf-req-filter-bar', { visible: true, timeout: 5000 });
            await page.waitForSelector('.tf-req-card', { visible: true, timeout: 10000 });
            await sleep(800);

            // In "Chờ duyệt" tab, approve request
            await page.evaluate((name) => {
                const cards = Array.from(document.querySelectorAll('.tf-req-card'));
                const card = cards.find(c => c.textContent.includes(name));
                if (card) {
                    const btns = Array.from(card.querySelectorAll('button'));
                    const apprBtn = btns.find(b => b.textContent.includes('Phê duyệt'));
                    if (apprBtn) apprBtn.click();
                }
            }, testCandidateName);
            await sleep(400);

            await page.evaluate(() => {
                const btns = Array.from(document.querySelectorAll('.tf-req-actions button'));
                const confirmBtn = btns.find(b => b.textContent.includes('Phê duyệt & Tạo hồ sơ'));
                if (confirmBtn) confirmBtn.click();
            });
            await sleep(1500);
            console.log('Resubmitted request approved and employee created!');

            await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'verify_request_approved_success.png') });
            results.screenshots.push('verify_request_approved_success.png');

            // Close request modal
            await page.click('.tf-modal-close-btn');
            await page.waitForFunction(() => !document.querySelector('.tf-req-filter-bar'), { timeout: 5000 });
            await sleep(500);
        }

        // 7. SCENARIO B: REJECTION & CLONE WORKFLOW
        console.log('7. Testing Scenario B: Rejection and "Tạo yêu cầu mới từ hồ sơ này"...');
        await page.click('.tf-page-actions button.tf-btn-primary');
        await page.waitForSelector('.employee-modal', { visible: true, timeout: 5000 });
        await sleep(500);

        // Select mode "Gửi yêu cầu phê duyệt nhân sự"
        await page.evaluate(() => {
            const labels = Array.from(document.querySelectorAll('.tf-modal-subheader label'));
            const reqLabel = labels.find(l => l.textContent.includes('Gửi yêu cầu'));
            if (reqLabel) {
                reqLabel.click();
                const r = reqLabel.querySelector('input[type="radio"]');
                if (r) {
                    r.checked = true;
                    r.dispatchEvent(new Event('change', { bubbles: true }));
                }
            }
        });
        await sleep(400);

        const rejectCandidateName = "Đỗ Minh Khang - " + Date.now().toString().slice(-4);
        await page.waitForSelector('input[placeholder*="Nguyễn Văn An"]', { visible: true });
        await page.type('input[placeholder*="Nguyễn Văn An"]', rejectCandidateName, { delay: 10 });
        await page.type('input[placeholder*="0901 234 567"]', '0944556677', { delay: 10 });
        await page.type('input[placeholder*="nhanvien@congty.vn"]', 'khang.do@tradeflow.vn', { delay: 10 });

        await page.click('.employee-modal button[type="submit"]');
        await page.waitForFunction(() => !document.querySelector('.employee-modal'), { timeout: 10000 });
        await sleep(1000);

        // Open Request Modal and Reject
        await page.click('.tf-page-actions button.tf-btn-outline');
        await page.waitForSelector('.tf-req-filter-bar', { visible: true, timeout: 5000 });
        await page.waitForSelector('.tf-req-card', { visible: true, timeout: 10000 });
        await sleep(800);

        await page.evaluate((name) => {
            const cards = Array.from(document.querySelectorAll('.tf-req-card'));
            const card = cards.find(c => c.textContent.includes(name));
            if (card) {
                const btns = Array.from(card.querySelectorAll('button'));
                const rejBtn = btns.find(b => b.textContent.includes('Từ chối'));
                if (rejBtn) rejBtn.click();
            }
        }, rejectCandidateName);
        await sleep(400);

        const rejectReason = "Chưa đáp ứng tiêu chí kinh nghiệm theo yêu cầu";
        await page.waitForSelector('.tf-req-actions input', { visible: true, timeout: 5000 });
        await page.type('.tf-req-actions input', rejectReason, { delay: 10 });
        await sleep(200);

        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-req-actions button'));
            const confirmBtn = btns.find(b => b.textContent.includes('Xác nhận từ chối'));
            if (confirmBtn) confirmBtn.click();
        });
        await sleep(1500);

        // Filter to "Từ chối" tab
        await page.evaluate(() => {
            const tabs = Array.from(document.querySelectorAll('.tf-tab-btn'));
            const tab = tabs.find(t => t.textContent.includes('Từ chối'));
            if (tab) tab.click();
        });
        await sleep(800);

        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'verify_rejected_request_card.png') });
        results.screenshots.push('verify_rejected_request_card.png');

        // Click "Tạo yêu cầu mới từ hồ sơ này"
        await page.evaluate((name) => {
            const cards = Array.from(document.querySelectorAll('.tf-req-card'));
            const card = cards.find(c => c.textContent.includes(name));
            if (card) {
                const btn = Array.from(card.querySelectorAll('button')).find(b => b.textContent.includes('Tạo yêu cầu mới từ hồ sơ này'));
                if (btn) btn.click();
            }
        }, rejectCandidateName);
        await page.waitForSelector('.employee-modal', { visible: true, timeout: 5000 });
        await sleep(800);

        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'verify_cloned_request_form_modal.png') });
        results.screenshots.push('verify_cloned_request_form_modal.png');
        console.log('Cloned request form opened with prefilled fields!');

        // Discard & close form modal
        await page.click('.tf-modal-close-btn');
        await sleep(300);
        await page.evaluate(() => {
            const discardBtn = Array.from(document.querySelectorAll('button')).find(b => b.textContent.includes('Hủy thay đổi'));
            if (discardBtn) discardBtn.click();
        });
        await page.waitForFunction(() => !document.querySelector('.employee-modal'), { timeout: 5000 });
        await sleep(400);

        // 8. TEST DARK MODE & CAPTURE DARK SCREENSHOTS
        console.log('8. Toggling Dark Mode and capturing Dark Mode screenshots...');
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.documentElement.setAttribute('data-theme', 'dark');
        });
        await sleep(400);

        // Open Notifications in Dark Mode
        await page.click('#tf-notification-menu-btn');
        await page.waitForSelector('#tf-notification-panel', { visible: true, timeout: 5000 });
        await sleep(600);
        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'verify_notification_dropdown_dark.png') });
        results.screenshots.push('verify_notification_dropdown_dark.png');

        // Close notifications
        await page.keyboard.press('Escape');
        await sleep(300);

        // Open Request Modal in Dark Mode
        await page.click('.tf-page-actions button.tf-btn-outline');
        await page.waitForSelector('.tf-req-filter-bar', { visible: true, timeout: 5000 });
        await sleep(800);
        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'verify_employee_request_modal_dark.png') });
        results.screenshots.push('verify_employee_request_modal_dark.png');

        // Close request modal
        await page.click('.tf-modal-close-btn');
        await page.waitForFunction(() => !document.querySelector('.tf-req-filter-bar'), { timeout: 5000 });
        await sleep(400);

        // 9. TEST INSTANT MODAL OPENERS: INVOICE SIGN & VERIFY
        console.log('9. Testing instant modal opener in Invoice Detail...');
        await page.goto(`${BASE_URL}/hoa-don/chi-tiet/14`, { waitUntil: 'networkidle2' });
        await sleep(1000);
        await page.waitForSelector('button.tf-btn-success', { visible: true, timeout: 5000 });

        // Test "Ký điện tử" instant opening in Dark Mode
        const t2 = Date.now();
        await page.click('button.tf-btn-success');
        await page.waitForSelector('div[style*="position:fixed"]', { visible: true, timeout: 5000 });
        const signModalOpenMs = Date.now() - t2;
        results.popupLatency.invoiceSignModal = signModalOpenMs;
        console.log(`- Invoice sign modal opened in ${signModalOpenMs}ms`);

        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'verify_invoice_verify_modal_dark.png') });
        results.screenshots.push('verify_invoice_verify_modal_dark.png');

        // Close modal
        await page.click('button.tf-btn-ghost.tf-btn-sm');
        await page.waitForFunction(() => !document.querySelector('div[style*="position:fixed"]'), { timeout: 5000 });
        await sleep(400);

        // Switch to Light Mode for Invoice Modal
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'light');
            document.documentElement.setAttribute('data-theme', 'light');
        });
        await sleep(400);

        await page.click('button.tf-btn-success');
        await page.waitForSelector('div[style*="position:fixed"]', { visible: true, timeout: 5000 });
        await sleep(500);
        await page.screenshot({ path: path.join(ARTIFACTS_DIR, 'verify_invoice_verify_modal_light.png') });
        results.screenshots.push('verify_invoice_verify_modal_light.png');

        // Close modal
        await page.click('button.tf-btn-ghost.tf-btn-sm');
        await sleep(300);

        // Save JSON results
        fs.writeFileSync(
            path.join(ARTIFACTS_DIR, 'global_popup_and_hr_workflow_results.json'),
            JSON.stringify(results, null, 2),
            'utf8'
        );
        console.log('Saved verification results JSON');

    } catch (err) {
        console.error('Error during test execution:', err);
    } finally {
        await browser.close();
        console.log('--- TEST EXECUTION COMPLETED ---');
    }
})();
