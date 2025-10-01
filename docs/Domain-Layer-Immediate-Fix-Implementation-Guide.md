# Domain Layer Immediate Fix Implementation Guide

**Status**: Implementation Ready  
**Priority**: Critical  
**Target**: Resolve Phase 10 Database Optimization compilation errors  
**Timeline**: Immediate (Current Sprint)  

## Quick Start Implementation

### 1. Create Missing Abstractions Namespace

**File**: `src/LankaConnect.Domain/Common/Abstractions/IDomainEvent.cs`
```csharp
using System;

namespace LankaConnect.Domain.Common.Abstractions
{
    public interface IDomainEvent
    {
        Guid Id { get; }
        DateTime OccurredAt { get; }
        string EventType { get; }
    }

    public abstract record DomainEvent : IDomainEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public abstract string EventType { get; }
    }
}
```

### 2. Fix Domain Event Interface Compliance

**File**: `src/LankaConnect.Domain/Infrastructure/Events/AutoScalingEvents.cs`
```csharp
using LankaConnect.Domain.Common.Abstractions;

namespace LankaConnect.Domain.Infrastructure.Events
{
    public record AutoScalingRequestedEvent : DomainEvent
    {
        public override string EventType => nameof(AutoScalingRequestedEvent);
        public string DatabaseInstanceId { get; init; } = string.Empty;
        public string ScalingReason { get; init; } = string.Empty;
        public int TargetCapacity { get; init; }
        public CulturalContext CulturalContext { get; init; } = null!;
    }

    public record CulturalPredictionUpdatedEvent : DomainEvent
    {
        public override string EventType => nameof(CulturalPredictionUpdatedEvent);
        public string PredictionId { get; init; } = string.Empty;
        public string CulturalEventType { get; init; } = string.Empty;
        public DateTime PredictedEventDate { get; init; }
        public double ConfidenceScore { get; init; }
        public string AffectedRegions { get; init; } = string.Empty;
    }
}
```

### 3. Create Canonical GeographicLocation Type

**File**: `src/LankaConnect.Domain/Shared/Geography/GeographicLocation.cs`
```csharp
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Shared.Geography
{
    public record GeographicLocation : ValueObject
    {
        public decimal Latitude { get; init; }
        public decimal Longitude { get; init; }
        public string Address { get; init; } = string.Empty;
        public string CountryCode { get; init; } = string.Empty;
        public string RegionCode { get; init; } = string.Empty;
        public string? CulturalSignificance { get; init; }
        public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Utc;

        public static GeographicLocation Create(
            decimal latitude, 
            decimal longitude, 
            string address = "",
            string countryCode = "",
            string regionCode = "",
            string? culturalSignificance = null,
            TimeZoneInfo? timeZone = null)
        {
            if (latitude < -90 || latitude > 90)
                throw new ArgumentException("Latitude must be between -90 and 90");
            
            if (longitude < -180 || longitude > 180)
                throw new ArgumentException("Longitude must be between -180 and 180");

            return new GeographicLocation
            {
                Latitude = latitude,
                Longitude = longitude,
                Address = address,
                CountryCode = countryCode,
                RegionCode = regionCode,
                CulturalSignificance = culturalSignificance,
                TimeZone = timeZone ?? TimeZoneInfo.Utc
            };
        }

        public double DistanceToKm(GeographicLocation other)
        {
            // Haversine formula implementation
            const double R = 6371; // Earth's radius in kilometers
            
            var dLat = ToRadians((double)(other.Latitude - Latitude));
            var dLon = ToRadians((double)(other.Longitude - Longitude));
            
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians((double)Latitude)) * 
                    Math.Cos(ToRadians((double)other.Latitude)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return R * c;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;

        public override IEnumerable<object> GetEqualityComponents()
        {
            yield return Latitude;
            yield return Longitude;
            yield return Address;
            yield return CountryCode;
            yield return RegionCode;
            yield return CulturalSignificance ?? "";
            yield return TimeZone.Id;
        }
    }
}
```

