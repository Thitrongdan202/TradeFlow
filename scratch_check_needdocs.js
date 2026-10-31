const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Employees/EmployeeRequestModal.razor', 'utf8');
const lines = content.split('\n');
lines.forEach((l, idx) => {
  if (l.includes('NeedAdditionalDocs')) {
    console.log((idx+1) + ': ' + l);
  }
});
