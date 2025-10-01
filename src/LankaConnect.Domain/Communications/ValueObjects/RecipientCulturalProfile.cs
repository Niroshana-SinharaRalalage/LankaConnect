using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Individual recipient's cultural profile for personalized email optimization
/// </summary>
public class RecipientCulturalProfile : ValueObject
{
    public SriLankanLanguage Language { get; }
    public CulturalBackground CulturalBackground { get; }
    public GeographicRegion GeographicLocation { get; }
    public TimeZoneInfo LocalTimeZone { get; }
    public IReadOnlyList<ReligiousContext> ObservedReligiousContexts { get; }

    public RecipientCulturalProfile(
        SriLankanLanguage Language,
        CulturalBackground CulturalBackground,
        GeographicRegion GeographicLocation,
        TimeZoneInfo? LocalTimeZone = null,
        IReadOnlyList<ReligiousContext>? ObservedReligiousContexts = null)
    {
        this.Language = Language;
        this.CulturalBackground = CulturalBackground;
        this.GeographicLocation = GeographicLocation;
        this.LocalTimeZone = LocalTimeZone ?? TimeZoneInfo.Utc;
        this.ObservedReligiousContexts = ObservedReligiousContexts ?? Array.Empty<ReligiousContext>();
    }

    public bool ObservesReligiousContext(ReligiousContext context)
    {
        return ObservedReligiousContexts.Contains(context);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Language;
        yield return CulturalBackground;
        yield return GeographicLocation;
        yield return LocalTimeZone.Id;
        
        foreach (var context in ObservedReligiousContexts.OrderBy(c => c))
            yield return context;
    }
}