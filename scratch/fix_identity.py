path = r"C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Web\Components\Account\IdentityComponentsEndpointRouteBuilderExtensions.cs"
with open(path, "r", encoding="utf-8") as f:
    lines = f.readlines()

replaced = False
for i, line in enumerate(lines):
    if "NgÆ°" in line:
        lines[i] = '                    details: "Người dùng đăng xuất",\r\n'
        replaced = True
        print(f"Replaced line {i+1}")

if replaced:
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.writelines(lines)
    print("File saved successfully.")
else:
    print("Pattern not found!")
