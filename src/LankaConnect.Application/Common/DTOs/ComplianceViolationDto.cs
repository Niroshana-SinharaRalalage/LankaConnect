using System;
using System.Collections.Generic;

namespace LankaConnect.Application.Common.DTOs;

/// <summary>
/// Data Transfer Object for ComplianceViolation across application boundaries.
/// Follows Clean Architecture principles by providing a layer-specific DTO.
/// </summary>
public class ComplianceViolationDto
{
    public string ViolationId { get; set; } = string.Empty;
    public string SLAId { get; set; } = string.Empty;
    public string ViolationType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CulturalImpact { get; set; }
    public TimeSpan ViolationDuration { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasCulturalImpact { get; set; }
    public bool IsCulturallySignificant { get; set; }
    public bool RequiresExecutiveAttention { get; set; }
    public bool RequiresImmediateAction { get; set; }
    public CulturalDataCategoryDto? AffectedCulturalCategory { get; set; }
    public List<ViolationHistoryEntryDto> ViolationHistory { get; set; } = new();
}

/// <summary>
/// DTO for CulturalDataCategory.
/// </summary>
public class CulturalDataCategoryDto
{
    public string CategoryId { get; set; } = string.Empty;
    public string CulturalSignificance { get; set; } = string.Empty;
    public string RequiredProtectionLevel { get; set; } = string.Empty;
    public List<string> ProtectedRegions { get; set; } = new();
    public List<string> CulturalGroups { get; set; } = new();
}

/// <summary>
/// DTO for ViolationHistoryEntry.
/// </summary>
public class ViolationHistoryEntryDto
{
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// DTO for cultural compliance validation results.
/// </summary>
public class CulturalComplianceValidationDto
{
    public bool GDPRCompliant { get; set; }
    public bool CulturalSensitivityCompliant { get; set; }
    public bool RequiresDualRemediation { get; set; }
    public List<string> ComplianceGaps { get; set; } = new();
    public DateTime ValidatedAt { get; set; }
}

/// <summary>
/// DTO for remediation plans.
/// </summary>
public class RemediationPlanDto
{
    public bool RequiresCulturalExpertConsultation { get; set; }
    public List<RemediationStepDto> RemediationSteps { get; set; } = new();
    public TimeSpan EstimatedDuration { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for individual remediation steps.
/// </summary>
public class RemediationStepDto
{
    public string StepType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TimeSpan EstimatedDuration { get; set; }
    public bool IsCritical { get; set; }
}

/// <summary>
/// Legacy DTO for backward compatibility with existing Application layer usage.
/// This replaces the duplicate ComplianceViolation classes.
/// </summary>
public class LegacyComplianceViolationDto
{
    public string ViolationId { get; set; } = string.Empty;
    public string RegionId { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double ActualValue { get; set; }
    public double TargetValue { get; set; }
    public DateTime ViolationTime { get; set; }
    public string Severity { get; set; } = string.Empty;

    // Performance monitoring specific fields
    public string ViolationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedTimestamp { get; set; }
    public string AffectedDataType { get; set; } = string.Empty;
    public List<string> RequiredActions { get; set; } = new();
    public DateTime RequiredResolutionDate { get; set; }
}