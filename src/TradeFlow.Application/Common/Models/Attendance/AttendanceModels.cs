using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Attendance;

public class AttendanceWidgetDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string WorkingBranch { get; set; } = string.Empty;

    public int? WorkShiftId { get; set; }
    public string ShiftName { get; set; } = "Chưa phân ca";
    public string ShiftCode { get; set; } = string.Empty;
    public TimeSpan ShiftStartTime { get; set; } = new(8, 0, 0);
    public TimeSpan ShiftEndTime { get; set; } = new(17, 30, 0);
    public string ShiftTimeRange => $"{ShiftStartTime:hh\\:mm} - {ShiftEndTime:hh\\:mm}";
    public int BreakMinutes { get; set; } = 90;
    public int GracePeriodMinutes { get; set; } = 15;
    public bool IsOvernight { get; set; }

    public int? TodayRecordId { get; set; }
    public DateTime? ClockInTimeUtc { get; set; }
    public DateTime? ClockOutTimeUtc { get; set; }
    public string ClockInDisplay { get; set; } = "--:--";
    public string ClockOutDisplay { get; set; } = "--:--";

    public AttendanceStatus Status { get; set; } = AttendanceStatus.NotClockedIn;
    public string StatusLabel { get; set; } = "Chưa vào ca";
    public string StatusCssClass { get; set; } = "tf-status-not-in";

    public int WorkingMinutes { get; set; }
    public string WorkingDurationDisplay { get; set; } = "0h 00m";

    public bool CanClockIn { get; set; } = true;
    public bool CanClockOut { get; set; } = false;
    public bool IsCompleted { get; set; } = false;

    public DateTime ServerTimeVn { get; set; } = DateTime.UtcNow.AddHours(7);

    // Overtime alerts & limits
    public bool IsOvertimeAlert { get; set; }
    public int OvertimeMinutes { get; set; }
    public decimal ValidOvertimeHours { get; set; }
    public decimal ExcessOvertimeHours { get; set; }
    public string OvertimeAlertTitle { get; set; } = string.Empty;
    public string OvertimeAlertMessage { get; set; } = string.Empty;
    public string OvertimeRemainingLimitDisplay { get; set; } = string.Empty;
}

public class AttendanceRecordDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;

    public int? WorkShiftId { get; set; }
    public string WorkShiftName { get; set; } = string.Empty;
    public string ShiftColor { get; set; } = "#2563eb";

    public DateTime WorkDate { get; set; }
    public string WorkDateDisplay => WorkDate.ToString("dd/MM/yyyy");
    public string DayOfWeekName => WorkDate.ToString("dddd", new System.Globalization.CultureInfo("vi-VN"));

    public DateTime? ClockInTime { get; set; }
    public DateTime? ClockOutTime { get; set; }
    public string ClockInDisplay => ClockInTime.HasValue ? ClockInTime.Value.AddHours(7).ToString("HH:mm:ss") : "--:--:--";
    public string ClockOutDisplay => ClockOutTime.HasValue ? ClockOutTime.Value.AddHours(7).ToString("HH:mm:ss") : "--:--:--";

    public AttendanceStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusCssClass { get; set; } = string.Empty;

    public int WorkingMinutes { get; set; }
    public string WorkingHoursDisplay => $"{WorkingMinutes / 60}h {(WorkingMinutes % 60):D2}m";

    public int LateMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public int OvertimeMinutes { get; set; }

    public bool IsOvernight { get; set; }
    public bool IsManuallyAdjusted { get; set; }
    public string? Notes { get; set; }
    public string? ClockInSource { get; set; }
    public string? ClockOutSource { get; set; }
}

public class AttendanceCalendarDayDto
{
    public DateTime Date { get; set; }
    public int DayNumber => Date.Day;
    public bool IsCurrentMonth { get; set; } = true;
    public bool IsToday { get; set; } = false;
    public bool IsWeekend => Date.DayOfWeek == DayOfWeek.Saturday || Date.DayOfWeek == DayOfWeek.Sunday;

    public bool HasRecord { get; set; } = false;
    public int? RecordId { get; set; }
    public AttendanceStatus? Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string BadgeClass { get; set; } = string.Empty;
    public string DotColor { get; set; } = "#94a3b8";

