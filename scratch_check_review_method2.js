const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Infrastructure/Services/EmployeeService.cs', 'utf8');
const lines = content.split('\n');
const idx = lines.findIndex(l => l.includes('ReviewEmployeeRequestAsync'));
console.log(lines.slice(idx + 70, idx + 140).join('\n'));
