using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.DisasterRecovery;

/// <summary>
/// Compliance reporting scope for disaster recovery and regulatory requirements
/// Defines the boundaries and parameters for compliance reporting operations
/// </summary>
public class ComplianceReportingScope
{
    public required string ScopeId { get; set; }
    public required string ScopeName { get; set; }
    public required ComplianceReportingScopeType ScopeType { get; set; }
    public required List<string> IncludedComponents { get; set; }
    public required List<string> ExcludedComponents { get; set; }
    public required DateTime ScopeStartDate { get; set; }
    public required DateTime ScopeEndDate { get; set; }
    public required List<ComplianceRequirement> ComplianceRequirements { get; set; }
    public required Dictionary<string, ComplianceParameter> Parameters { get; set; }
    public required ComplianceScopeGranularity Granularity { get; set; }
    public List<string> RegulatoryFrameworks { get; set; } = new();
    public Dictionary<string, object> ScopeMetadata { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public List<ComplianceScopeFilter> Filters { get; set; } = new();
    public Dictionary<string, string> CustomCriteria { get; set; } = new();

    public ComplianceReportingScope()
    {
        IncludedComponents = new List<string>();
        ExcludedComponents = new List<string>();
        ComplianceRequirements = new List<ComplianceRequirement>();
        Parameters = new Dictionary<string, ComplianceParameter>();
        RegulatoryFrameworks = new List<string>();
        ScopeMetadata = new Dictionary<string, object>();
        Filters = new List<ComplianceScopeFilter>();
        CustomCriteria = new Dictionary<string, string>();
    }

    public bool IsComponentInScope(string componentId)
    {
        return IncludedComponents.Contains(componentId) && !ExcludedComponents.Contains(componentId);
    }

    public bool IsDateInScope(DateTime date)
    {
        return date >= ScopeStartDate && date <= ScopeEndDate;
    }
}

/// <summary>
/// Compliance reporting scope types
/// </summary>
public enum ComplianceReportingScopeType
{
    System = 1,
    Application = 2,
    Database = 3,
    Network = 4,
    Security = 5,
    Business = 6,
    Financial = 7,
    Regulatory = 8
}

/// <summary>
/// Compliance scope granularity
/// </summary>
public enum ComplianceScopeGranularity
{
    Summary = 1,
    Detailed = 2,
    Comprehensive = 3,
    Audit = 4,
    Forensic = 5
}

/// <summary>
/// Compliance requirement
/// </summary>
public class ComplianceRequirement
{
    public required string RequirementId { get; set; }
    public required string RequirementName { get; set; }
    public required ComplianceRequirementType Type { get; set; }
    public required ComplianceRequirementSeverity Severity { get; set; }
    public required string Description { get; set; }
    public required List<string> ApplicableComponents { get; set; }
    public bool IsMandatory { get; set; } = true;
    public string? RegulatoryReference { get; set; }
    public Dictionary<string, object> RequirementMetadata { get; set; } = new();
}

/// <summary>
/// Compliance requirement types
/// </summary>
public enum ComplianceRequirementType
{
    DataProtection = 1,
    AccessControl = 2,
    AuditTrail = 3,
    BackupRecovery = 4,
    IncidentResponse = 5,
    SecurityMonitoring = 6,
    BusinessContinuity = 7,
    RiskManagement = 8
}

/// <summary>
/// Compliance requirement severity
/// </summary>
public enum ComplianceRequirementSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
    Mandatory = 5
}

/// <summary>
/// Compliance parameter
/// </summary>
public class ComplianceParameter
{
    public required string ParameterName { get; set; }
    public required object ParameterValue { get; set; }
    public required ComplianceParameterType Type { get; set; }
    public required bool IsRequired { get; set; }
    public string? Description { get; set; }
    public List<string> AllowedValues { get; set; } = new();
    public Dictionary<string, object> Constraints { get; set; } = new();
}

/// <summary>
/// Compliance parameter types
/// </summary>
public enum ComplianceParameterType
{
    String = 1,
    Number = 2,
    Boolean = 3,
    Date = 4,
    List = 5,
    Object = 6
}

/// <summary>
/// Compliance scope filter
/// </summary>
public class ComplianceScopeFilter
{
    public required string FilterId { get; set; }
    public required ComplianceFilterType FilterType { get; set; }
    public required string FilterCriteria { get; set; }
    public required ComplianceFilterOperator Operator { get; set; }
    public required object FilterValue { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Compliance filter types
/// </summary>
public enum ComplianceFilterType
{
    Component = 1,
    Date = 2,
    Severity = 3,
    Type = 4,
    Status = 5,
    Custom = 6
}

/// <summary>
/// Compliance filter operators
/// </summary>
public enum ComplianceFilterOperator
{
    Equals = 1,
    NotEquals = 2,
    GreaterThan = 3,
    LessThan = 4,
    Contains = 5,
    StartsWith = 6,
    EndsWith = 7,
    In = 8,
    NotIn = 9
}