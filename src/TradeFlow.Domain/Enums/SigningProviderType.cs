namespace TradeFlow.Domain.Enums;

/// <summary>
/// Loại nhà cung cấp phương thức ký số
/// </summary>
public enum SigningProviderType
{
    /// <summary>Chứng thư số phần mềm RSA cục bộ</summary>
    SoftwareRsa = 1,

    /// <summary>Thiết bị phần cứng USB Token / HSM (PKCS#11)</summary>
    UsbTokenPkcs11 = 2,

    /// <summary>Dịch vụ ký số từ xa (Cloud CA / Remote Signing)</summary>
    CloudCa = 3
}

public static class SigningProviderTypeExtensions
{
    public static string ToVietnamese(this SigningProviderType provider) => provider switch
    {
        SigningProviderType.SoftwareRsa => "Chứng thư số phần mềm (RSA Software)",
        SigningProviderType.UsbTokenPkcs11 => "Thiết bị USB Token / HSM (PKCS#11)",
        SigningProviderType.CloudCa => "Dịch vụ ký số từ xa (Cloud CA)",
        _ => "Không xác định"
    };
}
