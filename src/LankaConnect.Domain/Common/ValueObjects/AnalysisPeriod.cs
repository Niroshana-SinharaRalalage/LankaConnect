using System;
using System.Collections.Generic;
using System.Linq;

namespace LankaConnect.Domain.Common.ValueObjects;

/// <summary>
/// Value object representing an analysis period with cultural intelligence awareness
/// for the LankaConnect platform. Provides validation and cultural optimization
/// for time-based analysis operations.
/// </summary>
public sealed class AnalysisPeriod : IEquatable<AnalysisPeriod>, IComparable<AnalysisPeriod>
{
    /// <summary>
    /// Gets the duration of the analysis period.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets the total hours in the analysis period.
    /// </summary>
    public double TotalHours => Duration.TotalHours;

    /// <summary>
    /// Gets the total days in the analysis period.
    /// </summary>
    public double TotalDays => Duration.TotalDays;

    /// <summary>
    /// Gets the total minutes in the analysis period.
    /// </summary>
    public double TotalMinutes => Duration.TotalMinutes;

    /// <summary>
    /// Gets whether this is a cultural analysis period (30, 90, or seasonal periods).
    /// </summary>
    public bool IsCulturalAnalysisPeriod => 
        TotalDays == 30 || TotalDays == 90 || IsCulturalSeasonalPeriod();

    /// <summary>
    /// Gets whether this is a security analysis period (typically 7 days).
    /// </summary>
    public bool IsSecurityAnalysisPeriod => Math.Abs(TotalDays - 7) < 0.1;

    /// <summary>
    /// Gets whether this is a real-time analysis period (typically 1 hour).
    /// </summary>
    public bool IsRealTimeAnalysisPeriod => Math.Abs(TotalHours - 1) < 0.1;

    /// <summary>
    /// Gets the cultural context associated with this period, if any.
    /// </summary>
    public string? CulturalContext { get; private set; }

    /// <summary>
    /// Gets whether this period has been culturally expanded.
    /// </summary>
    public bool HasCulturalExpansion { get; private set; }

    /// <summary>
    /// Private constructor for creating AnalysisPeriod instances.
    /// </summary>
    private AnalysisPeriod(TimeSpan duration, string? culturalContext = null, bool hasCulturalExpansion = false)
    {
        Duration = duration;
        CulturalContext = culturalContext;
        HasCulturalExpansion = hasCulturalExpansion;
    }

    /// <summary>
    /// Creates a new AnalysisPeriod with validation.
    /// </summary>
    /// <param name="duration">The duration of the analysis period.</param>
    /// <returns>A new AnalysisPeriod instance.</returns>
    /// <exception cref="ArgumentException">Thrown when duration is invalid.</exception>
    public static AnalysisPeriod Create(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            throw new ArgumentException("Analysis period cannot be negative", nameof(duration));

        if (duration == TimeSpan.Zero)
            throw new ArgumentException("Analysis period cannot be zero", nameof(duration));

        if (duration > TimeSpan.FromDays(365))
            throw new ArgumentException("Analysis period cannot exceed 365 days", nameof(duration));

        return new AnalysisPeriod(duration);
    }

    /// <summary>
    /// Creates an AnalysisPeriod from a number of days.
    /// </summary>
    /// <param name="days">The number of days.</param>
    /// <returns>A new AnalysisPeriod instance.</returns>
    public static AnalysisPeriod FromDays(int days)
    {
        return Create(TimeSpan.FromDays(days));
    }

    /// <summary>
    /// Creates an AnalysisPeriod from a number of hours.
    /// </summary>
    /// <param name="hours">The number of hours.</param>
    /// <returns>A new AnalysisPeriod instance.</returns>
    public static AnalysisPeriod FromHours(int hours)
    {
        return Create(TimeSpan.FromHours(hours));
    }

    /// <summary>
    /// Creates an AnalysisPeriod from a number of minutes.
    /// </summary>
    /// <param name="minutes">The number of minutes.</param>
    /// <returns>A new AnalysisPeriod instance.</returns>
    public static AnalysisPeriod FromMinutes(int minutes)
    {
        return Create(TimeSpan.FromMinutes(minutes));
    }

    /// <summary>
    /// Creates a standard cultural event analysis period (30 days).
    /// </summary>
    /// <returns>A new AnalysisPeriod for cultural events.</returns>
    public static AnalysisPeriod CulturalEventPeriod()
    {
        return new AnalysisPeriod(TimeSpan.FromDays(30), "CulturalEvent");
    }

    /// <summary>
    /// Creates a standard security incident analysis period (7 days).
    /// </summary>
    /// <returns>A new AnalysisPeriod for security incidents.</returns>
    public static AnalysisPeriod SecurityIncidentPeriod()
    {
        return new AnalysisPeriod(TimeSpan.FromDays(7), "SecurityIncident");
    }

    /// <summary>
    /// Creates a standard real-time monitoring period (1 hour).
    /// </summary>
    /// <returns>A new AnalysisPeriod for real-time monitoring.</returns>
    public static AnalysisPeriod RealTimeMonitoringPeriod()
    {
        return new AnalysisPeriod(TimeSpan.FromHours(1), "RealTimeMonitoring");
    }

