const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1440,900'] });
    const page = await browser.newPage();
    await page.setViewport({ width: 1440, height: 900 });

    // Set cookie for light theme
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

    await page.goto('http://127.0.0.1:5169/cham-cong', { waitUntil: 'networkidle2' });
    await new Promise(r => setTimeout(r, 1500));

    await page.screenshot({ path: 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901/banner_before_light_mode.png' });
    console.log('Screenshot saved: banner_before_light_mode.png');
    await browser.close();
})();
