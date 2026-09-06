using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Pricing;

public class ExcelAnalysisResultDto
{
    public string FileName { get; set; } = string.Empty;
    public string TempFileReference { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }
    public string? QuotationTitle { get; set; }
    public string? QuotationNumber { get; set; }
    public DateTime? QuotationDate { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? ProgramTitle { get; set; }
    public string? PriceCondition { get; set; }
    public string? VatNote { get; set; }
    public int TotalRowsFound { get; set; }
    public int ImagesFound { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<ExcelParsedItemDto> Items { get; set; } = new();
}

public class ExcelParsedItemDto
{
    public int RowIndex { get; set; }
    public int SortOrder { get; set; }
    public string? Group { get; set; }
    public string NewCode { get; set; } = string.Empty;
    public string? LegacyCode { get; set; }
    public string? ProductInfo { get; set; }
    public decimal UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public string? ImageStorageRef { get; set; }
    public int? MatchedProductId { get; set; }
    public string? MatchedProductName { get; set; }
    public PriceMatchStatus MatchStatus { get; set; }
    public List<string> ValidationMessages { get; set; } = new();
}

public class ExcelImportCommitRequest
{
    public string TempFileReference { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
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
    public PriceListStatus InitialStatus { get; set; } = PriceListStatus.Active;
    public bool AutoCreateProducts { get; set; } = true;
    public List<ExcelParsedItemDto> Items { get; set; } = new();
}