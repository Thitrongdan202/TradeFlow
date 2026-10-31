const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Employees/EmployeeFormModal.razor', 'utf8');
const lines = content.split('\n');
console.log(lines.slice(0, 50).join('\n'));
console.log('--- Code section parameters ---');
const codeIdx = lines.findIndex(l => l.includes('@code {'));
console.log(lines.slice(codeIdx, codeIdx + 50).join('\n'));
