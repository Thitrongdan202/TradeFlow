import os

path = r'src/TradeFlow.Web/Program.cs'
with open(path, 'r', encoding='utf-8') as f:
    c = f.read()

target = """    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});"""

repl = """    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    // Đảm bảo cookie session bị thu hồi tức thì khi Admin cập nhật SecurityStamp
    options.ValidationInterval = TimeSpan.FromSeconds(2);
});"""

c = c.replace(target, repl, 1)

with open(path, 'w', encoding='utf-8') as f:
    f.write(c)

print('SUCCESS')
