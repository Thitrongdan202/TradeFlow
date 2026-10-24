namespace TradeFlow.Domain.Enums;

/// <summary>
/// Loại hợp đồng lao động theo quy định Bộ luật Lao động số 45/2019/QH14 và VB hợp nhất 18/VBHN-VPQH
/// </summary>
public enum EmploymentContractType
{
    /// <summary>
    /// Hợp đồng lao động không xác định thời hạn (Điểm a Khoản 1 Điều 20 BLLĐ)
    /// </summary>
    IndefiniteTerm = 1,

    /// <summary>
    /// Hợp đồng lao động xác định thời hạn (tối đa 36 tháng - Điểm b Khoản 1 Điều 20 BLLĐ)
    /// </summary>
    FixedTerm = 2,

    /// <summary>
    /// Hợp đồng thử việc riêng biệt (Điều 24, Điều 25 BLLĐ)
    /// </summary>
    Probation = 3
}
