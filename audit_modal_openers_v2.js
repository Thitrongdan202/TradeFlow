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

  // Search for patterns where an await happens right before setting modal to true
  const lines = content.split('\n');
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    if (/(?:_show|_is)\w*(?:Modal|Dialog|Popup|Drawer|DrawerOpen)\s*=\s*true/.test(line)) {
      // Check previous 15 lines for await
      const prevLines = lines.slice(Math.max(0, i - 15), i);
      const awaitLine = prevLines.find(l => /\bawait\s+/.test(l) && !/StateHasChanged/.test(l));
      if (awaitLine) {
        console.log(`[AWAIT BEFORE MODAL OPEN] ${relPath} (line ${i + 1}):`);
        console.log(`   Modal open line: ${line.trim()}`);
        console.log(`   Preceding await: ${awaitLine.trim()}`);
      }
    }
  }
}
