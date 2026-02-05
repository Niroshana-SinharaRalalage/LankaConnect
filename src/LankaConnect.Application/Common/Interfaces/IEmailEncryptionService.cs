namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Phase 6A.99: Email encryption service interface for GDPR compliance.
///
/// Problem Solved:
/// - Email addresses in failure logs need to be stored securely
/// - GDPR requires protection of personal data at rest
/// - Admin debugging needs ability to decrypt when authorized
///
/// Usage:
/// - Encrypt email addresses before storing in database
/// - Decrypt for display to authorized admin users only
/// - All decryption access should be audit logged
/// </summary>
public interface IEmailEncryptionService
{
    /// <summary>
    /// Encrypts an email address for secure storage.
    /// Returns empty string if input is null or empty.
    /// </summary>
    /// <param name="email">Plain text email address</param>
    /// <returns>Base64-encoded encrypted email</returns>
    string Encrypt(string email);

    /// <summary>
    /// Decrypts an encrypted email address for display.
    /// Returns empty string if input is null or empty.
    /// Should only be called for authorized admin users with audit logging.
    /// </summary>
    /// <param name="encryptedEmail">Base64-encoded encrypted email</param>
    /// <returns>Plain text email address</returns>
    string Decrypt(string encryptedEmail);
}
