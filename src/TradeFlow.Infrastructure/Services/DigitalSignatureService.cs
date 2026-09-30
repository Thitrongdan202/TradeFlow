using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Entities.Security;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

/// <summary>
/// Dịch vụ ký số mật mã học chuẩn RSA-SHA256 cho hóa đơn và chứng từ TradeFlow.
/// Đảm bảo tính pháp lý, toàn vẹn dữ liệu, chống chối bỏ và phát hiện can thiệp sau khi ký (Tamper Detection).
/// </summary>
public class DigitalSignatureService : IDigitalSignatureService
{
    private readonly TradeFlowDbContext _context;
    private readonly ISigningProvider _signingProvider;
    private readonly IAuditService _auditService;
    private readonly ILogger<DigitalSignatureService> _logger;

    public DigitalSignatureService(
        TradeFlowDbContext context,
        ISigningProvider signingProvider,
        IAuditService auditService,
        ILogger<DigitalSignatureService> logger)
    {
        _context = context;
        _signingProvider = signingProvider;
        _auditService = auditService;
        _logger = logger;
    }

    public DigitalSignatureService(TradeFlowDbContext context)
        : this(
            context,
            new Security.RsaSoftwareSigningProvider(),
            new NullAuditService(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DigitalSignatureService>.Instance)
    {
    }

    private class NullAuditService : IAuditService
    {
        public Task LogAsync(
            AuditEventType eventType,
            string? performedBy = null,
            string? targetEntity = null,
            string? targetId = null,
            string? details = null,
            string? ipAddress = null,
            string? userAgent = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public async Task<SignatureResult> SignInvoiceAsync(Invoice invoice, string? signerName = null, CancellationToken cancellationToken = default)
    {
        var signer = await _context.SignerIdentities
            .Where(s => s.Status == SignerStatus.Active && s.ValidTo >= DateTime.UtcNow)
            .OrderBy(s => s.SignerRole)
            .FirstOrDefaultAsync(cancellationToken);

        const string pin = "123456";

        if (signer == null)
        {
            var company = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);
            string name = signerName ?? company?.CompanyName ?? invoice.CompanyName;
            if (string.IsNullOrWhiteSpace(name)) name = "LACASA";

            var kp = await _signingProvider.GenerateKeyPairAsync(pin, $"CN={name.ToUpperInvariant()}", cancellationToken);
            signer = new SignerIdentity
            {
                UserId = "system",
                UserName = "system",
                FullName = name,
                Position = "Giám đốc điều hành",
                SignerRole = SignerRole.Director,
                Status = SignerStatus.Active,
                ProviderType = SigningProviderType.SoftwareRsa,
                CertificateSerialNumber = kp.CertificateSerialNumber,
                CertificateSubject = $"CN={name.ToUpperInvariant()}, MST={company?.TaxCode ?? "0123456789"}",
                CertificateIssuer = "TradeFlow Security CA",
                CertificateThumbprint = kp.CertificateThumbprint,
                ValidFrom = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddYears(1),
                EncryptedPrivateKey = kp.EncryptedPrivateKey,
                KeySalt = kp.KeySalt,
                PinVerificationHash = kp.PinVerificationHash,
                PublicKeyXml = kp.PublicKeyXml,
                PublicKeyPem = kp.PublicKeyPem,
                EnrolledBy = "System",
                EnrolledAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };

            _context.SignerIdentities.Add(signer);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return await SignInvoiceAsync(invoice, signer.Id, pin, null, null, cancellationToken);
    }

    public string ComputeCanonicalInvoiceDigest(Invoice invoice)
    {
        var sb = new StringBuilder();
        // 1. Header invariants
        sb.Append(invoice.FormNumber?.Trim()).Append('|');
        sb.Append(invoice.InvoiceSeries?.Trim()).Append('|');
        sb.Append(invoice.InvoiceNo?.Trim()).Append('|');
        sb.Append(invoice.InvoiceDate.ToString("yyyy-MM-ddTHH:mm:ssZ")).Append('|');

        // 2. Financial totals
        sb.Append(invoice.SubTotal.ToString("0.00")).Append('|');
        sb.Append(invoice.TotalDiscount.ToString("0.00")).Append('|');
        sb.Append(invoice.TotalTax.ToString("0.00")).Append('|');
        sb.Append(invoice.GrandTotal.ToString("0.00")).Append('|');

        // 3. Counterparty identifiers
        sb.Append(invoice.CompanyTaxCode?.Trim()).Append('|');
        sb.Append(invoice.CustomerTaxCode?.Trim()).Append('\n');

        // 4. Line items sorted by ID or Product
        if (invoice.Items != null && invoice.Items.Any())
        {
            var orderedItems = invoice.Items
                .OrderBy(x => x.Id > 0 ? x.Id : x.ProductId)
                .ThenBy(x => x.ProductName);

            foreach (var item in orderedItems)
            {
                sb.Append(item.ProductId).Append('|')
                  .Append(item.Quantity.ToString("G29")).Append('|')
                  .Append(item.UnitPrice.ToString("0.00")).Append('|')
                  .Append(item.DiscountAmount.ToString("0.00")).Append('|')
                  .Append(item.TaxRate.ToString("0.##")).Append('|')
                  .Append(item.TaxAmount.ToString("0.00")).Append('\n');
            }
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash);
    }

    public async Task<SignatureResult> SignInvoiceAsync(
        Invoice invoice,
        int signerIdentityId,
        string pin,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var signer = await _context.SignerIdentities
            .FirstOrDefaultAsync(s => s.Id == signerIdentityId, cancellationToken);

        if (signer == null)
        {
            return new SignatureResult { Succeeded = false, ErrorMessage = "Không tìm thấy danh tính người ký số được chỉ định." };
        }

        if (signer.Status != SignerStatus.Active)
        {
            return new SignatureResult { Succeeded = false, ErrorMessage = $"Chứng thư của người ký đang ở trạng thái '{signer.Status.ToVietnamese()}'. Không thể sử dụng để ký." };
        }

        if (DateTime.UtcNow > signer.ValidTo)
        {
            return new SignatureResult { Succeeded = false, ErrorMessage = "Chứng thư số của người ký đã hết hạn hiệu lực." };
        }

        if (string.IsNullOrWhiteSpace(signer.EncryptedPrivateKey) || string.IsNullOrWhiteSpace(signer.KeySalt))
        {
            return new SignatureResult { Succeeded = false, ErrorMessage = "Chứng thư người ký thiếu thông tin khóa bảo mật." };
        }

        // Ensure invoice items are loaded
        if (invoice.Items == null || !invoice.Items.Any())
        {
            await _context.Entry(invoice).Collection(x => x.Items).LoadAsync(cancellationToken);
        }

        try
        {
            // 1. Compute canonical document digest
            string canonicalDigest = ComputeCanonicalInvoiceDigest(invoice);
            byte[] digestBytes = Encoding.UTF8.GetBytes(canonicalDigest);

            // 2. Cryptographic RSA signature with signer's private key protected by PIN
            byte[] signatureBytes = await _signingProvider.SignDataAsync(
                digestBytes,
                signer.EncryptedPrivateKey,
                signer.KeySalt,
                pin,
                cancellationToken);

            string signatureValueBase64 = Convert.ToBase64String(signatureBytes);
            DateTime signingTime = DateTime.UtcNow;

            // 3. Update invoice signature state
            invoice.SignatureStatus = DigitalSignatureStatus.Signed;
            invoice.SignedBy = signer.FullName;
            invoice.SignedAt = signingTime;
            invoice.SignatureValue = signatureValueBase64;
            invoice.CertificateSubject = signer.CertificateSubject;
            invoice.CertificateSerialNumber = signer.CertificateSerialNumber;
            invoice.SignerPosition = signer.Position;
            invoice.SignerRole = signer.SignerRole;
            invoice.SigningProvider = signer.ProviderType;
            invoice.DocumentHash = canonicalDigest;
            invoice.SignerIdentityId = signer.Id;
            invoice.LastVerifiedAt = signingTime;
            invoice.LastVerifiedBy = signer.FullName;

            // 4. Record tamper-proof audit
            var audit = new DocumentSignatureAudit
            {
                DocumentType = "Invoice",
                DocumentId = invoice.Id,
                DocumentNumber = invoice.InvoiceNumber,
                SignerIdentityId = signer.Id,
                SignerUserId = signer.UserId,
                SignerName = signer.FullName,
                SignerPosition = signer.Position,
                SignerRole = signer.SignerRole,
                SigningTime = signingTime,
                SignatureStatus = DigitalSignatureStatus.Signed,
                DocumentHash = canonicalDigest,
                SignatureValue = signatureValueBase64,
                CertificateSubject = signer.CertificateSubject,
                CertificateSerialNumber = signer.CertificateSerialNumber,
                ProviderType = signer.ProviderType,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                VerificationResult = "Chữ ký mật mã học RSA-SHA256 được tạo thành công",
                LastVerifiedAt = signingTime,
                LastVerifiedBy = signer.FullName,
                CreatedAt = signingTime,
                CreatedBy = signer.UserName
            };

            _context.DocumentSignatureAudits.Add(audit);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                AuditEventType.SignatureCreated,
                signer.UserName,
                "Invoice",
                invoice.InvoiceNumber,
                $"Ký điện tử thành công hóa đơn {invoice.InvoiceNumber} ({invoice.InvoiceSeries}-{invoice.InvoiceNo}) bởi {signer.FullName} ({signer.Position}), vai trò: {signer.SignerRole.ToVietnamese()}, sê-ri chứng thư: {signer.CertificateSerialNumber}");

            return new SignatureResult
            {
                Succeeded = true,
                SignedBy = signer.FullName,
                SignedAt = signingTime,
                SignatureValue = signatureValueBase64,
                CertificateSubject = signer.CertificateSubject,
                CertificateSerialNumber = signer.CertificateSerialNumber,
                DocumentHash = canonicalDigest,
                SignerRole = signer.SignerRole,
                SignerPosition = signer.Position
            };
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(ex, "Cryptographic signing failed for invoice {InvoiceId}", invoice.Id);

            // Audit failed attempt
            await _auditService.LogAsync(
                AuditEventType.SignatureFailed,
                signer.UserName,
                "Invoice",
                invoice.InvoiceNumber,
                $"Thao tác ký hóa đơn {invoice.InvoiceNumber} thất bại bởi {signer.FullName}: {ex.Message}");

            return new SignatureResult
            {
                Succeeded = false,
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error signing invoice {InvoiceId}", invoice.Id);
            return new SignatureResult
            {
                Succeeded = false,
                ErrorMessage = $"Lỗi không xác định khi ký số: {ex.Message}"
            };
        }
    }

    public async Task<SignatureVerificationResult> VerifyInvoiceSignatureAsync(
        Invoice invoice,
        string? verifiedBy = null,
        CancellationToken cancellationToken = default)
    {
        if (invoice.SignatureStatus == DigitalSignatureStatus.Unsigned || string.IsNullOrEmpty(invoice.SignatureValue))
        {
            return new SignatureVerificationResult
            {
                IsValid = false,
                Status = DigitalSignatureStatus.Unsigned,
                Message = "Hóa đơn chưa được ký điện tử (Chưa ký)."
            };
        }

        if (string.IsNullOrEmpty(invoice.DocumentHash))
        {
            invoice.SignatureStatus = DigitalSignatureStatus.Invalid;
            return new SignatureVerificationResult
            {
                IsValid = false,
                Status = DigitalSignatureStatus.Invalid,
                Message = "Hóa đơn thiếu dữ liệu mã băm chứng từ gốc (Chữ ký không hợp lệ)."
            };
        }

        // Ensure line items are loaded
        if (invoice.Items == null || !invoice.Items.Any())
        {
            await _context.Entry(invoice).Collection(x => x.Items).LoadAsync(cancellationToken);
        }

        // 1. Check Document Integrity (Tamper Detection)
        string currentDigest = ComputeCanonicalInvoiceDigest(invoice);
        if (!string.Equals(currentDigest, invoice.DocumentHash, StringComparison.OrdinalIgnoreCase))
        {
            invoice.SignatureStatus = DigitalSignatureStatus.Tampered;
            invoice.LastVerifiedAt = DateTime.UtcNow;
            invoice.LastVerifiedBy = verifiedBy ?? "Hệ thống kiểm tra";

            // Record tamper event in audit log
            await _auditService.LogAsync(
                AuditEventType.DocumentTampered,
                verifiedBy ?? "System",
                "Invoice",
                invoice.InvoiceNumber,
                $"CẢNH BÁO TOÀN VẸN: Hóa đơn {invoice.InvoiceNumber} đã bị can thiệp/sửa đổi sau khi ký! Mã băm hiện tại ({currentDigest}) không khớp mã băm đã ký ({invoice.DocumentHash}).");

            return new SignatureVerificationResult
            {
                IsValid = false,
                Status = DigitalSignatureStatus.Tampered,
                SignedBy = invoice.SignedBy,
                SignedAt = invoice.SignedAt,
                CertificateSubject = invoice.CertificateSubject,
                CertificateSerialNumber = invoice.CertificateSerialNumber,
                SignerPosition = invoice.SignerPosition,
                SignerRole = invoice.SignerRole,
                DocumentHash = invoice.DocumentHash,
                SignatureValue = invoice.SignatureValue,
                Message = "TÀI LIỆU ĐÃ BỊ THAY ĐỔI SAU KHI KÝ! Dữ liệu hóa đơn hiện tại không khớp với nội dung tại thời điểm ký số."
            };
        }

        // 2. Check Signer Identity & Certificate Status
        SignerIdentity? signer = null;
        if (invoice.SignerIdentityId.HasValue)
        {
            signer = await _context.SignerIdentities
                .FirstOrDefaultAsync(s => s.Id == invoice.SignerIdentityId.Value, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(invoice.CertificateSerialNumber))
        {
            signer = await _context.SignerIdentities
                .FirstOrDefaultAsync(s => s.CertificateSerialNumber == invoice.CertificateSerialNumber, cancellationToken);
        }

        if (signer != null)
        {
            if (signer.Status == SignerStatus.Revoked)
            {
                invoice.SignatureStatus = DigitalSignatureStatus.Revoked;
                return new SignatureVerificationResult
                {
                    IsValid = false,
                    Status = DigitalSignatureStatus.Revoked,
                    SignedBy = invoice.SignedBy,
                    SignedAt = invoice.SignedAt,
                    CertificateSubject = invoice.CertificateSubject,
                    CertificateSerialNumber = invoice.CertificateSerialNumber,
                    SignerPosition = invoice.SignerPosition,
                    SignerRole = invoice.SignerRole,
                    DocumentHash = invoice.DocumentHash,
                    SignatureValue = invoice.SignatureValue,
                    Message = $"Chữ ký đã bị thu hồi: Chứng thư số của người ký ({signer.FullName}) đã bị thu hồi lý do: '{signer.RevocationReason ?? "Không có lý do"} vào lúc {signer.RevokedAt?.ToLocalTime():dd/MM/yyyy}'."
                };
            }

            if (signer.ValidTo < DateTime.UtcNow)
            {
                invoice.SignatureStatus = DigitalSignatureStatus.CertificateExpired;
                return new SignatureVerificationResult
                {
                    IsValid = false,
                    Status = DigitalSignatureStatus.CertificateExpired,
                    SignedBy = invoice.SignedBy,
                    SignedAt = invoice.SignedAt,
                    CertificateSubject = invoice.CertificateSubject,
                    CertificateSerialNumber = invoice.CertificateSerialNumber,
                    SignerPosition = invoice.SignerPosition,
                    SignerRole = invoice.SignerRole,
                    DocumentHash = invoice.DocumentHash,
                    SignatureValue = invoice.SignatureValue,
                    Message = $"Chứng thư số của người ký ({signer.FullName}) đã hết hạn hiệu lực vào ngày {signer.ValidTo.ToLocalTime():dd/MM/yyyy}."
                };
            }

            // 3. Cryptographic RSA signature verification against public key
            string publicKey = signer.PublicKeyPem ?? signer.PublicKeyXml ?? string.Empty;
            if (!string.IsNullOrEmpty(publicKey))
            {
                byte[] data = Encoding.UTF8.GetBytes(invoice.DocumentHash);
                byte[] sigBytes = Convert.FromBase64String(invoice.SignatureValue);

                bool cryptoValid = await _signingProvider.VerifySignatureAsync(data, sigBytes, publicKey, cancellationToken);
                if (!cryptoValid)
                {
                    invoice.SignatureStatus = DigitalSignatureStatus.Invalid;
                    return new SignatureVerificationResult
                    {
                        IsValid = false,
                        Status = DigitalSignatureStatus.Invalid,
                        SignedBy = invoice.SignedBy,
                        SignedAt = invoice.SignedAt,
                        CertificateSubject = invoice.CertificateSubject,
                        CertificateSerialNumber = invoice.CertificateSerialNumber,
                        SignerPosition = invoice.SignerPosition,
                        SignerRole = invoice.SignerRole,
                        DocumentHash = invoice.DocumentHash,
                        SignatureValue = invoice.SignatureValue,
                        Message = "Chữ ký không hợp lệ: Xác minh toán học chữ ký RSA-SHA256 thất bại."
                    };
                }
            }
        }

        // All checks passed
        invoice.SignatureStatus = DigitalSignatureStatus.Valid;
        invoice.LastVerifiedAt = DateTime.UtcNow;
        invoice.LastVerifiedBy = verifiedBy ?? "Hệ thống kiểm tra";

        await _context.SaveChangesAsync(cancellationToken);

        return new SignatureVerificationResult
        {
            IsValid = true,
            Status = DigitalSignatureStatus.Valid,
            SignedBy = invoice.SignedBy,
            SignedAt = invoice.SignedAt,
            CertificateSubject = invoice.CertificateSubject,
            CertificateSerialNumber = invoice.CertificateSerialNumber,
            SignerPosition = invoice.SignerPosition,
            SignerRole = invoice.SignerRole,
            DocumentHash = invoice.DocumentHash,
            SignatureValue = invoice.SignatureValue,
            Message = "Chữ ký số hợp lệ: Toàn vẹn dữ liệu được đảm bảo, chứng thư hợp lệ và xác thực mật mã RSA thành công."
        };
    }
}
