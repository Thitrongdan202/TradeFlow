namespace TradeFlow.Domain.Enums;

/// <summary>
/// Phạm vi dữ liệu áp dụng cho quyền truy cập
/// </summary>
public enum DataScope
{
    /// <summary>Chỉ dữ liệu của bản thân</summary>
    Self = 1,

    /// <summary>Dữ liệu của phòng ban / đội nhóm</summary>
    Department = 2,

    /// <summary>Toàn bộ dữ liệu của công ty</summary>
    All = 3
}
