using TradeFlow.Application.Common.Models.Attendance;

namespace TradeFlow.Application.Common.Interfaces;

public interface IAttendanceService
{
    // === 1. Cá nhân (Employee Self-Service) ===
    Task<AttendanceWidgetDto> GetTodayWidgetAsync(string userId, int? targetEmployeeId = null, CancellationToken ct = default);
    Task<AttendanceWidgetDto> ClockInAsync(string userId, ClockInCommand command, CancellationToken ct = default);
    Task<AttendanceWidgetDto> ClockOutAsync(string userId, ClockOutCommand command, CancellationToken ct = default);
    Task<AttendancePersonalKpiDto> GetPersonalKpisAsync(int employeeId, int month, int year, CancellationToken ct = default);
    Task<List<AttendanceCalendarDayDto>> GetPersonalCalendarDaysAsync(int? employeeId, int month, int year, CancellationToken ct = default);
    Task<List<AttendanceRecordDto>> GetPersonalHistoryAsync(int employeeId, int? month = null, int? year = null, CancellationToken ct = default);
    Task<List<AttendanceAdjustmentRequestDto>> GetPersonalAdjustmentRequestsAsync(int employeeId, CancellationToken ct = default);
    Task<List<LeaveRequestDto>> GetPersonalLeaveRequestsAsync(int employeeId, CancellationToken ct = default);
    Task<List<ShiftChangeRequestDto>> GetPersonalShiftChangeRequestsAsync(int employeeId, CancellationToken ct = default);

    Task<AttendanceAdjustmentRequestDto> SubmitAdjustmentRequestAsync(string userId, CreateAdjustmentRequestCommand command, CancellationToken ct = default);
    Task<LeaveRequestDto> SubmitLeaveRequestAsync(string userId, CreateLeaveRequestCommand command, CancellationToken ct = default);
    Task<ShiftChangeRequestDto> SubmitShiftChangeRequestAsync(string userId, CreateShiftChangeRequestCommand command, CancellationToken ct = default);

    // === 2. Quản lý nhóm (Manager Team Scope) ===
    Task<List<AttendanceRecordDto>> GetTeamAttendanceAsync(string managerUserId, DateTime date, int? departmentId = null, CancellationToken ct = default);
    Task<AttendanceHrDashboardKpiDto> GetTeamKpisAsync(string managerUserId, DateTime date, CancellationToken ct = default);

    // === 3. HR Enterprise Oversight ===
    Task<AttendanceHrDashboardKpiDto> GetHrDashboardKpisAsync(DateTime date, int? departmentId = null, string? branch = null, int? shiftId = null, CancellationToken ct = default);
    Task<List<AttendanceRecordDto>> GetLiveAttendanceTrackingAsync(DateTime date, int? departmentId = null, string? branch = null, int? shiftId = null, string? search = null, string? statusFilter = null, CancellationToken ct = default);
    Task<List<AttendanceRecordDto>> GetExceptionsListAsync(DateTime date, int? departmentId = null, CancellationToken ct = default);

    // === 4. Phê duyệt yêu cầu (Approval Workflows) ===
    Task<List<AttendanceAdjustmentRequestDto>> GetAllAdjustmentRequestsAsync(string? statusFilter = null, int? departmentId = null, CancellationToken ct = default);
    Task<List<LeaveRequestDto>> GetAllLeaveRequestsAsync(string? statusFilter = null, int? departmentId = null, CancellationToken ct = default);
    Task<List<ShiftChangeRequestDto>> GetAllShiftChangeRequestsAsync(string? statusFilter = null, int? departmentId = null, CancellationToken ct = default);

    Task<bool> ReviewAdjustmentRequestAsync(string reviewerUserId, ReviewRequestCommand command, CancellationToken ct = default);
    Task<bool> ReviewLeaveRequestAsync(string reviewerUserId, ReviewRequestCommand command, CancellationToken ct = default);
    Task<bool> ReviewShiftChangeRequestAsync(string reviewerUserId, ReviewRequestCommand command, CancellationToken ct = default);

    // Hoàn tác phê duyệt (Approval Undo)
    Task<UndoEligibilityDto> CheckUndoEligibilityAsync(string requestType, int requestId, string currentUserId, CancellationToken ct = default);
    Task<bool> UndoReviewRequestAsync(string userId, UndoReviewCommand command, CancellationToken ct = default);

    // Khôi phục ngày phép Admin (Privileged Leave Restoration)
    Task<bool> RestoreLeaveBalanceAsync(string adminUserId, RestoreLeaveCommand command, CancellationToken ct = default);

    // Theo dõi chi tiết nhân viên (Employee Monitoring Accordion)
    Task<EmployeeMonitoringDetailDto> GetEmployeeMonitoringDetailAsync(int employeeId, int month, int year, CancellationToken ct = default);

    // === 5. Quản lý Ca làm & Lịch làm việc (Work Shifts & Schedules) ===
    Task<List<WorkShiftDto>> GetAllWorkShiftsAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<WorkShiftDto> SaveWorkShiftAsync(WorkShiftDto shift, string userId, CancellationToken ct = default);
    Task<bool> DeleteWorkShiftAsync(int shiftId, string userId, CancellationToken ct = default);
    Task<bool> AssignShiftAsync(int employeeId, int shiftId, DateTime from, DateTime? to, string userId, CancellationToken ct = default);

    // === 6. Báo cáo & Audit Log ===
    Task<List<AttendanceReportSummaryDto>> GetMonthlyReportAsync(int month, int year, int? departmentId = null, string? branch = null, CancellationToken ct = default);
    Task<List<AttendanceAuditLogDto>> GetAuditLogsAsync(int? employeeId = null, int take = 50, CancellationToken ct = default);
    Task<int?> ResolveEmployeeIdForUserAsync(string userId, CancellationToken ct = default);

    // === 7. Số dư phép năm & Sổ cái phát sinh (Leave Balance & Ledger) ===
    Task<LeaveBalanceSummaryDto> GetLeaveBalanceSummaryAsync(int? employeeId, int year = 2026, CancellationToken ct = default);
    Task<bool> GrantExtraLeaveAsync(GrantExtraLeaveCommand command, string? performedByUserId, string? performedByUserName, bool isAdmin, CancellationToken ct = default);

    // === 8. Dữ liệu minh họa (Controlled Weekly Demo Dataset) ===
    Task<List<WeeklyDemoEmployeeDto>> GetWeeklyDemoDatasetAsync(CancellationToken ct = default);
    Task<bool> IsUserExemptFromAttendanceAsync(string userId, CancellationToken ct = default);
}
