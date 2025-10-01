using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

public class CalendarDataSynchronization : ValueObject
{
    public string SynchronizationId { get; private set; }
    public EnterpriseClientId ClientId { get; private set; }
    public DateTime SyncDate { get; private set; }
    public IReadOnlyDictionary<GeographicRegion, int> RegionalEventCounts { get; private set; }
    public IReadOnlyDictionary<CulturalEventType, int> EventTypeCounts { get; private set; }
    public IReadOnlyList<string> SynchronizedCalendars { get; private set; }
    public IReadOnlyList<string> SyncErrors { get; private set; }
    public IReadOnlyList<string> ConflictResolutions { get; private set; }
    public double SyncSuccessRate { get; private set; }
    public TimeSpan SyncDuration { get; private set; }
    public string SyncStatus { get; private set; }
    public DateTime NextSyncScheduled { get; private set; }
    public IReadOnlyDictionary<string, object> SyncMetrics { get; private set; }
    public int TotalEventsProcessed { get; private set; }
    public int ConflictsDetected { get; private set; }
    public bool HasCriticalErrors => SyncErrors.Any(e => e.Contains("critical", StringComparison.OrdinalIgnoreCase));

    private CalendarDataSynchronization(
        string synchronizationId,
        EnterpriseClientId clientId,
        DateTime syncDate,
        IReadOnlyDictionary<GeographicRegion, int> regionalEventCounts,
        IReadOnlyDictionary<CulturalEventType, int> eventTypeCounts,
        IReadOnlyList<string> synchronizedCalendars,
        IReadOnlyList<string> syncErrors,
        IReadOnlyList<string> conflictResolutions,
        double syncSuccessRate,
        TimeSpan syncDuration,
        string syncStatus,
        DateTime nextSyncScheduled,
        IReadOnlyDictionary<string, object> syncMetrics,
        int totalEventsProcessed,
        int conflictsDetected)
    {
        SynchronizationId = synchronizationId;
        ClientId = clientId;
        SyncDate = syncDate;
        RegionalEventCounts = regionalEventCounts;
        EventTypeCounts = eventTypeCounts;
        SynchronizedCalendars = synchronizedCalendars;
        SyncErrors = syncErrors;
        ConflictResolutions = conflictResolutions;
        SyncSuccessRate = syncSuccessRate;
        SyncDuration = syncDuration;
        SyncStatus = syncStatus;
        NextSyncScheduled = nextSyncScheduled;
        SyncMetrics = syncMetrics;
        TotalEventsProcessed = totalEventsProcessed;
        ConflictsDetected = conflictsDetected;
    }

    public static CalendarDataSynchronization Create(
        string synchronizationId,
        EnterpriseClientId clientId,
        DateTime syncDate,
        IReadOnlyDictionary<GeographicRegion, int> regionalEventCounts,
        IReadOnlyDictionary<CulturalEventType, int> eventTypeCounts,
        IEnumerable<string> synchronizedCalendars,
        IEnumerable<string> syncErrors,
        IEnumerable<string> conflictResolutions,
        double syncSuccessRate,
        TimeSpan syncDuration,
        string syncStatus,
        DateTime nextSyncScheduled,
        IReadOnlyDictionary<string, object>? syncMetrics = null,
        int totalEventsProcessed = 0,
        int conflictsDetected = 0)
    {
        if (string.IsNullOrWhiteSpace(synchronizationId)) throw new ArgumentException("Synchronization ID is required", nameof(synchronizationId));
        if (clientId == null) throw new ArgumentNullException(nameof(clientId));
        if (syncDate > DateTime.UtcNow) throw new ArgumentException("Sync date cannot be in the future", nameof(syncDate));
        if (regionalEventCounts == null) throw new ArgumentNullException(nameof(regionalEventCounts));
        if (eventTypeCounts == null) throw new ArgumentNullException(nameof(eventTypeCounts));
        if (syncSuccessRate < 0 || syncSuccessRate > 1) throw new ArgumentException("Sync success rate must be between 0 and 1", nameof(syncSuccessRate));
        if (syncDuration < TimeSpan.Zero) throw new ArgumentException("Sync duration cannot be negative", nameof(syncDuration));
        if (string.IsNullOrWhiteSpace(syncStatus)) throw new ArgumentException("Sync status is required", nameof(syncStatus));
        if (nextSyncScheduled <= syncDate) throw new ArgumentException("Next sync must be scheduled after current sync date", nameof(nextSyncScheduled));
        if (totalEventsProcessed < 0) throw new ArgumentException("Total events processed cannot be negative", nameof(totalEventsProcessed));
        if (conflictsDetected < 0) throw new ArgumentException("Conflicts detected cannot be negative", nameof(conflictsDetected));

        var calendarsList = synchronizedCalendars?.ToList() ?? throw new ArgumentNullException(nameof(synchronizedCalendars));
        var errorsList = syncErrors?.ToList() ?? new List<string>();
        var resolutionsList = conflictResolutions?.ToList() ?? new List<string>();
        var metrics = syncMetrics ?? new Dictionary<string, object>();

        if (!calendarsList.Any()) throw new ArgumentException("At least one synchronized calendar is required", nameof(synchronizedCalendars));

        // Validate sync status
        var validStatuses = new[] { "Completed", "Failed", "Partial", "In Progress", "Pending", "Cancelled" };
        if (!validStatuses.Contains(syncStatus))
            throw new ArgumentException($"Sync status must be one of: {string.Join(", ", validStatuses)}", nameof(syncStatus));

        // Validate calendar names
        var validCalendarTypes = new[] { 
            "Gregorian", "Buddhist", "Hindu", "Islamic", "Tamil", "Sinhala", 
            "Christian", "Cultural", "National", "Regional", "Enterprise" 
        };
        
        foreach (var calendar in calendarsList)
        {
            if (string.IsNullOrWhiteSpace(calendar))
                throw new ArgumentException("Calendar names cannot be null or empty", nameof(synchronizedCalendars));
        }

        return new CalendarDataSynchronization(
            synchronizationId,
            clientId,
            syncDate,
            regionalEventCounts,
            eventTypeCounts,
            calendarsList.AsReadOnly(),
            errorsList.AsReadOnly(),
            resolutionsList.AsReadOnly(),
            syncSuccessRate,
            syncDuration,
            syncStatus,
            nextSyncScheduled,
            metrics,
            totalEventsProcessed,
            conflictsDetected);
    }

