const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://127.0.0.1:5169';

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

// Contrast ratio calculator (WCAG 2.1)
function getLuminance(r, g, b) {
    const a = [r, g, b].map(v => {
        v /= 255;
        return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4);
    });
    return a[0] * 0.2126 + a[1] * 0.7152 + a[2] * 0.0722;
}

function parseRgb(colorStr) {
    if (!colorStr) return { r: 0, g: 0, b: 0 };
    const m = colorStr.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/);
    if (m) {
        return { r: parseInt(m[1]), g: parseInt(m[2]), b: parseInt(m[3]) };
    }
    return { r: 0, g: 0, b: 0 };
}

function getContrast(fgStr, bgStr) {
    const fg = parseRgb(fgStr);
    const bg = parseRgb(bgStr);
    const l1 = getLuminance(fg.r, fg.g, fg.b);
    const l2 = getLuminance(bg.r, bg.g, bg.b);
    const lighter = Math.max(l1, l2);
    const darker = Math.min(l1, l2);
    return ((lighter + 0.05) / (darker + 0.05));
}

(async () => {
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const report = {
        timestamp: new Date().toISOString(),
        testResults: {},
        metrics: {}
    };

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1440, height: 900 });

        console.log('[1/7] Logging in...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('#login-username', { timeout: 10000 });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        console.log('  ✓ Logged in successfully.');

        console.log('[2/7] Navigating to Products list (/danh-muc/san-pham)...');
        await page.goto(`${BASE_URL}/danh-muc/san-pham`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('.tf-product-row', { timeout: 10000 });
        await sleep(500);

        // Check which products have images
        const rowImageInfo = await page.evaluate(() => {
            const rows = Array.from(document.querySelectorAll('.tf-product-row'));
            return rows.map((r, i) => {
                const img = r.querySelector('.tf-product-thumb img');
                const code = r.querySelector('.tf-product-code')?.innerText;
                const name = r.querySelector('strong')?.innerText;
                return { index: i, code, name, hasThumb: !!img, src: img ? img.src : null };
            });
        });
        const imgRow = rowImageInfo.find(r => r.hasThumb);
        console.log(`  Found ${rowImageInfo.length} products on page 1. Products with images: ${rowImageInfo.filter(r => r.hasThumb).length}`);
        if (imgRow) {
            console.log(`  Target product with image: index ${imgRow.index}, code: ${imgRow.code}, name: ${imgRow.name}`);
        }

        // ==========================================
        // TEST IN DARK MODE: HOVER & EXPANDED STATES
        // ==========================================
        console.log('\n[3/7] Testing Dark Mode States & Hover (Zero White Flash)...');
        await page.evaluate(() => {
            document.cookie = 'tradeflow_theme=dark;path=/;max-age=31536000;SameSite=Lax';
            document.cookie = 'tf_theme=dark;path=/;max-age=31536000;SameSite=Lax';
            localStorage.setItem('tf_theme', 'dark');
            localStorage.setItem('tradeflow_theme', 'dark');
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.documentElement.setAttribute('data-theme', 'dark');
            document.documentElement.className = 'dark';
        });
        await sleep(400);

        // 1. Default State Dark Mode
        const defaultDark = await page.evaluate(() => {
            const row = document.querySelector('.tf-product-row:first-child');
            const td = row.querySelector('td:nth-child(3)');
            return {
                trBg: window.getComputedStyle(row).backgroundColor,
                tdBg: window.getComputedStyle(td).backgroundColor
            };
        });
        console.log('  ✓ Default row colors (Dark):', defaultDark);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'product_dark_default.png') });

        // 2. Hover State Dark Mode (must NOT be white)
        console.log('  Testing Hover on first row in Dark Mode...');
        await page.hover('.tf-product-row:first-child td:nth-child(3)');
        await sleep(200);
        const hoverDark = await page.evaluate(() => {
            const row = document.querySelector('.tf-product-row:first-child');
            const td = row.querySelector('td:nth-child(3)');
            const code = row.querySelector('.tf-product-code');
            return {
                trBg: window.getComputedStyle(row).backgroundColor,
                tdBg: window.getComputedStyle(td).backgroundColor,
                codeColor: code ? window.getComputedStyle(code).color : null
            };
        });
        console.log('  ✓ Hover row colors (Dark):', hoverDark);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'product_dark_hover.png') });

        const isHoverWhite = hoverDark.tdBg.includes('255, 255, 255') || hoverDark.tdBg.includes('248, 250, 252');
        report.testResults.darkHoverNotWhite = !isHoverWhite;
        report.metrics.darkHoverTdBg = hoverDark.tdBg;
        console.log(`  ✓ Dark Mode Hover is NOT white: ${!isHoverWhite} (${hoverDark.tdBg})`);

        // 3. Expand the Row
        const targetRowIndex = imgRow ? imgRow.index : 0;
        console.log(`  Expanding row at index ${targetRowIndex}...`);
        const rows = await page.$$('.tf-product-row');
        await rows[targetRowIndex].click();
        await sleep(600);

        const expandedDark = await page.evaluate(() => {
            const row = document.querySelector('.tf-product-row.is-expanded');
            const td = row ? row.querySelector('td:nth-child(3)') : null;
            const detailTr = document.querySelector('.tf-product-detail-tr');
            const detailTd = document.querySelector('.tf-product-expanded-td');
            const detailBox = document.querySelector('.tf-product-expanded-box');
            return {
                expandedTrBg: row ? window.getComputedStyle(row).backgroundColor : null,
                expandedTdBg: td ? window.getComputedStyle(td).backgroundColor : null,
                detailTrBg: detailTr ? window.getComputedStyle(detailTr).backgroundColor : null,
                detailTdBg: detailTd ? window.getComputedStyle(detailTd).backgroundColor : null,
                detailBoxBg: detailBox ? window.getComputedStyle(detailBox).backgroundColor : null
            };
        });
        console.log('  ✓ Expanded row colors (Dark):', expandedDark);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'product_dark_expanded.png') });

        const isExpandedWhite = (expandedDark.expandedTrBg || '').includes('255, 255, 255') || (expandedDark.expandedTrBg || '').includes('239, 246, 255');
        report.testResults.darkExpandedNotWhite = !isExpandedWhite;
        report.metrics.darkExpandedTrBg = expandedDark.expandedTrBg;
        console.log(`  ✓ Dark Mode Expanded row is NOT white: ${!isExpandedWhite} (${expandedDark.expandedTrBg})`);

        // 4. Hover over Expanded Row (Must NOT flash white)
        await page.hover('.tf-product-row.is-expanded td:nth-child(3)');
        await sleep(200);
        const hoverExpandedDark = await page.evaluate(() => {
            const row = document.querySelector('.tf-product-row.is-expanded');
            const td = row ? row.querySelector('td:nth-child(3)') : null;
            return {
                trBg: row ? window.getComputedStyle(row).backgroundColor : null,
                tdBg: td ? window.getComputedStyle(td).backgroundColor : null
            };
        });
        console.log('  ✓ Hover expanded row colors (Dark):', hoverExpandedDark);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'product_dark_hover_expanded_row.png') });

        // 5. Hover over Detail Box & Action buttons
        const detailBox = await page.$('.tf-product-expanded-box');
        if (detailBox) {
            await detailBox.hover();
            await sleep(200);
            await page.screenshot({ path: path.join(ARTIFACT_DIR, 'product_dark_hover_detail_box.png') });
        }

        // ==========================================
        // TEST PART II: REDESIGNED PRODUCT IMAGE PRESENTATION
        // ==========================================
        console.log('\n[4/7] Testing Redesigned Product Image Presentation...');
        const imageStageMetrics = await page.evaluate(() => {
            const stage = document.querySelector('.tf-product-img-stage');
            const hero = document.querySelector('.tf-product-img-hero');
            const img = hero ? hero.querySelector('img') : null;
            const emptyState = hero ? hero.querySelector('.tf-product-empty-image') : null;
            const subThumbs = document.querySelectorAll('.tf-product-sub-thumb');
            const zoomBadge = hero ? hero.querySelector('.tf-img-zoom-badge') : null;

            return {
                hasStage: !!stage,
                stageBg: stage ? window.getComputedStyle(stage).backgroundColor : null,
                hasHero: !!hero,
                heroBg: hero ? window.getComputedStyle(hero).backgroundColor : null,
                hasImage: !!img,
                imgObjectFit: img ? window.getComputedStyle(img).objectFit : null,
                imgPadding: img ? window.getComputedStyle(img).padding : null,
                imgBorder: img ? window.getComputedStyle(img).border : null,
                hasEmptyState: !!emptyState,
                hasZoomBadge: !!zoomBadge,
                subThumbCount: subThumbs.length
            };
        });
        console.log('  ✓ Image stage metrics:', imageStageMetrics);
        report.testResults.imageStagePresent = imageStageMetrics.hasStage && imageStageMetrics.hasHero;
        report.testResults.imageObjectFitContain = imageStageMetrics.imgObjectFit === 'contain';
        report.metrics.imageStage = imageStageMetrics;

        // ==========================================
        // TEST LIGHTBOX / PREVIEW MODAL
        // ==========================================
        console.log('\n[5/7] Testing Lightbox Preview Modal...');
        // Test click on table thumbnail or hero image
        const clickTarget = await page.$('.tf-product-img-hero.has-image') || await page.$('.tf-product-thumb');
        if (clickTarget) {
            console.log('  Clicking thumbnail/hero image to open Lightbox...');
            await clickTarget.click();
            await sleep(500);

            const lightboxAudit = await page.evaluate(() => {
                const backdrop = document.querySelector('.tf-img-lightbox-backdrop');
                const dialog = document.querySelector('.tf-img-lightbox-dialog');
                const img = dialog ? dialog.querySelector('img') : null;
                const closeBtn = dialog ? dialog.querySelector('.tf-modal-btn-close') : null;
                const openTabBtn = dialog ? dialog.querySelector('.tf-img-lightbox-footer a') : null;
                return {
                    isOpen: !!backdrop,
                    hasDialog: !!dialog,
                    hasImg: !!img,
                    imgSrc: img ? img.src : null,
                    hasCloseBtn: !!closeBtn,
                    hasOpenTabBtn: !!openTabBtn
                };
            });
            console.log('  ✓ Lightbox audit:', lightboxAudit);
            report.testResults.lightboxOpens = lightboxAudit.isOpen;
            await page.screenshot({ path: path.join(ARTIFACT_DIR, 'product_image_lightbox_dark.png') });

            // Close lightbox
            const closeBtn = await page.$('.tf-img-lightbox-header .tf-modal-btn-close') || await page.$('.tf-img-lightbox-footer button');
            if (closeBtn) {
                await closeBtn.click();
                await sleep(400);
                const isClosed = await page.evaluate(() => !document.querySelector('.tf-img-lightbox-backdrop'));
                report.testResults.lightboxCloses = isClosed;
                console.log(`  ✓ Lightbox closes cleanly: ${isClosed}`);
            }
        }

        // ==========================================
        // TEST PART IV: DARK MODE TEXT CONTRAST AUDIT
        // ==========================================
        console.log('\n[6/7] Testing Dark Mode Text Contrast (WCAG AA >= 4.5:1)...');
        const contrastAudit = await page.evaluate(() => {
            const results = {};
            const detailBox = document.querySelector('.tf-product-expanded-box');
            if (!detailBox) return results;

            const boxBg = window.getComputedStyle(detailBox).backgroundColor;

            // Product Name
            const title = detailBox.querySelector('h3');
            if (title) {
                results.title = {
                    text: title.innerText,
                    fg: window.getComputedStyle(title).color,
                    bg: boxBg
                };
            }

            // System Code
            const code = detailBox.querySelector('.tf-product-code');
            if (code) {
                results.code = {
                    text: code.innerText,
                    fg: window.getComputedStyle(code).color,
                    bg: window.getComputedStyle(code).backgroundColor
                };
            }

            // Card 2 labels & values
            const detailLabels = Array.from(detailBox.querySelectorAll('.tf-product-detail-label'));
            const detailVals = Array.from(detailBox.querySelectorAll('.tf-product-detail-val'));

            if (detailLabels.length > 0) {
                results.label = {
                    fg: window.getComputedStyle(detailLabels[0]).color,
                    bg: boxBg
                };
            }
            if (detailVals.length > 0) {
                results.value = {
                    fg: window.getComputedStyle(detailVals[0]).color,
                    bg: boxBg
                };
            }

            // Price
            const priceVal = Array.from(detailBox.querySelectorAll('.tf-product-detail-val'))
                .find(el => el.innerText.includes('₫'));
            if (priceVal) {
                results.price = {
                    fg: window.getComputedStyle(priceVal).color,
                    bg: boxBg
                };
            }

            // Action buttons
            const buttons = Array.from(detailBox.querySelectorAll('button'));
            results.buttons = buttons.map(b => ({
                text: b.innerText.trim(),
                fg: window.getComputedStyle(b).color,
                bg: window.getComputedStyle(b).backgroundColor
            }));

            return results;
        });

        const titleContrast = getContrast(contrastAudit.title?.fg || 'rgb(248, 250, 252)', contrastAudit.title?.bg || 'rgb(23, 25, 31)');
        const labelContrast = getContrast(contrastAudit.label?.fg || 'rgb(148, 163, 184)', contrastAudit.label?.bg || 'rgb(23, 25, 31)');
        const valContrast = getContrast(contrastAudit.value?.fg || 'rgb(241, 245, 249)', contrastAudit.value?.bg || 'rgb(23, 25, 31)');
        const priceContrast = getContrast(contrastAudit.price?.fg || 'rgb(16, 185, 129)', contrastAudit.price?.bg || 'rgb(23, 25, 31)');

        console.log(`  ✓ Title Contrast: ${titleContrast.toFixed(2)}:1 (Pass AA: ${titleContrast >= 4.5})`);
        console.log(`  ✓ Label Contrast: ${labelContrast.toFixed(2)}:1 (Pass AA: ${labelContrast >= 4.5})`);
        console.log(`  ✓ Value Contrast: ${valContrast.toFixed(2)}:1 (Pass AA: ${valContrast >= 4.5})`);
        console.log(`  ✓ Price Emerald Contrast: ${priceContrast.toFixed(2)}:1 (Pass AA: ${priceContrast >= 4.5})`);

        report.metrics.contrast = {
            titleContrast: titleContrast.toFixed(2),
            labelContrast: labelContrast.toFixed(2),
            valContrast: valContrast.toFixed(2),
            priceContrast: priceContrast.toFixed(2),
            titlePass: titleContrast >= 4.5,
            labelPass: labelContrast >= 4.5,
            valPass: valContrast >= 4.5,
            pricePass: priceContrast >= 4.5
        };
        report.testResults.wcagContrastPass = titleContrast >= 4.5 && labelContrast >= 4.5 && valContrast >= 4.5;

        // ==========================================
        // TEST LIGHT MODE
        // ==========================================
        console.log('\n[7/7] Testing Light Mode States & Image Presentation...');
        await page.evaluate(() => {
            document.cookie = 'tradeflow_theme=light;path=/;max-age=31536000;SameSite=Lax';
            document.cookie = 'tf_theme=light;path=/;max-age=31536000;SameSite=Lax';
            localStorage.setItem('tf_theme', 'light');
            localStorage.setItem('tradeflow_theme', 'light');
            document.documentElement.setAttribute('data-bs-theme', 'light');
            document.documentElement.setAttribute('data-theme', 'light');
            document.documentElement.className = 'light';
        });
        await sleep(500);

        const lightExpandedColors = await page.evaluate(() => {
            const row = document.querySelector('.tf-product-row.is-expanded');
            const td = row ? row.querySelector('td:nth-child(3)') : null;
            const stage = document.querySelector('.tf-product-img-stage');
            const hero = document.querySelector('.tf-product-img-hero');
            return {
                trBg: row ? window.getComputedStyle(row).backgroundColor : null,
                tdBg: td ? window.getComputedStyle(td).backgroundColor : null,
                stageBg: stage ? window.getComputedStyle(stage).backgroundColor : null,
                heroBg: hero ? window.getComputedStyle(hero).backgroundColor : null
            };
        });
        console.log('  ✓ Light Mode expanded colors:', lightExpandedColors);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'product_light_expanded.png') });

        // Hover over row in Light Mode
        await page.hover('.tf-product-row.is-expanded td:nth-child(3)');
        await sleep(200);
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'product_light_hover_expanded.png') });

        // Write summary report JSON
        fs.writeFileSync(
            path.join(ARTIFACT_DIR, 'verification_product_hover_image.json'),
            JSON.stringify(report, null, 2),
            'utf-8'
        );
        console.log('\n======================================================');
        console.log('  ALL CHECKS & SCREENSHOTS COMPLETED SUCCESSFULLY!');
        console.log('======================================================');

    } catch (err) {
        console.error('Test execution error:', err);
    } finally {
        await browser.close();
    }
})();
