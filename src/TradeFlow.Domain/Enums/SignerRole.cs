namespace TradeFlow.Domain.Enums;

/// <summary>
/// Vai trò thẩm quyền ký số
/// </summary>
public enum SignerRole
{
    /// <summary>Giám đốc điều hành</summary>
    Director = 1,

    /// <summary>Phó giám đốc</summary>
    DeputyDirector = 2,

    /// <summary>Quản trị viên an ninh được ủy quyền</summary>
    SecurityAdmin = 3,

    /// <summary>Người ký được ủy quyền</summary>
    AuthorizedSigner = 4
}

public static class SignerRoleExtensions
{
    public static string ToVietnamese(this SignerRole role) => role switch
    {
        SignerRole.Director => "Giám đốc",
        SignerRole.DeputyDirector => "Phó giám đốc",
        SignerRole.SecurityAdmin => "Quản trị viên an ninh",
        SignerRole.AuthorizedSigner => "Người ký được ủy quyền",
        _ => "Không xác định"
    };
}
