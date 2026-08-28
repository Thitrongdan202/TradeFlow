using System.Security.Claims;

namespace TradeFlow.Application.Common.Interfaces;

/// <summary>
/// Provides information about the currently authenticated user.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Username của người dùng hiện tại</summary>
    string? UserName { get; }

    /// <summary>ID của người dùng hiện tại</summary>
    string? UserId { get; }

    /// <summary>Địa chỉ IP của yêu cầu hiện tại</summary>
    string? IpAddress { get; }

    /// <summary>Người dùng có đang đăng nhập không?</summary>
    bool IsAuthenticated { get; }

    /// <summary>Kiểm tra người dùng có quyền trên tài nguyên không (server-side)</summary>
    bool HasPermission(string resource, string action);

    /// <summary>Claims của người dùng hiện tại</summary>
    IEnumerable<Claim> Claims { get; }
}
