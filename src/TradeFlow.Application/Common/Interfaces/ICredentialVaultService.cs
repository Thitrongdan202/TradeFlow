namespace TradeFlow.Application.Common.Interfaces;

/// <summary>
/// Dịch vụ mã hóa và quản trị thông tin xác thực an toàn dành riêng cho quyền ROOT/Admin.
/// Áp dụng chuẩn mã hóa đối xứng AES-256 bảo vệ mật khẩu ở tầng lưu trữ và cho phép
/// Quản trị viên ROOT xem/khôi phục mật khẩu mà không lưu trữ plaintext trong cơ sở dữ liệu.
/// </summary>
public interface ICredentialVaultService
{
    /// <summary>
    /// Mã hóa mật khẩu sang chuỗi Base64 bảo mật bằng thuật toán AES-256.
    /// </summary>
    /// <param name="plainPassword">Mật khẩu dạng văn bản gốc</param>
    /// <returns>Chuỗi Base64 chứa IV (16 bytes) và Ciphertext đã mã hóa</returns>
    string EncryptPassword(string plainPassword);

    /// <summary>
    /// Giải mã mật khẩu từ chuỗi Base64 đã lưu trữ bằng khóa ROOT bí mật.
    /// </summary>
    /// <param name="encryptedPassword">Chuỗi Base64 mã hóa</param>
    /// <returns>Mật khẩu gốc ban đầu hoặc null nếu không thể giải mã</returns>
    string? DecryptPassword(string? encryptedPassword);
}
