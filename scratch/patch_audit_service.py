import os

path = r'src/TradeFlow.Infrastructure/Services/AuditService.cs'
with open(path, 'r', encoding='utf-8') as f:
    c = f.read()

op_target = """            AuditEventType.PriceListCreated or
            AuditEventType.PriceListApproved => true,"""

op_repl = """            AuditEventType.PriceListCreated or
            AuditEventType.PriceListApproved or
            AuditEventType.PasswordReset or
            AuditEventType.PasswordChanged or
            AuditEventType.TemporaryPasswordIssued or
            AuditEventType.ForcedPasswordChange or
            AuditEventType.SessionRevoked or
            AuditEventType.AccountDisabled or
            AuditEventType.SuspiciousLogin or
            AuditEventType.UserLocked or
            AuditEventType.UserUnlocked => true,"""

title_target = """            AuditEventType.PermissionAssigned => "Gán quyền vai trò",
            AuditEventType.PermissionRevoked => "Thu hồi quyền vai trò","""

title_repl = """            AuditEventType.PermissionAssigned => "Gán quyền vai trò",
            AuditEventType.PermissionRevoked => "Thu hồi quyền vai trò",
            AuditEventType.PasswordReset => "Đặt lại mật khẩu tài khoản",
            AuditEventType.PasswordChanged => "Mật khẩu tài khoản đã thay đổi",
            AuditEventType.TemporaryPasswordIssued => "Cấp mật khẩu tạm thời",
            AuditEventType.ForcedPasswordChange => "Yêu cầu buộc đổi mật khẩu",
            AuditEventType.SessionRevoked => "Thu hồi phiên đăng nhập",
            AuditEventType.UserLocked => "Khóa tài khoản người dùng",
            AuditEventType.UserUnlocked => "Mở khóa tài khoản người dùng",
            AuditEventType.AccountDisabled => "Vô hiệu hóa tài khoản",
            AuditEventType.LoginFailed => "Cảnh báo đăng nhập thất bại",
            AuditEventType.SuspiciousLogin => "Cảnh báo bảo mật: Đăng nhập đáng ngờ","""

url_target = """            "role" or "rolepermission" => "/cai-dat/vai-tro","""
url_repl = """            "role" or "rolepermission" => "/cai-dat/vai-tro",
            "user" or "applicationuser" => "/cai-dat/nguoi-dung","""

c = c.replace(op_target, op_repl, 1).replace(title_target, title_repl, 1).replace(url_target, url_repl, 1)

with open(path, 'w', encoding='utf-8') as f:
    f.write(c)

print('SUCCESS')
