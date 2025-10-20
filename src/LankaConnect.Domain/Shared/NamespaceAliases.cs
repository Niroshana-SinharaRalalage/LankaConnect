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

// Global namespace alias definitions - UPDATED to point to actual type locations
// Note: Many types were moved from Domain.Shared to their proper domain locations

// AutoScalingDecision exists in Database and Performance - prefer Database for general use
global using AutoScalingDecision = LankaConnect.Domain.Common.Database.AutoScalingDecision;

// Note: The following types may not exist yet and need to be created in Domain.Shared:
// - ResponseAction, PerformanceAlert, CulturalIntelligenceContext, ServiceLevelAgreement
// - DateRange, AnalysisPeriod, DisasterRecoveryContext
// - ScalingTrigger, ScalingAction, ResponseActionType, ResponsePriority
// - DiasporaCommunity, CulturalSignificanceLevel, RevenueProtectionStrategy
// - DisasterRecoveryType, RecoveryPriority, AnalysisPeriodType

// Keep CulturalEventType - it exists in Domain.Common.Enums
global using CulturalEventType = LankaConnect.Domain.Common.Enums.CulturalEventType;

// South Asian Language canonical reference - resolves namespace conflicts
global using SouthAsianLanguage = LankaConnect.Domain.Common.Enums.SouthAsianLanguage;
