# TYPE INVENTORY AND CATEGORIZATION
**Date:** 2025-10-12
**Phase:** 1 of 5 (Architect's Proper Solution)
**Current Build Errors:** 68

## TYPE CATEGORIZATION BY CLEAN ARCHITECTURE LAYER

### INFRASTRUCTURE LAYER TYPES (12 types)
**Destination:** `LankaConnect.Infrastructure.Database.LoadBalancing.LoadBalancingModels.cs`

1. **ServerInstance** (Line 29) - Infrastructure concern: load balancing
2. **DomainDatabase** (Line 42) - Infrastructure concern: database management
3. **CachedAffinityScore** (Line 133) - Infrastructure concern: caching
4. **CulturalAffinityScoreCollection** (Line 145) - Infrastructure concern: load balancing
5. **CulturalLoadBalancingMetrics** (Line 156) - Infrastructure concern: load balancing metrics

**Destination:** `LankaConnect.Infrastructure.Monitoring.MonitoringModels.cs`

6. **HistoricalMetricsData** (Line 212) - Infrastructure: monitoring
7. **NotificationPreferences** (Line 250) - Infrastructure: alerting
8. **CorrelationConfiguration** (Line 261) - Infrastructure: monitoring (DELETE - use record in EngineResults)
9. **CompetitiveBenchmarkData** (Line 305) - Infrastructure: monitoring
10. **InterRegionOptimizationResult** (Line 361) - Infrastructure: multi-region

**Destination:** `LankaConnect.Infrastructure.DisasterRecovery.DisasterRecoveryModels.cs`

11. **DisasterRecoveryResult** (Line 400) - Infrastructure: disaster recovery
12. **CriticalTypes** (Line 18) - Infrastructure: disaster recovery

---

### APPLICATION LAYER TYPES (11 types)
**Destination:** `LankaConnect.Application.Common.Models.Performance.CostAnalysisModels.cs`

1. **CostAnalysisParameters** (Line 226) - Application: performance analysis
2. **CostPerformanceAnalysis** (Line 238) - Application: performance analysis
3. **RevenueCalculationModel** (Line 328) - Application: revenue management
4. **RevenueRiskCalculation** (Line 339) - Application: revenue management
5. **ScalingThresholdOptimization** (Line 350) - Application: performance optimization

**Destination:** `LankaConnect.Application.Common.Models.Business.BusinessCulturalModels.cs`

6. **BusinessCulturalContext** (Line 69) - Application: business logic
7. **BusinessDiscoveryOpportunity** (Line 105) - Application: business logic
8. **CrossCommunityConnectionOpportunities** (Line 93) - Application: business logic

**Destination:** `LankaConnect.Application.Common.Models.Performance.PerformanceModels.cs`

9. **MarketPositionAnalysis** (Line 316) - Application: analytics
10. **CreditCalculationPolicy** (Line 272) - Application: SLA (DELETE - use enum in EngineResults)
11. **RiskAssessmentTimeframe** (Line 283) - Application: SLA (DELETE - use enum in EngineResults)
12. **ThresholdAdjustmentReason** (Line 294) - Application: SLA (DELETE - use enum in EngineResults)

---

### DOMAIN LAYER TYPES (9 types)
**Destination:** `LankaConnect.Domain.CulturalIntelligence.Models.CulturalRoutingModels.cs`

1. **DomainCulturalContext** (Line 81) - Domain: cultural intelligence
2. **CulturalRoutingRationale** (Line 121) - Domain: routing logic
3. **ReligiousBackground** (Line 168) - Domain: cultural data
4. **LanguagePreferences** (Line 180) - Domain: CONFLICT - rename to UserLanguageProfile
5. **CulturalEventParticipation** (Line 191) - Domain: cultural events

**Destination:** `LankaConnect.Domain.Common.Enums` (existing file)

6. **GeographicScope** (Line 57) - Domain: geographic enum

**Destination:** `LankaConnect.Domain.Common.Security` (existing structure)

7. **DataProtectionRegulation** (Line 385) - Domain: security/compliance

---

### SUPPORTING ENUMS (4 enums) - KEEP IN DOMAIN.SHARED
**Already in correct location** (but better in separate files)

1. **CriticalityLevel** (Line 413) - Domain enum
2. **ServerStatus** (Line 422) - Infrastructure enum (move to LoadBalancingModels)
3. **DatabaseType** (Line 431) - Infrastructure enum (move to LoadBalancingModels)
4. **ParticipationType** (Line 440) - Domain enum

---

## SUMMARY STATISTICS

| Layer | Type Count | Files to Create |
|-------|------------|----------------|
| Infrastructure | 12 | 3 files |
| Application | 11 | 2 files |
| Domain | 9 | 1 file |
| Enums | 4 | Split across layers |
| **TOTAL** | **32** | **6 new files** |

---

## TYPES TO DELETE (Not Move)

These types already exist in other files as records/enums:

1. **CorrelationConfiguration** (class) - DELETE, use record in EngineResults.cs
2. **CreditCalculationPolicy** (class) - DELETE, use enum in EngineResults.cs
3. **RiskAssessmentTimeframe** (class) - DELETE, use enum in EngineResults.cs
4. **ThresholdAdjustmentReason** (class) - DELETE, use enum in EngineResults.cs

---

## CONFLICT RESOLUTION

### LanguagePreferences Conflict
**Issue:** Two different implementations exist

**Stage5MissingTypes.cs** (Line 180) - Mutable class:
```csharp
public class LanguagePreferences  // Infrastructure DTO
{
    public string PreferenceId { get; set; }
    public string PrimaryLanguage { get; set; }
    public List<string> SecondaryLanguages { get; set; }
    public Dictionary<string, double> ProficiencyLevels { get; set; }
}
```

**UserPreferenceValueObjects.cs** (Line 358) - Immutable ValueObject:
```csharp
public class LanguagePreferences : ValueObject  // Domain ValueObject
{
    public string[] PrimaryLanguages { get; }
    public string[] SecondaryLanguages { get; }
    public double MultilingualPreference { get; }
    public bool RequiresTranslation { get; }
}
```

**RESOLUTION:** Rename Stage5 version to `UserLanguageProfile` and move to Infrastructure layer

---

## EXECUTION PLAN SUMMARY

### Phase 2: Create 6 New Files
1. `Infrastructure.Database.LoadBalancing.LoadBalancingModels.cs` (5 types + 2 enums)
2. `Infrastructure.Monitoring.MonitoringModels.cs` (5 types)
3. `Infrastructure.DisasterRecovery.DisasterRecoveryModels.cs` (2 types)
4. `Application.Common.Models.Performance.CostAnalysisModels.cs` (5 types)
5. `Application.Common.Models.Business.BusinessCulturalModels.cs` (3 types)
6. `Domain.CulturalIntelligence.Models.CulturalRoutingModels.cs` (5 types)

### Phase 3: Move Types
- Copy types from Stage5MissingTypes.cs to new files
- Validate build after each file (incremental)

### Phase 4: Delete Stage5MissingTypes.cs
- Verify all types have been moved
- Delete the file
- Validate build

### Phase 5: Remove Type Aliases
- Remove all 103 `using X = ...` statements
- Add proper `using` directives
- Validate build

---

## VALIDATION CHECKPOINTS

After each new file creation:
```powershell
dotnet build C:\Work\LankaConnect\LankaConnect.sln
```

Expected error count progression:
- Current: 68 errors
- After Phase 2: 68 errors (new files exist but not used yet)
- After Phase 3: 40-50 errors (types moved, aliases still present)
- After Phase 4: 40-50 errors (Stage5 deleted, aliases redirect to new files)
- After Phase 5: 0 errors (aliases removed, direct references)

---

**NEXT STEP:** Phase 2 - Create proper model files
