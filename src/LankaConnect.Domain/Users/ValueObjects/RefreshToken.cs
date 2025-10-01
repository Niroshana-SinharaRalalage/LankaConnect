using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Users.ValueObjects;

public class RefreshToken : ValueObject
{
    public string Token { get; }
    public DateTime ExpiresAt { get; }
    public DateTime CreatedAt { get; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string CreatedByIp { get; }

    private RefreshToken(string token, DateTime expiresAt, string createdByIp)
    {
        Token = token;
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
        CreatedByIp = createdByIp;
        IsRevoked = false;
    }

    public static Result<RefreshToken> Create(string? token, DateTime? expiresAt, string? createdByIp)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result<RefreshToken>.Failure("Token is required");

        if (expiresAt == null || expiresAt <= DateTime.UtcNow)
            return Result<RefreshToken>.Failure("Expiration date must be in the future");

        if (string.IsNullOrWhiteSpace(createdByIp))
            return Result<RefreshToken>.Failure("Created by IP is required");

        return Result<RefreshToken>.Success(new RefreshToken(token, expiresAt.Value, createdByIp));
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string? revokedByIp = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedByIp = revokedByIp;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Token;
        yield return ExpiresAt;
        yield return CreatedAt;
        yield return CreatedByIp;
    }
}