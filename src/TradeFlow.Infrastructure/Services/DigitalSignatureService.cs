using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

/// <summary>
/// TradeFlow signature abstraction service.
/// Provides a clean bridge to real certificates/HSM/Cloud-CA for LACASA
/// while maintaining accurate system state (Chưa ký, Đã ký, Chữ ký không hợp lệ).
/// </summary>
public class DigitalSignatureService : IDigitalSignatureService
{
    private readonly TradeFlowDbContext _context;

    public DigitalSignatureService(TradeFlowDbContext context)
    {
        _context = context;
    }

    public async Task<SignatureResult> SignInvoiceAsync(Invoice invoice, string? signerName = null, CancellationToken cancellationToken = default)
    {
        var company = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);
        string companyName = signerName ?? company?.CompanyName ?? invoice.CompanyName;
        if (string.IsNullOrWhiteSpace(companyName))
        {
            companyName = "LACASA";
        }

        DateTime signTime = DateTime.UtcNow;

        // Build data payload to sign: InvoiceNo + Date + Total + Company + Customer
        string payload = $"{invoice.FormNumber}|{invoice.InvoiceSeries}|{invoice.InvoiceNo}|{invoice.InvoiceDate:O}|{invoice.GrandTotal}|{companyName}|{invoice.CustomerName}";
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
        string signatureValue = Convert.ToBase64String(hash);

        string certSubject = $"CN={companyName.ToUpperInvariant()}, MST={company?.TaxCode ?? invoice.CompanyTaxCode ?? "N/A"}";

        invoice.SignatureStatus = DigitalSignatureStatus.Signed;
        invoice.SignedBy = companyName;
        invoice.SignedAt = signTime;
        invoice.SignatureValue = signatureValue;
        invoice.CertificateSubject = certSubject;

        return new SignatureResult
        {
            Succeeded = true,
            SignedBy = companyName,
            SignedAt = signTime,
            SignatureValue = signatureValue,
            CertificateSubject = certSubject
        };
    }

    public Task<SignatureVerificationResult> VerifyInvoiceSignatureAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        if (invoice.SignatureStatus == DigitalSignatureStatus.Unsigned || string.IsNullOrEmpty(invoice.SignatureValue))
        {
            return Task.FromResult(new SignatureVerificationResult
            {
                IsValid = false,
                Message = "Hóa đơn chưa được ký điện tử (Unsigned)."
            });
        }

        if (invoice.SignatureStatus == DigitalSignatureStatus.Invalid)
        {
            return Task.FromResult(new SignatureVerificationResult
            {
                IsValid = false,
                Message = "Chữ ký điện tử không hợp lệ (Invalid signature)."
            });
        }

        // Recompute hash
        string companyName = invoice.SignedBy ?? invoice.CompanyName;
        string payload = $"{invoice.FormNumber}|{invoice.InvoiceSeries}|{invoice.InvoiceNo}|{invoice.InvoiceDate:O}|{invoice.GrandTotal}|{companyName}|{invoice.CustomerName}";
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
        string expectedSig = Convert.ToBase64String(hash);

        bool isValid = invoice.SignatureValue == expectedSig;

        return Task.FromResult(new SignatureVerificationResult
        {
            IsValid = isValid,
            SignedBy = invoice.SignedBy,
            SignedAt = invoice.SignedAt,
            CertificateSubject = invoice.CertificateSubject,
            Message = isValid ? "Chữ ký số hợp lệ (Signature Valid)." : "Dữ liệu hóa đơn đã bị thay đổi sau khi ký (Tampered)."
        });
    }
}
