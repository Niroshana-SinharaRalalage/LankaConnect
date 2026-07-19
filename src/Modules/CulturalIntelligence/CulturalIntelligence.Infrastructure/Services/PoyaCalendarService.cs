using System.Reflection;
using System.Text.Json;
using LankaConnect.Modules.CulturalIntelligence.Contracts.Services;

namespace LankaConnect.Modules.CulturalIntelligence.Infrastructure.Services;

/// <summary>
/// Real seed-file-backed implementation of <see cref="ICulturalCalendar"/> covering
/// the Sri Lankan Buddhist poya-day (full-moon) calendar. Closes Wave 8.5 GAP-1 by
/// retiring <c>StubCulturalCalendar</c>'s hardcoded return values.
///
/// Data source: <c>Services/poya-calendar.json</c> embedded as an assembly resource
/// during build (see csproj). Ships with dates for calendar years 2026, 2027, 2028.
/// Refresh the JSON annually against the Sri Lankan Government Almanac; this service
/// requires no code change to consume additional years — extend the <c>years</c>
/// dictionary in the JSON.
///
/// Cultural-context semantics implemented:
/// - <see cref="IsPoyaDay(DateTime)"/>: exact-date match against the seeded poya list.
/// - <see cref="GetEventAppropriateness"/>: religious/mixed events on poya days score 0.9;
///   secular/party-style events on major poya days (Vesak, Poson, Esala, Unduwap)
///   score 0.35 (conflict signal); everything else scores 0.7 (neutral).
/// - <see cref="ClassifyEventType"/>: passes the caller-supplied Category through
///   when non-null; falls back to "General".
/// - <see cref="GetDiasporaFriendliness"/>: religious/major-poya events default to
///   Moderate (diaspora often observe silently); Cultural events default to High;
///   Secular defaults to VeryHigh.
/// - <see cref="CalculateAppropriateness"/>: composes GetEventAppropriateness with a
///   background modifier (Buddhist / Hindu backgrounds get +0.1 on religious/mixed).
/// - <see cref="GetFestivalPeriod"/>: named-poya lookup + 1-day window (Vesak = 3-day
///   window per government practice).
/// - <see cref="IsOptimalFestivalTiming"/>: event StartDate ∈ [period.StartDate,
///   period.EndDate].
/// - <see cref="ClassifyEventNature"/>: category-driven (Religious / Cultural / Secular
///   / Mixed).
/// - <see cref="GetSignificantDates"/>: all seeded poya dates for the year, tagged
///   Critical for major poyas, High otherwise.
/// - <see cref="ValidateEventAgainstCalendar"/>: reports a suggestion when a Secular
///   event is scheduled on a major poya day.
/// </summary>
public sealed class PoyaCalendarService : ICulturalCalendar
{
    private const string SeedResourceName =
        "LankaConnect.Modules.CulturalIntelligence.Infrastructure.Services.poya-calendar.json";

    private static readonly IReadOnlyDictionary<int, IReadOnlyList<PoyaSeedEntry>> _byYear = LoadSeed();
    private static readonly HashSet<DateTime> _allPoyaDates = _byYear
        .SelectMany(kvp => kvp.Value.Select(p => p.Date.Date))
        .ToHashSet();

    public bool IsPoyaDay(DateTime date) => _allPoyaDates.Contains(date.Date);

    public CulturalAppropriateness GetEventAppropriateness(EventCulturalContext context, DateTime date)
    {
        var isPoya = IsPoyaDay(date);
        if (!isPoya) return new CulturalAppropriateness(0.7);

        var poya = LookupPoyaOn(date);
        var isMajor = poya?.IsMajor ?? false;
        var nature = ClassifyEventNature(context);

        return nature switch
        {
            EventNature.Religious => new CulturalAppropriateness(0.95),
            EventNature.Mixed => new CulturalAppropriateness(0.85),
            EventNature.Cultural => new CulturalAppropriateness(0.75),
            EventNature.Secular when isMajor => new CulturalAppropriateness(0.35),
            EventNature.Secular => new CulturalAppropriateness(0.55),
            _ => new CulturalAppropriateness(0.7),
        };
    }

