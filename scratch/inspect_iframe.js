const fs = require('fs');
const html = fs.readFileSync('C:\\Users\\thitr\\.gemini\\antigravity\\brain\\fb94d497-815d-460b-bd53-9de32ef4d901\\repro_create_print_iframe.html', 'utf8');

const footers = html.match(/<div[^>]*tf-a4-page-footer[^>]*>([\s\S]*?)<\/div>/gi) || [];
console.log('Footer div count:', footers.length);
footers.forEach((f, idx) => {
    const text = f.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
    console.log(`Footer ${idx + 1}:`, text);
});
