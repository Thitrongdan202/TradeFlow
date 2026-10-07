using System.Security.Cryptography;
using System.Text;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Infrastructure.Services.Security;

/// <summary>
/// Nhà cung cấp chữ ký số phần mềm RSA (PKCS#1 v1.5 với SHA-256).
/// Khóa riêng tư được bảo vệ nghiêm ngặt bằng mã hóa AES-GCM với khóa dẫn xuất PBKDF2 từ mã PIN người ký.
/// </summary>
public class RsaSoftwareSigningProvider : ISigningProvider
{
    private const int Pbkdf2Iterations = 100_000;
    private const int SaltSizeBytes = 16;
    private const int AesKeySizeBytes = 32; // 256 bits
    private const int GcmNonceSizeBytes = 12;
    private const int GcmTagSizeBytes = 16;

    public SigningProviderType ProviderType => SigningProviderType.SoftwareRsa;

    public Task<GeneratedKeyPair> GenerateKeyPairAsync(string pin, string subject, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pin) || pin.Length < 6)
        {
            throw new ArgumentException("Mã PIN phải có độ dài tối thiểu 6 ký tự.", nameof(pin));
        }

        using var rsa = RSA.Create(2048);

        // Export public key
        string publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
        string publicKeyXml = rsa.ToXmlString(false);

        // Generate serial number & thumbprint
        byte[] pubBytes = rsa.ExportSubjectPublicKeyInfo();
        string thumbprint = Convert.ToHexString(SHA256.HashData(pubBytes));
        string serialNumber = $"{DateTime.UtcNow:yyyyMMddHHmmss}{RandomNumberGenerator.GetInt32(1000, 9999)}";

        // Export private key
        byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();

        // Generate Salt
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        string saltBase64 = Convert.ToBase64String(salt);

        // Derive AES key using PBKDF2
        byte[] aesKey = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(pin),
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            AesKeySizeBytes);

        // Encrypt private key with AES-GCM
        byte[] nonce = RandomNumberGenerator.GetBytes(GcmNonceSizeBytes);
        byte[] ciphertext = new byte[privateKeyBytes.Length];
        byte[] tag = new byte[GcmTagSizeBytes];

        using (var aesGcm = new AesGcm(aesKey, GcmTagSizeBytes))
        {
            aesGcm.Encrypt(nonce, privateKeyBytes, ciphertext, tag);
        }

        // Pack [Nonce(12) + Tag(16) + Ciphertext]
        byte[] packed = new byte[GcmNonceSizeBytes + GcmTagSizeBytes + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, packed, 0, GcmNonceSizeBytes);
        Buffer.BlockCopy(tag, 0, packed, GcmNonceSizeBytes, GcmTagSizeBytes);
        Buffer.BlockCopy(ciphertext, 0, packed, GcmNonceSizeBytes + GcmTagSizeBytes, ciphertext.Length);

        string encryptedPrivateKeyBase64 = Convert.ToBase64String(packed);

        // Fast PIN verification hash: SHA256(salt + pin)
        byte[] pinCheckBytes = SHA256.HashData(Encoding.UTF8.GetBytes(saltBase64 + ":" + pin));
        string pinHashBase64 = Convert.ToBase64String(pinCheckBytes);

        // Zero out memory
        CryptographicOperations.ZeroMemory(privateKeyBytes);
        CryptographicOperations.ZeroMemory(aesKey);

        return Task.FromResult(new GeneratedKeyPair
        {
            PublicKeyXml = publicKeyXml,
            PublicKeyPem = publicKeyPem,
            EncryptedPrivateKey = encryptedPrivateKeyBase64,
            KeySalt = saltBase64,
            PinVerificationHash = pinHashBase64,
            CertificateSerialNumber = serialNumber,
            CertificateThumbprint = thumbprint
        });
    }

    public Task<byte[]> SignDataAsync(byte[] dataToSign, string encryptedPrivateKey, string keySalt, string pin, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pin))
        {
            throw new CryptographicException("Mã PIN người ký không được để trống.");
        }
        if (string.IsNullOrWhiteSpace(encryptedPrivateKey) || string.IsNullOrWhiteSpace(keySalt))
        {
            throw new CryptographicException("Dữ liệu khóa ký không hợp lệ hoặc bị thiếu.");
        }

        byte[] salt = Convert.FromBase64String(keySalt);
        byte[] packed = Convert.FromBase64String(encryptedPrivateKey);

        if (packed.Length < GcmNonceSizeBytes + GcmTagSizeBytes)
        {
            throw new CryptographicException("Dữ liệu khóa ký bị hỏng.");
        }

        // Derive AES key
        byte[] aesKey = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(pin),
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            AesKeySizeBytes);

        byte[] nonce = new byte[GcmNonceSizeBytes];
        byte[] tag = new byte[GcmTagSizeBytes];
        int ciphertextSize = packed.Length - GcmNonceSizeBytes - GcmTagSizeBytes;
        byte[] ciphertext = new byte[ciphertextSize];

        Buffer.BlockCopy(packed, 0, nonce, 0, GcmNonceSizeBytes);
        Buffer.BlockCopy(packed, GcmNonceSizeBytes, tag, 0, GcmTagSizeBytes);
        Buffer.BlockCopy(packed, GcmNonceSizeBytes + GcmTagSizeBytes, ciphertext, 0, ciphertextSize);

        byte[] decryptedPrivateKey = new byte[ciphertextSize];

        try
        {
            using (var aesGcm = new AesGcm(aesKey, GcmTagSizeBytes))
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, decryptedPrivateKey);
            }
        }
        catch (AuthenticationTagMismatchException)
        {
            CryptographicOperations.ZeroMemory(aesKey);
            throw new CryptographicException("Mã PIN người ký không chính xác.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(aesKey);
        }

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(decryptedPrivateKey, out _);
            byte[] signature = rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Task.FromResult(signature);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(decryptedPrivateKey);
        }
    }

    public Task<bool> VerifySignatureAsync(byte[] data, byte[] signature, string publicKeyXmlOrPem, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicKeyXmlOrPem) || signature.Length == 0)
        {
            return Task.FromResult(false);
        }

        try
        {
            using var rsa = RSA.Create();
            if (publicKeyXmlOrPem.Contains("BEGIN PUBLIC KEY") || publicKeyXmlOrPem.Contains("BEGIN RSA PUBLIC KEY"))
            {
                rsa.ImportFromPem(publicKeyXmlOrPem);
            }
            else
            {
                rsa.FromXmlString(publicKeyXmlOrPem);
            }

            bool isValid = rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Task.FromResult(isValid);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<ChangedPinKeyResult> ChangePinAsync(
        string encryptedPrivateKey,
        string currentKeySalt,
        string currentPin,
        string newPin,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currentPin))
        {
            throw new CryptographicException("Mã PIN hiện tại không được để trống.");
        }
        if (string.IsNullOrWhiteSpace(newPin) || newPin.Length < 6)
        {
            throw new ArgumentException("Mã PIN mới phải có tối thiểu 6 ký tự.", nameof(newPin));
        }
        if (string.IsNullOrWhiteSpace(encryptedPrivateKey) || string.IsNullOrWhiteSpace(currentKeySalt))
        {
            throw new CryptographicException("Dữ liệu khóa ký không hợp lệ hoặc bị thiếu.");
        }

        byte[] oldSalt = Convert.FromBase64String(currentKeySalt);
        byte[] oldPacked = Convert.FromBase64String(encryptedPrivateKey);

        if (oldPacked.Length < GcmNonceSizeBytes + GcmTagSizeBytes)
        {
            throw new CryptographicException("Dữ liệu khóa ký bị hỏng.");
        }

        // 1. Derive old AES key
        byte[] oldAesKey = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(currentPin),
            oldSalt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            AesKeySizeBytes);

        byte[] oldNonce = new byte[GcmNonceSizeBytes];
        byte[] oldTag = new byte[GcmTagSizeBytes];
        int ciphertextSize = oldPacked.Length - GcmNonceSizeBytes - GcmTagSizeBytes;
        byte[] oldCiphertext = new byte[ciphertextSize];

        Buffer.BlockCopy(oldPacked, 0, oldNonce, 0, GcmNonceSizeBytes);
        Buffer.BlockCopy(oldPacked, GcmNonceSizeBytes, oldTag, 0, GcmTagSizeBytes);
        Buffer.BlockCopy(oldPacked, GcmNonceSizeBytes + GcmTagSizeBytes, oldCiphertext, 0, ciphertextSize);

        byte[] decryptedPrivateKey = new byte[ciphertextSize];

        try
        {
            using (var aesGcm = new AesGcm(oldAesKey, GcmTagSizeBytes))
            {
                aesGcm.Decrypt(oldNonce, oldCiphertext, oldTag, decryptedPrivateKey);
            }
        }
        catch (AuthenticationTagMismatchException)
        {
            CryptographicOperations.ZeroMemory(oldAesKey);
            throw new CryptographicException("Mã PIN hiện tại không chính xác.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(oldAesKey);
        }

        try
        {
            // 2. Generate new salt and derive new AES key
            byte[] newSalt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            string newSaltBase64 = Convert.ToBase64String(newSalt);

            byte[] newAesKey = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(newPin),
                newSalt,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256,
                AesKeySizeBytes);

            byte[] newNonce = RandomNumberGenerator.GetBytes(GcmNonceSizeBytes);
            byte[] newCiphertext = new byte[decryptedPrivateKey.Length];
            byte[] newTag = new byte[GcmTagSizeBytes];

            using (var aesGcm = new AesGcm(newAesKey, GcmTagSizeBytes))
            {
                aesGcm.Encrypt(newNonce, decryptedPrivateKey, newCiphertext, newTag);
            }

            CryptographicOperations.ZeroMemory(newAesKey);

            byte[] newPacked = new byte[GcmNonceSizeBytes + GcmTagSizeBytes + newCiphertext.Length];
            Buffer.BlockCopy(newNonce, 0, newPacked, 0, GcmNonceSizeBytes);
            Buffer.BlockCopy(newTag, 0, newPacked, GcmNonceSizeBytes, GcmTagSizeBytes);
            Buffer.BlockCopy(newCiphertext, 0, newPacked, GcmNonceSizeBytes + GcmTagSizeBytes, newCiphertext.Length);

            string newEncryptedPrivateKeyBase64 = Convert.ToBase64String(newPacked);

            byte[] pinCheckBytes = SHA256.HashData(Encoding.UTF8.GetBytes(newSaltBase64 + ":" + newPin));
            string newPinHashBase64 = Convert.ToBase64String(pinCheckBytes);

            return Task.FromResult(new ChangedPinKeyResult
            {
                EncryptedPrivateKey = newEncryptedPrivateKeyBase64,
                KeySalt = newSaltBase64,
                PinVerificationHash = newPinHashBase64
            });
        }
        finally
        {
            CryptographicOperations.ZeroMemory(decryptedPrivateKey);
        }
    }
}
