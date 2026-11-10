using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.Employees;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Attendance;

/// <summary>
/// Sổ cái phát sinh và biến động số dư phép năm của nhân viên (Annual Leave Balance Ledger)
/// </summary>
public class LeaveBalanceLedger : AuditableEntity<int>
{
    public LeaveBalanceLedger() { }

    /// <summary>FK: Nhân viên được áp dụng</summary>
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>Năm áp dụng chế độ phép (Ví dụ: 2026)</summary>
    public int Year { get; set; }

    /// <summary>Loại giao dịch biến động phép</summary>
    public LeaveLedgerTransactionType TransactionType { get; set; } = LeaveLedgerTransactionType.MonthlyAccrual;

    /// <summary>
    /// Số ngày thay đổi (Dương: cộng thêm; Âm: khấu trừ khi nghỉ phép)
    /// </summary>
    public decimal DaysChanged { get; set; }

    /// <summary>Số dư phép tích lũy còn lại sau khi thực hiện giao dịch này</summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>Lý do biến động / Nội dung giao dịch</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>FK: Đơn xin nghỉ phép liên kết nếu là trừ phép hoặc hoàn phép</summary>
    public int? LeaveRequestId { get; set; }
    public LeaveRequest? LeaveRequest { get; set; }

    /// <summary>ID của người thực hiện giao dịch (User/Manager/Admin)</summary>
    public string? PerformedByUserId { get; set; }

    /// <summary>Tên hiển thị của người thực hiện giao dịch</summary>
    public string? PerformedByUserName { get; set; }

    /// <summary>Ghi chú bổ sung hoặc mã quyết định / căn cứ điều chỉnh</summary>
    public string? Notes { get; set; }
}
