using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Value object representing Google Calendar API credentials with secure handling
/// </summary>
public sealed class GoogleCalendarCredentials : ValueObject
{
    public string AccessToken { get; }
    public string RefreshToken { get; }
    public DateTime ExpiresAt { get; }
    public string? Scope { get; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public TimeSpan TimeUntilExpiry => ExpiresAt - DateTime.UtcNow;

    private GoogleCalendarCredentials(
        string accessToken,
        string refreshToken,
        DateTime expiresAt,
        string? scope = null)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        Scope = scope;
    }

    /// <summary>
    /// Creates Google Calendar credentials with validation
    /// </summary>
    public static Result<GoogleCalendarCredentials> Create(
        string accessToken,
        string refreshToken,
        DateTime expiresAt,
        string? scope = null)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return Result<GoogleCalendarCredentials>.Failure("Access token cannot be empty");

        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result<GoogleCalendarCredentials>.Failure("Refresh token cannot be empty");

        if (expiresAt <= DateTime.UtcNow)
            return Result<GoogleCalendarCredentials>.Failure("Credentials are already expired");

        return Result<GoogleCalendarCredentials>.Success(
            new GoogleCalendarCredentials(accessToken, refreshToken, expiresAt, scope));
    }

    /// <summary>
    /// Creates credentials with standard Google Calendar scope
    /// </summary>
    public static Result<GoogleCalendarCredentials> CreateWithCalendarScope(
        string accessToken,
        string refreshToken,
        DateTime expiresAt)
    {
        return Create(accessToken, refreshToken, expiresAt, "https://www.googleapis.com/auth/calendar");
    }

    /// <summary>
    /// Updates credentials with new access token after refresh
    /// </summary>
    public GoogleCalendarCredentials UpdateAccessToken(string newAccessToken, DateTime newExpiresAt)
    {
        return new GoogleCalendarCredentials(newAccessToken, RefreshToken, newExpiresAt, Scope);
    }

    /// <summary>
    /// Determines if credentials need to be refreshed
    /// </summary>
    public bool NeedsRefresh(TimeSpan bufferTime = default)
    {
        if (bufferTime == default)
            bufferTime = TimeSpan.FromMinutes(5); // Default 5-minute buffer

        return DateTime.UtcNow.Add(bufferTime) >= ExpiresAt;
    }

    /// <summary>
    /// Gets authorization header value for API requests
    /// </summary>
    public string GetAuthorizationHeader()
    {
        if (IsExpired)
            throw new InvalidOperationException("Cannot use expired credentials");

        return $"Bearer {AccessToken}";
    }

    /// <summary>
    /// Validates that credentials have required calendar permissions
    /// </summary>
    public bool HasCalendarPermissions()
    {
        return !string.IsNullOrEmpty(Scope) && 
               (Scope.Contains("calendar") || Scope.Contains("https://www.googleapis.com/auth/calendar"));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return AccessToken;
        yield return RefreshToken;
        yield return ExpiresAt;
        yield return Scope ?? string.Empty;
    }
}

/// <summary>
/// Value object representing Google Calendar ID
/// </summary>
public sealed class GoogleCalendarId : ValueObject
{
    public string Value { get; }

    private GoogleCalendarId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a Google Calendar ID with validation
    /// </summary>
    public static Result<GoogleCalendarId> Create(string calendarId)
    {
        if (string.IsNullOrWhiteSpace(calendarId))
            return Result<GoogleCalendarId>.Failure("Calendar ID cannot be empty");

        // Basic validation for Google Calendar ID format
        if (!IsValidGoogleCalendarId(calendarId))
            return Result<GoogleCalendarId>.Failure("Invalid Google Calendar ID format");

        return Result<GoogleCalendarId>.Success(new GoogleCalendarId(calendarId));
    }

    /// <summary>
    /// Creates primary calendar ID
    /// </summary>
    public static GoogleCalendarId Primary()
    {
        return new GoogleCalendarId("primary");
    }

    /// <summary>
    /// Creates calendar ID from email address
    /// </summary>
    public static Result<GoogleCalendarId> FromEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<GoogleCalendarId>.Failure("Email cannot be empty");

        if (!email.Contains("@"))
            return Result<GoogleCalendarId>.Failure("Invalid email format");

        return Result<GoogleCalendarId>.Success(new GoogleCalendarId(email));
    }

    private static bool IsValidGoogleCalendarId(string calendarId)
    {
        // Google Calendar IDs can be:
        // - "primary" for primary calendar
        // - Email addresses
        // - Calendar-specific IDs (alphanumeric with specific patterns)
        
        if (calendarId == "primary")
            return true;

        if (calendarId.Contains("@") && calendarId.Contains("."))
            return true; // Basic email validation

        // Calendar-specific ID validation (simplified)
        return calendarId.Length > 10 && calendarId.All(c => char.IsLetterOrDigit(c) || c == '@' || c == '.' || c == '_' || c == '-');
    }

    public override string ToString() => Value;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

/// <summary>
/// Value object representing Google Calendar Event ID
/// </summary>
public sealed class GoogleCalendarEventId : ValueObject
{
    public string Value { get; }

    private GoogleCalendarEventId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a Google Calendar Event ID with validation
    /// </summary>
    public static Result<GoogleCalendarEventId> Create(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            return Result<GoogleCalendarEventId>.Failure("Event ID cannot be empty");

        // Google Calendar event IDs are typically lowercase alphanumeric
        if (!IsValidEventId(eventId))
            return Result<GoogleCalendarEventId>.Failure("Invalid Google Calendar event ID format");

        return Result<GoogleCalendarEventId>.Success(new GoogleCalendarEventId(eventId));
    }

    /// <summary>
    /// Generates a new event ID
    /// </summary>
    public static GoogleCalendarEventId Generate()
    {
        // Generate a Google Calendar compatible event ID
        var guid = Guid.NewGuid().ToString("N")[..26]; // First 26 characters
        return new GoogleCalendarEventId(guid.ToLower());
    }

    private static bool IsValidEventId(string eventId)
    {
        // Google Calendar event IDs are typically 26 characters, lowercase alphanumeric
        return eventId.Length <= 1024 && // Maximum length
               eventId.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
    }

    public override string ToString() => Value;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}