using System;
using System.Collections.Generic;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Partners;

public enum PartnerType
{
    Customer = 1,
    Supplier = 2
}

public enum CustomerClassification
{
    Individual = 1,
    Company = 2
}

public enum DebtDirection
{
    Receivable = 1, // Khách hàng nợ (Phải thu)
    Payable = 2     // Nợ nhà cung cấp (Phải trả)
}

public enum DebtStatus
{
    Outstanding = 1, // Còn nợ hoàn toàn
    PartiallyPaid = 2, // Đã thanh toán một phần
    Settled = 3       // Đã thanh toán đủ
}

public class CustomerListItemDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? BusinessCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxCode { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public bool IsCompany { get; set; }
    public decimal CurrentReceivable { get; set; }
    public decimal TotalSales { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public int? MatchingSupplierId { get; set; }
    public string? MatchingSupplierCode { get; set; }
}

public class CustomerDetailDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? BusinessCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxCode { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public bool IsCompany { get; set; }

    // Financial Metrics
    public decimal CurrentReceivable { get; set; }
    public decimal TotalSales { get; set; }
    public int OrderCount { get; set; }
    public int InvoiceCount { get; set; }
    public DateTime? LastTransactionDate { get; set; }

    // Audit Metadata
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Overlap with Supplier
    public int? MatchingSupplierId { get; set; }
    public string? MatchingSupplierCode { get; set; }

