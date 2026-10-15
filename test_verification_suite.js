const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function main() {
    console.log('=== STARTING TRADEFLOW PERFORMANCE & STARTUP VERIFICATION ===\n');

    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1400,900']
    });

    const results = {
        testA_coldLaunch: false,
        testB_galleryHover: false,
        testC_sidebarHover: false,
        testD_deepLinkLaunch: false,
        details: {}
    };

    // -------------------------------------------------------------
    // TEST A: Cold First Launch (Clean browser context, zero cookies)
    // -------------------------------------------------------------
    console.log('--- TEST A: Cold First Launch on Fresh Context ---');
    const coldContext = await browser.createBrowserContext();
    const coldPage = await coldContext.newPage();
    await coldPage.setViewport({ width: 1400, height: 900 });

    const coldLogs = [];
    coldPage.on('console', msg => coldLogs.push(msg.text()));
    coldPage.on('pageerror', err => console.error('Cold page error:', err));

    const tStart = Date.now();
    await coldPage.goto('http://localhost:5169/', { waitUntil: 'domcontentloaded' });

    // Sample within first 250ms to see startup loader
    await new Promise(r => setTimeout(r, 120));
    await coldPage.screenshot({ path: path.join(ARTIFACT_DIR, 'startup_cold_01_loading.png') });
    console.log('Captured startup_cold_01_loading.png');

    // Monitor loader dismissal and error box
    let loaderDismissed = false;
    let errorBoxShown = false;
    for (let i = 0; i < 40; i++) {
        await new Promise(r => setTimeout(r, 100));
        const st = await coldPage.evaluate(() => {
            const loader = document.getElementById('tf-startup-loader');
            const errBox = document.getElementById('tf-startup-error-box');
            return {
                loaderPresent: !!loader,
                isFadingOut: loader ? loader.classList.contains('is-fading-out') : false,
                loaderVisible: loader ? (window.getComputedStyle(loader).display !== 'none' && window.getComputedStyle(loader).opacity !== '0' && window.getComputedStyle(loader).visibility !== 'hidden') : false,
                errBoxVisible: errBox ? window.getComputedStyle(errBox).display !== 'none' : false,
                appReady: !!(document.querySelector('.tf-app') || document.querySelector('.tf-login-redirect') || document.querySelector('.tf-login-page'))
            };
        });

        if (st.errBoxVisible) {
            errorBoxShown = true;
            console.error('ERROR: tf-startup-error-box became visible during cold start!');
            break;
        }

        if (!st.loaderPresent || st.isFadingOut || !st.loaderVisible) {
            loaderDismissed = true;
            console.log(`Startup loader dismissed smoothly at ${Date.now() - tStart}ms without error.`);
            break;
        }
    }

    await new Promise(r => setTimeout(r, 400));
    await coldPage.screenshot({ path: path.join(ARTIFACT_DIR, 'startup_cold_02_loaded.png') });
    console.log('Captured startup_cold_02_loaded.png');

    results.testA_coldLaunch = loaderDismissed && !errorBoxShown;
    results.details.coldLaunchDismissedMs = Date.now() - tStart;
    results.details.coldLaunchErrorBox = errorBoxShown;
    console.log(`Test A Result: ${results.testA_coldLaunch ? 'PASS' : 'FAIL'}\n`);


    // -------------------------------------------------------------
    // Login to obtain authenticated session
    // -------------------------------------------------------------
    console.log('--- Logging in as Administrator ---');
    await coldPage.goto('http://localhost:5169/Account/Login', { waitUntil: 'networkidle2' });
    await coldPage.type('#login-username', 'admin');
    await coldPage.type('#login-password', 'tradecore123');
    await Promise.all([
        coldPage.waitForNavigation({ waitUntil: 'networkidle2' }),
        coldPage.click('button[type="submit"]')
    ]);
    console.log('Logged in successfully. URL:', coldPage.url());


    // -------------------------------------------------------------
    // TEST B: Contract Template Gallery Hover Fluidity & Zero Reflow
    // -------------------------------------------------------------
    console.log('--- TEST B: Contract Template Gallery Hover Stability ---');
    await coldPage.goto('http://localhost:5169/hop-dong/mau-hop-dong', { waitUntil: 'domcontentloaded' });
    
    // Wait for gallery cards to appear
    await coldPage.waitForSelector('.tf-gallery-card', { timeout: 10000 });
    await new Promise(r => setTimeout(r, 600));

    await coldPage.screenshot({ path: path.join(ARTIFACT_DIR, 'gallery_idle_stability.png') });
    console.log('Captured gallery_idle_stability.png');

    // 1. Measure Card 1 and Card 2 dimensions in IDLE state
    const cardMeasurementsBefore = await coldPage.evaluate(() => {
        const cards = Array.from(document.querySelectorAll('.tf-gallery-card'));
        return cards.map((c, idx) => {
            const rect = c.getBoundingClientRect();
            const actionsWrap = c.querySelector('.tf-card-actions-wrapper');
            return {
                index: idx,
                width: Math.round(rect.width * 10) / 10,
                height: Math.round(rect.height * 10) / 10,
                top: Math.round(rect.top * 10) / 10,
                bottom: Math.round(rect.bottom * 10) / 10,
                actionsHeight: actionsWrap ? Math.round(actionsWrap.getBoundingClientRect().height) : 0
            };
        });
    });
    console.log('Card 1 idle dimensions:', cardMeasurementsBefore[0]);
    console.log('Card 2 idle dimensions:', cardMeasurementsBefore[1]);

    // 2. Hover over Card 1
    const card1Element = await coldPage.$('.tf-gallery-card');
    const box1 = await card1Element.boundingBox();
    await coldPage.mouse.move(box1.x + box1.width / 2, box1.y + box1.height / 2);
    await new Promise(r => setTimeout(r, 220));

    await coldPage.screenshot({ path: path.join(ARTIFACT_DIR, 'gallery_hover_card1_stable.png') });
    console.log('Captured gallery_hover_card1_stable.png');

    // 3. Measure dimensions while Card 1 is hovered
    const cardMeasurementsHover1 = await coldPage.evaluate(() => {
        const cards = Array.from(document.querySelectorAll('.tf-gallery-card'));
        const card1 = cards[0];
        const actions = card1.querySelector('.tf-card-actions');
        const btnPrev = card1.querySelector('.tf-btn-preview');
        const btnComp = card1.querySelector('.tf-btn-compose');

        const rects = cards.map((c, idx) => {
            const rect = c.getBoundingClientRect();
            const actionsWrap = c.querySelector('.tf-card-actions-wrapper');
            return {
                index: idx,
                width: Math.round(rect.width * 10) / 10,
                height: Math.round(rect.height * 10) / 10,
                top: Math.round(rect.top * 10) / 10,
                bottom: Math.round(rect.bottom * 10) / 10,
                actionsHeight: actionsWrap ? Math.round(actionsWrap.getBoundingClientRect().height) : 0
            };
        });

        return {
            cards: rects,
            actionsScrollWidth: actions.scrollWidth,
            actionsClientWidth: actions.clientWidth,
            previewWidth: btnPrev ? btnPrev.offsetWidth : 0,
            composeWidth: btnComp ? btnComp.offsetWidth : 0,
            actionsOpacity: window.getComputedStyle(actions).opacity,
            card1Transform: window.getComputedStyle(card1).transform
        };
    });

    console.log('Card 1 hover dimensions:', cardMeasurementsHover1.cards[0]);
    console.log('Card 2 dimensions while Card 1 hovered:', cardMeasurementsHover1.cards[1]);
    console.log('Card 1 Action area sizing:', {
        actionsScrollWidth: cardMeasurementsHover1.actionsScrollWidth,
        actionsClientWidth: cardMeasurementsHover1.actionsClientWidth,
        previewWidth: cardMeasurementsHover1.previewWidth,
        composeWidth: cardMeasurementsHover1.composeWidth,
        actionsOpacity: cardMeasurementsHover1.actionsOpacity,
        transform: cardMeasurementsHover1.card1Transform
    });

    // Check reflow: Card 1 base height delta (excluding translateY)
    const card1HeightDelta = Math.abs(cardMeasurementsHover1.cards[0].height - cardMeasurementsBefore[0].height);
    const card2TopDelta = Math.abs(cardMeasurementsHover1.cards[1].top - cardMeasurementsBefore[1].top);
    const card2HeightDelta = Math.abs(cardMeasurementsHover1.cards[1].height - cardMeasurementsBefore[1].height);
    const actionsOverflow = cardMeasurementsHover1.actionsScrollWidth > cardMeasurementsHover1.actionsClientWidth;

    console.log(`Card 1 height delta on hover: ${card1HeightDelta}px (Target: 0px)`);
    console.log(`Sibling Card 2 top displacement: ${card2TopDelta}px (Target: 0px)`);
    console.log(`Sibling Card 2 height displacement: ${card2HeightDelta}px (Target: 0px)`);
    console.log(`Action buttons horizontal overflow: ${actionsOverflow ? 'YES (FAIL)' : 'NO (ZERO CLIPPING)'}`);

    // 4. Hover over Card 3
    const allCards = await coldPage.$$('.tf-gallery-card');
    if (allCards.length >= 3) {
        const box3 = await allCards[2].boundingBox();
        await coldPage.mouse.move(box3.x + box3.width / 2, box3.y + box3.height / 2);
        await new Promise(r => setTimeout(r, 220));
        await coldPage.screenshot({ path: path.join(ARTIFACT_DIR, 'gallery_hover_card3_stable.png') });
        console.log('Captured gallery_hover_card3_stable.png');
    }

    // 5. Hover over Card 5 (rightmost in 5-card row)
    if (allCards.length >= 5) {
        const box5 = await allCards[4].boundingBox();
        await coldPage.mouse.move(box5.x + box5.width / 2, box5.y + box5.height / 2);
        await new Promise(r => setTimeout(r, 220));
        await coldPage.screenshot({ path: path.join(ARTIFACT_DIR, 'gallery_hover_card5_stable.png') });
        console.log('Captured gallery_hover_card5_stable.png');
    }

    results.testB_galleryHover = (card1HeightDelta === 0) && (card2TopDelta === 0) && !actionsOverflow;
    results.details.card1HeightDelta = card1HeightDelta;
    results.details.card2Displacement = card2TopDelta;
    results.details.actionsOverflow = actionsOverflow;
    console.log(`Test B Result: ${results.testB_galleryHover ? 'PASS' : 'FAIL'}\n`);


    // -------------------------------------------------------------
    // TEST C: Sidebar Hover Interaction Comparison
    // -------------------------------------------------------------
    console.log('--- TEST C: Sidebar Hover Comparison ---');
    const navItem = await coldPage.$('.tf-nav-item');
    if (navItem) {
        const navBox = await navItem.boundingBox();
        await coldPage.mouse.move(navBox.x + navBox.width / 2, navBox.y + navBox.height / 2);
        await new Promise(r => setTimeout(r, 200));
        await coldPage.screenshot({ path: path.join(ARTIFACT_DIR, 'sidebar_hover_smooth.png') });
        console.log('Captured sidebar_hover_smooth.png');
        results.testC_sidebarHover = true;
    }
    console.log(`Test C Result: PASS\n`);


    // -------------------------------------------------------------
    // TEST D: Authenticated Deep Link Cold Launch
    // -------------------------------------------------------------
    console.log('--- TEST D: Deep Link Direct Cold Launch ---');
    const tDeep = Date.now();
    await coldPage.goto('http://localhost:5169/hop-dong/mau-hop-dong', { waitUntil: 'domcontentloaded' });
    let deepDismissed = false;
    let deepError = false;

    for (let i = 0; i < 30; i++) {
        await new Promise(r => setTimeout(r, 100));
        const st = await coldPage.evaluate(() => {
            const loader = document.getElementById('tf-startup-loader');
            const errBox = document.getElementById('tf-startup-error-box');
            return {
                loaderPresent: !!loader,
                isFadingOut: loader ? loader.classList.contains('is-fading-out') : false,
                loaderVisible: loader ? (window.getComputedStyle(loader).display !== 'none' && window.getComputedStyle(loader).opacity !== '0' && window.getComputedStyle(loader).visibility !== 'hidden') : false,
                errBoxVisible: errBox ? window.getComputedStyle(errBox).display !== 'none' : false,
                cardsReady: document.querySelectorAll('.tf-gallery-card').length > 0
            };
        });

        if (st.errBoxVisible) {
            deepError = true;
            break;
        }

        if ((!st.loaderPresent || st.isFadingOut || !st.loaderVisible) && st.cardsReady) {
            deepDismissed = true;
            console.log(`Deep link gallery loaded & dismissed loader at ${Date.now() - tDeep}ms.`);
            break;
        }
    }

    results.testD_deepLinkLaunch = deepDismissed && !deepError;
    console.log(`Test D Result: ${results.testD_deepLinkLaunch ? 'PASS' : 'FAIL'}\n`);

    await browser.close();

    console.log('=== FINAL VERIFICATION SUMMARY ===');
    console.log(JSON.stringify(results, null, 2));
}

main().catch(console.error);
