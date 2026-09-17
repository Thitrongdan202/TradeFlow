using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Pricing;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Infrastructure.Services;

/// <summary>
/// Động cơ xác định chính sách thuế VAT theo quy định pháp luật thuế Việt Nam.
/// Tự động xử lý chính sách giảm thuế 10% -> 8% theo thời gian (hết hạn vào 31/12/2026) mà không cần can thiệp code.
/// </summary>
public class VatRuleEngine : IVatRuleEngine
{
    /// <summary>
    /// Thời điểm kết thúc chính sách giảm thuế GTGT tạm thời (Nghị định 72/2024/NĐ-CP).
    /// </summary>
    public static readonly DateTime VatReductionEndDateUtc = new(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);
    public static readonly DateTime VatReductionStartDateUtc = new(2022, 2, 1, 0, 0, 0, DateTimeKind.Utc);

    public VatRateResult DetermineVatRate(Product? product, DateTime invoiceDate)
    {
        if (product == null)
        {
            return new VatRateResult
            {
                Rate = 10m,
                DisplayText = "10%",
                IsNonTaxable = false,
                IsReduced = false,
                LegalBasis = "Mặc định (Chưa liên kết sản phẩm)"
            };
        }

        return DetermineVatRate(
            product.TaxTreatment,
            product.IsTaxReductionEligible,
            product.TaxRate,
            invoiceDate,
            product.TaxEffectiveFrom,
            product.TaxEffectiveTo);
    }

    public VatRateResult DetermineVatRate(
        TaxTreatment taxTreatment,
        bool isReductionEligible,
        decimal? explicitTaxRate,
        DateTime invoiceDate,
        DateTime? effectiveFrom = null,
        DateTime? effectiveTo = null)
    {
        // 1. Kiểm tra nếu có thuế suất tùy chỉnh với khoảng hiệu lực rõ ràng
        if (explicitTaxRate.HasValue)
        {
            var dateToCompare = invoiceDate.Kind == DateTimeKind.Utc ? invoiceDate : DateTime.SpecifyKind(invoiceDate, DateTimeKind.Utc);
            bool isEffective = true;
            if (effectiveFrom.HasValue && dateToCompare < effectiveFrom.Value) isEffective = false;
            if (effectiveTo.HasValue && dateToCompare > effectiveTo.Value) isEffective = false;

            if (isEffective)
            {
                return new VatRateResult
                {
                    Rate = explicitTaxRate.Value,
                    DisplayText = $"{explicitTaxRate.Value:0.##}%",
                    IsNonTaxable = explicitTaxRate.Value == 0,
                    IsReduced = false,
                    LegalBasis = "Cấu hình thuế suất riêng cho sản phẩm"
                };
            }
        }

        // 2. Xử lý theo từng phân loại thuế
        switch (taxTreatment)
        {
            case TaxTreatment.NonTaxable:
                return new VatRateResult
                {
                    Rate = 0m,
                    DisplayText = "Không chịu thuế",
                    IsNonTaxable = true,
                    IsReduced = false,
                    LegalBasis = "Không thuộc đối tượng chịu thuế GTGT theo Luật thuế GTGT"
                };

            case TaxTreatment.ZeroRate:
                return new VatRateResult
                {
                    Rate = 0m,
                    DisplayText = "0%",
                    IsNonTaxable = false,
                    IsReduced = false,
                    LegalBasis = "Thuế suất 0% (Hàng xuất khẩu, quốc tế)"
                };

            case TaxTreatment.Rate5:
                return new VatRateResult
                {
                    Rate = 5m,
                    DisplayText = "5%",
                    IsNonTaxable = false,
                    IsReduced = false,
                    LegalBasis = "Thuế suất 5% theo Luật thuế GTGT"
                };

            case TaxTreatment.Rate8:
                return new VatRateResult
                {
                    Rate = 8m,
                    DisplayText = "8%",
                    IsNonTaxable = false,
                    IsReduced = true,
                    LegalBasis = "Thuế suất 8% chỉ định"
                };

            case TaxTreatment.Standard10:
            default:
                // Quy tắc chính sách giảm thuế: 10% -> 8% nếu đủ điều kiện và ngày hóa đơn <= 31/12/2026
                var checkDate = invoiceDate.Kind == DateTimeKind.Utc ? invoiceDate : DateTime.SpecifyKind(invoiceDate, DateTimeKind.Utc);
                
                if (isReductionEligible && checkDate >= VatReductionStartDateUtc && checkDate <= VatReductionEndDateUtc)
                {
                    return new VatRateResult
                    {
                        Rate = 8m,
                        DisplayText = "8%",
                        IsNonTaxable = false,
                        IsReduced = true,
                        LegalBasis = "Nghị định 72/2024/NĐ-CP (Giảm thuế GTGT từ 10% xuống 8% đến hết 31/12/2026)"
                    };
                }

                return new VatRateResult
                {
                    Rate = 10m,
                    DisplayText = "10%",
                    IsNonTaxable = false,
                    IsReduced = false,
                    LegalBasis = isReductionEligible && checkDate > VatReductionEndDateUtc
                        ? "Hết thời gian hiệu lực giảm thuế (sau 31/12/2026) - áp dụng thuế suất chuẩn 10%"
                        : "Không thuộc nhóm được giảm thuế GTGT - áp dụng thuế suất chuẩn 10%"
                };
        }
    }
}
