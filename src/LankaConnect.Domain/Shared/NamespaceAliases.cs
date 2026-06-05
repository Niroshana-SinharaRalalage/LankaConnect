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

// W2C.6 (2026-06-05): CulturalEventType (40+ value enum) moved to SharedKernel.Cultural
// per ADR-008. The 350+ unqualified references across 62 files resolve via this alias.
// Was: LankaConnect.Domain.Common.Enums.CulturalEventType
global using CulturalEventType = LankaConnect.SharedKernel.Cultural.CulturalEventType;

// W2C.6 (2026-06-05): ReligiousObservanceLevel extracted from
// GoogleCalendarCulturalEvent.cs to its own file in SharedKernel.Cultural.
// Unblocks W2D.1b CulturalConflict + CulturalEvent moves.
global using ReligiousObservanceLevel = LankaConnect.SharedKernel.Cultural.ReligiousObservanceLevel;

// South Asian Language canonical reference - resolves namespace conflicts
// W2C.2 (2026-06-04): SouthAsianLanguage moved to SharedKernel.Cultural per ADR-008.
// The global alias keeps unqualified `SouthAsianLanguage` working for all consumers
// across LankaConnect.Domain without per-file using-directive churn.
global using SouthAsianLanguage = LankaConnect.SharedKernel.Cultural.SouthAsianLanguage;

// W2C.4 (2026-06-05): CulturalBackground moved to SharedKernel.Cultural per ADR-008.
// Same per-file-churn-avoidance pattern as SouthAsianLanguage. The canonical
// CulturalBackground has 8 Sri-Lankan-focused values (SinhalaBuddhist, TamilHindu,
// TamilSriLankan, SriLankanMuslim, SriLankanChristian, Burgher, Malay, Other).
// The broader 15-value South-Asian community granularity was renamed to
// `SouthAsianCommunity` in MultiLanguageRoutingModels.cs to avoid name collision.
global using CulturalBackground = LankaConnect.SharedKernel.Cultural.CulturalBackground;

// W2C.5 (2026-06-05): GeographicRegion ENUM moved to SharedKernel.Cultural per ADR-008.
// 35-value enum spanning SL provinces + diaspora regions + cities. Two deprecated
// empty-stub variants (Communications + Events) deleted. The Billing CLASS variant
// (distinct open-text tax-jurisdiction VO) renamed to `BillingRegion` to avoid
// name collision.
global using GeographicRegion = LankaConnect.SharedKernel.Cultural.GeographicRegion;

// W2D.1a (2026-06-05): CulturalContext value object moved to SharedKernel.Cultural
// per ADR-008. The canonical class (Language/CulturalBackground/GeographicRegion/
// ReligiousContext/TimeZone/CulturalNotes + factory methods for community types).
// The orphan POCO stub at Common/Database/AdditionalMissingModels.cs was deleted.
global using CulturalContext = LankaConnect.SharedKernel.Cultural.CulturalContext;

// W2D.1b (2026-06-05): CulturalConflict value object moved to SharedKernel.Cultural
// per ADR-008. The canonical Communications class (HasConflict/ConflictReason/
// ConflictingEventType/ConflictSeverity/SuggestedAlternativeTime/AlternativeTimeSlots/
// RecommendedStrategy/CulturalGuidance + factory methods CreateBuddhistConflict,
// CreateHinduConflict, CreateCulturalConflict). The Events record was renamed to
// EventCulturalConflict and the CulturalUserProfile nested record to
// UserCulturalConflict — both distinct concepts per architect Q3 pattern.
global using CulturalConflict = LankaConnect.SharedKernel.Cultural.CulturalConflict;
