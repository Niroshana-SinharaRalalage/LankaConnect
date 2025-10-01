using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Value object representing cultural calendar synchronization configuration
/// Manages personalized cultural event integration with Google Calendar
/// </summary>
public sealed class CulturalCalendarSync : ValueObject
{
    public GoogleCalendarId CalendarId { get; }
    public CulturalProfile UserProfile { get; }
    public DiasporaLocation Location { get; }
    public SyncFrequency Frequency { get; }
    public LanguagePreference Language { get; }
    public CulturalEventFilter EventFilter { get; }
    public SyncConfiguration Configuration { get; }
    public DateTime LastSyncTime { get; }
    public SyncStatus Status { get; }

    private CulturalCalendarSync(
        GoogleCalendarId calendarId,
        CulturalProfile userProfile,
        DiasporaLocation location,
        SyncFrequency frequency,
        LanguagePreference language,
        CulturalEventFilter eventFilter,
        SyncConfiguration configuration,
        DateTime lastSyncTime,
        SyncStatus status)
    {
        CalendarId = calendarId;
        UserProfile = userProfile;
        Location = location;
        Frequency = frequency;
        Language = language;
        EventFilter = eventFilter;
        Configuration = configuration;
        LastSyncTime = lastSyncTime;
        Status = status;
    }

    /// <summary>
    /// Creates cultural calendar sync for Buddhist practitioners
    /// </summary>
    public static Result<CulturalCalendarSync> CreateBuddhistSync(
        GoogleCalendarId calendarId,
        DiasporaLocation location,
        LanguagePreference language,
        SyncFrequency frequency = SyncFrequency.Daily)
    {
        var buddhistProfile = CulturalProfile.CreateBuddhistProfile(location.ToString(), language);
        var buddhistFilter = CulturalEventFilter.CreateBuddhistFilter();
        var configuration = SyncConfiguration.CreateBuddhistConfiguration();

        return Result<CulturalCalendarSync>.Success(new CulturalCalendarSync(
            calendarId, buddhistProfile, location, frequency, language,
            buddhistFilter, configuration, DateTime.UtcNow, SyncStatus.Active));
    }

    /// <summary>
    /// Creates cultural calendar sync for Hindu practitioners
    /// </summary>
    public static Result<CulturalCalendarSync> CreateHinduSync(
        GoogleCalendarId calendarId,
        DiasporaLocation location,
        LanguagePreference language,
        SyncFrequency frequency = SyncFrequency.Daily)
    {
        var hinduProfile = CulturalProfile.CreateHinduProfile(location.ToString(), language);
        var hinduFilter = CulturalEventFilter.CreateHinduFilter();
        var configuration = SyncConfiguration.CreateHinduConfiguration();

        return Result<CulturalCalendarSync>.Success(new CulturalCalendarSync(
            calendarId, hinduProfile, location, frequency, language,
            hinduFilter, configuration, DateTime.UtcNow, SyncStatus.Active));
    }

    /// <summary>
    /// Creates comprehensive multi-cultural calendar sync
    /// </summary>
    public static Result<CulturalCalendarSync> CreateMultiCulturalSync(
        GoogleCalendarId calendarId,
        DiasporaLocation location,
        LanguagePreference language,
        IEnumerable<CulturalEventType> includedEventTypes,
        SyncFrequency frequency = SyncFrequency.Daily)
    {
        var multiCulturalProfile = CulturalProfile.CreateMultiCulturalProfile(location.ToString(), language);
        var customFilter = CulturalEventFilter.CreateCustomFilter(includedEventTypes);
        var configuration = SyncConfiguration.CreateMultiCulturalConfiguration();

        return Result<CulturalCalendarSync>.Success(new CulturalCalendarSync(
            calendarId, multiCulturalProfile, location, frequency, language,
            customFilter, configuration, DateTime.UtcNow, SyncStatus.Active));
    }

    /// <summary>
    /// Updates sync status after synchronization attempt
    /// </summary>
    public CulturalCalendarSync UpdateSyncStatus(SyncStatus newStatus, DateTime syncTime)
    {
        return new CulturalCalendarSync(
            CalendarId, UserProfile, Location, Frequency, Language,
            EventFilter, Configuration, syncTime, newStatus);
    }

    /// <summary>
    /// Determines if synchronization is due based on frequency
    /// </summary>
    public bool IsSyncDue()
    {
        var timeSinceLastSync = DateTime.UtcNow - LastSyncTime;
        
        return Frequency switch
        {
            SyncFrequency.Hourly => timeSinceLastSync.TotalHours >= 1,
            SyncFrequency.Daily => timeSinceLastSync.TotalDays >= 1,
            SyncFrequency.Weekly => timeSinceLastSync.TotalDays >= 7,
            SyncFrequency.Monthly => timeSinceLastSync.TotalDays >= 30,
            _ => true
        };
    }

    /// <summary>
    /// Gets cultural events that should be included in sync
    /// </summary>
    public IEnumerable<CulturalEventType> GetIncludedEventTypes()
    {
        return EventFilter.IncludedEventTypes;
    }

