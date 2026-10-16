using System;
using System.Collections.Generic;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Infrastructure.Services.DataBackup;

// =========================================================================
// CÁC THỰC THỂ DỮ LIỆU ĐÓNG GÓI SAO LƯU TRADEFLOW (PORTABLE BACKUP ENTITIES)
// Khớp 100% với Domain Entities thực tế trong cơ sở dữ liệu TradeFlow
// =========================================================================

public class CompanySettingsBackupDto
{
    public int Id { get; set; } = 1;
    public string CompanyName { get; set; } = string.Empty;
    public string? TradingName { get; set; }
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountHolder { get; set; }
    public string? Website { get; set; }
    public string? LogoPath { get; set; }
    public string? SignaturePath { get; set; }
    public string? OrderQrCodePath { get; set; }
    public string? DefaultOrderNote { get; set; }
    public string? DefaultVatNote { get; set; }
    public string? OrderHotline { get; set; }
    public string? OrderFooterNote1 { get; set; }
    public string? OrderFooterNote2 { get; set; }
    public bool ShowLoadingScreen { get; set; } = true;
    public bool ShowCompanyNameOnLoading { get; set; } = true;
    public string SpinnerColor { get; set; } = "#10b981";
    public string SpinnerSpeed { get; set; } = "normal";
    public double SpinnerOpacity { get; set; } = 0.8;
}

public class CompanySecuritySettingsBackupDto
{
    public int Id { get; set; } = 1;
    public bool RequireSignerPin { get; set; } = true;
    public int PinMinLength { get; set; } = 6;
    public int EnrollmentCodeExpirationMinutes { get; set; } = 30;
    public int CertExpirationWarningDays { get; set; } = 30;
    public bool AllowAdminSignerEnrollment { get; set; } = true;
    public bool AutoRevokeOnTermination { get; set; } = true;
    public int MaxFailedSignAttempts { get; set; } = 5;
    public SigningProviderType DefaultSigningProvider { get; set; } = SigningProviderType.SoftwareRsa;
    public string XmlDsigCanonicalizationMethod { get; set; } = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
    public string XmlDsigSignatureMethod { get; set; } = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
    public string XmlDsigDigestMethod { get; set; } = "http://www.w3.org/2001/04/xmlenc#sha256";
}

