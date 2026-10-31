const fs = require('fs');

const path = 'src/TradeFlow.Application/Common/Interfaces/IEmployeeService.cs';
let content = fs.readFileSync(path, 'utf8');

const target = `    Task<EmployeeRequestDto> SubmitEmployeeRequestAsync(CreateEmployeeRequestCommand command, string requestedBy, CancellationToken cancellationToken = default);
    Task<EmployeeDto?> ReviewEmployeeRequestAsync(ReviewEmployeeRequestCommand command, string reviewedBy, CancellationToken cancellationToken = default);`;

const replacement = `    Task<EmployeeRequestDto> SubmitEmployeeRequestAsync(CreateEmployeeRequestCommand command, string requestedBy, CancellationToken cancellationToken = default);
    Task<EmployeeRequestDto> ResubmitEmployeeRequestAsync(ResubmitEmployeeRequestCommand command, string performedBy, CancellationToken cancellationToken = default);
    Task<EmployeeDto?> ReviewEmployeeRequestAsync(ReviewEmployeeRequestCommand command, string reviewedBy, CancellationToken cancellationToken = default);`;

content = content.replace(target, replacement);
fs.writeFileSync(path, content, 'utf8');
console.log('Updated IEmployeeService.cs with ResubmitEmployeeRequestAsync');
