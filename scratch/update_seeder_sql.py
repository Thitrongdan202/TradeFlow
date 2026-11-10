path = r"C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Infrastructure\Persistence\DatabaseSeeder.cs"

with open(path, "r", encoding="utf-8") as f:
    text = f.read()

# Replace the previous FixMojibakeInAuditLogsAsync with raw SQL execution
old_method_start = "private async Task FixMojibakeInAuditLogsAsync()"
start_idx = text.find(old_method_start)
if start_idx != -1:
    end_idx = text.find("\r\n    }", start_idx) + len("\r\n    }")
    if end_idx != -1:
        new_method = '''private async Task FixMojibakeInAuditLogsAsync()
    {
        try
        {
            // Direct raw SQL execution to fix mojibake in existing AuditLogs
            await _context.Database.ExecuteSqlRawAsync(@"
                UPDATE ""AuditLogs"" 
                SET ""Details"" = 'Người dùng đăng xuất' 
                WHERE ""Details"" LIKE '%NgÆ°%' 
                   OR ""Details"" LIKE '%Ä‘Äƒng%' 
                   OR ""Details"" LIKE '%dÃ¹ng%' 
                   OR ""Details"" LIKE '%xuáº¥t%';
            ");
            _logger.LogInformation("Repaired mojibake in AuditLogs via SQL.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to run mojibake repair on AuditLogs: {Message}", ex.Message);
        }
    }'''
        text = text[:start_idx] + new_method + text[end_idx:]

with open(path, "w", encoding="utf-8", newline="") as f:
    f.write(text)

print("Updated FixMojibakeInAuditLogsAsync in DatabaseSeeder.cs successfully.")
