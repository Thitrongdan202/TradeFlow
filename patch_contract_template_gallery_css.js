const fs = require('fs');
const file = 'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web/Components/Pages/Contracts/ContractTemplateGallery.razor.css';
let content = fs.readFileSync(file, 'utf8');

// 1. Search input transition
content = content.replace(
    '    transition: all 0.2s ease;',
    '    transition: border-color 0.1s ease, box-shadow 0.1s ease;'
);

// 2. Category bar scroll containment
content = content.replace(
    `/* --- Category Filter Pills --- */
.tf-category-bar {
    display: flex;
    align-items: center;
    gap: 8px;
    overflow-x: auto;
    padding-bottom: 2px;
    scrollbar-width: thin;
}`,
    `/* --- Category Filter Pills --- */
.tf-category-bar {
    display: flex;
    align-items: center;
    gap: 8px;
    overflow-x: auto;
    overflow-y: hidden;
    -webkit-overflow-scrolling: touch;
    overscroll-behavior-x: contain;
    padding-bottom: 2px;
    scrollbar-width: thin;
}`
);

// 3. Category pill transition
content = content.replace(
    '    transition: all 0.18s ease;',
    '    transition: background-color 0.08s ease, border-color 0.08s ease, color 0.08s ease;'
);

fs.writeFileSync(file, content, 'utf8');
console.log('Successfully patched ContractTemplateGallery.razor.css!');
