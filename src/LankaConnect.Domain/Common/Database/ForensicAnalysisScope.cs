namespace LankaConnect.Domain.Common.Database;

/// <summary>
/// Defines the scope of forensic analysis for cultural intelligence platform security
/// </summary>
public enum ForensicAnalysisScope
{
    /// <summary>
    /// System-wide forensic analysis across all components
    /// </summary>
    SystemWide = 0,

    /// <summary>
    /// Cultural data specific forensic analysis (sacred events, community data)
    /// </summary>
    CulturalData = 1,

    /// <summary>
    /// User data forensic analysis (PII, authentication data)
    /// </summary>
    UserData = 2,

    /// <summary>
    /// Database infrastructure forensic analysis
    /// </summary>
    DatabaseInfrastructure = 3,

    /// <summary>
    /// API endpoints and service layer forensic analysis
    /// </summary>
    ApiServices = 4,

    /// <summary>
    /// Cultural intelligence algorithms and ML models
    /// </summary>
    CulturalIntelligence = 5
}