    public string ShiftName { get; set; } = string.Empty;
    public string ClockInDisplay { get; set; } = string.Empty;
    public string ClockOutDisplay { get; set; } = string.Empty;
    public string WorkingHoursDisplay { get; set; } = string.Empty;
    public int LateMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public string? Notes { get; set; }
}

public class AttendancePersonalKpiDto
{
    public decimal TotalWorkDays { get; set; }
    public decimal TotalWorkingHours { get; set; }
    public int TotalLateDays { get; set; }
    public int TotalMissedClockOuts { get; set; }
    public decimal TotalLeaveDays { get; set; }
    public int PendingRequestsCount { get; set; }
}

public class AttendanceHrDashboardKpiDto
{
    public int TotalEligibleEmployees { get; set; }
    public int ClockedInCount { get; set; }
    public int NotClockedInCount { get; set; }
    public int LateCount { get; set; }
    public int EarlyLeaveCount { get; set; }
    public int OnLeaveCount { get; set; }
    public int AbsentCount { get; set; }
    public int MissingClockOutCount { get; set; }
    public int PendingRequestsCount { get; set; }
}

public class AttendanceAdjustmentRequestDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    public int? AttendanceRecordId { get; set; }
    public AttendanceAdjustmentType RequestType { get; set; }
    public string RequestTypeLabel { get; set; } = string.Empty;

    public DateTime TargetDate { get; set; }
    public string TargetDateDisplay => TargetDate.ToString("dd/MM/yyyy");

    public DateTime? ProposedClockIn { get; set; }
    public DateTime? ProposedClockOut { get; set; }
    public string ProposedClockInDisplay => ProposedClockIn.HasValue ? ProposedClockIn.Value.AddHours(7).ToString("HH:mm") : "--:--";
    public string ProposedClockOutDisplay => ProposedClockOut.HasValue ? ProposedClockOut.Value.AddHours(7).ToString("HH:mm") : "--:--";
    public string ProposedRangeDisplay => $"{ProposedClockInDisplay} - {ProposedClockOutDisplay}";

    public string Reason { get; set; } = string.Empty;
    public string? AttachmentPath { get; set; }

    public AttendanceRequestStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusCssClass { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public string CreatedAtDisplay => CreatedAt.AddHours(7).ToString("dd/MM/yyyy HH:mm");

    public string? ReviewedByUserName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
}

