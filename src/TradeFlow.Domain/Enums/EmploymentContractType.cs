namespace TradeFlow.Domain.Enums;

/// <summary>
/// Loại văn bản lao động, thử việc và thực tập/đào tạo
/// Tuân thủ Bộ luật Lao động (Văn bản hợp nhất 18/VBHN-VPQH ngày 12/02/2026)
/// </summary>
public enum EmploymentContractType
{
    /// <summary>Hợp đồng lao động không xác định thời hạn (Điểm a Khoản 1 Điều 20 BLLĐ)</summary>
    IndefiniteTerm = 1,

    /// <summary>Hợp đồng lao động xác định thời hạn (Tối đa 36 tháng theo Điểm b Khoản 1 Điều 20 BLLĐ)</summary>
    FixedTerm = 2,

    /// <summary>Hợp đồng / Thỏa thuận thử việc (Theo Điều 24 & Điều 25 BLLĐ)</summary>
    Probation = 3,

    /// <summary>Thỏa thuận thực tập sinh / Tiếp nhận thực tập (Internship Agreement)</summary>
    Internship = 4,

    /// <summary>Hợp đồng đào tạo nghề / tập nghề (Theo Điều 61 BLLĐ)</summary>
    VocationalTraining = 5
}
