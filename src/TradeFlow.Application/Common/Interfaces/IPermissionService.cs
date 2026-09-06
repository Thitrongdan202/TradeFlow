using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Interfaces;

/// <summary>
/// Dịch vụ phân quyền phía máy chủ (Server-side Permission Service).
/// Quản lý tra cứu quyền hạn hiệu lực từ vai trò của người dùng và lưu cache trong bộ nhớ.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Kiểm tra người dùng có quyền cụ thể hay không (bất đồng bộ).
    /// </summary>
    Task<bool> HasPermissionAsync(string userId, ResourceType resource, PermissionAction action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra người dùng có quyền cụ thể hay không qua tên chuỗi (bất đồng bộ).
    /// </summary>
    Task<bool> HasPermissionAsync(string userId, string resource, string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra người dùng có quyền cụ thể hay không (đồng bộ - tra cứu từ cache).
    /// </summary>
    bool HasPermission(string userId, string resource, string action);

    /// <summary>
    /// Lấy toàn bộ danh sách chuỗi quyền hạn hiệu lực ("Resource:Action") của người dùng (bất đồng bộ).
    /// </summary>
    Task<HashSet<string>> GetEffectivePermissionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy toàn bộ danh sách chuỗi quyền hạn hiệu lực của người dùng (đồng bộ - tra cứu từ cache).
    /// </summary>
    HashSet<string> GetEffectivePermissions(string userId);

    /// <summary>
    /// Xóa bộ nhớ đệm quyền hạn của một người dùng cụ thể.
    /// </summary>
    void InvalidateUserPermissions(string userId);

    /// <summary>
    /// Xóa toàn bộ bộ nhớ đệm quyền hạn của tất cả người dùng (ví dụ khi cập nhật vai trò).
    /// </summary>
    void InvalidateAllPermissions();
}