### 4. Create Global Using Aliases File

**File**: `src/LankaConnect.Domain/GlobalUsings.cs`
```csharp
// Global using aliases to resolve type ambiguities
global using SharedGeographicRegion = LankaConnect.Domain.Shared.Geography.GeographicRegion;
global using CommunicationsGeographicRegion = LankaConnect.Domain.Communications.Enums.GeographicRegion;
global using EventsGeographicRegion = LankaConnect.Domain.Events.Enums.GeographicRegion;

global using SharedCulturalContext = LankaConnect.Domain.Shared.Culture.CulturalContext;
global using CommunicationsCulturalContext = LankaConnect.Domain.Communications.ValueObjects.CulturalContext;

global using SharedCulturalEventType = LankaConnect.Domain.Shared.Events.CulturalEventType;

// Common domain imports
global using LankaConnect.Domain.Common;
global using LankaConnect.Domain.Common.Abstractions;
global using LankaConnect.Domain.Shared.ValueObjects;
```

### 5. Consolidate Duplicate Database Types

**File**: `src/LankaConnect.Domain/Shared/Database/CrossRegionSynchronizationResult.cs`
```csharp
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Shared.Database
{
    public class CrossRegionSynchronizationResult : BaseDomainEntity
    {
        public string SynchronizationId { get; private set; } = Guid.NewGuid().ToString();
        public string SourceRegion { get; private set; } = string.Empty;
        public List<string> TargetRegions { get; private set; } = new();
        public CulturalDataType DataType { get; private set; }
        public TimeSpan SynchronizationDuration { get; private set; }
        public bool ConsistencyAchieved { get; private set; }
        public double ConsistencyScore { get; private set; }
        public DateTime CompletedAt { get; private set; } = DateTime.UtcNow;
        public List<string> SynchronizationLogs { get; private set; } = new();
        public Dictionary<string, object> CulturalMetrics { get; private set; } = new();

        private CrossRegionSynchronizationResult() { } // EF Core

        public static CrossRegionSynchronizationResult Create(
            string sourceRegion,
            List<string> targetRegions,
            CulturalDataType dataType)
        {
            return new CrossRegionSynchronizationResult
            {
                Id = Guid.NewGuid(),
                SourceRegion = sourceRegion,
                TargetRegions = targetRegions,
                DataType = dataType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public void Complete(TimeSpan duration, bool consistencyAchieved, double score)
        {
            SynchronizationDuration = duration;
            ConsistencyAchieved = consistencyAchieved;
            ConsistencyScore = score;
            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddLog(string logEntry)
        {
            SynchronizationLogs.Add($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {logEntry}");
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetCulturalMetric(string key, object value)
        {
            CulturalMetrics[key] = value;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
```

