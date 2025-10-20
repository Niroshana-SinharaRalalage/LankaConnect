# DUPLICATE TYPE HIERARCHY & RELATIONSHIPS

## Visual Duplicate Map

```
CANONICAL SOURCE: Stage5MissingTypes.cs
├── ServerInstance (UNIQUE - no duplicates)
├── DomainCulturalContext (UNIQUE - no duplicates)
├── GeographicScope (UNIQUE - no duplicates)
├── BusinessCulturalContext (UNIQUE - no duplicates)
├── CrossCommunityConnectionOpportunities (UNIQUE - no duplicates)
├── CulturalRoutingRationale (UNIQUE - no duplicates)
├── CachedAffinityScore (UNIQUE - no duplicates)
├── CulturalAffinityScoreCollection (UNIQUE - no duplicates)
├── CulturalLoadBalancingMetrics (UNIQUE - no duplicates)
├── ReligiousBackground (UNIQUE - no duplicates)
├── LanguagePreferences → UserLanguageProfile (RENAME)
│   └── CONFLICT with Domain.Events.ValueObjects.Recommendations.LanguagePreferences (ValueObject)
│       RESOLUTION: Keep both, rename to UserLanguageProfile
├── CulturalEventParticipation (UNIQUE - no duplicates)
├── HistoricalMetricsData (UNIQUE - no duplicates)
├── CostAnalysisParameters ──────┐
│   └── DELETE from Application.Common.Models.Performance.CostAnalysisParameters.cs
├── CostPerformanceAnalysis ─────┐
│   └── DELETE from Application.Common.Models.Performance.CostPerformanceAnalysis.cs
├── NotificationPreferences ─────┐
│   └── DELETE from Domain.Common.Monitoring.AlertingTypes.cs:202
├── CorrelationConfiguration (CLASS) ──┐
│   └── CONFLICT with EngineResults.cs:149 (RECORD)
│       RESOLUTION: Delete class, keep record
├── CreditCalculationPolicy (CLASS) ───┐
│   └── CONFLICT with EngineResults.cs:213 (ENUM)
│       RESOLUTION: Delete class, keep enum
├── RiskAssessmentTimeframe (CLASS) ───┐
│   └── CONFLICT with EngineResults.cs:188 (ENUM)
│       RESOLUTION: Delete class, keep enum
├── ThresholdAdjustmentReason (CLASS) ─┐
│   └── CONFLICT with EngineResults.cs:196 (ENUM)
│       RESOLUTION: Delete class, keep enum
├── CompetitiveBenchmarkData ────────────────────────┐
│   ├── DELETE from Application.Common.Revenue.RevenueOptimizationTypes.cs:446
│   └── DELETE from Application.Common.Models.Performance.CompetitiveBenchmarkData.cs
├── MarketPositionAnalysis ──────────────────────────┐
│   ├── DELETE from Application.Common.Revenue.RevenueOptimizationTypes.cs:567
│   └── DELETE from Application.Common.Models.Performance.MarketPositionAnalysis.cs
├── RevenueCalculationModel ─────────────────────────────────────┐
│   ├── DELETE from Application.Common.Revenue.RevenueOptimizationTypes.cs:208
│   ├── DELETE from Application.Common.Models.Critical.AdditionalBackupTypes.cs:149
│   ├── DELETE from Application.Common.Models.Performance.RevenueCalculationModel.cs
│   └── DELETE from Domain.Common.DisasterRecovery.EmergencyRecoveryTypes.cs:137
├── RevenueRiskCalculation ──────────────────────────────────────┐
│   ├── DELETE from Application.Common.Revenue.RevenueOptimizationTypes.cs:72
│   ├── DELETE from Application.Common.Models.Critical.AdditionalBackupTypes.cs:168
│   ├── DELETE from Application.Common.Models.Performance.RevenueRiskCalculation.cs
│   └── DELETE from Domain.Common.DisasterRecovery.EmergencyRecoveryTypes.cs:147
├── ScalingThresholdOptimization ────────────────────────────────┐
│   ├── DELETE from Application.Common.Performance.AutoScalingPerformanceTypes.cs:187
│   └── DELETE from Application.Common.Models.Performance.ScalingThresholdOptimization.cs
├── InterRegionOptimizationResult ───────────────────────────────────┐
│   ├── DELETE from Application.Common.Security.CrossRegionSecurityTypes.cs:607
│   ├── DELETE from Application.Common.Enterprise.EnterpriseRevenueTypes.cs:1035
│   └── DELETE from Application.Common.Models.Performance.PerformanceMonitoringTypes.cs:149
├── DataProtectionRegulation ────────────────────────────────────┐
│   ├── DELETE from Application.Common.Models.Critical.AdditionalMissingTypes.cs:15
│   └── DELETE from Application.Common.Models.Performance.PerformanceMonitoringTypes.cs:120
└── DisasterRecoveryResult ──────┐
    └── DELETE from Domain.Shared.Types.CriticalTypes.cs:55

EXTERNAL CANONICAL SOURCES (Keep as-is):
├── EngineResults.cs
│   ├── CorrelationConfiguration (record) ✓
│   ├── RiskAssessmentTimeframe (enum) ✓
│   ├── ThresholdAdjustmentReason (enum) ✓
│   └── CreditCalculationPolicy (enum) ✓
└── UserPreferenceValueObjects.cs
    └── LanguagePreferences (ValueObject) ✓
```

