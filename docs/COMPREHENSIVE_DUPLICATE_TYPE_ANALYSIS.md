# COMPREHENSIVE DUPLICATE TYPE ANALYSIS

**Analysis Date:** 2025-10-12
**Canonical File:** `C:\Work\LankaConnect\src\LankaConnect.Domain\Shared\Stage5MissingTypes.cs`

## Executive Summary

**Total Duplicates Found:** 27 type definitions across 15 files
**Types with Duplicates:** 13 out of 32 types in Stage5MissingTypes.cs

## CRITICAL FINDING: LanguagePreferences Conflict

**HIGHEST PRIORITY - Different Implementations:**

### Type: LanguagePreferences

**CANONICAL (Stage5MissingTypes.cs - Line 180):**
```csharp
public class LanguagePreferences
{
    public string PreferenceId { get; set; } = string.Empty;
    public string PrimaryLanguage { get; set; } = string.Empty;
    public List<string> SecondaryLanguages { get; set; } = new();
    public Dictionary<string, double> ProficiencyLevels { get; set; } = new();
}
```

**DUPLICATE (UserPreferenceValueObjects.cs - Line 358):**
```csharp
public class LanguagePreferences : ValueObject
{
    public string[] PrimaryLanguages { get; }
    public string[] SecondaryLanguages { get; }
    public double MultilingualPreference { get; }
    public bool RequiresTranslation { get; }
}
```

**CONFLICT ANALYSIS:**
- Different inheritance: POCO vs ValueObject
- Different property types: string vs string[], List vs array
- Different properties: ProficiencyLevels vs MultilingualPreference/RequiresTranslation
- Different mutability: mutable vs immutable

**RESOLUTION REQUIRED:** Manual decision needed - these are fundamentally different types serving different purposes.

---

## Complete Duplicate Analysis

### 1. InterRegionOptimizationResult (3 duplicates)

**CANONICAL:** Stage5MissingTypes.cs:361
```csharp
public class InterRegionOptimizationResult
{
    public string ResultId { get; set; } = string.Empty;
    public bool IsOptimizationSuccessful { get; set; }
    public double PerformanceImprovement { get; set; }
    public List<string> OptimizedRegions { get; set; } = new();
}
```

**DUPLICATES TO DELETE:**
- `Application\Common\Security\CrossRegionSecurityTypes.cs:607` - Line 607
- `Application\Common\Enterprise\EnterpriseRevenueTypes.cs:1035` - Line 1035
- `Application\Common\Models\Performance\PerformanceMonitoringTypes.cs:149` - Line 149

---

### 2. RevenueRiskCalculation (4 duplicates)

**CANONICAL:** Stage5MissingTypes.cs:339
```csharp
public class RevenueRiskCalculation
{
    public string CalculationId { get; set; } = string.Empty;
    public decimal RevenueAtRisk { get; set; }
    public double RiskProbability { get; set; }
    public List<string> RiskFactors { get; set; } = new();
}
```

**DUPLICATES TO DELETE:**
- `Application\Common\Revenue\RevenueOptimizationTypes.cs:72` - Line 72
- `Application\Common\Models\Critical\AdditionalBackupTypes.cs:168` - Line 168
- `Application\Common\Models\Performance\RevenueRiskCalculation.cs:10` - Line 10 (entire file - inherits BaseEntity)
- `Domain\Common\DisasterRecovery\EmergencyRecoveryTypes.cs:147` - Line 147

---

### 3. RevenueCalculationModel (4 duplicates)

**CANONICAL:** Stage5MissingTypes.cs:328
```csharp
public class RevenueCalculationModel
{
    public string ModelId { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;
    public Dictionary<string, decimal> RevenueStreams { get; set; } = new();
    public decimal ProjectedRevenue { get; set; }
}
```

**DUPLICATES TO DELETE:**
- `Application\Common\Revenue\RevenueOptimizationTypes.cs:208` - Line 208
- `Application\Common\Models\Critical\AdditionalBackupTypes.cs:149` - Line 149
- `Application\Common\Models\Performance\RevenueCalculationModel.cs:10` - Line 10 (entire file - inherits BaseEntity)
- `Domain\Common\DisasterRecovery\EmergencyRecoveryTypes.cs:137` - Line 137

---

### 4. CompetitiveBenchmarkData (3 duplicates)

**CANONICAL:** Stage5MissingTypes.cs:305
```csharp
public class CompetitiveBenchmarkData
{
    public string BenchmarkId { get; set; } = string.Empty;
    public Dictionary<string, double> CompetitorMetrics { get; set; } = new();
    public DateTime BenchmarkDate { get; set; }
    public string MarketSegment { get; set; } = string.Empty;
}
```

**DUPLICATES TO DELETE:**
- `Application\Common\Revenue\RevenueOptimizationTypes.cs:446` - Line 446
- `Application\Common\Models\Performance\CompetitiveBenchmarkData.cs:6` - Line 6 (entire file - inherits BaseEntity)

