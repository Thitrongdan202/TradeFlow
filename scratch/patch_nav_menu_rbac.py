path = r'C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Web\Components\Layout\NavMenu.razor'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace('href="/cai-dat/vai-tro"', 'href="/cai-dat/phan-quyen"')
text = text.replace('Vai trò &amp; Phân quyền</span>', 'Quản lý quyền truy cập</span>')

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print("NavMenu.razor patched!")
