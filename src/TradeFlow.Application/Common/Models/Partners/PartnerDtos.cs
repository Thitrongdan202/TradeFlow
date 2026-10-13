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
    public string DocumentType { get; set; } = string.Empty; // "Đơn bán hàng" / "Hóa đơn" / "Đơn mua hàng"
    public string DocumentCode { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? LinkUrl { get; set; }
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
