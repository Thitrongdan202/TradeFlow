path = r'C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Web\Components\Pages\Admin\Roles\List.razor'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

target = "@inject AuthenticationStateProvider AuthStateProvider"
replacement = """@inject AuthenticationStateProvider AuthStateProvider
@inject NavigationManager Nav"""

text = text.replace(target, replacement)

target_init = "    protected override async Task OnInitializedAsync()\n    {"
replacement_init = """    protected override async Task OnInitializedAsync()
    {
        Nav.NavigateTo("/cai-dat/phan-quyen/roles", replace: true);
        return;"""

text = text.replace(target_init, replacement_init)
text = text.replace(target_init.replace('\n', '\r\n'), replacement_init.replace('\n', '\r\n'))

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print("List.razor redirected to /cai-dat/phan-quyen/roles!")
