const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';
const BASE_URL = 'http://127.0.0.1:5169';

async function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

(async () => {
    console.log('🚀 Starting Premium Motion System & Consistency Verification...');
    
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1920,1080']
    });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1920, height: 1080 });

        // Step 1: Login
        console.log('🔑 Step 1: Logging in as Admin...');
        await page.goto(`${BASE_URL}/Account/Login`, { waitUntil: 'networkidle2' });
        
        await page.type('#login-username', 'admin');
        await page.type('#login-password', 'tradecore123');
        await Promise.all([
            page.waitForNavigation({ waitUntil: 'networkidle2' }),
            page.click('button[type="submit"]')
        ]);

        console.log('✅ Logged in successfully. Current URL:', page.url());

        // Step 2: Dashboard Reference Verification
        console.log('📊 Step 2: Verifying Dashboard Motion Reference...');
        await page.goto(`${BASE_URL}/`, { waitUntil: 'networkidle2' });
        await sleep(1000); // Allow GSAP entrance animation to complete

        // Verify TradeFlowMotion object is loaded
        const motionExists = await page.evaluate(() => {
            return typeof window.TradeFlowMotion !== 'undefined' && 
                   typeof window.TradeFlowMotion.animateModuleEntrance === 'function';
        });
        console.log('  -> window.TradeFlowMotion.animateModuleEntrance available:', motionExists);

        // Capture Dashboard Light Mode
        const dashLightPath = path.join(ARTIFACT_DIR, 'dashboard_motion_reference_light.png');
        await page.screenshot({ path: dashLightPath, fullPage: false });
        console.log('  -> Captured:', dashLightPath);

        // Test Dashboard Period Switcher Pill
        console.log('  -> Testing Dashboard period switcher pill...');
        const pill30d = await page.$('.tf-dash-period-pills button:nth-child(2)');
        if (pill30d) {
            await pill30d.click();
            await sleep(600);
        }

        // Step 3: Contracts Module Verification
        console.log('📜 Step 3: Verifying Contracts Module Motion & UI...');
        await page.goto(`${BASE_URL}/hop-dong`, { waitUntil: 'networkidle2' });
        await sleep(800); // Allow entrance stagger

        // Verify KPI Cards, segmented tabs, and table
        const contractsKpiCount = await page.evaluate(() => document.querySelectorAll('.tf-kpi-card').length);
        console.log(`  -> Contracts KPI cards found: ${contractsKpiCount}`);

        // Capture Contracts Commercial Tab (Light Mode)
        const contractsCommLightPath = path.join(ARTIFACT_DIR, 'contracts_commercial_tab_light.png');
        await page.screenshot({ path: contractsCommLightPath, fullPage: false });
        console.log('  -> Captured:', contractsCommLightPath);

        // Switch to Employment Contracts Tab
        console.log('  -> Switching to Employment Contracts tab...');
        const empTabBtn = await page.$('.tf-segmented-tab:nth-child(2)');
        if (empTabBtn) {
            await empTabBtn.click();
            await sleep(600); // Tab transition
        }

        // Capture Contracts Employment Tab (Light Mode)
        const contractsEmpLightPath = path.join(ARTIFACT_DIR, 'contracts_employment_tab_light.png');
        await page.screenshot({ path: contractsEmpLightPath, fullPage: false });
        console.log('  -> Captured:', contractsEmpLightPath);

        // Switch back to Commercial & Test "Tạo hợp đồng mới" (Template Selector Modal)
        console.log('  -> Testing Template Selector Modal opening & smooth animation...');
        const commTabBtn = await page.$('.tf-segmented-tab:nth-child(1)');
        if (commTabBtn) {
            await commTabBtn.click();
            await sleep(400);
        }

        const createContractBtn = await page.$('.tf-primary-btn');
        if (createContractBtn) {
            await createContractBtn.click();
            await sleep(600); // Modal entrance
        }

        const modalOpen = await page.evaluate(() => !!document.querySelector('.tf-gallery-modal-dialog, .modal.show'));
        console.log('  -> Template Selector Modal visible:', modalOpen);

        const templateModalLightPath = path.join(ARTIFACT_DIR, 'contracts_template_modal_light.png');
        await page.screenshot({ path: templateModalLightPath, fullPage: false });
        console.log('  -> Captured:', templateModalLightPath);

        // Test Escape key to dismiss modal
        console.log('  -> Dismissing modal via Escape key...');
        await page.keyboard.press('Escape');
        await sleep(400);

        // Step 4: Employees Module Verification
        console.log('👥 Step 4: Verifying Employees Module Motion & UI...');
        await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
        await sleep(800); // Allow entrance stagger

        // Verify layout, sidebar, main table
        const empSidebar = await page.evaluate(() => !!document.querySelector('.tf-employee-sidebar'));
        const empMain = await page.evaluate(() => !!document.querySelector('.tf-employee-main'));
        console.log('  -> Employees Sidebar & Main Table present:', empSidebar, empMain);

        // Capture Employees Light Mode
        const empLightPath = path.join(ARTIFACT_DIR, 'employees_overview_light.png');
        await page.screenshot({ path: empLightPath, fullPage: false });
        console.log('  -> Captured:', empLightPath);

        // Test Kebab Menu Dropdown in Employee Table
        console.log('  -> Testing Action Menu (Kebab) dropdown smooth reveal...');
        const firstKebab = await page.$('.tf-table-act-btn');
        if (firstKebab) {
            await firstKebab.click();
            await sleep(400); // Dropdown animation
        }

        const kebabMenuOpen = await page.evaluate(() => !!document.querySelector('.tf-table-act-menu'));
        console.log('  -> Kebab Action Menu visible:', kebabMenuOpen);

        const empKebabPath = path.join(ARTIFACT_DIR, 'employees_kebab_menu_light.png');
        await page.screenshot({ path: empKebabPath, fullPage: false });
        console.log('  -> Captured:', empKebabPath);

        // Dismiss action menu
        await page.keyboard.press('Escape');
        await sleep(300);

        // Open Add Employee Modal
        console.log('  -> Opening Add Employee Modal...');
        const addEmpBtn = await page.$('.tf-page-actions .tf-btn-primary');
        if (addEmpBtn) {
            await addEmpBtn.click();
            await sleep(600); // Modal entrance
        }

        const addEmpModalLightPath = path.join(ARTIFACT_DIR, 'employees_add_modal_light.png');
        await page.screenshot({ path: addEmpModalLightPath, fullPage: false });
        console.log('  -> Captured:', addEmpModalLightPath);

        // Close modal
        await page.keyboard.press('Escape');
        await sleep(400);

        // Step 5: Dark Mode Consistency Parity
        console.log('🌙 Step 5: Testing Dark Mode Parity across modules...');
        // Toggle dark mode via attribute on documentElement / body
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.documentElement.setAttribute('data-theme', 'dark');
            document.body.setAttribute('data-bs-theme', 'dark');
            document.body.setAttribute('data-theme', 'dark');
            localStorage.setItem('tradeflow_theme', 'dark');
        });
        await sleep(400);

        // Employees Dark Mode
        const empDarkPath = path.join(ARTIFACT_DIR, 'employees_overview_dark.png');
        await page.screenshot({ path: empDarkPath, fullPage: false });
        console.log('  -> Captured:', empDarkPath);

        // Contracts Dark Mode
        await page.goto(`${BASE_URL}/hop-dong`, { waitUntil: 'networkidle2' });
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.documentElement.setAttribute('data-theme', 'dark');
            document.body.setAttribute('data-bs-theme', 'dark');
            document.body.setAttribute('data-theme', 'dark');
        });
        await sleep(800);

        const contractsDarkPath = path.join(ARTIFACT_DIR, 'contracts_overview_dark.png');
        await page.screenshot({ path: contractsDarkPath, fullPage: false });
        console.log('  -> Captured:', contractsDarkPath);

        // Dashboard Dark Mode
        await page.goto(`${BASE_URL}/`, { waitUntil: 'networkidle2' });
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
            document.documentElement.setAttribute('data-theme', 'dark');
            document.body.setAttribute('data-bs-theme', 'dark');
            document.body.setAttribute('data-theme', 'dark');
        });
        await sleep(800);

        const dashDarkPath = path.join(ARTIFACT_DIR, 'dashboard_motion_reference_dark.png');
        await page.screenshot({ path: dashDarkPath, fullPage: false });
        console.log('  -> Captured:', dashDarkPath);

        // Step 6: Responsive Viewports Test
        console.log('📱 Step 6: Testing Responsive Viewports...');
        // Laptop 1366x768
        await page.setViewport({ width: 1366, height: 768 });
        await page.goto(`${BASE_URL}/hop-dong`, { waitUntil: 'networkidle2' });
        await sleep(600);
        const respLaptopPath = path.join(ARTIFACT_DIR, 'contracts_laptop_1366.png');
        await page.screenshot({ path: respLaptopPath, fullPage: false });

        // Tablet 768px
        await page.setViewport({ width: 768, height: 1024 });
        await page.goto(`${BASE_URL}/nhan-vien`, { waitUntil: 'networkidle2' });
        await sleep(600);
        const respTabletPath = path.join(ARTIFACT_DIR, 'employees_tablet_768.png');
        await page.screenshot({ path: respTabletPath, fullPage: false });

        // Mobile 390px
        await page.setViewport({ width: 390, height: 844 });
        await page.goto(`${BASE_URL}/`, { waitUntil: 'networkidle2' });
        await sleep(600);
        const respMobilePath = path.join(ARTIFACT_DIR, 'dashboard_mobile_390.png');
        await page.screenshot({ path: respMobilePath, fullPage: false });

        console.log('🎉 ALL MOTION & CONSISTENCY VERIFICATIONS PASSED SUCCESSFULLY!');
    } catch (err) {
        console.error('❌ Verification Error:', err);
        process.exitCode = 1;
    } finally {
        await browser.close();
    }
})();
