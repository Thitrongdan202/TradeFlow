using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Pricing;

public class PriceListDto
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
    public int Year { get; set; }
    public string? ProgramTitle { get; set; }
    public string? PriceCondition { get; set; }
    public string? VatNote { get; set; }
    public PriceListStatus Status { get; set; }
    public string? OriginalFileName { get; set; }
    public string? OriginalFileStorageRef { get; set; }
    public int TotalItems { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public List<PriceListItemDto> Items { get; set; } = new();
}

public class PriceListItemDto
{
    public int Id { get; set; }
    public int PriceListId { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
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
    public PriceMatchStatus MatchStatus { get; set; }
}