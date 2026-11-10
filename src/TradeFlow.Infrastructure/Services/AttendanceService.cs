using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Attendance;
using TradeFlow.Domain.Entities.Attendance;
using TradeFlow.Domain.Entities.Employees;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class AttendanceService : IAttendanceService
{
    private readonly TradeFlowDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AttendanceService> _logger;
    private readonly IAuditService? _auditService;

    public AttendanceService(
        TradeFlowDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<AttendanceService> logger,
        IAuditService? auditService = null)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _auditService = auditService;
    }

    private static DateTime GetVnTime() => DateTime.UtcNow.AddHours(7);
    private static DateTime GetVnDate() => DateTime.SpecifyKind(GetVnTime().Date, DateTimeKind.Utc);
    private static DateTime UtcDate(int year, int month, int day) => DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);

    public async Task<int?> ResolveEmployeeIdForUserAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return null;

        // Owner / Administrator accounts are pure system administrative identities, never tied to employee records
        if (string.Equals(user.UserName, "admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(user.Email, "admin@tradeflow.local", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(user.Email, "admin@tradeflow.vn", StringComparison.OrdinalIgnoreCase) ||
            await _userManager.IsInRoleAsync(user, "Administrator"))
        {
            return null;
        }

        if (user.EmployeeId.HasValue && user.EmployeeId.Value > 0)
        {
            return user.EmployeeId.Value;
        }

        if (user != null && !string.IsNullOrWhiteSpace(user.Email))
        {
            var empByEmail = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Email == user.Email, ct);
            if (empByEmail != null) return empByEmail.Id;
        }

        if (user != null && !string.IsNullOrWhiteSpace(user.UserName))
        {
            var empByCode = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Code == user.UserName, ct);
            if (empByCode != null) return empByCode.Id;
        }

        // Owner/Admin accounts are not tied to attendance rosters
        if (user != null && (string.Equals(user.UserName, "admin", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(user.Email, "admin@tradeflow.vn", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        return null;
    }

    private async Task<WorkShift?> ResolveAssignedShiftAsync(int employeeId, DateTime date, CancellationToken ct = default)
    {
        var targetDate = date.Kind == DateTimeKind.Utc ? date.Date : DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var dayOfWeek = targetDate.DayOfWeek;

        var assignment = await _context.EmployeeShiftAssignments
            .Include(a => a.WorkShift)
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId &&
                        a.EffectiveFrom <= targetDate &&
                        (!a.EffectiveTo.HasValue || a.EffectiveTo.Value >= targetDate))
            .OrderByDescending(a => a.SpecificDate.HasValue)
            .ThenByDescending(a => a.DayOfWeek.HasValue)
            .FirstOrDefaultAsync(a => a.SpecificDate == targetDate || a.DayOfWeek == dayOfWeek || a.DayOfWeek == null, ct);

        if (assignment?.WorkShift != null && assignment.WorkShift.IsActive)
        {
            return assignment.WorkShift;
        }

        // Fallback default shift CA-HC or first active
        var defaultShift = await _context.WorkShifts
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.Code == "CA-HC")
            .ThenBy(s => s.Id)
            .FirstOrDefaultAsync(ct);

        return defaultShift;
    }

    public async Task<AttendanceWidgetDto> GetTodayWidgetAsync(string userId, int? targetEmployeeId = null, CancellationToken ct = default)
    {
        var employeeId = targetEmployeeId ?? await ResolveEmployeeIdForUserAsync(userId, ct);
        if (!employeeId.HasValue)
        {
            return new AttendanceWidgetDto
            {
                ServerTimeVn = GetVnTime(),
                StatusLabel = "Chưa liên kết hồ sơ nhân viên",
                CanClockIn = false,
                CanClockOut = false
            };
        }

        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId.Value, ct);

        if (employee == null)
        {
            return new AttendanceWidgetDto
            {
                ServerTimeVn = GetVnTime(),
                StatusLabel = "Không tìm thấy nhân sự",
                CanClockIn = false,
                CanClockOut = false
            };
        }

        var nowVn = GetVnTime();
        var todayVn = nowVn.Date;
        var shift = await ResolveAssignedShiftAsync(employee.Id, todayVn, ct);

        var todayRecord = await _context.AttendanceRecords
            .Include(r => r.WorkShift)
            .FirstOrDefaultAsync(r => r.EmployeeId == employee.Id && r.WorkDate == todayVn, ct);

        var widget = new AttendanceWidgetDto
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullName,
            AvatarPath = employee.AvatarPath,
            DepartmentName = employee.Department?.Name ?? "Chưa phân ban",
            PositionName = employee.Position?.Name ?? "Nhân viên",
            WorkingBranch = employee.WorkingBranch ?? "Văn phòng chính",
            ServerTimeVn = nowVn
        };

        if (shift != null)
        {
            widget.WorkShiftId = shift.Id;
            widget.ShiftName = shift.Name;
            widget.ShiftCode = shift.Code;
            widget.ShiftStartTime = shift.StartTime;
            widget.ShiftEndTime = shift.EndTime;
            widget.BreakMinutes = shift.BreakMinutes;
            widget.GracePeriodMinutes = shift.GracePeriodMinutes;
            widget.IsOvernight = shift.IsOvernight;
        }

        if (todayRecord != null)
        {
            widget.TodayRecordId = todayRecord.Id;
            widget.ClockInTimeUtc = todayRecord.ClockInTime;
            widget.ClockOutTimeUtc = todayRecord.ClockOutTime;
            widget.Status = todayRecord.Status;
            widget.WorkingMinutes = todayRecord.WorkingMinutes;

            if (todayRecord.ClockInTime.HasValue)
            {
                var inVn = todayRecord.ClockInTime.Value.AddHours(7);
                widget.ClockInDisplay = inVn.ToString("HH:mm:ss");

                if (!todayRecord.ClockOutTime.HasValue)
                {
                    widget.CanClockIn = false;
                    widget.CanClockOut = true;
                    widget.IsCompleted = false;

                    var curMinutes = Math.Max(0, (int)(nowVn - inVn).TotalMinutes);
                    if (shift != null && curMinutes > shift.BreakMinutes)
                    {
                        curMinutes -= shift.BreakMinutes;
                    }
                    widget.WorkingMinutes = curMinutes;
                    widget.WorkingDurationDisplay = $"{curMinutes / 60}h {(curMinutes % 60):D2}m";
                }
                else
                {
                    var outVn = todayRecord.ClockOutTime.Value.AddHours(7);
                    widget.ClockOutDisplay = outVn.ToString("HH:mm:ss");
                    widget.CanClockIn = false;
                    widget.CanClockOut = false;
                    widget.IsCompleted = true;
                    widget.WorkingDurationDisplay = $"{todayRecord.WorkingMinutes / 60}h {(todayRecord.WorkingMinutes % 60):D2}m";

                    if (shift != null && outVn.TimeOfDay > shift.EndTime)
                    {
                        var rawOt = (int)(outVn.TimeOfDay - shift.EndTime).TotalMinutes;
                        if (rawOt > 15)
                        {
                            var validOt = Math.Min(120, rawOt);
                            var excessOt = Math.Max(0, rawOt - 120);
                            widget.IsOvertimeAlert = true;
                            widget.OvertimeMinutes = rawOt;
                            widget.ValidOvertimeHours = Math.Round((decimal)validOt / 60m, 1);
                            widget.ExcessOvertimeHours = Math.Round((decimal)excessOt / 60m, 1);
                            widget.OvertimeAlertTitle = "Bạn đang chấm công ngoài giờ làm việc – Tăng ca";
                            widget.OvertimeAlertMessage = $"Ca làm việc: {shift.Name} (kết thúc lúc {shift.EndTime:hh\\:mm}). Giờ chấm ra thực tế: {outVn:HH:mm:ss}. Thời lượng tăng ca dự kiến: {rawOt / 60}h {(rawOt % 60):D2}m.";
                            var remLimit = Math.Max(0, 120 - validOt);
                            widget.OvertimeRemainingLimitDisplay = $"{remLimit / 60}h {remLimit % 60}m (giới hạn 2h/ngày)";
                        }
                    }
                }
            }

            MapStatusPresentation(widget.Status, out var label, out var css);
            widget.StatusLabel = label;
            widget.StatusCssClass = css;
        }
        else
        {
            widget.CanClockIn = true;
            widget.CanClockOut = false;
            widget.IsCompleted = false;
            widget.Status = AttendanceStatus.NotClockedIn;
            widget.StatusLabel = "Chưa vào ca";
            widget.StatusCssClass = "tf-status-not-in";
        }

        return widget;
    }

    public async Task<AttendanceWidgetDto> ClockInAsync(string userId, ClockInCommand command, CancellationToken ct = default)
    {
        var employeeId = command.EmployeeId > 0 ? command.EmployeeId : await ResolveEmployeeIdForUserAsync(userId, ct);
        if (!employeeId.HasValue)
            throw new InvalidOperationException("Không thể xác định hồ sơ nhân viên để chấm công.");

        var employee = await _context.Employees.FindAsync(new object[] { employeeId.Value }, ct)
            ?? throw new InvalidOperationException("Hồ sơ nhân viên không tồn tại.");

        var nowUtc = DateTime.UtcNow;
        var nowVn = nowUtc.AddHours(7);
        var todayVn = nowVn.Date;

        var existingRecord = await _context.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == employee.Id && r.WorkDate == todayVn, ct);

        if (existingRecord != null && existingRecord.ClockInTime.HasValue)
        {
            throw new InvalidOperationException($"Nhân viên {employee.FullName} đã chấm công vào ca lúc {existingRecord.ClockInTime.Value.AddHours(7):HH:mm:ss}. Không thể chấm vào lần 2!");
        }

        var shift = await ResolveAssignedShiftAsync(employee.Id, todayVn, ct);
        var lateMinutes = 0;
        var status = AttendanceStatus.OnTime;

        if (shift != null)
        {
            var shiftStart = shift.StartTime;
            var currentTime = nowVn.TimeOfDay;
            var graceLimit = shiftStart.Add(TimeSpan.FromMinutes(shift.GracePeriodMinutes));

            if (currentTime > graceLimit)
            {
                lateMinutes = (int)(currentTime - shiftStart).TotalMinutes;
                status = AttendanceStatus.Late;
            }
        }

        var record = existingRecord ?? new AttendanceRecord
        {
            EmployeeId = employee.Id,
            WorkDate = todayVn
        };

        record.WorkShiftId = shift?.Id;
        record.ClockInTime = nowUtc;
        record.ClockInSource = command.Source ?? "Web";
        record.ClockInIp = command.IpAddress;
        record.ClockInDevice = command.DeviceInfo;
        record.Status = status;
        record.LateMinutes = lateMinutes;
        record.Notes = command.Notes;

        if (record.Id == 0)
        {
            _context.AttendanceRecords.Add(record);
        }

        await _context.SaveChangesAsync(ct);

        // Audit Log
        var user = await _userManager.FindByIdAsync(userId);
        var audit = new AttendanceAuditLog
        {
            AttendanceRecordId = record.Id,
            Action = "CLOCK_IN",
            Description = $"Chấm công vào ca: {employee.FullName} ({employee.Code}) lúc {nowVn:HH:mm:ss} (Trạng thái: {(status == AttendanceStatus.Late ? $"Đi muộn {lateMinutes} phút" : "Đúng giờ")})",
            ChangedByUserId = userId,
            ChangedByUserName = user?.UserName ?? employee.FullName,
            ChangedAt = nowUtc,
            IpAddress = command.IpAddress,
            UserAgent = command.DeviceInfo,
            NewValuesJson = JsonSerializer.Serialize(new { record.Id, record.EmployeeId, ClockIn = record.ClockInTime, record.Status, record.LateMinutes })
        };
        _context.AttendanceAuditLogs.Add(audit);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Employee {EmpCode} clocked in successfully at {TimeVn}", employee.Code, nowVn);
        return await GetTodayWidgetAsync(userId, employee.Id, ct);
    }

    public async Task<AttendanceWidgetDto> ClockOutAsync(string userId, ClockOutCommand command, CancellationToken ct = default)
    {
        var employeeId = command.EmployeeId > 0 ? command.EmployeeId : await ResolveEmployeeIdForUserAsync(userId, ct);
        if (!employeeId.HasValue)
            throw new InvalidOperationException("Không thể xác định hồ sơ nhân viên để chấm công ra.");

        var employee = await _context.Employees.FindAsync(new object[] { employeeId.Value }, ct)
            ?? throw new InvalidOperationException("Hồ sơ nhân viên không tồn tại.");

        var nowUtc = DateTime.UtcNow;
        var nowVn = nowUtc.AddHours(7);
        var todayVn = nowVn.Date;

        var record = await _context.AttendanceRecords
            .Include(r => r.WorkShift)
            .FirstOrDefaultAsync(r => r.EmployeeId == employee.Id && r.WorkDate == todayVn, ct);

        if (record == null || !record.ClockInTime.HasValue)
        {
            throw new InvalidOperationException("Bạn chưa chấm công vào ca hôm nay, không thể chấm ra ca!");
        }

        if (record.ClockOutTime.HasValue)
        {
            throw new InvalidOperationException($"Bạn đã chấm công ra ca lúc {record.ClockOutTime.Value.AddHours(7):HH:mm:ss}!");
        }

        var shift = record.WorkShift ?? await ResolveAssignedShiftAsync(employee.Id, todayVn, ct);
        var earlyLeaveMinutes = 0;
        var validOvertimeMinutes = 0;
        var excessOvertimeMinutes = 0;
        var isOvertime = false;

        if (shift != null)
        {
            var shiftEnd = shift.EndTime;
            var currentTime = nowVn.TimeOfDay;

            if (currentTime < shiftEnd)
            {
                earlyLeaveMinutes = (int)(shiftEnd - currentTime).TotalMinutes;
                if (record.Status == AttendanceStatus.OnTime)
                {
                    record.Status = AttendanceStatus.EarlyLeave;
                }
                else if (record.Status == AttendanceStatus.Late)
                {
                    record.Status = AttendanceStatus.LateAndEarlyLeave;
                }
            }
            else if (currentTime > shiftEnd)
            {
                var rawOt = (int)(currentTime - shiftEnd).TotalMinutes;
                if (rawOt > 15)
                {
                    isOvertime = true;
                    // Daily limit: max 2 hours (120 min) per person per day
                    validOvertimeMinutes = Math.Min(120, rawOt);
                    excessOvertimeMinutes = Math.Max(0, rawOt - 120);
                }
            }
        }

        var totalElapsedMinutes = (int)(nowUtc - record.ClockInTime.Value).TotalMinutes;
        var breakMinutes = shift?.BreakMinutes ?? 60;
        var workingMinutes = Math.Max(0, totalElapsedMinutes - breakMinutes);

        record.ClockOutTime = nowUtc;
        record.ClockOutSource = command.Source ?? "Web";
        record.ClockOutIp = command.IpAddress;
        record.ClockOutDevice = command.DeviceInfo;
        record.EarlyLeaveMinutes = earlyLeaveMinutes;
        record.OvertimeMinutes = validOvertimeMinutes;
        record.WorkingMinutes = workingMinutes;

        if (isOvertime)
        {
            var otNote = excessOvertimeMinutes > 0
                ? $"[Tăng ca: {(validOvertimeMinutes + excessOvertimeMinutes) / 60}h {((validOvertimeMinutes + excessOvertimeMinutes) % 60):D2}m. Hợp lệ: {validOvertimeMinutes / 60}h {validOvertimeMinutes % 60}m (tối đa 2h/ngày). Vượt mức: {excessOvertimeMinutes} phút – Cần xét duyệt]"
                : $"[Tăng ca: {validOvertimeMinutes / 60}h {(validOvertimeMinutes % 60):D2}m hợp lệ]";
            record.Notes = string.IsNullOrWhiteSpace(record.Notes) ? otNote : $"{record.Notes} | {otNote}";
        }

        if (!string.IsNullOrWhiteSpace(command.Notes))
        {
            record.Notes = string.IsNullOrWhiteSpace(record.Notes) ? command.Notes : $"{record.Notes} | {command.Notes}";
        }

        await _context.SaveChangesAsync(ct);

        // Audit Log
        var user = await _userManager.FindByIdAsync(userId);
        var audit = new AttendanceAuditLog
        {
            AttendanceRecordId = record.Id,
            Action = "CLOCK_OUT",
            Description = $"Chấm công ra ca: {employee.FullName} ({employee.Code}) lúc {nowVn:HH:mm:ss} (Tổng làm việc: {workingMinutes / 60}h {workingMinutes % 60}m)",
            ChangedByUserId = userId,
            ChangedByUserName = user?.UserName ?? employee.FullName,
            ChangedAt = nowUtc,
            IpAddress = command.IpAddress,
            UserAgent = command.DeviceInfo,
            NewValuesJson = JsonSerializer.Serialize(new { record.Id, record.EmployeeId, ClockOut = record.ClockOutTime, record.WorkingMinutes, record.EarlyLeaveMinutes, record.Status })
        };
        _context.AttendanceAuditLogs.Add(audit);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Employee {EmpCode} clocked out successfully at {TimeVn}", employee.Code, nowVn);
        return await GetTodayWidgetAsync(userId, employee.Id, ct);
    }

    public async Task<AttendancePersonalKpiDto> GetPersonalKpisAsync(int employeeId, int month, int year, CancellationToken ct = default)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var startDate = UtcDate(year, month, 1);
        var endDate = UtcDate(year, month, daysInMonth);

        var records = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId && r.WorkDate >= startDate && r.WorkDate <= endDate)
            .ToListAsync(ct);

        var approvedLeaves = await _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.EmployeeId == employeeId &&
                        l.Status == AttendanceRequestStatus.Approved &&
                        l.StartDate <= endDate && l.EndDate >= startDate)
            .ToListAsync(ct);

        var pendingAdjustments = await _context.AttendanceAdjustmentRequests
            .CountAsync(r => r.EmployeeId == employeeId && r.Status == AttendanceRequestStatus.Pending, ct);
        var pendingLeaves = await _context.LeaveRequests
            .CountAsync(r => r.EmployeeId == employeeId && r.Status == AttendanceRequestStatus.Pending, ct);
        var pendingShifts = await _context.ShiftChangeRequests
            .CountAsync(r => r.EmployeeId == employeeId && r.Status == AttendanceRequestStatus.Pending, ct);

        var workDays = records.Count(r => r.ClockInTime.HasValue);
        var totalMinutes = records.Sum(r => r.WorkingMinutes);
        var lateDays = records.Count(r => r.LateMinutes > 0 || r.Status == AttendanceStatus.Late || r.Status == AttendanceStatus.LateAndEarlyLeave);
        var missedClockOuts = records.Count(r => r.ClockInTime.HasValue && !r.ClockOutTime.HasValue && r.WorkDate < GetVnDate());
        var leaveDays = approvedLeaves.Sum(l => l.TotalDays);

        return new AttendancePersonalKpiDto
        {
            TotalWorkDays = workDays,
            TotalWorkingHours = Math.Round((decimal)totalMinutes / 60m, 1),
            TotalLateDays = lateDays,
            TotalMissedClockOuts = missedClockOuts,
            TotalLeaveDays = leaveDays,
            PendingRequestsCount = pendingAdjustments + pendingLeaves + pendingShifts
        };
    }

    public async Task<List<AttendanceCalendarDayDto>> GetPersonalCalendarDaysAsync(int? employeeId, int month, int year, CancellationToken ct = default)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var firstDayOfMonth = UtcDate(year, month, 1);
        var lastDayOfMonth = UtcDate(year, month, daysInMonth);
        var todayVn = GetVnDate();

        var records = new List<AttendanceRecord>();
        var leaves = new List<LeaveRequest>();

        if (employeeId.HasValue && employeeId.Value > 0)
        {
            records = await _context.AttendanceRecords
                .Include(r => r.WorkShift)
                .AsNoTracking()
                .Where(r => r.EmployeeId == employeeId.Value && r.WorkDate >= firstDayOfMonth && r.WorkDate <= lastDayOfMonth)
                .ToListAsync(ct);

            leaves = await _context.LeaveRequests
                .AsNoTracking()
                .Where(l => l.EmployeeId == employeeId.Value &&
                            l.Status == AttendanceRequestStatus.Approved &&
                            l.StartDate <= lastDayOfMonth && l.EndDate >= firstDayOfMonth)
                .ToListAsync(ct);
        }

        var result = new List<AttendanceCalendarDayDto>();

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            var isToday = date == todayVn;
            var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

            var rec = records.FirstOrDefault(r => r.WorkDate == date);
            var onLeave = leaves.FirstOrDefault(l => l.StartDate.Date <= date && l.EndDate.Date >= date);

            var dayDto = new AttendanceCalendarDayDto
            {
                Date = date,
                IsCurrentMonth = true,
                IsToday = isToday
            };

            if (rec != null)
            {
                dayDto.HasRecord = true;
                dayDto.RecordId = rec.Id;
                dayDto.Status = rec.Status;
                dayDto.ShiftName = rec.WorkShift?.Name ?? "Ca chuẩn";
                dayDto.LateMinutes = rec.LateMinutes;
                dayDto.EarlyLeaveMinutes = rec.EarlyLeaveMinutes;
                dayDto.Notes = rec.Notes;

                if (rec.ClockInTime.HasValue)
                {
                    dayDto.ClockInDisplay = rec.ClockInTime.Value.AddHours(7).ToString("HH:mm");
                }
                if (rec.ClockOutTime.HasValue)
                {
                    dayDto.ClockOutDisplay = rec.ClockOutTime.Value.AddHours(7).ToString("HH:mm");
                    dayDto.WorkingHoursDisplay = $"{rec.WorkingMinutes / 60}h {(rec.WorkingMinutes % 60):D2}m";
                }
                else if (rec.ClockInTime.HasValue && date < todayVn)
                {
                    dayDto.Status = AttendanceStatus.MissingClockOut;
                    dayDto.ClockOutDisplay = "Quên chấm";
                }

                MapCalendarDotAndBadge(dayDto.Status ?? AttendanceStatus.OnTime, out var label, out var badge, out var dot);
                dayDto.StatusLabel = label;
                dayDto.BadgeClass = badge;
                dayDto.DotColor = dot;
            }
            else if (onLeave != null)
            {
                dayDto.HasRecord = true;
                dayDto.Status = AttendanceStatus.OnLeave;
                dayDto.StatusLabel = $"Nghỉ phép ({onLeave.TotalDays}n)";
                dayDto.BadgeClass = "tf-cal-badge-leave";
                dayDto.DotColor = "#3b82f6";
                dayDto.Notes = onLeave.Reason;
            }
            else
            {
                if (isWeekend)
                {
                    dayDto.StatusLabel = "Cuối tuần";
                    dayDto.BadgeClass = "tf-cal-badge-weekend";
                    dayDto.DotColor = "#94a3b8";
                }
                else if (date < todayVn)
                {
                    dayDto.StatusLabel = "Vắng mặt";
                    dayDto.BadgeClass = "tf-cal-badge-absent";
                    dayDto.DotColor = "#ef4444";
                }
                else
                {
                    dayDto.StatusLabel = "Chưa đến";
                    dayDto.BadgeClass = "tf-cal-badge-future";
                    dayDto.DotColor = "#cbd5e1";
                }
            }

            result.Add(dayDto);
        }

        return result;
    }

    public async Task<List<AttendanceRecordDto>> GetPersonalHistoryAsync(int employeeId, int? month = null, int? year = null, CancellationToken ct = default)
    {
        var nowVn = GetVnDate();
        var targetYear = year ?? nowVn.Year;
        var targetMonth = month ?? nowVn.Month;

        var daysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);
        var start = UtcDate(targetYear, targetMonth, 1);
        var end = UtcDate(targetYear, targetMonth, daysInMonth);

        var query = _context.AttendanceRecords
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .Include(r => r.Employee).ThenInclude(e => e.Position)
            .Include(r => r.WorkShift)
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId && r.WorkDate >= start && r.WorkDate <= end)
            .OrderByDescending(r => r.WorkDate);

        var list = await query.ToListAsync(ct);
        return list.Select(MapToRecordDto).ToList();
    }

    public async Task<List<AttendanceAdjustmentRequestDto>> GetPersonalAdjustmentRequestsAsync(int employeeId, CancellationToken ct = default)
    {
        var items = await _context.AttendanceAdjustmentRequests
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return items.Select(MapAdjustmentRequestDto).ToList();
    }

    public async Task<List<LeaveRequestDto>> GetPersonalLeaveRequestsAsync(int employeeId, CancellationToken ct = default)
    {
        var items = await _context.LeaveRequests
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return items.Select(MapLeaveRequestDto).ToList();
    }

    public async Task<List<ShiftChangeRequestDto>> GetPersonalShiftChangeRequestsAsync(int employeeId, CancellationToken ct = default)
    {
        var items = await _context.ShiftChangeRequests
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .Include(r => r.CurrentWorkShift)
            .Include(r => r.ProposedWorkShift)
            .Include(r => r.SubstituteEmployee)
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return items.Select(MapShiftChangeRequestDto).ToList();
    }

    public async Task<AttendanceAdjustmentRequestDto> SubmitAdjustmentRequestAsync(string userId, CreateAdjustmentRequestCommand command, CancellationToken ct = default)
    {
        var employeeId = command.EmployeeId > 0 ? command.EmployeeId : await ResolveEmployeeIdForUserAsync(userId, ct);
        if (!employeeId.HasValue)
            throw new InvalidOperationException("Không thể xác định hồ sơ nhân viên.");

        var employee = await _context.Employees.FindAsync(new object[] { employeeId.Value }, ct)
            ?? throw new InvalidOperationException("Hồ sơ nhân viên không tồn tại.");

        var request = new AttendanceAdjustmentRequest
        {
            EmployeeId = employee.Id,
            AttendanceRecordId = command.AttendanceRecordId,
            RequestType = command.RequestType,
            TargetDate = DateTime.SpecifyKind(command.TargetDate.Date, DateTimeKind.Utc),
            ProposedClockIn = command.ProposedClockIn,
            ProposedClockOut = command.ProposedClockOut,
            Reason = command.Reason,
            AttachmentPath = command.AttachmentPath,
            Status = AttendanceRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.AttendanceAdjustmentRequests.Add(request);
        await _context.SaveChangesAsync(ct);

        var audit = new AttendanceAuditLog
        {
            RequestId = request.Id,
            Action = "REQUEST_ADJUSTMENT",
            Description = $"Gửi yêu cầu điều chỉnh công: {employee.FullName} ngày {request.TargetDate:dd/MM/yyyy}. Lý do: {request.Reason}",
            ChangedByUserId = userId,
            ChangedByUserName = employee.FullName,
            ChangedAt = DateTime.UtcNow
        };
        _context.AttendanceAuditLogs.Add(audit);
        await _context.SaveChangesAsync(ct);

        if (_auditService != null)
        {
            await _auditService.LogAsync(
                AuditEventType.AttendanceAdjustmentSubmitted,
                performedBy: employee.FullName,
                targetEntity: "AttendanceAdjustmentRequest",
                targetId: request.Id.ToString(),
                details: $"Yêu cầu điều chỉnh công: {employee.FullName} ({request.TargetDate:dd/MM/yyyy}). Lý do: {request.Reason}",
                cancellationToken: ct);
        }

        return MapAdjustmentRequestDto(request);
    }

    public async Task<LeaveRequestDto> SubmitLeaveRequestAsync(string userId, CreateLeaveRequestCommand command, CancellationToken ct = default)
    {
        var employeeId = command.EmployeeId > 0 ? command.EmployeeId : await ResolveEmployeeIdForUserAsync(userId, ct);
        if (!employeeId.HasValue)
            throw new InvalidOperationException("Không thể xác định hồ sơ nhân viên.");

        var employee = await _context.Employees.FindAsync(new object[] { employeeId.Value }, ct)
            ?? throw new InvalidOperationException("Hồ sơ nhân viên không tồn tại.");

        var totalDays = (decimal)(command.EndDate.Date - command.StartDate.Date).TotalDays + 1m;
        if (totalDays <= 0) totalDays = 1m;

        var request = new LeaveRequest
        {
            EmployeeId = employee.Id,
            LeaveType = command.LeaveType,
            StartDate = command.StartDate.Date,
            EndDate = command.EndDate.Date,
            TotalDays = totalDays,
            Reason = command.Reason,
            AttachmentPath = command.AttachmentPath,
            Status = AttendanceRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeaveRequests.Add(request);
        await _context.SaveChangesAsync(ct);

        var audit = new AttendanceAuditLog
        {
            RequestId = request.Id,
            Action = "REQUEST_LEAVE",
            Description = $"Gửi đơn xin nghỉ phép: {employee.FullName} từ {request.StartDate:dd/MM/yyyy} đến {request.EndDate:dd/MM/yyyy} ({totalDays} ngày)",
            ChangedByUserId = userId,
            ChangedByUserName = employee.FullName,
            ChangedAt = DateTime.UtcNow
        };
        _context.AttendanceAuditLogs.Add(audit);
        await _context.SaveChangesAsync(ct);

        if (_auditService != null)
        {
            await _auditService.LogAsync(
                AuditEventType.LeaveRequestSubmitted,
                performedBy: employee.FullName,
                targetEntity: "LeaveRequest",
                targetId: request.Id.ToString(),
                details: $"Đơn xin nghỉ phép: {employee.FullName} từ {request.StartDate:dd/MM/yyyy} đến {request.EndDate:dd/MM/yyyy} ({totalDays} ngày). Lý do: {request.Reason}",
                cancellationToken: ct);
        }

        return MapLeaveRequestDto(request);
    }

    public async Task<ShiftChangeRequestDto> SubmitShiftChangeRequestAsync(string userId, CreateShiftChangeRequestCommand command, CancellationToken ct = default)
    {
        var employeeId = command.EmployeeId > 0 ? command.EmployeeId : await ResolveEmployeeIdForUserAsync(userId, ct);
        if (!employeeId.HasValue)
            throw new InvalidOperationException("Không thể xác định hồ sơ nhân viên.");

        var employee = await _context.Employees.FindAsync(new object[] { employeeId.Value }, ct)
            ?? throw new InvalidOperationException("Hồ sơ nhân viên không tồn tại.");

        var request = new ShiftChangeRequest
        {
            EmployeeId = employee.Id,
            TargetDate = command.TargetDate.Date,
            CurrentWorkShiftId = command.CurrentWorkShiftId,
            ProposedWorkShiftId = command.ProposedWorkShiftId,
            SubstituteEmployeeId = command.SubstituteEmployeeId,
            Reason = command.Reason,
            Status = AttendanceRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.ShiftChangeRequests.Add(request);
        await _context.SaveChangesAsync(ct);

        var audit = new AttendanceAuditLog
        {
            RequestId = request.Id,
            Action = "REQUEST_SHIFT_CHANGE",
            Description = $"Gửi đơn đổi ca: {employee.FullName} ngày {request.TargetDate:dd/MM/yyyy}",
            ChangedByUserId = userId,
            ChangedByUserName = employee.FullName,
            ChangedAt = DateTime.UtcNow
        };
        _context.AttendanceAuditLogs.Add(audit);
        await _context.SaveChangesAsync(ct);

        if (_auditService != null)
        {
            await _auditService.LogAsync(
                AuditEventType.ShiftChangeSubmitted,
                performedBy: employee.FullName,
                targetEntity: "ShiftChangeRequest",
                targetId: request.Id.ToString(),
                details: $"Đơn xin đổi ca: {employee.FullName} ngày {request.TargetDate:dd/MM/yyyy}. Lý do: {request.Reason}",
                cancellationToken: ct);
        }

        return MapShiftChangeRequestDto(request);
    }

    // === 2. Manager Team Scope ===
    public async Task<List<AttendanceRecordDto>> GetTeamAttendanceAsync(string managerUserId, DateTime date, int? departmentId = null, CancellationToken ct = default)
    {
        var managerEmpId = await ResolveEmployeeIdForUserAsync(managerUserId, ct);
        var managerEmp = managerEmpId.HasValue ? await _context.Employees.FindAsync(new object[] { managerEmpId.Value }, ct) : null;

        var targetDate = date.Date;
        var managerUser = await _userManager.FindByIdAsync(managerUserId);
        bool isAdmin = managerUser != null && (string.Equals(managerUser.UserName, "admin", StringComparison.OrdinalIgnoreCase) || await _userManager.IsInRoleAsync(managerUser, "Administrator"));
        bool isHr = managerUser != null && await _userManager.IsInRoleAsync(managerUser, "HR");
        bool isManager = managerUser != null && await _userManager.IsInRoleAsync(managerUser, "Manager");

        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Where(e => e.Status == EmployeeStatus.Active || e.Code == "NV00005");

        if (isAdmin || isHr)
        {
            if (departmentId.HasValue && departmentId.Value > 0)
            {
                query = query.Where(e => e.DepartmentId == departmentId.Value);
            }
            // Admin & HR monitor all eligible employees, explicitly including HR employees
        }
        else if (isManager)
        {
            if (managerEmp != null && managerEmp.DepartmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == managerEmp.DepartmentId);
            }
            else if (departmentId.HasValue && departmentId.Value > 0)
            {
                query = query.Where(e => e.DepartmentId == departmentId.Value);
            }
        }
        else
        {
            if (managerEmpId.HasValue)
            {
                query = query.Where(e => e.Id == managerEmpId.Value);
            }
        }

        // Exclude only exempt root admins without attendance obligations
        var exemptUserEmpIds = await _context.Users
            .Where(u => u.EmployeeId.HasValue && (u.UserName == "admin" || u.Email == "admin@tradeflow.local"))
            .Select(u => u.EmployeeId!.Value)
            .ToListAsync(ct);
        if (exemptUserEmpIds.Count > 0)
        {
            query = query.Where(e => !exemptUserEmpIds.Contains(e.Id));
        }

        var teamEmployees = await query.ToListAsync(ct);
        var empIds = teamEmployees.Select(e => e.Id).ToList();

        var records = await _context.AttendanceRecords
            .Include(r => r.WorkShift)
            .Where(r => empIds.Contains(r.EmployeeId) && r.WorkDate == targetDate)
            .ToListAsync(ct);

        var leaves = await _context.LeaveRequests
            .Where(l => empIds.Contains(l.EmployeeId) &&
                        l.Status == AttendanceRequestStatus.Approved &&
                        l.StartDate <= targetDate && l.EndDate >= targetDate)
            .ToListAsync(ct);

        var result = new List<AttendanceRecordDto>();

        foreach (var emp in teamEmployees)
        {
            var rec = records.FirstOrDefault(r => r.EmployeeId == emp.Id);
            var onLeave = leaves.FirstOrDefault(l => l.EmployeeId == emp.Id);

            if (rec != null)
            {
                rec.Employee = emp;
                result.Add(MapToRecordDto(rec));
            }
            else if (onLeave != null)
            {
                result.Add(new AttendanceRecordDto
                {
                    EmployeeId = emp.Id,
                    EmployeeCode = emp.Code,
                    EmployeeName = emp.FullName,
                    AvatarPath = emp.AvatarPath,
                    DepartmentName = emp.Department?.Name ?? string.Empty,
                    PositionName = emp.Position?.Name ?? string.Empty,
                    Branch = emp.WorkingBranch ?? string.Empty,
                    WorkDate = targetDate,
                    Status = AttendanceStatus.OnLeave,
                    StatusLabel = "Nghỉ phép",
                    StatusCssClass = "tf-status-on-leave",
                    Notes = onLeave.Reason
                });
            }
            else
            {
                result.Add(new AttendanceRecordDto
                {
                    EmployeeId = emp.Id,
                    EmployeeCode = emp.Code,
                    EmployeeName = emp.FullName,
                    AvatarPath = emp.AvatarPath,
                    DepartmentName = emp.Department?.Name ?? string.Empty,
                    PositionName = emp.Position?.Name ?? string.Empty,
                    Branch = emp.WorkingBranch ?? string.Empty,
                    WorkDate = targetDate,
                    Status = AttendanceStatus.NotClockedIn,
                    StatusLabel = "Chưa vào ca",
                    StatusCssClass = "tf-status-not-in"
                });
            }
        }

        return result.OrderBy(r => r.EmployeeCode).ToList();
    }

    public async Task<AttendanceHrDashboardKpiDto> GetTeamKpisAsync(string managerUserId, DateTime date, CancellationToken ct = default)
    {
        var records = await GetTeamAttendanceAsync(managerUserId, date, null, ct);
        var total = records.Count;

        var clockedIn = records.Count(r => r.ClockInTime.HasValue);
        var notClockedIn = records.Count(r => r.Status == AttendanceStatus.NotClockedIn);
        var late = records.Count(r => r.LateMinutes > 0 || r.Status == AttendanceStatus.Late || r.Status == AttendanceStatus.LateAndEarlyLeave);
        var earlyLeave = records.Count(r => r.EarlyLeaveMinutes > 0 || r.Status == AttendanceStatus.EarlyLeave || r.Status == AttendanceStatus.LateAndEarlyLeave);
        var onLeave = records.Count(r => r.Status == AttendanceStatus.OnLeave);
        var absent = records.Count(r => r.Status == AttendanceStatus.Absent);
        var missingOut = records.Count(r => r.ClockInTime.HasValue && !r.ClockOutTime.HasValue && r.WorkDate < GetVnDate());

        return new AttendanceHrDashboardKpiDto
        {
            TotalEligibleEmployees = total,
            ClockedInCount = clockedIn,
            NotClockedInCount = notClockedIn,
            LateCount = late,
            EarlyLeaveCount = earlyLeave,
            OnLeaveCount = onLeave,
            AbsentCount = absent,
            MissingClockOutCount = missingOut,
            PendingRequestsCount = 0
        };
    }

    // === 3. HR Enterprise Oversight ===
    public async Task<AttendanceHrDashboardKpiDto> GetHrDashboardKpisAsync(DateTime date, int? departmentId = null, string? branch = null, int? shiftId = null, CancellationToken ct = default)
    {
        var list = await GetLiveAttendanceTrackingAsync(date, departmentId, branch, shiftId, null, null, ct);
        var total = list.Count;

        var clockedIn = list.Count(r => r.ClockInTime.HasValue);
        var notClockedIn = list.Count(r => r.Status == AttendanceStatus.NotClockedIn);
        var late = list.Count(r => r.LateMinutes > 0 || r.Status == AttendanceStatus.Late || r.Status == AttendanceStatus.LateAndEarlyLeave);
        var earlyLeave = list.Count(r => r.EarlyLeaveMinutes > 0 || r.Status == AttendanceStatus.EarlyLeave || r.Status == AttendanceStatus.LateAndEarlyLeave);
        var onLeave = list.Count(r => r.Status == AttendanceStatus.OnLeave);
        var absent = list.Count(r => r.Status == AttendanceStatus.Absent);
        var missingOut = list.Count(r => r.ClockInTime.HasValue && !r.ClockOutTime.HasValue && r.WorkDate < GetVnDate());

        var pendingAdjustments = await _context.AttendanceAdjustmentRequests.CountAsync(r => r.Status == AttendanceRequestStatus.Pending, ct);
        var pendingLeaves = await _context.LeaveRequests.CountAsync(r => r.Status == AttendanceRequestStatus.Pending, ct);
        var pendingShifts = await _context.ShiftChangeRequests.CountAsync(r => r.Status == AttendanceRequestStatus.Pending, ct);

        return new AttendanceHrDashboardKpiDto
        {
            TotalEligibleEmployees = total,
            ClockedInCount = clockedIn,
            NotClockedInCount = notClockedIn,
            LateCount = late,
            EarlyLeaveCount = earlyLeave,
            OnLeaveCount = onLeave,
            AbsentCount = absent,
            MissingClockOutCount = missingOut,
            PendingRequestsCount = pendingAdjustments + pendingLeaves + pendingShifts
        };
    }

    public async Task<List<AttendanceRecordDto>> GetLiveAttendanceTrackingAsync(
        DateTime date,
        int? departmentId = null,
        string? branch = null,
        int? shiftId = null,
        string? search = null,
        string? statusFilter = null,
        CancellationToken ct = default)
    {
        var targetDate = date.Date;

        var empQuery = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Where(e => e.Status == EmployeeStatus.Active);

        if (departmentId.HasValue && departmentId.Value > 0)
            empQuery = empQuery.Where(e => e.DepartmentId == departmentId.Value);

        if (!string.IsNullOrWhiteSpace(branch))
            empQuery = empQuery.Where(e => e.WorkingBranch == branch);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            empQuery = empQuery.Where(e => e.FullName.ToLower().Contains(s) || e.Code.ToLower().Contains(s) || (e.Email != null && e.Email.ToLower().Contains(s)));
        }

        var activeEmployees = await empQuery.ToListAsync(ct);

        // Security & HR Boundary: Exclude Owner/Admin & exempt accounts from HR rosters
        var adminEmpIds = await _context.Users
            .Where(u => u.EmployeeId.HasValue && (u.UserName == "admin" || u.Email == "admin@tradeflow.local" || u.Email == "admin@tradeflow.vn"))
            .Select(u => u.EmployeeId!.Value)
            .ToListAsync(ct);
        if (adminEmpIds.Count > 0)
        {
            activeEmployees = activeEmployees.Where(e => !adminEmpIds.Contains(e.Id)).ToList();
        }

        var empIds = activeEmployees.Select(e => e.Id).ToList();

        var records = await _context.AttendanceRecords
            .Include(r => r.WorkShift)
            .Where(r => empIds.Contains(r.EmployeeId) && r.WorkDate == targetDate)
            .ToListAsync(ct);

        var leaves = await _context.LeaveRequests
            .Where(l => empIds.Contains(l.EmployeeId) &&
                        l.Status == AttendanceRequestStatus.Approved &&
                        l.StartDate <= targetDate && l.EndDate >= targetDate)
            .ToListAsync(ct);

        var result = new List<AttendanceRecordDto>();

        foreach (var emp in activeEmployees)
        {
            var rec = records.FirstOrDefault(r => r.EmployeeId == emp.Id);
            var onLeave = leaves.FirstOrDefault(l => l.EmployeeId == emp.Id);

            if (rec != null)
            {
                rec.Employee = emp;
                result.Add(MapToRecordDto(rec));
            }
            else if (onLeave != null)
            {
                result.Add(new AttendanceRecordDto
                {
                    EmployeeId = emp.Id,
                    EmployeeCode = emp.Code,
                    EmployeeName = emp.FullName,
                    AvatarPath = emp.AvatarPath,
                    DepartmentName = emp.Department?.Name ?? string.Empty,
                    PositionName = emp.Position?.Name ?? string.Empty,
                    Branch = emp.WorkingBranch ?? string.Empty,
                    WorkDate = targetDate,
                    Status = AttendanceStatus.OnLeave,
                    StatusLabel = "Nghỉ phép",
                    StatusCssClass = "tf-status-on-leave",
                    Notes = onLeave.Reason
                });
            }
            else
            {
                result.Add(new AttendanceRecordDto
                {
                    EmployeeId = emp.Id,
                    EmployeeCode = emp.Code,
                    EmployeeName = emp.FullName,
                    AvatarPath = emp.AvatarPath,
                    DepartmentName = emp.Department?.Name ?? string.Empty,
                    PositionName = emp.Position?.Name ?? string.Empty,
                    Branch = emp.WorkingBranch ?? string.Empty,
                    WorkDate = targetDate,
                    Status = AttendanceStatus.NotClockedIn,
                    StatusLabel = "Chưa vào ca",
                    StatusCssClass = "tf-status-not-in"
                });
            }
        }

        if (shiftId.HasValue && shiftId.Value > 0)
        {
            result = result.Where(r => r.WorkShiftId == shiftId.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            if (Enum.TryParse<AttendanceStatus>(statusFilter, true, out var parsedStatus))
            {
                result = result.Where(r => r.Status == parsedStatus).ToList();
            }
        }

        return result.OrderBy(r => r.EmployeeCode).ToList();
    }

    public async Task<List<AttendanceRecordDto>> GetExceptionsListAsync(DateTime date, int? departmentId = null, CancellationToken ct = default)
    {
        var all = await GetLiveAttendanceTrackingAsync(date, departmentId, null, null, null, null, ct);
        return all.Where(r => r.Status == AttendanceStatus.Late ||
                              r.Status == AttendanceStatus.EarlyLeave ||
                              r.Status == AttendanceStatus.LateAndEarlyLeave ||
                              r.Status == AttendanceStatus.MissingClockOut ||
                              r.Status == AttendanceStatus.NotClockedIn ||
                              r.Status == AttendanceStatus.Absent)
                  .ToList();
    }

    // === 4. Phê duyệt yêu cầu ===
    public async Task<List<AttendanceAdjustmentRequestDto>> GetAllAdjustmentRequestsAsync(string? statusFilter = null, int? departmentId = null, CancellationToken ct = default)
    {
        var query = _context.AttendanceAdjustmentRequests
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<AttendanceRequestStatus>(statusFilter, true, out var status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            query = query.Where(r => r.Employee.DepartmentId == departmentId.Value);
        }

        var items = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
        return items.Select(MapAdjustmentRequestDto).ToList();
    }

    public async Task<List<LeaveRequestDto>> GetAllLeaveRequestsAsync(string? statusFilter = null, int? departmentId = null, CancellationToken ct = default)
    {
        var query = _context.LeaveRequests
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<AttendanceRequestStatus>(statusFilter, true, out var status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            query = query.Where(r => r.Employee.DepartmentId == departmentId.Value);
        }

        var items = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
        return items.Select(MapLeaveRequestDto).ToList();
    }

    public async Task<List<ShiftChangeRequestDto>> GetAllShiftChangeRequestsAsync(string? statusFilter = null, int? departmentId = null, CancellationToken ct = default)
    {
        var query = _context.ShiftChangeRequests
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .Include(r => r.CurrentWorkShift)
            .Include(r => r.ProposedWorkShift)
            .Include(r => r.SubstituteEmployee)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<AttendanceRequestStatus>(statusFilter, true, out var status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            query = query.Where(r => r.Employee.DepartmentId == departmentId.Value);
        }

        var items = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
        return items.Select(MapShiftChangeRequestDto).ToList();
    }

    private async Task<string> ResolveReviewerDisplayNameAsync(ApplicationUser? user)
    {
        if (user == null) return "Owner / Admin";
        if (string.Equals(user.UserName, "admin", StringComparison.OrdinalIgnoreCase) ||
            await _userManager.IsInRoleAsync(user, "Administrator"))
        {
            return "Owner / Admin";
        }
        return !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.UserName;
    }

    public async Task<bool> ReviewAdjustmentRequestAsync(string reviewerUserId, ReviewRequestCommand command, CancellationToken ct = default)
    {
        var request = await _context.AttendanceAdjustmentRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);

        if (request == null) return false;

        var reviewerEmpId = await ResolveEmployeeIdForUserAsync(reviewerUserId, ct);
        if ((reviewerEmpId.HasValue && reviewerEmpId.Value == request.EmployeeId) ||
            (!string.IsNullOrEmpty(request.Employee?.UserId) && request.Employee.UserId == reviewerUserId))
        {
            throw new InvalidOperationException("Bạn không thể tự phê duyệt đơn giải trình của chính mình.");
        }

        var reviewer = await _userManager.FindByIdAsync(reviewerUserId);
        var reviewerDisplayName = await ResolveReviewerDisplayNameAsync(reviewer);

        request.Status = command.IsApproved ? AttendanceRequestStatus.Approved : AttendanceRequestStatus.Rejected;
        request.ReviewedByUserId = reviewerUserId;
        request.ReviewedByUserName = reviewerDisplayName;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNotes = command.ReviewNotes;

        if (command.IsApproved)
        {
            // Apply changes to AttendanceRecord
            var record = request.AttendanceRecordId.HasValue
                ? await _context.AttendanceRecords.FindAsync(new object[] { request.AttendanceRecordId.Value }, ct)
                : await _context.AttendanceRecords.FirstOrDefaultAsync(r => r.EmployeeId == request.EmployeeId && r.WorkDate == request.TargetDate.Date, ct);

            if (record == null)
            {
                record = new AttendanceRecord
                {
                    EmployeeId = request.EmployeeId,
                    WorkDate = request.TargetDate.Date
                };
                _context.AttendanceRecords.Add(record);
            }

            if (request.ProposedClockIn.HasValue)
                record.ClockInTime = request.ProposedClockIn.Value;
            if (request.ProposedClockOut.HasValue)
                record.ClockOutTime = request.ProposedClockOut.Value;

            record.IsManuallyAdjusted = true;
            record.Status = AttendanceStatus.Adjusted;
            record.Notes = string.IsNullOrWhiteSpace(record.Notes)
                ? $"Điều chỉnh công: {request.Reason}"
                : $"{record.Notes} | Điều chỉnh: {request.Reason}";

            if (record.ClockInTime.HasValue && record.ClockOutTime.HasValue)
            {
                var dur = (int)(record.ClockOutTime.Value - record.ClockInTime.Value).TotalMinutes;
                record.WorkingMinutes = Math.Max(0, dur - 60);
            }
        }

        await _context.SaveChangesAsync(ct);

        var audit = new AttendanceAuditLog
        {
            RequestId = request.Id,
            Action = command.IsApproved ? "APPROVE_ADJUSTMENT" : "REJECT_ADJUSTMENT",
            Description = $"{(command.IsApproved ? "Phê duyệt" : "Từ chối")} yêu cầu điều chỉnh công: {request.Employee.FullName} ngày {request.TargetDate:dd/MM/yyyy}. Ghi chú: {command.ReviewNotes}",
            ChangedByUserId = reviewerUserId,
            ChangedByUserName = reviewerDisplayName,
            ChangedAt = DateTime.UtcNow
        };
        _context.AttendanceAuditLogs.Add(audit);
        await _context.SaveChangesAsync(ct);

        if (_auditService != null)
        {
            await _auditService.LogAsync(
                command.IsApproved ? AuditEventType.AttendanceAdjustmentApproved : AuditEventType.AttendanceAdjustmentRejected,
                performedBy: reviewerDisplayName,
                targetEntity: "AttendanceAdjustmentRequest",
                targetId: request.Id.ToString(),
                details: $"{(command.IsApproved ? "Phê duyệt" : "Từ chối")} điều chỉnh công của {request.Employee.FullName} ({request.TargetDate:dd/MM/yyyy}). Ghi chú: {command.ReviewNotes}",
                cancellationToken: ct);
        }

        return true;
    }

    

    public async Task<bool> ReviewLeaveRequestAsync(string reviewerUserId, ReviewRequestCommand command, CancellationToken ct = default)
    {
        var request = await _context.LeaveRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);

        if (request == null) return false;

        var reviewerEmpId = await ResolveEmployeeIdForUserAsync(reviewerUserId, ct);
        if ((reviewerEmpId.HasValue && reviewerEmpId.Value == request.EmployeeId) ||
            (!string.IsNullOrEmpty(request.Employee?.UserId) && request.Employee.UserId == reviewerUserId))
        {
            throw new InvalidOperationException("Bạn không thể tự phê duyệt đơn nghỉ phép của chính mình.");
        }

        var reviewer = await _userManager.FindByIdAsync(reviewerUserId);
        var reviewerDisplayName = await ResolveReviewerDisplayNameAsync(reviewer);

        request.Status = command.IsApproved ? AttendanceRequestStatus.Approved : AttendanceRequestStatus.Rejected;
        request.ReviewedByUserId = reviewerUserId;
        request.ReviewedByUserName = reviewerDisplayName;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNotes = command.ReviewNotes;

        if (command.IsApproved)
        {
            // For each day in range, create/update record with status OnLeave
            var curDate = request.StartDate.Date;
            var endDate = request.EndDate.Date;

            while (curDate <= endDate)
            {
                var rec = await _context.AttendanceRecords
                    .FirstOrDefaultAsync(r => r.EmployeeId == request.EmployeeId && r.WorkDate == curDate, ct);

                if (rec == null)
                {
                    rec = new AttendanceRecord
                    {
                        EmployeeId = request.EmployeeId,
                        WorkDate = curDate,
                        Status = AttendanceStatus.OnLeave,
                        Notes = $"Nghỉ phép: {request.Reason}"
                    };
                    _context.AttendanceRecords.Add(rec);
                }
                else
                {
                    rec.Status = AttendanceStatus.OnLeave;
                    rec.Notes = $"Nghỉ phép: {request.Reason}";
                }

                curDate = curDate.AddDays(1);
            }

            // If annual leave, record deduction in LeaveBalanceLedger
            if (request.LeaveType == LeaveType.AnnualLeave)
            {
                var year = request.StartDate.Year;
                var currentSummary = await GetLeaveBalanceSummaryAsync(request.EmployeeId, year, ct);
                var balanceAfter = Math.Max(0, currentSummary.RemainingDays - request.TotalDays);

                var ledger = new LeaveBalanceLedger
                {
                    EmployeeId = request.EmployeeId,
                    Year = year,
                    TransactionType = LeaveLedgerTransactionType.LeaveUsageDeduction,
                    DaysChanged = -request.TotalDays,
                    BalanceAfter = balanceAfter,
                    Reason = $"Khấu trừ nghỉ phép năm ({request.TotalDays} ngày) từ {request.StartDate:dd/MM/yyyy} đến {request.EndDate:dd/MM/yyyy}: {request.Reason}",
                    LeaveRequestId = request.Id,
                    PerformedByUserId = reviewerUserId,
                    PerformedByUserName = reviewerDisplayName,
                    CreatedAt = DateTime.UtcNow
                };
                _context.LeaveBalanceLedgers.Add(ledger);
            }
        }

        await _context.SaveChangesAsync(ct);

        var audit = new AttendanceAuditLog
        {
            RequestId = request.Id,
            Action = command.IsApproved ? "APPROVE_LEAVE" : "REJECT_LEAVE",
            Description = $"{(command.IsApproved ? "Phê duyệt" : "Từ chối")} đơn nghỉ phép: {request.Employee.FullName} ({request.TotalDays} ngày). Ghi chú: {command.ReviewNotes}",
            ChangedByUserId = reviewerUserId,
            ChangedByUserName = reviewerDisplayName,
            ChangedAt = DateTime.UtcNow
        };
        _context.AttendanceAuditLogs.Add(audit);
        await _context.SaveChangesAsync(ct);

        if (_auditService != null)
        {
            await _auditService.LogAsync(
                command.IsApproved ? AuditEventType.LeaveRequestApproved : AuditEventType.LeaveRequestRejected,
                performedBy: reviewerDisplayName,
                targetEntity: "LeaveRequest",
                targetId: request.Id.ToString(),
                details: $"{(command.IsApproved ? "Phê duyệt" : "Từ chối")} đơn nghỉ phép của {request.Employee.FullName} ({request.StartDate:dd/MM/yyyy} - {request.EndDate:dd/MM/yyyy}). Ghi chú: {command.ReviewNotes}",
                cancellationToken: ct);
        }

        return true;
    }

    

    public async Task<bool> ReviewShiftChangeRequestAsync(string reviewerUserId, ReviewRequestCommand command, CancellationToken ct = default)
    {
        var request = await _context.ShiftChangeRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct);

        if (request == null) return false;

        var reviewerEmpId = await ResolveEmployeeIdForUserAsync(reviewerUserId, ct);
        if ((reviewerEmpId.HasValue && reviewerEmpId.Value == request.EmployeeId) ||
            (!string.IsNullOrEmpty(request.Employee?.UserId) && request.Employee.UserId == reviewerUserId))
        {
            throw new InvalidOperationException("Bạn không thể tự phê duyệt đơn đổi ca của chính mình.");
        }

        var reviewer = await _userManager.FindByIdAsync(reviewerUserId);
        var reviewerDisplayName = await ResolveReviewerDisplayNameAsync(reviewer);

        request.Status = command.IsApproved ? AttendanceRequestStatus.Approved : AttendanceRequestStatus.Rejected;
        request.ReviewedByUserId = reviewerUserId;
        request.ReviewedByUserName = reviewerDisplayName;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNotes = command.ReviewNotes;

        if (command.IsApproved)
        {
            // Assign proposed shift for the specific date
            var assignment = new EmployeeShiftAssignment
            {
                EmployeeId = request.EmployeeId,
                WorkShiftId = request.ProposedWorkShiftId,
                SpecificDate = request.TargetDate.Date,
                EffectiveFrom = request.TargetDate.Date,
                EffectiveTo = request.TargetDate.Date,
                Notes = $"Đổi ca phê duyệt từ đơn #{request.Id}"
            };
            _context.EmployeeShiftAssignments.Add(assignment);
        }

        await _context.SaveChangesAsync(ct);

        var audit = new AttendanceAuditLog
        {
            RequestId = request.Id,
            Action = command.IsApproved ? "APPROVE_SHIFT_CHANGE" : "REJECT_SHIFT_CHANGE",
            Description = $"{(command.IsApproved ? "Phê duyệt" : "Từ chối")} đơn đổi ca: {request.Employee.FullName} ngày {request.TargetDate:dd/MM/yyyy}",
            ChangedByUserId = reviewerUserId,
            ChangedByUserName = reviewerDisplayName,
            ChangedAt = DateTime.UtcNow
        };
        _context.AttendanceAuditLogs.Add(audit);
        await _context.SaveChangesAsync(ct);

        if (_auditService != null)
        {
            await _auditService.LogAsync(
                command.IsApproved ? AuditEventType.ShiftChangeApproved : AuditEventType.ShiftChangeRejected,
                performedBy: reviewerDisplayName,
                targetEntity: "ShiftChangeRequest",
                targetId: request.Id.ToString(),
                details: $"{(command.IsApproved ? "Phê duyệt" : "Từ chối")} đơn đổi ca của {request.Employee.FullName} (ngày {request.TargetDate:dd/MM/yyyy}). Ghi chú: {command.ReviewNotes}",
                cancellationToken: ct);
        }

        return true;
    }

    

    // === Approval Undo, Leave Restoration & Employee Monitoring ===

    public async Task<UndoEligibilityDto> CheckUndoEligibilityAsync(string requestType, int requestId, string currentUserId, CancellationToken ct = default)
    {
        var result = new UndoEligibilityDto { CanUndo = false };
        var currentUser = await _userManager.FindByIdAsync(currentUserId);
        if (currentUser == null)
        {
            result.ReasonIfNotEligible = "Không tìm thấy người dùng hiện tại.";
            return result;
        }

        var isHighRole = string.Equals(currentUser.UserName, "admin", StringComparison.OrdinalIgnoreCase) ||
                         await _userManager.IsInRoleAsync(currentUser, "Administrator") ||
                         await _userManager.IsInRoleAsync(currentUser, "HR") ||
                         await _userManager.IsInRoleAsync(currentUser, "Manager");

        if (!isHighRole)
        {
            result.ReasonIfNotEligible = "Nhân viên không được tự hoàn tác quyết định của người duyệt.";
            return result;
        }

        var nowUtc = DateTime.UtcNow;
        var nowVn = nowUtc.AddHours(7);

        if (string.Equals(requestType, "leave", StringComparison.OrdinalIgnoreCase))
        {
            var req = await _context.LeaveRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == requestId, ct);

            if (req == null)
            {
                result.ReasonIfNotEligible = "Không tìm thấy đơn xin nghỉ phép.";
                return result;
            }

            if (req.Status != AttendanceRequestStatus.Approved && req.Status != AttendanceRequestStatus.Rejected)
            {
                result.ReasonIfNotEligible = "Chỉ có thể hoàn tác đơn đã phê duyệt hoặc đã từ chối.";
                return result;
            }

            var empId = await ResolveEmployeeIdForUserAsync(currentUserId, ct);
            if (empId.HasValue && empId.Value == req.EmployeeId)
            {
                result.ReasonIfNotEligible = "Bạn không thể tự hoàn tác quyết định trên đơn của chính mình.";
                return result;
            }

            var reviewedAt = req.ReviewedAt ?? req.CreatedAt;
            var isAdvanceNotice = (req.StartDate.Date - req.CreatedAt.Date).TotalDays >= 3;
            var maxHours = isAdvanceNotice ? 72 : 24;
            var deadlineUtc = reviewedAt.AddHours(maxHours);

            // Cannot undo after the leave has started!
            var leaveStartUtc = req.StartDate.Date.AddHours(-7); // Midnight VN time in UTC
            if (leaveStartUtc < deadlineUtc)
            {
                deadlineUtc = leaveStartUtc;
            }

            result.DeadlineVn = deadlineUtc.AddHours(7);
            result.PriorDecisionLabel = req.Status == AttendanceRequestStatus.Approved ? "Đã chấp thuận" : "Đã từ chối";
            result.ReviewerName = req.ReviewedByUserName ?? "Người duyệt";
            result.ReviewedAtVn = reviewedAt.AddHours(7);

            if (nowUtc >= deadlineUtc)
            {
                if (nowUtc >= leaveStartUtc)
                {
                    result.ReasonIfNotEligible = $"Kỳ nghỉ phép đã bắt đầu từ ngày {req.StartDate:dd/MM/yyyy}. Quyền hoàn tác quyết định đã kết thúc khi kỳ nghỉ bắt đầu.";
                }
                else
                {
                    result.ReasonIfNotEligible = $"Đã quá thời hạn hoàn tác tối đa ({maxHours} giờ) kể từ khi quyết định được thực hiện.";
                }
                return result;
            }

            var remaining = deadlineUtc - nowUtc;
            result.CanUndo = true;
            result.RemainingTime = remaining;
            result.RemainingTimeDisplay = $"{(int)remaining.TotalHours}h {remaining.Minutes:D2}m";
            return result;
        }
        else if (string.Equals(requestType, "adjustment", StringComparison.OrdinalIgnoreCase))
        {
            var req = await _context.AttendanceAdjustmentRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == requestId, ct);

            if (req == null)
            {
                result.ReasonIfNotEligible = "Không tìm thấy yêu cầu điều chỉnh công.";
                return result;
            }

            if (req.Status != AttendanceRequestStatus.Approved && req.Status != AttendanceRequestStatus.Rejected)
            {
                result.ReasonIfNotEligible = "Chỉ có thể hoàn tác đơn đã phê duyệt hoặc đã từ chối.";
                return result;
            }

            var empId = await ResolveEmployeeIdForUserAsync(currentUserId, ct);
            if (empId.HasValue && empId.Value == req.EmployeeId)
            {
                result.ReasonIfNotEligible = "Bạn không thể tự hoàn tác quyết định trên đơn của chính mình.";
                return result;
            }

            var reviewedAt = req.ReviewedAt ?? req.CreatedAt;
            var deadlineUtc = reviewedAt.AddHours(24);
            result.DeadlineVn = deadlineUtc.AddHours(7);
            result.PriorDecisionLabel = req.Status == AttendanceRequestStatus.Approved ? "Đã chấp thuận" : "Đã từ chối";
            result.ReviewerName = req.ReviewedByUserName ?? "Người duyệt";
            result.ReviewedAtVn = reviewedAt.AddHours(7);

            if (nowUtc >= deadlineUtc)
            {
                result.ReasonIfNotEligible = "Đã quá thời hạn hoàn tác 24 giờ kể từ thời điểm quyết định.";
                return result;
            }

            var remaining = deadlineUtc - nowUtc;
            result.CanUndo = true;
            result.RemainingTime = remaining;
            result.RemainingTimeDisplay = $"{(int)remaining.TotalHours}h {remaining.Minutes:D2}m";
            return result;
        }
        else if (string.Equals(requestType, "shift", StringComparison.OrdinalIgnoreCase))
        {
            var req = await _context.ShiftChangeRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == requestId, ct);

            if (req == null)
            {
                result.ReasonIfNotEligible = "Không tìm thấy yêu cầu đổi ca.";
                return result;
            }

            if (req.Status != AttendanceRequestStatus.Approved && req.Status != AttendanceRequestStatus.Rejected)
            {
                result.ReasonIfNotEligible = "Chỉ có thể hoàn tác đơn đã phê duyệt hoặc đã từ chối.";
                return result;
            }

            var empId = await ResolveEmployeeIdForUserAsync(currentUserId, ct);
            if (empId.HasValue && empId.Value == req.EmployeeId)
            {
                result.ReasonIfNotEligible = "Bạn không thể tự hoàn tác quyết định trên đơn của chính mình.";
                return result;
            }

            var reviewedAt = req.ReviewedAt ?? req.CreatedAt;
            var deadlineUtc = reviewedAt.AddHours(24);
            result.DeadlineVn = deadlineUtc.AddHours(7);
            result.PriorDecisionLabel = req.Status == AttendanceRequestStatus.Approved ? "Đã chấp thuận" : "Đã từ chối";
            result.ReviewerName = req.ReviewedByUserName ?? "Người duyệt";
            result.ReviewedAtVn = reviewedAt.AddHours(7);

            if (nowUtc >= deadlineUtc)
            {
                result.ReasonIfNotEligible = "Đã quá thời hạn hoàn tác 24 giờ kể từ thời điểm quyết định.";
                return result;
            }

            var remaining = deadlineUtc - nowUtc;
            result.CanUndo = true;
            result.RemainingTime = remaining;
            result.RemainingTimeDisplay = $"{(int)remaining.TotalHours}h {remaining.Minutes:D2}m";
            return result;
        }

        result.ReasonIfNotEligible = "Loại yêu cầu không hợp lệ.";
        return result;
    }

    public async Task<bool> UndoReviewRequestAsync(string userId, UndoReviewCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new InvalidOperationException("Vui lòng nhập lý do hoàn tác quyết định.");

        var eligibility = await CheckUndoEligibilityAsync(command.RequestType, command.RequestId, userId, ct);
        if (!eligibility.CanUndo)
        {
            throw new InvalidOperationException(eligibility.ReasonIfNotEligible);
        }

        var actor = await _userManager.FindByIdAsync(userId);
        var actorDisplayName = await ResolveReviewerDisplayNameAsync(actor);
        var nowUtc = DateTime.UtcNow;

        if (string.Equals(command.RequestType, "leave", StringComparison.OrdinalIgnoreCase))
        {
            var req = await _context.LeaveRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct)
                ?? throw new InvalidOperationException("Không tìm thấy đơn xin nghỉ phép.");

            var priorStatus = req.Status;

            // Revert leave balance deduction if was approved
            if (priorStatus == AttendanceRequestStatus.Approved && req.LeaveType == LeaveType.AnnualLeave)
            {
                var year = req.StartDate.Year;
                var currentSummary = await GetLeaveBalanceSummaryAsync(req.EmployeeId, year, ct);
                var balanceAfter = currentSummary.RemainingDays + req.TotalDays;

                var ledger = new LeaveBalanceLedger
                {
                    EmployeeId = req.EmployeeId,
                    Year = year,
                    TransactionType = LeaveLedgerTransactionType.LeaveCancellationReversal,
                    DaysChanged = req.TotalDays,
                    BalanceAfter = balanceAfter,
                    Reason = $"Hoàn trả {req.TotalDays} ngày phép do hoàn tác quyết định duyệt đơn: {command.Reason}",
                    LeaveRequestId = req.Id,
                    PerformedByUserId = userId,
                    PerformedByUserName = actorDisplayName,
                    Notes = $"Hoàn tác quyết định duyệt đơn phép #{req.Id}",
                    CreatedAt = nowUtc
                };
                _context.LeaveBalanceLedgers.Add(ledger);

                // Reset attendance records marked OnLeave for this range
                var curDate = req.StartDate.Date;
                var endDate = req.EndDate.Date;
                while (curDate <= endDate)
                {
                    var rec = await _context.AttendanceRecords
                        .FirstOrDefaultAsync(r => r.EmployeeId == req.EmployeeId && r.WorkDate == curDate, ct);
                    if (rec != null && rec.Status == AttendanceStatus.OnLeave)
                    {
                        rec.Status = AttendanceStatus.NotClockedIn;
                        rec.Notes = $"Đã hoàn tác quyết định nghỉ phép: {command.Reason}";
                    }
                    curDate = curDate.AddDays(1);
                }
            }

            req.Status = AttendanceRequestStatus.Pending;
            req.ReviewNotes = $"[Hoàn tác {nowUtc.AddHours(7):dd/MM/yyyy HH:mm} bởi {actorDisplayName}: {command.Reason}] " + (req.ReviewNotes ?? "");
            req.ReviewedAt = null;
            req.ReviewedByUserId = null;
            req.ReviewedByUserName = null;

            await _context.SaveChangesAsync(ct);

            var dateRangeStr = req.StartDate.Date == req.EndDate.Date ? req.StartDate.ToString("dd/MM/yyyy") : $"{req.StartDate:dd/MM/yyyy} - {req.EndDate:dd/MM/yyyy}";
            var audit = new AttendanceAuditLog
            {
                RequestId = req.Id,
                Action = "UNDO_LEAVE_DECISION",
                Description = $"Hoàn tác quyết định đơn nghỉ phép của {req.Employee.FullName} ({dateRangeStr}). Đưa về 'Chờ duyệt'. Lý do: {command.Reason}",
                ChangedByUserId = userId,
                ChangedByUserName = actorDisplayName,
                ChangedAt = nowUtc
            };
            _context.AttendanceAuditLogs.Add(audit);
            await _context.SaveChangesAsync(ct);

            if (_auditService != null)
            {
                await _auditService.LogAsync(
                    AuditEventType.LeaveRequestSubmitted,
                    performedBy: actorDisplayName,
                    targetEntity: "LeaveRequest",
                    targetId: req.Id.ToString(),
                    details: $"Hoàn tác quyết định đơn nghỉ phép {req.Employee.FullName}. Đưa về Chờ duyệt. Lý do: {command.Reason}",
                    cancellationToken: ct);
            }
            return true;
        }
        else if (string.Equals(command.RequestType, "adjustment", StringComparison.OrdinalIgnoreCase))
        {
            var req = await _context.AttendanceAdjustmentRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct)
                ?? throw new InvalidOperationException("Không tìm thấy đơn giải trình.");

            var priorStatus = req.Status;

            // Revert record adjustment if prior was approved
            if (priorStatus == AttendanceRequestStatus.Approved)
            {
                var record = req.AttendanceRecordId.HasValue
                    ? await _context.AttendanceRecords.FindAsync(new object[] { req.AttendanceRecordId.Value }, ct)
                    : await _context.AttendanceRecords.FirstOrDefaultAsync(r => r.EmployeeId == req.EmployeeId && r.WorkDate == req.TargetDate.Date, ct);

                if (record != null)
                {
                    record.IsManuallyAdjusted = false;
                    record.Status = AttendanceStatus.PendingAdjustment;
                    record.Notes = $"Đã hoàn tác quyết định duyệt giải trình: {command.Reason}";
                }
            }

            req.Status = AttendanceRequestStatus.Pending;
            req.ReviewNotes = $"[Hoàn tác {nowUtc.AddHours(7):dd/MM/yyyy HH:mm} bởi {actorDisplayName}: {command.Reason}] " + (req.ReviewNotes ?? "");
            req.ReviewedAt = null;
            req.ReviewedByUserId = null;
            req.ReviewedByUserName = null;

            await _context.SaveChangesAsync(ct);

            var audit = new AttendanceAuditLog
            {
                RequestId = req.Id,
                Action = "UNDO_ADJUSTMENT_DECISION",
                Description = $"Hoàn tác quyết định giải trình công: {req.Employee.FullName} ({req.TargetDate:dd/MM/yyyy}). Đưa về 'Chờ duyệt'. Lý do: {command.Reason}",
                ChangedByUserId = userId,
                ChangedByUserName = actorDisplayName,
                ChangedAt = nowUtc
            };
            _context.AttendanceAuditLogs.Add(audit);
            await _context.SaveChangesAsync(ct);

            return true;
        }
        else if (string.Equals(command.RequestType, "shift", StringComparison.OrdinalIgnoreCase))
        {
            var req = await _context.ShiftChangeRequests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == command.RequestId, ct)
                ?? throw new InvalidOperationException("Không tìm thấy đơn đổi ca.");

            var priorStatus = req.Status;
            if (priorStatus == AttendanceRequestStatus.Approved)
            {
                var assignment = await _context.EmployeeShiftAssignments
                    .FirstOrDefaultAsync(a => a.EmployeeId == req.EmployeeId && a.SpecificDate == req.TargetDate.Date, ct);
                if (assignment != null)
                {
                    _context.EmployeeShiftAssignments.Remove(assignment);
                }
            }

            req.Status = AttendanceRequestStatus.Pending;
            req.ReviewNotes = $"[Hoàn tác {nowUtc.AddHours(7):dd/MM/yyyy HH:mm} bởi {actorDisplayName}: {command.Reason}] " + (req.ReviewNotes ?? "");
            req.ReviewedAt = null;
            req.ReviewedByUserId = null;
            req.ReviewedByUserName = null;

            await _context.SaveChangesAsync(ct);

            var audit = new AttendanceAuditLog
            {
                RequestId = req.Id,
                Action = "UNDO_SHIFT_DECISION",
                Description = $"Hoàn tác quyết định đổi ca: {req.Employee.FullName} ({req.TargetDate:dd/MM/yyyy}). Đưa về 'Chờ duyệt'. Lý do: {command.Reason}",
                ChangedByUserId = userId,
                ChangedByUserName = actorDisplayName,
                ChangedAt = nowUtc
            };
            _context.AttendanceAuditLogs.Add(audit);
            await _context.SaveChangesAsync(ct);

            return true;
        }

        return false;
    }

    public async Task<bool> RestoreLeaveBalanceAsync(string adminUserId, RestoreLeaveCommand command, CancellationToken ct = default)
    {
        if (command.EmployeeId <= 0 || command.Days == 0 || string.IsNullOrWhiteSpace(command.Reason))
            throw new InvalidOperationException("Dữ liệu điều chỉnh không hợp lệ: Cần chỉ định nhân viên, số ngày khác 0 và lý do rõ ràng.");

        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        var isAdmin = adminUser != null && (string.Equals(adminUser.UserName, "admin", StringComparison.OrdinalIgnoreCase) || await _userManager.IsInRoleAsync(adminUser, "Administrator"));
        if (!isAdmin)
        {
            throw new UnauthorizedAccessException("Chỉ có tài khoản Quản trị viên cấp cao (Owner / Administrator) mới có quyền thực hiện chức năng đặc quyền này.");
        }

        // Anti-double refund check:
        if (command.RelatedLeaveRequestId.HasValue && command.RelatedLeaveRequestId.Value > 0)
        {
            var alreadyReversed = await _context.LeaveBalanceLedgers.AnyAsync(
                l => l.LeaveRequestId == command.RelatedLeaveRequestId.Value &&
                     (l.TransactionType == LeaveLedgerTransactionType.LeaveCancellationReversal ||
                      l.TransactionType == LeaveLedgerTransactionType.AdminGrant),
                ct);

            if (alreadyReversed)
            {
                throw new InvalidOperationException(
                    "Đơn nghỉ phép này đã được hoàn trả ngày phép trước đó (qua hoàn tác phê duyệt hoặc điều chỉnh). " +
                    "Để chống cộng trùng, không thể khôi phục tự động lần 2 cho cùng một đơn. " +
                    "Nếu muốn điều chỉnh số dư bổ sung độc lập, vui lòng bỏ chọn đơn liên quan.");
            }
        }

        var emp = await _context.Employees.FindAsync(new object[] { command.EmployeeId }, ct)
            ?? throw new InvalidOperationException("Không tìm thấy nhân viên.");

        var year = GetVnTime().Year;
        var summary = await GetLeaveBalanceSummaryAsync(command.EmployeeId, year, ct);
        var balanceBefore = summary.RemainingDays;
        var balanceAfter = Math.Max(0, balanceBefore + command.Days);
        var adminDisplayName = await ResolveReviewerDisplayNameAsync(adminUser);
        var nowUtc = DateTime.UtcNow;

        var ledger = new LeaveBalanceLedger
        {
            EmployeeId = command.EmployeeId,
            Year = year,
            TransactionType = LeaveLedgerTransactionType.AdminGrant,
            DaysChanged = command.Days,
            BalanceAfter = balanceAfter,
            Reason = command.Reason,
            LeaveRequestId = command.RelatedLeaveRequestId,
            PerformedByUserId = adminUserId,
            PerformedByUserName = adminDisplayName,
            Notes = command.Notes ?? $"Admin điều chỉnh số dư: Trước {balanceBefore} -> Sau {balanceAfter} ngày",
            CreatedAt = nowUtc
        };
        _context.LeaveBalanceLedgers.Add(ledger);

        var audit = new AttendanceAuditLog
        {
            Action = "ADMIN_RESTORE_LEAVE",
            Description = $"[Admin khôi phục ngày phép] {adminDisplayName} điều chỉnh {(command.Days > 0 ? $"+{command.Days}" : $"{command.Days}")} ngày cho {emp.FullName} ({emp.Code}). Số dư: {balanceBefore} -> {balanceAfter}. Lý do: {command.Reason}" +
                          (command.RelatedLeaveRequestId.HasValue ? $" (Đơn liên quan #{command.RelatedLeaveRequestId.Value})" : ""),
            ChangedByUserId = adminUserId,
            ChangedByUserName = adminDisplayName,
            ChangedAt = nowUtc
        };
        _context.AttendanceAuditLogs.Add(audit);

        await _context.SaveChangesAsync(ct);

        if (_auditService != null)
        {
            await _auditService.LogAsync(
                AuditEventType.LeaveBalanceAdjusted,
                performedBy: adminDisplayName,
                targetEntity: "LeaveBalanceLedger",
                targetId: ledger.Id.ToString(),
                details: $"Admin khôi phục {command.Days} ngày phép cho {emp.FullName}. Số dư: {balanceBefore} -> {balanceAfter}. Lý do: {command.Reason}",
                cancellationToken: ct);
        }

        return true;
    }

    public async Task<EmployeeMonitoringDetailDto> GetEmployeeMonitoringDetailAsync(int employeeId, int month, int year, CancellationToken ct = default)
    {
        var emp = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy nhân viên.");

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var startDate = UtcDate(year, month, 1);
        var endDate = UtcDate(year, month, daysInMonth).AddHours(23).AddMinutes(59).AddSeconds(59);

        var nowVn = GetVnTime();
        var todayVn = UtcDate(nowVn.Year, nowVn.Month, nowVn.Day);

        var shift = await ResolveAssignedShiftAsync(employeeId, todayVn, ct);

        // Fetch records in month
        var records = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId && r.WorkDate >= startDate && r.WorkDate <= endDate)
            .ToListAsync(ct);

        // Fetch leaves in month
        var leaves = await _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.EmployeeId == employeeId &&
                        l.StartDate <= endDate && l.EndDate >= startDate)
            .ToListAsync(ct);

        // Fetch adjustment requests in month
        var adjustments = await _context.AttendanceAdjustmentRequests
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId &&
                        r.TargetDate >= startDate && r.TargetDate <= endDate)
            .ToListAsync(ct);

        var leaveSummary = await GetLeaveBalanceSummaryAsync(employeeId, year, ct);

        // Calculate restored/adjusted days
        var restoredDays = await _context.LeaveBalanceLedgers
            .AsNoTracking()
            .Where(l => l.EmployeeId == employeeId && l.Year == year &&
                        (l.TransactionType == LeaveLedgerTransactionType.AdminGrant ||
                         l.TransactionType == LeaveLedgerTransactionType.LeaveCancellationReversal ||
                         l.TransactionType == LeaveLedgerTransactionType.Adjustment))
            .SumAsync(l => l.DaysChanged, ct);

        // Build anomalies list & metrics
        var anomalies = new List<EmployeeAnomalyRecordDto>();
        int fullDays = 0;
        int missingIn = 0;
        int missingOut = 0;
        int unclockedDays = 0;
        int lateTimes = 0;
        int earlyTimes = 0;

        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            if (d > todayVn) break; // Don't flag future days

            var isWeekend = d.DayOfWeek == DayOfWeek.Sunday;
            if (isWeekend) continue;

            var rec = records.FirstOrDefault(r => r.WorkDate == d);
            var onLeave = leaves.FirstOrDefault(l => l.Status == AttendanceRequestStatus.Approved && l.StartDate <= d && l.EndDate >= d);
            var adj = adjustments.FirstOrDefault(a => a.TargetDate == d);

            if (onLeave != null)
            {
                continue;
            }

            if (rec != null)
            {
                if (rec.ClockInTime.HasValue && rec.ClockOutTime.HasValue)
                {
                    fullDays++;
                }

                if (rec.LateMinutes > 0 || rec.Status == AttendanceStatus.Late || rec.Status == AttendanceStatus.LateAndEarlyLeave)
                {
                    lateTimes++;
                    anomalies.Add(new EmployeeAnomalyRecordDto
                    {
                        Date = d,
                        AnomalyType = "Đi muộn",
                        Status = (adj != null && adj.Status == AttendanceRequestStatus.Approved) ? "Đã xử lý" : (adj != null && adj.Status == AttendanceRequestStatus.Pending ? "Đang chờ duyệt" : "Chờ xử lý"),
                        Details = $"Đi muộn {rec.LateMinutes} phút (vào ca lúc {rec.ClockInTime!.Value.AddHours(7):HH:mm})",
                        RequestId = adj?.Id
                    });
                }

                if (rec.EarlyLeaveMinutes > 0 || rec.Status == AttendanceStatus.EarlyLeave || rec.Status == AttendanceStatus.LateAndEarlyLeave)
                {
                    earlyTimes++;
                    anomalies.Add(new EmployeeAnomalyRecordDto
                    {
                        Date = d,
                        AnomalyType = "Về sớm",
                        Status = (adj != null && adj.Status == AttendanceRequestStatus.Approved) ? "Đã xử lý" : (adj != null && adj.Status == AttendanceRequestStatus.Pending ? "Đang chờ duyệt" : "Chờ xử lý"),
                        Details = $"Về sớm {rec.EarlyLeaveMinutes} phút (ra ca lúc {rec.ClockOutTime!.Value.AddHours(7):HH:mm})",
                        RequestId = adj?.Id
                    });
                }

                if (rec.ClockInTime.HasValue && !rec.ClockOutTime.HasValue)
                {
                    var shiftEnd = shift?.EndTime ?? new TimeSpan(17, 30, 0);
                    var graceLimit = shiftEnd.Add(TimeSpan.FromMinutes(shift?.GracePeriodMinutes ?? 15));
                    bool isPastShiftEnd = (d < todayVn) || (d == todayVn && nowVn.TimeOfDay > graceLimit);

                    if (isPastShiftEnd)
                    {
                        missingOut++;
                        anomalies.Add(new EmployeeAnomalyRecordDto
                        {
                            Date = d,
                            AnomalyType = "Thiếu check-out",
                            Status = (adj != null && adj.Status == AttendanceRequestStatus.Approved) ? "Đã xử lý" : (adj != null && adj.Status == AttendanceRequestStatus.Pending ? "Đang chờ duyệt" : "Chờ xử lý"),
                            Details = $"Đã chấm vào lúc {rec.ClockInTime.Value.AddHours(7):HH:mm} nhưng chưa chấm ra ca sau khi hết giờ làm",
                            RequestId = adj?.Id
                        });
                    }
                }

                if (!rec.ClockInTime.HasValue)
                {
                    missingIn++;
                    anomalies.Add(new EmployeeAnomalyRecordDto
                    {
                        Date = d,
                        AnomalyType = "Thiếu check-in",
                        Status = (adj != null && adj.Status == AttendanceRequestStatus.Approved) ? "Đã xử lý" : (adj != null && adj.Status == AttendanceRequestStatus.Pending ? "Đang chờ duyệt" : "Chờ xử lý"),
                        Details = "Không có dữ liệu chấm công vào ca hợp lệ",
                        RequestId = adj?.Id
                    });
                }
            }
            else
            {
                unclockedDays++;
                missingIn++;
                anomalies.Add(new EmployeeAnomalyRecordDto
                {
                    Date = d,
                    AnomalyType = "Thiếu check-in",
                    Status = (adj != null && adj.Status == AttendanceRequestStatus.Approved) ? "Đã xử lý" : (adj != null && adj.Status == AttendanceRequestStatus.Pending ? "Đang chờ duyệt" : "Chờ xử lý"),
                    Details = "Không có lượt chấm công nào trong ngày làm việc",
                    RequestId = adj?.Id
                });
            }
        }

        var totalWorkingMins = records.Sum(r => r.WorkingMinutes);
        var totalOvertimeMins = records.Sum(r => r.OvertimeMinutes);

        var roleName = "Nhân viên";
        if (!string.IsNullOrEmpty(emp.UserId))
        {
            var u = await _userManager.FindByIdAsync(emp.UserId);
            if (u != null)
            {
                var userRoles = await _userManager.GetRolesAsync(u);
                if (userRoles.Count > 0) roleName = string.Join(", ", userRoles);
            }
        }

        var dto = new EmployeeMonitoringDetailDto
        {
            EmployeeId = emp.Id,
            EmployeeCode = emp.Code,
            EmployeeName = emp.FullName,
            AvatarPath = emp.AvatarPath,
            DepartmentName = emp.Department?.Name ?? "Chưa phân ban",
            PositionName = emp.Position?.Name ?? "Nhân viên",
            RoleName = roleName,
            ShiftName = shift?.Name ?? "Ca hành chính",
            ShiftTimeRange = shift != null ? $"{shift.StartTime:hh\\:mm} - {shift.EndTime:hh\\:mm}" : "08:00 - 17:30",

            Month = month,
            Year = year,
            MonthlyStandardWorkDays = 26,
            FullAttendanceDays = fullDays,
            UnclockedDays = unclockedDays,
            LateTimes = lateTimes,
            EarlyLeaveTimes = earlyTimes,
            MissedClockInTimes = missingIn,
            MissedClockOutTimes = missingOut,
            TotalWorkingHours = Math.Round((decimal)totalWorkingMins / 60m, 1),
            TotalOvertimeHours = Math.Round((decimal)totalOvertimeMins / 60m, 1),

            TotalLeaveEntitlement = leaveSummary.TotalEntitlement,
            UsedLeaveDays = leaveSummary.UsedDays,
            PendingLeaveDays = leaveSummary.PendingDays,
            RemainingLeaveDays = leaveSummary.RemainingDays,
            RestoredOrAdjustedLeaveDays = restoredDays,

            MissingPunchDaysCount = missingIn,
            MissingCheckOutDaysCount = missingOut,
            MissingExplanationCount = anomalies.Count(a => a.Status == "Chờ xử lý"),
            LateOrEarlyDaysCount = lateTimes + earlyTimes,
            PendingRequestsCount = anomalies.Count(a => a.Status == "Đang chờ duyệt"),
            ResolvedAnomaliesCount = anomalies.Count(a => a.Status == "Đã xử lý"),
            PendingAnomaliesCount = anomalies.Count(a => a.Status == "Chờ xử lý"),
            Anomalies = anomalies.OrderByDescending(a => a.Date).ToList()
        };

        return dto;
    }

    // === 5. Work Shifts ===
    public async Task<List<WorkShiftDto>> GetAllWorkShiftsAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _context.WorkShifts.AsNoTracking();
        if (!includeInactive) query = query.Where(s => s.IsActive);

        var list = await query.OrderBy(s => s.StartTime).ToListAsync(ct);
        return list.Select(s => new WorkShiftDto
        {
            Id = s.Id,
            Code = s.Code,
            Name = s.Name,
            ShiftType = s.ShiftType,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            BreakMinutes = s.BreakMinutes,
            GracePeriodMinutes = s.GracePeriodMinutes,
            FlexibleMinutes = s.FlexibleMinutes,
            IsOvernight = s.IsOvernight,
            IsActive = s.IsActive,
            Color = s.Color,
            AppliesTo = s.AppliesTo,
            Notes = s.Notes
        }).ToList();
    }

    public async Task<WorkShiftDto> SaveWorkShiftAsync(WorkShiftDto dto, string userId, CancellationToken ct = default)
    {
        WorkShift entity;
        if (dto.Id > 0)
        {
            entity = await _context.WorkShifts.FindAsync(new object[] { dto.Id }, ct)
                ?? throw new InvalidOperationException("Không tìm thấy ca làm việc.");
        }
        else
        {
            entity = new WorkShift();
            _context.WorkShifts.Add(entity);
        }

        entity.Code = dto.Code.Trim().ToUpper();
        entity.Name = dto.Name.Trim();
        entity.ShiftType = dto.ShiftType;
        entity.StartTime = dto.StartTime;
        entity.EndTime = dto.EndTime;
        entity.BreakMinutes = dto.BreakMinutes;
        entity.GracePeriodMinutes = dto.GracePeriodMinutes;
        entity.FlexibleMinutes = dto.FlexibleMinutes;
        entity.IsOvernight = dto.IsOvernight;
        entity.IsActive = dto.IsActive;
        entity.Color = string.IsNullOrWhiteSpace(dto.Color) ? "#2563eb" : dto.Color;
        entity.AppliesTo = dto.AppliesTo;
        entity.Notes = dto.Notes;

        await _context.SaveChangesAsync(ct);
        dto.Id = entity.Id;
        return dto;
    }

    public async Task<bool> DeleteWorkShiftAsync(int shiftId, string userId, CancellationToken ct = default)
    {
        var shift = await _context.WorkShifts.FindAsync(new object[] { shiftId }, ct);
        if (shift == null) return false;

        // Soft delete: mark inactive
        shift.IsActive = false;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    

    public async Task<bool> AssignShiftAsync(int employeeId, int shiftId, DateTime from, DateTime? to, string userId, CancellationToken ct = default)
    {
        var assignment = new EmployeeShiftAssignment
        {
            EmployeeId = employeeId,
            WorkShiftId = shiftId,
            EffectiveFrom = from.Date,
            EffectiveTo = to?.Date
        };
        _context.EmployeeShiftAssignments.Add(assignment);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    

    // === 6. Báo cáo & Audit Log ===
    public async Task<List<AttendanceReportSummaryDto>> GetMonthlyReportAsync(int month, int year, int? departmentId = null, string? branch = null, CancellationToken ct = default)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var startDate = UtcDate(year, month, 1);
        var endDate = UtcDate(year, month, daysInMonth);

        var empQuery = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Where(e => e.Status == EmployeeStatus.Active);

        if (departmentId.HasValue && departmentId.Value > 0)
            empQuery = empQuery.Where(e => e.DepartmentId == departmentId.Value);

        if (!string.IsNullOrWhiteSpace(branch))
            empQuery = empQuery.Where(e => e.WorkingBranch == branch);

        var employees = await empQuery.ToListAsync(ct);

        // Security & HR Boundary: Exclude Owner/Admin & exempt accounts from monthly reports
        var adminEmpIds = await _context.Users
            .Where(u => u.EmployeeId.HasValue && (u.UserName == "admin" || u.Email == "admin@tradeflow.local" || u.Email == "admin@tradeflow.vn"))
            .Select(u => u.EmployeeId!.Value)
            .ToListAsync(ct);
        if (adminEmpIds.Count > 0)
        {
            employees = employees.Where(e => !adminEmpIds.Contains(e.Id)).ToList();
        }

        var empIds = employees.Select(e => e.Id).ToList();

        var records = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(r => empIds.Contains(r.EmployeeId) && r.WorkDate >= startDate && r.WorkDate <= endDate)
            .ToListAsync(ct);

        var leaves = await _context.LeaveRequests
            .AsNoTracking()
            .Where(l => empIds.Contains(l.EmployeeId) &&
                        l.Status == AttendanceRequestStatus.Approved &&
                        l.StartDate <= endDate && l.EndDate >= startDate)
            .ToListAsync(ct);

        var list = new List<AttendanceReportSummaryDto>();

        foreach (var emp in employees)
        {
            var empRecords = records.Where(r => r.EmployeeId == emp.Id).ToList();
            var empLeaves = leaves.Where(l => l.EmployeeId == emp.Id).ToList();

            var actualDays = (decimal)empRecords.Count(r => r.ClockInTime.HasValue);
            var actualMinutes = empRecords.Sum(r => r.WorkingMinutes);
            var lateTimes = empRecords.Count(r => r.LateMinutes > 0);
            var lateMinutes = empRecords.Sum(r => r.LateMinutes);
            var earlyTimes = empRecords.Count(r => r.EarlyLeaveMinutes > 0);
            var earlyMinutes = empRecords.Sum(r => r.EarlyLeaveMinutes);
            var paidLeaves = empLeaves.Where(l => l.LeaveType != LeaveType.UnpaidLeave).Sum(l => l.TotalDays);
            var unpaidLeaves = empLeaves.Where(l => l.LeaveType == LeaveType.UnpaidLeave).Sum(l => l.TotalDays);

            list.Add(new AttendanceReportSummaryDto
            {
                EmployeeId = emp.Id,
                EmployeeCode = emp.Code,
                EmployeeName = emp.FullName,
                AvatarPath = emp.AvatarPath,
                DepartmentName = emp.Department?.Name ?? string.Empty,
                PositionName = emp.Position?.Name ?? string.Empty,
                StandardWorkDays = 26m,
                ActualWorkDays = actualDays,
                TotalWorkingHours = Math.Round((decimal)actualMinutes / 60m, 1),
                LateTimes = lateTimes,
                LateTotalMinutes = lateMinutes,
                EarlyLeaveTimes = earlyTimes,
                EarlyLeaveTotalMinutes = earlyMinutes,
                PaidLeaveDays = paidLeaves,
                UnpaidLeaveDays = unpaidLeaves,
                OvertimeHours = Math.Round((decimal)empRecords.Sum(r => r.OvertimeMinutes) / 60m, 1),
                AttendanceScore = Math.Max(0, Math.Min(100, 100 - (lateTimes * 2) - (earlyTimes * 2) - ((int)unpaidLeaves * 5)))
            });
        }

        return list.OrderBy(r => r.EmployeeCode).ToList();
    }

    public async Task<List<AttendanceAuditLogDto>> GetAuditLogsAsync(int? employeeId = null, int take = 50, CancellationToken ct = default)
    {
        var query = _context.AttendanceAuditLogs.AsNoTracking();

        if (employeeId.HasValue && employeeId.Value > 0)
        {
            var recordIds = await _context.AttendanceRecords
                .Where(r => r.EmployeeId == employeeId.Value)
                .Select(r => (int?)r.Id)
                .ToListAsync(ct);

            query = query.Where(a => recordIds.Contains(a.AttendanceRecordId));
        }

        var items = await query.OrderByDescending(a => a.ChangedAt).Take(take).ToListAsync(ct);

        return items.Select(a => new AttendanceAuditLogDto
        {
            Id = a.Id,
            AttendanceRecordId = a.AttendanceRecordId,
            RequestId = a.RequestId,
            Action = a.Action,
            Description = a.Description,
            ChangedByUserName = a.ChangedByUserName ?? "Hệ thống",
            ChangedAt = a.ChangedAt,
            IpAddress = a.IpAddress,
            UserAgent = a.UserAgent
        }).ToList();
    }

    // === Helpers ===
    private static void MapStatusPresentation(AttendanceStatus status, out string label, out string css)
    {
        switch (status)
        {
            case AttendanceStatus.OnTime:
                label = "Đúng giờ";
                css = "tf-status-on-time";
                break;
            case AttendanceStatus.Late:
                label = "Đi muộn";
                css = "tf-status-late";
                break;
            case AttendanceStatus.EarlyLeave:
                label = "Về sớm";
                css = "tf-status-early";
                break;
            case AttendanceStatus.LateAndEarlyLeave:
                label = "Muộn & Về sớm";
                css = "tf-status-late-early";
                break;
            case AttendanceStatus.MissingClockOut:
                label = "Quên chấm ra";
                css = "tf-status-missing-out";
                break;
            case AttendanceStatus.Absent:
                label = "Vắng mặt";
                css = "tf-status-absent";
                break;
            case AttendanceStatus.OnLeave:
                label = "Nghỉ phép";
                css = "tf-status-on-leave";
                break;
            case AttendanceStatus.Holiday:
                label = "Nghỉ lễ";
                css = "tf-status-holiday";
                break;
            case AttendanceStatus.Adjusted:
                label = "Đã điều chỉnh";
                css = "tf-status-adjusted";
                break;
            default:
                label = "Chưa vào ca";
                css = "tf-status-not-in";
                break;
        }
    }

    private static void MapCalendarDotAndBadge(AttendanceStatus status, out string label, out string badge, out string dot)
    {
        switch (status)
        {
            case AttendanceStatus.OnTime:
                label = "Đúng giờ";
                badge = "tf-cal-badge-ontime";
                dot = "#10b981";
                break;
            case AttendanceStatus.Late:
                label = "Đi muộn";
                badge = "tf-cal-badge-late";
                dot = "#f59e0b";
                break;
            case AttendanceStatus.EarlyLeave:
                label = "Về sớm";
                badge = "tf-cal-badge-early";
                dot = "#f59e0b";
                break;
            case AttendanceStatus.LateAndEarlyLeave:
                label = "Muộn & sớm";
                badge = "tf-cal-badge-late-early";
                dot = "#f97316";
                break;
            case AttendanceStatus.MissingClockOut:
                label = "Quên chấm ra";
                badge = "tf-cal-badge-missing-out";
                dot = "#ef4444";
                break;
            case AttendanceStatus.Absent:
                label = "Vắng mặt";
                badge = "tf-cal-badge-absent";
                dot = "#ef4444";
                break;
            case AttendanceStatus.OnLeave:
                label = "Nghỉ phép";
                badge = "tf-cal-badge-leave";
                dot = "#3b82f6";
                break;
            case AttendanceStatus.Holiday:
                label = "Nghỉ lễ";
                badge = "tf-cal-badge-holiday";
                dot = "#8b5cf6";
                break;
            case AttendanceStatus.Adjusted:
                label = "Đã điều chỉnh";
                badge = "tf-cal-badge-adjusted";
                dot = "#06b6d4";
                break;
            default:
                label = "Chưa vào";
                badge = "tf-cal-badge-not-in";
                dot = "#94a3b8";
                break;
        }
    }

    private static AttendanceRecordDto MapToRecordDto(AttendanceRecord r)
    {
        MapStatusPresentation(r.Status, out var label, out var css);
        return new AttendanceRecordDto
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            EmployeeCode = r.Employee?.Code ?? string.Empty,
            EmployeeName = r.Employee?.FullName ?? string.Empty,
            AvatarPath = r.Employee?.AvatarPath,
            DepartmentName = r.Employee?.Department?.Name ?? string.Empty,
            PositionName = r.Employee?.Position?.Name ?? string.Empty,
            Branch = r.Employee?.WorkingBranch ?? string.Empty,
            WorkShiftId = r.WorkShiftId,
            WorkShiftName = r.WorkShift?.Name ?? "Ca chuẩn",
            ShiftColor = r.WorkShift?.Color ?? "#2563eb",
            WorkDate = r.WorkDate,
            ClockInTime = r.ClockInTime,
            ClockOutTime = r.ClockOutTime,
            Status = r.Status,
            StatusLabel = label,
            StatusCssClass = css,
            WorkingMinutes = r.WorkingMinutes,
            LateMinutes = r.LateMinutes,
            EarlyLeaveMinutes = r.EarlyLeaveMinutes,
            OvertimeMinutes = r.OvertimeMinutes,
            IsOvernight = r.IsOvernight,
            IsManuallyAdjusted = r.IsManuallyAdjusted,
            Notes = r.Notes,
            ClockInSource = r.ClockInSource,
            ClockOutSource = r.ClockOutSource
        };
    }

    private static AttendanceAdjustmentRequestDto MapAdjustmentRequestDto(AttendanceAdjustmentRequest r)
    {
        var typeLabel = r.RequestType switch
        {
            AttendanceAdjustmentType.MissedClockIn => "Quên chấm vào",
            AttendanceAdjustmentType.MissedClockOut => "Quên chấm ra",
            AttendanceAdjustmentType.TimeCorrection => "Sai lệch giờ",
            AttendanceAdjustmentType.DeviceError => "Lỗi thiết bị",
            AttendanceAdjustmentType.BusinessTrip => "Công tác bên ngoài",
            _ => "Khác"
        };

        var (statusLabel, statusCss) = r.Status switch
        {
            AttendanceRequestStatus.Approved => ("Đã duyệt", "tf-req-approved"),
            AttendanceRequestStatus.Rejected => ("Từ chối", "tf-req-rejected"),
            AttendanceRequestStatus.Cancelled => ("Đã hủy", "tf-req-cancelled"),
            _ => ("Chờ duyệt", "tf-req-pending")
        };

        return new AttendanceAdjustmentRequestDto
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            EmployeeCode = r.Employee?.Code ?? string.Empty,
            EmployeeName = r.Employee?.FullName ?? string.Empty,
            AvatarPath = r.Employee?.AvatarPath,
            DepartmentName = r.Employee?.Department?.Name ?? string.Empty,
            AttendanceRecordId = r.AttendanceRecordId,
            RequestType = r.RequestType,
            RequestTypeLabel = typeLabel,
            TargetDate = r.TargetDate,
            ProposedClockIn = r.ProposedClockIn,
            ProposedClockOut = r.ProposedClockOut,
            Reason = r.Reason,
            AttachmentPath = r.AttachmentPath,
            Status = r.Status,
            StatusLabel = statusLabel,
            StatusCssClass = statusCss,
            CreatedAt = r.CreatedAt,
            ReviewedByUserName = r.ReviewedByUserName,
            ReviewedAt = r.ReviewedAt,
            ReviewNotes = r.ReviewNotes
        };
    }

    private static LeaveRequestDto MapLeaveRequestDto(LeaveRequest r)
    {
        var typeLabel = r.LeaveType switch
        {
            LeaveType.AnnualLeave => "Phép năm",
            LeaveType.SickLeave => "Nghỉ ốm / BHXH",
            LeaveType.MaternityLeave => "Nghỉ thai sản",
            LeaveType.UnpaidLeave => "Nghỉ không lương",
            LeaveType.WeddingLeave => "Nghỉ việc cưới",
            LeaveType.FuneralLeave => "Nghỉ việc tang",
            _ => "Nghỉ phép khác"
        };

        var (statusLabel, statusCss) = r.Status switch
        {
            AttendanceRequestStatus.Approved => ("Đã duyệt", "tf-req-approved"),
            AttendanceRequestStatus.Rejected => ("Từ chối", "tf-req-rejected"),
            AttendanceRequestStatus.Cancelled => ("Đã hủy", "tf-req-cancelled"),
            _ => ("Chờ duyệt", "tf-req-pending")
        };

        return new LeaveRequestDto
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            EmployeeCode = r.Employee?.Code ?? string.Empty,
            EmployeeName = r.Employee?.FullName ?? string.Empty,
            AvatarPath = r.Employee?.AvatarPath,
            DepartmentName = r.Employee?.Department?.Name ?? string.Empty,
            LeaveType = r.LeaveType,
            LeaveTypeLabel = typeLabel,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            TotalDays = r.TotalDays,
            Reason = r.Reason,
            AttachmentPath = r.AttachmentPath,
            Status = r.Status,
            StatusLabel = statusLabel,
            StatusCssClass = statusCss,
            CreatedAt = r.CreatedAt,
            ReviewedByUserName = r.ReviewedByUserName,
            ReviewedAt = r.ReviewedAt,
            ReviewNotes = r.ReviewNotes
        };
    }

    private static ShiftChangeRequestDto MapShiftChangeRequestDto(ShiftChangeRequest r)
    {
        var (statusLabel, statusCss) = r.Status switch
        {
            AttendanceRequestStatus.Approved => ("Đã duyệt", "tf-req-approved"),
            AttendanceRequestStatus.Rejected => ("Từ chối", "tf-req-rejected"),
            AttendanceRequestStatus.Cancelled => ("Đã hủy", "tf-req-cancelled"),
            _ => ("Chờ duyệt", "tf-req-pending")
        };

        return new ShiftChangeRequestDto
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            EmployeeCode = r.Employee?.Code ?? string.Empty,
            EmployeeName = r.Employee?.FullName ?? string.Empty,
            AvatarPath = r.Employee?.AvatarPath,
            DepartmentName = r.Employee?.Department?.Name ?? string.Empty,
            TargetDate = r.TargetDate,
            CurrentWorkShiftId = r.CurrentWorkShiftId,
            CurrentWorkShiftName = r.CurrentWorkShift?.Name ?? string.Empty,
            ProposedWorkShiftId = r.ProposedWorkShiftId,
            ProposedWorkShiftName = r.ProposedWorkShift?.Name ?? string.Empty,
            SubstituteEmployeeId = r.SubstituteEmployeeId,
            SubstituteEmployeeName = r.SubstituteEmployee?.FullName,
            Reason = r.Reason,
            Status = r.Status,
            StatusLabel = statusLabel,
            StatusCssClass = statusCss,
            CreatedAt = r.CreatedAt,
            ReviewedByUserName = r.ReviewedByUserName,
            ReviewedAt = r.ReviewedAt,
            ReviewNotes = r.ReviewNotes
        };
    }

    public async Task<bool> IsUserExemptFromAttendanceAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        if (string.Equals(user.UserName, "admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(user.Email, "admin@tradeflow.vn", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Administrator") || roles.Contains("Admin") || roles.Contains("Owner"))
        {
            return true;
        }

        var activePolicy = await _context.AttendancePolicies.AsNoTracking().FirstOrDefaultAsync(p => p.IsActive, ct);
        if (activePolicy != null && !string.IsNullOrWhiteSpace(activePolicy.ExemptEmployeeIds) && user.EmployeeId.HasValue)
        {
            var exemptIds = activePolicy.ExemptEmployeeIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (exemptIds.Contains(user.EmployeeId.Value.ToString())) return true;
        }

        return false;
    }

    public async Task<LeaveBalanceSummaryDto> GetLeaveBalanceSummaryAsync(int? employeeId, int year = 2026, CancellationToken ct = default)
    {
        if (!employeeId.HasValue || employeeId.Value <= 0)
        {
            return new LeaveBalanceSummaryDto
            {
                Year = year,
                TotalEntitlement = 12.0m,
                AccruedDays = 11.0m,
                UsedDays = 0,
                PendingDays = 0
            };
        }

        var emp = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId.Value, ct);

        var dto = new LeaveBalanceSummaryDto
        {
            EmployeeId = employeeId.Value,
            EmployeeCode = emp?.Code ?? string.Empty,
            EmployeeName = emp?.FullName ?? string.Empty,
            Year = year,
            TotalEntitlement = 12.0m
        };

        var transactions = await _context.LeaveBalanceLedgers
            .AsNoTracking()
            .Where(l => l.EmployeeId == employeeId.Value && l.Year == year)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(ct);

        if (transactions.Count == 0)
        {
            var currentMonth = (year == GetVnTime().Year) ? GetVnTime().Month : 12;
            decimal runningBalance = 0;
            var newLedgers = new List<LeaveBalanceLedger>();

            for (int m = 1; m <= currentMonth; m++)
            {
                runningBalance += 1.0m;
                var accrual = new LeaveBalanceLedger
                {
                    EmployeeId = employeeId.Value,
                    Year = year,
                    TransactionType = LeaveLedgerTransactionType.MonthlyAccrual,
                    DaysChanged = 1.0m,
                    BalanceAfter = runningBalance,
                    Reason = $"Tích lũy phép tháng {m:D2}/{year} (Quy chế 1 ngày/tháng đủ điều kiện)",
                    PerformedByUserName = "Hệ thống tự động",
                    CreatedAt = new DateTime(year, m, 1, 0, 0, 0, DateTimeKind.Utc)
                };
                newLedgers.Add(accrual);
            }

            _context.LeaveBalanceLedgers.AddRange(newLedgers);
            await _context.SaveChangesAsync(ct);

            transactions = await _context.LeaveBalanceLedgers
                .AsNoTracking()
                .Where(l => l.EmployeeId == employeeId.Value && l.Year == year)
                .OrderBy(l => l.CreatedAt)
                .ToListAsync(ct);
        }

        dto.AccruedDays = transactions.Where(t => t.DaysChanged > 0).Sum(t => t.DaysChanged);

        var approvedAnnualLeaves = await _context.LeaveRequests
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId.Value &&
                        r.Status == AttendanceRequestStatus.Approved &&
                        r.LeaveType == LeaveType.AnnualLeave &&
                        r.StartDate.Year == year)
            .SumAsync(r => (decimal?)r.TotalDays, ct) ?? 0;

        dto.UsedDays = approvedAnnualLeaves;

        var pendingAnnualLeaves = await _context.LeaveRequests
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId.Value &&
                        r.Status == AttendanceRequestStatus.Pending &&
                        r.LeaveType == LeaveType.AnnualLeave &&
                        r.StartDate.Year == year)
            .SumAsync(r => (decimal?)r.TotalDays, ct) ?? 0;

        dto.PendingDays = pendingAnnualLeaves;

        dto.Transactions = transactions.Select(t => new LeaveLedgerTransactionDto
        {
            Id = t.Id,
            EmployeeId = t.EmployeeId,
            Year = t.Year,
            TransactionType = t.TransactionType,
            TransactionTypeLabel = t.TransactionType switch
            {
                LeaveLedgerTransactionType.MonthlyAccrual => "Tích lũy hàng tháng",
                LeaveLedgerTransactionType.LeaveUsageDeduction => "Khấu trừ nghỉ phép",
                LeaveLedgerTransactionType.ManagerGrant => "Quản lý cấp thêm",
                LeaveLedgerTransactionType.AdminGrant => "Admin cấp thưởng",
                LeaveLedgerTransactionType.LeaveCancellationReversal => "Hoàn phép hủy đơn",
                LeaveLedgerTransactionType.AnnualRollover => "Kết chuyển năm trước",
                _ => "Điều chỉnh nhân sự"
            },
            DaysChanged = t.DaysChanged,
            BalanceAfter = t.BalanceAfter,
            Reason = t.Reason,
            LeaveRequestId = t.LeaveRequestId,
            PerformedByUserName = t.PerformedByUserName,
            CreatedAt = t.CreatedAt,
            Notes = t.Notes
        }).OrderByDescending(t => t.CreatedAt).ToList();

        return dto;
    }

    public async Task<bool> GrantExtraLeaveAsync(GrantExtraLeaveCommand command, string? performedByUserId, string? performedByUserName, bool isAdmin, CancellationToken ct = default)
    {
        if (command.EmployeeId <= 0 || command.Days <= 0 || string.IsNullOrWhiteSpace(command.Reason))
            return false;

        var emp = await _context.Employees.FindAsync(new object[] { command.EmployeeId }, ct);
        if (emp == null) return false;

        var year = GetVnTime().Year;
        var summary = await GetLeaveBalanceSummaryAsync(command.EmployeeId, year, ct);
        var balanceAfter = summary.RemainingDays + command.Days;

        var actorDisplayName = isAdmin ? "Owner / Admin" : (performedByUserName ?? "Quản lý");

        var ledger = new LeaveBalanceLedger
        {
            EmployeeId = command.EmployeeId,
            Year = year,
            TransactionType = isAdmin ? LeaveLedgerTransactionType.AdminGrant : LeaveLedgerTransactionType.ManagerGrant,
            DaysChanged = command.Days,
            BalanceAfter = balanceAfter,
            Reason = command.Reason,
            PerformedByUserId = performedByUserId,
            PerformedByUserName = actorDisplayName,
            Notes = command.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeaveBalanceLedgers.Add(ledger);

        var audit = new AttendanceAuditLog
        {
            Action = "GRANT_EXTRA_LEAVE",
            Description = $"{actorDisplayName} cấp thêm {command.Days} ngày phép cho nhân viên {emp.FullName} ({emp.Code}). Lý do: {command.Reason}",
            ChangedByUserId = performedByUserId,
            ChangedByUserName = actorDisplayName,
            ChangedAt = DateTime.UtcNow
        };
        _context.AttendanceAuditLogs.Add(audit);

        await _context.SaveChangesAsync(ct);
        return true;
    }

    

    public Task<List<WeeklyDemoEmployeeDto>> GetWeeklyDemoDatasetAsync(CancellationToken ct = default)
    {
        // Controlled, reproducible demonstration dataset for Week 09/11/2026 - 15/11/2026
        // Strictly labeled as DEMO / DỮ LIỆU MINH HỌA and isolated from live production data.
        var startDate = new DateTime(2026, 11, 9);
        var weekDays = Enumerable.Range(0, 7).Select(i => startDate.AddDays(i)).ToList();

        var list = new List<WeeklyDemoEmployeeDto>
        {
            new()
            {
                Id = 901,
                Code = "NV-DEMO-01",
                Name = "Nguyễn Hải Đăng",
                AvatarPath = "/images/avatars/default.png",
                Department = "Phòng Kỹ thuật & Công nghệ",
                Position = "Kỹ sư Hệ thống",
                ShiftName = "Ca hành chính (08:00 - 17:30)",
                TotalWorkDays = 5,
                TotalHours = 40.0m,
                LateCount = 0,
                LeaveCount = 0,
                Days = new List<DemoDayStatusDto>
                {
                    new() { Date = weekDays[0], DayOfWeekName = "T2 (09/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:55", ClockOut = "17:32", WorkingHours = "8h 00m", Note = "Đúng chuẩn" },
                    new() { Date = weekDays[1], DayOfWeekName = "T3 (10/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:58", ClockOut = "17:35", WorkingHours = "8h 00m", Note = "Đúng chuẩn" },
                    new() { Date = weekDays[2], DayOfWeekName = "T4 (11/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:50", ClockOut = "17:30", WorkingHours = "8h 00m", Note = "Đúng chuẩn" },
                    new() { Date = weekDays[3], DayOfWeekName = "T5 (12/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:52", ClockOut = "17:40", WorkingHours = "8h 00m", Note = "Đúng chuẩn" },
                    new() { Date = weekDays[4], DayOfWeekName = "T6 (13/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:54", ClockOut = "17:30", WorkingHours = "8h 00m", Note = "Đúng chuẩn" },
                    new() { Date = weekDays[5], DayOfWeekName = "T7 (14/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--", Note = "Nghỉ cuối tuần" },
                    new() { Date = weekDays[6], DayOfWeekName = "CN (15/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--", Note = "Nghỉ cuối tuần" }
                }
            },
            new()
            {
                Id = 902,
                Code = "NV-DEMO-02",
                Name = "Trần Thu Hương",
                AvatarPath = "/images/avatars/default.png",
                Department = "Phòng Kinh doanh & Bán lẻ",
                Position = "Chuyên viên Kinh doanh",
                ShiftName = "Ca hành chính (08:00 - 17:30)",
                TotalWorkDays = 5,
                TotalHours = 38.5m,
                LateCount = 2,
                LeaveCount = 0,
                Days = new List<DemoDayStatusDto>
                {
                    new() { Date = weekDays[0], DayOfWeekName = "T2 (09/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "08:00", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[1], DayOfWeekName = "T3 (10/11)", Status = AttendanceStatus.Late, StatusLabel = "Đi muộn 35m", BadgeClass = "tf-demo-late", ClockIn = "08:35", ClockOut = "17:30", WorkingHours = "7h 25m", Note = "Tắc đường cầu vượt" },
                    new() { Date = weekDays[2], DayOfWeekName = "T4 (11/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:55", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[3], DayOfWeekName = "T5 (12/11)", Status = AttendanceStatus.Late, StatusLabel = "Đi muộn 45m", BadgeClass = "tf-demo-late", ClockIn = "08:45", ClockOut = "17:30", WorkingHours = "7h 15m", Note = "Hỏng xe máy" },
                    new() { Date = weekDays[4], DayOfWeekName = "T6 (13/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:58", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[5], DayOfWeekName = "T7 (14/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" },
                    new() { Date = weekDays[6], DayOfWeekName = "CN (15/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" }
                }
            },
            new()
            {
                Id = 903,
                Code = "NV-DEMO-03",
                Name = "Lê Quốc Tuấn",
                AvatarPath = "/images/avatars/default.png",
                Department = "Phòng Kho vận & Logistics",
                Position = "Thủ kho",
                ShiftName = "Ca hành chính (08:00 - 17:30)",
                TotalWorkDays = 5,
                TotalHours = 38.8m,
                LateCount = 0,
                LeaveCount = 0,
                Days = new List<DemoDayStatusDto>
                {
                    new() { Date = weekDays[0], DayOfWeekName = "T2 (09/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:50", ClockOut = "17:35", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[1], DayOfWeekName = "T3 (10/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:55", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[2], DayOfWeekName = "T4 (11/11)", Status = AttendanceStatus.EarlyLeave, StatusLabel = "Về sớm 1h15m", BadgeClass = "tf-demo-early", ClockIn = "07:52", ClockOut = "16:15", WorkingHours = "6h 45m", Note = "Đón con đi khám bệnh" },
                    new() { Date = weekDays[3], DayOfWeekName = "T5 (12/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:48", ClockOut = "17:40", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[4], DayOfWeekName = "T6 (13/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:56", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[5], DayOfWeekName = "T7 (14/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" },
                    new() { Date = weekDays[6], DayOfWeekName = "CN (15/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" }
                }
            },
            new()
            {
                Id = 904,
                Code = "NV-DEMO-04",
                Name = "Phạm Minh Châu",
                AvatarPath = "/images/avatars/default.png",
                Department = "Phòng Tài chính Kế toán",
                Position = "Kế toán viên",
                ShiftName = "Ca hành chính (08:00 - 17:30)",
                TotalWorkDays = 5,
                TotalHours = 38.0m,
                LateCount = 0,
                LeaveCount = 0,
                Days = new List<DemoDayStatusDto>
                {
                    new() { Date = weekDays[0], DayOfWeekName = "T2 (09/11)", Status = AttendanceStatus.MissingClockOut, StatusLabel = "Quên chấm ra", BadgeClass = "tf-demo-missing", ClockIn = "08:02", ClockOut = "--:--", WorkingHours = "Chờ giải trình", Note = "Đã gửi đơn giải trình giờ ra 17:35" },
                    new() { Date = weekDays[1], DayOfWeekName = "T3 (10/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:55", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[2], DayOfWeekName = "T4 (11/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:50", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[3], DayOfWeekName = "T5 (12/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:54", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[4], DayOfWeekName = "T6 (13/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:58", ClockOut = "17:35", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[5], DayOfWeekName = "T7 (14/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" },
                    new() { Date = weekDays[6], DayOfWeekName = "CN (15/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" }
                }
            },
            new()
            {
                Id = 905,
                Code = "NV-DEMO-05",
                Name = "Hoàng Trọng Nam",
                AvatarPath = "/images/avatars/default.png",
                Department = "Phòng Hành chính Nhân sự",
                Position = "Chuyên viên Nhân sự",
                ShiftName = "Ca hành chính (08:00 - 17:30)",
                TotalWorkDays = 3,
                TotalHours = 24.0m,
                LateCount = 0,
                LeaveCount = 2,
                Days = new List<DemoDayStatusDto>
                {
                    new() { Date = weekDays[0], DayOfWeekName = "T2 (09/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:50", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[1], DayOfWeekName = "T3 (10/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:52", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[2], DayOfWeekName = "T4 (11/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:55", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[3], DayOfWeekName = "T5 (12/11)", Status = AttendanceStatus.OnLeave, StatusLabel = "Phép năm (Đã duyệt)", BadgeClass = "tf-demo-leave", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "8h (Hưởng lương)", Note = "Nghỉ phép thường niên" },
                    new() { Date = weekDays[4], DayOfWeekName = "T6 (13/11)", Status = AttendanceStatus.OnLeave, StatusLabel = "Phép năm (Đã duyệt)", BadgeClass = "tf-demo-leave", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "8h (Hưởng lương)", Note = "Nghỉ phép thường niên" },
                    new() { Date = weekDays[5], DayOfWeekName = "T7 (14/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" },
                    new() { Date = weekDays[6], DayOfWeekName = "CN (15/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" }
                }
            },
            new()
            {
                Id = 906,
                Code = "NV-DEMO-06",
                Name = "Vũ Thùy Linh",
                AvatarPath = "/images/avatars/default.png",
                Department = "Phòng Kinh doanh & Bán lẻ",
                Position = "Đại diện Bán hàng",
                ShiftName = "Ca hành chính (08:00 - 17:30)",
                TotalWorkDays = 4,
                TotalHours = 32.0m,
                LateCount = 0,
                LeaveCount = 1,
                Days = new List<DemoDayStatusDto>
                {
                    new() { Date = weekDays[0], DayOfWeekName = "T2 (09/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:58", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[1], DayOfWeekName = "T3 (10/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:56", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[2], DayOfWeekName = "T4 (11/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:55", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[3], DayOfWeekName = "T5 (12/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:52", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[4], DayOfWeekName = "T6 (13/11)", Status = AttendanceStatus.PendingAdjustment, StatusLabel = "Nghỉ phép (Chờ duyệt)", BadgeClass = "tf-demo-pending", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "Đang chờ duyệt", Note = "Đơn nghỉ phép gửi lúc 14:00 T5" },
                    new() { Date = weekDays[5], DayOfWeekName = "T7 (14/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" },
                    new() { Date = weekDays[6], DayOfWeekName = "CN (15/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" }
                }
            },
            new()
            {
                Id = 907,
                Code = "NV-DEMO-07",
                Name = "Đặng Việt Hoàng",
                AvatarPath = "/images/avatars/default.png",
                Department = "Phòng Kỹ thuật & Công nghệ",
                Position = "Kỹ thuật viên Hiện trường",
                ShiftName = "Ca hành chính (08:00 - 17:30)",
                TotalWorkDays = 4,
                TotalHours = 32.0m,
                LateCount = 0,
                LeaveCount = 0,
                Days = new List<DemoDayStatusDto>
                {
                    new() { Date = weekDays[0], DayOfWeekName = "T2 (09/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:55", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[1], DayOfWeekName = "T3 (10/11)", Status = AttendanceStatus.Absent, StatusLabel = "Vắng không phép", BadgeClass = "tf-demo-absent", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "0h", Note = "Không liên lạc được, HR đã gửi thông báo" },
                    new() { Date = weekDays[2], DayOfWeekName = "T4 (11/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:50", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[3], DayOfWeekName = "T5 (12/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:52", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[4], DayOfWeekName = "T6 (13/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:54", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[5], DayOfWeekName = "T7 (14/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" },
                    new() { Date = weekDays[6], DayOfWeekName = "CN (15/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" }
                }
            },
            new()
            {
                Id = 908,
                Code = "NV-DEMO-08",
                Name = "Bùi Ngọc Hân",
                AvatarPath = "/images/avatars/default.png",
                Department = "Phòng Marketing & Truyền thông",
                Position = "Content Creator",
                ShiftName = "Ca hành chính / Ca chiều",
                TotalWorkDays = 5,
                TotalHours = 40.0m,
                LateCount = 0,
                LeaveCount = 0,
                Days = new List<DemoDayStatusDto>
                {
                    new() { Date = weekDays[0], DayOfWeekName = "T2 (09/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:58", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[1], DayOfWeekName = "T3 (10/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:55", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[2], DayOfWeekName = "T4 (11/11)", Status = AttendanceStatus.Adjusted, StatusLabel = "Đổi ca chiều", BadgeClass = "tf-demo-shiftchange", ClockIn = "12:55", ClockOut = "21:05", WorkingHours = "8h 00m", Note = "Đổi ca chiều livestream sự kiện ra mắt" },
                    new() { Date = weekDays[3], DayOfWeekName = "T5 (12/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:50", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[4], DayOfWeekName = "T6 (13/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "07:55", ClockOut = "17:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[5], DayOfWeekName = "T7 (14/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" },
                    new() { Date = weekDays[6], DayOfWeekName = "CN (15/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" }
                }
            },
            new()
            {
                Id = 909,
                Code = "NV-DEMO-09",
                Name = "Ngô Bảo Long",
                AvatarPath = "/images/avatars/default.png",
                Department = "Phòng Giao vận & Điều phối",
                Position = "Tài xế giao hàng",
                ShiftName = "Ca sáng (06:00 - 14:30)",
                TotalWorkDays = 5,
                TotalHours = 40.0m,
                LateCount = 0,
                LeaveCount = 0,
                Days = new List<DemoDayStatusDto>
                {
                    new() { Date = weekDays[0], DayOfWeekName = "T2 (09/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "05:52", ClockOut = "14:35", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[1], DayOfWeekName = "T3 (10/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "05:50", ClockOut = "14:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[2], DayOfWeekName = "T4 (11/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "05:55", ClockOut = "14:32", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[3], DayOfWeekName = "T5 (12/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "05:48", ClockOut = "14:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[4], DayOfWeekName = "T6 (13/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "05:50", ClockOut = "14:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[5], DayOfWeekName = "T7 (14/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" },
                    new() { Date = weekDays[6], DayOfWeekName = "CN (15/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" }
                }
            },
            new()
            {
                Id = 910,
                Code = "NV-DEMO-10",
                Name = "Đỗ Thanh Vân",
                AvatarPath = "/images/avatars/default.png",
                Department = "Phòng Chăm sóc Khách hàng",
                Position = "Tổng đài viên",
                ShiftName = "Ca chiều (14:00 - 22:30)",
                TotalWorkDays = 5,
                TotalHours = 39.5m,
                LateCount = 1,
                LeaveCount = 0,
                Days = new List<DemoDayStatusDto>
                {
                    new() { Date = weekDays[0], DayOfWeekName = "T2 (09/11)", Status = AttendanceStatus.Late, StatusLabel = "Đi muộn 25m", BadgeClass = "tf-demo-late", ClockIn = "14:25", ClockOut = "22:30", WorkingHours = "7h 35m", Note = "Mưa to đường ngập" },
                    new() { Date = weekDays[1], DayOfWeekName = "T3 (10/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "13:55", ClockOut = "22:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[2], DayOfWeekName = "T4 (11/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "13:50", ClockOut = "22:35", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[3], DayOfWeekName = "T5 (12/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "13:52", ClockOut = "22:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[4], DayOfWeekName = "T6 (13/11)", Status = AttendanceStatus.OnTime, StatusLabel = "Đúng giờ", BadgeClass = "tf-demo-ontime", ClockIn = "13:58", ClockOut = "22:30", WorkingHours = "8h 00m" },
                    new() { Date = weekDays[5], DayOfWeekName = "T7 (14/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" },
                    new() { Date = weekDays[6], DayOfWeekName = "CN (15/11)", Status = AttendanceStatus.NotClockedIn, StatusLabel = "Nghỉ tuần", BadgeClass = "tf-demo-weekend", ClockIn = "--:--", ClockOut = "--:--", WorkingHours = "--" }
                }
            }
        };

        return Task.FromResult(list);
    }
}
