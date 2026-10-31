const fs = require('fs');
const file = 'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web/Components/Pages/Employees/EmployeeRequestModal.razor';
let content = fs.readFileSync(file, 'utf8');

// 1. Add IAuthorizationService inject
content = content.replace(
    '@inject AuthenticationStateProvider AuthStateProvider',
    '@inject AuthenticationStateProvider AuthStateProvider\r\n@inject Microsoft.AspNetCore.Authorization.IAuthorizationService AuthorizationService'
);

// 2. Replace AuthorizeView with @if (_canApprove)
content = content.replace(
    '                                    <AuthorizeView Policy="Permission:EmployeeRequests:Approve">',
    '                                    @if (_canApprove)\r\n                                    {'
);
content = content.replace(
    '                                    </AuthorizeView>',
    '                                    }'
);

// 3. Add fields
content = content.replace(
    '    private List<EmployeeRequestDto> _allRequests = new();',
    '    private readonly Dictionary<EmployeeRequestStatus, List<EmployeeRequestDto>> _requestsByStatus = new();\r\n    private List<EmployeeRequestDto> _allRequests = new();\r\n    private bool _canApprove;'
);

// 4. Update OnParametersSetAsync
content = content.replace(
    '            _reviewNotes = string.Empty;\r\n            if (!TargetRequestId.HasValue)',
    '            _reviewNotes = string.Empty;\r\n\r\n            var authState = await AuthStateProvider.GetAuthenticationStateAsync();\r\n            var authResult = await AuthorizationService.AuthorizeAsync(authState.User, "Permission:EmployeeRequests:Approve");\r\n            _canApprove = authResult.Succeeded;\r\n\r\n            if (!TargetRequestId.HasValue)'
);

// 5. Update LoadRequestsAsync
content = content.replace(
    '            _needDocsCount = _allRequests.Count(r => r.Status == EmployeeRequestStatus.NeedAdditionalDocs);\r\n            if (TargetRequestId.HasValue)',
    '            _needDocsCount = _allRequests.Count(r => r.Status == EmployeeRequestStatus.NeedAdditionalDocs);\r\n\r\n            _requestsByStatus.Clear();\r\n            _requestsByStatus[EmployeeRequestStatus.Pending] = _allRequests.Where(r => r.Status == EmployeeRequestStatus.Pending).ToList();\r\n            _requestsByStatus[EmployeeRequestStatus.NeedAdditionalDocs] = _allRequests.Where(r => r.Status == EmployeeRequestStatus.NeedAdditionalDocs).ToList();\r\n            _requestsByStatus[EmployeeRequestStatus.Approved] = _allRequests.Where(r => r.Status == EmployeeRequestStatus.Approved).ToList();\r\n            _requestsByStatus[EmployeeRequestStatus.Rejected] = _allRequests.Where(r => r.Status == EmployeeRequestStatus.Rejected).ToList();\r\n\r\n            if (TargetRequestId.HasValue)'
);

// 6. Update FilterByStatus & ApplyFilter
const oldFilter = '    private void FilterByStatus(EmployeeRequestStatus? status)\r\n    {\r\n        _selectedStatus = status;\r\n        _processingRequestId = null;\r\n        ApplyFilter();\r\n    }\r\n\r\n    private void ApplyFilter()\r\n    {\r\n        if (_selectedStatus.HasValue)\r\n        {\r\n            _filteredRequests = _allRequests.Where(r => r.Status == _selectedStatus.Value).ToList();\r\n        }\r\n        else\r\n        {\r\n            _filteredRequests = _allRequests.ToList();\r\n        }\r\n    }';

const newFilter = '    private void FilterByStatus(EmployeeRequestStatus? status)\r\n    {\r\n        if (_selectedStatus == status) return;\r\n        _selectedStatus = status;\r\n        _processingRequestId = null;\r\n        ApplyFilter();\r\n    }\r\n\r\n    private void ApplyFilter()\r\n    {\r\n        if (_selectedStatus.HasValue && _requestsByStatus.TryGetValue(_selectedStatus.Value, out var list))\r\n        {\r\n            _filteredRequests = list;\r\n        }\r\n        else\r\n        {\r\n            _filteredRequests = _allRequests;\r\n        }\r\n    }';

content = content.replace(oldFilter, newFilter);

fs.writeFileSync(file, content, 'utf8');
console.log('Update complete!');
