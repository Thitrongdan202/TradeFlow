using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Constants;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Employees;
using TradeFlow.Domain.Entities.Employees;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly TradeFlowDbContext _context;
    private readonly ISystemCodeGenerator _codeGenerator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly ISecurityService _securityService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        TradeFlowDbContext context,
        ISystemCodeGenerator codeGenerator,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        ISecurityService securityService,
        UserManager<ApplicationUser> userManager,
        ILogger<EmployeeService> logger)
    {
        _context = context;
        _codeGenerator = codeGenerator;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _securityService = securityService;
        _userManager = userManager;
        _logger = logger;
    }

    private static DateTime? NormalizeToUtc(DateTime? date)
    {
        if (!date.HasValue) return null;
        var val = date.Value;
        if (val.Kind == DateTimeKind.Unspecified || val.Kind == DateTimeKind.Local)
            return DateTime.SpecifyKind(val, DateTimeKind.Utc);
        return val.ToUniversalTime();
    }

    private static DateTime NormalizeToUtc(DateTime date)
    {
        if (date.Kind == DateTimeKind.Unspecified || date.Kind == DateTimeKind.Local)
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        return date.ToUniversalTime();
    }

    public async Task<List<EmployeeDto>> GetEmployeesAsync(EmployeeFilterDto? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .AsNoTracking();

        if (filter != null)
        {
            if (filter.Status.HasValue)
            {
                query = query.Where(e => e.Status == filter.Status.Value);
            }

            if (filter.DepartmentId.HasValue && filter.DepartmentId.Value > 0)
            {
                query = query.Where(e => e.DepartmentId == filter.DepartmentId.Value);
            }

            if (filter.PositionId.HasValue && filter.PositionId.Value > 0)
            {
                query = query.Where(e => e.PositionId == filter.PositionId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(e =>
                    e.Code.ToLower().Contains(term) ||
                    e.FullName.ToLower().Contains(term) ||
                    (e.Phone != null && e.Phone.Contains(term)) ||
                    (e.AttendanceCode != null && e.AttendanceCode.ToLower().Contains(term)) ||
                    (e.NationalId != null && e.NationalId.Contains(term)));
            }
        }

        var employees = await query
            .OrderBy(e => e.Code)
            .ToListAsync(cancellationToken);

        // Map user accounts if linked
        var userIds = employees.Where(e => !string.IsNullOrEmpty(e.UserId)).Select(e => e.UserId!).Distinct().ToList();
        var userMap = new Dictionary<string, string>();
        if (userIds.Count > 0)
        {
            var users = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync(cancellationToken);
            userMap = users.ToDictionary(u => u.Id, u => u.UserName ?? string.Empty);
        }

        return employees.Select(e => MapToDto(e, userMap)).ToList();
    }

    public async Task<int> GetEmployeeCountAsync(EmployeeStatus? status = null, CancellationToken cancellationToken = default)
    {
        if (status.HasValue)
        {
            return await _context.Employees.CountAsync(e => e.Status == status.Value, cancellationToken);
        }
        return await _context.Employees.CountAsync(cancellationToken);
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee == null) return null;

        string? userName = null;
        if (!string.IsNullOrEmpty(employee.UserId))
        {
            var user = await _userManager.FindByIdAsync(employee.UserId);
            userName = user?.UserName;
        }

        var userMap = !string.IsNullOrEmpty(employee.UserId) && !string.IsNullOrEmpty(userName)
            ? new Dictionary<string, string> { [employee.UserId] = userName }
            : new Dictionary<string, string>();

        return MapToDto(employee, userMap);
    }

    public async Task<string> GetNextEmployeeCodeAsync(CancellationToken cancellationToken = default)
    {
        return await _codeGenerator.PeekNextCodeAsync(SystemCodeConstants.Employee, cancellationToken);
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeCommand command, string performedBy, CancellationToken cancellationToken = default)
    {
        string code = command.Code?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code))
        {
            code = await _codeGenerator.GenerateCodeAsync(SystemCodeConstants.Employee, cancellationToken);
        }
        else
        {
            bool exists = await _context.Employees.AnyAsync(e => e.Code == code, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException($"Mã nhân viên '{code}' đã tồn tại trong hệ thống.");
            }
        }

        var employee = new Employee
        {
            Code = code,
            AttendanceCode = command.AttendanceCode?.Trim(),
            FullName = command.FullName.Trim(),
            NationalId = command.NationalId?.Trim(),
            DateOfBirth = NormalizeToUtc(command.DateOfBirth),
            Gender = command.Gender?.Trim(),
            AvatarPath = command.AvatarPath?.Trim(),
            Phone = command.Phone?.Trim(),
            Email = command.Email?.Trim(),
            Address = command.Address?.Trim(),
            Facebook = command.Facebook?.Trim(),
            MobileDevice = command.MobileDevice?.Trim(),
            DepartmentId = command.DepartmentId > 0 ? command.DepartmentId : null,
            PositionId = command.PositionId > 0 ? command.PositionId : null,
            WorkingBranch = command.WorkingBranch?.Trim(),
            PayrollBranch = command.PayrollBranch?.Trim(),
            StartDate = NormalizeToUtc(command.StartDate),
            Status = EmployeeStatus.Active,
            BankInformation = command.BankInformation?.Trim(),
            DebtAndAdvance = command.DebtAndAdvance,
            Notes = command.Notes?.Trim(),
            UserId = !string.IsNullOrWhiteSpace(command.UserId) ? command.UserId.Trim() : null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.EmployeeCreated,
            performedBy,
            "Employee",
            employee.Code,
            $"Thêm mới nhân viên '{employee.FullName}' (Mã: {employee.Code}).");

        if (!string.IsNullOrEmpty(employee.UserId))
        {
            await _auditService.LogAsync(
                AuditEventType.EmployeeLinkedToUser,
                performedBy,
                "Employee",
                employee.Code,
                $"Liên kết nhân viên '{employee.FullName}' với tài khoản người dùng ID '{employee.UserId}'.");
        }

        return (await GetEmployeeByIdAsync(employee.Id, cancellationToken))!;
    }

    public async Task<EmployeeDto> UpdateEmployeeAsync(UpdateEmployeeCommand command, string performedBy, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);
        if (employee == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy nhân viên ID '{command.Id}'.");
        }

        string? previousUserId = employee.UserId;

        employee.FullName = command.FullName.Trim();
        employee.AttendanceCode = command.AttendanceCode?.Trim();
        employee.NationalId = command.NationalId?.Trim();
        employee.DateOfBirth = NormalizeToUtc(command.DateOfBirth);
        employee.Gender = command.Gender?.Trim();
        if (!string.IsNullOrWhiteSpace(command.AvatarPath))
        {
            employee.AvatarPath = command.AvatarPath.Trim();
        }
        employee.Phone = command.Phone?.Trim();
        employee.Email = command.Email?.Trim();
        employee.Address = command.Address?.Trim();
        employee.Facebook = command.Facebook?.Trim();
        employee.MobileDevice = command.MobileDevice?.Trim();
        employee.DepartmentId = command.DepartmentId > 0 ? command.DepartmentId : null;
        employee.PositionId = command.PositionId > 0 ? command.PositionId : null;
        employee.WorkingBranch = command.WorkingBranch?.Trim();
        employee.PayrollBranch = command.PayrollBranch?.Trim();
        employee.StartDate = NormalizeToUtc(command.StartDate);
        employee.BankInformation = command.BankInformation?.Trim();
        employee.DebtAndAdvance = command.DebtAndAdvance;
        employee.Notes = command.Notes?.Trim();
        employee.UserId = !string.IsNullOrWhiteSpace(command.UserId) ? command.UserId.Trim() : null;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = performedBy;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.EmployeeUpdated,
            performedBy,
            "Employee",
            employee.Code,
            $"Cập nhật thông tin nhân viên '{employee.FullName}' (Mã: {employee.Code}).");

        if (previousUserId != employee.UserId)
        {
            if (!string.IsNullOrEmpty(employee.UserId))
            {
                await _auditService.LogAsync(
                    AuditEventType.EmployeeLinkedToUser,
                    performedBy,
                    "Employee",
                    employee.Code,
                    $"Liên kết nhân viên '{employee.FullName}' với tài khoản người dùng ID '{employee.UserId}'.");
            }
            else
            {
                await _auditService.LogAsync(
                    AuditEventType.EmployeeUnlinkedFromUser,
                    performedBy,
                    "Employee",
                    employee.Code,
                    $"Hủy liên kết tài khoản người dùng cho nhân viên '{employee.FullName}'.");
            }
        }

        return (await GetEmployeeByIdAsync(employee.Id, cancellationToken))!;
    }

    public async Task<bool> RetireEmployeeAsync(RetireEmployeeCommand command, string performedBy, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);
        if (employee == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy nhân viên ID '{command.Id}'.");
        }

        if (employee.Status == EmployeeStatus.Retired)
        {
            return true;
        }

        employee.Status = EmployeeStatus.Retired;
        employee.TerminationDate = NormalizeToUtc(command.TerminationDate);
        employee.TerminationReason = command.TerminationReason.Trim();
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = performedBy;

        await _context.SaveChangesAsync(cancellationToken);

        // If employee has a linked login account, offboard and revoke security tokens
        if (!string.IsNullOrEmpty(employee.UserId))
        {
            try
            {
                await _securityService.OffboardEmployeeAsync(
                    employee.UserId,
                    performedBy,
                    $"Thôi việc nhân viên {employee.FullName} ({employee.Code}): {command.TerminationReason}",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi thực hiện quy trình bảo mật thôi việc tài khoản {UserId} của nhân viên {Code}", employee.UserId, employee.Code);
            }
        }

        await _auditService.LogAsync(
            AuditEventType.EmployeeRetired,
            performedBy,
            "Employee",
            employee.Code,
            $"Thực hiện thủ tục thôi việc cho nhân viên '{employee.FullName}' (Mã: {employee.Code}). Lý do: {command.TerminationReason}.");

        return true;
    }

    public async Task<bool> ReactivateEmployeeAsync(int id, string performedBy, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (employee == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy nhân viên ID '{id}'.");
        }

        employee.Status = EmployeeStatus.Active;
        employee.TerminationDate = null;
        employee.TerminationReason = null;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = performedBy;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.EmployeeStatusChanged,
            performedBy,
            "Employee",
            employee.Code,
            $"Kích hoạt lại trạng thái 'Đang làm việc' cho nhân viên '{employee.FullName}' (Mã: {employee.Code}).");

        return true;
    }

    public async Task<List<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _context.Departments
            .Where(d => d.IsActive)
            .Include(d => d.Employees)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return departments.Select(d => new DepartmentDto
        {
            Id = d.Id,
            Code = d.Code,
            Name = d.Name,
            Description = d.Description,
            IsActive = d.IsActive,
            EmployeeCount = d.Employees.Count(e => e.Status == EmployeeStatus.Active)
        }).ToList();
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(string code, string name, string? description, string performedBy, CancellationToken cancellationToken = default)
    {
        string normCode = code.Trim().ToUpperInvariant();
        bool exists = await _context.Departments.AnyAsync(d => d.Code == normCode, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Mã phòng ban '{normCode}' đã tồn tại.");
        }

        var department = new Department
        {
            Code = normCode,
            Name = name.Trim(),
            Description = description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.DepartmentCreated,
            performedBy,
            "Department",
            department.Code,
            $"Tạo mới phòng ban '{department.Name}' (Mã: {department.Code}).");

        return new DepartmentDto
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive,
            EmployeeCount = 0
        };
    }

    public async Task<List<PositionDto>> GetPositionsAsync(CancellationToken cancellationToken = default)
    {
        var positions = await _context.Positions
            .Where(p => p.IsActive)
            .Include(p => p.Employees)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return positions.Select(p => new PositionDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Description = p.Description,
            IsActive = p.IsActive,
            EmployeeCount = p.Employees.Count(e => e.Status == EmployeeStatus.Active)
        }).ToList();
    }

    public async Task<PositionDto> CreatePositionAsync(string code, string name, string? description, string performedBy, CancellationToken cancellationToken = default)
    {
        string normCode = code.Trim().ToUpperInvariant();
        bool exists = await _context.Positions.AnyAsync(p => p.Code == normCode, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Mã chức danh '{normCode}' đã tồn tại.");
        }

        var position = new Position
        {
            Code = normCode,
            Name = name.Trim(),
            Description = description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        };

        _context.Positions.Add(position);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.PositionCreated,
            performedBy,
            "Position",
            position.Code,
            $"Tạo mới chức danh '{position.Name}' (Mã: {position.Code}).");

        return new PositionDto
        {
            Id = position.Id,
            Code = position.Code,
            Name = position.Name,
            Description = position.Description,
            IsActive = position.IsActive,
            EmployeeCount = 0
        };
    }

    public async Task<List<EmployeeRequestDto>> GetEmployeeRequestsAsync(EmployeeRequestStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.EmployeeRequests
            .Include(r => r.Department)
            .Include(r => r.Position)
            .AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);

        return list.Select(r => new EmployeeRequestDto
        {
            Id = r.Id,
            FullName = r.FullName,
            Code = r.Code,
            AttendanceCode = r.AttendanceCode,
            Phone = r.Phone,
            Email = r.Email,
            NationalId = r.NationalId,
            DateOfBirth = r.DateOfBirth,
            Gender = r.Gender,
            Address = r.Address,
            DepartmentId = r.DepartmentId,
            DepartmentName = r.Department?.Name,
            PositionId = r.PositionId,
            PositionName = r.Position?.Name,
            WorkingBranch = r.WorkingBranch,
            PayrollBranch = r.PayrollBranch,
            StartDate = r.StartDate,
            BankInformation = r.BankInformation,
            Notes = r.Notes,
            ProposedUserId = r.ProposedUserId,
            Status = r.Status,
            RequestedBy = r.RequestedBy,
            ApprovedBy = r.ApprovedBy,
            ApprovedAt = r.ApprovedAt,
            ReviewNotes = r.ReviewNotes,
            CreatedEmployeeId = r.CreatedEmployeeId,
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    public async Task<int> GetPendingRequestCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeRequests.CountAsync(r => r.Status == EmployeeRequestStatus.Pending, cancellationToken);
    }

    public async Task<EmployeeRequestDto> SubmitEmployeeRequestAsync(CreateEmployeeRequestCommand command, string requestedBy, CancellationToken cancellationToken = default)
    {
        var request = new EmployeeRequest
        {
            FullName = command.FullName.Trim(),
            Code = command.Code?.Trim(),
            AttendanceCode = command.AttendanceCode?.Trim(),
            Phone = command.Phone?.Trim(),
            Email = command.Email?.Trim(),
            NationalId = command.NationalId?.Trim(),
            DateOfBirth = NormalizeToUtc(command.DateOfBirth),
            Gender = command.Gender?.Trim(),
            Address = command.Address?.Trim(),
            DepartmentId = command.DepartmentId > 0 ? command.DepartmentId : null,
            PositionId = command.PositionId > 0 ? command.PositionId : null,
            WorkingBranch = command.WorkingBranch?.Trim(),
            PayrollBranch = command.PayrollBranch?.Trim(),
            StartDate = NormalizeToUtc(command.StartDate),
            BankInformation = command.BankInformation?.Trim(),
            Notes = command.Notes?.Trim(),
            ProposedUserId = !string.IsNullOrWhiteSpace(command.ProposedUserId) ? command.ProposedUserId.Trim() : null,
            Status = EmployeeRequestStatus.Pending,
            RequestedBy = requestedBy,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = requestedBy
        };

        _context.EmployeeRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.EmployeeRequestSubmitted,
            requestedBy,
            "EmployeeRequest",
            request.Id.ToString(),
            $"Gửi yêu cầu thêm nhân viên mới '{request.FullName}'.");

        var created = await _context.EmployeeRequests
            .Include(r => r.Department)
            .Include(r => r.Position)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        return new EmployeeRequestDto
        {
            Id = created!.Id,
            FullName = created.FullName,
            Code = created.Code,
            AttendanceCode = created.AttendanceCode,
            Phone = created.Phone,
            Email = created.Email,
            NationalId = created.NationalId,
            DateOfBirth = created.DateOfBirth,
            Gender = created.Gender,
            Address = created.Address,
            DepartmentId = created.DepartmentId,
            DepartmentName = created.Department?.Name,
            PositionId = created.PositionId,
            PositionName = created.Position?.Name,
            WorkingBranch = created.WorkingBranch,
            PayrollBranch = created.PayrollBranch,
            StartDate = created.StartDate,
            BankInformation = created.BankInformation,
            Notes = created.Notes,
            ProposedUserId = created.ProposedUserId,
            Status = created.Status,
            RequestedBy = created.RequestedBy,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<EmployeeDto?> ReviewEmployeeRequestAsync(ReviewEmployeeRequestCommand command, string reviewedBy, CancellationToken cancellationToken = default)
    {
        var request = await _context.EmployeeRequests
            .Include(r => r.Department)
            .Include(r => r.Position)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, cancellationToken);

        if (request == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy yêu cầu thêm nhân viên ID '{command.RequestId}'.");
        }

        if (request.Status != EmployeeRequestStatus.Pending)
        {
            throw new InvalidOperationException($"Yêu cầu này đã được xử lý trước đó (trạng thái: {request.Status}).");
        }

        if (command.Approve)
        {
            // Create employee from request
            var createCmd = new CreateEmployeeCommand
            {
                Code = request.Code,
                AttendanceCode = request.AttendanceCode,
                FullName = request.FullName,
                NationalId = request.NationalId,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                DepartmentId = request.DepartmentId,
                PositionId = request.PositionId,
                WorkingBranch = request.WorkingBranch,
                PayrollBranch = request.PayrollBranch,
                StartDate = request.StartDate,
                BankInformation = request.BankInformation,
                Notes = request.Notes,
                UserId = request.ProposedUserId
            };

            var employeeDto = await CreateEmployeeAsync(createCmd, reviewedBy, cancellationToken);

            request.Status = EmployeeRequestStatus.Approved;
            request.ApprovedBy = reviewedBy;
            request.ApprovedAt = DateTime.UtcNow;
            request.ReviewNotes = command.ReviewNotes?.Trim();
            request.CreatedEmployeeId = employeeDto.Id;
            request.UpdatedAt = DateTime.UtcNow;
            request.UpdatedBy = reviewedBy;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                AuditEventType.EmployeeRequestApproved,
                reviewedBy,
                "EmployeeRequest",
                request.Id.ToString(),
                $"Phê duyệt yêu cầu thêm nhân viên '{request.FullName}' -> Đã tạo hồ sơ nhân viên '{employeeDto.Code}'.");

            return employeeDto;
        }
        else
        {
            request.Status = EmployeeRequestStatus.Rejected;
            request.ApprovedBy = reviewedBy;
            request.ApprovedAt = DateTime.UtcNow;
            request.ReviewNotes = command.ReviewNotes?.Trim();
            request.UpdatedAt = DateTime.UtcNow;
            request.UpdatedBy = reviewedBy;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                AuditEventType.EmployeeRequestRejected,
                reviewedBy,
                "EmployeeRequest",
                request.Id.ToString(),
                $"Từ chối yêu cầu thêm nhân viên '{request.FullName}'. Lý do: {command.ReviewNotes}");

            return null;
        }
    }

    private static EmployeeDto MapToDto(Employee e, Dictionary<string, string> userMap)
    {
        string? userName = null;
        if (!string.IsNullOrEmpty(e.UserId) && userMap.TryGetValue(e.UserId, out var name))
        {
            userName = name;
        }

        return new EmployeeDto
        {
            Id = e.Id,
            Code = e.Code,
            AttendanceCode = e.AttendanceCode,
            FullName = e.FullName,
            NationalId = e.NationalId,
            DateOfBirth = e.DateOfBirth,
            Gender = e.Gender,
            AvatarPath = e.AvatarPath,
            Phone = e.Phone,
            Email = e.Email,
            Address = e.Address,
            Facebook = e.Facebook,
            MobileDevice = e.MobileDevice,
            DepartmentId = e.DepartmentId,
            DepartmentName = e.Department?.Name,
            PositionId = e.PositionId,
            PositionName = e.Position?.Name,
            WorkingBranch = e.WorkingBranch,
            PayrollBranch = e.PayrollBranch,
            StartDate = e.StartDate,
            Status = e.Status,
            TerminationDate = e.TerminationDate,
            TerminationReason = e.TerminationReason,
            BankInformation = e.BankInformation,
            DebtAndAdvance = e.DebtAndAdvance,
            Notes = e.Notes,
            UserId = e.UserId,
            UserName = userName,
            CreatedAt = e.CreatedAt,
            CreatedBy = e.CreatedBy,
            UpdatedAt = e.UpdatedAt,
            UpdatedBy = e.UpdatedBy
        };
    }
}
