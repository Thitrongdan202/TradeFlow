const { execSync } = require('child_process');
const fs = require('fs');

const head = execSync('git show HEAD:src/TradeFlow.Web/Components/Layout/NavMenu.razor', { cwd: 'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow' }).toString('utf8');

const regex = /href="([^"]+)"[^>]*>([\s\S]*?)<\/NavLink>/g;
let match;
const links = [];
const seen = new Set();
while ((match = regex.exec(head)) !== null) {
    const href = match[1];
    const text = match[2].replace(/<[^>]+>/g, '').trim();
    const key = `${href} :: ${text}`;
    if (!seen.has(key)) {
        seen.add(key);
        links.push({ href, text });
    }
}

console.log('Total unique links in HEAD NavMenu:', links.length);
console.log(JSON.stringify(links, null, 2));
