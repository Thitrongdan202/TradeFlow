const fs = require('fs');
const path = require('path');

function searchDir(dir) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const e of entries) {
    const full = path.join(dir, e.name);
    if (e.isDirectory() && e.name !== 'bin' && e.name !== 'obj' && e.name !== 'node_modules') {
      searchDir(full);
    } else if (e.isFile() && (e.name.endsWith('.cs') || e.name.endsWith('.razor'))) {
      const content = fs.readFileSync(full, 'utf8');
      const matches = content.match(/Permission:[a-zA-Z0-9:]+/g);
      if (matches) {
        console.log(`${full}:`, Array.from(new Set(matches)));
      }
    }
  }
}

searchDir('src');
