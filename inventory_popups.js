const fs = require('fs');
const path = require('path');

const baseDir = path.join(__dirname, 'src/TradeFlow.Web');

function findFiles(dir, filter) {
  let results = [];
  const list = fs.readdirSync(dir);
  for (const item of list) {
    const fullPath = path.join(dir, item);
    const stat = fs.statSync(fullPath);
    if (stat && stat.isDirectory()) {
      results = results.concat(findFiles(fullPath, filter));
    } else if (filter(fullPath)) {
      results.push(fullPath);
    }
  }
  return results;
}

const razorFiles = findFiles(baseDir, f => f.endsWith('.razor'));
console.log(`Found ${razorFiles.length} razor files.`);

const popups = [];

for (const file of razorFiles) {
  const content = fs.readFileSync(file, 'utf8');
  const filename = path.basename(file);
  const isModalName = /modal|dialog|popup|drawer|offcanvas/i.test(filename);
  const hasModalClass = /tf-modal|modal-dialog|modal-backdrop|\bmodal\b|tf-drawer|tf-offcanvas|tf-modal-overlay/i.test(content);
  const hasIsOpenParam = /\[Parameter\]\s*public\s+bool\s+IsOpen/i.test(content);
  const hasShowModalField = /bool\s+(_show|_is)(.*)(Modal|Dialog|Popup)/i.test(content);

  if (isModalName || hasModalClass || hasIsOpenParam) {
    const relPath = path.relative(baseDir, file).replace(/\\/g, '/');
    const moduleMatch = relPath.match(/^Components\/Pages\/([^/]+)/);
    const moduleName = moduleMatch ? moduleMatch[1] : (relPath.includes('Account') ? 'Account' : 'Shared/Common');

    // Check async loading in OnInitialized or OnParametersSet
    const hasOnInitialized = /OnInitializedAsync/i.test(content);
    const hasOnParametersSet = /OnParametersSetAsync/i.test(content);
    const hasServiceCalls = /await\s+\w+Service\./i.test(content) || /await\s+DbContext\./i.test(content);
    const hasStateHasChanged = /StateHasChanged\(\)/i.test(content);
    const hasBackdropFilter = /backdrop-filter/i.test(content);
    const hasTransitionAll = /transition:\s*all/i.test(content);

    popups.push({
      relPath,
      filename,
      moduleName,
      isModalName,
      hasModalClass,
      hasIsOpenParam,
      hasOnInitialized,
      hasOnParametersSet,
      hasServiceCalls,
      hasStateHasChanged
    });
  }
}

console.log(`Identified ${popups.length} modal/dialog/popup components.\n`);
popups.sort((a, b) => a.moduleName.localeCompare(b.moduleName) || a.filename.localeCompare(b.filename));

const modules = {};
popups.forEach(p => {
  if (!modules[p.moduleName]) modules[p.moduleName] = [];
  modules[p.moduleName].push(p);
});

for (const mod in modules) {
  console.log(`=== Module: ${mod} (${modules[mod].length} popups) ===`);
  modules[mod].forEach(p => {
    console.log(`  - ${p.filename} [${p.relPath}]`);
    console.log(`    IsOpenParam: ${p.hasIsOpenParam}, OnInitialized: ${p.hasOnInitialized}, OnParamsSet: ${p.hasOnParametersSet}, ServiceCalls: ${p.hasServiceCalls}`);
  });
}

fs.writeFileSync('popup_inventory.json', JSON.stringify(popups, null, 2), 'utf8');
console.log('\nWrote popup_inventory.json');
