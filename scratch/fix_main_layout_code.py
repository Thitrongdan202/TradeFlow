path = r"C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Web\Components\Layout\MainLayout.razor"

with open(path, "r", encoding="utf-8") as f:
    text = f.read()

target1 = "    private ErrorBoundary? _contentErrorBoundary;"
replacement1 = """    private ErrorBoundary? _contentErrorBoundary;
    private string _currentTimeFormatted = DateTime.Now.ToString("HH:mm • dd/MM/yyyy");
    private System.Threading.Timer? _clockTimer;"""

target2 = "        Nav.LocationChanged += OnLocationChanged;"
replacement2 = """        Nav.LocationChanged += OnLocationChanged;
        _clockTimer = new System.Threading.Timer(_ =>
        {
            _currentTimeFormatted = DateTime.Now.ToString("HH:mm • dd/MM/yyyy");
            InvokeAsync(StateHasChanged);
        }, null, 10000, 30000);"""

target3 = "        Nav.LocationChanged -= OnLocationChanged;"
replacement3 = """        Nav.LocationChanged -= OnLocationChanged;
        _clockTimer?.Dispose();"""

if target1 in text:
    text = text.replace(target1, replacement1, 1)
    print("Replaced target1")
else:
    print("Target1 not found!")

if target2 in text:
    text = text.replace(target2, replacement2, 1)
    print("Replaced target2")
else:
    print("Target2 not found!")

if target3 in text:
    text = text.replace(target3, replacement3, 1)
    print("Replaced target3")
else:
    print("Target3 not found!")

with open(path, "w", encoding="utf-8", newline="") as f:
    f.write(text)

print("Updated MainLayout.razor code successfully.")
