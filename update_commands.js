const fs = require('fs');

const path = 'src/TradeFlow.Application/Common/Models/Employees/EmployeeCommands.cs';
let content = fs.readFileSync(path, 'utf8');

const additionalCommands = `
public class ResubmitEmployeeRequestCommand : CreateEmployeeRequestCommand
{
    public int RequestId { get; set; }
    public string? ResubmitNotes { get; set; }
}
`;

content = content.trimEnd() + '\n' + additionalCommands;
fs.writeFileSync(path, content, 'utf8');
console.log('Updated EmployeeCommands.cs with ResubmitEmployeeRequestCommand');
