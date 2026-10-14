namespace TradeFlow.Application.Common.Models.Contracts;

public class ContractItemDto
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int? ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatRate { get; set; } = 10;
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? Specifications { get; set; }
    public int SortOrder { get; set; } = 1;
    public string? Note { get; set; }
}
