const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACTS_DIR = 'C:/Users/thitr/.gemini/antigravity/brain/fb94d497-815d-460b-bd53-9de32ef4d901';

async function run() {
  const browser = await puppeteer.launch({
    headless: 'new',
    args: ['--no-sandbox', '--disable-setuid-sandbox', '--window-size=1920,1080']
  });

  const page = await browser.newPage();
  await page.setViewport({ width: 1440, height: 900 });

  console.log('--- Step 1: Login ---');
  await page.goto('http://127.0.0.1:5169/Account/Login', { waitUntil: 'networkidle2' });
  await page.waitForSelector('#login-username', { timeout: 10000 });
  await page.type('#login-username', 'admin');
  await page.type('#login-password', 'tradecore123');
  await Promise.all([
    page.waitForNavigation({ waitUntil: 'networkidle2' }),
    page.click('button[type="submit"]')
  ]);
  console.log('Logged in successfully. Current URL:', page.url());

  const results = {
    partnerCoordinates: {},
    responsiveWraps: {},
    approvalModalTabs: {},
    scrollPerformance: {},
    screenshots: []
  };

  // Helper to test partner page
  async function testPartnerPage(route, name, filePrefix) {
    console.log(`\nTesting ${name} (${route})...`);
    await page.goto(`http://127.0.0.1:5169${route}`, { waitUntil: 'networkidle2' });
    await page.waitForSelector('.tf-page-header', { timeout: 10000 });

    // Measure bounding boxes at 1440x900
    const coords = await page.evaluate(() => {
      const header = document.querySelector('.tf-page-header');
      const left = document.querySelector('.tf-page-header-left');
      const actions = document.querySelector('.tf-page-header-actions');
      const title = document.querySelector('.tf-page-title');
      const kpiCard = document.querySelector('.tf-partner-metric-card, .tf-debt-kpi-card');
      const kpiText = document.querySelector('.tf-metric-data, .tf-kpi-content');

      const hBox = header ? header.getBoundingClientRect() : null;
      const lBox = left ? left.getBoundingClientRect() : null;
      const aBox = actions ? actions.getBoundingClientRect() : null;
      const tBox = title ? title.getBoundingClientRect() : null;
      const kBox = kpiCard ? kpiCard.getBoundingClientRect() : null;
      const ktBox = kpiText ? kpiText.getBoundingClientRect() : null;

      const titleStyle = title ? window.getComputedStyle(title) : null;
      const leftStyle = left ? window.getComputedStyle(left) : null;

      return {
        header: hBox ? { x: Math.round(hBox.x), y: Math.round(hBox.y), width: Math.round(hBox.width), height: Math.round(hBox.height) } : null,
        left: lBox ? { x: Math.round(lBox.x), y: Math.round(lBox.y), width: Math.round(lBox.width), height: Math.round(lBox.height) } : null,
        actions: aBox ? { x: Math.round(aBox.x), y: Math.round(aBox.y), width: Math.round(aBox.width), height: Math.round(aBox.height) } : null,
        title: tBox ? { x: Math.round(tBox.x), y: Math.round(tBox.y), width: Math.round(tBox.width), textAlign: titleStyle?.textAlign } : null,
        leftTextAlign: leftStyle?.textAlign,
        kpi: kBox ? { x: Math.round(kBox.x), y: Math.round(kBox.y), width: Math.round(kBox.width), height: Math.round(kBox.height) } : null,
        kpiText: ktBox ? { x: Math.round(ktBox.x), width: Math.round(ktBox.width) } : null
      };
    });

    results.partnerCoordinates[name] = coords;
    console.log(`${name} Header Left X:`, coords.left?.x, '(Expected ~248-260 on wide desktop, NOT 540+)');
    console.log(`${name} Header Actions X:`, coords.actions?.x, 'Width:', coords.actions?.width);

    // Capture Light screenshot at 1440x900
    const lightPath = path.join(ARTIFACTS_DIR, `${filePrefix}_light_1440.png`);
    await page.screenshot({ path: lightPath });
    results.screenshots.push(lightPath);

    // Switch to Dark mode
    await page.evaluate(() => {
      document.documentElement.setAttribute('data-theme', 'dark');
      document.documentElement.setAttribute('data-bs-theme', 'dark');
      document.body.setAttribute('data-theme', 'dark');
      document.body.setAttribute('data-bs-theme', 'dark');
    });
    await new Promise(r => setTimeout(r, 300));
    const darkPath = path.join(ARTIFACTS_DIR, `${filePrefix}_dark_1440.png`);
    await page.screenshot({ path: darkPath });
    results.screenshots.push(darkPath);

    // Switch back to Light mode
    await page.evaluate(() => {
      document.documentElement.removeAttribute('data-theme');
      document.documentElement.removeAttribute('data-bs-theme');
      document.body.removeAttribute('data-theme');
      document.body.removeAttribute('data-bs-theme');
    });
  }

  // Run tests on all three partner pages
  await testPartnerPage('/doi-tac/khach-hang', 'Customers', 'verify_customers');
  await testPartnerPage('/doi-tac/nha-cung-cap', 'Suppliers', 'verify_suppliers');
  await testPartnerPage('/doi-tac/cong-no', 'Debt', 'verify_debt');

  // Test responsive wrapping at 1366x768 and 1920x1080
  console.log('\n--- Step 2: Testing Responsive Viewports ---');
  for (const vp of [{ width: 1366, height: 768, name: '1366x768' }, { width: 1920, height: 1080, name: '1920x1080' }]) {
    await page.setViewport({ width: vp.width, height: vp.height });
    await page.goto('http://127.0.0.1:5169/doi-tac/cong-no', { waitUntil: 'networkidle2' });
    await page.waitForSelector('.tf-page-header');

    const debtWrap = await page.evaluate(() => {
      const search = document.querySelector('.tf-header-search-wrap');
      const actionCluster = document.querySelector('.tf-header-action-cluster');
      const sBox = search?.getBoundingClientRect();
      const aBox = actionCluster?.getBoundingClientRect();
      return {
        searchWidth: Math.round(sBox?.width || 0),
        actionClusterWidth: Math.round(aBox?.width || 0),
        searchX: Math.round(sBox?.x || 0),
        actionClusterX: Math.round(aBox?.x || 0)
      };
    });
    results.responsiveWraps[vp.name] = debtWrap;
    console.log(`Debt at ${vp.name}: Search width = ${debtWrap.searchWidth}px, Action cluster width = ${debtWrap.actionClusterWidth}px`);

    const vpScreenshot = path.join(ARTIFACTS_DIR, `verify_debt_${vp.name}.png`);
    await page.screenshot({ path: vpScreenshot });
    results.screenshots.push(vpScreenshot);
  }

  // Restore 1440x900
  await page.setViewport({ width: 1440, height: 900 });

  // Step 3: Test Employee Approval Modal
  console.log('\n--- Step 3: Testing Employee Approval Modal ---');
  await page.goto('http://127.0.0.1:5169/nhan-vien?tab=requests', { waitUntil: 'networkidle2' });
  await page.waitForSelector('.tf-req-filter-bar, .tf-tab-btn', { timeout: 10000 });

  // Inspect tab styles
  const tabStyles = await page.evaluate(() => {
    const tabs = Array.from(document.querySelectorAll('.tf-tab-btn'));
    return tabs.map(t => {
      const s = window.getComputedStyle(t);
      return {
        text: t.innerText.trim(),
        backgroundColor: s.backgroundColor,
        color: s.color,
        borderRadius: s.borderRadius,
        border: s.border,
        boxShadow: s.boxShadow,
        isActive: t.classList.contains('active')
      };
    });
  });
  results.approvalModalTabs = tabStyles;
  console.log('Tab styles detected (count: ' + tabStyles.length + '):');
  tabStyles.forEach((t, i) => console.log(`  Tab ${i + 1}: "${t.text.replace(/\n/g, ' ')}" active=${t.isActive} bg=${t.backgroundColor} radius=${t.borderRadius}`));

  // Capture Light approval modal
  const approvalLight = path.join(ARTIFACTS_DIR, 'verify_approval_modal_light.png');
  await page.screenshot({ path: approvalLight });
  results.screenshots.push(approvalLight);

  // Switch to Dark mode and capture
  await page.evaluate(() => {
    document.documentElement.setAttribute('data-theme', 'dark');
    document.documentElement.setAttribute('data-bs-theme', 'dark');
    document.body.setAttribute('data-theme', 'dark');
    document.body.setAttribute('data-bs-theme', 'dark');
  });
  await new Promise(r => setTimeout(r, 400));
  const approvalDark = path.join(ARTIFACTS_DIR, 'verify_approval_modal_dark.png');
  await page.screenshot({ path: approvalDark });
  results.screenshots.push(approvalDark);

  // Switch back to Light mode
  await page.evaluate(() => {
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.removeAttribute('data-bs-theme');
    document.body.removeAttribute('data-theme');
    document.body.removeAttribute('data-bs-theme');
  });

  // Test clicking each tab in the approval modal
  const tabButtons = await page.$$('.tf-tab-btn');
  console.log(`Clicking through ${tabButtons.length} filter tabs...`);
  for (let i = 0; i < tabButtons.length; i++) {
    await tabButtons[i].click();
    await new Promise(r => setTimeout(r, 200));
  }

  // Step 4: Test Employee Form Modal Scrolling Performance
  console.log('\n--- Step 4: Testing Employee Form Modal Scrolling Performance ---');
  await page.goto('http://127.0.0.1:5169/nhan-vien', { waitUntil: 'networkidle2' });
  await page.waitForSelector('.tf-page-actions, .tf-page-header-actions', { timeout: 10000 });
  
  // Click "Nhân viên" primary button to open modal
  await page.evaluate(() => {
    const btns = Array.from(document.querySelectorAll('.tf-page-actions button.tf-btn-primary, button.tf-btn-primary'));
    const btn = btns.find(b => b.innerText.includes('Nhân viên') || b.innerText.includes('Thêm'));
    if (btn) btn.click();
  });

  await page.waitForSelector('.employee-modal-body, .tf-modal-body', { timeout: 5000 });

  const scrollMetrics = await page.evaluate(async () => {
    const scrollBody = document.querySelector('.employee-modal-body') || document.querySelector('.tf-modal-body');
    if (!scrollBody) return { error: 'scroll body not found' };

    const cs = window.getComputedStyle(scrollBody);
    const initialScrollBehavior = cs.scrollBehavior;
    const initialTransform = cs.transform;
    const initialContain = cs.contain;

    // Scroll test: 20 wheel steps down and up, measure duration
    const startTime = performance.now();
    const frames = [];
    let lastTime = startTime;

    for (let i = 0; i < 20; i++) {
      scrollBody.scrollTop = i * 40;
      await new Promise(r => requestAnimationFrame(r));
      const now = performance.now();
      frames.push(now - lastTime);
      lastTime = now;
    }

    for (let i = 20; i >= 0; i--) {
      scrollBody.scrollTop = i * 40;
      await new Promise(r => requestAnimationFrame(r));
      const now = performance.now();
      frames.push(now - lastTime);
      lastTime = now;
    }

    const totalTime = performance.now() - startTime;
    const avgFrameTime = frames.reduce((a, b) => a + b, 0) / frames.length;
    const maxFrameTime = Math.max(...frames);

    return {
      scrollBehavior: initialScrollBehavior,
      transform: initialTransform,
      contain: initialContain,
      totalTimeMs: totalTime,
      avgFrameTimeMs: avgFrameTime,
      maxFrameTimeMs: maxFrameTime,
      frameCount: frames.length,
      scrollHeight: scrollBody.scrollHeight,
      clientHeight: scrollBody.clientHeight
    };
  });

  results.scrollPerformance = scrollMetrics;
  console.log('Scroll performance metrics:');
  console.log(`  scroll-behavior: ${scrollMetrics.scrollBehavior} (Expected auto)`);
  console.log(`  contain: ${scrollMetrics.contain}`);
  console.log(`  transform: ${scrollMetrics.transform}`);
  console.log(`  avg frame time: ${scrollMetrics.avgFrameTimeMs?.toFixed(2)}ms`);
  console.log(`  max frame time: ${scrollMetrics.maxFrameTimeMs?.toFixed(2)}ms`);

  const formModalScreenshot = path.join(ARTIFACTS_DIR, 'verify_employee_form_modal_scrolling.png');
  await page.screenshot({ path: formModalScreenshot });
  results.screenshots.push(formModalScreenshot);

  // Save full results JSON
  fs.writeFileSync(path.join(ARTIFACTS_DIR, 'partner_and_approval_verification_results.json'), JSON.stringify(results, null, 2), 'utf8');
  console.log('\nAll verification tests completed successfully!');

  await browser.close();
}

run().catch(err => {
  console.error('Error during verification:', err);
  process.exit(1);
});