    // Linked Transactions
    public List<PartnerLinkedTransactionDto> RecentTransactions { get; set; } = new();
}

public class CustomerFilterDto
{
    public string? SearchTerm { get; set; }
    public CustomerClassification? CustomerType { get; set; } // null = All, Individual, Company
    public bool? IsActive { get; set; } // null = All, true = Active, false = Inactive
    public decimal? MinReceivable { get; set; }
    public decimal? MaxReceivable { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public string? CreatedBy { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SupplierListItemDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? BusinessCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxCode { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public decimal CurrentPayable { get; set; }
    public decimal TotalPurchases { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public int? MatchingCustomerId { get; set; }
    public string? MatchingCustomerCode { get; set; }
}

public class SupplierDetailDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? BusinessCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxCode { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }

    // Financial Metrics
    public decimal CurrentPayable { get; set; }
    public decimal TotalPurchases { get; set; }
    public DateTime? LastTransactionDate { get; set; }

    // Audit Metadata
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Overlap with Customer
    public int? MatchingCustomerId { get; set; }
    public string? MatchingCustomerCode { get; set; }

    // Linked Transactions
    public List<PartnerLinkedTransactionDto> RecentTransactions { get; set; } = new();
}

public class SupplierFilterDto
{
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public decimal? MinPayable { get; set; }
    public decimal? MaxPayable { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public string? CreatedBy { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PartnerLinkedTransactionDto
{
    public int? DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty; // "Đơn bán hàng" / "Hóa đơn VAT" / "Hóa đơn bán lẻ" / "Thanh toán (Sổ quỹ)" / "Điều chỉnh" / "Chiết khấu"
    public string DocumentCode { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public decimal Amount { get; set; } // Giá trị chứng từ
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal DebitAmount { get; set; } // Phát sinh tăng (Nợ)
    public decimal CreditAmount { get; set; } // Phát sinh giảm (Có / Thanh toán)
    public decimal RunningBalance { get; set; } // Dư nợ lũy kế sau giao dịch
    public string Status { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? ReferenceCode { get; set; }
    public string? Notes { get; set; }
    public string? LinkUrl { get; set; }
    public List<PartnerTransactionLineItemDto> LineItems { get; set; } = new();
}

public class PartnerTransactionLineItemDto
{
    public int SortOrder { get; set; }
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

public class DeletePartnerResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsReferenced { get; set; }
    public bool SoftDeleted { get; set; }
    public int ReferenceCount { get; set; }
}

public class BulkDeletePartnerResult
{
    public int RequestedCount { get; set; }
    public int DeletedCount { get; set; }
    public int DeactivatedCount { get; set; }
    public List<string> Messages { get; set; } = new();
}

public class PartnerDebtSummaryDto
{
    /// <summary>Tổng nợ phải thu từ khách hàng (Receivables)</summary>
    public decimal TotalCustomerReceivables { get; set; }

    /// <summary>Tổng nợ phải trả cho nhà cung cấp (Payables)</summary>
    public decimal TotalSupplierPayables { get; set; }

    /// <summary>Chênh lệch công nợ (Đối soát tổng hợp)</summary>
    public decimal NetDifference => TotalCustomerReceivables - TotalSupplierPayables;

    /// <summary>Số lượng khách hàng hiện đang có công nợ phải thu</summary>
    public int CustomersWithDebtCount { get; set; }

    /// <summary>Số lượng nhà cung cấp hiện đang có công nợ phải trả</summary>
    public int SuppliersWithDebtCount { get; set; }

    /// <summary>Tổng số khách hàng trong hệ thống</summary>
    public int TotalCustomersCount { get; set; }

    /// <summary>Tổng số nhà cung cấp trong hệ thống</summary>
    public int TotalSuppliersCount { get; set; }

    /// <summary>Thông báo trạng thái dữ liệu tài chính (nếu có phân hệ chưa hoàn tất)</summary>
    public string? FinancialDataSourceNote { get; set; }
}

public class DebtRecordDto
{
    public int Id { get; set; }
    public PartnerType PartnerType { get; set; }
    public DebtDirection Direction { get; set; }
    public int PartnerId { get; set; }
    public string PartnerCode { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string DocumentType { get; set; } = string.Empty; // "Đơn bán hàng" / "Hóa đơn bán hàng" / "Hóa đơn VAT"
    public string DocumentCode { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public DebtStatus DebtStatus { get; set; }
    public string? PaymentMethod { get; set; }
    public string? LinkUrl { get; set; }
    public int? DocumentId => Id;
}

public class DebtFilterDto
{
    public PartnerType? PartnerType { get; set; } // null = All, Customer, Supplier
    public DebtStatus? DebtStatus { get; set; } // null = All, Outstanding, PartiallyPaid, Settled
    public string? SearchTerm { get; set; }
    public decimal? MinBalance { get; set; }
    public decimal? MaxBalance { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

public enum DebtExportScope
{
    CurrentPartner = 1,
    SelectedPartners = 2,
    AllFiltered = 3
}

public class RecordDebtPaymentDto
{
    public PartnerType PartnerType { get; set; } = PartnerType.Customer;
    public int PartnerId { get; set; }
    public DebtTransactionType TransactionType { get; set; } = DebtTransactionType.Payment;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string PaymentMethod { get; set; } = "Tiền mặt";
    public string? ReferenceCode { get; set; }
    public int? InvoiceId { get; set; }
    public int? SalesOrderId { get; set; }
    public string? Notes { get; set; }
}

public class RecordDebtPaymentResult
{
    public bool Succeeded { get; set; }
    public int? PaymentId { get; set; }
    public string? PaymentCode { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal UpdatedBalance { get; set; }
    public decimal NewRemainingBalance => UpdatedBalance;
}

public class PartnerDebtStatementExportRequest
{
    public DebtExportScope Scope { get; set; } = DebtExportScope.CurrentPartner;
    public PartnerType? PartnerType { get; set; }
    public List<int> PartnerIds { get; set; } = new();
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? DatePresetTitle { get; set; }
    public bool IncludeLineDetails { get; set; } = true;
    public bool IncludeUnit { get; set; } = true;
    public bool IncludeQuantity { get; set; } = true;
    public bool IncludeUnitPrice { get; set; } = true;
    public bool IncludeDiscount { get; set; } = true;
    public bool IncludeVat { get; set; } = true;
    public bool IncludeCostOrPurchasePrice { get; set; } = true;
    public bool IncludeLineTotal { get; set; } = true;
}

public class DebtManagementExportRequest
{
    public DebtExportScope Scope { get; set; } = DebtExportScope.AllFiltered;
    public PartnerType? PartnerType { get; set; }
    public List<int> SelectedCustomerIds { get; set; } = new();
    public List<int> SelectedSupplierIds { get; set; } = new();
    public DebtFilterDto? Filter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? DatePresetTitle { get; set; }
    public bool IncludeIndividualPartnerSheets { get; set; } = true;
}

public class PartnerStatementSummaryDto
{
    public int PartnerId { get; set; }
    public PartnerType PartnerType { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal ClosingBalance { get; set; }
    public string BalanceDirectionDescription { get; set; } = string.Empty;
    public List<PartnerLinkedTransactionDto> Transactions { get; set; } = new();
}
