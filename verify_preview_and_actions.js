const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('🚀 Starting Comprehensive Document Preview & Action Verification...');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1920,1080']
    });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1920, height: 1080 });

        // 1. Sign In
        console.log('🔑 Signing in to TradeFlow...');
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        console.log('✅ Logged in successfully. Current URL:', page.url());

        // 2. Navigate to Document Center (/tai-lieu)
        console.log('📄 Navigating to /tai-lieu...');
        await page.goto('http://127.0.0.1:5169/tai-lieu', { waitUntil: 'networkidle2' });
        await sleep(2500);

        // Ensure Dark Mode first for primary verification
        const isDark = await page.evaluate(() => document.documentElement.getAttribute('data-bs-theme') === 'dark' || document.documentElement.classList.contains('dark'));
        if (!isDark) {
            console.log('🌙 Switching to Dark Mode...');
            await page.click('.tf-theme-toggle');
            await sleep(800);
        }

        // 3. Test Action Column Dropdown Menu in Dark Mode
        console.log('\n🔘 1. Testing Action Column Dropdown Menu in Dark Mode...');
        const moreBtns = await page.$$('.tf-act-more');
        console.log(`Found ${moreBtns.length} more-action buttons.`);
        if (moreBtns.length > 0) {
            await moreBtns[0].click();
            await sleep(500);

            const menuMetrics = await page.evaluate(() => {
                const menu = document.querySelector('.tf-act-menu');
                if (!menu) return null;
                const rect = menu.getBoundingClientRect();
                const items = Array.from(menu.querySelectorAll('.tf-act-menu-item')).map(i => i.textContent.trim());
                return {
                    width: Math.round(rect.width),
                    height: Math.round(rect.height),
                    top: Math.round(rect.top),
                    left: Math.round(rect.left),
                    items
                };
            });
            console.log('Action Menu Metrics:', JSON.stringify(menuMetrics, null, 2));

            const actionMenuDarkPath = path.join(ARTIFACT_DIR, 'action_menu_open_dark.png');
            await page.screenshot({ path: actionMenuDarkPath });
            console.log('📸 Captured Action Menu Dark:', actionMenuDarkPath);

            // Test outside click closes menu
            console.log('🖱️ Clicking outside backdrop to test dismissal...');
            await page.evaluate(() => {
                const backdrop = document.querySelector('.tf-act-dropdown-backdrop');
                if (backdrop) backdrop.click();
            });
            await sleep(400);
            const menuClosed = await page.evaluate(() => !document.querySelector('.tf-act-menu'));
            console.log(`Action menu closed on outside click: ${menuClosed}`);
        }

        // 4. Test Preview for Demo / Placeholder File (Row 0)
        console.log('\n📄 2. Testing Preview of Demo Placeholder File...');
        const previewBtns = await page.$$('.tf-act-preview');
        console.log(`Found ${previewBtns.length} preview buttons in table.`);

        if (previewBtns.length > 0) {
            console.log('Clicking first preview button (demo placeholder)...');
            await previewBtns[0].click();
            await sleep(1500);

            // Measure modal centering and positioning
            const modalMetrics = await page.evaluate(() => {
                const backdrop = document.querySelector('.tf-modal-backdrop');
                const dialog = document.querySelector('.tf-preview-modal-dialog');
                const stateBox = document.querySelector('.tf-preview-state-box');
                const stateTitle = document.querySelector('.tf-preview-state-title');
                const stateDesc = document.querySelector('.tf-preview-state-desc');
                const scrollLocked = document.body.style.overflow === 'hidden';

                if (!backdrop || !dialog) return null;

                const backdropStyle = window.getComputedStyle(backdrop);
                const dialogRect = dialog.getBoundingClientRect();
                const viewportWidth = window.innerWidth;
                const viewportHeight = window.innerHeight;

                const dialogCenterX = dialogRect.left + dialogRect.width / 2;
                const dialogCenterY = dialogRect.top + dialogRect.height / 2;

                return {
                    backdropPosition: backdropStyle.position,
                    backdropZIndex: backdropStyle.zIndex,
                    scrollLocked,
                    dialogRect: {
                        top: Math.round(dialogRect.top),
                        left: Math.round(dialogRect.left),
                        width: Math.round(dialogRect.width),
                        height: Math.round(dialogRect.height)
                    },
                    viewport: { width: viewportWidth, height: viewportHeight },
                    dialogCenterX: Math.round(dialogCenterX),
                    viewportCenterX: Math.round(viewportWidth / 2),
                    dialogCenterY: Math.round(dialogCenterY),
                    viewportCenterY: Math.round(viewportHeight / 2),
                    hasStateBox: !!stateBox,
                    stateTitle: stateTitle ? stateTitle.textContent.trim() : null,
                    stateDesc: stateDesc ? stateDesc.textContent.trim() : null
                };
            });

            console.log('Modal Centering & Metrics (Placeholder):', JSON.stringify(modalMetrics, null, 2));

            const placeholderDarkPath = path.join(ARTIFACT_DIR, 'preview_modal_placeholder_warning_dark.png');
            await page.screenshot({ path: placeholderDarkPath });
            console.log('📸 Captured Placeholder Warning:', placeholderDarkPath);

            const centeredDarkPath = path.join(ARTIFACT_DIR, 'preview_modal_centered_dark.png');
            await page.screenshot({ path: centeredDarkPath });
            console.log('📸 Captured Centered Modal:', centeredDarkPath);

            // Test Close via ESC
            console.log('⌨️ Testing ESC key to close modal...');
            await page.keyboard.press('Escape');
            await sleep(600);
            let modalClosed = await page.evaluate(() => !document.querySelector('.tf-preview-modal-dialog'));
            console.log(`Modal closed via ESC: ${modalClosed}`);

            if (!modalClosed) {
                const closeBtn = await page.$('.tf-preview-modal-dialog .tf-modal-close-btn');
                if (closeBtn) {
                    await closeBtn.click();
                    await sleep(500);
                }
            }
        }

        // 5. Test Preview for Valid Document (Search for PNK or PXK in table)
        console.log('\n📄 3. Testing Preview of Valid Warehouse Document (PNK-2026-0001.pdf)...');
        // Let's filter or find row with PNK or filter category
        const rowData = await page.evaluate(() => {
            const trs = Array.from(document.querySelectorAll('.tf-doc-table tbody tr'));
            return trs.map((tr, index) => ({
                index,
                text: tr.textContent
            }));
        });

        let validRowIndex = rowData.findIndex(r => r.text.includes('PNK') || r.text.includes('PXK') || r.text.includes('Phiếu nhập kho'));
        console.log(`Found valid voucher row at index: ${validRowIndex}`);

        if (validRowIndex !== -1) {
            const tablePreviewBtns = await page.$$('.tf-act-preview');
            if (tablePreviewBtns.length > validRowIndex) {
                await tablePreviewBtns[validRowIndex].click();
                await sleep(2000);

                const validPreviewMetrics = await page.evaluate(() => {
                    const iframe = document.querySelector('.tf-preview-frame');
                    const dialog = document.querySelector('.tf-preview-modal-dialog');
                    const title = document.querySelector('.tf-preview-doc-title');
                    const badge = document.querySelector('.tf-badge-success');
                    const openTabBtn = document.querySelector('a.tf-btn-secondary[target="_blank"]');

                    return {
                        hasIframe: !!iframe,
                        iframeSrc: iframe ? iframe.getAttribute('src') : null,
                        titleText: title ? title.textContent.trim() : null,
                        hasSuccessBadge: !!badge,
                        hasOpenTabBtn: !!openTabBtn,
                        openTabHref: openTabBtn ? openTabBtn.getAttribute('href') : null
                    };
                });

                console.log('Valid Preview Metrics:', JSON.stringify(validPreviewMetrics, null, 2));

                const validPdfDarkPath = path.join(ARTIFACT_DIR, 'preview_modal_valid_pdf_dark.png');
                await page.screenshot({ path: validPdfDarkPath });
                console.log('📸 Captured Valid PDF Preview:', validPdfDarkPath);

                // Close modal
                const closeBtn = await page.$('.tf-preview-modal-dialog .tf-modal-close-btn');
                if (closeBtn) {
                    await closeBtn.click();
                    await sleep(600);
                }
            }
        }

        // 6. Test Light Mode
        console.log('\n☀️ 4. Testing Light Mode...');
        await page.click('.tf-theme-toggle');
        await sleep(800);

        // Open Action Menu in Light Mode
        const lightMoreBtns = await page.$$('.tf-act-more');
        if (lightMoreBtns.length > 0) {
            await lightMoreBtns[0].click();
            await sleep(500);

            const actionMenuLightPath = path.join(ARTIFACT_DIR, 'action_menu_open_light.png');
            await page.screenshot({ path: actionMenuLightPath });
            console.log('📸 Captured Action Menu Light:', actionMenuLightPath);

            // Close menu
            await page.evaluate(() => {
                const backdrop = document.querySelector('.tf-act-dropdown-backdrop');
                if (backdrop) backdrop.click();
            });
            await sleep(300);
        }

        // Open Preview Modal in Light Mode
        const lightPreviewBtns = await page.$$('.tf-act-preview');
        if (lightPreviewBtns.length > 0) {
            await lightPreviewBtns[0].click();
            await sleep(1500);

            const previewLightPath = path.join(ARTIFACT_DIR, 'preview_modal_centered_light.png');
            await page.screenshot({ path: previewLightPath });
            console.log('📸 Captured Preview Modal Light:', previewLightPath);

            const closeBtn = await page.$('.tf-preview-modal-dialog .tf-modal-close-btn');
            if (closeBtn) {
                await closeBtn.click();
                await sleep(500);
            }
        }

        // 7. Test Responsive Viewports (768px Tablet, 390px Mobile)
        console.log('\n📱 5. Testing Responsive Viewports...');
        // Tablet 768px
        await page.setViewport({ width: 768, height: 1024 });
        await sleep(600);
        const previewBtnsTablet = await page.$$('.tf-act-preview');
        if (previewBtnsTablet.length > 0) {
            await previewBtnsTablet[0].click();
            await sleep(1000);
            const tabletPath = path.join(ARTIFACT_DIR, 'preview_modal_768_tablet.png');
            await page.screenshot({ path: tabletPath });
            console.log('📸 Captured Tablet Preview:', tabletPath);

            const closeBtn = await page.$('.tf-preview-modal-dialog .tf-modal-close-btn');
            if (closeBtn) {
                await closeBtn.click();
                await sleep(500);
            }
        }

        // Mobile 390px
        await page.setViewport({ width: 390, height: 844 });
        await sleep(600);
        const mobileMore = await page.$$('.tf-act-more');
        if (mobileMore.length > 0) {
            await mobileMore[0].click();
            await sleep(500);
            const viewDetailItem = await page.$('.tf-act-menu-item');
            if (viewDetailItem) {
                await viewDetailItem.click();
                await sleep(1000);
                const mobilePath = path.join(ARTIFACT_DIR, 'preview_modal_390_mobile.png');
                await page.screenshot({ path: mobilePath });
                console.log('📸 Captured Mobile Preview:', mobilePath);

                const closeBtn = await page.$('.tf-preview-modal-dialog .tf-modal-close-btn');
                if (closeBtn) {
                    await closeBtn.click();
                    await sleep(500);
                }
            }
        }

        console.log('\n🎉 ALL VERIFICATION STEPS COMPLETED SUCCESSFULLY!');
    } catch (err) {
        console.error('❌ Verification Error:', err);
    } finally {
        await browser.close();
    }
})();
