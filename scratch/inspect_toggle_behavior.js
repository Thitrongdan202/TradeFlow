const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900']
    });
    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    try {
        await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
        await new Promise(r => setTimeout(r, 400));

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

        await page.waitForSelector('.tf-sidebar', { timeout: 10000 });
        await new Promise(r => setTimeout(r, 1000));

        const getSidebarState = async () => {
            return await page.evaluate(() => {
                const sb = document.querySelector('.tf-sidebar');
                const label = document.querySelector('.tf-sidebar .tf-nav-label');
                const cs = window.getComputedStyle(sb);
                const labelCs = label ? window.getComputedStyle(label) : null;
                return {
                    width: Math.round(sb.getBoundingClientRect().width),
                    cssWidth: cs.width,
                    hasIsCollapsed: sb.classList.contains('is-collapsed'),
                    labelDisplay: labelCs ? labelCs.display : 'no-label',
                    localStorageVal: localStorage.getItem('tf_sidebar_collapsed')
                };
            });
        };

        console.log('Initial state:', await getSidebarState());

        // Click topbar hamburger
        console.log('\n--- Clicking Topbar Hamburger ---');
        await page.click('.tf-sidebar-toggle');
        await new Promise(r => setTimeout(r, 500));
        console.log('State after topbar hamburger click:', await getSidebarState());

        // Click topbar hamburger again
        console.log('\n--- Clicking Topbar Hamburger Again ---');
        await page.click('.tf-sidebar-toggle');
        await new Promise(r => setTimeout(r, 500));
        console.log('State after 2nd topbar hamburger click:', await getSidebarState());

        // Click bottom collapse button
        console.log('\n--- Clicking Bottom Collapse Button ---');
        await page.click('.tf-nav-collapse-btn');
        await new Promise(r => setTimeout(r, 500));
        console.log('State after bottom collapse click:', await getSidebarState());

    } catch (err) {
        console.error(err);
    } finally {
        await browser.close();
    }
})();
