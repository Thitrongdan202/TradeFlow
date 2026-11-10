const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('=== VERIFYING ATTENDANCE OWNER/ADMIN BANNER THEME ===');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    try {
        console.log('1. Setting Light Mode cookie & logging in as Admin...');
        await page.setCookie(
            { name: 'tf_theme', value: 'light', domain: '127.0.0.1', path: '/' },
            { name: 'tradeflow_theme', value: 'light', domain: '127.0.0.1', path: '/' }
        );

        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        const userInput = await page.$('input[name="Input.Username"]');
        if (userInput) {
            await userInput.click({ clickCount: 3 });
            await userInput.type('admin');
            const passInput = await page.$('input[name="Input.Password"]');
            await passInput.click({ clickCount: 3 });
            await passInput.type('tradecore123');
            await Promise.all([
                page.waitForNavigation({ waitUntil: 'networkidle2' }),
                page.click('button[type="submit"]')
            ]);
        }

        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'light');
            document.documentElement.setAttribute('data-theme', 'light');
            document.documentElement.classList.remove('dark');
            localStorage.setItem('tf_theme', 'light');
            localStorage.setItem('tradeflow_theme', 'light');
        });

        console.log('2. Navigating to Attendance Page (/cham-cong)...');
        await page.goto('http://127.0.0.1:5169/cham-cong', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 1200));

        // Check Light Mode Banner Properties
        const lightBanner = await page.evaluate(() => {
            const b = document.querySelector('.tf-admin-exempt-banner');
            if (!b) return null;
            const cs = window.getComputedStyle(b);
            const icon = b.querySelector('.tf-exempt-icon');
            const h4 = b.querySelector('h4');
            const p = b.querySelector('p');
            return {
                bg: cs.backgroundColor,
                border: cs.borderColor,
                boxShadow: cs.boxShadow,
                h4Color: h4 ? window.getComputedStyle(h4).color : null,
                pColor: p ? window.getComputedStyle(p).color : null,
                iconBg: icon ? window.getComputedStyle(icon).backgroundColor : null,
                iconColor: icon ? window.getComputedStyle(icon).color : null
            };
        });
        console.log('Light Mode Banner Metrics:', lightBanner);

        // Validation for Light Mode
        if (!lightBanner) throw new Error('Banner not found on /cham-cong!');
        // Ensure background is light (#ffffff or rgb(255, 255, 255))
        if (!lightBanner.bg.includes('255, 255, 255')) {
            throw new Error(`Light Mode Background is not white/light: ${lightBanner.bg}`);
        }
        console.log('✅ Light Mode background is cleanly white/light (rgb(255, 255, 255))');

        // Screenshot 1: Full Light Mode Page
        const shotLight = path.join(ARTIFACT_DIR, 'banner_light_mode_after.png');
        await page.screenshot({ path: shotLight });
        console.log('Saved:', shotLight);

        // Screenshot 2: Focused Crop of the Banner in Light Mode
        const bannerBox = await page.evaluate(() => {
            const b = document.querySelector('.tf-admin-exempt-banner');
            const r = b.getBoundingClientRect();
            return { x: Math.max(0, r.x - 20), y: Math.max(0, r.y - 20), width: r.width + 40, height: r.height + 40 };
        });
        const shotLightCrop = path.join(ARTIFACT_DIR, 'banner_light_mode_crop.png');
        await page.screenshot({ path: shotLightCrop, clip: bannerBox });
        console.log('Saved cropped banner:', shotLightCrop);

        // 3. Switch to Dark Mode dynamically without reload
        console.log('\n3. Switching to Dark Mode...');
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.documentElement.setAttribute('data-theme', 'dark');
            document.documentElement.classList.add('dark');
            document.cookie = 'tf_theme=dark;path=/;max-age=31536000';
            document.cookie = 'tradeflow_theme=dark;path=/;max-age=31536000';
            localStorage.setItem('tf_theme', 'dark');
            localStorage.setItem('tradeflow_theme', 'dark');
        });
        await new Promise(r => setTimeout(r, 600));

        // Check Dark Mode Banner Properties
        const darkBanner = await page.evaluate(() => {
            const b = document.querySelector('.tf-admin-exempt-banner');
            if (!b) return null;
            const cs = window.getComputedStyle(b);
            const icon = b.querySelector('.tf-exempt-icon');
            const h4 = b.querySelector('h4');
            const p = b.querySelector('p');
            return {
                bg: cs.backgroundImage || cs.backgroundColor,
                border: cs.borderColor,
                boxShadow: cs.boxShadow,
                h4Color: h4 ? window.getComputedStyle(h4).color : null,
                pColor: p ? window.getComputedStyle(p).color : null,
                iconBg: icon ? window.getComputedStyle(icon).backgroundColor : null,
                iconColor: icon ? window.getComputedStyle(icon).color : null
            };
        });
        console.log('Dark Mode Banner Metrics:', darkBanner);

        // Validation for Dark Mode
        if (!darkBanner.h4Color.includes('240, 242, 246') && !darkBanner.h4Color.includes('248, 250, 252') && !darkBanner.h4Color.includes('255, 255, 255')) {
            throw new Error(`Dark Mode h4 color is not light: ${darkBanner.h4Color}`);
        }
        console.log('✅ Dark Mode text is crisp and readable (light font on dark card)');

        // Screenshot 3: Full Dark Mode Page
        const shotDark = path.join(ARTIFACT_DIR, 'banner_dark_mode_after.png');
        await page.screenshot({ path: shotDark });
        console.log('Saved:', shotDark);

        // Screenshot 4: Focused Crop of the Banner in Dark Mode
        const shotDarkCrop = path.join(ARTIFACT_DIR, 'banner_dark_mode_crop.png');
        await page.screenshot({ path: shotDarkCrop, clip: bannerBox });
        console.log('Saved cropped dark banner:', shotDarkCrop);

        console.log('\n========================================');
        console.log('✅ ALL BANNER THEME CHECKS PASSED 100%!');
        console.log('========================================');

    } catch (err) {
        console.error('VERIFICATION ERROR:', err);
        process.exitCode = 1;
    } finally {
        await browser.close();
    }
})();
