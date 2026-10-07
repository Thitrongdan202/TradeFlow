namespace TradeFlow.Domain.Enums;

/// <summary>Trạng thái yêu cầu thêm nhân viên</summary>
public enum EmployeeRequestStatus
{
    /// <summary>Chờ duyệt</summary>
    Pending = 1,

    /// <summary>Đã duyệt</summary>
    Approved = 2,

    /// <summary>Từ chối</summary>
    Rejected = 3,

    /// <summary>Đã hủy</summary>
    Cancelled = 4
}
