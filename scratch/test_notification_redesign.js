const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

(async () => {
    console.log('=== TESTING REDESIGNED NOTIFICATION CENTER ===');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    // 1. Login
    console.log('[1/6] Logging in as Admin...');
    await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('input[name="Input.Username"]', 'admin');
    await page.type('input[name="Input.Password"]', 'tradecore123');
    await Promise.all([
        page.waitForNavigation({ waitUntil: 'networkidle2' }),
        page.click('button[type="submit"]')
    ]);
    await new Promise(r => setTimeout(r, 1000));

    // Ensure Light Mode first
    await page.evaluate(() => {
        document.documentElement.setAttribute('data-bs-theme', 'light');
        document.documentElement.setAttribute('data-theme', 'light');
        document.documentElement.classList.remove('dark');
        document.cookie = 'tf_theme=light;path=/;max-age=31536000';
        document.cookie = 'tradeflow_theme=light;path=/;max-age=31536000';
        localStorage.setItem('tf_theme', 'light');
        localStorage.setItem('tradeflow_theme', 'light');
    });
    await new Promise(r => setTimeout(r, 500));

    // 2. Open Notification Dropdown
    console.log('[2/6] Opening Notification Center in Desktop Light Mode...');
    const bellBtn = await page.$('#tf-notification-menu-btn');
    if (!bellBtn) {
        console.error('FAIL: Bell button not found!');
        process.exit(1);
    }
    await bellBtn.click();
    await new Promise(r => setTimeout(r, 600));

    // Verify Panel Metrics
    const panelMetrics = await page.evaluate(() => {
        const panel = document.querySelector('.tf-notification-panel');
        if (!panel) return null;
        const rect = panel.getBoundingClientRect();
        const header = panel.querySelector('.tf-notification-header');
        const footer = panel.querySelector('.tf-notification-footer');
        const body = panel.querySelector('.tf-notification-body');
        const cards = Array.from(panel.querySelectorAll('.tf-notif-card'));

        return {
            width: rect.width,
            height: rect.height,
            right: window.innerWidth - rect.right,
            left: rect.left,
            top: rect.top,
            cardCount: cards.length,
            firstCard: cards.length > 0 ? {
                title: cards[0].querySelector('.tf-notif-card-title')?.innerText.trim(),
                typeTag: cards[0].querySelector('.tf-notif-type-tag')?.innerText.trim(),
                time: cards[0].querySelector('.tf-notif-time-badge')?.innerText.trim(),
                message: cards[0].querySelector('.tf-notif-card-message')?.innerText.trim(),
                isUnread: cards[0].classList.contains('is-unread'),
                hasMetaGrid: !!cards[0].querySelector('.tf-notif-meta-grid'),
                actions: Array.from(cards[0].querySelectorAll('.tf-btn-action, .tf-link-view-detail')).map(b => b.innerText.trim())
            } : null
        };
    });

    console.log('Panel Metrics:', panelMetrics);

    // Capture Desktop Light Mode
    await page.screenshot({
        path: path.join(ARTIFACT_DIR, 'notification_redesign_desktop_light.png'),
        clip: { x: 400, y: 0, width: 950, height: 850 }
    });
    console.log('  ✓ Saved notification_redesign_desktop_light.png');

    // 3. Switch to Desktop Dark Mode
    console.log('[3/6] Capturing Desktop Dark Mode...');
    await page.evaluate(() => {
        document.documentElement.setAttribute('data-bs-theme', 'dark');
        document.documentElement.setAttribute('data-theme', 'dark');
        document.documentElement.classList.add('dark');
        document.cookie = 'tf_theme=dark;path=/;max-age=31536000';
        document.cookie = 'tradeflow_theme=dark;path=/;max-age=31536000';
        localStorage.setItem('tf_theme', 'dark');
        localStorage.setItem('tradeflow_theme', 'dark');
    });
    await new Promise(r => setTimeout(r, 400));

    await page.screenshot({
        path: path.join(ARTIFACT_DIR, 'notification_redesign_desktop_dark.png'),
        clip: { x: 400, y: 0, width: 950, height: 850 }
    });
    console.log('  ✓ Saved notification_redesign_desktop_dark.png');

    // 4. Test Tablet Viewport (768px)
    console.log('[4/6] Testing Tablet Viewport (768px)...');
    await page.setViewport({ width: 768, height: 1024 });
    await new Promise(r => setTimeout(r, 400));

    await page.screenshot({
        path: path.join(ARTIFACT_DIR, 'notification_redesign_tablet_dark.png'),
        fullPage: false
    });
    console.log('  ✓ Saved notification_redesign_tablet_dark.png');

    // 5. Test Mobile Viewports (390px iPhone & 360px Android)
    console.log('[5/6] Testing Mobile Viewports (390px & 360px)...');
    await page.setViewport({ width: 390, height: 844 });
    await new Promise(r => setTimeout(r, 400));

    await page.screenshot({
        path: path.join(ARTIFACT_DIR, 'notification_redesign_mobile_390px.png'),
        fullPage: false
    });
    console.log('  ✓ Saved notification_redesign_mobile_390px.png');

    await page.setViewport({ width: 360, height: 740 });
    await new Promise(r => setTimeout(r, 400));

    await page.screenshot({
        path: path.join(ARTIFACT_DIR, 'notification_redesign_mobile_360px.png'),
        fullPage: false
    });
    console.log('  ✓ Saved notification_redesign_mobile_360px.png');

    // 6. Test Interactions: Refresh button & Mark all read
    console.log('[6/6] Testing Interactions (Refresh & Read All)...');
    await page.setViewport({ width: 1440, height: 900 });
    await new Promise(r => setTimeout(r, 400));

    // Test Refresh click
    const refreshBtn = await page.$('.tf-notif-refresh-btn');
    if (refreshBtn) {
        await refreshBtn.click();
        await new Promise(r => setTimeout(r, 800));
        console.log('  ✓ Clicked Refresh button successfully');
    }

    // Verify Mark all as read button
    const markReadBtn = await page.$('.tf-notif-mark-read-btn');
    if (markReadBtn) {
        await markReadBtn.click();
        await new Promise(r => setTimeout(r, 800));
        console.log('  ✓ Clicked Mark All Read button successfully');
    }

    const postReadState = await page.evaluate(() => {
        const allReadPill = document.querySelector('.tf-notif-all-read-pill');
        const unreadCards = document.querySelectorAll('.tf-notif-card.is-unread');
        return {
            hasAllReadPill: !!allReadPill,
            unreadCount: unreadCards.length
        };
    });
    console.log('Post-Read State:', postReadState);

    await browser.close();
    console.log('=== TEST SUITE COMPLETED SUCCESSFULLY ===');
})();
