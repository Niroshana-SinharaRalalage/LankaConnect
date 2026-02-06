using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? Bio { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? AccountLockedUntil { get; set; }

    public string? EmailVerificationToken { get; set; }

    public DateTime? EmailVerificationTokenExpiresAt { get; set; }

    public int FailedLoginAttempts { get; set; }

    public bool IsEmailVerified { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public string? PasswordHash { get; set; }

    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    public int Role { get; set; }

    public string? ExternalProviderId { get; set; }

    public int IdentityProvider { get; set; }

    public string? ProfilePhotoBlobName { get; set; }

    public string? ProfilePhotoUrl { get; set; }

    public string? City { get; set; }

    public string? Country { get; set; }

    public string? State { get; set; }

    public string? ZipCode { get; set; }

    public int? PendingUpgradeRole { get; set; }

    public DateTime? UpgradeRequestedAt { get; set; }

    public virtual ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual ICollection<UserCulturalInterest> UserCulturalInterests { get; set; } = new List<UserCulturalInterest>();

    public virtual UserEmailPreference? UserEmailPreference { get; set; }

    public virtual ICollection<UserLanguage> UserLanguages { get; set; } = new List<UserLanguage>();

    public virtual ICollection<UserPreferredMetroArea> UserPreferredMetroAreas { get; set; } = new List<UserPreferredMetroArea>();

    public virtual ICollection<UserRefreshToken> UserRefreshTokens { get; set; } = new List<UserRefreshToken>();
}
