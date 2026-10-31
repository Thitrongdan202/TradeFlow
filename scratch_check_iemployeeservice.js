const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Application/Common/Interfaces/IEmployeeService.cs', 'utf8');
console.log(content);