    /// <summary>
    /// Creates a culturally optimized period for the given cultural event type.
    /// </summary>
    /// <param name="culturalEventType">The type of cultural event.</param>
    /// <returns>A new culturally optimized AnalysisPeriod.</returns>
    public AnalysisPeriod GetCulturallyOptimizedPeriod(string culturalEventType)
    {
        if (string.IsNullOrWhiteSpace(culturalEventType))
            return this;

        // Cultural events typically need longer analysis periods
        var optimizedDuration = culturalEventType.ToLowerInvariant() switch
        {
            var type when type.Contains("vesak") => TimeSpan.FromDays(Math.Max(TotalDays * 1.5, 14)),
            var type when type.Contains("ramadan") => TimeSpan.FromDays(Math.Max(TotalDays * 2.0, 30)),
            var type when type.Contains("diwali") => TimeSpan.FromDays(Math.Max(TotalDays * 1.3, 10)),
            var type when type.Contains("christmas") => TimeSpan.FromDays(Math.Max(TotalDays * 1.2, 7)),
            _ => TimeSpan.FromDays(Math.Max(TotalDays * 1.2, 7))
        };

        return new AnalysisPeriod(optimizedDuration, culturalEventType, true);
    }

    /// <summary>
    /// Expands the analysis period based on cultural significance.
    /// </summary>
    /// <param name="significanceMultiplier">The multiplier for cultural significance.</param>
    /// <returns>A new expanded AnalysisPeriod.</returns>
    public AnalysisPeriod ExpandForCulturalSignificance(double significanceMultiplier)
    {
        if (significanceMultiplier <= 0)
            throw new ArgumentException("Significance multiplier must be positive", nameof(significanceMultiplier));

        var expandedDuration = TimeSpan.FromTicks((long)(Duration.Ticks * significanceMultiplier));
        return new AnalysisPeriod(expandedDuration, CulturalContext, true);
    }

    /// <summary>
    /// Checks if the period falls within the specified date range.
    /// </summary>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">The end date.</param>
    /// <returns>True if the period duration is smaller than the range.</returns>
    public bool IsWithinRange(DateTime startDate, DateTime endDate)
    {
        var rangeDuration = endDate - startDate;
        return Duration <= rangeDuration;
    }

    /// <summary>
    /// Splits the analysis period into non-overlapping windows of the specified size.
    /// </summary>
    /// <param name="windowSize">The size of each analysis window.</param>
    /// <returns>A collection of analysis windows.</returns>
    public IEnumerable<AnalysisPeriod> GetAnalysisWindows(AnalysisPeriod windowSize)
    {
        if (windowSize.Duration >= Duration)
        {
            yield return this;
            yield break;
        }

        var windows = new List<AnalysisPeriod>();
        var totalWindows = (int)Math.Ceiling(TotalMinutes / windowSize.TotalMinutes);

        for (int i = 0; i < totalWindows; i++)
        {
            windows.Add(windowSize);
        }

        foreach (var window in windows.Distinct())
        {
            yield return window;
        }
    }

    /// <summary>
    /// Determines if this is a cultural seasonal period (e.g., festivals spanning months).
    /// </summary>
    private bool IsCulturalSeasonalPeriod()
    {
        // Consider periods that align with common cultural seasons
        return TotalDays >= 60 && TotalDays <= 120; // 2-4 months for seasonal festivals
    }

    /// <summary>
    /// Returns a string representation of the analysis period.
    /// </summary>
    public override string ToString()
    {
        if (TotalDays >= 1)
        {
            var days = (int)TotalDays;
            var dayText = days == 1 ? "day" : "days";
            return $"{days} {dayText}";
        }

        if (TotalHours >= 1)
        {
            var hours = (int)TotalHours;
            var hourText = hours == 1 ? "hour" : "hours";
            return $"{hours} {hourText}";
        }

        var minutes = (int)TotalMinutes;
        var minuteText = minutes == 1 ? "minute" : "minutes";
        return $"{minutes} {minuteText}";
    }

    #region Equality and Comparison

    /// <summary>
    /// Determines equality with another AnalysisPeriod.
    /// </summary>
    public bool Equals(AnalysisPeriod? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Duration.Equals(other.Duration);
    }

    /// <summary>
    /// Determines equality with another object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is AnalysisPeriod other && Equals(other);
    }

    /// <summary>
    /// Gets the hash code for this AnalysisPeriod.
    /// </summary>
    public override int GetHashCode()
    {
        return Duration.GetHashCode();
    }

    /// <summary>
    /// Compares this AnalysisPeriod to another.
    /// </summary>
    public int CompareTo(AnalysisPeriod? other)
    {
        if (other is null) return 1;
        return Duration.CompareTo(other.Duration);
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(AnalysisPeriod? left, AnalysisPeriod? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(AnalysisPeriod? left, AnalysisPeriod? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    /// Less than operator.
    /// </summary>
    public static bool operator <(AnalysisPeriod left, AnalysisPeriod right)
    {
        return left.CompareTo(right) < 0;
    }

    /// <summary>
    /// Greater than operator.
    /// </summary>
    public static bool operator >(AnalysisPeriod left, AnalysisPeriod right)
    {
        return left.CompareTo(right) > 0;
    }

    /// <summary>
    /// Less than or equal operator.
    /// </summary>
    public static bool operator <=(AnalysisPeriod left, AnalysisPeriod right)
    {
        return left.CompareTo(right) <= 0;
    }

    /// <summary>
    /// Greater than or equal operator.
    /// </summary>
    public static bool operator >=(AnalysisPeriod left, AnalysisPeriod right)
    {
        return left.CompareTo(right) >= 0;
    }

    #endregion
}