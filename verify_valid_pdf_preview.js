const puppeteer = require('puppeteer');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('🚀 Testing Valid PDF Preview via "Kho bãi & Vận hành" Category...');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1920,1080']
    });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1920, height: 1080 });

        // 1. Sign In
        console.log('🔑 Signing in...');
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);

        // 2. Navigate to Document Center
        console.log('📄 Navigating to /tai-lieu...');
        await page.goto('http://127.0.0.1:5169/tai-lieu', { waitUntil: 'networkidle2' });
        await sleep(2000);

        // Ensure Dark Mode
        const isDark = await page.evaluate(() => document.documentElement.getAttribute('data-bs-theme') === 'dark' || document.documentElement.classList.contains('dark'));
        if (!isDark) {
            await page.click('.tf-theme-toggle');
            await sleep(800);
        }

        // 3. Click "Kho bãi & Vận hành" pill
        console.log('📦 Clicking "Kho bãi & Vận hành" pill...');
        await page.evaluate(() => {
            const pills = Array.from(document.querySelectorAll('.tf-doc-nav-pill'));
            const warehousePill = pills.find(p => p.textContent.includes('Kho bãi'));
            if (warehousePill) warehousePill.click();
        });
        await sleep(1500);

        // 4. Click Preview on first row (PXK-2026-0001.pdf)
        const previewBtns = await page.$$('.tf-act-preview');
        console.log(`Found ${previewBtns.length} preview buttons in warehouse filtered list.`);
        if (previewBtns.length > 0) {
            console.log('Opening Preview Modal for row 0 (PXK-2026-0001.pdf)...');
            await previewBtns[0].click();
            await sleep(2500);

            const modalInfo = await page.evaluate(() => {
                const dialog = document.querySelector('.tf-preview-modal-dialog');
                const title = document.querySelector('.tf-preview-title');
                const iframe = document.querySelector('.tf-preview-iframe');
                const stateBox = document.querySelector('.tf-preview-state-box');
                const openTabBtn = document.querySelector('a.tf-btn-outline-primary');
                const downloadBtn = document.querySelector('.tf-preview-modal-footer a[download]');

                return {
                    hasDialog: !!dialog,
                    title: title ? title.textContent.trim() : null,
                    hasIframe: !!iframe,
                    iframeSrc: iframe ? iframe.getAttribute('src') : null,
                    hasStateBox: !!stateBox,
                    openTabHref: openTabBtn ? openTabBtn.getAttribute('href') : null,
                    downloadHref: downloadBtn ? downloadBtn.getAttribute('href') : null
                };
            });

            console.log('Preview Modal Info for Valid PDF:', JSON.stringify(modalInfo, null, 2));

            const validPdfDarkPath = path.join(ARTIFACT_DIR, 'preview_modal_valid_pdf_dark.png');
            await page.screenshot({ path: validPdfDarkPath });
            console.log('📸 Captured Valid PDF Preview:', validPdfDarkPath);
        }

        console.log('✅ Done!');
    } catch (err) {
        console.error('Error:', err);
    } finally {
        await browser.close();
    }
})();
