using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Events;
using Email = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Domain.Users;

public class User : BaseEntity
{
    public Email Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public PhoneNumber? PhoneNumber { get; private set; }
    public string? Bio { get; private set; }
    public bool IsActive { get; private set; }
    
    // Authentication properties
    public string? PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerificationTokenExpiresAt { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiresAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? AccountLockedUntil { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    
    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public string FullName => $"{FirstName} {LastName}";

    // EF Core constructor
    private User() 
    {
        Email = null!;
        FirstName = null!;
        LastName = null!;
        Role = UserRole.User;
    }

    private User(Email email, string firstName, string lastName, UserRole role = UserRole.User)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
        IsActive = true;
        IsEmailVerified = false;
        FailedLoginAttempts = 0;
    }

    public static Result<User> Create(Email? email, string firstName, string lastName, UserRole role = UserRole.User)
    {
        if (email == null)
            return Result<User>.Failure("Email is required");

        if (string.IsNullOrWhiteSpace(firstName))
            return Result<User>.Failure("First name is required");

        if (string.IsNullOrWhiteSpace(lastName))
            return Result<User>.Failure("Last name is required");

        var user = new User(email, firstName.Trim(), lastName.Trim(), role);
        
        // Raise domain event
        user.RaiseDomainEvent(new UserCreatedEvent(user.Id, email.Value, user.FullName));
        
        return Result<User>.Success(user);
    }

    public Result UpdateProfile(string firstName, string lastName, PhoneNumber? phoneNumber, string? bio)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Failure("First name is required");

        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Failure("Last name is required");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber;
        Bio = bio?.Trim();
        
        MarkAsUpdated();
        return Result.Success();
    }

    public Result ChangeEmail(Email? newEmail)
    {
        if (newEmail == null)
            return Result.Failure("Email is required");

        Email = newEmail;
        MarkAsUpdated();
        return Result.Success();
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    // Authentication methods
    public Result SetPassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure("Password hash is required");

        PasswordHash = passwordHash;
        MarkAsUpdated();
        return Result.Success();
    }

    public Result ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            return Result.Failure("Password hash is required");

        PasswordHash = newPasswordHash;
        // Clear password reset token when password is changed
        PasswordResetToken = null;
        PasswordResetTokenExpiresAt = null;
        // Reset failed login attempts
        FailedLoginAttempts = 0;
        AccountLockedUntil = null;
        
        MarkAsUpdated();
        RaiseDomainEvent(new UserPasswordChangedEvent(Id, Email.Value));
        return Result.Success();
    }

    public Result VerifyEmail()
    {
        if (IsEmailVerified)
            return Result.Failure("Email is already verified");

        IsEmailVerified = true;
        EmailVerificationToken = null;
        EmailVerificationTokenExpiresAt = null;
        MarkAsUpdated();
        
        RaiseDomainEvent(new UserEmailVerifiedEvent(Id, Email.Value));
        return Result.Success();
    }

    public Result SetEmailVerificationToken(string token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure("Verification token is required");

        if (expiresAt <= DateTime.UtcNow)
            return Result.Failure("Expiration date must be in the future");

        EmailVerificationToken = token;
        EmailVerificationTokenExpiresAt = expiresAt;
        MarkAsUpdated();
        return Result.Success();
    }

    public Result SetPasswordResetToken(string token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure("Reset token is required");

        if (expiresAt <= DateTime.UtcNow)
            return Result.Failure("Expiration date must be in the future");

        PasswordResetToken = token;
        PasswordResetTokenExpiresAt = expiresAt;
        MarkAsUpdated();
        return Result.Success();
    }

    public Result AddRefreshToken(RefreshToken refreshToken)
    {
        if (refreshToken == null)
            return Result.Failure("Refresh token is required");

        // Remove expired tokens
        _refreshTokens.RemoveAll(rt => rt.IsExpired);

        // Limit the number of active refresh tokens per user
        const int maxRefreshTokens = 5;
        if (_refreshTokens.Count >= maxRefreshTokens)
        {
            // Remove the oldest token
            var oldestToken = _refreshTokens.OrderBy(rt => rt.CreatedAt).First();
            _refreshTokens.Remove(oldestToken);
        }

        _refreshTokens.Add(refreshToken);
        MarkAsUpdated();
        return Result.Success();
    }

    public Result RevokeRefreshToken(string token, string? revokedByIp = null)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(rt => rt.Token == token);
        if (refreshToken == null)
            return Result.Failure("Refresh token not found");

        if (refreshToken.IsRevoked)
            return Result.Failure("Token is already revoked");

        refreshToken.Revoke(revokedByIp);
        MarkAsUpdated();
        return Result.Success();
    }

    public void RevokeAllRefreshTokens(string? revokedByIp = null)
    {
        foreach (var token in _refreshTokens.Where(rt => rt.IsActive))
        {
            token.Revoke(revokedByIp);
        }
        MarkAsUpdated();
    }

    public RefreshToken? GetRefreshToken(string token)
    {
        return _refreshTokens.FirstOrDefault(rt => rt.Token == token && rt.IsActive);
    }

    public Result RecordFailedLoginAttempt()
    {
        FailedLoginAttempts++;
        
        // Lock account after 5 failed attempts for 30 minutes
        if (FailedLoginAttempts >= 5)
        {
            AccountLockedUntil = DateTime.UtcNow.AddMinutes(30);
            RaiseDomainEvent(new UserAccountLockedEvent(Id, Email.Value, AccountLockedUntil.Value));
        }
        
        MarkAsUpdated();
        return Result.Success();
    }

    public Result RecordSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        AccountLockedUntil = null;
        LastLoginAt = DateTime.UtcNow;
        MarkAsUpdated();
        
        RaiseDomainEvent(new UserLoggedInEvent(Id, Email.Value, LastLoginAt.Value));
        return Result.Success();
    }

    public bool IsAccountLocked => AccountLockedUntil.HasValue && AccountLockedUntil.Value > DateTime.UtcNow;

    public bool IsEmailVerificationTokenValid(string token)
    {
        return EmailVerificationToken == token && 
               EmailVerificationTokenExpiresAt.HasValue && 
               EmailVerificationTokenExpiresAt.Value > DateTime.UtcNow;
    }

    public bool IsPasswordResetTokenValid(string token)
    {
        return PasswordResetToken == token && 
               PasswordResetTokenExpiresAt.HasValue && 
               PasswordResetTokenExpiresAt.Value > DateTime.UtcNow;
    }

    public Result ChangeRole(UserRole newRole)
    {
        if (Role == newRole)
            return Result.Failure("User already has this role");

        var oldRole = Role;
        Role = newRole;
        MarkAsUpdated();
        
        RaiseDomainEvent(new UserRoleChangedEvent(Id, Email.Value, oldRole, newRole));
        return Result.Success();
    }
}