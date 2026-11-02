const puppeteer = require('puppeteer');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('📱 Testing Mobile (390px) & Light Mode Valid Preview...');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 390, height: 844, isMobile: true, hasTouch: true });

        // 1. Sign In
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);

        // 2. Navigate to /tai-lieu
        await page.goto('http://127.0.0.1:5169/tai-lieu', { waitUntil: 'networkidle2' });
        await sleep(2500);

        // Capture Mobile Table View
        const mobileTablePath = path.join(ARTIFACT_DIR, 'document_center_390_mobile_actions.png');
        await page.screenshot({ path: mobileTablePath });
        console.log('📸 Captured Mobile Table:', mobileTablePath);

        // Click first more button or preview button on mobile
        const moreBtns = await page.$$('.tf-act-more');
        console.log(`Found ${moreBtns.length} more-btns on mobile.`);
        if (moreBtns.length > 0) {
            await moreBtns[0].click();
            await sleep(600);

            // Click "Xem chi tiết" in menu
            const menuItems = await page.$$('.tf-act-menu-item');
            console.log(`Found ${menuItems.length} menu items.`);
            if (menuItems.length > 0) {
                await menuItems[0].click();
                await sleep(1500);

                const mobilePreviewPath = path.join(ARTIFACT_DIR, 'preview_modal_390_mobile.png');
                await page.screenshot({ path: mobilePreviewPath });
                console.log('📸 Captured Mobile Preview Modal:', mobilePreviewPath);
            }
        }

        // Switch to Desktop 1440px Light Mode to capture valid PDF in Light Mode
        await page.setViewport({ width: 1440, height: 900 });
        await sleep(500);

        // Ensure Light Mode
        const isDark = await page.evaluate(() => document.documentElement.getAttribute('data-bs-theme') === 'dark' || document.documentElement.classList.contains('dark'));
        if (isDark) {
            await page.click('.tf-theme-toggle');
            await sleep(800);
        }

        // Filter Warehouse to get valid PDF
        await page.evaluate(() => {
            const pills = Array.from(document.querySelectorAll('.tf-doc-nav-pill'));
            const warehousePill = pills.find(p => p.textContent.includes('Kho bãi'));
            if (warehousePill) warehousePill.click();
        });
        await sleep(1200);

        const previewBtns = await page.$$('.tf-act-preview');
        if (previewBtns.length > 0) {
            await previewBtns[0].click();
            await sleep(2000);

            const lightValidPdfPath = path.join(ARTIFACT_DIR, 'preview_modal_valid_pdf_light.png');
            await page.screenshot({ path: lightValidPdfPath });
            console.log('📸 Captured Valid PDF Preview Light:', lightValidPdfPath);
        }

        console.log('✅ Mobile and Light tests completed!');
    } catch (err) {
        console.error('Error:', err);
    } finally {
        await browser.close();
    }
})();
