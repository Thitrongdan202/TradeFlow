using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Employees;

public class EmployeeFilterDto
{
    public string? SearchTerm { get; set; }
    public EmployeeStatus? Status { get; set; } = EmployeeStatus.Active;
    public int? DepartmentId { get; set; }
    public int? PositionId { get; set; }
    public UserStatus? AccountStatus { get; set; }
    public bool? HasAccount { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
