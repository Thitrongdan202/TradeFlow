const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({ headless: 'new' });
    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    // 1. Login
    await page.goto('http://localhost:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.waitForSelector('#login-username');
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle2' }), page.click('button[type="submit"]')]);

    async function setTheme(theme) {
        await page.evaluate((t) => {
            document.documentElement.setAttribute('data-bs-theme', t);
            document.documentElement.setAttribute('data-theme', t);
            if (t === 'dark') {
                document.body.classList.add('theme-dark');
                document.body.classList.remove('theme-light');
            } else {
                document.body.classList.add('theme-light');
                document.body.classList.remove('theme-dark');
            }
        }, theme);
        await new Promise(r => setTimeout(r, 400));
    }

    // 2. Inspect /kho/hang-loi in Dark Mode
    await page.goto('http://localhost:5169/kho/hang-loi', { waitUntil: 'networkidle2' });
    await setTheme('dark');
    await new Promise(r => setTimeout(r, 1000));

    const quarantineData = await page.evaluate(() => {
        // Warning Banner
        const banner = document.querySelector('.tw-bg-amber-50.tw-border-amber-200');
        const bannerTitle = banner ? banner.querySelector('.tw-font-bold') : null;
        const bannerBody = banner ? banner.querySelector('div > div > span:last-child') : null;
        const bannerIcon = banner ? banner.querySelector('svg') : null;

        // Warehouse Nav Tabs
        const navContainer = document.querySelector('.tf-warehouse-nav-container');
        const navLinks = Array.from(document.querySelectorAll('.tf-warehouse-nav-container a')).map(a => ({
            text: a.innerText.trim(),
            href: a.getAttribute('href'),
            className: a.className,
            color: window.getComputedStyle(a).color,
            bgColor: window.getComputedStyle(a).backgroundColor,
            borderColor: window.getComputedStyle(a).borderColor
        }));

        // Segmented Status Tabs
        const segTabs = Array.from(document.querySelectorAll('.tf-segmented-tab')).map(btn => {
            const label = btn.querySelector('span:first-child');
            const badge = btn.querySelector('span:last-child');
            return {
                text: label ? label.innerText.trim() : btn.innerText.trim(),
                isActive: btn.classList.contains('is-active'),
                btnColor: window.getComputedStyle(btn).color,
                btnBg: window.getComputedStyle(btn).backgroundColor,
                badgeText: badge ? badge.innerText.trim() : null,
                badgeColor: badge ? window.getComputedStyle(badge).color : null,
                badgeBg: badge ? window.getComputedStyle(badge).backgroundColor : null
            };
        });

        return {
            warningBanner: {
                found: !!banner,
                bgColor: banner ? window.getComputedStyle(banner).backgroundColor : null,
                borderColor: banner ? window.getComputedStyle(banner).borderColor : null,
                textColor: banner ? window.getComputedStyle(banner).color : null,
                titleColor: bannerTitle ? window.getComputedStyle(bannerTitle).color : null,
                bodyColor: bannerBody ? window.getComputedStyle(bannerBody).color : null,
                iconColor: bannerIcon ? window.getComputedStyle(bannerIcon).color : null
            },
            navContainer: {
                bg: navContainer ? window.getComputedStyle(navContainer).backgroundColor : null,
                border: navContainer ? window.getComputedStyle(navContainer).borderColor : null
            },
            navLinks,
            segTabs
        };
    });

    console.log('--- QUARANTINE PAGE (DARK MODE) DIAGNOSIS ---');
    console.log(JSON.stringify(quarantineData, null, 2));

    await browser.close();
})();
