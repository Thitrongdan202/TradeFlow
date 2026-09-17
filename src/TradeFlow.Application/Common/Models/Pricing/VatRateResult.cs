namespace TradeFlow.Application.Common.Models.Pricing;

public class VatRateResult
{
    /// <summary>Mức thuế suất thực tế áp dụng (0, 5, 8, 10)</summary>
    public decimal Rate { get; set; }

    /// <summary>Mô tả hiển thị (vd: "8%", "10%", "Không chịu thuế")</summary>
    public string DisplayText { get; set; } = string.Empty;

    /// <summary>Là đối tượng không chịu thuế GTGT</summary>
    public bool IsNonTaxable { get; set; }

    /// <summary>Được hưởng chính sách giảm thuế 10% -> 8%</summary>
    public bool IsReduced { get; set; }

    /// <summary>Căn cứ pháp lý áp dụng</summary>
    public string LegalBasis { get; set; } = string.Empty;
}
