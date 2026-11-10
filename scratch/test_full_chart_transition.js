const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    await page.setContent(`
        <html>
        <head>
            <style>
                .tf-chart-svg { width: 800px; height: 240px; }
            </style>
        </head>
        <body>
            <svg class="tf-chart-svg" viewBox="0 0 800 240">
                <path class="tf-chart-area-path" fill="rgba(37,99,235,0.2)" d="M 55.0 160.0 C 420.0 160.0, 420.0 60.0, 785.0 60.0 L 785.0 188.0 L 55.0 188.0 Z" />
                <path class="tf-chart-line-path" fill="none" stroke="#2563eb" stroke-width="2.5" d="M 55.0 160.0 C 420.0 160.0, 420.0 60.0, 785.0 60.0" />
            </svg>
            <script>
                // Complete Chart Smooth Transition Controller
                function initChartTransitionController() {
                    const lineEl = document.querySelector('.tf-chart-line-path');
                    const areaEl = document.querySelector('.tf-chart-area-path');
                    if (!lineEl) return;

                    let currentPoints = null;
                    let animId = null;
                    let isInternalUpdate = false;

                    function parsePathToPoints(d) {
                        if (!d) return [];
                        const pts = [];
                        const m = d.match(/M\s+([-\d.]+)\s+([-\d.]+)/);
                        if (m) pts.push({ x: parseFloat(m[1]), y: parseFloat(m[2]) });
                        const cRegex = /C\s+[-\d.]+\s+[-\d.]+,\s+[-\d.]+\s+[-\d.]+,\s+([-\d.]+)\s+([-\d.]+)/g;
                        let cm;
                        while ((cm = cRegex.exec(d)) !== null) {
                            pts.push({ x: parseFloat(cm[1]), y: parseFloat(cm[2]) });
                        }
                        const lRegex = /L\s+([-\d.]+)\s+([-\d.]+)/g;
                        let lm;
                        while ((lm = lRegex.exec(d)) !== null) {
                            const x = parseFloat(lm[1]);
                            const y = parseFloat(lm[2]);
                            if (pts.length === 0 || Math.abs(x - pts[pts.length-1].x) > 0.5) {
                                pts.push({ x, y });
                            }
                        }
                        return pts;
                    }

                    function sampleCurveY(points, x) {
                        if (!points || points.length === 0) return 188;
                        if (points.length === 1) return points[0].y;
                        if (x <= points[0].x) return points[0].y;
                        if (x >= points[points.length - 1].x) return points[points.length - 1].y;
                        for (let i = 0; i < points.length - 1; i++) {
                            if (x >= points[i].x && x <= points[i + 1].x) {
                                const dx = points[i + 1].x - points[i].x;
                                if (dx === 0) return points[i].y;
                                const t = (x - points[i].x) / dx;
                                return points[i].y + t * (points[i + 1].y - points[i].y);
                            }
                        }
                        return 188;
                    }

                    function resample(points, count = 48, left = 55.0, right = 785.0) {
                        const res = [];
                        for (let i = 0; i < count; i++) {
                            const x = left + (i * (right - left)) / (count - 1);
                            const y = sampleCurveY(points, x);
                            res.push({ x, y });
                        }
                        return res;
                    }

                    function buildPaths(points, bottom = 188.0) {
                        if (!points || points.length === 0) return { lineD: '', areaD: '' };
                        let lineD = 'M ' + points[0].x.toFixed(1) + ' ' + points[0].y.toFixed(1);
                        for (let i = 0; i < points.length - 1; i++) {
                            const p0 = points[i];
                            const p1 = points[i + 1];
                            const midX = ((p0.x + p1.x) / 2).toFixed(1);
                            lineD += ' C ' + midX + ' ' + p0.y.toFixed(1) + ', ' + midX + ' ' + p1.y.toFixed(1) + ', ' + p1.x.toFixed(1) + ' ' + p1.y.toFixed(1);
                        }
                        const areaD = lineD + ' L ' + points[points.length - 1].x.toFixed(1) + ' ' + bottom.toFixed(1) + ' L ' + points[0].x.toFixed(1) + ' ' + bottom.toFixed(1) + ' Z';
                        return { lineD, areaD };
                    }

                    // Initial state
                    currentPoints = resample(parsePathToPoints(lineEl.getAttribute('d')), 48);

                    const observer = new MutationObserver((mutations) => {
                        for (const m of mutations) {
                            if (isInternalUpdate) continue;
                            if (m.type === 'attributes' && m.attributeName === 'd') {
                                const oldD = m.oldValue;
                                const newD = lineEl.getAttribute('d');
                                if (!oldD || !newD || oldD === newD) continue;

                                const targetRaw = parsePathToPoints(newD);
                                if (targetRaw.length === 0) continue;

                                const startPts = currentPoints ? currentPoints : resample(parsePathToPoints(oldD), 48);
                                const targetPts = resample(targetRaw, 48);

                                if (animId) cancelAnimationFrame(animId);

                                const startTime = performance.now();
                                const duration = 320; // 320ms smooth transition
                                window.__tweenRuns = (window.__tweenRuns || 0) + 1;

                                function animate(now) {
                                    const elapsed = now - startTime;
                                    const t = Math.min(1, elapsed / duration);
                                    // easeOutCubic
                                    const ease = 1 - Math.pow(1 - t, 3);

                                    const framePts = [];
                                    for (let i = 0; i < 48; i++) {
                                        framePts.push({
                                            x: startPts[i].x,
                                            y: startPts[i].y + (targetPts[i].y - startPts[i].y) * ease
                                        });
                                    }
                                    currentPoints = framePts;
                                    const p = buildPaths(framePts);

                                    isInternalUpdate = true;
                                    lineEl.setAttribute('d', p.lineD);
                                    if (areaEl) areaEl.setAttribute('d', p.areaD);
                                    isInternalUpdate = false;

                                    if (t < 1) {
                                        animId = requestAnimationFrame(animate);
                                    } else {
                                        animId = null;
                                        isInternalUpdate = true;
                                        lineEl.setAttribute('d', newD);
                                        if (areaEl) {
                                            const finalArea = areaEl.getAttribute('d');
                                            // Keep area synced
                                        }
                                        isInternalUpdate = false;
                                        window.__animCompleted = true;
                                    }
                                }
                                animId = requestAnimationFrame(animate);
                            }
                        }
                    });

                    observer.observe(lineEl, {
                        attributes: true,
                        attributeFilter: ['d'],
                        attributeOldValue: true
                    });
                }

                initChartTransitionController();
            </script>
        </body>
        </html>
    `);

    console.log('Testing 1: Single update (30d -> 7d)...');
    await page.$eval('.tf-chart-line-path', el => {
        el.setAttribute('d', 'M 55.0 80.0 C 145.5 80.0, 145.5 140.0, 236.0 140.0 C 326.5 140.0, 326.5 30.0, 417.0 30.0 C 507.5 30.0, 507.5 110.0, 598.0 110.0 C 688.5 110.0, 688.5 50.0, 785.0 50.0');
    });

    await page.waitForFunction(() => window.__animCompleted === true);
    console.log('Single update tween completed successfully!');

    console.log('Testing 2: Rapid successive clicks (interruption / redirect test)...');
    window.__animCompleted = false;
    await page.evaluate(async () => {
        const line = document.querySelector('.tf-chart-line-path');
        // Click 1
        line.setAttribute('d', 'M 55.0 30.0 C 420.0 30.0, 420.0 170.0, 785.0 170.0');
        // Click 2 after 50ms (mid-animation)
        await new Promise(r => setTimeout(r, 50));
        line.setAttribute('d', 'M 55.0 120.0 C 420.0 120.0, 420.0 40.0, 785.0 40.0');
        // Click 3 after 50ms (mid-animation)
        await new Promise(r => setTimeout(r, 50));
        line.setAttribute('d', 'M 55.0 60.0 C 420.0 60.0, 420.0 100.0, 785.0 100.0');
    });

    await page.waitForFunction(() => window.__animCompleted === true);
    const tweenRuns = await page.evaluate(() => window.__tweenRuns);
    console.log('Total tween triggers recorded:', tweenRuns);
    const finalD = await page.$eval('.tf-chart-line-path', el => el.getAttribute('d'));
    console.log('Final D matches target click 3:', finalD.includes('M 55.0 60.0'));

    await browser.close();
})();
