using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TradeFlow.Application.Common.Models.Security;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Interfaces;

public interface IRoleManagementService
{
    Task<RoleOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);

    Task<List<RoleListItemDto>> GetRolesAsync(string? searchTerm = null, CancellationToken cancellationToken = default);

    Task<RoleDetailDto?> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, string? RoleId)> CreateRoleAsync(CreateRoleCommand command, string createdBy, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> UpdateRoleAsync(UpdateRoleCommand command, string updatedBy, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, string? NewRoleId)> CloneRoleAsync(string sourceRoleId, string newName, string? newDescription, string clonedBy, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> ToggleRoleStatusAsync(string roleId, bool isActive, string updatedBy, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> DeleteRoleAsync(string roleId, string deletedBy, CancellationToken cancellationToken = default);

    Task<List<RolePermissionItemDto>> GetRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> UpdateRolePermissionsAsync(string roleId, List<RolePermissionItemDto> permissions, string updatedBy, CancellationToken cancellationToken = default);

    Task<List<UserRoleAssignmentDto>> GetUserRoleAssignmentsAsync(string? searchTerm = null, string? roleFilter = null, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> AssignSecondaryRoleAsync(string userId, string roleId, string assignedBy, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> RemoveSecondaryRoleAsync(string userId, string revokedBy, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> GrantTemporaryRoleAsync(GrantTemporaryRoleCommand command, string grantedBy, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> RevokeTemporaryRoleAsync(int assignmentId, string revokedBy, CancellationToken cancellationToken = default);

    Task<List<TemporaryRoleAssignmentItemDto>> GetTemporaryRoleAssignmentsAsync(CancellationToken cancellationToken = default);
}
