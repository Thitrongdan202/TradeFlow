using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Domain.Entities.Security;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services.Security;

public class SecurityService : ISecurityService
{
    private readonly TradeFlowDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISigningProvider _signingProvider;
    private readonly IAuditService _auditService;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(
        TradeFlowDbContext context,
        UserManager<ApplicationUser> userManager,
        ISigningProvider signingProvider,
        IAuditService auditService,
        ILogger<SecurityService> logger)
    {
        _context = context;
        _userManager = userManager;
        _signingProvider = signingProvider;
        _auditService = auditService;
        _logger = logger;
    }

    public Task<IssuedEnrollmentCodeResult> IssueEnrollmentCodeAsync(
        string targetUserId,
        SignerRole targetRole,
        string position,
        int expirationMinutes,
        string issuedBy,
        CancellationToken cancellationToken = default)
    {
        return IssueEnrollmentCodeAsync(targetUserId, targetRole, position, true, expirationMinutes, issuedBy, cancellationToken);
    }

    public async Task<IssuedEnrollmentCodeResult> IssueEnrollmentCodeAsync(
        string targetUserId,
        SignerRole targetRole,
        string position,
        bool hasExpiration,
        int? expirationMinutes,
        string issuedBy,
        CancellationToken cancellationToken = default)
    {
        var targetUser = await _userManager.FindByIdAsync(targetUserId);
        if (targetUser == null)
        {
            return new IssuedEnrollmentCodeResult { Succeeded = false, ErrorMessage = "Không tìm thấy người dùng được chỉ định." };
        }

        if (targetUser.Status != UserStatus.Active)
        {
            return new IssuedEnrollmentCodeResult { Succeeded = false, ErrorMessage = "Tài khoản người dùng đang bị khóa hoặc vô hiệu hóa." };
        }

        if (string.IsNullOrWhiteSpace(position))
        {
            position = targetRole.ToVietnamese();
        }

        DateTime now = DateTime.UtcNow;
        DateTime? expiresAt = null;
        if (hasExpiration)
        {
            int expMinutes = expirationMinutes.GetValueOrDefault(1440);
            if (expMinutes <= 0) expMinutes = 30;
            expiresAt = now.AddMinutes(expMinutes);
        }

        // Generate 16-char cryptographically random code formatted as TF-SIGN-XXXX-XXXX-XXXX
        string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // readable chars without ambiguous 0/O/1/I
        var rawSb = new StringBuilder("TF-SIGN-");
        for (int i = 0; i < 12; i++)
        {
            if (i > 0 && i % 4 == 0) rawSb.Append('-');
            rawSb.Append(chars[RandomNumberGenerator.GetInt32(chars.Length)]);
        }
        string rawCode = rawSb.ToString();

        // Hash raw code with SHA-256 for secure storage
        string codeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawCode)));

        var enrollmentCode = new SignerEnrollmentCode
        {
            CodeHash = codeHash,
            TargetUserId = targetUser.Id,
            TargetUserName = targetUser.UserName ?? string.Empty,
            TargetFullName = targetUser.FullName ?? targetUser.UserName ?? string.Empty,
            TargetPosition = position.Trim(),
            TargetSignerRole = targetRole,
            HasExpiration = hasExpiration,
            ExpiresAt = expiresAt,
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = now,
            CreatedBy = issuedBy
        };

        _context.SignerEnrollmentCodes.Add(enrollmentCode);
        await _context.SaveChangesAsync(cancellationToken);

        string expiryText = hasExpiration && expiresAt.HasValue
            ? $"hết hạn: {expiresAt.Value:dd/MM/yyyy HH:mm} UTC"
            : "không giới hạn thời gian (dùng 1 lần duy nhất)";

        await _auditService.LogAsync(
            AuditEventType.SignerCodeIssued,
            issuedBy,
            "SignerEnrollmentCode",
            targetUser.UserName,
            $"Cấp mã kích hoạt người ký cho {targetUser.FullName} ({targetUser.UserName}), vai trò: {targetRole.ToVietnamese()}, chức vụ: {position}, {expiryText}");

        return new IssuedEnrollmentCodeResult
        {
            Succeeded = true,
            RawCode = rawCode,
            HasExpiration = hasExpiration,
            ExpiresAt = expiresAt
        };
    }

    public async Task<bool> RevokeEnrollmentCodeAsync(int codeId, string reason, string revokedBy, CancellationToken cancellationToken = default)
    {
        var code = await _context.SignerEnrollmentCodes.FirstOrDefaultAsync(c => c.Id == codeId, cancellationToken);
        if (code == null || code.IsRevoked || code.IsUsed) return false;

        code.IsRevoked = true;
        code.RevokedAt = DateTime.UtcNow;
        code.RevokedBy = revokedBy;
        code.RevocationReason = reason;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.SignerCodeRevoked,
            revokedBy,
            "SignerEnrollmentCode",
            code.TargetUserName,
            $"Hủy mã kích hoạt người ký của {code.TargetFullName}. Lý do: {reason}");

        return true;
    }

    public async Task<List<SignerEnrollmentCodeDto>> GetEnrollmentCodesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SignerEnrollmentCodes
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new SignerEnrollmentCodeDto
            {
                Id = c.Id,
                TargetUserId = c.TargetUserId,
                TargetUserName = c.TargetUserName,
                TargetFullName = c.TargetFullName,
                TargetPosition = c.TargetPosition,
                TargetSignerRole = c.TargetSignerRole,
                HasExpiration = c.HasExpiration,
                ExpiresAt = c.ExpiresAt,
                IsUsed = c.IsUsed,
                UsedAt = c.UsedAt,
                UsedBy = c.UsedBy,
                IsRevoked = c.IsRevoked,
                RevokedAt = c.RevokedAt,
                RevokedBy = c.RevokedBy,
                RevocationReason = c.RevocationReason,
                CreatedBy = c.CreatedBy ?? string.Empty,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SignerEnrollmentResult> EnrollSignerAsync(
        string rawEnrollmentCode,
        string pin,
        string? performedBy = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawEnrollmentCode))
        {
            return new SignerEnrollmentResult { Succeeded = false, ErrorMessage = "Vui lòng nhập mã kích hoạt người ký." };
        }

        if (string.IsNullOrWhiteSpace(pin) || pin.Length < 6)
        {
            return new SignerEnrollmentResult { Succeeded = false, ErrorMessage = "Mã PIN bảo mật phải có ít nhất 6 ký tự." };
        }

        string cleanCode = rawEnrollmentCode.Trim().ToUpperInvariant();
        string codeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cleanCode)));

        var code = await _context.SignerEnrollmentCodes
            .FirstOrDefaultAsync(c => c.CodeHash == codeHash, cancellationToken);

        if (code == null)
        {
            return new SignerEnrollmentResult { Succeeded = false, ErrorMessage = "Mã kích hoạt không chính xác hoặc không tồn tại trong hệ thống." };
        }

        if (code.IsRevoked)
        {
            return new SignerEnrollmentResult { Succeeded = false, ErrorMessage = "Mã kích hoạt này đã bị thu hồi bởi quản trị viên." };
        }

        if (code.IsUsed)
        {
            return new SignerEnrollmentResult { Succeeded = false, ErrorMessage = "Mã kích hoạt này đã được sử dụng trước đó (chỉ dùng 1 lần duy nhất)." };
        }

        if (code.HasExpiration && code.ExpiresAt.HasValue && DateTime.UtcNow > code.ExpiresAt.Value)
        {
            return new SignerEnrollmentResult { Succeeded = false, ErrorMessage = $"Mã kích hoạt đã hết hạn hiệu lực lúc {code.ExpiresAt.Value.ToLocalTime():dd/MM/yyyy HH:mm}." };
        }

        var targetUser = await _userManager.FindByIdAsync(code.TargetUserId);
        if (targetUser == null || targetUser.Status != UserStatus.Active)
        {
            return new SignerEnrollmentResult { Succeeded = false, ErrorMessage = "Tài khoản người dùng không hoạt động hoặc không tồn tại." };
        }

        var company = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);
        string companyName = company?.CompanyName ?? "TRADEFLOW";
        string companyTaxCode = company?.TaxCode ?? "0123456789";

        string subject = $"CN={code.TargetFullName.ToUpperInvariant()}, POSITION={code.TargetPosition.ToUpperInvariant()}, O={companyName.ToUpperInvariant()}, MST={companyTaxCode}, C=VN";

        try
        {
            var keyPair = await _signingProvider.GenerateKeyPairAsync(pin, subject, cancellationToken);

            DateTime now = DateTime.UtcNow;

            var existingSigner = await _context.SignerIdentities
                .FirstOrDefaultAsync(s => (s.UserId == targetUser.Id || (string.IsNullOrEmpty(s.CertificateSerialNumber) && s.SignerRole == code.TargetSignerRole)), cancellationToken);

            SignerIdentity signer;
            if (existingSigner != null)
            {
                signer = existingSigner;
                signer.UserId = targetUser.Id;
                signer.UserName = targetUser.UserName ?? string.Empty;
                signer.FullName = code.TargetFullName;
                signer.Position = code.TargetPosition;
                signer.SignerRole = code.TargetSignerRole;
                signer.Status = SignerStatus.Active;
                signer.ProviderType = _signingProvider.ProviderType;
                signer.CertificateSerialNumber = keyPair.CertificateSerialNumber;
                signer.CertificateSubject = subject;
                signer.CertificateIssuer = $"TradeFlow CA - {companyName}";
                signer.CertificateThumbprint = keyPair.CertificateThumbprint;
                signer.ValidFrom = now;
                signer.ValidTo = now.AddYears(1);
                signer.EncryptedPrivateKey = keyPair.EncryptedPrivateKey;
                signer.KeySalt = keyPair.KeySalt;
                signer.PinVerificationHash = keyPair.PinVerificationHash;
                signer.PublicKeyXml = keyPair.PublicKeyXml;
                signer.PublicKeyPem = keyPair.PublicKeyPem;
                signer.EnrollmentCodeHash = code.CodeHash;
                signer.EnrolledBy = code.CreatedBy ?? performedBy ?? "System";
                signer.EnrolledAt = now;
                signer.FailedPinAttempts = 0;
                signer.LockoutEnd = null;
                signer.UpdatedAt = now;
                signer.UpdatedBy = performedBy ?? targetUser.UserName;
            }
            else
            {
                signer = new SignerIdentity
                {
                    UserId = targetUser.Id,
                    UserName = targetUser.UserName ?? string.Empty,
                    FullName = code.TargetFullName,
                    Position = code.TargetPosition,
                    SignerRole = code.TargetSignerRole,
                    Status = SignerStatus.Active,
                    ProviderType = _signingProvider.ProviderType,
                    CertificateSerialNumber = keyPair.CertificateSerialNumber,
                    CertificateSubject = subject,
                    CertificateIssuer = $"TradeFlow CA - {companyName}",
                    CertificateThumbprint = keyPair.CertificateThumbprint,
                    ValidFrom = now,
                    ValidTo = now.AddYears(1),
                    EncryptedPrivateKey = keyPair.EncryptedPrivateKey,
                    KeySalt = keyPair.KeySalt,
                    PinVerificationHash = keyPair.PinVerificationHash,
                    PublicKeyXml = keyPair.PublicKeyXml,
                    PublicKeyPem = keyPair.PublicKeyPem,
                    EnrollmentCodeHash = code.CodeHash,
                    EnrolledBy = code.CreatedBy ?? performedBy ?? "System",
                    EnrolledAt = now,
                    CreatedAt = now,
                    CreatedBy = performedBy ?? targetUser.UserName
                };
                _context.SignerIdentities.Add(signer);
            }

            // Mark code as used
            code.IsUsed = true;
            code.UsedAt = now;
            code.UsedBy = performedBy ?? targetUser.UserName;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                AuditEventType.SignerEnrolled,
                performedBy ?? targetUser.UserName,
                "SignerIdentity",
                signer.CertificateSerialNumber,
                $"Kích hoạt thành công chứng thư ký số cho {signer.FullName} ({signer.Position}), vai trò: {signer.SignerRole.ToVietnamese()}, sê-ri: {signer.CertificateSerialNumber}");

            return new SignerEnrollmentResult
            {
                Succeeded = true,
                SignerIdentityId = signer.Id,
                CertificateSerialNumber = signer.CertificateSerialNumber,
                FullName = signer.FullName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enrolling signer identity");
            return new SignerEnrollmentResult
            {
                Succeeded = false,
                ErrorMessage = $"Lỗi trong quá trình tạo khóa ký và chứng thư: {ex.Message}"
            };
        }
    }

    public async Task<bool> RevokeSignerAsync(int signerId, string reason, string revokedBy, CancellationToken cancellationToken = default)
    {
        var signer = await _context.SignerIdentities.FirstOrDefaultAsync(s => s.Id == signerId, cancellationToken);
        if (signer == null || signer.Status == SignerStatus.Revoked) return false;

        signer.Status = SignerStatus.Revoked;
        signer.RevokedAt = DateTime.UtcNow;
        signer.RevokedBy = revokedBy;
        signer.RevocationReason = reason;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.SignerRevoked,
            revokedBy,
            "SignerIdentity",
            signer.CertificateSerialNumber,
            $"Thu hồi quyền ký số và chứng thư của {signer.FullName} (Sê-ri: {signer.CertificateSerialNumber}). Lý do: {reason}");

        return true;
    }

    public async Task<List<SignerIdentityDto>> GetAllSignersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SignerIdentities
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SignerIdentityDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserName = s.UserName,
                FullName = s.FullName,
                Position = s.Position,
                SignerRole = s.SignerRole,
                Status = s.Status,
                ProviderType = s.ProviderType,
                CertificateSerialNumber = s.CertificateSerialNumber ?? string.Empty,
                CertificateSubject = s.CertificateSubject ?? string.Empty,
                CertificateIssuer = s.CertificateIssuer ?? string.Empty,
                CertificateThumbprint = s.CertificateThumbprint ?? string.Empty,
                ValidFrom = s.ValidFrom,
                ValidTo = s.ValidTo,
                EnrolledBy = s.EnrolledBy ?? string.Empty,
                EnrolledAt = s.EnrolledAt,
                RevokedAt = s.RevokedAt,
                RevokedBy = s.RevokedBy,
                RevocationReason = s.RevocationReason,
                HandwrittenSignatureImage = s.HandwrittenSignatureImage,
                FailedPinAttempts = s.FailedPinAttempts,
                LockoutEnd = s.LockoutEnd
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SignerIdentityDto>> GetActiveSignersForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        return await _context.SignerIdentities
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.Status == SignerStatus.Active && s.ValidTo >= now)
            .OrderBy(s => s.SignerRole)
            .Select(s => new SignerIdentityDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserName = s.UserName,
                FullName = s.FullName,
                Position = s.Position,
                SignerRole = s.SignerRole,
                Status = s.Status,
                ProviderType = s.ProviderType,
                CertificateSerialNumber = s.CertificateSerialNumber ?? string.Empty,
                CertificateSubject = s.CertificateSubject ?? string.Empty,
                CertificateIssuer = s.CertificateIssuer ?? string.Empty,
                CertificateThumbprint = s.CertificateThumbprint ?? string.Empty,
                ValidFrom = s.ValidFrom,
                ValidTo = s.ValidTo,
                EnrolledBy = s.EnrolledBy ?? string.Empty,
                EnrolledAt = s.EnrolledAt,
                HandwrittenSignatureImage = s.HandwrittenSignatureImage,
                FailedPinAttempts = s.FailedPinAttempts,
                LockoutEnd = s.LockoutEnd
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SignerIdentity?> GetSignerByIdAsync(int signerId, CancellationToken cancellationToken = default)
    {
        return await _context.SignerIdentities
            .FirstOrDefaultAsync(s => s.Id == signerId, cancellationToken);
    }

    public async Task<bool> UpdateSignerProfileAsync(
        int signerId,
        string fullName,
        string position,
        string? handwrittenSignatureImage,
        string performedBy,
        CancellationToken cancellationToken = default)
    {
        var signer = await _context.SignerIdentities.FirstOrDefaultAsync(s => s.Id == signerId, cancellationToken);
        if (signer == null) return false;

        signer.FullName = fullName.Trim();
        signer.Position = position.Trim();
        if (handwrittenSignatureImage != null)
        {
            signer.HandwrittenSignatureImage = handwrittenSignatureImage;
        }
        signer.UpdatedAt = DateTime.UtcNow;
        signer.UpdatedBy = performedBy;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.SignerProfileUpdated,
            performedBy,
            "SignerIdentity",
            signer.Id.ToString(),
            $"Cập nhật hồ sơ người ký: {signer.FullName} ({signer.Position})");

        return true;
    }

    public async Task<PinOperationResult> SetupSignerPinAndCredentialAsync(
        int signerId,
        string pin,
        string performedBy,
        CancellationToken cancellationToken = default)
    {
        var signer = await _context.SignerIdentities.FirstOrDefaultAsync(s => s.Id == signerId, cancellationToken);
        if (signer == null)
        {
            return new PinOperationResult { Succeeded = false, ErrorMessage = "Không tìm thấy hồ sơ người ký." };
        }

        if (string.IsNullOrWhiteSpace(pin) || pin.Length < 6)
        {
            return new PinOperationResult { Succeeded = false, ErrorMessage = "Mã PIN bảo mật phải có ít nhất 6 ký tự." };
        }

        var company = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);
        string companyName = company?.CompanyName ?? "TRADEFLOW";
        string companyTaxCode = company?.TaxCode ?? "0123456789";

        string subject = $"CN={signer.FullName.ToUpperInvariant()}, POSITION={signer.Position.ToUpperInvariant()}, O={companyName.ToUpperInvariant()}, MST={companyTaxCode}, C=VN";

        try
        {
            var keyPair = await _signingProvider.GenerateKeyPairAsync(pin, subject, cancellationToken);
            DateTime now = DateTime.UtcNow;

            signer.CertificateSerialNumber = keyPair.CertificateSerialNumber;
            signer.CertificateSubject = subject;
            signer.CertificateIssuer = $"TradeFlow CA - {companyName}";
            signer.CertificateThumbprint = keyPair.CertificateThumbprint;
            signer.ValidFrom = now;
            signer.ValidTo = now.AddYears(1);
            signer.EncryptedPrivateKey = keyPair.EncryptedPrivateKey;
            signer.KeySalt = keyPair.KeySalt;
            signer.PinVerificationHash = keyPair.PinVerificationHash;
            signer.PublicKeyXml = keyPair.PublicKeyXml;
            signer.PublicKeyPem = keyPair.PublicKeyPem;
            signer.FailedPinAttempts = 0;
            signer.LockoutEnd = null;
            signer.Status = SignerStatus.Active;
            signer.UpdatedAt = now;
            signer.UpdatedBy = performedBy;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                AuditEventType.SignerPinChanged,
                performedBy,
                "SignerIdentity",
                signer.CertificateSerialNumber,
                $"Thiết lập mã PIN và cặp khóa chứng thư số thành công cho người ký {signer.FullName} ({signer.Position})");

            return new PinOperationResult { Succeeded = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up signer PIN and credential for signer {SignerId}", signerId);
            return new PinOperationResult { Succeeded = false, ErrorMessage = $"Lỗi thiết lập mã PIN: {ex.Message}" };
        }
    }

    public async Task<PinOperationResult> ChangeSignerPinAsync(
        int signerId,
        string currentPin,
        string newPin,
        string performedBy,
        CancellationToken cancellationToken = default)
    {
        var signer = await _context.SignerIdentities.FirstOrDefaultAsync(s => s.Id == signerId, cancellationToken);
        if (signer == null)
        {
            return new PinOperationResult { Succeeded = false, ErrorMessage = "Không tìm thấy hồ sơ người ký." };
        }

        if (signer.IsLockedOut)
        {
            return new PinOperationResult { Succeeded = false, ErrorMessage = $"Tài khoản ký số đang tạm khóa đến {signer.LockoutEnd!.Value.ToLocalTime():HH:mm:ss dd/MM/yyyy} do nhập sai PIN nhiều lần." };
        }

        if (!signer.HasCredential || string.IsNullOrEmpty(signer.EncryptedPrivateKey) || string.IsNullOrEmpty(signer.KeySalt))
        {
            return new PinOperationResult { Succeeded = false, ErrorMessage = "Người ký chưa có chứng thư hoặc khóa ký để đổi PIN. Vui lòng thiết lập PIN mới." };
        }

        if (string.IsNullOrWhiteSpace(newPin) || newPin.Length < 6)
        {
            return new PinOperationResult { Succeeded = false, ErrorMessage = "Mã PIN mới phải có ít nhất 6 ký tự." };
        }

        try
        {
            var result = await _signingProvider.ChangePinAsync(signer.EncryptedPrivateKey, signer.KeySalt, currentPin, newPin, cancellationToken);
            signer.EncryptedPrivateKey = result.EncryptedPrivateKey;
            signer.KeySalt = result.KeySalt;
            signer.PinVerificationHash = result.PinVerificationHash;
            signer.FailedPinAttempts = 0;
            signer.LockoutEnd = null;
            signer.UpdatedAt = DateTime.UtcNow;
            signer.UpdatedBy = performedBy;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                AuditEventType.SignerPinChanged,
                performedBy,
                "SignerIdentity",
                signer.CertificateSerialNumber,
                $"Đổi mã PIN ký số thành công cho người ký {signer.FullName}");

            return new PinOperationResult { Succeeded = true };
        }
        catch (CryptographicException)
        {
            signer.FailedPinAttempts++;
            var settings = await GetSecuritySettingsAsync(cancellationToken);
            if (signer.FailedPinAttempts >= settings.MaxFailedSignAttempts)
            {
                signer.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                await _auditService.LogAsync(
                    AuditEventType.SignerLockedOut,
                    performedBy,
                    "SignerIdentity",
                    signer.CertificateSerialNumber,
                    $"Khóa tạm thời người ký {signer.FullName} trong 15 phút do nhập sai mã PIN hiện tại {signer.FailedPinAttempts} lần.");
            }
            await _context.SaveChangesAsync(cancellationToken);

            return new PinOperationResult
            {
                Succeeded = false,
                ErrorMessage = signer.IsLockedOut
                    ? "Mã PIN hiện tại không đúng. Tài khoản ký số đã bị khóa tạm thời 15 phút."
                    : $"Mã PIN hiện tại không đúng. Còn {Math.Max(0, settings.MaxFailedSignAttempts - signer.FailedPinAttempts)} lần thử."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing PIN for signer {SignerId}", signerId);
            return new PinOperationResult { Succeeded = false, ErrorMessage = $"Lỗi khi đổi mã PIN: {ex.Message}" };
        }
    }

    public async Task<PinOperationResult> AdminResetSignerPinAsync(
        int signerId,
        string reason,
        string performedBy,
        CancellationToken cancellationToken = default)
    {
        var signer = await _context.SignerIdentities.FirstOrDefaultAsync(s => s.Id == signerId, cancellationToken);
        if (signer == null)
        {
            return new PinOperationResult { Succeeded = false, ErrorMessage = "Không tìm thấy hồ sơ người ký." };
        }

        signer.EncryptedPrivateKey = null;
        signer.KeySalt = null;
        signer.PinVerificationHash = null;
        signer.CertificateSerialNumber = string.Empty;
        signer.CertificateSubject = string.Empty;
        signer.CertificateIssuer = string.Empty;
        signer.CertificateThumbprint = string.Empty;
        signer.PublicKeyPem = string.Empty;
        signer.PublicKeyXml = string.Empty;
        signer.FailedPinAttempts = 0;
        signer.LockoutEnd = null;
        signer.UpdatedAt = DateTime.UtcNow;
        signer.UpdatedBy = performedBy;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.SignerPinReset,
            performedBy,
            "SignerIdentity",
            signer.Id.ToString(),
            $"Quản trị viên {performedBy} đã đặt lại mã PIN và xóa khóa ký cũ của người ký {signer.FullName}. Người ký cần thiết lập lại mã PIN mới. Lý do: {reason}");

        return new PinOperationResult { Succeeded = true };
    }

    public async Task<bool> UnlockSignerLockoutAsync(int signerId, string performedBy, CancellationToken cancellationToken = default)
    {
        var signer = await _context.SignerIdentities.FirstOrDefaultAsync(s => s.Id == signerId, cancellationToken);
        if (signer == null) return false;

        signer.FailedPinAttempts = 0;
        signer.LockoutEnd = null;
        signer.UpdatedAt = DateTime.UtcNow;
        signer.UpdatedBy = performedBy;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.SignerProfileUpdated,
            performedBy,
            "SignerIdentity",
            signer.CertificateSerialNumber,
            $"Quản trị viên {performedBy} đã mở khóa tài khoản ký số cho {signer.FullName}.");

        return true;
    }

    public async Task<OffboardEmployeeResult> OffboardEmployeeAsync(
        string userId,
        string reason,
        string performedBy,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new OffboardEmployeeResult { Succeeded = false, ErrorMessage = "Không tìm thấy người dùng." };
        }

        if (user.UserName == performedBy)
        {
            return new OffboardEmployeeResult { Succeeded = false, ErrorMessage = "Bạn không thể tự thực hiện quy trình thôi việc cho chính mình." };
        }

        DateTime now = DateTime.UtcNow;

        // 1. Disable account
        user.Status = UserStatus.Disabled;

        // 2. Invalidate active security stamp (kills active cookies/tokens)
        await _userManager.UpdateSecurityStampAsync(user);
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return new OffboardEmployeeResult
            {
                Succeeded = false,
                ErrorMessage = string.Join("; ", updateResult.Errors.Select(e => e.Description))
            };
        }

        // 3. Revoke all active signer identities for this employee
        var activeSigners = await _context.SignerIdentities
            .Where(s => s.UserId == userId && s.Status == SignerStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var signer in activeSigners)
        {
            signer.Status = SignerStatus.Revoked;
            signer.RevokedAt = now;
            signer.RevokedBy = performedBy;
            signer.RevocationReason = $"Quy trình thôi việc: {reason}";
        }

        // 4. Invalidate all pending enrollment codes for this employee
        var pendingCodes = await _context.SignerEnrollmentCodes
            .Where(c => c.TargetUserId == userId && !c.IsUsed && !c.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var code in pendingCodes)
        {
            code.IsRevoked = true;
            code.RevokedAt = now;
            code.RevokedBy = performedBy;
            code.RevocationReason = $"Quy trình thôi việc: {reason}";
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 5. Complete audit trail
        await _auditService.LogAsync(
            AuditEventType.UserOffboarded,
            performedBy,
            "ApplicationUser",
            user.UserName,
            $"Thực hiện quy trình thôi việc cho nhân viên {user.FullName} ({user.UserName}). Lý do: {reason}. Đã vô hiệu hóa tài khoản, hủy phiên làm việc và thu hồi {activeSigners.Count} chứng thư ký số.");

        return new OffboardEmployeeResult
        {
            Succeeded = true,
            UserName = user.UserName,
            RevokedSignerIdentitiesCount = activeSigners.Count
        };
    }

    public async Task<PasswordResetResult> AdminResetPasswordAsync(
        string userId,
        string? performedBy = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new PasswordResetResult { Succeeded = false, ErrorMessage = "Không tìm thấy người dùng." };
        }

        // Generate 12-char secure temporary password: e.g. TF#8mK2!wP9q
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*";

        var sb = new StringBuilder();
        sb.Append(upper[RandomNumberGenerator.GetInt32(upper.Length)]);
        sb.Append(lower[RandomNumberGenerator.GetInt32(lower.Length)]);
        sb.Append(digits[RandomNumberGenerator.GetInt32(digits.Length)]);
        sb.Append(special[RandomNumberGenerator.GetInt32(special.Length)]);

        string allChars = upper + lower + digits + special;
        for (int i = 0; i < 8; i++)
        {
            sb.Append(allChars[RandomNumberGenerator.GetInt32(allChars.Length)]);
        }

        string tempPassword = sb.ToString();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, token, tempPassword);
        if (!resetResult.Succeeded)
        {
            return new PasswordResetResult
            {
                Succeeded = false,
                ErrorMessage = string.Join("; ", resetResult.Errors.Select(e => e.Description))
            };
        }

        await _userManager.UpdateSecurityStampAsync(user);

        // Audit log strictly WITHOUT logging the password value
        await _auditService.LogAsync(
            AuditEventType.PasswordReset,
            performedBy ?? "System",
            "ApplicationUser",
            user.UserName,
            $"Quản trị viên đã đặt lại mật khẩu tạm thời cho người dùng {user.UserName}. Yêu cầu đổi mật khẩu sau khi đăng nhập.");

        return new PasswordResetResult
        {
            Succeeded = true,
            TempPassword = tempPassword,
            UserName = user.UserName
        };
    }

    public async Task<CompanySecuritySettings> GetSecuritySettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _context.CompanySecuritySettings.FirstOrDefaultAsync(cancellationToken);
        if (settings == null)
        {
            settings = new CompanySecuritySettings
            {
                RequireSignerPin = true,
                PinMinLength = 6,
                EnrollmentCodeExpirationMinutes = 30,
                CertExpirationWarningDays = 30,
                AllowAdminSignerEnrollment = true,
                AutoRevokeOnTermination = true,
                MaxFailedSignAttempts = 5,
                DefaultSigningProvider = SigningProviderType.SoftwareRsa,
                CreatedBy = "System"
            };
            _context.CompanySecuritySettings.Add(settings);
            await _context.SaveChangesAsync(cancellationToken);
        }
        return settings;
    }

    public async Task<bool> UpdateSecuritySettingsAsync(CompanySecuritySettings settings, string updatedBy, CancellationToken cancellationToken = default)
    {
        var existing = await _context.CompanySecuritySettings.FirstOrDefaultAsync(cancellationToken);
        if (existing == null)
        {
            settings.CreatedBy = updatedBy;
            settings.CreatedAt = DateTime.UtcNow;
            _context.CompanySecuritySettings.Add(settings);
        }
        else
        {
            existing.RequireSignerPin = settings.RequireSignerPin;
            existing.PinMinLength = settings.PinMinLength;
            existing.EnrollmentCodeExpirationMinutes = settings.EnrollmentCodeExpirationMinutes;
            existing.CertExpirationWarningDays = settings.CertExpirationWarningDays;
            existing.AllowAdminSignerEnrollment = settings.AllowAdminSignerEnrollment;
            existing.AutoRevokeOnTermination = settings.AutoRevokeOnTermination;
            existing.MaxFailedSignAttempts = settings.MaxFailedSignAttempts;
            existing.DefaultSigningProvider = settings.DefaultSigningProvider;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = updatedBy;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.SecuritySettingsUpdated,
            updatedBy,
            "CompanySecuritySettings",
            "SecurityPolicy",
            "Cập nhật cấu hình chính sách an ninh và chữ ký số của doanh nghiệp.");

        return true;
    }

    public async Task<SecurityDashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        DateTime warningDate = now.AddDays(30);

        var totalSigners = await _context.SignerIdentities.CountAsync(cancellationToken);
        var activeSigners = await _context.SignerIdentities.CountAsync(s => s.Status == SignerStatus.Active && s.ValidTo >= now, cancellationToken);
        var expiringCerts = await _context.SignerIdentities.CountAsync(s => s.Status == SignerStatus.Active && s.ValidTo < warningDate && s.ValidTo >= now, cancellationToken);
        var revokedSigners = await _context.SignerIdentities.CountAsync(s => s.Status == SignerStatus.Revoked, cancellationToken);
        var activeCodes = await _context.SignerEnrollmentCodes.CountAsync(c => !c.IsUsed && !c.IsRevoked && c.ExpiresAt > now, cancellationToken);
        var signedDocs = await _context.DocumentSignatureAudits.CountAsync(s => s.SignatureStatus == DigitalSignatureStatus.Signed || s.SignatureStatus == DigitalSignatureStatus.Valid, cancellationToken);
        var tamperedAlerts = await _context.DocumentSignatureAudits.CountAsync(s => s.SignatureStatus == DigitalSignatureStatus.Tampered || s.SignatureStatus == DigitalSignatureStatus.Invalid, cancellationToken);
        var activeUsers = await _userManager.Users.CountAsync(u => u.Status == UserStatus.Active, cancellationToken);
        var disabledUsers = await _userManager.Users.CountAsync(u => u.Status == UserStatus.Disabled || u.Status == UserStatus.Locked, cancellationToken);

        return new SecurityDashboardStats
        {
            TotalSigners = totalSigners,
            ActiveSigners = activeSigners,
            ExpiringCertificates = expiringCerts,
            RevokedSigners = revokedSigners,
            ActiveEnrollmentCodes = activeCodes,
            TotalSignedDocuments = signedDocs,
            TamperedAlerts = tamperedAlerts,
            ActiveUsers = activeUsers,
            DisabledUsers = disabledUsers
        };
    }

    public async Task<List<SecurityAlertDto>> GetSecurityAlertsAsync(CancellationToken cancellationToken = default)
    {
        var alerts = new List<SecurityAlertDto>();
        DateTime now = DateTime.UtcNow;
        DateTime warningDate = now.AddDays(30);

        // Expiring certificates
        var expiring = await _context.SignerIdentities
            .Where(s => s.Status == SignerStatus.Active && s.ValidTo < warningDate && s.ValidTo >= now)
            .OrderBy(s => s.ValidTo)
            .ToListAsync(cancellationToken);

        foreach (var s in expiring)
        {
            int daysLeft = (int)Math.Max(0, (s.ValidTo - now).TotalDays);
            alerts.Add(new SecurityAlertDto
            {
                AlertType = daysLeft <= 7 ? "error" : "warning",
                Title = "Chứng thư số sắp hết hạn",
                Message = $"Chứng thư số của {s.FullName} ({s.Position}, Sê-ri: {s.CertificateSerialNumber}) sẽ hết hạn sau {daysLeft} ngày (hết hạn ngày {s.ValidTo.ToLocalTime():dd/MM/yyyy}).",
                Timestamp = s.ValidTo
            });
        }

        // Expired certificates still marked active
        var expired = await _context.SignerIdentities
            .Where(s => s.Status == SignerStatus.Active && s.ValidTo < now)
            .ToListAsync(cancellationToken);

        foreach (var s in expired)
        {
            alerts.Add(new SecurityAlertDto
            {
                AlertType = "error",
                Title = "Chứng thư số đã hết hiệu lực",
                Message = $"Chứng thư số của {s.FullName} ({s.Position}, Sê-ri: {s.CertificateSerialNumber}) đã hết hạn từ ngày {s.ValidTo.ToLocalTime():dd/MM/yyyy}. Cần gia hạn hoặc cấp mới.",
                Timestamp = s.ValidTo
            });
        }

        // Recent tampered or invalid signatures
        var recentTampered = await _context.DocumentSignatureAudits
            .Where(a => a.SignatureStatus == DigitalSignatureStatus.Tampered || a.SignatureStatus == DigitalSignatureStatus.Invalid)
            .OrderByDescending(a => a.SigningTime)
            .Take(5)
            .ToListAsync(cancellationToken);

        foreach (var a in recentTampered)
        {
            alerts.Add(new SecurityAlertDto
            {
                AlertType = "error",
                Title = a.SignatureStatus == DigitalSignatureStatus.Tampered ? "Cảnh báo toàn vẹn: Tài liệu bị thay đổi!" : "Chữ ký số không hợp lệ",
                Message = $"Chứng từ {a.DocumentNumber} ({a.DocumentType}) phát hiện chữ ký không khớp dữ liệu hiện tại. Thao tác bởi {a.SignerName}.",
                Timestamp = a.LastVerifiedAt ?? a.SigningTime
            });
        }

        return alerts;
    }

    public async Task<List<DocumentSignatureAuditDto>> GetSignatureAuditsAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentSignatureAudits
            .AsNoTracking()
            .OrderByDescending(a => a.SigningTime)
            .Take(take)
            .Select(a => new DocumentSignatureAuditDto
            {
                Id = a.Id,
                DocumentType = a.DocumentType,
                DocumentId = a.DocumentId,
                DocumentNumber = a.DocumentNumber,
                SignerIdentityId = a.SignerIdentityId,
                SignerName = a.SignerName,
                SignerPosition = a.SignerPosition,
                SignerRole = a.SignerRole,
                SigningTime = a.SigningTime,
                SignatureStatus = a.SignatureStatus,
                DocumentHash = a.DocumentHash,
                SignatureValue = a.SignatureValue,
                CertificateSerialNumber = a.CertificateSerialNumber,
                ProviderType = a.ProviderType,
                VerificationResult = a.VerificationResult,
                LastVerifiedAt = a.LastVerifiedAt,
                LastVerifiedBy = a.LastVerifiedBy,
                ErrorMessage = a.ErrorMessage
            })
            .ToListAsync(cancellationToken);
    }
}
