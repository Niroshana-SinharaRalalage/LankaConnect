using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Modules.Communications.Domain.Entities;

public class UserEmailPreferences : LegacyBaseEntity
{
    public Guid UserId { get; private set; }
    public bool AllowMarketing { get; private set; }
    public bool AllowNotifications { get; private set; }
    public bool AllowNewsletters { get; private set; }
    public bool AllowTransactional { get; private set; }
    public string? PreferredLanguage { get; private set; }
    public TimeZoneInfo? TimeZone { get; private set; }

    // For EF Core
    private UserEmailPreferences() { }

    private UserEmailPreferences(Guid userId)
    {
        UserId = userId;
        AllowMarketing = false;
        AllowNotifications = true;
        AllowNewsletters = true;
        AllowTransactional = true;
        PreferredLanguage = "en-US";
         // Initialize UpdatedAt timestamp
    }

    public static Result<UserEmailPreferences> Create(Guid userId)
    {
        if (userId == Guid.Empty)
            return Result<UserEmailPreferences>.Failure("User ID is required");

        var preferences = new UserEmailPreferences(userId);
        return Result<UserEmailPreferences>.Success(preferences);
    }

    public Result UpdateMarketingPreference(bool allow)
    {
        AllowMarketing = allow;
        return Result.Success();
    }

    public Result UpdateNotificationPreference(bool allow)
    {
        AllowNotifications = allow;
        return Result.Success();
    }

    public Result UpdateNewsletterPreference(bool allow)
    {
        AllowNewsletters = allow;
        return Result.Success();
    }

    public Result UpdateTransactionalPreference(bool allow)
    {
        // Transactional emails should always be allowed for security/verification
        if (!allow)
            return Result.Failure("Transactional emails cannot be disabled");

        AllowTransactional = allow;
        return Result.Success();
    }

    public Result UpdatePreferredLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return Result.Failure("Language is required");

        PreferredLanguage = language;
        return Result.Success();
    }
}