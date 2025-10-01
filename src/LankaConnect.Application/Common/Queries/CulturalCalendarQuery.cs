using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Queries;

/// <summary>
/// Example cacheable query for Buddhist/Hindu calendar data
/// Demonstrates cache-aside pattern implementation for cultural intelligence
/// </summary>
public class CulturalCalendarQuery : IQuery<CulturalCalendarResponse>, ICacheableQuery
{
    public CalendarType CalendarType { get; set; }
    public DateTime Date { get; set; }
    public string GeographicRegion { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string? UserId { get; set; }

    public string GetCacheKey()
    {
        return CalendarType switch
        {
            CalendarType.Buddhist => CulturalCacheKey
                .ForBuddhistCalendar(GeographicRegion, Language, Date, UserId)
                .GenerateKey(),
            CalendarType.Hindu => CulturalCacheKey
                .ForHinduCalendar(GeographicRegion, Language, Date, UserId)
                .GenerateKey(),
            _ => throw new ArgumentException($"Unsupported calendar type: {CalendarType}")
        };
    }

    public TimeSpan GetCacheTtl()
    {
        // Calendar data is relatively stable - cache for 30 days
        // But user-specific personalization data should have shorter TTL
        return string.IsNullOrEmpty(UserId) 
            ? TimeSpan.FromDays(30) 
            : TimeSpan.FromDays(7);
    }

    public bool ShouldCache()
    {
        // Always cache calendar queries as they involve expensive astronomical calculations
        return true;
    }
}

public enum CalendarType
{
    Buddhist,
    Hindu
}

public class CulturalCalendarResponse
{
    public DateTime Date { get; set; }
    public CalendarType CalendarType { get; set; }
    public string FormattedDate { get; set; } = string.Empty;
    public List<CulturalEvent> Events { get; set; } = new();
    public List<string> Observations { get; set; } = new();
    public MoonPhase? MoonPhase { get; set; }
    public bool IsHoliday { get; set; }
    public bool IsAuspiciousDay { get; set; }
    public string CulturalContext { get; set; } = string.Empty;
}

public class CulturalEvent
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Significance { get; set; } = string.Empty;
    public List<string> Traditions { get; set; } = new();
    public string GeographicRelevance { get; set; } = string.Empty;
}

public class MoonPhase
{
    public string Phase { get; set; } = string.Empty;
    public double Illumination { get; set; }
    public string CulturalSignificance { get; set; } = string.Empty;
}