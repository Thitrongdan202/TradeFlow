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
const inlineModals = [];

for (const file of razorFiles) {
  const content = fs.readFileSync(file, 'utf8');
  const filename = path.basename(file);
  const relPath = path.relative(baseDir, file).replace(/\\/g, '/');

  // Match inline modal markup like <div class="tf-modal... or <div class="modal...
  const matches = [...content.matchAll(/<div[^>]*class=["']([^"']*(?:tf-modal|modal-dialog|modal-backdrop|tf-drawer|tf-offcanvas)[^"']*)["'][^>]*>/gi)];
  if (matches.length > 0) {
    inlineModals.push({
      relPath,
      filename,
      matches: matches.map(m => m[0].slice(0, 100))
    });
  }
}

console.log(`Found ${inlineModals.length} razor files containing modal markup:\n`);
inlineModals.forEach(m => {
  console.log(`- ${m.relPath}`);
  m.matches.forEach(match => console.log(`    ${match}`));
});