public class LeaveRequestDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    public LeaveType LeaveType { get; set; }
    public string LeaveTypeLabel { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string DateRangeDisplay => StartDate.Date == EndDate.Date
        ? StartDate.ToString("dd/MM/yyyy")
        : $"{StartDate:dd/MM/yyyy} - {EndDate:dd/MM/yyyy}";

    public decimal TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? AttachmentPath { get; set; }

    public AttendanceRequestStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusCssClass { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public string CreatedAtDisplay => CreatedAt.AddHours(7).ToString("dd/MM/yyyy HH:mm");

    public string? ReviewedByUserName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
}

public class ShiftChangeRequestDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    public DateTime TargetDate { get; set; }
    public string TargetDateDisplay => TargetDate.ToString("dd/MM/yyyy");

    public int CurrentWorkShiftId { get; set; }
    public string CurrentWorkShiftName { get; set; } = string.Empty;

    public int ProposedWorkShiftId { get; set; }
    public string ProposedWorkShiftName { get; set; } = string.Empty;

    public int? SubstituteEmployeeId { get; set; }
    public string? SubstituteEmployeeName { get; set; }

    public string Reason { get; set; } = string.Empty;

    public AttendanceRequestStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusCssClass { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public string CreatedAtDisplay => CreatedAt.AddHours(7).ToString("dd/MM/yyyy HH:mm");

    public string? ReviewedByUserName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
}

public class WorkShiftDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public WorkShiftType ShiftType { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string TimeRangeDisplay => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    public int BreakMinutes { get; set; }
    public int GracePeriodMinutes { get; set; }
    public int FlexibleMinutes { get; set; }
    public bool IsOvernight { get; set; }
    public bool IsActive { get; set; }
    public string Color { get; set; } = "#2563eb";
    public string? AppliesTo { get; set; }
    public string? Notes { get; set; }
    public int AssignedEmployeeCount { get; set; }
}

public class AttendanceAuditLogDto
{
    public int Id { get; set; }
    public int? AttendanceRecordId { get; set; }
    public int? RequestId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ChangedByUserName { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedAtDisplay => ChangedAt.AddHours(7).ToString("dd/MM/yyyy HH:mm:ss");
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class AttendanceReportSummaryDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string WorkingBranch { get; set; } = string.Empty;

    public decimal StandardWorkDays { get; set; } = 26;
    public decimal ActualWorkDays { get; set; }
    public decimal TotalWorkingHours { get; set; }
    public int LateTimes { get; set; }
    public int LateTotalMinutes { get; set; }
    public int EarlyLeaveTimes { get; set; }
    public int EarlyLeaveTotalMinutes { get; set; }
    public int MissedClockOutTimes { get; set; }
    public decimal LeaveDays { get; set; }
    public decimal PaidLeaveDays { get; set; }
    public decimal UnpaidLeaveDays { get; set; }
    public decimal OvertimeHours { get; set; }
    public int AttendanceScore { get; set; } = 100;
}

// Commands
public class ClockInCommand
{
    public int? EmployeeId { get; set; }
    public string? Device { get; set; }
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public string? Source { get; set; } = "Web";
    public string? Notes { get; set; }
}

public class ClockOutCommand
{
    public int? EmployeeId { get; set; }
    public string? Device { get; set; }
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public string? Source { get; set; } = "Web";
    public string? Notes { get; set; }
}

public class CreateAdjustmentRequestCommand
{
    public int? EmployeeId { get; set; }
    public int? AttendanceRecordId { get; set; }
    public AttendanceAdjustmentType RequestType { get; set; }
    public DateTime TargetDate { get; set; }
    public DateTime? ProposedClockIn { get; set; }
    public DateTime? ProposedClockOut { get; set; }
    public TimeSpan? ProposedClockInTime { get; set; }
    public TimeSpan? ProposedClockOutTime { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? AttachmentPath { get; set; }
}

public class CreateLeaveRequestCommand
{
    public int? EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalDays { get; set; } = 1.0m;
    public string Reason { get; set; } = string.Empty;
    public string? AttachmentPath { get; set; }
}

public class CreateShiftChangeRequestCommand
{
    public int? EmployeeId { get; set; }
    public DateTime TargetDate { get; set; }
    public int CurrentWorkShiftId { get; set; }
    public int ProposedWorkShiftId { get; set; }
    public int? SubstituteEmployeeId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ReviewRequestCommand
{
    public int RequestId { get; set; }
    public bool Approved { get; set; }
    public bool IsApproved { get => Approved; set => Approved = value; }
    public string? ReviewNotes { get; set; }
}

public class LeaveBalanceSummaryDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int Year { get; set; } = 2026;
    public decimal TotalEntitlement { get; set; } = 12.0m;
    public decimal TotalEntitled => TotalEntitlement;
    public decimal AccruedDays { get; set; } = 11.0m;
    public decimal UsedDays { get; set; } = 0.0m;
    public decimal PendingDays { get; set; } = 0.0m;
    public decimal RemainingDays => Math.Max(0, AccruedDays - UsedDays);
    public List<LeaveLedgerTransactionDto> Transactions { get; set; } = new();
}

public class LeaveLedgerTransactionDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public LeaveLedgerTransactionType TransactionType { get; set; }
    public string TransactionTypeLabel { get; set; } = string.Empty;
    public decimal DaysChanged { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? LeaveRequestId { get; set; }
    public string? PerformedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedAtDisplay => CreatedAt.AddHours(7).ToString("dd/MM/yyyy HH:mm");
    public string? Notes { get; set; }
}

public class GrantExtraLeaveCommand
{
    public int EmployeeId { get; set; }
    public decimal Days { get; set; } = 1.0m;
    public decimal DaysToRestore { get => Days; set => Days = value; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class DemoDayStatusDto
{
    public DateTime Date { get; set; }
    public string DayOfWeekName { get; set; } = string.Empty;
    public string DateDisplay => Date.ToString("dd/MM");
    public AttendanceStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string BadgeClass { get; set; } = string.Empty;
    public string ClockIn { get; set; } = "--:--";
    public string ClockOut { get; set; } = "--:--";
    public string WorkingHours { get; set; } = "--";
    public string Note { get; set; } = string.Empty;
}

public class WeeklyDemoEmployeeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string ShiftName { get; set; } = "Ca hành chính";
    public List<DemoDayStatusDto> Days { get; set; } = new();
    public int TotalWorkDays { get; set; }
    public decimal TotalHours { get; set; }
    public int LateCount { get; set; }
    public int LeaveCount { get; set; }
}

public class UndoReviewCommand
{
    public int RequestId { get; set; }
    public string RequestType { get; set; } = "adjustment";
    public string Category { get => RequestType; set => RequestType = value; } // "adjustment", "leave", "shift"
    public string Reason { get; set; } = string.Empty;
}

public class UndoEligibilityDto
{
    public bool CanUndo { get; set; }
    public string ReasonIfNotEligible { get; set; } = string.Empty;
    public TimeSpan RemainingTime { get; set; }
    public string RemainingTimeDisplay { get; set; } = string.Empty;
    public DateTime? DeadlineVn { get; set; }
    public string PriorDecisionLabel { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public DateTime? ReviewedAtVn { get; set; }
}

public class RestoreLeaveCommand
{
    public int EmployeeId { get; set; }
    public decimal Days { get; set; } = 1.0m;
    public string Reason { get; set; } = string.Empty;
    public int? RelatedLeaveRequestId { get; set; }
    public string? Notes { get; set; }
}

public class EmployeeMonitoringDetailDto
{
    // Thông tin cá nhân và công việc
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string RoleName { get; set; } = "Nhân viên";
    public string ShiftName { get; set; } = "Ca hành chính";
    public string ShiftTimeRange { get; set; } = "08:00 - 17:30";

    // Theo dõi công
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal MonthlyStandardWorkDays { get; set; } = 26;
    public decimal FullAttendanceDays { get; set; }
    public decimal UnclockedDays { get; set; }
    public int LateTimes { get; set; }
    public int EarlyLeaveTimes { get; set; }
    public int MissedClockInTimes { get; set; }
    public int MissedClockOutTimes { get; set; }
    public decimal TotalWorkingHours { get; set; }
    public decimal TotalOvertimeHours { get; set; }

    // Theo dõi phép
    public decimal TotalLeaveEntitlement { get; set; } = 12.0m;
    public decimal UsedLeaveDays { get; set; }
    public decimal PendingLeaveDays { get; set; }
    public decimal RemainingLeaveDays { get; set; }
    public decimal RestoredOrAdjustedLeaveDays { get; set; }

    // UI Aliases
    public decimal MonthlyWorkdays => MonthlyStandardWorkDays;
    public decimal MissingWorkdays => UnclockedDays;
    public int LateCount => LateTimes;
    public int EarlyCount => EarlyLeaveTimes;
    public int MissingCheckInCount => MissedClockInTimes;
    public int MissingCheckOutCount => MissedClockOutTimes;
    public string TotalWorkingHoursDisplay => $"{TotalWorkingHours:N1} giờ";
    public string TotalOvertimeHoursDisplay => $"{TotalOvertimeHours:N1} giờ";
    public decimal TotalLeaveEntitled => TotalLeaveEntitlement;
    public decimal LeaveUsedDays => UsedLeaveDays;
    public decimal LeavePendingDays => PendingLeaveDays;
    public decimal LeaveRemainingDays => RemainingLeaveDays;
    public decimal LeaveAdjustedOrRestoredDays => RestoredOrAdjustedLeaveDays;

    // Theo dõi vi phạm & bất thường
    public int MissingPunchDaysCount { get; set; }
    public int MissingCheckOutDaysCount { get; set; }
    public int MissingExplanationCount { get; set; }
    public int LateOrEarlyDaysCount { get; set; }
    public int PendingRequestsCount { get; set; }
    public int ResolvedAnomaliesCount { get; set; }
    public int PendingAnomaliesCount { get; set; }
    public List<EmployeeAnomalyRecordDto> Anomalies { get; set; } = new();
}

public class EmployeeAnomalyRecordDto
{
    public DateTime Date { get; set; }
    public string DateDisplay => Date.ToString("dd/MM/yyyy");
    public string AnomalyType { get; set; } = string.Empty; // "Thiếu check-in", "Thiếu check-out", "Đi muộn", "Về sớm", "Thiếu giải trình"
    public string Status { get; set; } = "Chờ xử lý"; // "Chờ xử lý" | "Đã xử lý" | "Đang chờ duyệt"
    public string Details { get; set; } = string.Empty;
    public int? RequestId { get; set; }
}