    /// <summary>
    /// Determines if event passes the cultural filter
    /// </summary>
    public bool ShouldIncludeEvent(GoogleCalendarCulturalEvent culturalEvent)
    {
        // Check event type filter
        if (!EventFilter.IncludedEventTypes.Contains(culturalEvent.EventType))
            return false;

        // Check significance level filter
        if (culturalEvent.Significance < EventFilter.MinimumSignificance)
            return false;

        // Check diaspora relevance
        if (!EventFilter.IncludeGlobalEvents && 
            !culturalEvent.DiasporaRelevance.IsRelevantForLocation(Location.ToString()))
            return false;

        return true;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CalendarId;
        yield return UserProfile;
        yield return Location;
        yield return Frequency;
        yield return Language;
        yield return EventFilter;
        yield return Configuration;
    }
}

/// <summary>
/// Synchronization frequency options
/// </summary>
public enum SyncFrequency
{
    Hourly,
    Daily,
    Weekly,
    Monthly,
    Manual
}

/// <summary>
/// Current synchronization status
/// </summary>
public enum SyncStatus
{
    Active,
    Paused,
    Error,
    Inactive,
    Pending
}

/// <summary>
/// Cultural event filtering configuration
/// </summary>
public sealed class CulturalEventFilter : ValueObject
{
    public IEnumerable<CulturalEventType> IncludedEventTypes { get; }
    public CulturalSignificance MinimumSignificance { get; }
    public bool IncludeGlobalEvents { get; }
    public bool IncludeTempleEvents { get; }
    public bool IncludeCommunityEvents { get; }

    private CulturalEventFilter(
        IEnumerable<CulturalEventType> includedEventTypes,
        CulturalSignificance minimumSignificance,
        bool includeGlobalEvents,
        bool includeTempleEvents,
        bool includeCommunityEvents)
    {
        IncludedEventTypes = includedEventTypes;
        MinimumSignificance = minimumSignificance;
        IncludeGlobalEvents = includeGlobalEvents;
        IncludeTempleEvents = includeTempleEvents;
        IncludeCommunityEvents = includeCommunityEvents;
    }

    public static CulturalEventFilter CreateBuddhistFilter()
    {
        var buddhistEvents = new[]
        {
            CulturalEventType.VesakPoya,
            CulturalEventType.Poyaday,
            CulturalEventType.EsalaPerahera,
            CulturalEventType.UnduvapPoya,
            CulturalEventType.BuddhaPurnima,
            CulturalEventType.SinhalaNewYear
        };

        return new CulturalEventFilter(
            buddhistEvents, CulturalSignificance.Medium, true, true, true);
    }

    public static CulturalEventFilter CreateHinduFilter()
    {
        var hinduEvents = new[]
        {
            CulturalEventType.Deepavali,
            CulturalEventType.Thaipusam,
            CulturalEventType.Navaratri,
            CulturalEventType.Holi,
            CulturalEventType.TamilNewYear
        };

        return new CulturalEventFilter(
            hinduEvents, CulturalSignificance.Medium, true, true, true);
    }

    public static CulturalEventFilter CreateCustomFilter(IEnumerable<CulturalEventType> eventTypes)
    {
        return new CulturalEventFilter(
            eventTypes, CulturalSignificance.Low, true, true, true);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return string.Join(",", IncludedEventTypes.OrderBy(e => e));
        yield return MinimumSignificance;
        yield return IncludeGlobalEvents;
        yield return IncludeTempleEvents;
        yield return IncludeCommunityEvents;
    }
}

/// <summary>
/// Synchronization behavior configuration
/// </summary>
public sealed class SyncConfiguration : ValueObject
{
    public bool CreateReminders { get; }
    public int ReminderDaysBefore { get; }
    public bool UseMultilingualDescriptions { get; }
    public bool EnableConflictDetection { get; }
    public bool AutoResolveConflicts { get; }
    public bool IncludePreparationPeriods { get; }

    private SyncConfiguration(
        bool createReminders,
        int reminderDaysBefore,
        bool useMultilingualDescriptions,
        bool enableConflictDetection,
        bool autoResolveConflicts,
        bool includePreparationPeriods)
    {
        CreateReminders = createReminders;
        ReminderDaysBefore = reminderDaysBefore;
        UseMultilingualDescriptions = useMultilingualDescriptions;
        EnableConflictDetection = enableConflictDetection;
        AutoResolveConflicts = autoResolveConflicts;
        IncludePreparationPeriods = includePreparationPeriods;
    }

    public static SyncConfiguration CreateBuddhistConfiguration()
    {
        return new SyncConfiguration(true, 3, true, true, true, true);
    }

    public static SyncConfiguration CreateHinduConfiguration()
    {
        return new SyncConfiguration(true, 7, true, true, true, true);
    }

    public static SyncConfiguration CreateMultiCulturalConfiguration()
    {
        return new SyncConfiguration(true, 5, true, true, false, true);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CreateReminders;
        yield return ReminderDaysBefore;
        yield return UseMultilingualDescriptions;
        yield return EnableConflictDetection;
        yield return AutoResolveConflicts;
        yield return IncludePreparationPeriods;
    }
}