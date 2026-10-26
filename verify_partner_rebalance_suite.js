const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

async function runSuite() {
  const browser = await puppeteer.launch({
    headless: 'new',
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  const page = await browser.newPage();
  await page.setViewport({ width: 1440, height: 900 });

  console.log('Logging in to TradeFlow...');
  await page.goto('http://localhost:5169/Account/Login', { waitUntil: 'networkidle2' });
  await page.type('#login-username', 'admin');
  await page.type('#login-password', 'tradecore123');
  await Promise.all([
    page.waitForNavigation({ waitUntil: 'networkidle2' }),
    page.click('button[type="submit"]')
  ]);
  console.log('Login successful.');

  const results = {
    timestamp: new Date().toISOString(),
    tests: []
  };

  function toObj(rect) {
    if (!rect) return null;
    return {
      x: Math.round(rect.x),
      y: Math.round(rect.y),
      width: Math.round(rect.width),
      height: Math.round(rect.height),
      left: Math.round(rect.left),
      right: Math.round(rect.right)
    };
  }

  // Helper to toggle theme
  async function setTheme(mode) {
    await page.evaluate((theme) => {
      try {
        document.documentElement.setAttribute('data-theme', theme);
        document.documentElement.setAttribute('data-bs-theme', theme);
        try { localStorage.setItem('tf-theme', theme); } catch(e) {}
      } catch(err) {
        console.error('setTheme error:', err);
      }
    }, mode);
    await new Promise(r => setTimeout(r, 400));
  }

  console.log('--- 1. Testing Customer List (/doi-tac/khach-hang) ---');
  await page.goto('http://localhost:5169/doi-tac/khach-hang', { waitUntil: 'networkidle0' });
  await page.waitForSelector('.tf-table');

  // Customer Light
  await setTheme('light');
  const custLightMetrics = await page.evaluate(() => {
    const content = document.querySelector('.tf-content');
    const container = document.querySelector('.tf-page-container');
    const split = document.querySelector('.tf-split-layout');
    const sidebar = document.querySelector('.tf-filter-sidebar');
    const cardBody = document.querySelector('.tf-card-body');
    const table = document.querySelector('.tf-table');
    const actionTh = document.querySelector('.tf-col-actions');
    const firstRowActionStack = document.querySelector('.tf-table tbody tr:first-child .tf-row-actions-stack');
    const stackButtons = firstRowActionStack ? Array.from(firstRowActionStack.querySelectorAll('.tf-btn-action-stack')).map(b => b.textContent.trim()) : [];
    
    return {
      containerPadding: window.getComputedStyle(container).padding,
      splitGrid: window.getComputedStyle(split).gridTemplateColumns,
      splitGap: window.getComputedStyle(split).gap,
      sidebarWidth: sidebar ? sidebar.getBoundingClientRect().width : null,
      tableWidth: table ? table.getBoundingClientRect().width : null,
      cardBodyScrollWidth: cardBody ? cardBody.scrollWidth : null,
      cardBodyClientWidth: cardBody ? cardBody.clientWidth : null,
      actionColWidth: actionTh ? actionTh.getBoundingClientRect().width : null,
      actionColIsSticky: actionTh ? window.getComputedStyle(actionTh).position : null,
      stackButtons: stackButtons,
      firstRowHeight: document.querySelector('.tf-table tbody tr:first-child') ? document.querySelector('.tf-table tbody tr:first-child').getBoundingClientRect().height : null
    };
  });
  console.log('Customer Light Metrics:', custLightMetrics);

  const custLightScreenshot = path.join(ARTIFACT_DIR, 'customer_list_rebalanced_light.png');
  await page.screenshot({ path: custLightScreenshot, fullPage: false });

  // Customer Dark
  await setTheme('dark');
  const custDarkScreenshot = path.join(ARTIFACT_DIR, 'customer_list_rebalanced_dark.png');
  await page.screenshot({ path: custDarkScreenshot, fullPage: false });

  results.tests.push({
    module: 'Customer List',
    metrics: custLightMetrics,
    screenshots: ['customer_list_rebalanced_light.png', 'customer_list_rebalanced_dark.png'],
    status: (custLightMetrics.actionColWidth <= 92 && custLightMetrics.stackButtons.length >= 3) ? 'PASS' : 'FAIL'
  });

  console.log('--- 2. Testing Supplier List (/doi-tac/nha-cung-cap) ---');
  await page.goto('http://localhost:5169/doi-tac/nha-cung-cap', { waitUntil: 'networkidle0' });
  await page.waitForSelector('.tf-table');

  await setTheme('light');
  const supLightMetrics = await page.evaluate(() => {
    const container = document.querySelector('.tf-page-container');
    const split = document.querySelector('.tf-split-layout');
    const sidebar = document.querySelector('.tf-filter-sidebar');
    const cardBody = document.querySelector('.tf-card-body');
    const table = document.querySelector('.tf-table');
    const actionTh = document.querySelector('.tf-col-actions');
    const firstRowActionStack = document.querySelector('.tf-table tbody tr:first-child .tf-row-actions-stack');
    const stackButtons = firstRowActionStack ? Array.from(firstRowActionStack.querySelectorAll('.tf-btn-action-stack')).map(b => b.textContent.trim()) : [];
    
    return {
      containerPadding: window.getComputedStyle(container).padding,
      splitGrid: window.getComputedStyle(split).gridTemplateColumns,
      splitGap: window.getComputedStyle(split).gap,
      sidebarWidth: sidebar ? sidebar.getBoundingClientRect().width : null,
      tableWidth: table ? table.getBoundingClientRect().width : null,
      cardBodyScrollWidth: cardBody ? cardBody.scrollWidth : null,
      cardBodyClientWidth: cardBody ? cardBody.clientWidth : null,
      actionColWidth: actionTh ? actionTh.getBoundingClientRect().width : null,
      actionColIsSticky: actionTh ? window.getComputedStyle(actionTh).position : null,
      stackButtons: stackButtons,
      firstRowHeight: document.querySelector('.tf-table tbody tr:first-child') ? document.querySelector('.tf-table tbody tr:first-child').getBoundingClientRect().height : null
    };
  });
  console.log('Supplier Light Metrics:', supLightMetrics);

  const supLightScreenshot = path.join(ARTIFACT_DIR, 'supplier_list_rebalanced_light.png');
  await page.screenshot({ path: supLightScreenshot, fullPage: false });

  await setTheme('dark');
  const supDarkScreenshot = path.join(ARTIFACT_DIR, 'supplier_list_rebalanced_dark.png');
  await page.screenshot({ path: supDarkScreenshot, fullPage: false });

  results.tests.push({
    module: 'Supplier List',
    metrics: supLightMetrics,
    screenshots: ['supplier_list_rebalanced_light.png', 'supplier_list_rebalanced_dark.png'],
    status: (supLightMetrics.actionColWidth <= 92 && supLightMetrics.stackButtons.length >= 3) ? 'PASS' : 'FAIL'
  });

  console.log('--- 3. Testing Debt Management (/doi-tac/cong-no) ---');
  await page.goto('http://localhost:5169/doi-tac/cong-no', { waitUntil: 'networkidle0' });
  await page.waitForSelector('.tf-table');

  // Tab 1: All Debt
  await setTheme('light');
  const debtAllLightMetrics = await page.evaluate(() => {
    const container = document.querySelector('.tf-debt-container');
    const split = document.querySelector('.tf-split-layout');
    const cardBody = document.querySelector('.tf-card-body');
    const table = document.querySelector('.tf-table');
    const actionTh = document.querySelector('.tf-col-actions');
    const firstRowActionStack = document.querySelector('.tf-table tbody tr:first-child .tf-row-actions-stack');
    const stackButtons = firstRowActionStack ? Array.from(firstRowActionStack.querySelectorAll('.tf-btn-action-stack')).map(b => b.textContent.trim()) : [];
    
    return {
      containerPadding: window.getComputedStyle(container).padding,
      splitGrid: window.getComputedStyle(split).gridTemplateColumns,
      tableWidth: table ? table.getBoundingClientRect().width : null,
      cardBodyScrollWidth: cardBody ? cardBody.scrollWidth : null,
      cardBodyClientWidth: cardBody ? cardBody.clientWidth : null,
      actionColWidth: actionTh ? actionTh.getBoundingClientRect().width : null,
      actionColIsSticky: actionTh ? window.getComputedStyle(actionTh).position : null,
      stackButtons: stackButtons
    };
  });
  console.log('Debt Tab 1 Metrics:', debtAllLightMetrics);

  const debtAllLightScreenshot = path.join(ARTIFACT_DIR, 'debt_all_rebalanced_light.png');
  await page.screenshot({ path: debtAllLightScreenshot, fullPage: false });

  await setTheme('dark');
  const debtAllDarkScreenshot = path.join(ARTIFACT_DIR, 'debt_all_rebalanced_dark.png');
  await page.screenshot({ path: debtAllDarkScreenshot, fullPage: false });

  // Tab 2: Phải thu khách hàng
  const tabs = await page.$$('.tf-debt-tab');
  if (tabs.length >= 2) {
    await tabs[1].click();
    await new Promise(r => setTimeout(r, 600));
    await page.waitForSelector('.tf-table');
    const debtTab2Screenshot = path.join(ARTIFACT_DIR, 'debt_receivables_rebalanced_dark.png');
    await page.screenshot({ path: debtTab2Screenshot, fullPage: false });
  }

  // Tab 3: Phải trả nhà cung cấp
  const updatedTabs = await page.$$('.tf-debt-tab');
  if (updatedTabs.length >= 3) {
    await updatedTabs[2].click();
    await new Promise(r => setTimeout(r, 600));
    await page.waitForSelector('.tf-table');
    const debtTab3Screenshot = path.join(ARTIFACT_DIR, 'debt_payables_rebalanced_dark.png');
    await page.screenshot({ path: debtTab3Screenshot, fullPage: false });
  }

  results.tests.push({
    module: 'Debt Management',
    metrics: debtAllLightMetrics,
    screenshots: [
      'debt_all_rebalanced_light.png',
      'debt_all_rebalanced_dark.png',
      'debt_receivables_rebalanced_dark.png',
      'debt_payables_rebalanced_dark.png'
    ],
    status: (debtAllLightMetrics.actionColWidth <= 92 && debtAllLightMetrics.stackButtons.length >= 2) ? 'PASS' : 'FAIL'
  });

  console.log('--- 4. Auditing Other Sidebar Modules ---');

  // Dashboard
  await page.goto('http://localhost:5169/', { waitUntil: 'networkidle0' });
  await setTheme('dark');
  await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_dashboard_dark.png'), fullPage: false });

  // Sales POS
  await page.goto('http://localhost:5169/ban-hang', { waitUntil: 'networkidle0' });
  await setTheme('dark');
  await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_pos_dark.png'), fullPage: false });

  // Invoices List
  await page.goto('http://localhost:5169/hoa-don', { waitUntil: 'networkidle0' });
  await setTheme('dark');
  await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_invoices_dark.png'), fullPage: false });

  // Invoices Create
  await page.goto('http://localhost:5169/hoa-don/tao-moi', { waitUntil: 'networkidle0' });
  await setTheme('dark');
  await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_invoice_create_dark.png'), fullPage: false });

  // Warehouses Quarantine
  await page.goto('http://localhost:5169/kho/hang-loi-cach-ly', { waitUntil: 'networkidle0' });
  await setTheme('dark');
  await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_quarantine_dark.png'), fullPage: false });

  // Stock Ledger
  await page.goto('http://localhost:5169/kho/the-kho', { waitUntil: 'networkidle0' });
  await setTheme('dark');
  await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_stock_ledger_dark.png'), fullPage: false });

  // Pricing Compare
  await page.goto('http://localhost:5169/bang-gia/so-sanh', { waitUntil: 'networkidle0' });
  await setTheme('light');
  await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_pricing_compare_light.png'), fullPage: false });
  await setTheme('dark');
  await page.screenshot({ path: path.join(ARTIFACT_DIR, 'audit_pricing_compare_dark.png'), fullPage: false });

  fs.writeFileSync(
    path.join(ARTIFACT_DIR, 'partner_module_rebalance_verification.json'),
    JSON.stringify(results, null, 2),
    'utf-8'
  );

  console.log('--- ALL VERIFICATION TESTS COMPLETED SUCCESSFULLY ---');
  await browser.close();
}

runSuite().catch(err => {
  console.error('Test Suite Failed:', err);
  process.exit(1);
});
