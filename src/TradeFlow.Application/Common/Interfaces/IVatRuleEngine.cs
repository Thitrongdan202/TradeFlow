using TradeFlow.Application.Common.Models.Pricing;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Interfaces;

public interface IVatRuleEngine
{
    /// <summary>
    /// Xác định mức thuế suất VAT và căn cứ pháp lý dựa trên thông tin sản phẩm và ngày chứng từ/hóa đơn.
    /// </summary>
    VatRateResult DetermineVatRate(Product? product, DateTime invoiceDate);

    /// <summary>
    /// Xác định mức thuế suất VAT dựa trên phân loại thuế, tính hợp lệ giảm thuế và ngày áp dụng.
    /// </summary>
    VatRateResult DetermineVatRate(
        TaxTreatment taxTreatment,
        bool isReductionEligible,
        decimal? explicitTaxRate,
        DateTime invoiceDate,
        DateTime? effectiveFrom = null,
        DateTime? effectiveTo = null);
}
