using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Services.Security;
using Xunit;

namespace TradeFlow.UnitTests.Security;

public class RsaSoftwareSigningProviderTests
{
    private readonly RsaSoftwareSigningProvider _provider;

    public RsaSoftwareSigningProviderTests()
    {
        _provider = new RsaSoftwareSigningProvider();
    }

    [Fact]
    public async Task GenerateKeyPairAsync_CreatesValidRsa2048KeyPair_EncryptedWithPin()
    {
        // Arrange
        string pin = "123456";
        string subject = "CN=NGUYEN VAN A, POSITION=GIAM DOC, O=TRADEFLOW, MST=0101234567, C=VN";

        // Act
        var keyPair = await _provider.GenerateKeyPairAsync(pin, subject);

        // Assert
        keyPair.Should().NotBeNull();
        _provider.ProviderType.Should().Be(SigningProviderType.SoftwareRsa);
        keyPair.CertificateSerialNumber.Should().NotBeNullOrWhiteSpace();
        keyPair.CertificateThumbprint.Should().NotBeNullOrWhiteSpace();
        keyPair.EncryptedPrivateKey.Should().NotBeNullOrWhiteSpace();
        keyPair.KeySalt.Should().NotBeNullOrWhiteSpace();
        keyPair.PinVerificationHash.Should().NotBeNullOrWhiteSpace();
        keyPair.PublicKeyPem.Should().Contain("BEGIN PUBLIC KEY");
        keyPair.PublicKeyXml.Should().Contain("<RSAKeyValue>");

        // Encrypted private key should NOT contain plaintext PEM headers
        keyPair.EncryptedPrivateKey.Should().NotContain("BEGIN PRIVATE KEY");
        keyPair.EncryptedPrivateKey.Should().NotContain("BEGIN RSA PRIVATE KEY");
    }

    [Fact]
    public async Task SignDataAsync_WithCorrectPin_ProducesVerifiableSignature()
    {
        // Arrange
        string pin = "SecurePin123!";
        string subject = "CN=LE VAN B, O=TRADEFLOW, C=VN";
        var keyPair = await _provider.GenerateKeyPairAsync(pin, subject);

        byte[] data = Encoding.UTF8.GetBytes("INV-2026-001|2026-09-30|CUSTOMER_MST_0123|10000000|1000000|11000000");

        // Act
        byte[] signatureBytes = await _provider.SignDataAsync(
            data,
            keyPair.EncryptedPrivateKey,
            keyPair.KeySalt,
            pin);

        // Assert
        signatureBytes.Should().NotBeNullOrEmpty();

        // Verify with public key
        bool isValid = await _provider.VerifySignatureAsync(data, signatureBytes, keyPair.PublicKeyPem);
        isValid.Should().BeTrue("chữ ký được tạo với đúng mã PIN phải được xác thực hợp lệ bằng khóa công khai");
    }

    [Fact]
    public async Task SignDataAsync_WithWrongPin_ThrowsCryptographicException()
    {
        // Arrange
        string pin = "CorrectPin999";
        string wrongPin = "WrongPin111";
        var keyPair = await _provider.GenerateKeyPairAsync(pin, "CN=TEST");
        byte[] data = Encoding.UTF8.GetBytes("DOC_DIGEST_XYZ");

        // Act & Assert
        var act = async () => await _provider.SignDataAsync(
            data,
            keyPair.EncryptedPrivateKey,
            keyPair.KeySalt,
            wrongPin);

        await act.Should().ThrowAsync<CryptographicException>()
            .WithMessage("*Mã PIN*không chính xác*");
    }

    [Fact]
    public async Task VerifySignatureAsync_WhenDataAltered_ReturnsFalse()
    {
        // Arrange
        string pin = "ValidPin";
        var keyPair = await _provider.GenerateKeyPairAsync(pin, "CN=TEST");
        byte[] originalData = Encoding.UTF8.GetBytes("AMOUNT=10000000");
        byte[] tamperedData = Encoding.UTF8.GetBytes("AMOUNT=99000000"); // modified amount

        byte[] signatureBytes = await _provider.SignDataAsync(
            originalData,
            keyPair.EncryptedPrivateKey,
            keyPair.KeySalt,
            pin);

        // Act
        bool isValid = await _provider.VerifySignatureAsync(tamperedData, signatureBytes, keyPair.PublicKeyPem);

        // Assert
        isValid.Should().BeFalse("khi mã băm dữ liệu bị can thiệp sau khi ký, kết quả xác minh phải là FALSE");
    }
}
