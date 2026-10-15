const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox'] });
    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });
    await page.goto('http://localhost:5169/Account/Login', { waitUntil: 'domcontentloaded' });
    await page.waitForSelector('#login-username');
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([page.waitForNavigation({ waitUntil: 'domcontentloaded' }), page.click('button[type="submit"]')]);
    
    await page.goto('http://localhost:5169/doi-tac/khach-hang', { waitUntil: 'domcontentloaded' });
    await new Promise(r => setTimeout(r, 1200));
    
    // Click 'Lập phiếu thu'
    const btn = await page.waitForSelector('button[title="Lập phiếu thu tiền khách hàng"]');
    await btn.click();
    await new Promise(r => setTimeout(r, 800));
    
    const info = await page.evaluate(() => {
        const backdrop = document.querySelector('.tf-modal-backdrop');
        const dialog = document.querySelector('.voucher-dialog');
        if (!dialog) return { error: 'No dialog found' };
        const dBox = dialog.getBoundingClientRect();
        const bBox = backdrop ? backdrop.getBoundingClientRect() : null;
        const dStyle = window.getComputedStyle(dialog);
        const bStyle = backdrop ? window.getComputedStyle(backdrop) : null;
        
        let parent = dialog.parentElement;
        const parentChain = [];
        while (parent && parent !== document.body) {
            const cs = window.getComputedStyle(parent);
            parentChain.push({
                tag: parent.tagName,
                class: parent.className,
                position: cs.position,
                transform: cs.transform,
                overflow: cs.overflow,
                filter: cs.filter,
                top: cs.top,
                marginTop: cs.marginTop
            });
            parent = parent.parentElement;
        }
        
        return {
            windowInnerHeight: window.innerHeight,
            windowScrollY: window.scrollY,
            dialogBox: {
                top: dBox.top,
                bottom: dBox.bottom,
                left: dBox.left,
                right: dBox.right,
                width: dBox.width,
                height: dBox.height
            },
            backdropBox: bBox ? {
                top: bBox.top,
                bottom: bBox.bottom,
                left: bBox.left,
                right: bBox.right,
                width: bBox.width,
                height: bBox.height
            } : null,
            dialogPosition: dStyle.position,
            dialogMargin: dStyle.margin,
            dialogTransform: dStyle.transform,
            dialogTop: dStyle.top,
            backdropPosition: bStyle ? bStyle.position : null,
            backdropTop: bStyle ? bStyle.top : null,
            parentChain
        };
    });
    
    console.log(JSON.stringify(info, null, 2));
    await page.screenshot({ path: 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901/investigate_voucher_dialog.png' });
    await browser.close();
})();
