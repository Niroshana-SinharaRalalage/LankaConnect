using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Common.Database;

/// <summary>
/// Value object representing an identifier for compromised data with cultural intelligence awareness
/// </summary>
public class CompromisedDataIdentifier : ValueObject
{
    /// <summary>
    /// Unique identifier for the compromised data
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Type of compromised data (User, Cultural, System, API)
    /// </summary>
    public string DataType { get; }

    /// <summary>
    /// Cultural sensitivity level of the compromised data
    /// </summary>
    public string CulturalSensitivityLevel { get; }

    /// <summary>
    /// Timestamp when compromise was detected
    /// </summary>
    public DateTime DetectedAt { get; }

    /// <summary>
    /// Source system where compromise was detected
    /// </summary>
    public string SourceSystem { get; }

    public CompromisedDataIdentifier(Guid id, string dataType, string culturalSensitivityLevel, DateTime detectedAt, string sourceSystem)
    {
        Id = id;
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        CulturalSensitivityLevel = culturalSensitivityLevel ?? throw new ArgumentNullException(nameof(culturalSensitivityLevel));
        DetectedAt = detectedAt;
        SourceSystem = sourceSystem ?? throw new ArgumentNullException(nameof(sourceSystem));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
        yield return DataType;
        yield return CulturalSensitivityLevel;
        yield return DetectedAt;
        yield return SourceSystem;
    }
}