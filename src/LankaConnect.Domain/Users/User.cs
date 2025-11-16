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

    // Profile photo properties
    public string? ProfilePhotoUrl { get; private set; }
    public string? ProfilePhotoBlobName { get; private set; }

    // Location property (nullable - privacy choice)
    public UserLocation? Location { get; private set; }

    // Cultural preferences (collections)
    private readonly List<CulturalInterest> _culturalInterests = new();
    public IReadOnlyCollection<CulturalInterest> CulturalInterests => _culturalInterests.AsReadOnly();

    private readonly List<LanguagePreference> _languages = new();
    public IReadOnlyCollection<LanguagePreference> Languages => _languages.AsReadOnly();

    // Metro area preferences (Phase 5A)
    private readonly List<Guid> _preferredMetroAreaIds = new();
    public IReadOnlyList<Guid> PreferredMetroAreaIds => _preferredMetroAreaIds.AsReadOnly();

    // Phase 6A.9: EF Core shadow navigation property (infrastructure concern only)
    // This field exists ONLY for EF Core's use - managed entirely by infrastructure layer
    // Domain layer does NOT modify this - see UpdateUserPreferredMetroAreasCommandHandler
    // Per ADR-009: Shadow navigation accessed via ChangeTracker API in infrastructure layer
#pragma warning disable CS0169 // Field is used by EF Core via reflection
    private ICollection<Domain.Events.MetroArea>? _preferredMetroAreaEntities;
