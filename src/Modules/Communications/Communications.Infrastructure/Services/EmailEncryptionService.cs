using System.Security.Cryptography;
using System.Text;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Host.AllInOne.Services;

/// <summary>
/// Phase 6A.99: AES-256 encryption service for email addresses.
///
/// Purpose:
/// - Encrypt email addresses before storing in email_failure_details table
/// - GDPR compliance: personal data encrypted at rest
/// - Decrypt for authorized admin users with audit logging
///
/// Security Notes:
/// - Key is stored in Azure Key Vault (via app configuration)
/// - Uses AES-256-CBC with unique IV per encryption
/// - IV is prepended to ciphertext for decryption
///
/// Configuration:
/// - Encryption:EmailKey - Base64-encoded 32-byte key (256 bits)
/// - If key not configured, generates a random key (dev only, logs warning)
/// </summary>
public class EmailEncryptionService : IEmailEncryptionService
{
    private readonly byte[] _key;
    private readonly ILogger<EmailEncryptionService> _logger;

    public EmailEncryptionService(
        IConfiguration configuration,
        ILogger<EmailEncryptionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var keyBase64 = configuration["Encryption:EmailKey"];

        if (string.IsNullOrEmpty(keyBase64))
        {
            // Development fallback: generate a deterministic key (NOT for production)
            _logger.LogWarning(
                "[Phase 6A.99] Encryption:EmailKey not configured. Using development fallback key. " +
                "Configure Encryption:EmailKey in Azure Key Vault for production.");

            // Use a fixed development key (deterministic for dev testing)
            _key = SHA256.HashData(Encoding.UTF8.GetBytes("LankaConnect-Dev-EmailEncryption-Key-Do-Not-Use-In-Production"));
        }
        else
        {
            try
            {
                _key = Convert.FromBase64String(keyBase64);

                if (_key.Length != 32)
                {
                    _logger.LogError(
                        "[Phase 6A.99] Encryption:EmailKey must be exactly 32 bytes (256 bits). Got {ActualLength} bytes.",
                        _key.Length);
                    throw new InvalidOperationException($"Encryption key must be 32 bytes, got {_key.Length}");
                }

                _logger.LogInformation("[Phase 6A.99] Email encryption service initialized with configured key");
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "[Phase 6A.99] Encryption:EmailKey is not valid Base64");
                throw new InvalidOperationException("Encryption:EmailKey must be valid Base64", ex);
            }
        }
    }

    /// <inheritdoc />
    public string Encrypt(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return string.Empty;
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Generate random IV for each encryption
            aes.GenerateIV();
            var iv = aes.IV;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(email);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Prepend IV to encrypted data
            var result = new byte[iv.Length + encryptedBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.99] Failed to encrypt email");
            // Return empty rather than expose the email in error scenarios
            return string.Empty;
        }
    }

    /// <inheritdoc />
    public string Decrypt(string encryptedEmail)
    {
        if (string.IsNullOrEmpty(encryptedEmail))
        {
            return string.Empty;
        }

        try
        {
            var fullData = Convert.FromBase64String(encryptedEmail);

            if (fullData.Length < 17) // IV (16 bytes) + at least 1 byte of data
            {
                _logger.LogWarning("[Phase 6A.99] Encrypted data too short to contain IV");
                return string.Empty;
            }

            // Extract IV from first 16 bytes
            var iv = new byte[16];
            Buffer.BlockCopy(fullData, 0, iv, 0, 16);

            // Extract ciphertext
            var ciphertext = new byte[fullData.Length - 16];
            Buffer.BlockCopy(fullData, 16, ciphertext, 0, ciphertext.Length);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (FormatException)
        {
            _logger.LogWarning("[Phase 6A.99] Invalid Base64 in encrypted email");
            return string.Empty;
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(ex, "[Phase 6A.99] Failed to decrypt email - possibly wrong key or corrupted data");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.99] Unexpected error decrypting email");
            return string.Empty;
        }
    }
}
