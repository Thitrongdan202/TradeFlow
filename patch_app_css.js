const fs = require('fs');
const file = 'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web/wwwroot/app.css';
let content = fs.readFileSync(file, 'utf8');

// 1. Optimize .tf-tab-btn, .tf-req-filter-tab
const oldTabBtn = `  outline: none;
  transition: all 0.15s ease-in-out;
  line-height: 1.4;`.replace(/\r?\n/g, '\r\n');

const newTabBtn = `  outline: none;
  transition: background-color 0.08s ease, border-color 0.08s ease, color 0.08s ease;
  line-height: 1.4;`.replace(/\r?\n/g, '\r\n');

// 2. Optimize .tf-segmented-tab
const oldSegTab = `  white-space: nowrap;
  transition: all 0.16s cubic-bezier(0.16, 1, 0.3, 1);
  text-decoration: none;`.replace(/\r?\n/g, '\r\n');

const newSegTab = `  white-space: nowrap;
  transition: background-color 0.08s ease, border-color 0.08s ease, color 0.08s ease, box-shadow 0.08s ease;
  text-decoration: none;`.replace(/\r?\n/g, '\r\n');

if (content.includes(oldTabBtn)) {
    content = content.replace(oldTabBtn, newTabBtn);
    console.log('Replaced .tf-tab-btn transition');
} else {
    console.log('oldTabBtn not found');
}

if (content.includes(oldSegTab)) {
    content = content.replace(oldSegTab, newSegTab);
    console.log('Replaced .tf-segmented-tab transition');
} else {
    console.log('oldSegTab not found');
}

fs.writeFileSync(file, content, 'utf8');
console.log('Successfully patched app.css transitions!');
