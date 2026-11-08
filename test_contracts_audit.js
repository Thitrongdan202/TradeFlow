const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');
const http = require('http');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://127.0.0.1:5169';

async function sleep(ms) {
    return new Promise(r => setTimeout(r, ms));
}

(async () => {
    console.log('=== TRADEFLOW CONTRACT AUDIT & PDF EXPORT TEST ===');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1920,1080']
    });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1920, height: 1080 });

        // Step 1: Login
        console.log('1. Logging in as Admin...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('#login-submit-button')
        ]);
        console.log('   Logged in successfully.');

        // Step 2: Navigate to Contracts
        console.log('2. Navigating to Contracts List (/hop-dong)...');
        await page.goto(`${BASE_URL}/hop-dong`, { waitUntil: 'networkidle2' });
        await sleep(1000);

        const contractsInfo = await page.evaluate(() => {
            const rows = Array.from(document.querySelectorAll('table tbody tr'));
            return rows.map(r => {
                const cols = Array.from(r.querySelectorAll('td')).map(c => c.innerText.trim());
                return cols.join(' | ');
            });
        });
        console.log('   Commercial Contracts found:', contractsInfo);

        // Step 3: Get Cookies for authenticated fetch
        const cookies = await page.cookies();
        const cookieHeader = cookies.map(c => `${c.name}=${c.value}`).join('; ');

        // Helper to fetch binary PDF
        async function fetchPdf(urlPath, outputPath) {
            return new Promise((resolve, reject) => {
                const req = http.get(`${BASE_URL}${urlPath}`, {
                    headers: { 'Cookie': cookieHeader }
                }, (res) => {
                    if (res.statusCode !== 200) {
                        return reject(new Error(`Status ${res.statusCode} for ${urlPath}`));
                    }
                    const chunks = [];
                    res.on('data', chunk => chunks.push(chunk));
                    res.on('end', () => {
                        const buffer = Buffer.concat(chunks);
                        fs.writeFileSync(outputPath, buffer);
                        resolve({ size: buffer.length, header: buffer.slice(0, 8).toString() });
                    });
                });
                req.on('error', reject);
            });
        }

        // Test 1: B2B Contract PDF
        console.log('3. Downloading B2B Contract PDF (Contract ID 1)...');
        try {
            const b2bPdfPath = path.join(ARTIFACT_DIR, 'contract_b2b_export_current.pdf');
            const res1 = await fetchPdf('/api/contracts/1/export-pdf', b2bPdfPath);
            console.log(`   Saved: ${b2bPdfPath} (${res1.size} bytes, header: ${res1.header})`);
        } catch (e) {
            console.log('   Error downloading contract 1:', e.message);
        }

        // Test 2: Template B2B_SALE PDF
        console.log('4. Downloading Template B2B_SALE PDF...');
        try {
            const tplB2bPath = path.join(ARTIFACT_DIR, 'template_b2b_sale_export.pdf');
            const res2 = await fetchPdf('/api/contracts/templates/B2B_SALE/export-pdf', tplB2bPath);
            console.log(`   Saved: ${tplB2bPath} (${res2.size} bytes, header: ${res2.header})`);
        } catch (e) {
            console.log('   Error downloading template B2B_SALE:', e.message);
        }

        // Test 3: Template B2C_RETAIL PDF
        console.log('5. Downloading Template B2C_RETAIL PDF...');
        try {
            const tplB2cPath = path.join(ARTIFACT_DIR, 'template_b2c_retail_export.pdf');
            const res3 = await fetchPdf('/api/contracts/templates/B2C_RETAIL/export-pdf', tplB2cPath);
            console.log(`   Saved: ${tplB2cPath} (${res3.size} bytes, header: ${res3.header})`);
        } catch (e) {
            console.log('   Error downloading template B2C_RETAIL:', e.message);
        }

        // Test 4: Employment Contract PDF
        console.log('6. Downloading Employment Contract PDF (ID 1)...');
        try {
            const empPdfPath = path.join(ARTIFACT_DIR, 'contract_employment_export_current.pdf');
            const res4 = await fetchPdf('/api/contracts/employment/1/export-pdf', empPdfPath);
            console.log(`   Saved: ${empPdfPath} (${res4.size} bytes, header: ${res4.header})`);
        } catch (e) {
            console.log('   Error downloading employment contract 1:', e.message);
        }

        // Step 4: Capture UI Preview Screenshots
        console.log('7. Capturing UI Contract Detail Preview (/hop-dong/1)...');
        await page.goto(`${BASE_URL}/hop-dong/1`, { waitUntil: 'networkidle2' });
        await sleep(1000);
        const detailShotPath = path.join(ARTIFACT_DIR, 'contract_ui_preview_detail.png');
        await page.screenshot({ path: detailShotPath, fullPage: false });
        console.log(`   Saved UI Preview: ${detailShotPath}`);

        // Step 5: Capture Contract Template Gallery Preview
        console.log('8. Capturing Template Gallery (/hop-dong/mau-hop-dong)...');
        await page.goto(`${BASE_URL}/hop-dong/mau-hop-dong`, { waitUntil: 'networkidle2' });
        await sleep(1000);
        const galleryShotPath = path.join(ARTIFACT_DIR, 'contract_template_gallery_preview.png');
        await page.screenshot({ path: galleryShotPath, fullPage: false });
        console.log(`   Saved Gallery Preview: ${galleryShotPath}`);

    } catch (err) {
        console.error('Audit failed with error:', err);
    } finally {
        await browser.close();
    }
})();
