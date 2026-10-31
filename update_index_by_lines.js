const fs = require('fs');

const path = 'src/TradeFlow.Web/Components/Pages/Employees/Index.razor';
let lines = fs.readFileSync(path, 'utf8').split(/\r?\n/);

// 1. Update lines around Child Modals
const modalIdx = lines.findIndex(l => l.includes('<EmployeeFormModal @bind-IsOpen="_showCreateModal"'));
if (modalIdx !== -1) {
  lines[modalIdx] = '    <EmployeeFormModal @bind-IsOpen="_showCreateModal" OnEmployeeSaved="HandleEmployeeSaved" PreloadedRequest="_formPreloadedRequest" IsResubmitMode="_isFormResubmitMode" IsCloneMode="_isFormCloneMode" />';
  lines[modalIdx + 1] = '    <EmployeeRequestModal IsOpen="_showRequestModal" IsOpenChanged="HandleRequestModalClosed" TargetRequestId="_targetRequestId" OnRequestsChanged="HandleRequestsChanged" OnResubmitRequested="HandleResubmitRequested" OnCloneRequested="HandleCloneRequested" />';
}

// 2. Add fields in @code
const codeIdx = lines.findIndex(l => l.includes('private bool _showCreateModal;'));
if (codeIdx !== -1) {
  lines.splice(codeIdx + 2, 0,
    '    private EmployeeRequestDto? _formPreloadedRequest;',
    '    private bool _isFormResubmitMode;',
    '    private bool _isFormCloneMode;'
  );
}

// 3. Add OpenCreateModal, HandleResubmitRequested, HandleCloneRequested
const openReqIdx = lines.findIndex(l => l.includes('private void OpenRequestModal()'));
if (openReqIdx !== -1) {
  lines.splice(openReqIdx, 0,
    '    private void OpenCreateModal()',
    '    {',
    '        _formPreloadedRequest = null;',
    '        _isFormResubmitMode = false;',
    '        _isFormCloneMode = false;',
    '        _showCreateModal = true;',
    '    }',
    '',
    '    private void HandleResubmitRequested(EmployeeRequestDto req)',
    '    {',
    '        _formPreloadedRequest = req;',
    '        _isFormResubmitMode = true;',
    '        _isFormCloneMode = false;',
    '        _showCreateModal = true;',
    '    }',
    '',
    '    private void HandleCloneRequested(EmployeeRequestDto req)',
    '    {',
    '        _formPreloadedRequest = req;',
    '        _isFormResubmitMode = false;',
    '        _isFormCloneMode = true;',
    '        _showCreateModal = true;',
    '    }',
    ''
  );
}

fs.writeFileSync(path, lines.join('\r\n'), 'utf8');
console.log('Successfully updated Index.razor by lines');
