namespace LankaConnect.Domain.Events.ValueObjects;

public record CulturalPeriod
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public bool IsMultiDay => (EndDate - StartDate).TotalDays >= 1;
    public TimeSpan Duration => EndDate - StartDate;

    public CulturalPeriod(DateTime startDate, DateTime endDate, string name, string description = "")
    {
        if (endDate < startDate)
            throw new ArgumentException("End date cannot be before start date");
            
        StartDate = startDate;
        EndDate = endDate;
        Name = name ?? string.Empty;
        Description = description ?? string.Empty;
    }

    public bool Contains(DateTime date) => date.Date >= StartDate.Date && date.Date <= EndDate.Date;
    public bool Overlaps(DateTime start, DateTime end) => StartDate <= end && EndDate >= start;
}