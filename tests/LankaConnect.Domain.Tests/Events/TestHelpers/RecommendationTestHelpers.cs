using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Events.ValueObjects.Recommendations;

namespace LankaConnect.Domain.Tests.Events.TestHelpers;

/// <summary>
/// Test helper class for Event Recommendation Engine tests
/// Contains mock objects and helper methods for testing
/// </summary>
public class TestUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CulturalBackground { get; set; } = "Sri Lankan";
    public string Location { get; set; } = "Fremont, CA";
    public int Age { get; set; } = 35;
    public Coordinates? Coordinates { get; set; }
}

/// <summary>
/// Helper methods for creating test events
/// </summary>
public static class EventTestHelpers
{
    public static Event CreateTestEvent(string title)
    {
        return Event.Create(
            EventTitle.Create(title).Value,
            EventDescription.Create($"Test event: {title}").Value,
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(3),
            Guid.NewGuid(),
            100
        ).Value;
    }
}

/// <summary>
/// Mock extensions for Event domain model to support recommendation testing
/// Note: These properties will need to be added to the actual Event model
/// </summary>
public static class EventTestExtensions
{
    // These properties will need to be added to the Event domain model
    public static string Category { get; set; } = "General";
    public static string CulturalCategory { get; set; } = "";
    public static string[] Languages { get; set; } = new[] { "English" };
    public static string[] TargetAgeGroups { get; set; } = new[] { "All Ages" };
    public static bool IsFamilyFriendly { get; set; } = true;
    public static bool IsAdultOnly { get; set; } = false;
    public static bool IsOnline { get; set; } = false;
    public static string Location { get; set; } = "";
    public static string[] TransportationOptions { get; set; } = Array.Empty<string>();
    public static ConflictType ConflictType { get; set; } = ConflictType.None;
    public static Priority Priority { get; set; } = Priority.Medium;
    public static CommitmentLevel RequiredCommitmentLevel { get; set; } = CommitmentLevel.Medium;
    public static string InvolvementType { get; set; } = "Casual";
}

/// <summary>
/// Mock cultural appropriateness with value wrapper
/// </summary>
public class CulturalAppropriateness
{
    public double Value { get; }
    
    public CulturalAppropriateness(double value)
    {
        Value = Math.Max(0.0, Math.Min(1.0, value));
    }

    // Static factory methods for common appropriateness levels
    public static CulturalAppropriateness HighlyAppropriate => new(0.95);
    public static CulturalAppropriateness Appropriate => new(0.75);
    public static CulturalAppropriateness Neutral => new(0.50);
    public static CulturalAppropriateness Questionable => new(0.25);
    public static CulturalAppropriateness Inappropriate => new(0.05);
}