public class ContractPartyASettingsBackupDto
{
    public int Id { get; set; } = 1;
    public string CompanyName { get; set; } = string.Empty;
    public string? TradingName { get; set; }
    public string TaxCode { get; set; } = string.Empty;
    public string? BusinessRegistrationNumber { get; set; }
    public string RegisteredAddress { get; set; } = string.Empty;
    public string? ContactAddress { get; set; }
    public string RepresentativeName { get; set; } = string.Empty;
    public string RepresentativePosition { get; set; } = string.Empty;
    public string? RepresentativeAuthority { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public string? BankAccountHolder { get; set; }
    public string? BankBranch { get; set; }
    public string? DefaultPaymentInstructions { get; set; }
    public string? DefaultDeliveryTerms { get; set; }
    public string? DefaultWarrantyTerms { get; set; }
}

public class DocumentCategoryBackupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SystemSequenceBackupDto
{
    public int Id { get; set; }
    public string SequenceKey { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public long CurrentNumber { get; set; }
    public int Step { get; set; } = 1;
    public string FormatPattern { get; set; } = "{Prefix}{Number:D6}";
    public string? Description { get; set; }
}

public class RoleBackupDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public class RolePermissionBackupDto
{
    public int Id { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public ResourceType Resource { get; set; }
    public PermissionAction Action { get; set; }
    public bool IsGranted { get; set; } = true;
    public DateTime GrantedAt { get; set; }
    public string? GrantedBy { get; set; }
}

public class UserBackupDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class UserRoleBackupDto
{
    public string UserId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
}

public class ProductCategoryBackupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ParentCategoryId { get; set; }
}

public class UnitOfMeasureBackupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CurrencyBackupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public decimal ExchangeRate { get; set; } = 1;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public class DepartmentBackupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PositionBackupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class WarehouseBackupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ManagerName { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CustomerBackupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxCode { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SupplierBackupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxCode { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductBackupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? LegacyCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public int? CategoryId { get; set; }
    public int? UnitId { get; set; }
    public string? Description { get; set; }
    public string? Specifications { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductImageBackupDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageReference { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class EmployeeBackupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? AttendanceCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? AvatarPath { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Facebook { get; set; }
    public string? MobileDevice { get; set; }
    public int? DepartmentId { get; set; }
    public int? PositionId { get; set; }
    public string? WorkingBranch { get; set; }
    public string? PayrollBranch { get; set; }
    public DateTime? StartDate { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public DateTime? TerminationDate { get; set; }
    public string? TerminationReason { get; set; }
    public string? BankInformation { get; set; }
    public decimal DebtAndAdvance { get; set; } = 0;
    public string? Notes { get; set; }
    public string? UserId { get; set; }
}

public class EmployeeRequestBackupDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? AttendanceCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? NationalId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public int? DepartmentId { get; set; }
    public int? PositionId { get; set; }
    public string? WorkingBranch { get; set; }
    public string? PayrollBranch { get; set; }
    public DateTime? StartDate { get; set; }
    public string? BankInformation { get; set; }
    public string? Notes { get; set; }
    public string? ProposedUserId { get; set; }
    public EmployeeRequestStatus Status { get; set; } = EmployeeRequestStatus.Pending;
    public string RequestedBy { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public int? CreatedEmployeeId { get; set; }
}

public class SignerIdentityBackupDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public SignerRole SignerRole { get; set; } = SignerRole.AuthorizedSigner;
    public SignerStatus Status { get; set; } = SignerStatus.Active;
    public SigningProviderType ProviderType { get; set; } = SigningProviderType.SoftwareRsa;
    public string? CertificateSerialNumber { get; set; }
    public string? CertificateSubject { get; set; }
    public string? CertificateIssuer { get; set; }
    public string? CertificateThumbprint { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
}

public class SignerEnrollmentCodeBackupDto
{
    public int Id { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public string TargetUserId { get; set; } = string.Empty;
    public string TargetUserName { get; set; } = string.Empty;
    public string TargetFullName { get; set; } = string.Empty;
    public string TargetPosition { get; set; } = string.Empty;
    public SignerRole TargetSignerRole { get; set; } = SignerRole.AuthorizedSigner;
    public bool HasExpiration { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? UsedBy { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedBy { get; set; }
    public string? RevocationReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PriceListBackupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? QuotationNumber { get; set; }
    public DateTime? QuotationDate { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int? Month { get; set; }
    public int? Quarter { get; set; }
    public int Year { get; set; } = DateTime.UtcNow.Year;
    public string? ProgramTitle { get; set; }
    public string? PriceCondition { get; set; }
    public string? VatNote { get; set; }
    public PriceListStatus Status { get; set; } = PriceListStatus.Draft;
    public string? OriginalFileName { get; set; }
    public string? OriginalFileStorageRef { get; set; }
    public int TotalItems { get; set; }
    public string? Notes { get; set; }
}

public class PriceListItemBackupDto
{
    public int Id { get; set; }
    public int PriceListId { get; set; }
    public int? ProductId { get; set; }
    public int SortOrder { get; set; }
    public string? Group { get; set; }
    public string NewCode { get; set; } = string.Empty;
    public string? LegacyCode { get; set; }
    public string? ProductInfo { get; set; }
    public string? ImageStorageRef { get; set; }
    public decimal UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal? VatRate { get; set; }
    public string? Note { get; set; }
    public PriceMatchStatus MatchStatus { get; set; } = PriceMatchStatus.Matched;
}

public class ContractTemplateBackupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Thương mại";
    public string Description { get; set; } = string.Empty;
    public string TypicalUseCase { get; set; } = string.Empty;
    public ContractPartyBType? TargetPartyType { get; set; }
    public string RequiredInfo { get; set; } = string.Empty;
    public string Icon { get; set; } = "file-text";
    public string Version { get; set; } = "1.0";
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public string LegalBasis { get; set; } = string.Empty;
    public string KnownLimitations { get; set; } = string.Empty;
    public bool IsReviewedByLegal { get; set; } = false;
    public string DefaultClausesJson { get; set; } = "[]";
}

public class QuotationBackupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime QuotationDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerTaxCode { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerContactPerson { get; set; }
    public string? CustomerEmail { get; set; }
    public string? SalespersonName { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal GrandTotal { get; set; }
    public int? SalesOrderId { get; set; }
}

public class QuotationItemBackupDto
{
    public int Id { get; set; }
    public int QuotationId { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string PriceSource { get; set; } = string.Empty;
    public decimal DiscountRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; } = 1;
    public string? Notes { get; set; }
}

public class SalesOrderBackupDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerTaxCode { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Notes { get; set; }
    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }
    public int? QuotationId { get; set; }
}

public class SalesOrderItemBackupDto
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string PriceSource { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }
}

public class InvoiceBackupDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string FormNumber { get; set; } = "1";
    public string InvoiceSeries { get; set; } = "1C26TFL";
    public string InvoiceNo { get; set; } = "00000001";
    public DateTime InvoiceDate { get; set; }
    public int? SalesOrderId { get; set; }
    public int? CustomerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyTaxCode { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyEmail { get; set; }
    public string? CompanyBankAccount { get; set; }
    public string? CompanyBankName { get; set; }
    public string? CompanyLogoUrl { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerCompanyName { get; set; }
    public string? CustomerTaxCode { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerBankAccount { get; set; }
    public string? CustomerBankName { get; set; }
    public string? PaymentMethod { get; set; } = "TM/CK";
    public string? Notes { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public InvoiceType Type { get; set; } = InvoiceType.VatInvoice;
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }
    public string? TaxAuthorityCode { get; set; }
    public string? QrCodeData { get; set; }
    public DigitalSignatureStatus SignatureStatus { get; set; } = DigitalSignatureStatus.Unsigned;
    public string? SignedBy { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? SignatureValue { get; set; }
    public string? CertificateSubject { get; set; }
    public string? CertificateSerialNumber { get; set; }
    public string? SignerPosition { get; set; }
    public SignerRole? SignerRole { get; set; }
    public SigningProviderType? SigningProvider { get; set; }
    public string? DocumentHash { get; set; }
    public int? SignerIdentityId { get; set; }
    public string? SignerHandwrittenSignatureImage { get; set; }
}

public class InvoiceItemBackupDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int? ProductId { get; set; }
    public int SortOrder { get; set; } = 1;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public class PartnerPaymentBackupDto
{
    public int Id { get; set; }
    public string PaymentCode { get; set; } = string.Empty;
    public int PartnerType { get; set; }
    public int PartnerId { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public DebtTransactionType TransactionType { get; set; } = DebtTransactionType.Payment;
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Tiền mặt";
    public string? ReferenceCode { get; set; }
    public int? InvoiceId { get; set; }
    public int? SalesOrderId { get; set; }
    public string? Notes { get; set; }
}

public class ContractBackupDto
{
    public int Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = "B2B_SALE";
    public string TemplateVersion { get; set; } = "1.0";
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    public DateTime ContractDate { get; set; }
    public string ContractPlace { get; set; } = "Cần Thơ";
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public ContractPartyBType PartyBType { get; set; } = ContractPartyBType.Company;
    public int? PartnerId { get; set; }

    // Bên A
    public string PartyAName { get; set; } = string.Empty;
    public string? PartyATaxCode { get; set; }
    public string? PartyAAddress { get; set; }
    public string? PartyAContactAddress { get; set; }
    public string? PartyARepresentative { get; set; }
    public string? PartyAPosition { get; set; }
    public string? PartyAAuthority { get; set; }
    public string? PartyAPhone { get; set; }
    public string? PartyAEmail { get; set; }
    public string? PartyABankName { get; set; }
    public string? PartyABankAccount { get; set; }
    public string? PartyABankAccountHolder { get; set; }
    public string? PartyABankBranch { get; set; }

    // Bên B
    public string PartyBName { get; set; } = string.Empty;
    public string? PartyBTaxCode { get; set; }
    public string? PartyBIdCardNumber { get; set; }
    public string? PartyBIdCardDate { get; set; }
    public string? PartyBIdCardPlace { get; set; }
    public string? PartyBAddress { get; set; }
    public string? PartyBDeliveryAddress { get; set; }
    public string? PartyBPhone { get; set; }
    public string? PartyBEmail { get; set; }
    public string? PartyBRepresentative { get; set; }
    public string? PartyBPosition { get; set; }
    public string? PartyBAuthority { get; set; }
    public string? PartyBBankName { get; set; }
    public string? PartyBBankAccount { get; set; }

    // Tài chính
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public string? AmountInWords { get; set; }

    // Điều khoản
    public string? PaymentTerms { get; set; }
    public string? DeliveryTerms { get; set; }
    public string? InstallationTerms { get; set; }
    public string? WarrantyTerms { get; set; }
    public string? BreachPenaltyTerms { get; set; }
    public string? DisputeResolutionTerms { get; set; }
    public string? GeneralTerms { get; set; }
    public string CustomClausesJson { get; set; } = "[]";

    // Ký & Xác thực
    public ContractExecutionMethod ExecutionMethod { get; set; } = ContractExecutionMethod.NotSpecified;
    public DateTime? PartyASignedAt { get; set; }
    public string? PartyASignedBy { get; set; }
    public string? PartyASignatureData { get; set; }
    public DateTime? PartyBSignedAt { get; set; }
    public string? PartyBSignedBy { get; set; }
    public string? PartyBSignatureData { get; set; }
    public string DocumentHash { get; set; } = string.Empty;
    public bool IsPaperPrinted { get; set; }
    public DateTime? PrintedAt { get; set; }
    public string? PrintedBy { get; set; }
    public int? PaperSignedScanAttachmentId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime? FinalizedAt { get; set; }
    public string? FinalizedBy { get; set; }
    public int? ParentContractId { get; set; }
    public string? Notes { get; set; }
}

public class ContractItemBackupDto
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int? ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = "Cái";
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatRate { get; set; } = 10;
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? Specifications { get; set; }
    public int SortOrder { get; set; }
    public string? Note { get; set; }
}

public class DocumentAttachmentBackupDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? CategoryId { get; set; }
    public string RelatedEntityType { get; set; } = "General";
    public int? RelatedEntityId { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public class DocumentSignatureAuditBackupDto
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = "Invoice";
    public int DocumentId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public int? SignerIdentityId { get; set; }
    public string? SignerUserId { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public string SignerPosition { get; set; } = string.Empty;
    public SignerRole SignerRole { get; set; } = SignerRole.AuthorizedSigner;
    public DateTime SigningTime { get; set; } = DateTime.UtcNow;
    public DigitalSignatureStatus SignatureStatus { get; set; } = DigitalSignatureStatus.Signed;
    public string DocumentHash { get; set; } = string.Empty;
    public string SignatureValue { get; set; } = string.Empty;
    public string? CertificateSubject { get; set; }
    public string? CertificateSerialNumber { get; set; }
    public SigningProviderType ProviderType { get; set; } = SigningProviderType.SoftwareRsa;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? VerificationResult { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public string? LastVerifiedBy { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AuditLogBackupDto
{
    public long Id { get; set; }
    public AuditEventType EventType { get; set; }
    public string? PerformedBy { get; set; }
    public string? TargetEntity { get; set; }
    public string? TargetId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime OccurredAt { get; set; }
}
