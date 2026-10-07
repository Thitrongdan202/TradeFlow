using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Interfaces;

public class GeneratedKeyPair
{
    public string PublicKeyXml { get; set; } = string.Empty;
    public string PublicKeyPem { get; set; } = string.Empty;
    public string EncryptedPrivateKey { get; set; } = string.Empty;
    public string KeySalt { get; set; } = string.Empty;
    public string PinVerificationHash { get; set; } = string.Empty;
    public string CertificateSerialNumber { get; set; } = string.Empty;
    public string CertificateThumbprint { get; set; } = string.Empty;
}

public interface ISigningProvider
{
    SigningProviderType ProviderType { get; }
    Task<GeneratedKeyPair> GenerateKeyPairAsync(string pin, string subject, CancellationToken cancellationToken = default);
    Task<byte[]> SignDataAsync(byte[] dataToSign, string encryptedPrivateKey, string keySalt, string pin, CancellationToken cancellationToken = default);
    Task<bool> VerifySignatureAsync(byte[] data, byte[] signature, string publicKeyXmlOrPem, CancellationToken cancellationToken = default);
    Task<ChangedPinKeyResult> ChangePinAsync(string encryptedPrivateKey, string currentKeySalt, string currentPin, string newPin, CancellationToken cancellationToken = default);
}

public class ChangedPinKeyResult
{
    public string EncryptedPrivateKey { get; set; } = string.Empty;
    public string KeySalt { get; set; } = string.Empty;
    public string PinVerificationHash { get; set; } = string.Empty;
}
