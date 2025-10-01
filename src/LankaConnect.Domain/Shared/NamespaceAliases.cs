/// <summary>
/// Namespace aliases to resolve compilation conflicts for missing types
/// This file provides canonical namespace references to eliminate CS0104 ambiguous reference errors
///
/// Usage Pattern:
/// using MissingTypes = LankaConnect.Domain.Shared;
/// using CulturalEnums = LankaConnect.Domain.Common.Enums;
///
/// Then reference as: MissingTypes.AutoScalingDecision or CulturalEnums.SouthAsianLanguage
/// </summary>

// Global namespace alias definitions for missing type resolution
global using AutoScalingDecision = LankaConnect.Domain.Shared.AutoScalingDecision;
// SecurityIncident moved to Application.Common.Interfaces.IDatabaseSecurityOptimizationEngine
global using ResponseAction = LankaConnect.Domain.Shared.ResponseAction;
global using PerformanceAlert = LankaConnect.Domain.Shared.PerformanceAlert;
global using CulturalIntelligenceContext = LankaConnect.Domain.Shared.CulturalIntelligenceContext;
global using ServiceLevelAgreement = LankaConnect.Domain.Shared.ServiceLevelAgreement;
global using DateRange = LankaConnect.Domain.Shared.DateRange;
global using AnalysisPeriod = LankaConnect.Domain.Shared.AnalysisPeriod;
global using DisasterRecoveryContext = LankaConnect.Domain.Shared.DisasterRecoveryContext;

// Enum aliases for clarity
global using ScalingTrigger = LankaConnect.Domain.Shared.ScalingTrigger;
global using ScalingAction = LankaConnect.Domain.Shared.ScalingAction;
// SecurityIncidentType moved to Application.Common.Security.CrossRegionSecurityTypes
// IncidentSeverity moved to Application.Common.Security.CrossRegionSecurityTypes
global using ResponseActionType = LankaConnect.Domain.Shared.ResponseActionType;
global using ResponsePriority = LankaConnect.Domain.Shared.ResponsePriority;
global using CulturalEventType = LankaConnect.Domain.Common.Enums.CulturalEventType;
global using DiasporaCommunity = LankaConnect.Domain.Shared.DiasporaCommunity;
global using CulturalSignificanceLevel = LankaConnect.Domain.Shared.CulturalSignificanceLevel;
global using RevenueProtectionStrategy = LankaConnect.Domain.Shared.RevenueProtectionStrategy;
global using DisasterRecoveryType = LankaConnect.Domain.Shared.DisasterRecoveryType;
global using RecoveryPriority = LankaConnect.Domain.Shared.RecoveryPriority;
global using AnalysisPeriodType = LankaConnect.Domain.Shared.AnalysisPeriodType;

// South Asian Language canonical reference - resolves namespace conflicts
global using SouthAsianLanguage = LankaConnect.Domain.Common.Enums.SouthAsianLanguage;