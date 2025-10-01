using LankaConnect.Domain.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace LankaConnect.Domain.Common.ValueObjects;

/// <summary>
/// Consolidated AlertSeverity value object with cultural intelligence capabilities
/// Canonical implementation replacing all duplicate AlertSeverity definitions
/// Supports diaspora community notifications and sacred event handling
/// </summary>
public sealed class AlertSeverity : ValueObject, IComparable<AlertSeverity>
{
    private static readonly Dictionary<int, AlertSeverity> _values = new();
    private static readonly Dictionary<string, AlertSeverity> _nameValues = new();

    /// <summary>
    /// Gets the numeric value of the alert severity (1-5)
    /// </summary>
    public int Value { get; private init; }

    /// <summary>
    /// Gets the name of the alert severity
    /// </summary>
    public string Name { get; private init; }

    /// <summary>
    /// Low priority alerts - informational only
    /// </summary>
    public static readonly AlertSeverity Low = new(1, "Low");

    /// <summary>
    /// Medium priority alerts - requires attention within hours
    /// </summary>
    public static readonly AlertSeverity Medium = new(2, "Medium");

    /// <summary>
    /// High priority alerts - requires attention within hour
    /// </summary>
    public static readonly AlertSeverity High = new(3, "High");

    /// <summary>
    /// Critical alerts - requires immediate attention
    /// </summary>
    public static readonly AlertSeverity Critical = new(4, "Critical");

    /// <summary>
    /// Sacred event alerts - highest priority for cultural intelligence
    /// Requires immediate attention for sacred/religious events in diaspora communities
    /// </summary>
    public static readonly AlertSeverity Sacred = new(5, "Sacred");

    /// <summary>
    /// Private constructor for creating AlertSeverity instances
    /// </summary>
    private AlertSeverity(int value, string name)
    {
        Value = value;
        Name = name;

        _values[value] = this;
        _nameValues[name] = this;
    }

    /// <summary>
    /// JSON constructor for deserialization
    /// </summary>
    [JsonConstructor]
    private AlertSeverity()
    {
        // Default values for JSON deserialization
        Value = 1;
        Name = "Low";
    }

    /// <summary>
    /// Creates an AlertSeverity from a numeric value
    /// </summary>
    /// <param name="value">The severity value (1-5)</param>
    /// <returns>The corresponding AlertSeverity</returns>
    /// <exception cref="ArgumentException">Thrown when value is invalid</exception>
    public static AlertSeverity FromValue(int value)
    {
        if (_values.TryGetValue(value, out var severity))
        {
            return severity;
        }

        throw new ArgumentException($"Invalid AlertSeverity value: {value}");
    }

    /// <summary>
    /// Creates an AlertSeverity from a name
    /// </summary>
    /// <param name="name">The severity name</param>
    /// <returns>The corresponding AlertSeverity</returns>
    /// <exception cref="ArgumentException">Thrown when name is invalid</exception>
    public static AlertSeverity FromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("AlertSeverity name cannot be null or empty");
        }

        if (_nameValues.TryGetValue(name, out var severity))
        {
            return severity;
        }

        throw new ArgumentException($"Invalid AlertSeverity name: {name}");
    }

    /// <summary>
    /// Gets all available alert severity levels
    /// </summary>
    /// <returns>All AlertSeverity instances in ascending order</returns>
    public static IEnumerable<AlertSeverity> GetAll()
    {
        return _values.Values.OrderBy(s => s.Value);
    }

    /// <summary>
    /// Gets the processing order for alerts (highest priority first)
    /// </summary>
    /// <returns>AlertSeverity instances in processing order (Sacred first, Low last)</returns>
    public static IEnumerable<AlertSeverity> GetProcessingOrder()
    {
        return _values.Values.OrderByDescending(s => s.Value);
    }

    /// <summary>
    /// Creates an escalation matrix with timeframes for each severity level
    /// </summary>
    /// <returns>Dictionary mapping severity to escalation timeframe</returns>
    public static Dictionary<AlertSeverity, TimeSpan> CreateEscalationMatrix()
    {
        return new Dictionary<AlertSeverity, TimeSpan>
        {
            [Sacred] = TimeSpan.FromMinutes(5),     // Sacred events need immediate attention
            [Critical] = TimeSpan.FromMinutes(30),   // Critical within 30 minutes
            [High] = TimeSpan.FromHours(1),         // High within 1 hour
            [Medium] = TimeSpan.FromHours(2),       // Medium within 2 hours
            [Low] = TimeSpan.FromHours(4)           // Low within 4 hours
        };
    }

    /// <summary>
    /// Determines if this severity represents a sacred event
    /// Sacred events require special cultural intelligence handling
    /// </summary>
    /// <returns>True if this is a sacred event severity</returns>
    public bool IsSacredEvent()
    {
        return this == Sacred;
    }

    /// <summary>
    /// Determines if this severity requires immediate attention
    /// </summary>
    /// <returns>True if severity is Critical or Sacred</returns>
    public bool RequiresImmediateAttention()
    {
        return Value >= Critical.Value;
    }

    /// <summary>
    /// Gets the notification priority for diaspora community notifications
    /// </summary>
    /// <returns>The corresponding notification priority</returns>
    public NotificationPriority GetNotificationPriority()
    {
        return Value switch
        {
            5 => NotificationPriority.Emergency,  // Sacred
            4 => NotificationPriority.High,       // Critical
            3 => NotificationPriority.Medium,     // High
            2 => NotificationPriority.Low,        // Medium
            1 => NotificationPriority.Info,       // Low
            _ => NotificationPriority.Info
        };
    }

    /// <summary>
    /// Compares this AlertSeverity with another for ordering
    /// </summary>
    /// <param name="other">The other AlertSeverity to compare with</param>
    /// <returns>Comparison result</returns>
    public int CompareTo(AlertSeverity? other)
    {
        if (other is null) return 1;
        return Value.CompareTo(other.Value);
    }

    /// <summary>
    /// Implicit conversion to int for backward compatibility
    /// </summary>
    /// <param name="severity">The AlertSeverity to convert</param>
    /// <returns>The numeric value</returns>
    public static implicit operator int(AlertSeverity severity)
    {
        return severity.Value;
    }

    /// <summary>
    /// Explicit conversion from int to AlertSeverity
    /// </summary>
    /// <param name="value">The numeric value to convert</param>
    /// <returns>The corresponding AlertSeverity</returns>
    public static explicit operator AlertSeverity(int value)
    {
        return FromValue(value);
    }

    /// <summary>
    /// Returns the string representation of this AlertSeverity
    /// </summary>
    /// <returns>The name of the severity level</returns>
    public override string ToString()
    {
        return Name;
    }

    /// <summary>
    /// Gets the values to be used for equality comparison
    /// </summary>
    /// <returns>The values for equality</returns>
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return Name;
    }
}

/// <summary>
/// Notification priority levels for diaspora community notifications
/// Maps to AlertSeverity for consistent priority handling
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Informational notifications
    /// </summary>
    Info = 1,

    /// <summary>
    /// Low priority notifications
    /// </summary>
    Low = 2,

    /// <summary>
    /// Medium priority notifications
    /// </summary>
    Medium = 3,

    /// <summary>
    /// High priority notifications
    /// </summary>
    High = 4,

    /// <summary>
    /// Emergency notifications for sacred events
    /// </summary>
    Emergency = 5
}