    public bool IsSyncSuccessful()
    {
        return SyncStatus == "Completed" && SyncSuccessRate >= 0.95 && !HasCriticalErrors;
    }

    public GeographicRegion GetMostActiveRegion()
    {
        return RegionalEventCounts.Any() 
            ? RegionalEventCounts.OrderByDescending(x => x.Value).First().Key
            : GeographicRegion.WesternProvince; // Default fallback - Western Province contains Colombo
    }

    public CulturalEventType GetMostCommonEventType()
    {
        return EventTypeCounts.Any()
            ? EventTypeCounts.OrderByDescending(x => x.Value).First().Key
            : CulturalEventType.Religious; // Default fallback
    }

    public int GetTotalEventsAcrossRegions()
    {
        return RegionalEventCounts.Values.Sum();
    }

    public double GetConflictRate()
    {
        return TotalEventsProcessed > 0 ? (double)ConflictsDetected / TotalEventsProcessed : 0;
    }

    public string GetSyncQuality()
    {
        if (!IsSyncSuccessful()) return "Poor";
        if (SyncSuccessRate >= 0.99 && GetConflictRate() <= 0.01) return "Excellent";
        if (SyncSuccessRate >= 0.97 && GetConflictRate() <= 0.03) return "Good";
        if (SyncSuccessRate >= 0.95 && GetConflictRate() <= 0.05) return "Fair";
        return "Poor";
    }

    public TimeSpan GetTimeUntilNextSync()
    {
        return NextSyncScheduled > DateTime.UtcNow 
            ? NextSyncScheduled - DateTime.UtcNow 
            : TimeSpan.Zero;
    }

    public IReadOnlyList<string> GetSyncRecommendations()
    {
        var recommendations = new List<string>();

        if (SyncSuccessRate < 0.90)
            recommendations.Add("Review sync configuration for reliability improvements");

        if (GetConflictRate() > 0.05)
            recommendations.Add("Implement enhanced conflict resolution strategies");

        if (SyncDuration > TimeSpan.FromMinutes(30))
            recommendations.Add("Optimize sync performance for faster execution");

        if (HasCriticalErrors)
            recommendations.Add("Address critical errors before next sync");

        if (SyncErrors.Count > 10)
            recommendations.Add("Investigate and resolve recurring sync errors");

        if (!RegionalEventCounts.Any())
            recommendations.Add("Verify regional calendar data sources");

        if (recommendations.Count == 0)
            recommendations.Add("Sync performing well, maintain current configuration");

        return recommendations.AsReadOnly();
    }

    public string GetSyncPerformanceGrade()
    {
        var score = 0.0;
        
        // Success rate weight: 40%
        score += SyncSuccessRate * 0.4;
        
        // Error rate weight: 30%
        var errorRate = SyncErrors.Count / Math.Max(1.0, TotalEventsProcessed);
        score += Math.Max(0, 1 - errorRate) * 0.3;
        
        // Conflict resolution weight: 20%
        var conflictResolutionRate = ConflictsDetected > 0 ? ConflictResolutions.Count / (double)ConflictsDetected : 1.0;
        score += Math.Min(1.0, conflictResolutionRate) * 0.2;
        
        // Speed weight: 10%
        var speedScore = SyncDuration <= TimeSpan.FromMinutes(5) ? 1.0 : 
                        SyncDuration <= TimeSpan.FromMinutes(15) ? 0.8 :
                        SyncDuration <= TimeSpan.FromMinutes(30) ? 0.6 : 0.4;
        score += speedScore * 0.1;

        return score switch
        {
            >= 0.95 => "A+",
            >= 0.90 => "A",
            >= 0.85 => "A-",
            >= 0.80 => "B+",
            >= 0.75 => "B",
            >= 0.70 => "B-",
            >= 0.65 => "C+",
            >= 0.60 => "C",
            >= 0.55 => "C-",
            >= 0.50 => "D",
            _ => "F"
        };
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return SynchronizationId;
        yield return ClientId;
        yield return SyncDate;
        yield return SyncSuccessRate;
        yield return SyncDuration;
        yield return SyncStatus;
        yield return NextSyncScheduled;
        yield return TotalEventsProcessed;
        yield return ConflictsDetected;
        
        foreach (var count in RegionalEventCounts.OrderBy(x => x.Key))
        {
            yield return count.Key;
            yield return count.Value;
        }
        
        foreach (var count in EventTypeCounts.OrderBy(x => x.Key))
        {
            yield return count.Key;
            yield return count.Value;
        }
        
        foreach (var calendar in SynchronizedCalendars)
            yield return calendar;
        
        foreach (var error in SyncErrors)
            yield return error;
        
        foreach (var resolution in ConflictResolutions)
            yield return resolution;
        
        foreach (var metric in SyncMetrics.OrderBy(x => x.Key))
        {
            yield return metric.Key;
            yield return metric.Value;
        }
    }
}