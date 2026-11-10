path = r'C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Web\Components\Pages\Admin\Settings\Index.razor'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace('href="/cai-dat/vai-tro"', 'href="/cai-dat/phan-quyen"')
text = text.replace('<h3 class="tf-settings-card-title">Vai trò &amp; Phân quyền</h3>', '<h3 class="tf-settings-card-title">Quản lý quyền truy cập</h3>')

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print("Settings/Index.razor patched!")
