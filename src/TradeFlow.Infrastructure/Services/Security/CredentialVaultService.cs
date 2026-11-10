using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;

namespace TradeFlow.Infrastructure.Services.Security;

/// <summary>
/// Triển khai dịch vụ mã hóa đối xứng AES-256 cho kho chứng chỉ ROOT của TradeFlow ERP.
/// </summary>
public class CredentialVaultService : ICredentialVaultService
{
    private readonly byte[] _keyBytes;
    private readonly ILogger<CredentialVaultService> _logger;

    public CredentialVaultService(IConfiguration configuration, ILogger<CredentialVaultService> logger)
    {
        _logger = logger;

        // Khóa bí mật chủ quyền ROOT (Ưu tiên lấy từ Configuration, nếu không có sử dụng khóa hệ thống cố định)
        var secret = configuration["Security:CredentialVaultMasterKey"]
                     ?? "TradeFlow_Root_Credential_Vault_Key_2026_Secure_Aes256!#MasterSecretKey_Enterprise_ERP";

        using var sha256 = SHA256.Create();
        _keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(secret));
    }

    public string EncryptPassword(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
        {
            return string.Empty;
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = _keyBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            var iv = aes.IV;
            using var encryptor = aes.CreateEncryptor(aes.Key, iv);
            var plainBytes = Encoding.UTF8.GetBytes(plainPassword);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Gói [16 bytes IV] + [cipherBytes]
            var resultBytes = new byte[iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, resultBytes, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, resultBytes, iv.Length, cipherBytes.Length);

            return Convert.ToBase64String(resultBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi mã hóa mật khẩu kho ROOT.");
            throw;
        }
    }

    public string? DecryptPassword(string? encryptedPassword)
    {
        if (string.IsNullOrWhiteSpace(encryptedPassword))
        {
            return null;
        }

        try
        {
            var rawBytes = Convert.FromBase64String(encryptedPassword.Trim());
            if (rawBytes.Length <= 16)
            {
                return null;
            }

            var iv = new byte[16];
            Buffer.BlockCopy(rawBytes, 0, iv, 0, 16);

            var cipherBytes = new byte[rawBytes.Length - 16];
            Buffer.BlockCopy(rawBytes, 16, cipherBytes, 0, cipherBytes.Length);

            using var aes = Aes.Create();
            aes.Key = _keyBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor(aes.Key, iv);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Không thể giải mã mật khẩu: {Message}", ex.Message);
            return null;
        }
    }
}
