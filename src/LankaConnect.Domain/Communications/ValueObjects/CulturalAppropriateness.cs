using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Cultural Appropriateness scoring value object for Cultural Intelligence Engine
/// Represents the appropriateness score of cultural content or events
/// Range: 0.0 (completely inappropriate) to 1.0 (highly appropriate)
/// </summary>
public class CulturalAppropriateness : ValueObject
{
    public double Value { get; private set; }
    public AppropriatenessLevel Level { get; private set; }
    public string CulturalContext { get; private set; }
    public IEnumerable<string> CulturalFactors { get; private set; }
    
    public CulturalAppropriateness(double value, AppropriatenessLevel level, string culturalContext = "", IEnumerable<string>? culturalFactors = null)
    {
        if (value < 0.0 || value > 1.0)
            throw new ArgumentOutOfRangeException(nameof(value), "Appropriateness score must be between 0.0 and 1.0");
            
        Value = value;
        Level = level;
        CulturalContext = culturalContext ?? string.Empty;
        CulturalFactors = culturalFactors?.ToList() ?? new List<string>();
    }
    
    public static CulturalAppropriateness HighlyAppropriate(string context = "") => 
        new(0.9, AppropriatenessLevel.HighlyAppropriate, context);
        
    public static CulturalAppropriateness Appropriate(string context = "") => 
        new(0.7, AppropriatenessLevel.Appropriate, context);
        
    public static CulturalAppropriateness MildConcern(string context = "") => 
        new(0.6, AppropriatenessLevel.MildConcern, context);
        
    public static CulturalAppropriateness ModerateConcern(string context = "") => 
        new(0.4, AppropriatenessLevel.ModerateConcern, context);
        
    public static CulturalAppropriateness HighConcern(string context = "") => 
        new(0.2, AppropriatenessLevel.HighConcern, context);
        
    public static CulturalAppropriateness Inappropriate(string context = "") => 
        new(0.1, AppropriatenessLevel.Inappropriate, context);
    
    public bool IsAppropriate => Value >= 0.6;
    public bool IsHighlyAppropriate => Value >= 0.8;
    public bool IsInappropriate => Value < 0.4;
    
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return Level;
        yield return CulturalContext;
    }
}

public enum AppropriatenessLevel
{
    HighlyAppropriate = 1,
    Appropriate = 2,
    MildConcern = 3,
    ModerateConcern = 4,
    HighConcern = 5,
    Inappropriate = 6
}