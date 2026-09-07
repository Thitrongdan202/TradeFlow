using System.ComponentModel.DataAnnotations;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Sales;

public class SalesOrderDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveryDate { get; set; }
    
    [Required(ErrorMessage = "Vui lòng chọn khách hàng.")]
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
    
    public List<SalesOrderItemDto> Items { get; set; } = new();
}

public class SalesOrderItemDto
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    
    [Required(ErrorMessage = "Vui lòng chọn sản phẩm.")]
    public int ProductId { get; set; }
    
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}