/// <summary>
/// Mock helper methods for creating test events with various properties
/// These will fail until the Event model is extended with recommendation properties
/// </summary>
public static class EventTestHelpers
{
    public static Event CreateEventWithLocation(string title, string location)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.Location = location;
        return evt;
    }

    public static Event CreateEventWithCoordinates(string title, Coordinates coordinates)
    {
        var evt = CreateTestEvent(title);
        // Would need Coordinates property on Event
        return evt;
    }

    public static Event CreateEventWithTransportation(string title, string[] transportationOptions)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.TransportationOptions = transportationOptions;
        return evt;
    }

    public static Event CreateMultiLocationEvent(string title, string[] locations)
    {
        var evt = CreateTestEvent(title);
        // Would need multiple locations support
        return evt;
    }

    public static Event CreateOnlineEvent(string title)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.IsOnline = true;
        return evt;
    }

    public static Event CreateEventWithCategory(string title, string category)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.Category = category;
        return evt;
    }

    public static Event CreateEventWithCulturalCategory(string title, string culturalCategory)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.CulturalCategory = culturalCategory;
        return evt;
    }

    public static Event CreateEventWithLanguage(string title, string[] languages)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.Languages = languages;
        return evt;
    }

    public static Event CreateEventWithAgeTarget(string title, string[] targetAgeGroups)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.TargetAgeGroups = targetAgeGroups;
        return evt;
    }

    public static Event CreateEventWithTime(string title, int hour, int minute, DayOfWeek? dayOfWeek = null)
    {
        var baseDate = DateTime.UtcNow.AddDays(7);
        if (dayOfWeek.HasValue)
        {
            // Adjust to the specified day of week
            var daysToAdd = ((int)dayOfWeek.Value - (int)baseDate.DayOfWeek + 7) % 7;
            baseDate = baseDate.AddDays(daysToAdd);
        }
        
        var eventDate = baseDate.Date.AddHours(hour).AddMinutes(minute);
        
        return Event.Create(
            EventTitle.Create(title).Value,
            EventDescription.Create($"Test event: {title}").Value,
            eventDate,
            eventDate.AddHours(2),
            Guid.NewGuid(),
            100
        ).Value;
    }

    public static Event CreateFamilyEvent(string title)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.IsFamilyFriendly = true;
        EventTestExtensions.IsAdultOnly = false;
        return evt;
    }

    public static Event CreateAdultEvent(string title)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.IsFamilyFriendly = false;
        EventTestExtensions.IsAdultOnly = true;
        return evt;
    }

    public static Event CreateAllAgesEvent(string title)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.IsFamilyFriendly = true;
        EventTestExtensions.IsAdultOnly = false;
        return evt;
    }

    public static Event CreateChildrenEvent(string title)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.TargetAgeGroups = new[] { "Children", "Family" };
        return evt;
    }

    public static Event CreateVolunteerEvent(string title)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.InvolvementType = "Volunteer";
        EventTestExtensions.RequiredCommitmentLevel = CommitmentLevel.Medium;
        return evt;
    }

    public static Event CreateLeadershipEvent(string title)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.InvolvementType = "Leadership";
        EventTestExtensions.RequiredCommitmentLevel = CommitmentLevel.High;
        return evt;
    }

    public static Event CreateCasualEvent(string title)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.InvolvementType = "Casual";
        EventTestExtensions.RequiredCommitmentLevel = CommitmentLevel.Low;
        return evt;
    }

    public static Event CreateMembershipEvent(string title)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.InvolvementType = "Membership";
        EventTestExtensions.RequiredCommitmentLevel = CommitmentLevel.Medium;
        return evt;
    }

    public static Event CreateFoodEvent(string title)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.Category = "Food";
        return evt;
    }

    public static Event CreateBusinessEvent(string title)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.Category = "Business";
        return evt;
    }

    public static Event CreateEventWithConflict(string title, ConflictType conflictType, Priority priority)
    {
        var evt = CreateTestEvent(title);
        EventTestExtensions.ConflictType = conflictType;
        EventTestExtensions.Priority = priority;
        return evt;
    }

    public static Event CreateEventWithScore(string title, bool allScoresMax = false, bool allScoresMin = false)
    {
        var evt = CreateTestEvent(title);
        // Mock scoring properties would be set here
        return evt;
    }

    public static Event CreateEventWithMissingData(string title)
    {
        var evt = CreateTestEvent(title);
        // Mock missing data scenario
        EventTestExtensions.Location = ""; // Missing location
        return evt;
    }

    public static Event CreateEventWithInvalidData(string title)
    {
        var evt = CreateTestEvent(title);
        // Mock invalid data scenario
        return evt;
    }

    public static Event CreateEventWithExtremeValues(string title)
    {
        var evt = CreateTestEvent(title);
        // Mock extreme values scenario
        return evt;
    }

    public static Event CreateEventWithTiebreakingFactors(string title, double score, DateTime date, string category, int capacity)
    {
        var evt = Event.Create(
            EventTitle.Create(title).Value,
            EventDescription.Create($"Test event: {title}").Value,
            date,
            date.AddHours(3),
            Guid.NewGuid(),
            capacity
        ).Value;
        
        EventTestExtensions.Category = category;
        return evt;
    }

    private static Event CreateTestEvent(string title)
    {
        return Event.Create(
            EventTitle.Create(title).Value,
            EventDescription.Create($"Test event: {title}").Value,
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(30).AddHours(3),
            Guid.NewGuid(),
            100
        ).Value;
    }
}