---

## Duplicate Frequency Distribution

```
4 duplicates:
  ├── RevenueCalculationModel (4 duplicates)
  └── RevenueRiskCalculation (4 duplicates)

3 duplicates:
  ├── InterRegionOptimizationResult (3 duplicates)
  ├── CompetitiveBenchmarkData (3 duplicates)
  ├── MarketPositionAnalysis (3 duplicates)
  ├── ScalingThresholdOptimization (3 duplicates)
  └── DataProtectionRegulation (3 duplicates)

2 duplicates:
  ├── CostPerformanceAnalysis (2 duplicates)
  ├── CostAnalysisParameters (2 duplicates)
  ├── NotificationPreferences (2 duplicates)
  ├── DisasterRecoveryResult (2 duplicates)
  ├── CorrelationConfiguration (2 implementations - class vs record)
  ├── RiskAssessmentTimeframe (2 implementations - class vs enum)
  ├── ThresholdAdjustmentReason (2 implementations - class vs enum)
  ├── CreditCalculationPolicy (2 implementations - class vs enum)
  └── LanguagePreferences (2 implementations - Infrastructure vs Domain)

0 duplicates (UNIQUE):
  ├── ServerInstance
  ├── DomainCulturalContext
  ├── GeographicScope
  ├── BusinessCulturalContext
  ├── CrossCommunityConnectionOpportunities
  ├── CulturalRoutingRationale
  ├── CachedAffinityScore
  ├── CulturalAffinityScoreCollection
  ├── CulturalLoadBalancingMetrics
  ├── ReligiousBackground
  ├── CulturalEventParticipation
  └── HistoricalMetricsData
```

---

## Files by Duplicate Count

### High Duplication (4+ duplicates)
```
RevenueOptimizationTypes.cs: 4 duplicates
  - RevenueRiskCalculation
  - RevenueCalculationModel
  - CompetitiveBenchmarkData
  - MarketPositionAnalysis
```

### Medium Duplication (2-3 duplicates)
```
PerformanceMonitoringTypes.cs: 2 duplicates
  - DataProtectionRegulation
  - InterRegionOptimizationResult

AdditionalBackupTypes.cs: 2 duplicates
  - RevenueCalculationModel
  - RevenueRiskCalculation

EmergencyRecoveryTypes.cs: 2 duplicates
  - RevenueCalculationModel
  - RevenueRiskCalculation

Stage5MissingTypes.cs: 5 conflicts (to be resolved)
  - CorrelationConfiguration (delete)
  - CreditCalculationPolicy (delete)
  - RiskAssessmentTimeframe (delete)
  - ThresholdAdjustmentReason (delete)
  - LanguagePreferences (rename)
```

### Single Duplicates (1 duplicate)
```
CrossRegionSecurityTypes.cs: 1 duplicate (InterRegionOptimizationResult)
EnterpriseRevenueTypes.cs: 1 duplicate (InterRegionOptimizationResult)
AutoScalingPerformanceTypes.cs: 1 duplicate (ScalingThresholdOptimization)
AdditionalMissingTypes.cs: 1 duplicate (DataProtectionRegulation)
CriticalTypes.cs: 1 duplicate (DisasterRecoveryResult)
AlertingTypes.cs: 1 duplicate (NotificationPreferences)
```

