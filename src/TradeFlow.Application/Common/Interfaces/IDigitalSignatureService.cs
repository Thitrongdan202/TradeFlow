using TradeFlow.Domain.Entities.Sales;

namespace TradeFlow.Application.Common.Interfaces;

public class SignatureResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SignedBy { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? SignatureValue { get; set; }
    public string? CertificateSubject { get; set; }
}

public class SignatureVerificationResult
{
    public bool IsValid { get; set; }
    public string? SignedBy { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? CertificateSubject { get; set; }
    public string? Message { get; set; }
}

public interface IDigitalSignatureService
{
    Task<SignatureResult> SignInvoiceAsync(Invoice invoice, string? signerName = null, CancellationToken cancellationToken = default);
    Task<SignatureVerificationResult> VerifyInvoiceSignatureAsync(Invoice invoice, CancellationToken cancellationToken = default);
}
