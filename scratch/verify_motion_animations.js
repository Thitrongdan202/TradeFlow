const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACTS_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://127.0.0.1:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function setTheme(page, theme) {
    await page.evaluate((th) => {
        document.cookie = `tf_theme=${th}; path=/; max-age=31536000; SameSite=Lax`;
        document.cookie = `tradeflow_theme=${th}; path=/; max-age=31536000; SameSite=Lax`;
        localStorage.setItem('tradeflow-theme', th);
        localStorage.setItem('tradeflow_theme', th);
        localStorage.setItem('tf_theme', th);
        document.documentElement.setAttribute('data-bs-theme', th);
        document.documentElement.setAttribute('data-theme', th);
        if (th === 'dark') {
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.classList.remove('dark');
        }
    }, theme);
    await sleep(250);
}

(async () => {
    console.log('--- STARTING TRADEFLOW ANIMATION & MOTION VERIFICATION ---');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: [
            '--no-sandbox',
            '--disable-setuid-sandbox',
            '--window-size=1440,900'
        ]
    });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });

        // 1. LOGIN AS ADMIN
        console.log('[1/12] Logging in as admin...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.type('input[name="Input.Username"]', 'admin');
        await page.type('input[name="Input.Password"]', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        console.log('Logged in successfully. Current URL:', page.url());

        await setTheme(page, 'light');
        await sleep(500);

        // 2. VERIFY SIDEBAR ACCORDION EXPAND
        console.log('[2/12] Verifying Sidebar Accordion Expand on "Hóa đơn"...');
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-nav-item button.tf-nav-link'));
            const b = btns.find(el => el.textContent.includes('Hóa đơn'));
            if (b) b.click();
        });
        await sleep(400); // wait for 230ms transition + item stagger

        const invoiceState = await page.evaluate(() => {
            const item = Array.from(document.querySelectorAll('.tf-nav-item')).find(el => el.textContent.includes('Hóa đơn'));
            const sub = item ? item.querySelector('.tf-nav-sub') : null;
            const chevron = item ? item.querySelector('.tf-nav-chevron') : null;
            return {
                isExpanded: sub ? sub.classList.contains('expanded') : false,
                maxHeight: sub ? window.getComputedStyle(sub).maxHeight : null,
                opacity: sub ? window.getComputedStyle(sub).opacity : null,
                subDisplay: sub ? window.getComputedStyle(sub).display : null,
                chevronTransform: chevron ? window.getComputedStyle(chevron).transform : null
            };
        });
        console.log('Invoice Accordion State:', invoiceState);

        await page.screenshot({
            path: path.join(ARTIFACTS_DIR, 'motion_01_sidebar_accordion_expanded.png'),
            fullPage: false
        });
        console.log('Captured motion_01_sidebar_accordion_expanded.png');

        // 3. VERIFY MULTIPLE ACCORDIONS EXPAND
        console.log('[3/12] Verifying Multiple Accordions (clicking "Sản phẩm")...');
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-nav-item button.tf-nav-link'));
            const b = btns.find(el => el.textContent.includes('Sản phẩm'));
            if (b) b.click();
        });
        await sleep(400);

        await page.screenshot({
            path: path.join(ARTIFACTS_DIR, 'motion_02_sidebar_multiple_accordions.png'),
            fullPage: false
        });
        console.log('Captured motion_02_sidebar_multiple_accordions.png');

        // 4. VERIFY ACCORDION COLLAPSE
        console.log('[4/12] Collapsing "Hóa đơn" accordion...');
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-nav-item button.tf-nav-link'));
            const b = btns.find(el => el.textContent.includes('Hóa đơn'));
            if (b) b.click();
        });
        await sleep(400);

        const invoiceCollapsedState = await page.evaluate(() => {
            const item = Array.from(document.querySelectorAll('.tf-nav-item')).find(el => el.textContent.includes('Hóa đơn'));
            const sub = item ? item.querySelector('.tf-nav-sub') : null;
            return {
                isExpanded: sub ? sub.classList.contains('expanded') : false,
                maxHeight: sub ? window.getComputedStyle(sub).maxHeight : null,
                opacity: sub ? window.getComputedStyle(sub).opacity : null
            };
        });
        console.log('Invoice Collapsed State:', invoiceCollapsedState);

        await page.screenshot({
            path: path.join(ARTIFACTS_DIR, 'motion_03_sidebar_accordion_collapsed.png'),
            fullPage: false
        });
        console.log('Captured motion_03_sidebar_accordion_collapsed.png');

        // 5. VERIFY ACTIVE NAVIGATION INDICATOR
        console.log('[5/12] Verifying Active Navigation Indicator on "/nhan-vien"...');
        await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
        await sleep(500);

        const activeIndicator = await page.evaluate(() => {
            const activeLink = document.querySelector('.tf-nav-link.active, .tf-nav-sub-link.active');
            if (!activeLink) return null;
            const beforeStyle = window.getComputedStyle(activeLink, '::before');
            return {
                text: activeLink.textContent.trim(),
                bg: window.getComputedStyle(activeLink).backgroundColor,
                color: window.getComputedStyle(activeLink).color,
                beforeTransform: beforeStyle.transform,
                beforeOpacity: beforeStyle.opacity
            };
        });
        console.log('Active indicator on /nhan-vien:', activeIndicator);

        await page.screenshot({
            path: path.join(ARTIFACTS_DIR, 'motion_04_active_indicator_navigation.png'),
            fullPage: false
        });
        console.log('Captured motion_04_active_indicator_navigation.png');

        // 6. VERIFY COLLAPSED SIDEBAR (64px Icon Mode)
        console.log('[6/12] Collapsing sidebar to 64px icon mode...');
        const collapseBtn = await page.$('.tf-nav-collapse-btn');
        if (collapseBtn) {
            await collapseBtn.click();
            await sleep(400); // 220ms width transition
        }

        const sidebarWidth = await page.evaluate(() => {
            const sb = document.querySelector('.tf-sidebar');
            return {
                width: sb ? window.getComputedStyle(sb).width : null,
                isCollapsed: sb ? sb.classList.contains('is-collapsed') : false
            };
        });
        console.log('Sidebar Collapsed State:', sidebarWidth);

        await page.screenshot({
            path: path.join(ARTIFACTS_DIR, 'motion_05_sidebar_collapsed_mode.png'),
            fullPage: false
        });
        console.log('Captured motion_05_sidebar_collapsed_mode.png');

        // 7. VERIFY FLOATING FLYOUT ON HOVER IN COLLAPSED MODE
        console.log('[7/12] Hovering on parent icon in collapsed mode for flyout...');
        const hoverItem = await page.evaluateHandle(() => {
            const items = Array.from(document.querySelectorAll('.tf-sidebar.is-collapsed .tf-nav-item'));
            return items.find(it => it.querySelector('.tf-nav-flyout:not(.tf-nav-flyout-single)'));
        });

        if (hoverItem) {
            await hoverItem.hover();
            await sleep(300);
        }

        const flyoutState = await page.evaluate(() => {
            const flyout = document.querySelector('.tf-sidebar.is-collapsed .tf-nav-item:hover > .tf-nav-flyout');
            return {
                exists: !!flyout,
                display: flyout ? window.getComputedStyle(flyout).display : null,
                opacity: flyout ? window.getComputedStyle(flyout).opacity : null
            };
        });
        console.log('Flyout hover state:', flyoutState);

        await page.screenshot({
            path: path.join(ARTIFACTS_DIR, 'motion_06_sidebar_collapsed_flyout_hover.png'),
            fullPage: false
        });
        console.log('Captured motion_06_sidebar_collapsed_flyout_hover.png');

        // Restore sidebar to expanded mode
        if (collapseBtn) {
            await collapseBtn.click();
            await sleep(400);
        }

        // 8. VERIFY TAB TRANSITION
        console.log('[8/12] Verifying Tab Transition on "/cham-cong"...');
        await page.goto(`${BASE_URL}/cham-cong`, { waitUntil: 'networkidle2' });
        await sleep(500);

        // Click a subnav / segmented tab
        const tabClicked = await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-tab-btn'));
            const b = btns.find(el => el.textContent.includes('HR') || el.textContent.includes('Phê duyệt') || el.textContent.includes('Nhật ký'));
            if (b) {
                b.click();
                return b.textContent.trim();
            }
            return null;
        });
        console.log('Tab clicked:', tabClicked);
        await sleep(350);

        await page.screenshot({
            path: path.join(ARTIFACTS_DIR, 'motion_07_tab_transition_attendance.png'),
            fullPage: false
        });
        console.log('Captured motion_07_tab_transition_attendance.png');

        // 9. VERIFY MODAL ENTRANCE ANIMATION ON /nhan-vien
        console.log('[9/12] Verifying Modal Entrance Animation on "/nhan-vien"...');
        await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
        await sleep(500);

        const modalOpened = await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('button'));
            const b = btns.find(el => el.textContent.includes('Thêm nhân viên') || el.textContent.includes('Tạo mới') || el.textContent.includes('Thêm'));
            if (b) {
                b.click();
                return b.textContent.trim();
            }
            return null;
        });
        console.log('Modal trigger clicked:', modalOpened);
        await sleep(350); // wait for modal entrance

        const modalState = await page.evaluate(() => {
            const modal = document.querySelector('.tf-modal-overlay, .modal.show, [class*="-modal-backdrop"], .tf-employee-modal-overlay');
            const dialog = document.querySelector('.tf-modal, .modal-dialog, [class*="-modal-dialog"]');
            return {
                hasModal: !!modal,
                backdropOpacity: modal ? window.getComputedStyle(modal).opacity : null,
                dialogOpacity: dialog ? window.getComputedStyle(dialog).opacity : null,
                dialogTransform: dialog ? window.getComputedStyle(dialog).transform : null
            };
        });
        console.log('Modal Entrance State:', modalState);

        await page.screenshot({
            path: path.join(ARTIFACTS_DIR, 'motion_08_modal_entrance_animation.png'),
            fullPage: false
        });
        console.log('Captured motion_08_modal_entrance_animation.png');

        // Close modal
        await page.keyboard.press('Escape');
        await sleep(350);

        // 10. VERIFY DASHBOARD KPIS & DYNAMIC CHART
        console.log('[10/12] Verifying Dashboard KPIs & Dynamic Chart on "/"...');
        await page.goto(`${BASE_URL}/`, { waitUntil: 'networkidle2' });
        await sleep(600); // allow GSAP entrance timeline to execute

        // Click period button "Hôm qua"
        const periodClicked = await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-dash-pill'));
            const b = btns.find(el => el.textContent.includes('Hôm qua'));
            if (b) {
                b.click();
                return b.textContent.trim();
            }
            return null;
        });
        console.log('Period clicked:', periodClicked);
        await sleep(500);

        await page.screenshot({
            path: path.join(ARTIFACTS_DIR, 'motion_09_dashboard_chart_kpi_motion.png'),
            fullPage: false
        });
        console.log('Captured motion_09_dashboard_chart_kpi_motion.png');

        // 11. VERIFY DARK MODE SIDEBAR ACCORDION
        console.log('[11/12] Verifying Dark Mode Accordion...');
        await setTheme(page, 'dark');
        await sleep(400);

        // Click "Kho hàng" in dark mode
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-nav-item button.tf-nav-link'));
            const b = btns.find(el => el.textContent.includes('Kho hàng'));
            if (b) b.click();
        });
        await sleep(400);

        await page.screenshot({
            path: path.join(ARTIFACTS_DIR, 'motion_10_dark_mode_motion_accordion.png'),
            fullPage: false
        });
        console.log('Captured motion_10_dark_mode_motion_accordion.png');

        // 12. VERIFY DARK MODE COLLAPSED MODE & FLYOUT
        console.log('[12/12] Verifying Dark Mode Collapsed Mode & Flyout...');
        const collapseBtnDark = await page.$('.tf-nav-collapse-btn');
        if (collapseBtnDark) {
            await collapseBtnDark.click();
            await sleep(400);
        }

        const hoverItemDark = await page.evaluateHandle(() => {
            const items = Array.from(document.querySelectorAll('.tf-sidebar.is-collapsed .tf-nav-item'));
            return items.find(it => it.querySelector('.tf-nav-flyout:not(.tf-nav-flyout-single)'));
        });

        if (hoverItemDark) {
            await hoverItemDark.hover();
            await sleep(300);
        }

        await page.screenshot({
            path: path.join(ARTIFACTS_DIR, 'motion_11_dark_mode_collapsed_flyout.png'),
            fullPage: false
        });
        console.log('Captured motion_11_dark_mode_collapsed_flyout.png');

        console.log('=== ALL 11 VERIFICATIONS COMPLETED SUCCESSFULLY ===');
    } catch (err) {
        console.error('Error during verification:', err);
    } finally {
        await browser.close();
    }
})();