---

### 5. MarketPositionAnalysis (3 duplicates)

**CANONICAL:** Stage5MissingTypes.cs:316
```csharp
public class MarketPositionAnalysis
{
    public string AnalysisId { get; set; } = string.Empty;
    public double MarketShare { get; set; }
    public int CompetitiveRank { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
}
```

**DUPLICATES TO DELETE:**
- `Application\Common\Revenue\RevenueOptimizationTypes.cs:567` - Line 567
- `Application\Common\Models\Performance\MarketPositionAnalysis.cs:6` - Line 6 (entire file - inherits BaseEntity)

---

### 6. ScalingThresholdOptimization (3 duplicates)

**CANONICAL:** Stage5MissingTypes.cs:350
```csharp
public class ScalingThresholdOptimization
{
    public string OptimizationId { get; set; } = string.Empty;
    public Dictionary<string, double> OptimizedThresholds { get; set; } = new();
    public double ImprovementPercentage { get; set; }
    public List<string> ChangedThresholds { get; set; } = new();
}
```

**DUPLICATES TO DELETE:**
- `Application\Common\Performance\AutoScalingPerformanceTypes.cs:187` - Line 187
- `Application\Common\Models\Performance\ScalingThresholdOptimization.cs:6` - Line 6 (entire file - inherits BaseEntity)

---

### 7. DisasterRecoveryResult (2 duplicates)

**CANONICAL:** Stage5MissingTypes.cs:400
```csharp
public class DisasterRecoveryResult
{
    public string ResultId { get; set; } = string.Empty;
    public bool IsRecoverySuccessful { get; set; }
    public TimeSpan RecoveryTime { get; set; }
    public List<string> RecoveredServices { get; set; } = new();
    public double DataIntegrityScore { get; set; }
}
```

**DUPLICATES TO DELETE:**
- `Domain\Shared\Types\CriticalTypes.cs:55` - Line 55

---

### 8. DataProtectionRegulation (3 duplicates)

**CANONICAL:** Stage5MissingTypes.cs:385
```csharp
public class DataProtectionRegulation
{
    public string RegulationId { get; set; } = string.Empty;
    public string RegulationName { get; set; } = string.Empty;
    public List<string> RequiredControls { get; set; } = new();
    public List<string> ApplicableRegions { get; set; } = new();
}
```

**DUPLICATES TO DELETE:**
- `Application\Common\Models\Critical\AdditionalMissingTypes.cs:15` - Line 15
- `Application\Common\Models\Performance\PerformanceMonitoringTypes.cs:120` - Line 120

---

### 9. CostPerformanceAnalysis (2 duplicates)

**CANONICAL:** Stage5MissingTypes.cs:238
```csharp
public class CostPerformanceAnalysis
{
    public string AnalysisId { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public double PerformanceScore { get; set; }
    public double CostEfficiencyRatio { get; set; }
    public List<string> Recommendations { get; set; } = new();
}
```

**DUPLICATES TO DELETE:**
- `Application\Common\Models\Performance\CostPerformanceAnalysis.cs:6` - Line 6 (entire file)

---

### 10. CostAnalysisParameters (2 duplicates)

**CANONICAL:** Stage5MissingTypes.cs:226
```csharp
public class CostAnalysisParameters
{
    public string AnalysisId { get; set; } = string.Empty;
    public DateTime AnalysisPeriodStart { get; set; }
    public DateTime AnalysisPeriodEnd { get; set; }
    public List<string> CostCategories { get; set; } = new();
    public string CurrencyCode { get; set; } = "USD";
}
```

**DUPLICATES TO DELETE:**
- `Application\Common\Models\Performance\CostAnalysisParameters.cs:6` - Line 6 (entire file)

---

### 11. NotificationPreferences (2 duplicates)

**CANONICAL:** Stage5MissingTypes.cs:250
```csharp
public class NotificationPreferences
{
    public string PreferenceId { get; set; } = string.Empty;
    public List<string> NotificationChannels { get; set; } = new();
    public Dictionary<string, bool> ChannelEnabled { get; set; } = new();
    public string PreferredLanguage { get; set; } = string.Empty;
}
```

**DUPLICATES TO DELETE:**
- `Domain\Common\Monitoring\AlertingTypes.cs:202` - Line 202

---

### 12. CorrelationConfiguration (2 duplicates - DIFFERENT TYPES)

**CANONICAL:** Stage5MissingTypes.cs:261
```csharp
public class CorrelationConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public TimeSpan CorrelationWindow { get; set; }
    public int MinimumCorrelatedAlerts { get; set; }
    public List<string> CorrelationKeys { get; set; } = new();
}
```

**DUPLICATE (RECORD TYPE):**
- `Domain\Common\Models\EngineResults.cs:149` - Line 149
```csharp
public record CorrelationConfiguration(...)
```

