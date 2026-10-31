const fs = require('fs');
const file = 'C:/Users/thitr/.gemini/antigravity/scratch/TradeFlow/src/TradeFlow.Web/Components/Pages/Employees/EmployeeRequestModal.razor';
let content = fs.readFileSync(file, 'utf8');

const target = `    private int? _processingRequestId;
    private string _reviewNotes = string.Empty;

    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen)
        {
            _alertMessage = null;
            _processingRequestId = null;
            _reviewNotes = string.Empty;

            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var authResult = await AuthorizationService.AuthorizeAsync(authState.User, "Permission:EmployeeRequests:Approve");
            _canApprove = authResult.Succeeded;

            if (!TargetRequestId.HasValue)
            {
                _selectedStatus = EmployeeRequestStatus.Pending;
            }
            await LoadRequestsAsync();
        }
    }`.replace(/\r?\n/g, '\r\n');

const replacement = `    private int? _processingRequestId;
    private string _reviewNotes = string.Empty;
    private bool _wasOpen;
    private int? _lastTargetRequestId;

    protected override async Task OnParametersSetAsync()
    {
        if (IsOpen)
        {
            bool justOpened = !_wasOpen;
            bool targetChanged = TargetRequestId != _lastTargetRequestId;
            _wasOpen = true;
            _lastTargetRequestId = TargetRequestId;

            if (justOpened || targetChanged)
            {
                _alertMessage = null;
                _processingRequestId = null;
                _reviewNotes = string.Empty;

                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                var authResult = await AuthorizationService.AuthorizeAsync(authState.User, "Permission:EmployeeRequests:Approve");
                _canApprove = authResult.Succeeded;

                if (!TargetRequestId.HasValue && justOpened)
                {
                    _selectedStatus = EmployeeRequestStatus.Pending;
                }
                await LoadRequestsAsync();
            }
        }
        else
        {
            _wasOpen = false;
        }
    }`.replace(/\r?\n/g, '\r\n');

if (content.includes(target)) {
    content = content.replace(target, replacement);
    fs.writeFileSync(file, content, 'utf8');
    console.log('Successfully updated OnParametersSetAsync in EmployeeRequestModal.razor');
} else {
    console.error('Target not found in EmployeeRequestModal.razor');
}
