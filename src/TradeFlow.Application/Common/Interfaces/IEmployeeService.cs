using TradeFlow.Application.Common.Models.Employees;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Interfaces;

/// <summary>
/// Giao diện quản lý nhân viên, phòng ban, chức danh và luồng phê duyệt yêu cầu thêm nhân viên.
/// </summary>
public interface IEmployeeService
{
    // === Danh sách & Thông tin nhân viên ===
    Task<List<EmployeeDto>> GetEmployeesAsync(EmployeeFilterDto? filter = null, CancellationToken cancellationToken = default);
    Task<int> GetEmployeeCountAsync(EmployeeStatus? status = null, CancellationToken cancellationToken = default);
    Task<EmployeeDto?> GetEmployeeByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<string> GetNextEmployeeCodeAsync(CancellationToken cancellationToken = default);

    // === Thao tác nghiệp vụ nhân viên ===
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeCommand command, string performedBy, CancellationToken cancellationToken = default);
    Task<EmployeeDto> UpdateEmployeeAsync(UpdateEmployeeCommand command, string performedBy, CancellationToken cancellationToken = default);
    Task<bool> RetireEmployeeAsync(RetireEmployeeCommand command, string performedBy, CancellationToken cancellationToken = default);
    Task<bool> ReactivateEmployeeAsync(int id, string performedBy, CancellationToken cancellationToken = default);

    // === Phòng ban & Chức danh ===
    Task<List<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
    Task<DepartmentDto> CreateDepartmentAsync(string code, string name, string? description, string performedBy, CancellationToken cancellationToken = default);

    Task<List<PositionDto>> GetPositionsAsync(CancellationToken cancellationToken = default);
    Task<PositionDto> CreatePositionAsync(string code, string name, string? description, string performedBy, CancellationToken cancellationToken = default);

    // === Quy trình yêu cầu & Phê duyệt ===
    Task<List<EmployeeRequestDto>> GetEmployeeRequestsAsync(EmployeeRequestStatus? status = null, CancellationToken cancellationToken = default);
    Task<int> GetPendingRequestCountAsync(CancellationToken cancellationToken = default);
    Task<EmployeeRequestDto> SubmitEmployeeRequestAsync(CreateEmployeeRequestCommand command, string requestedBy, CancellationToken cancellationToken = default);
    Task<EmployeeDto?> ReviewEmployeeRequestAsync(ReviewEmployeeRequestCommand command, string reviewedBy, CancellationToken cancellationToken = default);
}
