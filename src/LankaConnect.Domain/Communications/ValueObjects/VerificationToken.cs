using System.Security.Cryptography;
using System.Text;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.ValueObjects;

public class VerificationToken : ValueObject
{
    public string Value { get; }
    public DateTime ExpiresAt { get; }

    private VerificationToken(string value, DateTime expiresAt)
    {
        Value = value;
        ExpiresAt = expiresAt;
    }

    public static Result<VerificationToken> Create(int validityHours = 24)
    {
        if (validityHours <= 0 || validityHours > 168) // Max 1 week
            return Result<VerificationToken>.Failure("Token validity must be between 1 and 168 hours");

        var token = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddHours(validityHours);

        return Result<VerificationToken>.Success(new VerificationToken(token, expiresAt));
    }

    public static Result<VerificationToken> FromExisting(string token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result<VerificationToken>.Failure("Token value is required");

        if (expiresAt <= DateTime.UtcNow)
            return Result<VerificationToken>.Failure("Token has expired");

        return Result<VerificationToken>.Success(new VerificationToken(token, expiresAt));
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public bool IsValid(string tokenToVerify)
    {
        return !IsExpired && 
               !string.IsNullOrEmpty(tokenToVerify) && 
               string.Equals(Value, tokenToVerify, StringComparison.Ordinal);
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32]; // 256-bit token
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return ExpiresAt;
    }
}