**File**: `src/LankaConnect.Domain/Shared/Performance/PerformanceThreshold.cs`
```csharp
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Shared.Performance
{
    public enum PerformanceMetricType
    {
        CpuUtilization,
        MemoryUtilization,
        DatabaseResponseTime,
        ConnectionPoolUtilization,
        CulturalEventResponseTime,
        CommunityEngagementLatency,
        DiasporaNetworkLatency
    }

    public enum ThresholdSeverity
    {
        Info,
        Warning,
        Critical,
        Emergency
    }

    public record PerformanceThreshold : ValueObject
    {
        public PerformanceMetricType MetricType { get; init; }
        public double WarningThreshold { get; init; }
        public double CriticalThreshold { get; init; }
        public double EmergencyThreshold { get; init; }
        public TimeSpan EvaluationWindow { get; init; }
        public bool IsEnabled { get; init; }
        public CulturalDataPriority CulturalPriority { get; init; }
        public string? CulturalContext { get; init; }

        public static PerformanceThreshold Create(
            PerformanceMetricType metricType,
            double warningThreshold,
            double criticalThreshold,
            double emergencyThreshold,
            TimeSpan evaluationWindow,
            CulturalDataPriority culturalPriority = CulturalDataPriority.Level5General)
        {
            if (warningThreshold >= criticalThreshold)
                throw new ArgumentException("Warning threshold must be less than critical");

            if (criticalThreshold >= emergencyThreshold)
                throw new ArgumentException("Critical threshold must be less than emergency");

            return new PerformanceThreshold
            {
                MetricType = metricType,
                WarningThreshold = warningThreshold,
                CriticalThreshold = criticalThreshold,
                EmergencyThreshold = emergencyThreshold,
                EvaluationWindow = evaluationWindow,
                IsEnabled = true,
                CulturalPriority = culturalPriority
            };
        }

        public ThresholdSeverity EvaluateSeverity(double currentValue)
        {
            if (currentValue >= EmergencyThreshold) return ThresholdSeverity.Emergency;
            if (currentValue >= CriticalThreshold) return ThresholdSeverity.Critical;
            if (currentValue >= WarningThreshold) return ThresholdSeverity.Warning;
            return ThresholdSeverity.Info;
        }

        public override IEnumerable<object> GetEqualityComponents()
        {
            yield return MetricType;
            yield return WarningThreshold;
            yield return CriticalThreshold;
            yield return EmergencyThreshold;
            yield return EvaluationWindow;
            yield return CulturalPriority;
        }
    }
}
```

### 6. Update BackupRecoveryModels.cs to Include Missing Namespace

**Fix**: Update the using statements at the top of the file:
```csharp
// Add this line to existing using statements
using LankaConnect.Domain.Common.Abstractions;
```

### 7. Create Directory Structure

Execute these commands to create the required directory structure:

```bash
# Create shared domain directories
mkdir -p "src/LankaConnect.Domain/Shared/Geography"
mkdir -p "src/LankaConnect.Domain/Shared/Culture"  
mkdir -p "src/LankaConnect.Domain/Shared/Events"
mkdir -p "src/LankaConnect.Domain/Shared/Database"
mkdir -p "src/LankaConnect.Domain/Shared/Performance"
mkdir -p "src/LankaConnect.Domain/Common/Abstractions"
mkdir -p "src/LankaConnect.Domain/Infrastructure/Events"
```

## Implementation Order

### Step 1: Create Foundation Files (No Dependencies)
1. `Common/Abstractions/IDomainEvent.cs`
2. `GlobalUsings.cs`  
3. `Shared/Geography/GeographicLocation.cs`
4. `Shared/Performance/PerformanceThreshold.cs`

### Step 2: Fix Domain Events  
1. `Infrastructure/Events/AutoScalingEvents.cs`
2. Update any existing domain event classes to inherit from `DomainEvent`

### Step 3: Consolidate Database Models
1. `Shared/Database/CrossRegionSynchronizationResult.cs`
2. Update existing files to reference consolidated types

### Step 4: Update Using Statements
1. Add missing namespace references to `BackupRecoveryModels.cs`
2. Update all files with ambiguous references to use global aliases

### Step 5: Remove Duplicate Definitions
1. Remove duplicate `CrossRegionSynchronizationResult` from `ConsistencyModels.cs` and `ShardingModels.cs`
2. Remove duplicate `PerformanceThreshold` definitions
3. Update references to point to consolidated types

## Testing Strategy

### Immediate Verification Tests

