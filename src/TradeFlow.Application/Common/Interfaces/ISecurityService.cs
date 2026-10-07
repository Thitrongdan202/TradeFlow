using TradeFlow.Domain.Entities.Security;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Interfaces;

public class IssuedEnrollmentCodeResult
{
    public bool Succeeded { get; set; }
    public string? RawCode { get; set; }
    public bool HasExpiration { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SignerEnrollmentResult
{
    public bool Succeeded { get; set; }
    public int? SignerIdentityId { get; set; }
    public string? CertificateSerialNumber { get; set; }
    public string? FullName { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PinOperationResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
}

public class OffboardEmployeeResult
{
    public bool Succeeded { get; set; }
    public string? UserName { get; set; }
    public int RevokedSignerIdentitiesCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PasswordResetResult
{
    public bool Succeeded { get; set; }
    public string? TempPassword { get; set; }
    public string? UserName { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SignerIdentityDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public SignerRole SignerRole { get; set; }
    public SignerStatus Status { get; set; }
    public SigningProviderType ProviderType { get; set; }
    public string CertificateSerialNumber { get; set; } = string.Empty;
    public string CertificateSubject { get; set; } = string.Empty;
    public string CertificateIssuer { get; set; } = string.Empty;
    public string CertificateThumbprint { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string EnrolledBy { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedBy { get; set; }
    public string? RevocationReason { get; set; }
    public string? HandwrittenSignatureImage { get; set; }
    public int FailedPinAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
    public bool HasCredential => !string.IsNullOrEmpty(CertificateSerialNumber);
    public bool IsExpired => HasCredential && DateTime.UtcNow > ValidTo;
    public bool IsExpiringSoon => HasCredential && Status == SignerStatus.Active && (ValidTo - DateTime.UtcNow).TotalDays <= 30;
}

public class SignerEnrollmentCodeDto
{
    public int Id { get; set; }
    public string TargetUserId { get; set; } = string.Empty;
    public string TargetUserName { get; set; } = string.Empty;
    public string TargetFullName { get; set; } = string.Empty;
    public string TargetPosition { get; set; } = string.Empty;
    public SignerRole TargetSignerRole { get; set; }
    public bool HasExpiration { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? UsedBy { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedBy { get; set; }
    public string? RevocationReason { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsExpired => HasExpiration && ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    public bool IsActive => !IsUsed && !IsRevoked && !IsExpired;
}

public class DocumentSignatureAuditDto
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public int DocumentId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public int? SignerIdentityId { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public string SignerPosition { get; set; } = string.Empty;
    public SignerRole SignerRole { get; set; }
    public DateTime SigningTime { get; set; }
    public DigitalSignatureStatus SignatureStatus { get; set; }
    public string DocumentHash { get; set; } = string.Empty;
    public string SignatureValue { get; set; } = string.Empty;
    public string? CertificateSerialNumber { get; set; }
    public SigningProviderType ProviderType { get; set; }
    public string? VerificationResult { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public string? LastVerifiedBy { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SecurityDashboardStats
{
    public int TotalSigners { get; set; }
    public int ActiveSigners { get; set; }
    public int ExpiringCertificates { get; set; }
    public int RevokedSigners { get; set; }
    public int ActiveEnrollmentCodes { get; set; }
    public int TotalSignedDocuments { get; set; }
    public int TamperedAlerts { get; set; }
    public int ActiveUsers { get; set; }
    public int DisabledUsers { get; set; }
}

public class SecurityAlertDto
{
    public string AlertType { get; set; } = "info"; // "warning", "error", "info"
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public interface ISecurityService
{
    // Enrollment Codes
    Task<IssuedEnrollmentCodeResult> IssueEnrollmentCodeAsync(string targetUserId, SignerRole targetRole, string position, bool hasExpiration, int? expirationMinutes, string issuedBy, CancellationToken cancellationToken = default);
    Task<IssuedEnrollmentCodeResult> IssueEnrollmentCodeAsync(string targetUserId, SignerRole targetRole, string position, int expirationMinutes, string issuedBy, CancellationToken cancellationToken = default);
    Task<bool> RevokeEnrollmentCodeAsync(int codeId, string reason, string revokedBy, CancellationToken cancellationToken = default);
    Task<List<SignerEnrollmentCodeDto>> GetEnrollmentCodesAsync(CancellationToken cancellationToken = default);

    // Signer Identities & Enrollment
    Task<SignerEnrollmentResult> EnrollSignerAsync(string rawEnrollmentCode, string pin, string? performedBy = null, CancellationToken cancellationToken = default);
    Task<bool> RevokeSignerAsync(int signerId, string reason, string revokedBy, CancellationToken cancellationToken = default);
    Task<List<SignerIdentityDto>> GetAllSignersAsync(CancellationToken cancellationToken = default);
    Task<List<SignerIdentityDto>> GetActiveSignersForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<SignerIdentity?> GetSignerByIdAsync(int signerId, CancellationToken cancellationToken = default);
    Task<bool> UpdateSignerProfileAsync(int signerId, string fullName, string position, string? handwrittenSignatureImage, string performedBy, CancellationToken cancellationToken = default);

    // PIN Setup & Security Operations
    Task<PinOperationResult> SetupSignerPinAndCredentialAsync(int signerId, string pin, string performedBy, CancellationToken cancellationToken = default);
    Task<PinOperationResult> ChangeSignerPinAsync(int signerId, string currentPin, string newPin, string performedBy, CancellationToken cancellationToken = default);
    Task<PinOperationResult> AdminResetSignerPinAsync(int signerId, string reason, string performedBy, CancellationToken cancellationToken = default);
    Task<bool> UnlockSignerLockoutAsync(int signerId, string performedBy, CancellationToken cancellationToken = default);

    // Employee Offboarding
    Task<OffboardEmployeeResult> OffboardEmployeeAsync(string userId, string reason, string performedBy, CancellationToken cancellationToken = default);

    // Controlled Admin Password Reset
    Task<PasswordResetResult> AdminResetPasswordAsync(string userId, string? performedBy = null, CancellationToken cancellationToken = default);

    // Company Security Settings
    Task<CompanySecuritySettings> GetSecuritySettingsAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateSecuritySettingsAsync(CompanySecuritySettings settings, string updatedBy, CancellationToken cancellationToken = default);

    // Dashboard & Audits
    Task<SecurityDashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken = default);
    Task<List<SecurityAlertDto>> GetSecurityAlertsAsync(CancellationToken cancellationToken = default);
    Task<List<DocumentSignatureAuditDto>> GetSignatureAuditsAsync(int take = 50, CancellationToken cancellationToken = default);
}
