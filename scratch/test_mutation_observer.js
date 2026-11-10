const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    await page.setContent(`
        <html>
        <body>
            <svg id="svg" viewBox="0 0 800 240" width="800" height="240">
                <path id="line" fill="none" stroke="#2563eb" stroke-width="2.5" d="M 55.0 160.0 C 420.0 160.0, 420.0 60.0, 785.0 60.0" />
            </svg>
            <script>
                window.__tweenStarted = false;
                window.__tweenCompleted = false;

                const lineEl = document.getElementById('line');
                let isTweening = false;

                const observer = new MutationObserver((mutations) => {
                    for (const m of mutations) {
                        if (isTweening) continue;
                        if (m.type === 'attributes' && m.attributeName === 'd') {
                            const oldD = m.oldValue;
                            const newD = lineEl.getAttribute('d');
                            if (oldD && newD && oldD !== newD) {
                                window.__tweenStarted = true;
                                console.log('Intercepted d change from:', oldD, 'to:', newD);
                                // Hold oldD and start tween
                                isTweening = true;
                                lineEl.setAttribute('d', oldD);
                                
                                // Simulated 200ms tween
                                setTimeout(() => {
                                    lineEl.setAttribute('d', newD);
                                    isTweening = false;
                                    window.__tweenCompleted = true;
                                }, 200);
                            }
                        }
                    }
                });

                observer.observe(lineEl, {
                    attributes: true,
                    attributeFilter: ['d'],
                    attributeOldValue: true
                });
            </script>
        </body>
        </html>
    `);

    // Simulate Blazor updating the d attribute
    await page.$eval('#line', el => el.setAttribute('d', 'M 55.0 50.0 C 420.0 50.0, 420.0 140.0, 785.0 140.0'));

    await page.waitForFunction(() => window.__tweenStarted === true);
    console.log('Tween successfully started upon Blazor attribute update!');

    await page.waitForFunction(() => window.__tweenCompleted === true);
    console.log('Tween successfully finished!');

    await browser.close();
})();
