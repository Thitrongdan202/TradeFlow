namespace TradeFlow.Application.Common.Interfaces;

/// <summary>
/// Service for reversible encryption of sensitive data (e.g., admin-viewable passwords).
/// The encryption key must NEVER be stored in the database or frontend code.
/// Key is provided via secure configuration (environment variable / secrets).
/// </summary>
public interface IPasswordEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext value using a symmetric key from configuration.
    /// The ciphertext is safe to store in PostgreSQL.
    /// </summary>
    string Encrypt(string plaintext);

    /// <summary>
    /// Decrypts a ciphertext value. Must only be called by authorized administrators.
    /// Callers are responsible for auditing this operation.
    /// </summary>
    string Decrypt(string ciphertext);
}