### Entire Files to Delete (7 files)
```
Application\Common\Models\Performance\:
  - RevenueRiskCalculation.cs
  - RevenueCalculationModel.cs
  - CompetitiveBenchmarkData.cs
  - MarketPositionAnalysis.cs
  - ScalingThresholdOptimization.cs
  - CostPerformanceAnalysis.cs
  - CostAnalysisParameters.cs
```

---

## Type Categories

### Infrastructure DTOs (mutable, database mapping)
```
✓ UserLanguageProfile (renamed from LanguagePreferences)
✓ ServerInstance
✓ CulturalUserContext
✓ GeographicLocation
✓ CulturalEventContext
```

### Domain ValueObjects (immutable, business logic)
```
✓ LanguagePreferences (Events.ValueObjects.Recommendations)
```

### Configuration Records (immutable, structural equality)
```
✓ CorrelationConfiguration (EngineResults.cs)
```

### Enumerations (fixed value sets)
```
✓ RiskAssessmentTimeframe (Daily/Weekly/Monthly/Quarterly)
✓ ThresholdAdjustmentReason (Performance/Business/Technical/Compliance)
✓ CreditCalculationPolicy (Automatic/Manual/Hybrid)
✓ GeographicScope (Local/Regional/National/Continental/Global)
✓ ServerStatus (Online/Offline/Degraded/Maintenance/Failed)
✓ DatabaseType (SqlServer/PostgreSQL/MySQL/MongoDB/Redis)
✓ ParticipationType (Attendee/Organizer/Sponsor/Volunteer/Speaker)
```

### Business Models (complex behavior)
```
✓ RevenueCalculationModel
✓ RevenueRiskCalculation
✓ CompetitiveBenchmarkData
✓ MarketPositionAnalysis
✓ ScalingThresholdOptimization
✓ InterRegionOptimizationResult
✓ DataProtectionRegulation
✓ DisasterRecoveryResult
```

---

## Dependency Graph

```
CulturalAffinityGeographicLoadBalancer
  ├── depends on: UserLanguageProfile (renamed)
  ├── depends on: ReligiousBackground
  ├── depends on: CulturalEventParticipation
  └── depends on: ServerInstance

IEventRecommendationEngine
  ├── depends on: LanguagePreferences (ValueObject)
  ├── depends on: LanguageCompatibilityScore
  └── depends on: UserPreference services

EngineResults.cs (Domain Models)
  ├── provides: CorrelationConfiguration (record)
  ├── provides: RiskAssessmentTimeframe (enum)
  ├── provides: ThresholdAdjustmentReason (enum)
  └── provides: CreditCalculationPolicy (enum)
```

---

## Clean Architecture Compliance

### Before Cleanup:
```
❌ Domain layer references Infrastructure types
❌ ValueObjects mixed with POCOs
❌ Enums defined as classes
❌ Records defined as classes
❌ 27 duplicate definitions across layers
```

### After Cleanup:
```
✅ Clear separation: Infrastructure DTOs vs Domain ValueObjects
✅ Proper enum usage for categorization
✅ Record types for immutable configuration
✅ Single canonical source for each type
✅ Zero duplicate definitions
```

---

## Summary Statistics

| Metric | Count |
|--------|-------|
| Total duplicates found | 27 |
| Clean deletions | 21 |
| Rename operations | 1 |
| Conflict resolutions | 6 |
| Files to delete entirely | 7 |
| Files to modify | 8 |
| Expected LOC reduction | ~250 |
| Expected build errors fixed | ~60 |

---

## Risk Matrix

| Operation | Risk Level | Mitigation |
|-----------|-----------|------------|
| Delete entire files | LOW | Files unused, easily reversible |
| Delete duplicate types | LOW | Canonical source preserved |
| Rename LanguagePreferences | MEDIUM | IDE refactoring tool, preview changes |
| Delete conflicting classes | LOW | Canonical enum/record preserved |
| Build validation | N/A | Automated, repeatable |

---

This hierarchy map provides a complete visual understanding of all duplicate type relationships and their resolution strategy.
