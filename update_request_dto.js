const fs = require('fs');

const path = 'src/TradeFlow.Application/Common/Models/Employees/EmployeeRequestDto.cs';
let content = fs.readFileSync(path, 'utf8');
content = content.replace(
  '    public DateTime CreatedAt { get; set; }',
  '    public DateTime CreatedAt { get; set; }\n    public DateTime? UpdatedAt { get; set; }'
);
fs.writeFileSync(path, content, 'utf8');
console.log('Updated EmployeeRequestDto.cs with UpdatedAt');
