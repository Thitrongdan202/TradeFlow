path = r"C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Infrastructure\Persistence\DatabaseSeeder.cs"

with open(path, "r", encoding="utf-8") as f:
    lines = f.readlines()

# 1. Insert call in SeedAsync after line with SeedWarehouseOperationsDataAsync
call_inserted = False
for i, line in enumerate(lines[:120]):
    if "await SeedWarehouseOperationsDataAsync();" in line:
        lines.insert(i + 1, "            await FixMojibakeInAuditLogsAsync();\r\n")
        call_inserted = True
        break

assert call_inserted, "Failed to insert call in SeedAsync"

# 2. Insert method before the final closing brace '}\n' or '}'
last_idx = len(lines) - 1
while last_idx >= 0 and lines[last_idx].strip() != "}":
    last_idx -= 1

assert last_idx >= 0, "Failed to find final closing brace"

method_lines = [
    "\r\n",
    "    private async Task FixMojibakeInAuditLogsAsync()\r\n",
    "    {\r\n",
    "        try\r\n",
    "        {\r\n",
    "            await _context.Database.ExecuteSqlRawAsync(@\"\r\n",
    "                UPDATE \"\"AuditLogs\"\" \r\n",
    "                SET \"\"Details\"\" = 'Người dùng đăng xuất' \r\n",
    "                WHERE \"\"Details\"\" LIKE '%NgÆ°%' \r\n",
    "                   OR \"\"Details\"\" LIKE '%Ä‘Äƒng%' \r\n",
    "                   OR \"\"Details\"\" LIKE '%dÃ¹ng%' \r\n",
    "                   OR \"\"Details\"\" LIKE '%xuáº¥t%';\r\n",
    "            \");\r\n",
    "            _logger.LogInformation(\"Repaired mojibake in AuditLogs via SQL.\");\r\n",
    "        }\r\n",
    "        catch (Exception ex)\r\n",
    "        {\r\n",
    "            _logger.LogWarning(ex, \"Failed to run mojibake repair on AuditLogs: {Message}\", ex.Message);\r\n",
    "        }\r\n",
    "    }\r\n"
]

lines[last_idx:last_idx] = method_lines

with open(path, "w", encoding="utf-8", newline="") as f:
    f.writelines(lines)

print("Updated DatabaseSeeder.cs cleanly!")
