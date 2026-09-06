const fs = require('fs');
const path = require('path');

function walkDir(dir) {
    let results = [];
    const list = fs.readdirSync(dir);
    list.forEach(file => {
        file = path.join(dir, file);
        const stat = fs.statSync(file);
        if (stat && stat.isDirectory()) {
            results = results.concat(walkDir(file));
        } else {
            if (file.endsWith('.cs') || file.endsWith('.razor')) {
                results.push(file);
            }
        }
    });
    return results;
}

const files = walkDir('src');
files.forEach(file => {
    let content = fs.readFileSync(file, 'utf8');
    let newContent = content
        .split('Quýotation').join('Quotation')
        .split('Quýarter').join('Quarter')
        .split('Quýery').join('Query')
        .split('reQuýired').join('required')
        .split('reQuýest').join('request')
        .split('DANH MỤCŨ').join('DANH MỤC');
    
    if (content !== newContent) {
        fs.writeFileSync(file, newContent, 'utf8');
        console.log("Fixed", file);
    }
});
