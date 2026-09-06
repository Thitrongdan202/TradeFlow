using Microsoft.AspNetCore.Authorization;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Infrastructure.Authorization;

/// <summary>
/// Yêu cầu phân quyền dựa trên cặp (ResourceType, PermissionAction).
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public ResourceType Resource { get; }
    public PermissionAction Action { get; }

    public PermissionRequirement(ResourceType resource, PermissionAction action)
    {
        Resource = resource;
        Action = action;
    }
}