    public string ClassifyEventType(EventCulturalContext context)
        => string.IsNullOrWhiteSpace(context.Category) ? "General" : context.Category!;

    public DiasporaFriendliness GetDiasporaFriendliness(EventCulturalContext context)
        => ClassifyEventNature(context) switch
        {
            EventNature.Religious => DiasporaFriendliness.Moderate,
            EventNature.Cultural => DiasporaFriendliness.High,
            EventNature.Secular => DiasporaFriendliness.VeryHigh,
            EventNature.Mixed => DiasporaFriendliness.High,
            _ => DiasporaFriendliness.Moderate,
        };

    public CulturalAppropriateness CalculateAppropriateness(EventCulturalContext context, string culturalBackground)
    {
        var basis = context.StartDate.HasValue
            ? GetEventAppropriateness(context, context.StartDate.Value)
            : new CulturalAppropriateness(0.7);

        var modifier = culturalBackground switch
        {
            "Buddhist" or "Hindu" when ClassifyEventNature(context) is EventNature.Religious or EventNature.Mixed => 0.10,
            "Secular" when ClassifyEventNature(context) is EventNature.Secular => 0.05,
            _ => 0.0,
        };

        return new CulturalAppropriateness(Math.Min(1.0, basis.Value + modifier));
    }

    public FestivalPeriod GetFestivalPeriod(string festivalName, int year)
    {
        if (!_byYear.TryGetValue(year, out var yearPoyas))
        {
            // Year not seeded — return a neutral, empty period so callers don't crash.
            var fallback = new DateTime(year, 1, 1);
            return new FestivalPeriod(fallback, fallback, festivalName);
        }

        var match = yearPoyas.FirstOrDefault(p =>
            string.Equals(p.Type, festivalName, StringComparison.OrdinalIgnoreCase) ||
            p.EnglishName.Contains(festivalName, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            var fallback = new DateTime(year, 1, 1);
            return new FestivalPeriod(fallback, fallback, festivalName);
        }

        // Major poyas (Vesak/Poson/Esala/Unduwap) get a 3-day window per government
        // holiday practice; monthly poyas get a 1-day window.
        var windowDays = match.IsMajor ? 3 : 1;
        var start = match.Date.AddDays(-(windowDays - 1) / 2.0);
        var end = start.AddDays(windowDays - 1);
        return new FestivalPeriod(start, end, match.EnglishName);
    }

    public bool IsOptimalFestivalTiming(EventCulturalContext context, FestivalPeriod period)
    {
        if (context.StartDate is not { } eventStart) return false;
        return eventStart >= period.StartDate && eventStart <= period.EndDate;
    }

    public EventNature ClassifyEventNature(EventCulturalContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Category)) return EventNature.Cultural;
        var cat = context.Category!.Trim();
        if (cat.Equals("Religious", StringComparison.OrdinalIgnoreCase)) return EventNature.Religious;
        if (cat.Equals("Cultural", StringComparison.OrdinalIgnoreCase)) return EventNature.Cultural;
        if (cat.Equals("Secular", StringComparison.OrdinalIgnoreCase)) return EventNature.Secular;
        if (cat.Equals("Mixed", StringComparison.OrdinalIgnoreCase)) return EventNature.Mixed;

