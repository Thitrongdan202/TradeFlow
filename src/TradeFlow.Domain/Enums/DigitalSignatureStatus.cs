namespace TradeFlow.Domain.Enums;

/// <summary>
/// Trạng thái chữ ký điện tử của hóa đơn
/// </summary>
public enum DigitalSignatureStatus
{
    /// <summary>Chưa ký</summary>
    Unsigned = 0,

    /// <summary>Đã ký</summary>
    Signed = 1,

    /// <summary>Chữ ký không hợp lệ</summary>
    Invalid = 2
}
