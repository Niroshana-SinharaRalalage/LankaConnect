using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.ValueObjects;

public record CulturalDate
{
    public DateTime Date { get; init; }
    public string Name { get; init; }
    public string EnglishName { get; init; }
    public string SinhalaName { get; init; }
    public string TamilName { get; init; }
    public PoyadayType? PoyadayType { get; init; }
    public bool IsMajorPoya { get; init; }
    public bool HasSpecialObservances { get; init; }
    public bool IsAdjustedForWeekend { get; init; }
    public bool HasAmericanHolidayAdjustment { get; init; }

    public CulturalDate(DateTime date, string englishName, string sinhalaName, string tamilName, 
        PoyadayType? poyadayType = null, bool isMajorPoya = false, bool hasSpecialObservances = false,
        bool isAdjustedForWeekend = false, bool hasAmericanHolidayAdjustment = false)
    {
        Date = date;
        Name = englishName; // Name is the same as EnglishName for simplicity
        EnglishName = englishName;
        SinhalaName = sinhalaName;
        TamilName = tamilName;
        PoyadayType = poyadayType;
        IsMajorPoya = isMajorPoya;
        HasSpecialObservances = hasSpecialObservances;
        IsAdjustedForWeekend = isAdjustedForWeekend;
        HasAmericanHolidayAdjustment = hasAmericanHolidayAdjustment;
    }
}