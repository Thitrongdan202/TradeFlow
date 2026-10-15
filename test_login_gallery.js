const puppeteer = require('puppeteer');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function run() {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1400,900']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1400, height: 900 });

    page.on('console', msg => console.log(`[Browser ${msg.type()}] ${msg.text()}`));
    page.on('pageerror', err => console.error('Browser page error:', err));

    console.log('1. Navigating to login...');
    await page.goto('http://localhost:5169/Account/Login', { waitUntil: 'networkidle2' });

    console.log('2. Entering credentials...');
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    
    console.log('3. Submitting login form...');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    console.log('Logged in URL:', page.url());

    // Now test navigating directly to Contract Template Gallery
    console.log('4. Navigating to Contract Template Gallery (/hop-dong/mau-hop-dong)...');
    const t0 = Date.now();
    await page.goto('http://localhost:5169/hop-dong/mau-hop-dong', { waitUntil: 'domcontentloaded' });
    
    // Check loading overlay state every 100ms for 3.5 seconds
    for (let i = 0; i < 35; i++) {
        await new Promise(r => setTimeout(r, 100));
        const elapsed = Date.now() - t0;
        const state = await page.evaluate(() => {
            const startupLoader = document.getElementById('tf-startup-loader');
            const globalLoader = document.querySelector('.tf-loading-overlay:not(#tf-startup-loader)');
            const errBox = document.getElementById('tf-startup-error-box');
            const cards = document.querySelectorAll('.tf-gallery-card');
            return {
                startupExists: !!startupLoader,
                startupVisible: startupLoader ? (window.getComputedStyle(startupLoader).display !== 'none' && window.getComputedStyle(startupLoader).opacity !== '0' && window.getComputedStyle(startupLoader).visibility !== 'hidden') : false,
                startupClass: startupLoader ? startupLoader.className : '',
                errorBoxVisible: errBox ? window.getComputedStyle(errBox).display !== 'none' : false,
                globalLoaderExists: !!globalLoader,
                globalLoaderActive: globalLoader ? globalLoader.classList.contains('is-active') : false,
                cardCount: cards.length
            };
        });
        if (i % 5 === 0 || state.errorBoxVisible || state.cardCount > 0) {
            console.log(`[${elapsed}ms] State:`, state);
            if (state.cardCount > 0) {
                console.log(`Gallery cards rendered at ${elapsed}ms!`);
                break;
            }
        }
    }

    // Wait a brief moment for complete render
    await new Promise(r => setTimeout(r, 500));
    await page.screenshot({ path: path.join(ARTIFACT_DIR, 'gallery_loaded.png') });
    console.log('Captured gallery_loaded.png');

    // Test Hover Performance on Card 1
    console.log('5. Testing hover on Card 1...');
    const card1 = await page.$('.tf-gallery-card');
    if (card1) {
        const box = await card1.boundingBox();
        console.log('Card 1 bounds:', box);
        // Move mouse to card
        await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
        await new Promise(r => setTimeout(r, 200));
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'gallery_hover_card1.png') });
        console.log('Captured gallery_hover_card1.png');

        // Check computed styles and action buttons visibility on hover
        const hoverInfo = await page.evaluate(() => {
            const card = document.querySelector('.tf-gallery-card');
            const actions = card.querySelector('.tf-card-actions');
            const hint = card.querySelector('.tf-card-idle-hint');
            const btnPrev = card.querySelector('.tf-btn-preview');
            const btnComp = card.querySelector('.tf-btn-compose');
            return {
                cardTransform: window.getComputedStyle(card).transform,
                cardBoxShadow: window.getComputedStyle(card).boxShadow,
                actionsOpacity: window.getComputedStyle(actions).opacity,
                hintOpacity: window.getComputedStyle(hint).opacity,
                previewWidth: btnPrev ? btnPrev.offsetWidth : 0,
                composeWidth: btnComp ? btnComp.offsetWidth : 0,
                actionsWidth: actions.offsetWidth,
                actionsClientWidth: actions.clientWidth,
                actionsScrollWidth: actions.scrollWidth
            };
        });
        console.log('Hover state info:', hoverInfo);
    }

    await browser.close();
}

run().catch(console.error);
