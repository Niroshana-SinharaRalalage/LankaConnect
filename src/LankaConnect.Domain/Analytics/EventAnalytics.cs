using LankaConnect.Domain.Common;
using LankaConnect.Domain.Analytics.DomainEvents;

namespace LankaConnect.Domain.Analytics;

/// <summary>
/// EventAnalytics Aggregate Root
/// Tracks analytics data for events (views, registrations, conversion rates)
/// Separate aggregate from Event to maintain different consistency boundaries
/// </summary>
public class EventAnalytics : BaseEntity
{
    public Guid EventId { get; private set; }
    public int TotalViews { get; private set; }
    public int UniqueViewers { get; private set; }
    public int RegistrationCount { get; private set; }
    public DateTime? LastViewedAt { get; private set; }

    /// <summary>
    /// Conversion rate: (Registrations / Views) * 100
    /// </summary>
    public decimal ConversionRate => TotalViews > 0
        ? Math.Round((decimal)RegistrationCount / TotalViews * 100, 2)
        : 0;

    // EF Core constructor
    private EventAnalytics() { }

    /// <summary>
    /// Factory method to create new EventAnalytics
    /// </summary>
    public static EventAnalytics Create(Guid eventId)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Event ID cannot be empty", nameof(eventId));

        var analytics = new EventAnalytics
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            TotalViews = 0,
            UniqueViewers = 0,
            RegistrationCount = 0,
            LastViewedAt = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return analytics;
    }

    /// <summary>
    /// Record a view of the event
    /// </summary>
    /// <param name="userId">User ID (null for anonymous views)</param>
    /// <param name="ipAddress">IP address of viewer (required)</param>
    public void RecordView(Guid? userId, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address is required", nameof(ipAddress));

        TotalViews++;
        LastViewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Raise domain event for background processing
        RaiseDomainEvent(new EventViewRecordedDomainEvent(EventId, userId, ipAddress));
    }

    /// <summary>
    /// Update unique viewer count (calculated asynchronously)
    /// </summary>
    public void UpdateUniqueViewers(int count)
    {
        if (count < 0)
            throw new ArgumentException("Unique viewer count cannot be negative", nameof(count));

        UniqueViewers = count;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update registration count (synced from Event aggregate)
    /// </summary>
    public void UpdateRegistrationCount(int count)
    {
        if (count < 0)
            throw new ArgumentException("Registration count cannot be negative", nameof(count));

        RegistrationCount = count;
        UpdatedAt = DateTime.UtcNow;
    }
}