#pragma warning restore CS0169

    // Authentication properties
    public IdentityProvider IdentityProvider { get; private set; }
    public string? ExternalProviderId { get; private set; }
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

    // Phase 6A.0: Role upgrade tracking for Event Organizer approval workflow
    public UserRole? PendingUpgradeRole { get; private set; }
    public DateTime? UpgradeRequestedAt { get; private set; }

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    // External logins collection (for multi-provider social login)
    private readonly List<ExternalLogin> _externalLogins = new();
    public IReadOnlyCollection<ExternalLogin> ExternalLogins => _externalLogins.AsReadOnly();

    public string FullName => $"{FirstName} {LastName}";

    // EF Core constructor
    private User()
    {
        Email = null!;
        FirstName = null!;
        LastName = null!;
        Role = UserRole.GeneralUser;
        IdentityProvider = IdentityProvider.Local;
    }

    private User(Email email, string firstName, string lastName, UserRole role = UserRole.GeneralUser)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
        IsActive = true;
        IsEmailVerified = false;
        FailedLoginAttempts = 0;
        IdentityProvider = IdentityProvider.Local; // Default to Local for backward compatibility
    }

    public static Result<User> Create(Email? email, string firstName, string lastName, UserRole role = UserRole.GeneralUser)
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

    /// <summary>
    /// Creates a user from an external identity provider (e.g., Microsoft Entra External ID)
    /// Epic 1 Phase 2: Enhanced to support federated social providers (Facebook, Google, Apple)
    /// </summary>
    /// <param name="identityProvider">The identity provider (should be EntraExternal for social logins)</param>
    /// <param name="externalProviderId">The external user ID from Entra (OID claim)</param>
    /// <param name="email">User's email address</param>
    /// <param name="firstName">User's first name</param>
    /// <param name="lastName">User's last name</param>
    /// <param name="federatedProvider">The federated social provider (Microsoft/Facebook/Google/Apple)</param>
    /// <param name="providerEmail">Email from the social provider</param>
    /// <param name="role">User's role (defaults to User)</param>
    public static Result<User> CreateFromExternalProvider(
        IdentityProvider identityProvider,
        string? externalProviderId,
        Email? email,
        string firstName,
        string lastName,
        FederatedProvider federatedProvider,
        string? providerEmail = null,
        UserRole role = UserRole.GeneralUser)
    {
        // Validate that it's an external provider
        if (identityProvider == IdentityProvider.Local)
            return Result<User>.Failure("Cannot create user from external provider using Local identity provider");

        if (email == null)
            return Result<User>.Failure("Email is required");

        if (string.IsNullOrWhiteSpace(externalProviderId))
            return Result<User>.Failure("External provider ID is required for external providers");

        if (string.IsNullOrWhiteSpace(firstName))
            return Result<User>.Failure("First name is required");

        if (string.IsNullOrWhiteSpace(lastName))
            return Result<User>.Failure("Last name is required");

        var user = new User(email, firstName.Trim(), lastName.Trim(), role)
        {
            IdentityProvider = identityProvider,
            ExternalProviderId = externalProviderId.Trim(),
            IsEmailVerified = true, // External providers pre-verify emails
            PasswordHash = null // External providers manage passwords
        };

        // Epic 1 Phase 2: Automatically link the federated provider
        var linkResult = user.LinkExternalProvider(
            federatedProvider,
            externalProviderId.Trim(),
            providerEmail ?? email.Value);

        if (linkResult.IsFailure)
        {
            // This shouldn't happen for new users, but handle gracefully
            return Result<User>.Failure($"Failed to link external provider: {linkResult.Error}");
        }

        // Raise domain event specific to external provider creation
        user.RaiseDomainEvent(new UserCreatedFromExternalProviderEvent(
            user.Id,
            email.Value,
            user.FullName,
            identityProvider,
            externalProviderId.Trim()));

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
        // Business rule: External provider users cannot set passwords
        if (IdentityProvider.IsExternalProvider())
            return Result.Failure("Cannot set password for external provider users");

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure("Password hash is required");

        PasswordHash = passwordHash;
        MarkAsUpdated();
        return Result.Success();
    }

    public Result ChangePassword(string newPasswordHash)
    {
        // Business rule: External provider users cannot change passwords
        if (IdentityProvider.IsExternalProvider())
            return Result.Failure("Cannot change password for external provider users");

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

    /// <summary>
    /// Determines if this user uses local authentication
    /// </summary>
    public bool IsLocalProvider()
    {
        return IdentityProvider == IdentityProvider.Local;
    }

    /// <summary>
    /// Determines if this user uses an external identity provider
    /// </summary>
    public bool IsExternalProvider()
    {
        return IdentityProvider.IsExternalProvider();
    }

    /// <summary>
    /// Updates the user's profile photo
    /// </summary>
    /// <param name="url">Azure Blob Storage URL for the profile photo</param>
    /// <param name="blobName">Azure Blob Storage blob name for cleanup</param>
    public Result UpdateProfilePhoto(string url, string blobName)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Result.Failure("Profile photo URL is required");

        if (string.IsNullOrWhiteSpace(blobName))
            return Result.Failure("Profile photo blob name is required");

        ProfilePhotoUrl = url.Trim();
        ProfilePhotoBlobName = blobName.Trim();

        MarkAsUpdated();
        RaiseDomainEvent(new UserProfilePhotoUpdatedEvent(Id, ProfilePhotoUrl, ProfilePhotoBlobName));

        return Result.Success();
    }

    /// <summary>
    /// Removes the user's profile photo
    /// </summary>
    public Result RemoveProfilePhoto()
    {
        if (string.IsNullOrEmpty(ProfilePhotoUrl) || string.IsNullOrEmpty(ProfilePhotoBlobName))
            return Result.Failure("No profile photo to remove");

        var oldUrl = ProfilePhotoUrl;
        var oldBlobName = ProfilePhotoBlobName;

        ProfilePhotoUrl = null;
        ProfilePhotoBlobName = null;

        MarkAsUpdated();
        RaiseDomainEvent(new UserProfilePhotoRemovedEvent(Id, oldUrl, oldBlobName));

        return Result.Success();
    }

    /// <summary>
    /// Updates the user's location
    /// </summary>
    /// <param name="location">New location (nullable - user can choose not to share location for privacy)</param>
    /// <returns>Success result</returns>
    public Result UpdateLocation(UserLocation? location)
    {
        Location = location;
        MarkAsUpdated();

        // Only raise event if location is being set (not cleared)
        if (location != null)
        {
            RaiseDomainEvent(new UserLocationUpdatedEvent(
                Id,
                Email.Value,
                location.City,
                location.State,
                location.Country));
        }

        return Result.Success();
    }

    /// <summary>
    /// Updates user's cultural interests (0-10 allowed)
    /// Empty collection clears all interests (privacy choice)
    /// </summary>
    public Result UpdateCulturalInterests(IEnumerable<CulturalInterest>? interests)
    {
        var interestList = interests?.ToList() ?? new List<CulturalInterest>();

        // Validate max 10 interests
        if (interestList.Count > 10)
        {
            return Result.Failure("Cannot have more than 10 cultural interests");
        }

        // Clear and add interests (removes duplicates automatically via Distinct)
        _culturalInterests.Clear();
        _culturalInterests.AddRange(interestList.Distinct());

        MarkAsUpdated();

        // Only raise event if setting interests (not clearing)
        if (_culturalInterests.Any())
        {
            RaiseDomainEvent(new CulturalInterestsUpdatedEvent(Id, _culturalInterests.AsReadOnly()));
        }

        return Result.Success();
    }

    /// <summary>
    /// Updates user's language preferences (1-5 languages required)
    /// Per architect guidance: At least 1 language required, maximum 5
    /// </summary>
    public Result UpdateLanguages(IEnumerable<LanguagePreference>? languages)
    {
        var languageList = languages?.ToList() ?? new List<LanguagePreference>();

        // Validate minimum 1 language required
        if (!languageList.Any())
        {
            return Result.Failure("At least 1 language is required");
        }

        // Validate maximum 5 languages
        if (languageList.Count > 5)
        {
            return Result.Failure("Cannot have more than 5 languages");
        }

        // Clear and add languages (removes duplicates automatically via Distinct)
        _languages.Clear();
        _languages.AddRange(languageList.Distinct());

        MarkAsUpdated();

        // Always raise event (languages are required, not privacy choice like interests)
        RaiseDomainEvent(new LanguagesUpdatedEvent(Id, _languages.AsReadOnly()));

        return Result.Success();
    }

    /// <summary>
    /// Updates user's preferred metro areas for location-based filtering (0-20 allowed)
    /// Empty collection clears all preferences (privacy choice - user can opt out)
    /// Phase 5B: User Preferred Metro Areas - Expanded to 20 max limit
    /// Phase 6A.9: EF Core shadow navigation updated by infrastructure layer per ADR-009
    /// Architecture: Domain validates business rules, infrastructure handles persistence
    /// </summary>
    /// <param name="metroAreaIds">List of metro area GUIDs for domain logic</param>
    public Result UpdatePreferredMetroAreas(IEnumerable<Guid>? metroAreaIds)
    {
        var metroAreaList = metroAreaIds?.ToList() ?? new List<Guid>();

        // Validate max 20 metro areas (Phase 5B: Expanded from 10 to 20)
        if (metroAreaList.Count > 20)
            return Result.Failure("Cannot select more than 20 preferred metro areas");

        // Validate no duplicates
        if (metroAreaList.Distinct().Count() != metroAreaList.Count)
            return Result.Failure("Duplicate metro area IDs are not allowed");

        // Update domain collection (used by business logic)
        _preferredMetroAreaIds.Clear();
        _preferredMetroAreaIds.AddRange(metroAreaList.Distinct());

        // NOTE: _preferredMetroAreaEntities shadow navigation is updated by infrastructure layer
        // See UpdateUserPreferredMetroAreasCommandHandler - uses EF Core ChangeTracker API
        // Domain layer only maintains GUID list for business logic per ADR-009

        MarkAsUpdated();

        // Only raise event if setting preferences (not clearing for privacy)
        if (_preferredMetroAreaIds.Any())
            RaiseDomainEvent(new UserPreferredMetroAreasUpdatedEvent(Id, _preferredMetroAreaIds.AsReadOnly()));

        return Result.Success();
    }

    /// <summary>
    /// Phase 6A.9 FIX: Sync domain's metro area ID list from loaded EF Core entities
    /// This method is called ONLY by infrastructure layer after loading user from database
    /// Does NOT mark entity as updated or raise events - this is a hydration concern
    /// Per ADR-009: Domain maintains List&lt;Guid&gt; for business logic, EF Core has shadow navigation for persistence
    /// </summary>
    /// <param name="metroAreaIds">IDs extracted from loaded shadow navigation entities</param>
    internal void SyncPreferredMetroAreaIdsFromEntities(IEnumerable<Guid> metroAreaIds)
    {
        _preferredMetroAreaIds.Clear();
        _preferredMetroAreaIds.AddRange(metroAreaIds);
        // NOTE: Do NOT call MarkAsUpdated() - this is a read operation, not a modification
        // NOTE: Do NOT raise domain events - this is infrastructure hydration, not business operation
    }

    // External Login Management (Epic 1 Phase 2 - Social Login)

    /// <summary>
    /// Links an external social provider to the user's account
    /// Allows users to login with multiple social providers
    /// </summary>
    public Result LinkExternalProvider(
        FederatedProvider provider,
        string externalProviderId,
        string providerEmail)
    {
        // Validate external login can be created
        var externalLoginResult = ExternalLogin.Create(provider, externalProviderId, providerEmail);
        if (!externalLoginResult.IsSuccess)
            return Result.Failure(externalLoginResult.Error);

        var externalLogin = externalLoginResult.Value;

        // Business rule: Cannot link the same provider twice
        if (_externalLogins.Any(login => login.Provider == provider))
            return Result.Failure($"{provider.ToDisplayName()} is already linked to this account");

        // Add external login
        _externalLogins.Add(externalLogin);

        MarkAsUpdated();
        RaiseDomainEvent(new ExternalProviderLinkedEvent(
            Id,
            provider,
            externalProviderId,
            providerEmail));

        return Result.Success();
    }

    /// <summary>
    /// Unlinks an external social provider from the user's account
    /// Business rule: Cannot unlink if it's the last authentication method
    /// </summary>
    public Result UnlinkExternalProvider(FederatedProvider provider)
    {
        // Find the external login
        var externalLogin = _externalLogins.FirstOrDefault(login => login.Provider == provider);
        if (externalLogin == null)
            return Result.Failure($"{provider.ToDisplayName()} is not linked to this account");

        // Business rule: Prevent unlinking the last authentication method
        // User must have either:
        // 1. A password (local authentication), OR
        // 2. At least one other external provider
        var hasPassword = !string.IsNullOrEmpty(PasswordHash);
        var hasOtherProviders = _externalLogins.Count > 1;

        if (!hasPassword && !hasOtherProviders)
            return Result.Failure("Cannot unlink your last authentication method. Please set a password or link another provider first");

        // Remove external login
        _externalLogins.Remove(externalLogin);

        MarkAsUpdated();
        RaiseDomainEvent(new ExternalProviderUnlinkedEvent(
            Id,
            provider,
            externalLogin.ExternalProviderId));

        return Result.Success();
    }

    /// <summary>
    /// Checks if the user has linked a specific external provider
    /// </summary>
    public bool HasExternalLogin(FederatedProvider provider)
    {
        return _externalLogins.Any(login => login.Provider == provider);
    }

    /// <summary>
    /// Gets the external login for a specific provider (if linked)
    /// </summary>
    public ExternalLogin? GetExternalLogin(FederatedProvider provider)
    {
        return _externalLogins.FirstOrDefault(login => login.Provider == provider);
    }

    // Phase 6A.0: Role upgrade management methods

    /// <summary>
    /// Sets a pending role upgrade (e.g., Event Organizer) awaiting admin approval
    /// </summary>
    public Result SetPendingUpgradeRole(UserRole pendingRole)
    {
        // Business rule: Can only request upgrade if no pending request exists
        if (PendingUpgradeRole.HasValue)
            return Result.Failure("A role upgrade request is already pending");

        // Business rule: Cannot request downgrade or same role
        if (pendingRole <= Role)
            return Result.Failure("Can only request upgrade to a higher role");

        // Business rule: GeneralUser can only request upgrade to EventOrganizer
        if (Role == UserRole.GeneralUser && pendingRole != UserRole.EventOrganizer)
            return Result.Failure("General users can only request Event Organizer role");

        PendingUpgradeRole = pendingRole;
        UpgradeRequestedAt = DateTime.UtcNow;

        MarkAsUpdated();
        RaiseDomainEvent(new UserRoleUpgradeRequestedEvent(Id, Email.Value, pendingRole));

        return Result.Success();
    }

    /// <summary>
    /// Approves the pending role upgrade and changes the user's role
    /// </summary>
    public Result ApproveRoleUpgrade()
    {
        if (!PendingUpgradeRole.HasValue)
            return Result.Failure("No pending role upgrade to approve");

        var newRole = PendingUpgradeRole.Value;
        var oldRole = Role;

        Role = newRole;
        PendingUpgradeRole = null;
        UpgradeRequestedAt = null;

        MarkAsUpdated();
        RaiseDomainEvent(new UserRoleChangedEvent(Id, Email.Value, oldRole, newRole));

        return Result.Success();
    }

    /// <summary>
    /// Rejects the pending role upgrade request
    /// </summary>
    public Result RejectRoleUpgrade(string? reason = null)
    {
        if (!PendingUpgradeRole.HasValue)
            return Result.Failure("No pending role upgrade to reject");

        var rejectedRole = PendingUpgradeRole.Value;
        PendingUpgradeRole = null;
        UpgradeRequestedAt = null;

        MarkAsUpdated();
        RaiseDomainEvent(new UserRoleUpgradeRejectedEvent(Id, Email.Value, rejectedRole, reason));

        return Result.Success();
    }

    /// <summary>
    /// Cancels a pending role upgrade request (user-initiated)
    /// </summary>
    public Result CancelRoleUpgrade()
    {
        if (!PendingUpgradeRole.HasValue)
            return Result.Failure("No pending role upgrade to cancel");

        PendingUpgradeRole = null;
        UpgradeRequestedAt = null;

        MarkAsUpdated();
        return Result.Success();
    }

}