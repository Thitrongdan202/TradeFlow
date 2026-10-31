const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Application/Common/Models/Employees/EmployeeCommands.cs', 'utf8');
console.log(content);
