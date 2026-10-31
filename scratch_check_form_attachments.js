const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Employees/EmployeeFormModal.razor', 'utf8');
const lines = content.split('\n');
const idx = lines.findIndex(l => l.includes('SubmitEmployeeRequestAsync') || l.includes('DocumentService'));
console.log(lines.slice(idx - 10, idx + 40).join('\n'));
