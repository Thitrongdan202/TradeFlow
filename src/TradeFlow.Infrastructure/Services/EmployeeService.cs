using TradeFlow.Application.Common.Models.Documents;
using TradeFlow.Domain.Entities.Documents;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Models.Users;
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
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly ILogger<EmployeeService> _logger;
    private readonly IMemoryCache? _cache;

    private const string CacheKeyDepartments = "RefData_Departments";
    private const string CacheKeyPositions = "RefData_Positions";
    private const string CacheKeyUserLookups = "RefData_UserLookups";

    public EmployeeService(
        TradeFlowDbContext context,
        ISystemCodeGenerator codeGenerator,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        ISecurityService securityService,
        UserManager<ApplicationUser> userManager,
        ILogger<EmployeeService> logger,
        IMemoryCache? cache = null,
        IServiceScopeFactory? scopeFactory = null)
    {
        _context = context;
        _codeGenerator = codeGenerator;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _securityService = securityService;
        _userManager = userManager;
        _logger = logger;
        _cache = cache;
        _scopeFactory = scopeFactory;
    }

    private void InvalidateDepartmentCache() => _cache?.Remove(CacheKeyDepartments);
    private void InvalidatePositionCache() => _cache?.Remove(CacheKeyPositions);
    private void InvalidateUserCache() => _cache?.Remove(CacheKeyUserLookups);

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
        var userMap = new Dictionary<string, EmployeeUserMetadata>();
        if (userIds.Count > 0)
        {
            var users = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync(cancellationToken);

            userMap = users.ToDictionary(
                u => u.Id,
                u => new EmployeeUserMetadata
                {
                    UserName = u.UserName ?? string.Empty,
                    Role = u.UserRoles.FirstOrDefault()?.Role?.Name,
                    Status = u.Status,
                    MustChangePassword = u.MustChangePassword,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                });
        }

        var result = employees.Select(e => MapToDto(e, userMap)).ToList();

        if (filter != null)
        {
            if (filter.HasAccount.HasValue)
            {
                result = result.Where(e => e.HasAccount == filter.HasAccount.Value).ToList();
            }
            if (filter.AccountStatus.HasValue)
            {
                result = result.Where(e => e.AccountStatus == filter.AccountStatus.Value).ToList();
            }
        }

        return result;
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

        var userMap = new Dictionary<string, EmployeeUserMetadata>();
        if (!string.IsNullOrEmpty(employee.UserId))
        {
            var user = await _userManager.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == employee.UserId, cancellationToken);

            if (user != null)
            {
                userMap[employee.UserId] = new EmployeeUserMetadata
                {
                    UserName = user.UserName ?? string.Empty,
                    Role = user.UserRoles.FirstOrDefault()?.Role?.Name,
                    Status = user.Status,
                    MustChangePassword = user.MustChangePassword,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };
            }
        }

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
        employee.AvatarPath = string.IsNullOrWhiteSpace(command.AvatarPath) ? null : command.AvatarPath.Trim();
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

        // If employee has a linked login account, offboard, terminate status and revoke security tokens
        if (!string.IsNullOrEmpty(employee.UserId))
        {
            try
            {
                if (_scopeFactory != null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var securityService = scope.ServiceProvider.GetRequiredService<ISecurityService>();

                    var user = await userManager.FindByIdAsync(employee.UserId);
                    if (user != null)
                    {
                        user.Status = UserStatus.Terminated;
                        await userManager.UpdateSecurityStampAsync(user);
                        await userManager.UpdateAsync(user);
                    }

                    await securityService.OffboardEmployeeAsync(
                        employee.UserId,
                        $"Thôi việc nhân viên {employee.FullName} ({employee.Code}): {command.TerminationReason}",
                        performedBy,
                        cancellationToken);
                }
                else
                {
                    var user = await _userManager.FindByIdAsync(employee.UserId);
                    if (user != null)
                    {
                        user.Status = UserStatus.Terminated;
                        await _userManager.UpdateSecurityStampAsync(user);
                        await _userManager.UpdateAsync(user);
                    }

                    await _securityService.OffboardEmployeeAsync(
                        employee.UserId,
                        $"Thôi việc nhân viên {employee.FullName} ({employee.Code}): {command.TerminationReason}",
                        performedBy,
                        cancellationToken);
                }
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

    public async Task<List<DepartmentDto>> GetDepartmentsAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        if (!includeInactive && _cache != null && _cache.TryGetValue(CacheKeyDepartments, out List<DepartmentDto>? cached) && cached != null)
        {
            return cached;
        }

        var query = _context.Departments.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(d => d.IsActive);
        }

        var list = await query.OrderBy(d => d.Name)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Description = d.Description,
                IsActive = d.IsActive,
                EmployeeCount = d.Employees.Count(e => e.Status == EmployeeStatus.Active)
            })
            .ToListAsync(cancellationToken);

        if (!includeInactive && _cache != null)
        {
            _cache.Set(CacheKeyDepartments, list, TimeSpan.FromMinutes(15));
        }

        return list;
    }

    public async Task<DepartmentDto?> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Description = d.Description,
                IsActive = d.IsActive,
                EmployeeCount = d.Employees.Count(e => e.Status == EmployeeStatus.Active)
            })
            .FirstOrDefaultAsync(cancellationToken);
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

        InvalidateDepartmentCache();
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

    public async Task<DepartmentDto> UpdateDepartmentAsync(int id, string code, string name, string? description, string performedBy, CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (department == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy phòng ban với ID '{id}'.");
        }

        string normCode = code.Trim().ToUpperInvariant();
        if (normCode != department.Code)
        {
            bool exists = await _context.Departments.AnyAsync(d => d.Code == normCode && d.Id != id, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException($"Mã phòng ban '{normCode}' đã tồn tại.");
            }
            department.Code = normCode;
        }

        department.Name = name.Trim();
        department.Description = description?.Trim();
        department.UpdatedAt = DateTime.UtcNow;
        department.UpdatedBy = performedBy;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.DepartmentUpdated,
            performedBy,
            "Department",
            department.Code,
            $"Cập nhật thông tin phòng ban '{department.Name}' (Mã: {department.Code}).");

        InvalidateDepartmentCache();
        return new DepartmentDto
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive,
            EmployeeCount = department.Employees.Count(e => e.Status == EmployeeStatus.Active)
        };
    }

    public async Task<bool> DeactivateDepartmentAsync(int id, string performedBy, CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (department == null) return false;

        department.IsActive = false;
        department.UpdatedAt = DateTime.UtcNow;
        department.UpdatedBy = performedBy;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.DepartmentDeactivated,
            performedBy,
            "Department",
            department.Code,
            $"Ngừng sử dụng phòng ban '{department.Name}' (Mã: {department.Code}).");

        InvalidateDepartmentCache();
        return true;
    }

    public async Task<bool> ReactivateDepartmentAsync(int id, string performedBy, CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (department == null) return false;

        department.IsActive = true;
        department.UpdatedAt = DateTime.UtcNow;
        department.UpdatedBy = performedBy;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.DepartmentReactivated,
            performedBy,
            "Department",
            department.Code,
            $"Kích hoạt lại phòng ban '{department.Name}' (Mã: {department.Code}).");

        InvalidateDepartmentCache();
        return true;
    }

    public async Task<bool> DeleteDepartmentAsync(int id, string performedBy, CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (department == null) return false;

        // Safety check: Cannot hard-delete if referenced by any Employee or EmployeeRequest
        int empCount = await _context.Employees.CountAsync(e => e.DepartmentId == id, cancellationToken);
        int reqCount = await _context.EmployeeRequests.CountAsync(r => r.DepartmentId == id, cancellationToken);
        if (empCount > 0 || reqCount > 0)
        {
            throw new InvalidOperationException($"Không thể xóa phòng ban '{department.Name}' vì đang được tham chiếu bởi {empCount + reqCount} hồ sơ nhân sự hoặc yêu cầu tuyển dụng. Vui lòng chuyển sang 'Ngừng sử dụng' thay thế.");
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.DepartmentDeleted,
            performedBy,
            "Department",
            department.Code,
            $"Xóa vĩnh viễn phòng ban '{department.Name}' (Mã: {department.Code}).");

        InvalidateDepartmentCache();
        return true;
    }

    public async Task<List<PositionDto>> GetPositionsAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        if (!includeInactive && _cache != null && _cache.TryGetValue(CacheKeyPositions, out List<PositionDto>? cached) && cached != null)
        {
            return cached;
        }

        var query = _context.Positions.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        var list = await query.OrderBy(p => p.Name)
            .Select(p => new PositionDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                IsActive = p.IsActive,
                EmployeeCount = p.Employees.Count(e => e.Status == EmployeeStatus.Active)
            })
            .ToListAsync(cancellationToken);

        if (!includeInactive && _cache != null)
        {
            _cache.Set(CacheKeyPositions, list, TimeSpan.FromMinutes(15));
        }

        return list;
    }

    public async Task<PositionDto?> GetPositionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Positions
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PositionDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                IsActive = p.IsActive,
                EmployeeCount = p.Employees.Count(e => e.Status == EmployeeStatus.Active)
            })
            .FirstOrDefaultAsync(cancellationToken);
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

        InvalidatePositionCache();
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

    public async Task<PositionDto> UpdatePositionAsync(int id, string code, string name, string? description, string performedBy, CancellationToken cancellationToken = default)
    {
        var position = await _context.Positions
            .Include(p => p.Employees)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (position == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy chức danh với ID '{id}'.");
        }

        string normCode = code.Trim().ToUpperInvariant();
        if (normCode != position.Code)
        {
            bool exists = await _context.Positions.AnyAsync(p => p.Code == normCode && p.Id != id, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException($"Mã chức danh '{normCode}' đã tồn tại.");
            }
            position.Code = normCode;
        }

        position.Name = name.Trim();
        position.Description = description?.Trim();
        position.UpdatedAt = DateTime.UtcNow;
        position.UpdatedBy = performedBy;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.PositionUpdated,
            performedBy,
            "Position",
            position.Code,
            $"Cập nhật thông tin chức danh '{position.Name}' (Mã: {position.Code}).");

        InvalidatePositionCache();
        return new PositionDto
        {
            Id = position.Id,
            Code = position.Code,
            Name = position.Name,
            Description = position.Description,
            IsActive = position.IsActive,
            EmployeeCount = position.Employees.Count(e => e.Status == EmployeeStatus.Active)
        };
    }

    public async Task<bool> DeactivatePositionAsync(int id, string performedBy, CancellationToken cancellationToken = default)
    {
        var position = await _context.Positions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (position == null) return false;

        position.IsActive = false;
        position.UpdatedAt = DateTime.UtcNow;
        position.UpdatedBy = performedBy;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.PositionDeactivated,
            performedBy,
            "Position",
            position.Code,
            $"Ngừng sử dụng chức danh '{position.Name}' (Mã: {position.Code}).");

        InvalidatePositionCache();
        return true;
    }

    public async Task<bool> ReactivatePositionAsync(int id, string performedBy, CancellationToken cancellationToken = default)
    {
        var position = await _context.Positions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (position == null) return false;

        position.IsActive = true;
        position.UpdatedAt = DateTime.UtcNow;
        position.UpdatedBy = performedBy;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.PositionReactivated,
            performedBy,
            "Position",
            position.Code,
            $"Kích hoạt lại chức danh '{position.Name}' (Mã: {position.Code}).");

        InvalidatePositionCache();
        return true;
    }

    public async Task<bool> DeletePositionAsync(int id, string performedBy, CancellationToken cancellationToken = default)
    {
        var position = await _context.Positions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (position == null) return false;

        // Safety check: Cannot hard-delete if referenced by any Employee or EmployeeRequest
        int empCount = await _context.Employees.CountAsync(e => e.PositionId == id, cancellationToken);
        int reqCount = await _context.EmployeeRequests.CountAsync(r => r.PositionId == id, cancellationToken);
        if (empCount > 0 || reqCount > 0)
        {
            throw new InvalidOperationException($"Không thể xóa chức danh '{position.Name}' vì đang được tham chiếu bởi {empCount + reqCount} hồ sơ nhân sự hoặc yêu cầu tuyển dụng. Vui lòng chuyển sang 'Ngừng sử dụng' thay thế.");
        }

        _context.Positions.Remove(position);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.PositionDeleted,
            performedBy,
            "Position",
            position.Code,
            $"Xóa vĩnh viễn chức danh '{position.Name}' (Mã: {position.Code}).");

        InvalidatePositionCache();
        return true;
    }

    public async Task<List<EmployeeRequestDto>> GetEmployeeRequestsAsync(EmployeeRequestStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.EmployeeRequests.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var list = await query.OrderByDescending(r => r.CreatedAt)
            .Select(r => new EmployeeRequestDto
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
                DepartmentName = r.Department != null ? r.Department.Name : null,
                PositionId = r.PositionId,
                PositionName = r.Position != null ? r.Position.Name : null,
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
            })
            .ToListAsync(cancellationToken);

        if (list.Count > 0)
        {
            var reqIds = list.Select(r => r.Id).ToList();
            var attachments = await _context.DocumentAttachments
                .AsNoTracking()
                .Where(d => d.RelatedEntityType == "EmployeeRequest" && d.RelatedEntityId.HasValue && reqIds.Contains(d.RelatedEntityId.Value))
                .Select(d => new DocumentAttachmentDto
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    ContentType = d.ContentType,
                    FileSize = d.FileSize,
                    CategoryId = d.CategoryId,
                    CategoryName = d.Category != null ? d.Category.Name : null,
                    RelatedEntityType = d.RelatedEntityType,
                    RelatedEntityId = d.RelatedEntityId,
                    Description = d.Description,
                    Status = d.Status,
                    CreatedAt = d.CreatedAt,
                    CreatedBy = d.CreatedBy
                })
                .ToListAsync(cancellationToken);

            var attLookup = attachments
                .GroupBy(d => d.RelatedEntityId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var req in list)
            {
                if (attLookup.TryGetValue(req.Id, out var atts))
                {
                    req.Attachments = atts;
                }
            }
        }

        return list;
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

    public async Task<EmployeeRequestDto> ResubmitEmployeeRequestAsync(ResubmitEmployeeRequestCommand command, string performedBy, CancellationToken cancellationToken = default)
    {
        var request = await _context.EmployeeRequests
            .Include(r => r.Department)
            .Include(r => r.Position)
            .FirstOrDefaultAsync(r => r.Id == command.RequestId, cancellationToken);

        if (request == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy yêu cầu thêm nhân viên ID '{command.RequestId}'.");
        }

        if (request.Status != EmployeeRequestStatus.NeedAdditionalDocs && request.Status != EmployeeRequestStatus.Pending)
        {
            throw new InvalidOperationException($"Chỉ có thể gửi lại yêu cầu đang ở trạng thái 'Cần bổ sung hồ sơ' hoặc 'Chờ duyệt'. Trạng thái hiện tại: {request.Status}.");
        }

        if (!string.IsNullOrWhiteSpace(command.FullName)) request.FullName = command.FullName.Trim();
        if (!string.IsNullOrWhiteSpace(command.Phone)) request.Phone = command.Phone.Trim();
        if (!string.IsNullOrWhiteSpace(command.Email)) request.Email = command.Email.Trim();
        if (!string.IsNullOrWhiteSpace(command.NationalId)) request.NationalId = command.NationalId.Trim();
        if (command.DepartmentId.HasValue && command.DepartmentId.Value > 0) request.DepartmentId = command.DepartmentId.Value;
        if (command.PositionId.HasValue && command.PositionId.Value > 0) request.PositionId = command.PositionId.Value;
        if (command.DateOfBirth.HasValue) request.DateOfBirth = NormalizeToUtc(command.DateOfBirth);
        if (!string.IsNullOrWhiteSpace(command.Gender)) request.Gender = command.Gender.Trim();
        if (command.StartDate.HasValue) request.StartDate = NormalizeToUtc(command.StartDate);
        if (!string.IsNullOrWhiteSpace(command.WorkingBranch)) request.WorkingBranch = command.WorkingBranch.Trim();
        if (!string.IsNullOrWhiteSpace(command.PayrollBranch)) request.PayrollBranch = command.PayrollBranch.Trim();
        if (!string.IsNullOrWhiteSpace(command.BankInformation)) request.BankInformation = command.BankInformation.Trim();
        if (!string.IsNullOrWhiteSpace(command.Address)) request.Address = command.Address.Trim();
        if (!string.IsNullOrWhiteSpace(command.ProposedUserId)) request.ProposedUserId = command.ProposedUserId.Trim();

        if (!string.IsNullOrWhiteSpace(command.ResubmitNotes))
        {
            request.Notes = string.IsNullOrWhiteSpace(request.Notes)
                ? $"[Gửi lại bổ sung {DateTime.Now:dd/MM/yyyy HH:mm}]: {command.ResubmitNotes.Trim()}"
                : $"{request.Notes}\n[Gửi lại bổ sung {DateTime.Now:dd/MM/yyyy HH:mm}]: {command.ResubmitNotes.Trim()}";
        }

        request.Status = EmployeeRequestStatus.Pending; // Transitions back to Pending for review
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = performedBy;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.EmployeeRequestResubmitted,
            performedBy,
            "EmployeeRequest",
            request.Id.ToString(),
            $"Bổ sung hồ sơ và gửi lại yêu cầu tuyển dụng/thêm nhân viên '{request.FullName}' để xem xét phê duyệt.");

        return new EmployeeRequestDto
        {
            Id = request.Id,
            Code = request.Code,
            FullName = request.FullName,
            NationalId = request.NationalId,
            Phone = request.Phone,
            Email = request.Email,
            DepartmentId = request.DepartmentId,
            DepartmentName = request.Department?.Name,
            PositionId = request.PositionId,
            PositionName = request.Position?.Name,
            Status = request.Status,
            RequestedBy = request.RequestedBy,
            ReviewNotes = request.ReviewNotes,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt
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

            // Link attached dossier documents from the EmployeeRequest to the newly created Employee profile
            var requestAttachments = await _context.DocumentAttachments
                .Where(d => d.RelatedEntityType == "EmployeeRequest" && d.RelatedEntityId == request.Id)
                .ToListAsync(cancellationToken);

            foreach (var att in requestAttachments)
            {
                var empAttachment = new DocumentAttachment
                {
                    FileName = att.FileName,
                    StoragePath = att.StoragePath,
                    ContentType = att.ContentType,
                    FileSize = att.FileSize,
                    CategoryId = att.CategoryId,
                    RelatedEntityType = "Employee",
                    RelatedEntityId = employeeDto.Id,
                    Description = att.Description,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = reviewedBy
                };
                _context.DocumentAttachments.Add(empAttachment);
            }

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
                $"Phê duyệt yêu cầu thêm nhân viên '{request.FullName}' -> Đã tạo hồ sơ nhân viên '{employeeDto.Code}' và liên kết {requestAttachments.Count} tài liệu hồ sơ.");

            return employeeDto;
        }
        else if (command.RequestAdditionalDocs)
        {
            request.Status = EmployeeRequestStatus.NeedAdditionalDocs;
            request.ApprovedBy = reviewedBy;
            request.ApprovedAt = DateTime.UtcNow;
            request.ReviewNotes = command.ReviewNotes?.Trim();
            request.UpdatedAt = DateTime.UtcNow;
            request.UpdatedBy = reviewedBy;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                AuditEventType.EmployeeRequestAdditionalDocsRequested,
                reviewedBy,
                "EmployeeRequest",
                request.Id.ToString(),
                $"Yêu cầu bổ sung hồ sơ nhân sự cho '{request.FullName}'. Ý kiến: {command.ReviewNotes}");

            return null;
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

    private class EmployeeUserMetadata
    {
        public string UserName { get; set; } = string.Empty;
        public string? Role { get; set; }
        public UserStatus Status { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    private static EmployeeDto MapToDto(Employee e, Dictionary<string, EmployeeUserMetadata> userMap)
    {
        EmployeeUserMetadata? meta = null;
        if (!string.IsNullOrEmpty(e.UserId))
        {
            userMap.TryGetValue(e.UserId, out meta);
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
            UserName = meta?.UserName,
            AccountRole = meta?.Role,
            AccountStatus = meta?.Status,
            AccountMustChangePassword = meta?.MustChangePassword ?? false,
            AccountCreatedAt = meta?.CreatedAt,
            AccountLastLoginAt = meta?.LastLoginAt,
            CreatedAt = e.CreatedAt,
            CreatedBy = e.CreatedBy,
            UpdatedAt = e.UpdatedAt,
            UpdatedBy = e.UpdatedBy
        };
    }
public async Task<List<DocumentAttachmentDto>> GetEmployeeAttachmentsAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var attachments = await _context.DocumentAttachments
            .Include(d => d.Category)
            .Where(d => d.RelatedEntityType == "Employee" && d.RelatedEntityId == employeeId)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return attachments.Select(d => new DocumentAttachmentDto
        {
            Id = d.Id,
            FileName = d.FileName,
            ContentType = d.ContentType,
            FileSize = d.FileSize,
            CategoryId = d.CategoryId,
            CategoryName = d.Category?.Name,
            RelatedEntityType = d.RelatedEntityType,
            RelatedEntityId = d.RelatedEntityId,
            Description = d.Description,
            Status = d.Status,
            CreatedAt = d.CreatedAt,
            CreatedBy = d.CreatedBy
        }).ToList();
    }

    public async Task<List<DocumentAttachmentDto>> GetEmployeeRequestAttachmentsAsync(int requestId, CancellationToken cancellationToken = default)
    {
        var attachments = await _context.DocumentAttachments
            .Include(d => d.Category)
            .Where(d => d.RelatedEntityType == "EmployeeRequest" && d.RelatedEntityId == requestId)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return attachments.Select(d => new DocumentAttachmentDto
        {
            Id = d.Id,
            FileName = d.FileName,
            ContentType = d.ContentType,
            FileSize = d.FileSize,
            CategoryId = d.CategoryId,
            CategoryName = d.Category?.Name,
            RelatedEntityType = d.RelatedEntityType,
            RelatedEntityId = d.RelatedEntityId,
            Description = d.Description,
            Status = d.Status,
            CreatedAt = d.CreatedAt,
            CreatedBy = d.CreatedBy
        }).ToList();
    }

    public async Task<List<UserLookupDto>> GetAvailableUsersAsync(CancellationToken cancellationToken = default)
    {
        if (_cache != null && _cache.TryGetValue(CacheKeyUserLookups, out List<UserLookupDto>? cached) && cached != null)
        {
            return cached;
        }

        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.UserName)
            .Select(u => new UserLookupDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                FullName = u.FullName ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        if (_cache != null)
        {
            _cache.Set(CacheKeyUserLookups, users, TimeSpan.FromMinutes(15));
        }

        return users;
    }
}