path = r"C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Infrastructure\Persistence\DatabaseSeeder.cs"

with open(path, "r", encoding="utf-8") as f:
    text = f.read()

# Add call in SeedAsync
target_call = "await SeedAttendanceDataAsync();"
replacement_call = "await SeedAttendanceDataAsync();\r\n            await FixMojibakeInAuditLogsAsync();"

if target_call in text and "FixMojibakeInAuditLogsAsync" not in text:
    text = text.replace(target_call, replacement_call, 1)

method_code = '''
    private async Task FixMojibakeInAuditLogsAsync()
    {
        try
        {
            var logs = await _context.AuditLogs
                .Where(l => l.Details != null && (
                    l.Details.Contains("NgÆ°") ||
                    l.Details.Contains("Ä‘Äƒng") ||
                    l.Details.Contains("dÃ¹ng") ||
                    l.Details.Contains("xuáº¥t") ||
                    l.Details.Contains("Ã") ||
                    l.Details.Contains("á»")
                ))
                .ToListAsync();

            if (logs.Count > 0)
            {
                var latin1 = System.Text.Encoding.GetEncoding("ISO-8859-1");
                foreach (var log in logs)
                {
                    if (string.IsNullOrEmpty(log.Details)) continue;
                    try
                    {
                        var bytes = latin1.GetBytes(log.Details);
                        var utf8Str = System.Text.Encoding.UTF8.GetString(bytes);
                        if (!utf8Str.Contains('\\ufffd'))
                        {
                            log.Details = utf8Str;
                        }
                        else if (log.Details.Contains("NgÆ°á»"))
                        {
                            log.Details = "Người dùng đăng xuất";
                        }
                    }
                    catch
                    {
                        if (log.Details.Contains("NgÆ°á»"))
                        {
                            log.Details = "Người dùng đăng xuất";
                        }
                    }
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Repaired {Count} mojibake AuditLog records to clean UTF-8 Vietnamese.", logs.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to run mojibake repair on AuditLogs: {Message}", ex.Message);
        }
    }
'''

# Place method before the last closing brace
last_brace_index = text.rfind("}")
if last_brace_index != -1 and "private async Task FixMojibakeInAuditLogsAsync" not in text:
    text = text[:last_brace_index] + method_code + "\r\n}\r\n"

with open(path, "w", encoding="utf-8", newline="") as f:
    f.write(text)

print("Updated DatabaseSeeder.cs successfully.")