        // Common word matches
        if (cat.Contains("temple", StringComparison.OrdinalIgnoreCase) ||
            cat.Contains("dhamma", StringComparison.OrdinalIgnoreCase) ||
            cat.Contains("puja", StringComparison.OrdinalIgnoreCase))
            return EventNature.Religious;
        if (cat.Contains("dance", StringComparison.OrdinalIgnoreCase) ||
            cat.Contains("music", StringComparison.OrdinalIgnoreCase) ||
            cat.Contains("food", StringComparison.OrdinalIgnoreCase))
            return EventNature.Cultural;
        if (cat.Contains("party", StringComparison.OrdinalIgnoreCase) ||
            cat.Contains("bar", StringComparison.OrdinalIgnoreCase) ||
            cat.Contains("club", StringComparison.OrdinalIgnoreCase))
            return EventNature.Secular;

        return EventNature.Cultural;
    }

    public IReadOnlyList<SignificantDate> GetSignificantDates(int year)
    {
        if (!_byYear.TryGetValue(year, out var yearPoyas)) return Array.Empty<SignificantDate>();
        return yearPoyas
            .Select(p => new SignificantDate(
                p.EnglishName,
                p.Date,
                p.IsMajor ? SignificanceLevel.Critical : SignificanceLevel.High))
            .ToArray();
    }

    public CalendarValidationResult ValidateEventAgainstCalendar(EventCulturalContext context)
    {
        if (context.StartDate is not { } eventStart)
        {
            return new CalendarValidationResult(true, "TBD event — no calendar conflict to evaluate.");
        }

        if (!IsPoyaDay(eventStart)) return new CalendarValidationResult(true, "No poya-day conflict.");

        var poya = LookupPoyaOn(eventStart);
        var nature = ClassifyEventNature(context);

        if (nature == EventNature.Secular && (poya?.IsMajor ?? false))
        {
            var suggestions = new List<string>
            {
                $"'{poya!.EnglishName}' is a major religious observance day. Consider moving the event to the following day.",
                "If the event MUST be on this date, consider a Cultural framing (add heritage content) to reduce conflict.",
            };
            return new CalendarValidationResult(false, $"Secular event scheduled on major poya day '{poya.EnglishName}'.", suggestions);
        }

        return new CalendarValidationResult(true, $"Event scheduled on poya day '{poya?.EnglishName ?? "unknown"}' — appropriate for nature '{nature}'.");
    }

    private PoyaSeedEntry? LookupPoyaOn(DateTime date)
    {
        if (!_byYear.TryGetValue(date.Year, out var yearPoyas)) return null;
        return yearPoyas.FirstOrDefault(p => p.Date.Date == date.Date);
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<PoyaSeedEntry>> LoadSeed()
    {
        using var stream = typeof(PoyaCalendarService).Assembly.GetManifestResourceStream(SeedResourceName)
            ?? throw new InvalidOperationException(
                $"Poya calendar seed '{SeedResourceName}' not found in assembly resources. " +
                "Verify Services/poya-calendar.json is included as an EmbeddedResource in " +
                "CulturalIntelligence.Infrastructure.csproj.");

        using var doc = JsonDocument.Parse(stream);
        var yearsElement = doc.RootElement.GetProperty("years");
        var byYear = new Dictionary<int, IReadOnlyList<PoyaSeedEntry>>();

        foreach (var yearProp in yearsElement.EnumerateObject())
        {
            if (!int.TryParse(yearProp.Name, out var year)) continue;
            var entries = new List<PoyaSeedEntry>();
            foreach (var entry in yearProp.Value.EnumerateArray())
            {
                var date = DateTime.SpecifyKind(entry.GetProperty("date").GetDateTime(), DateTimeKind.Utc);
                var englishName = entry.GetProperty("englishName").GetString() ?? "";
                var type = entry.GetProperty("type").GetString() ?? "";
                var isMajor = entry.GetProperty("isMajor").GetBoolean();
                var description = entry.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                entries.Add(new PoyaSeedEntry(date, englishName, type, isMajor, description));
            }
            byYear[year] = entries;
        }

        return byYear;
    }

    /// <summary>Internal record projecting a JSON seed entry.</summary>
    internal sealed record PoyaSeedEntry(DateTime Date, string EnglishName, string Type, bool IsMajor, string Description);
}
