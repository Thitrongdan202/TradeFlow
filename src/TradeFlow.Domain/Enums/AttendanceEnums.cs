namespace TradeFlow.Domain.Enums;

/// <summary>Trạng thái chấm công của nhân viên trong ngày</summary>
public enum AttendanceStatus
{
    /// <summary>Chưa vào ca làm</summary>
    NotClockedIn = 1,

    /// <summary>Đã vào ca (đang làm việc)</summary>
    ClockedIn = 2,

    /// <summary>Đã ra ca (hoàn thành ca làm bình thường)</summary>
    ClockedOut = 3,

    /// <summary>Đi muộn</summary>
    Late = 4,

    /// <summary>Về sớm</summary>
    EarlyLeave = 5,

    /// <summary>Đi muộn và về sớm</summary>
    LateAndEarlyLeave = 6,

    /// <summary>Thiếu lượt chấm công ra (quên chấm ra)</summary>
    MissingClockOut = 7,

    /// <summary>Nghỉ phép có hưởng lương hoặc không hưởng lương</summary>
    OnLeave = 8,

    /// <summary>Vắng mặt không phép</summary>
    Absent = 9,

    /// <summary>Đang chờ xử lý điều chỉnh công</summary>
    PendingAdjustment = 10,

    /// <summary>Đã được điều chỉnh công sau giải trình / phê duyệt</summary>
    Adjusted = 11,

    /// <summary>Nghỉ lễ / Tết</summary>
    Holiday = 12,

    /// <summary>Đúng giờ (đạt chuẩn ca làm việc)</summary>
    OnTime = 13
}

/// <summary>Loại yêu cầu điều chỉnh chấm công</summary>
public enum AttendanceAdjustmentType
{
    /// <summary>Quên chấm công vào ca</summary>
    MissedClockIn = 1,

    /// <summary>Quên chấm công ra ca</summary>
    MissedClockOut = 2,

    /// <summary>Điều chỉnh lại cả giờ vào và giờ ra</summary>
    TimeCorrection = 3,

    /// <summary>Bổ sung lý do công tác / việc ngoài công ty</summary>
    BusinessTrip = 4,

    /// <summary>Lý do kỹ thuật / thiết bị lỗi</summary>
    DeviceError = 5,

    /// <summary>Khác</summary>
    Other = 6
}

/// <summary>Trạng thái phê duyệt yêu cầu chấm công</summary>
public enum AttendanceRequestStatus
{
    /// <summary>Chờ phê duyệt</summary>
    Pending = 1,

    /// <summary>Đã phê duyệt chấp thuận</summary>
    Approved = 2,

    /// <summary>Đã từ chối</summary>
    Rejected = 3,

    /// <summary>Người gửi đã thu hồi / hủy</summary>
    Cancelled = 4
}

/// <summary>Loại nghỉ phép</summary>
public enum LeaveType
{
    /// <summary>Nghỉ phép năm (hưởng nguyên lương)</summary>
    AnnualLeave = 1,

    /// <summary>Nghỉ ốm đau / khám chữa bệnh (hưởng BHXH)</summary>
    SickLeave = 2,

    /// <summary>Nghỉ việc riêng không hưởng lương</summary>
    UnpaidLeave = 3,

    /// <summary>Nghỉ thai sản / chế độ nam nghỉ khi vợ sinh</summary>
    MaternityLeave = 4,

    /// <summary>Nghỉ kết hôn (hưởng nguyên lương theo BLLĐ)</summary>
    WeddingLeave = 5,

    /// <summary>Nghỉ tang chế (hưởng nguyên lương theo BLLĐ)</summary>
    FuneralLeave = 6,

    /// <summary>Nghỉ bù làm thêm giờ / công tác</summary>
    CompensatoryLeave = 7,

    /// <summary>Nghỉ khác</summary>
    Other = 8
}

/// <summary>Phân loại ca làm việc</summary>
public enum WorkShiftType
{
    /// <summary>Ca hành chính tiêu chuẩn</summary>
    StandardOffice = 1,

    /// <summary>Ca sáng</summary>
    Morning = 2,

    /// <summary>Ca chiều</summary>
    Afternoon = 3,

    /// <summary>Ca đêm (qua ngày hôm sau)</summary>
    Night = 4,

    /// <summary>Ca linh hoạt</summary>
    Flexible = 5
}

/// <summary>Loại giao dịch biến động sổ cái phép năm</summary>
public enum LeaveLedgerTransactionType
{
    /// <summary>Tích lũy phép định kỳ hàng tháng (1 ngày/tháng đủ điều kiện)</summary>
    MonthlyAccrual = 1,

    /// <summary>Khấu trừ khi đơn nghỉ phép năm được phê duyệt</summary>
    LeaveUsageDeduction = 2,

    /// <summary>Quản lý trực tiếp cấp thêm ngày phép thưởng cho nhân viên</summary>
    ManagerGrant = 3,

    /// <summary>Quản trị viên hệ thống cấp thêm phép hoặc điều chỉnh</summary>
    AdminGrant = 4,

    /// <summary>Hoàn phép khi đơn nghỉ phép đã duyệt bị hủy hợp lệ</summary>
    LeaveCancellationReversal = 5,

    /// <summary>Kết chuyển số dư phép từ năm trước</summary>
    AnnualRollover = 6,

    /// <summary>Điều chỉnh nhân sự có văn bản</summary>
    Adjustment = 7
}
