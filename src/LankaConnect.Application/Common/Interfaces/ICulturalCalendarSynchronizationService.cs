using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.Database;
// ARCHITECTURE FIX: Remove using alias to ensure exact signature matching

namespace LankaConnect.Application.Common.Interfaces;

public interface ICulturalCalendarSynchronizationService : IDisposable
{
    Task<Result<CulturalEventCalendar>> SynchronizeBuddhistCalendarAsync(
        List<string> regions,
        DateTime synchronizationDate,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalEventCalendar>> SynchronizeHinduCalendarAsync(
        List<string> regions,
        DateTime synchronizationDate,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalEventCalendar>> SynchronizeIslamicCalendarAsync(
        List<string> regions,
        DateTime synchronizationDate,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalEventCalendar>> SynchronizeSikhCalendarAsync(
        List<string> regions,
        DateTime synchronizationDate,
        CancellationToken cancellationToken = default);

    Task<Result<CrossRegionSynchronizationResult>> ValidateAstronomicalAccuracyAsync(
        CulturalEventType eventType,
        List<string> regions,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CulturalAuthoritySource>>> GetReligiousAuthoritySourcesAsync(
        CulturalDataType calendarType,
        string region,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalConflictResolution>> ResolveCalendarDiscrepancyAsync(
        CulturalEventType eventType,
        Dictionary<string, DateTime> regionalDates,
        CancellationToken cancellationToken = default);

    Task<Result<TimeSpan>> CalculateOptimalSynchronizationIntervalAsync(
        CulturalEventType eventType,
        List<string> regions,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> ValidateCalendarAuthorityAsync(
        CulturalAuthoritySource authority,
        CulturalDataType calendarType,
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<string, DateTime>>> GetNextCulturalEventDatesAsync(
        CulturalEventType eventType,
        List<string> regions,
        int monthsAhead,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalEventPrediction>> PredictCulturalEventImpactAsync(
        CulturalEventType eventType,
        string region,
        DateTime eventDate,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CulturalEventType>>> GetUpcomingCulturalEventsAsync(
        string region,
        TimeSpan timeWindow,
        CancellationToken cancellationToken = default);

    Task<Result> RegisterCulturalCalendarAuthorityAsync(
        CulturalAuthoritySource authority,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateRegionalCalendarPreferencesAsync(
        string region,
        Dictionary<CulturalEventType, LankaConnect.Domain.Common.CulturalSignificance> preferences,
        CancellationToken cancellationToken = default);

    Task<Result<double>> CalculateCalendarSynchronizationAccuracyAsync(
        CulturalEventType eventType,
        List<string> regions,
        TimeSpan evaluationPeriod,
        CancellationToken cancellationToken = default);
}