**File**: `tests/LankaConnect.Domain.Tests/Shared/Geography/GeographicLocationTests.cs`
```csharp
using FluentAssertions;
using LankaConnect.Domain.Shared.Geography;
using Xunit;

namespace LankaConnect.Domain.Tests.Shared.Geography
{
    public class GeographicLocationTests
    {
        [Fact]
        public void Create_ValidCoordinates_ReturnsGeographicLocation()
        {
            // Arrange
            var latitude = 6.9271m; // Colombo
            var longitude = 79.8612m;

            // Act
            var location = GeographicLocation.Create(latitude, longitude, "Colombo, Sri Lanka");

            // Assert
            location.Should().NotBeNull();
            location.Latitude.Should().Be(latitude);
            location.Longitude.Should().Be(longitude);
            location.Address.Should().Be("Colombo, Sri Lanka");
        }

        [Fact]
        public void Create_InvalidLatitude_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                GeographicLocation.Create(91m, 0m));
        }

        [Fact]
        public void DistanceToKm_CalculatesCorrectDistance()
        {
            // Arrange - Colombo to Kandy approximately 115km
            var colombo = GeographicLocation.Create(6.9271m, 79.8612m);
            var kandy = GeographicLocation.Create(7.2906m, 80.6337m);

            // Act
            var distance = colombo.DistanceToKm(kandy);

            // Assert
            distance.Should().BeApproximately(115, 10); // Within 10km accuracy
        }
    }
}
```

### Cultural Intelligence Validation Test

**File**: `tests/LankaConnect.Domain.Tests/CulturalIntelligenceIntegrityTests.cs`
```csharp
using FluentAssertions;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Shared.Geography;
using Xunit;

namespace LankaConnect.Domain.Tests
{
    public class CulturalIntelligenceIntegrityTests
    {
        [Fact]
        public void CulturalContext_AfterConsolidation_PreservesBuddhistCommunityFeatures()
        {
            // Test that cultural context consolidation preserves functionality
            var region = CommunicationsGeographicRegion.SriLanka;
            var context = CulturalContext.CreateForBuddhistCommunity(region);
            
            context.Should().NotBeNull();
            context.ReligiousContext.Should().Be(ReligiousContext.BuddhistPoyaday);
            context.IsReligiousObservanceConsidered.Should().BeTrue();
        }

        [Fact]
        public void GeographicLocation_SupportsCulturalSignificance()
        {
            // Test that new geographic location preserves cultural context
            var templeLocation = GeographicLocation.Create(
                6.9271m, 79.8612m, "Temple of the Sacred Tooth Relic",
                culturalSignificance: "Most sacred Buddhist temple in Sri Lanka");

            templeLocation.CulturalSignificance.Should().NotBeNullOrEmpty();
            templeLocation.CulturalSignificance.Should().Contain("sacred Buddhist");
        }
    }
}
```

## Rollback Plan

If issues arise during implementation:

1. **Immediate Rollback**
   - Revert to previous commit
   - Remove newly created shared files
   - Restore original duplicate definitions

2. **Partial Rollback**  
   - Keep foundation files (IDomainEvent, GeographicLocation)
   - Restore duplicate database models temporarily
   - Complete consolidation in future sprint

3. **Fix-Forward**
   - Address specific compilation errors
   - Add compatibility shims
   - Gradual migration approach

## Success Criteria

✅ **Zero compilation errors across all projects**  
✅ **All tests pass including cultural intelligence features**  
✅ **Phase 10 Database Optimization tests can execute**  
✅ **No performance regression in cultural features**  
✅ **Clean Architecture boundaries maintained**  

## Final Checklist

Before considering this implementation complete:

- [ ] All duplicate types consolidated or aliased
- [ ] All domain events implement IDomainEvent correctly  
- [ ] All namespace ambiguities resolved
- [ ] All missing dependencies created
- [ ] Cultural intelligence features validated
- [ ] Performance tests pass
- [ ] Integration tests pass
- [ ] Code review completed

---

**Implementation Contact**: System Architecture Team  
**Cultural Intelligence Validation**: Cultural Domain Expert  
**Technical Review**: Lead Developer  
**Business Sign-off**: Product Owner  

*This guide provides immediate, actionable steps to resolve compilation errors while preserving the cultural intelligence platform's integrity and enabling Phase 10 Database Optimization testing.*