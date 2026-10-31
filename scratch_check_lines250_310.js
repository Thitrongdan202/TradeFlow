const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Employees/EmployeeRequestModal.razor', 'utf8');
const lines = content.split('\n');
console.log(lines.slice(250, 310).join('\n'));
