using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Entities.Security;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Interfaces;

public class SignatureResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SignedBy { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? SignatureValue { get; set; }
    public string? CertificateSubject { get; set; }
    public string? CertificateSerialNumber { get; set; }
    public string? DocumentHash { get; set; }
    public SignerRole? SignerRole { get; set; }
    public string? SignerPosition { get; set; }
}

public class SignatureVerificationResult
{
    public bool IsValid { get; set; }
    public DigitalSignatureStatus Status { get; set; } = DigitalSignatureStatus.Unsigned;
    public string? SignedBy { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? CertificateSubject { get; set; }
    public string? CertificateSerialNumber { get; set; }
    public string? SignerPosition { get; set; }
    public SignerRole? SignerRole { get; set; }
    public string? DocumentHash { get; set; }
    public string? SignatureValue { get; set; }
    public string? Message { get; set; }
    public bool IsTampered => Status == DigitalSignatureStatus.Tampered;
}

public interface IDigitalSignatureService
{
    Task<SignatureResult> SignInvoiceAsync(Invoice invoice, int signerIdentityId, string pin, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
    Task<SignatureResult> SignInvoiceAsync(Invoice invoice, string? signerName = null, CancellationToken cancellationToken = default);
    Task<SignatureVerificationResult> VerifyInvoiceSignatureAsync(Invoice invoice, string? verifiedBy = null, CancellationToken cancellationToken = default);
    string ComputeCanonicalInvoiceDigest(Invoice invoice);
}
