const fs = require('fs');

const path = 'src/TradeFlow.Web/Components/Pages/Employees/Index.razor';
let content = fs.readFileSync(path, 'utf8');

// 1. Update buttons opening create modal
content = content.replace(
  /@onclick="\(\) => _showCreateModal = true"/g,
  '@onclick="OpenCreateModal"'
);

// 2. Update child modal tags
const modalTagTarget = `<EmployeeFormModal @bind-IsOpen="_showCreateModal" OnEmployeeSaved="HandleEmployeeSaved" />
<EmployeeRequestModal IsOpen="_showRequestModal" IsOpenChanged="HandleRequestModalClosed" TargetRequestId="_targetRequestId" OnRequestsChanged="HandleRequestsChanged" />`;

const modalTagReplacement = `<EmployeeFormModal @bind-IsOpen="_showCreateModal" OnEmployeeSaved="HandleEmployeeSaved" PreloadedRequest="_formPreloadedRequest" IsResubmitMode="_isFormResubmitMode" IsCloneMode="_isFormCloneMode" />
<EmployeeRequestModal IsOpen="_showRequestModal" IsOpenChanged="HandleRequestModalClosed" TargetRequestId="_targetRequestId" OnRequestsChanged="HandleRequestsChanged" OnResubmitRequested="HandleResubmitRequested" OnCloneRequested="HandleCloneRequested" />`;

content = content.replace(modalTagTarget, modalTagReplacement);

// 3. Add variables and methods in @code
const codeVarTarget = `    private bool _showCreateModal;
    private bool _showRequestModal;`;

const codeVarReplacement = `    private bool _showCreateModal;
    private bool _showRequestModal;
    private EmployeeRequestDto? _formPreloadedRequest;
    private bool _isFormResubmitMode;
    private bool _isFormCloneMode;`;

content = content.replace(codeVarTarget, codeVarReplacement);

const methodsTarget = `    private void OpenRequestModal()
    {
        _targetRequestId = null;
        _showRequestModal = true;
    }`;

const methodsReplacement = `    private void OpenCreateModal()
    {
        _formPreloadedRequest = null;
        _isFormResubmitMode = false;
        _isFormCloneMode = false;
        _showCreateModal = true;
    }

    private void HandleResubmitRequested(EmployeeRequestDto req)
    {
        _formPreloadedRequest = req;
        _isFormResubmitMode = true;
        _isFormCloneMode = false;
        _showCreateModal = true;
    }

    private void HandleCloneRequested(EmployeeRequestDto req)
    {
        _formPreloadedRequest = req;
        _isFormResubmitMode = false;
        _isFormCloneMode = true;
        _showCreateModal = true;
    }

    private void OpenRequestModal()
    {
        _targetRequestId = null;
        _showRequestModal = true;
    }`;

content = content.replace(methodsTarget, methodsReplacement);

fs.writeFileSync(path, content, 'utf8');
console.log('Successfully updated Index.razor');
