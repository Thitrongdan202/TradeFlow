namespace TradeFlow.Domain.Enums;

/// <summary>
/// Phương thức giao kết và ký hợp đồng lao động
/// </summary>
public enum EmploymentSigningMethod
{
    /// <summary>Chưa ký</summary>
    None = 0,

    /// <summary>Ký trên văn bản giấy truyền thống (có bản scan/chụp đính kèm)</summary>
    PaperSigned = 1,

    /// <summary>Ký tay điện tử trực tiếp trên ứng dụng TradeFlow (Touch/Mouse Signature)</summary>
    InAppDrawnSignature = 2,

    /// <summary>Ký số nội bộ TradeFlow (Chứng thư điện tử doanh nghiệp kèm Hash SHA-256)</summary>
    InternalDigitalSignature = 3,

    /// <summary>Nền tảng chứng thực hợp đồng điện tử theo NĐ 337/2025/NĐ-CP</summary>
    NationalPlatform = 4
}