**RESOLUTION:** Keep record type in EngineResults.cs, delete class from Stage5MissingTypes.cs IF EngineResults version is complete.

---

### 13. Enum Conflicts (3 enums)

#### RiskAssessmentTimeframe
**CANONICAL:** Stage5MissingTypes.cs:283 (CLASS)
**DUPLICATE:** EngineResults.cs:188 (ENUM)
**CONFLICT:** Different types (class vs enum) - needs manual resolution

#### ThresholdAdjustmentReason
**CANONICAL:** Stage5MissingTypes.cs:294 (CLASS)
**DUPLICATE:** EngineResults.cs:196 (ENUM)
**CONFLICT:** Different types (class vs enum) - needs manual resolution

#### CreditCalculationPolicy
**CANONICAL:** Stage5MissingTypes.cs:272 (CLASS)
**DUPLICATE:** EngineResults.cs:213 (ENUM)
**CONFLICT:** Different types (class vs enum) - needs manual resolution

---

## DELETE LIST - Organized by File

### File: Application\Common\Security\CrossRegionSecurityTypes.cs
**DELETE:**
- Line 607: `public class InterRegionOptimizationResult`

### File: Application\Common\Enterprise\EnterpriseRevenueTypes.cs
**DELETE:**
- Line 1035: `public class InterRegionOptimizationResult`

### File: Application\Common\Revenue\RevenueOptimizationTypes.cs
**DELETE:**
- Line 72: `public class RevenueRiskCalculation`
- Line 208: `public class RevenueCalculationModel`
- Line 446: `public class CompetitiveBenchmarkData`
- Line 567: `public class MarketPositionAnalysis`

### File: Application\Common\Performance\AutoScalingPerformanceTypes.cs
**DELETE:**
- Line 187: `public class ScalingThresholdOptimization`

### File: Application\Common\Models\Critical\AdditionalBackupTypes.cs
**DELETE:**
- Line 149: `public class RevenueCalculationModel`
- Line 168: `public class RevenueRiskCalculation`

### File: Application\Common\Models\Critical\AdditionalMissingTypes.cs
**DELETE:**
- Line 15: `public class DataProtectionRegulation`

### File: Application\Common\Models\Performance\PerformanceMonitoringTypes.cs
**DELETE:**
- Line 120: `public class DataProtectionRegulation`
- Line 149: `public class InterRegionOptimizationResult`

### File: Domain\Shared\Types\CriticalTypes.cs
**DELETE:**
- Line 55: `public class DisasterRecoveryResult`

### File: Domain\Common\DisasterRecovery\EmergencyRecoveryTypes.cs
**DELETE:**
- Line 137: `public class RevenueCalculationModel`
- Line 147: `public class RevenueRiskCalculation`

### File: Domain\Common\Monitoring\AlertingTypes.cs
**DELETE:**
- Line 202: `public class NotificationPreferences`

### FILES TO DELETE ENTIRELY (Single-type files inheriting BaseEntity):
1. `Application\Common\Models\Performance\RevenueRiskCalculation.cs`
2. `Application\Common\Models\Performance\RevenueCalculationModel.cs`
3. `Application\Common\Models\Performance\CompetitiveBenchmarkData.cs`
4. `Application\Common\Models\Performance\MarketPositionAnalysis.cs`
5. `Application\Common\Models\Performance\ScalingThresholdOptimization.cs`
6. `Application\Common\Models\Performance\CostPerformanceAnalysis.cs`
7. `Application\Common\Models\Performance\CostAnalysisParameters.cs`

---

## MANUAL RESOLUTION REQUIRED

### 1. LanguagePreferences (CRITICAL)
**Action:** Decide which implementation to keep based on usage patterns
**Files:**
- Stage5MissingTypes.cs:180
- UserPreferenceValueObjects.cs:358

### 2. CorrelationConfiguration
**Action:** Verify EngineResults.cs record is complete, then delete Stage5MissingTypes.cs class
**Files:**
- Stage5MissingTypes.cs:261 (class)
- EngineResults.cs:149 (record)

### 3. Enum vs Class Conflicts (3 items)
**Action:** Determine correct type for each:
- RiskAssessmentTimeframe
- ThresholdAdjustmentReason
- CreditCalculationPolicy

---

## Statistics

**Total Types in Stage5MissingTypes.cs:** 32
**Types with No Duplicates:** 19
**Types with Duplicates:** 13
**Total Duplicate Instances:** 27
**Files Affected:** 15
**Files to Delete Entirely:** 7
**Manual Resolution Items:** 5

## Recommended Action Order

1. **HALT:** Resolve manual conflicts first (LanguagePreferences, enum vs class)
2. Delete entire single-type files (7 files)
3. Delete specific type definitions from multi-type files (11 deletions across 8 files)
4. Run build validation
5. Remove any remaining orphaned using statements
