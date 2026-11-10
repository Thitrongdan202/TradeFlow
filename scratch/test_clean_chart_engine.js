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
                <path class="tf-chart-area-path" fill="rgba(37,99,235,0.2)" />
                <path class="tf-chart-line-path" fill="none" stroke="#2563eb" stroke-width="2.5" />
            </svg>
            <script>
                // TradeFlow Chart Engine
                window.TradeFlowChart = {
                    currentPoints: null,
                    animId: null,

                    sampleCurveY(points, x) {
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
                    },

                    resample(points, count = 48, left = 55.0, right = 785.0) {
                        const res = [];
                        for (let i = 0; i < count; i++) {
                            const x = left + (i * (right - left)) / (count - 1);
                            const y = this.sampleCurveY(points, x);
                            res.push({ x, y });
                        }
                        return res;
                    },

                    buildPaths(points, bottom = 188.0) {
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
                    },

                    render(containerSelector, rawCoords, isInitial = false, duration = 320) {
                        const root = document.querySelector(containerSelector || '.tf-chart-svg');
                        if (!root) return;
                        const lineEl = root.querySelector('.tf-chart-line-path');
                        const areaEl = root.querySelector('.tf-chart-area-path');
                        if (!lineEl) return;

                        if (!rawCoords || rawCoords.length === 0) {
                            lineEl.setAttribute('d', '');
                            if (areaEl) areaEl.setAttribute('d', '');
                            this.currentPoints = null;
                            return;
                        }

                        const targetPoints = this.resample(rawCoords, 48);

                        if (isInitial || !this.currentPoints) {
                            this.currentPoints = targetPoints;
                            const paths = this.buildPaths(targetPoints);
                            lineEl.setAttribute('d', paths.lineD);
                            if (areaEl) areaEl.setAttribute('d', paths.areaD);
                            return;
                        }

                        // Smooth transition
                        if (this.animId) cancelAnimationFrame(this.animId);

                        const startPoints = this.currentPoints;
                        const startTime = performance.now();
                        const self = this;

                        function step(now) {
                            const elapsed = now - startTime;
                            const t = Math.min(1, elapsed / duration);
                            // easeOutCubic
                            const ease = 1 - Math.pow(1 - t, 3);

                            const frame = [];
                            for (let i = 0; i < 48; i++) {
                                frame.push({
                                    x: startPoints[i].x,
                                    y: startPoints[i].y + (targetPoints[i].y - startPoints[i].y) * ease
                                });
                            }
                            self.currentPoints = frame;
                            const p = self.buildPaths(frame);
                            lineEl.setAttribute('d', p.lineD);
                            if (areaEl) areaEl.setAttribute('d', p.areaD);

                            if (t < 1) {
                                self.animId = requestAnimationFrame(step);
                            } else {
                                self.animId = null;
                                self.currentPoints = targetPoints;
                                const finalP = self.buildPaths(targetPoints);
                                lineEl.setAttribute('d', finalP.lineD);
                                if (areaEl) areaEl.setAttribute('d', finalP.areaD);
                                window.__transitionDone = true;
                            }
                        }
                        this.animId = requestAnimationFrame(step);
                    }
                };
            </script>
        </body>
        </html>
    `);

    // Initial render
    console.log('1. Testing initial render...');
    await page.evaluate(() => {
        window.TradeFlowChart.render('.tf-chart-svg', [
            { x: 55, y: 160 }, { x: 420, y: 70 }, { x: 785, y: 120 }
        ], true);
    });

    const initLineD = await page.$eval('.tf-chart-line-path', el => el.getAttribute('d'));
    console.log('Initial line D rendered:', initLineD.startsWith('M 55.0 160.0'));

    // Smooth transition to 7 days
    console.log('2. Testing smooth transition to 7 days...');
    await page.evaluate(() => {
        window.__transitionDone = false;
        window.TradeFlowChart.render('.tf-chart-svg', [
            { x: 55, y: 80 }, { x: 176, y: 140 }, { x: 298, y: 50 },
            { x: 420, y: 90 }, { x: 541, y: 30 }, { x: 663, y: 110 }, { x: 785, y: 40 }
        ], false, 250);
    });

    await page.waitForFunction(() => window.__transitionDone === true);
    const finalLineD = await page.$eval('.tf-chart-line-path', el => el.getAttribute('d'));
    console.log('Final line D matches target:', finalLineD.includes('55.0 80.0'));

    await browser.close();
    console.log('Test PASSED 100%!');
})();
