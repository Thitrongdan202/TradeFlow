path = r"C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Web\Components\Layout\NavMenu.razor"

with open(path, "r", encoding="utf-8") as f:
    text = f.read()

old_partners = """    private void TogglePartners()
    {
        if (!IsSectionActive("doi-tac"))
        {
            IsPartnersOpen = true;
            Nav.NavigateTo("/doi-tac/khach-hang");
        }
        else
        {
            IsPartnersOpen = !IsPartnersOpen;
        }
    }"""

new_partners = "    private void TogglePartners() => IsPartnersOpen = !IsPartnersOpen;"

old_settings = """    private void ToggleSettings()
    {
        if (!IsSectionActive("cai-dat"))
        {
            IsSettingsOpen = true;
            Nav.NavigateTo("/cai-dat");
        }
        else
        {
            IsSettingsOpen = !IsSettingsOpen;
        }
    }"""

new_settings = "    private void ToggleSettings() => IsSettingsOpen = !IsSettingsOpen;"

if old_partners in text:
    text = text.replace(old_partners, new_partners)
    print("Updated TogglePartners")
else:
    print("Old partners not matched")

if old_settings in text:
    text = text.replace(old_settings, new_settings)
    print("Updated ToggleSettings")
else:
    print("Old settings not matched")

with open(path, "w", encoding="utf-8", newline="") as f:
    f.write(text)

print("Updated NavMenu.razor successfully.")
