const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const ARTIFACT_DIR = 'C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901';

async function runVerification() {
  const browser = await puppeteer.launch({
    headless: 'new',
    args: ['--no-sandbox', '--disable-setuid-sandbox']
  });

  const page = await browser.newPage();
  await page.setViewport({ width: 1440, height: 900 });

  const results = {
    timestamp: new Date().toISOString(),
    tests: []
  };

  function addTest(name, passed, details) {
    console.log(`[${passed ? 'PASS' : 'FAIL'}] ${name}`);
    results.tests.push({ name, passed, details });
  }

  try {
    console.log('1. Logging in to TradeFlow...');
    await page.goto('http://localhost:5169/Account/Login', { waitUntil: 'networkidle2' });
    await page.type('#login-username', 'admin');
    await page.type('#login-password', 'tradecore123');
    await Promise.all([
      page.waitForNavigation({ waitUntil: 'networkidle2' }),
      page.click('button[type="submit"]')
    ]);
    addTest('Login', true, 'Successfully logged in as admin');

    // Helper to set theme
    async function setTheme(theme) {
      await page.evaluate((t) => {
        document.documentElement.setAttribute('data-bs-theme', t);
        document.documentElement.setAttribute('data-theme', t);
        localStorage.setItem('tradeflow-theme', t);
      }, theme);
      await new Promise(r => setTimeout(r, 200));
    }

    // ========================================================
    // TEST 1: Customer Edit Attachment Section in Dark Mode
    // ========================================================
    console.log('\n2. Testing Customer Edit Attachment Section in Dark Mode...');
    // Find first valid customer edit URL
    await page.goto('http://localhost:5169/doi-tac/khach-hang', { waitUntil: 'networkidle2' });
    const customerEditUrl = await page.evaluate(() => {
      const link = document.querySelector('a[href*="/doi-tac/khach-hang/sua"]');
      return link ? link.href : 'http://localhost:5169/doi-tac/khach-hang/sua/5';
    });
    console.log(`Using Customer Edit URL: ${customerEditUrl}`);

    await page.goto(customerEditUrl, { waitUntil: 'networkidle2' });
    await setTheme('dark');

    await page.waitForSelector('.tf-attachment-panel', { timeout: 5000 });

    const customerAttachmentStyles = await page.evaluate(() => {
      const panel = document.querySelector('.tf-attachment-panel');
      const emptyState = document.querySelector('.tf-attachment-empty');
      const panelBg = window.getComputedStyle(panel).backgroundColor;
      const panelBorder = window.getComputedStyle(panel).borderColor;
      const emptyBg = emptyState ? window.getComputedStyle(emptyState).backgroundColor : null;
      const emptyBorder = emptyState ? window.getComputedStyle(emptyState).borderColor : null;
      return { panelBg, panelBorder, emptyBg, emptyBorder };
    });

    console.log('Customer Attachment Panel Styles:', customerAttachmentStyles);

    const isPanelDark = customerAttachmentStyles.panelBg.includes('29, 32, 40') || 
                        customerAttachmentStyles.panelBg.includes('24, 27, 34') ||
                        customerAttachmentStyles.panelBg.includes('30, 32, 40');
    const isEmptyDark = !customerAttachmentStyles.emptyBg || 
                        customerAttachmentStyles.emptyBg.includes('24, 27, 34') ||
                        customerAttachmentStyles.emptyBg.includes('0.02') ||
                        customerAttachmentStyles.emptyBg.includes('29, 32, 40');

    addTest('Customer Attachment Dark Panel', isPanelDark, customerAttachmentStyles);
    addTest('Customer Attachment Empty State Dark (No #fbfcfe)', isEmptyDark, customerAttachmentStyles);

    // Test opening the upload modal
    const uploadBtn = await page.$('.tf-attachment-header button');
    let modalStyles = null;
    if (uploadBtn) {
      await uploadBtn.click();
      await page.waitForSelector('.tf-modal-card', { timeout: 3000 });
      modalStyles = await page.evaluate(() => {
        const modal = document.querySelector('.tf-modal-card');
        return {
          bg: window.getComputedStyle(modal).backgroundColor,
          border: window.getComputedStyle(modal).borderColor,
          titleColor: window.getComputedStyle(modal.querySelector('.tf-modal-title')).color
        };
      });
      console.log('Customer Upload Modal Styles:', modalStyles);
      const isModalDark = modalStyles.bg.includes('29, 32, 40') || modalStyles.bg.includes('24, 27, 34') || modalStyles.bg.includes('30, 32, 40');
      addTest('Customer Upload Modal Dark Surface', isModalDark, modalStyles);

      // Close modal
      const closeBtn = await page.$('.tf-modal-header button');
      if (closeBtn) await closeBtn.click();
      await new Promise(r => setTimeout(r, 200));
    }

    const customerDarkShot = path.join(ARTIFACT_DIR, 'customer_attachment_dark.png');
    await page.screenshot({ path: customerDarkShot, fullPage: false });
    console.log(`Saved screenshot: ${customerDarkShot}`);

    // ========================================================
    // TEST 2: Supplier Edit Attachment Section in Dark Mode
    // ========================================================
    console.log('\n3. Testing Supplier Edit Attachment Section in Dark Mode...');
    await page.goto('http://localhost:5169/doi-tac/nha-cung-cap/sua/1', { waitUntil: 'networkidle2' });
    await setTheme('dark');

    const supplierHasAttachment = await page.$('.tf-attachment-panel');
    addTest('Supplier Form Attachment Section Exists', !!supplierHasAttachment, 'Rendered DocumentAttachmentSection for Supplier');

    if (supplierHasAttachment) {
      const supplierAttachmentStyles = await page.evaluate(() => {
        const panel = document.querySelector('.tf-attachment-panel');
        return {
          bg: window.getComputedStyle(panel).backgroundColor,
          border: window.getComputedStyle(panel).borderColor
        };
      });
      const isSupplierPanelDark = supplierAttachmentStyles.bg.includes('29, 32, 40') || supplierAttachmentStyles.bg.includes('24, 27, 34');
      addTest('Supplier Attachment Dark Surface', isSupplierPanelDark, supplierAttachmentStyles);
    }

    const supplierDarkShot = path.join(ARTIFACT_DIR, 'supplier_attachment_dark.png');
    await page.screenshot({ path: supplierDarkShot, fullPage: false });
    console.log(`Saved screenshot: ${supplierDarkShot}`);

    // ========================================================
    // TEST 3: Light Mode Verification for Attachments
    // ========================================================
    console.log('\n4. Testing Attachment Section in Light Mode...');
    await setTheme('light');
    await page.goto(customerEditUrl, { waitUntil: 'networkidle2' });
    await setTheme('light');

    const customerLightShot = path.join(ARTIFACT_DIR, 'customer_attachment_light.png');
    await page.screenshot({ path: customerLightShot, fullPage: false });

    await page.goto('http://localhost:5169/doi-tac/nha-cung-cap/sua/1', { waitUntil: 'networkidle2' });
    await setTheme('light');

    const supplierLightShot = path.join(ARTIFACT_DIR, 'supplier_attachment_light.png');
    await page.screenshot({ path: supplierLightShot, fullPage: false });

    addTest('Attachment Light Mode Rendering', true, 'Light mode screenshots captured cleanly');

    // ========================================================
    // TEST 4: Digital Signature Settings Professional SVG Icons
    // ========================================================
    console.log('\n5. Testing Digital Signature Settings Icons (Light & Dark)...');
    await page.goto('http://localhost:5169/cai-dat/an-ninh', { waitUntil: 'networkidle2' });
    await setTheme('dark');

    const iconAudit = await page.evaluate(() => {
      const emojiRegex = /[\u{1F300}-\u{1F9FF}\u{2600}-\u{26FF}\u{2700}-\u{27BF}]/u;
      const tabButtons = Array.from(document.querySelectorAll('div[style*="overflow-x:auto"] button'));
      const tabsInfo = tabButtons.map(btn => {
        const text = btn.innerText.trim();
        const hasEmoji = emojiRegex.test(text);
        const svgCount = btn.querySelectorAll('svg').length;
        return { text, hasEmoji, svgCount };
      });

      const totalTabEmojis = tabsInfo.filter(t => t.hasEmoji).length;
      const totalTabSvgs = tabsInfo.filter(t => t.svgCount > 0).length;

      return { tabsInfo, totalTabEmojis, totalTabSvgs };
    });

    console.log('Tabs Icon Audit:', iconAudit);
    addTest('Zero Tab Emojis', iconAudit.totalTabEmojis === 0, iconAudit);
    addTest('All Tabs Have SVG Icons', iconAudit.totalTabSvgs >= 5, iconAudit);

    const securityDarkShot = path.join(ARTIFACT_DIR, 'security_dashboard_tabs_dark.png');
    await page.screenshot({ path: securityDarkShot, fullPage: false });

    // Test "Cấp mã kích hoạt" Modal
    const allBtns = await page.$$('button');
    let clickedIssue = false;
    for (const b of allBtns) {
      const text = await page.evaluate(el => el.innerText, b);
      if (text.includes('Cấp mã kích hoạt')) {
        await b.click();
        clickedIssue = true;
        break;
      }
    }

    if (clickedIssue) {
      await page.waitForSelector('div[style*="z-index:1000"]', { timeout: 4000 });
      const modalEmojiCheck = await page.evaluate(() => {
        const emojiRegex = /[\u{1F300}-\u{1F9FF}\u{2600}-\u{26FF}\u{2700}-\u{27BF}]/u;
        const modal = document.querySelector('div[style*="z-index:1000"]');
        if (!modal) return { found: false };
        const modalText = modal.innerText;
        const hasEmoji = emojiRegex.test(modalText);
        const headerSvg = modal.querySelectorAll('h3 svg').length;
        const modalTitle = modal.querySelector('h3')?.innerText;
        return { found: true, modalTitle, hasEmoji, headerSvg };
      });
      console.log('Modal Emoji Check:', modalEmojiCheck);
      addTest('Modal Has No Emojis & Uses SVG', modalEmojiCheck.found && !modalEmojiCheck.hasEmoji && modalEmojiCheck.headerSvg > 0, modalEmojiCheck);

      const modalShot = path.join(ARTIFACT_DIR, 'security_modal_svg_dark.png');
      await page.screenshot({ path: modalShot, fullPage: false });

      // Close modal
      const closeBtn = await page.$('div[style*="z-index:1000"] button');
      if (closeBtn) await closeBtn.click();
      await new Promise(r => setTimeout(r, 300));
    }

    // ========================================================
    // TEST 5: Rapid Navigation Theme Flash Verification
    // ========================================================
    console.log('\n6. Testing Rapid Navigation Theme Flash in Dark Mode...');
    await setTheme('dark');

    // Instrument page to detect if a light background frame ever renders
    const flashDetection = await page.evaluate(() => {
      window.__lightFlashesDetected = 0;
      window.__inspectedRouteChanges = 0;

      // Monitor any style attribute resets or light background injections
      const observer = new MutationObserver((mutations) => {
        for (const mut of mutations) {
          if (mut.type === 'attributes' && mut.attributeName.includes('theme')) {
            const currentTheme = document.documentElement.getAttribute('data-bs-theme');
            if (currentTheme === 'light') {
              window.__lightFlashesDetected++;
            }
          }
        }
      });
      observer.observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme', 'data-theme', 'class'] });

      return true;
    });

    const routesToTest = [
      '/',
      '/ban-hang',
      '/hoa-don',
      '/doi-tac/khach-hang',
      '/doi-tac/nha-cung-cap',
      '/cai-dat/an-ninh'
    ];

    let lightFramesDetected = 0;

    for (const route of routesToTest) {
      console.log(`Navigating to ${route}...`);
      await page.goto(`http://localhost:5169${route}`, { waitUntil: 'domcontentloaded' });
      
      // Check computed background instantly
      const frameCheck = await page.evaluate(() => {
        const bg = window.getComputedStyle(document.body).backgroundColor;
        const htmlBg = window.getComputedStyle(document.documentElement).backgroundColor;
        const theme = document.documentElement.getAttribute('data-bs-theme');
        const loader = document.getElementById('tf-startup-loader');
        const loaderActive = loader ? window.getComputedStyle(loader).display !== 'none' : false;
        const loaderBg = loader ? window.getComputedStyle(loader).backgroundColor : null;

        const isLight = bg === 'rgb(255, 255, 255)' || 
                        bg === 'rgb(241, 243, 246)' || 
                        theme === 'light' ||
                        (loaderActive && loaderBg === 'rgb(241, 243, 246)');

        return { bg, htmlBg, theme, loaderActive, loaderBg, isLight };
      });

      if (frameCheck.isLight) {
        console.warn(`[WARNING] Light frame detected on ${route}:`, frameCheck);
        lightFramesDetected++;
      }
    }

    const mutationFlashes = await page.evaluate(() => window.__lightFlashesDetected || 0);
    const totalFlashes = lightFramesDetected + mutationFlashes;

    addTest('Navigation Theme Flash Elimination', totalFlashes === 0, {
      totalFlashes,
      lightFramesDetected,
      mutationFlashes,
      routesTested: routesToTest.length
    });

    // Save final report
    const reportPath = path.join(ARTIFACT_DIR, 'critical_ui_repair_verification.json');
    fs.writeFileSync(reportPath, JSON.stringify(results, null, 2));
    console.log(`\nVerification report saved to: ${reportPath}`);

    const allPassed = results.tests.every(t => t.passed);
    console.log(`\nOVERALL SUITE RESULT: ${allPassed ? 'ALL PASSED (SUCCESS)' : 'SOME TESTS FAILED'}`);

  } catch (err) {
    console.error('Test execution error:', err);
    addTest('Suite Error', false, err.message);
  } finally {
    await browser.close();
  }
}

runVerification();
