const fs = require('fs');

const path = 'src/TradeFlow.Domain/Enums/AuditEventType.cs';
let content = fs.readFileSync(path, 'utf8');
content = content.replace(
  'EmployeeRequestAdditionalDocsRequested = 189,',
  'EmployeeRequestAdditionalDocsRequested = 189,\n    EmployeeRequestResubmitted = 194,'
);
fs.writeFileSync(path, content, 'utf8');
console.log('Updated AuditEventType.cs');
