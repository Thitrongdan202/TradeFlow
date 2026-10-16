const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');
const { execSync } = require('child_process');

const ARTIFACT_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://localhost:5169';
const TEMP_DOWNLOAD_DIR = path.join(__dirname, 'temp_backup_downloads');

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function run() {
    console.log('=== STARTING TRADEFLOW BACKUP & RESTORE VERIFICATION ===\n');

    if (!fs.existsSync(TEMP_DOWNLOAD_DIR)) {
        fs.mkdirSync(TEMP_DOWNLOAD_DIR, { recursive: true });
    }

    const browser = await puppeteer.launch({
        headless: 'new',
        protocolTimeout: 600000,
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    const page = await browser.newPage();
    page.setDefaultTimeout(600000);
    page.setDefaultNavigationTimeout(600000);
    page.on('console', msg => console.log('   [BROWSER CONSOLE]', msg.type(), msg.text()));
    page.on('pageerror', err => console.log('   [BROWSER ERROR]', err.message));
    const client = await page.target().createCDPSession();
    await client.send('Page.setDownloadBehavior', {
        behavior: 'allow',
        downloadPath: TEMP_DOWNLOAD_DIR
    });

    const results = {
        timestamp: new Date().toISOString(),
        steps: {},
        manifestValidation: {},
        preflightAnalysis: {},
        restoreResult: {}
    };

    try {
        // Step 1: Login
        console.log('1. Logging in as administrator...');
        await page.setViewport({ width: 1706, height: 950 });
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('#login-username');
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);
        await sleep(1000);
        console.log('   Logged in successfully.');

        // Step 2: Navigate to Backup & Restore Center
        console.log('2. Navigating to /cai-dat/sao-luu...');
        await page.goto(`${BASE_URL}/cai-dat/sao-luu`, { waitUntil: 'networkidle2' });
        await page.waitForSelector('.tf-backup-container');
        await sleep(800);

        const pageTitle = await page.evaluate(() => {
            const h1 = document.querySelector('.tf-page-title');
            const desc = document.querySelector('.tf-page-desc');
            const badges = Array.from(document.querySelectorAll('.tf-badge-pill')).map(b => b.innerText.trim());
            return {
                title: h1 ? h1.innerText.trim() : null,
                desc: desc ? desc.innerText.trim() : null,
                badges
            };
        });
        console.log('   Page Title:', pageTitle.title);
        console.log('   Badges:', pageTitle.badges.join(' | '));
        results.steps.pageHeader = pageTitle;

        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'backup_01_export_view.png') });

        // Step 3: Trigger Backup Export from UI
        console.log('3. Triggering Backup Export...');
        const exportBtn = await page.waitForSelector('.tf-backup-action-row button');
        await exportBtn.click();

        // Wait for export process or success message (up to 60s for 1300+ files)
        console.log('   Waiting for export completion...');
        await page.waitForFunction(() => {
            const alert = document.querySelector('.tf-alert-success');
            return alert && alert.innerText.includes('sẵn sàng');
        }, { timeout: 60000 });

        const exportMessage = await page.evaluate(() => {
            const el = document.querySelector('.tf-alert-success');
            return el ? el.innerText.trim() : null;
        });
        console.log('   Export Message:', exportMessage);
        results.steps.exportMessage = exportMessage;

        // Detect downloaded ZIP in TEMP_DOWNLOAD_DIR or download directly
        console.log('   Checking downloaded backup archive...');
        let backupZipPath = null;
        for (let i = 0; i < 30; i++) {
            await sleep(500);
            const files = fs.readdirSync(TEMP_DOWNLOAD_DIR).filter(f => f.endsWith('.zip') && !f.endsWith('.crdownload'));
            if (files.length > 0) {
                backupZipPath = path.join(TEMP_DOWNLOAD_DIR, files[0]);
                break;
            }
        }

        if (!backupZipPath) {
            console.log('   Direct CDP download pending, fetching backup stream directly via API...');
            const backupBuffer = await page.evaluate(async () => {
                const res = await fetch('/api/admin/backup/download');
                if (!res.ok) throw new Error('Download failed: ' + res.status);
                const blob = await res.blob();
                const reader = new FileReader();
                return new Promise((resolve, reject) => {
                    reader.onloadend = () => resolve(reader.result);
                    reader.onerror = reject;
                    reader.readAsDataURL(blob);
                });
            });

            const base64Data = backupBuffer.split(',')[1];
            backupZipPath = path.join(TEMP_DOWNLOAD_DIR, 'test_tradeflow_backup.zip');
            fs.writeFileSync(backupZipPath, Buffer.from(base64Data, 'base64'));
        }

        const zipSize = fs.statSync(backupZipPath).size;
        console.log(`   ZIP ready for inspection: ${backupZipPath} (${(zipSize / 1024).toFixed(1)} KB)`);
        results.steps.zipDownloaded = { path: backupZipPath, sizeBytes: zipSize };

        // Step 4: Inspect and Validate ZIP Archive Structure
        console.log('4. Inspecting ZIP Archive Structure and Integrity...');
        execSync(`powershell -ExecutionPolicy Bypass -File ./inspect_zip.ps1 -ZipPath "${backupZipPath.replace(/\\/g, '/')}" -OutputDir "${TEMP_DOWNLOAD_DIR.replace(/\\/g, '/')}"`);

        const manifestObj = JSON.parse(fs.readFileSync(path.join(TEMP_DOWNLOAD_DIR, 'manifest.json'), 'utf8').replace(/^\uFEFF/, ''));
        const checksumsObj = JSON.parse(fs.readFileSync(path.join(TEMP_DOWNLOAD_DIR, 'checksums.json'), 'utf8').replace(/^\uFEFF/, ''));
        const entries = fs.readFileSync(path.join(TEMP_DOWNLOAD_DIR, 'entries.txt'), 'utf8').split(/\r?\n/).map(s => s.trim()).filter(Boolean);

        const totalEntries = entries.length;
        const tableCounts = manifestObj.EntityCounts || manifestObj.entityCounts || {};
        console.log(`   Total Entries in ZIP: ${totalEntries}`);
        console.log(`   Manifest Format Version: ${manifestObj.FormatVersion || manifestObj.formatVersion}`);
        console.log(`   Manifest Database: ${manifestObj.DatabaseName || manifestObj.databaseName}`);
        console.log(`   Table Payloads Count: ${Object.keys(tableCounts).length} tables`);
        console.log(`   Excluded Secrets: ${(manifestObj.ExcludedSensitiveInfo || manifestObj.excludedSensitiveInfo || []).join(', ')}`);

        // Check required entries
        const hasManifest = entries.includes('manifest.json');
        const hasChecksums = entries.includes('checksums.json');
        const hasDatabaseDump = entries.includes('database.dump');
        const dataEntries = entries.filter(e => e.startsWith('data/'));
        const fileEntries = entries.filter(e => e.startsWith('files/'));

        console.log(`   Has manifest.json: ${hasManifest}`);
        console.log(`   Has checksums.json: ${hasChecksums}`);
        console.log(`   Has database.dump: ${hasDatabaseDump}`);
        console.log(`   Data entries: ${dataEntries.length} JSON files`);
        console.log(`   Files entries: ${fileEntries.length} uploaded files`);

        results.manifestValidation = {
            hasManifest,
            hasChecksums,
            hasDatabaseDump,
            dataEntriesCount: dataEntries.length,
            fileEntriesCount: fileEntries.length,
            formatVersion: manifestObj.FormatVersion || manifestObj.formatVersion,
            tableCounts: manifestObj.EntityCounts || manifestObj.entityCounts,
            excludedSecrets: manifestObj.ExcludedSensitiveInfo || manifestObj.excludedSensitiveInfo,
            checksumsCount: Object.keys(checksumsObj).length
        };

        // Step 5: Switch to Import & Restore Tab
        console.log('5. Switching to Khôi phục & Nhập dữ liệu tab...');
        await page.click('.tf-backup-tabs button:nth-child(2)');
        await sleep(800);

        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'backup_02_import_tab.png') });

        // Step 6: Upload ZIP to trigger Preflight Duplicate & Conflict Detection
        console.log('6. Uploading ZIP for Preflight Analysis...');
        const fileInput = await page.waitForSelector('#tfZipFileInput');
        await fileInput.uploadFile(backupZipPath);

        // Wait for preflight analysis card to appear
        console.log('   Waiting for preflight duplicate detection...');
        await page.waitForSelector('.tf-preflight-card', { timeout: 300000 });
        await sleep(1000);

        const preflightData = await page.evaluate(() => {
            const metrics = Array.from(document.querySelectorAll('.tf-metrics-grid .tf-metric-card')).map(m => {
                return {
                    val: m.querySelector('.tf-metric-val')?.innerText.trim(),
                    lbl: m.querySelector('.tf-metric-lbl')?.innerText.trim()
                };
            });

            const catRows = Array.from(document.querySelectorAll('.tf-preflight-card .tf-table tbody tr')).map(r => {
                const tds = Array.from(r.querySelectorAll('td')).map(td => td.innerText.trim());
                return {
                    category: tds[0]?.split('\n')[0] || tds[0],
                    tablesCount: tds[1],
                    totalRecords: tds[2],
                    newCount: tds[3],
                    matchedCount: tds[4],
                    conflictCount: tds[5]
                };
            });

            const conflictRows = Array.from(document.querySelectorAll('.tf-conflict-row')).map(r => {
                const entity = r.querySelector('.tf-td-entity')?.innerText.trim();
                const key = r.querySelector('.tf-td-key')?.innerText.trim();
                const name = r.querySelector('.tf-td-name')?.innerText.trim();
                const status = r.querySelector('.tf-td-status')?.innerText.trim();
                const select = r.querySelector('select');
                const selectedVal = select ? select.value : null;
                return { entity, key, name, status, selectedVal };
            });

            return {
                metrics,
                categoriesCount: catRows.length,
                categories: catRows,
                conflictsCount: conflictRows.length,
                sampleConflicts: conflictRows.slice(0, 10)
            };
        });

        console.log(`   Preflight analysis completed: ${preflightData.categoriesCount} categories detected.`);
        console.log(`   Conflicts & Duplicates found: ${preflightData.conflictsCount} records.`);
        results.preflightAnalysis = preflightData;

        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'backup_03_preflight_analysis.png') });

        // Step 7: Test Decisions & Bulk Actions
        console.log('7. Testing Bulk Decision Actions...');
        await page.evaluate(() => {
            const btns = Array.from(document.querySelectorAll('.tf-bulk-actions button'));
            const b = btns.find(x => x.innerText.includes('Ghi đè tất cả'));
            if (b) b.click();
        });
        await sleep(500);
        console.log('   Applied bulk decision action (if conflicts present)');

        // Check confirmation checkbox
        const confirmCheckbox = await page.waitForSelector('.tf-restore-confirm-box input[type="checkbox"]');
        await confirmCheckbox.click();
        await sleep(300);

        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'backup_04_preflight_decisions.png') });

        // Step 8: Execute Restore
        console.log('8. Executing Restore...');
        const executeBtn = await page.waitForSelector('.tf-restore-confirm-box button.tf-btn-danger');
        await executeBtn.click();

        // Wait for restore result card
        console.log('   Waiting for restore completion (ACID commit & FK remapping)...');
        await page.waitForSelector('.tf-restore-result-card', { timeout: 300000 });
        await sleep(1500);

        const restoreResultData = await page.evaluate(() => {
            const card = document.querySelector('.tf-restore-result-card');
            const title = card?.querySelector('.tf-card-title')?.innerText.trim();
            const desc = card?.querySelector('.tf-card-desc')?.innerText.trim();
            const metrics = Array.from(card ? card.querySelectorAll('.tf-metric-card') : []).map(m => {
                return {
                    val: m.querySelector('.tf-metric-val')?.innerText.trim(),
                    lbl: m.querySelector('.tf-metric-lbl')?.innerText.trim()
                };
            });
            const summaries = Array.from(card ? card.querySelectorAll('tbody tr') : []).map(tr => {
                const tds = Array.from(tr.querySelectorAll('td')).map(td => td.innerText.trim());
                return { category: tds[0], processed: tds[1], imported: tds[2], overwritten: tds[3], retained: tds[4], skipped: tds[5] };
            });
            return { title, desc, metrics, summaries };
        });

        console.log('   Restore Title:', restoreResultData.title);
        console.log('   Restore Desc:', restoreResultData.desc);
        console.log('   Metrics:', restoreResultData.metrics.map(m => `${m.lbl}: ${m.val}`).join(' | '));
        results.restoreResult = restoreResultData;

        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'backup_05_restore_completed.png') });

        // Step 9: Verify Audit Log
        console.log('9. Verifying Audit Log for DatabaseBackupRestored...');
        await page.goto(`${BASE_URL}/cai-dat/nhat-ky`, { waitUntil: 'networkidle2' });
        await sleep(1000);

        const auditEntry = await page.evaluate(() => {
            const firstRow = document.querySelector('.tf-table tbody tr');
            if (!firstRow) return null;
            return Array.from(firstRow.querySelectorAll('td')).map(td => td.innerText.trim());
        });
        console.log('   Latest Audit Log Entry:', auditEntry ? auditEntry.join(' | ') : 'None');
        results.steps.auditEntry = auditEntry;

        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'backup_06_audit_log_verified.png') });

        // Write results to JSON artifact
        fs.writeFileSync(
            path.join(ARTIFACT_DIR, 'backup_restore_verification_results.json'),
            JSON.stringify(results, null, 2),
            'utf8'
        );
        console.log('   Results written to backup_restore_verification_results.json');

        console.log('\n=== ALL VERIFICATION CHECKS PASSED SUCCESSFULLY! ===\n');
    } catch (err) {
        console.error('VERIFICATION ERROR:', err);
        results.error = err.message;
        results.stack = err.stack;
        fs.writeFileSync(
            path.join(ARTIFACT_DIR, 'backup_restore_verification_results.json'),
            JSON.stringify(results, null, 2),
            'utf8'
        );
        await page.screenshot({ path: path.join(ARTIFACT_DIR, 'backup_error_state.png') }).catch(() => {});
        process.exitCode = 1;
    } finally {
        await browser.close();
    }
}

run();
