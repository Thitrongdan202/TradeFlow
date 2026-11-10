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
                <path id="area" fill="rgba(37,99,235,0.2)" />
                <path id="line" fill="none" stroke="#2563eb" stroke-width="2.5" />
            </svg>
            <script>
                function sampleCurveY(points, x) {
                    if (points.length === 0) return 188;
                    if (points.length === 1) return points[0].y;
                    if (x <= points[0].x) return points[0].y;
                    if (x >= points[points.length - 1].x) return points[points.length - 1].y;
                    for (let i = 0; i < points.length - 1; i++) {
                        if (x >= points[i].x && x <= points[i + 1].x) {
                            const t = (x - points[i].x) / (points[i + 1].x - points[i].x);
                            return points[i].y + t * (points[i + 1].y - points[i].y);
                        }
                    }
                    return 188;
                }

                function resample(points, count = 48, left = 55, right = 785) {
                    const res = [];
                    for (let i = 0; i < count; i++) {
                        const x = left + (i * (right - left)) / (count - 1);
                        const y = sampleCurveY(points, x);
                        res.push({ x, y });
                    }
                    return res;
                }

                function buildPaths(points, bottom = 188) {
                    if (points.length === 0) return { lineD: '', areaD: '' };
                    let lineD = 'M ' + points[0].x.toFixed(1) + ' ' + points[0].y.toFixed(1);
                    for (let i = 0; i < points.length - 1; i++) {
                        const p0 = points[i];
                        const p1 = points[i + 1];
                        const midX = ((p0.x + p1.x) / 2).toFixed(1);
                        lineD += ' C ' + midX + ' ' + p0.y.toFixed(1) + ', ' + midX + ' ' + p1.y.toFixed(1) + ', ' + p1.x.toFixed(1) + ' ' + p1.y.toFixed(1);
                    }
                    const areaD = lineD + ' L ' + points[points.length - 1].x.toFixed(1) + ' ' + bottom + ' L ' + points[0].x.toFixed(1) + ' ' + bottom + ' Z';
                    return { lineD, areaD };
                }

                window.currentPoints = resample([{x: 55, y: 160}, {x: 400, y: 50}, {x: 785, y: 120}], 48);
                const initPaths = buildPaths(window.currentPoints);
                document.getElementById('line').setAttribute('d', initPaths.lineD);
                document.getElementById('area').setAttribute('d', initPaths.areaD);

                window.animateTo = function(newRawPoints, duration = 320) {
                    const startPoints = window.currentPoints;
                    const targetPoints = resample(newRawPoints, 48);
                    const startTime = performance.now();
                    window.__animFrames = 0;

                    function step(now) {
                        window.__animFrames++;
                        const elapsed = now - startTime;
                        const t = Math.min(1, elapsed / duration);
                        // easeOutCubic
                        const ease = 1 - Math.pow(1 - t, 3);

                        const current = [];
                        for (let i = 0; i < 48; i++) {
                            const x = startPoints[i].x;
                            const y = startPoints[i].y + (targetPoints[i].y - startPoints[i].y) * ease;
                            current.push({ x, y });
                        }
                        window.currentPoints = current;
                        const p = buildPaths(current);
                        document.getElementById('line').setAttribute('d', p.lineD);
                        document.getElementById('area').setAttribute('d', p.areaD);

                        if (t < 1) {
                            requestAnimationFrame(step);
                        } else {
                            window.__done = true;
                        }
                    }
                    requestAnimationFrame(step);
                };
            </script>
        </body>
        </html>
    `);

    // Trigger animation to 7 days data
    await page.evaluate(() => {
        const raw7d = [
            { x: 55, y: 140 }, { x: 176, y: 120 }, { x: 298, y: 40 },
            { x: 420, y: 60 }, { x: 541, y: 90 }, { x: 663, y: 30 }, { x: 785, y: 70 }
        ];
        window.animateTo(raw7d, 300);
    });

    await page.waitForFunction(() => window.__done === true);
    const frames = await page.evaluate(() => window.__animFrames);
    console.log('Animation completed with frames:', frames);

    const finalLineD = await page.$eval('#line', el => el.getAttribute('d'));
    console.log('Final line D exists and valid?', finalLineD.startsWith('M 55.0') && finalLineD.includes('C '));

    await browser.close();
})();
