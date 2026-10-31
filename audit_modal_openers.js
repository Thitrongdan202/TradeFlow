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

for (const file of razorFiles) {
  const content = fs.readFileSync(file, 'utf8');
  const relPath = path.relative(baseDir, file).replace(/\\/g, '/');

  // Look for async methods that open a modal after awaiting data
  // e.g. async Task Open... { await ...; _showModal = true; }
  const methodMatches = [...content.matchAll(/(?:private|public|protected)\s+async\s+Task\s+(\w+)\s*\([^)]*\)\s*\{([^}]+)\}/g)];
  for (const m of methodMatches) {
    const methodName = m[1];
    const body = m[2];
    if (/(?:_show|_is)\w*(?:Modal|Dialog|Popup|Drawer)\s*=\s*true/.test(body)) {
      // Check if there is an await BEFORE the modal is set to true
      const parts = body.split(/(?:_show|_is)\w*(?:Modal|Dialog|Popup|Drawer)\s*=\s*true/);
      const beforeOpen = parts[0];
      if (/await\s+/.test(beforeOpen)) {
        console.log(`[SLOW OPENER DETECTED] ${relPath} :: ${methodName}()`);
        const awaitLines = beforeOpen.split('\n').filter(l => l.includes('await'));
        awaitLines.forEach(l => console.log(`   --> ${l.trim()}`));
      }
    }
  }
}
