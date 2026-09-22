using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Sales;

public class QuotationDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime QuotationDate { get; set; } = DateTime.UtcNow;
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
    public string? SalesOrderCode { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public List<QuotationItemDto> Items { get; set; } = new();
}

public class QuotationItemDto
{
    public int Id { get; set; }
    public int QuotationId { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public string PriceSource { get; set; } = string.Empty;
    public decimal DiscountRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; } = 1;
    public string? Notes { get; set; }
}

public class QuotationFilterDto
{
    public string? SearchTerm { get; set; }
    public QuotationStatus? Status { get; set; }
    public int? CustomerId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
