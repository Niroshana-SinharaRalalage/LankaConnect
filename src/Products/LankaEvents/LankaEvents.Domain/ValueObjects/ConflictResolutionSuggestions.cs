namespace LankaConnect.Domain.Events.ValueObjects;

public record ConflictResolutionSuggestions
{
    public IReadOnlyList<DateTime> AlternativeDates { get; init; }
    public IReadOnlyList<string> ContentModifications { get; init; }
    public IReadOnlyList<string> TimingAdjustments { get; init; }
    public string PreferredResolution { get; init; }

    public ConflictResolutionSuggestions(IEnumerable<DateTime> alternativeDates,
        IEnumerable<string> contentModifications, IEnumerable<string> timingAdjustments,
        string preferredResolution = "")
    {
        AlternativeDates = alternativeDates?.ToList().AsReadOnly() ?? new List<DateTime>().AsReadOnly();
        ContentModifications = contentModifications?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
        TimingAdjustments = timingAdjustments?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
        PreferredResolution = preferredResolution ?? string.Empty;
    }
}