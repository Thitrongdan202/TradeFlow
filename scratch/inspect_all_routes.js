const fs = require('fs');
const path = require('path');

const baseDir = 'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web/Components/Pages';

function getPages(dir, fileList = []) {
    const files = fs.readdirSync(dir);
    for (const f of files) {
        const full = path.join(dir, f);
        const stat = fs.statSync(full);
        if (stat.isDirectory()) {
            getPages(full, fileList);
        } else if (f.endsWith('.razor')) {
            const content = fs.readFileSync(full, 'utf8');
            const pageMatch = content.match(/@page\s+"([^"]+)"/g);
            if (pageMatch) {
                pageMatch.forEach(pm => {
                    const r = pm.replace('@page', '').replace(/"/g, '').trim();
                    fileList.push({ route: r, file: path.relative('C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web', full).replace(/\\/g, '/') });
                });
            }
        }
    }
    return fileList;
}

const pages = getPages(baseDir);
console.log('Total @page routes found:', pages.length);
pages.sort((a,b) => a.route.localeCompare(b.route));
console.log(JSON.stringify(pages, null, 2));
