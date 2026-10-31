using TradeFlow.Application.Common.Models.Documents;
using TradeFlow.Application.Common.Models.Employees;
using TradeFlow.Application.Common.Models.Users;
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
    Task<List<DepartmentDto>> GetDepartmentsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<DepartmentDto?> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DepartmentDto> CreateDepartmentAsync(string code, string name, string? description, string performedBy, CancellationToken cancellationToken = default);
    Task<DepartmentDto> UpdateDepartmentAsync(int id, string code, string name, string? description, string performedBy, CancellationToken cancellationToken = default);
    Task<bool> DeactivateDepartmentAsync(int id, string performedBy, CancellationToken cancellationToken = default);
    Task<bool> ReactivateDepartmentAsync(int id, string performedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteDepartmentAsync(int id, string performedBy, CancellationToken cancellationToken = default);

    Task<List<PositionDto>> GetPositionsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<PositionDto?> GetPositionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PositionDto> CreatePositionAsync(string code, string name, string? description, string performedBy, CancellationToken cancellationToken = default);
    Task<PositionDto> UpdatePositionAsync(int id, string code, string name, string? description, string performedBy, CancellationToken cancellationToken = default);
    Task<bool> DeactivatePositionAsync(int id, string performedBy, CancellationToken cancellationToken = default);
    Task<bool> ReactivatePositionAsync(int id, string performedBy, CancellationToken cancellationToken = default);
    Task<bool> DeletePositionAsync(int id, string performedBy, CancellationToken cancellationToken = default);

    // === Quy trình yêu cầu & Phê duyệt ===
    Task<List<EmployeeRequestDto>> GetEmployeeRequestsAsync(EmployeeRequestStatus? status = null, CancellationToken cancellationToken = default);
    Task<int> GetPendingRequestCountAsync(CancellationToken cancellationToken = default);
    Task<EmployeeRequestDto> SubmitEmployeeRequestAsync(CreateEmployeeRequestCommand command, string requestedBy, CancellationToken cancellationToken = default);
    Task<EmployeeRequestDto> ResubmitEmployeeRequestAsync(ResubmitEmployeeRequestCommand command, string performedBy, CancellationToken cancellationToken = default);
    Task<EmployeeDto?> ReviewEmployeeRequestAsync(ReviewEmployeeRequestCommand command, string reviewedBy, CancellationToken cancellationToken = default);

    // === Hồ sơ & Tài liệu đính kèm nhân sự ===
    Task<List<DocumentAttachmentDto>> GetEmployeeAttachmentsAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<List<DocumentAttachmentDto>> GetEmployeeRequestAttachmentsAsync(int requestId, CancellationToken cancellationToken = default);

    // === Danh mục người dùng liên kết (nhẹ, tối ưu hóa bộ nhớ) ===
    Task<List<UserLookupDto>> GetAvailableUsersAsync(CancellationToken cancellationToken = default);
}
