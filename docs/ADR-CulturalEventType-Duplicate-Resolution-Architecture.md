# Architecture Decision Record: CulturalEventType Duplicate Resolution

## Status
**CRITICAL RESOLUTION REQUIRED** - 106 CS0104 compilation errors

## Context

### Problem Statement
Critical duplicate type conflict causing 106 CS0104 compilation errors:

1. **Domain Enum (Canonical)**: `LankaConnect.Domain.Common.Enums.CulturalEventType`
   - **Purpose**: Represents cultural event categories/types (enum values)
   - **Values**: VesakDayBuddhist, DiwaliHindu, EidAlFitr, etc. (30+ cultural event types)
   - **Location**: Domain layer (correct architectural placement)
   - **Usage**: Throughout codebase for event classification

2. **Application Record (Conflicting)**: `LankaConnect.Application.Common.Interfaces.CulturalEventType`
   - **Purpose**: Represents specific event instances (record with properties)
   - **Properties**: EventId, EventName, Significance, StartDate, EndDate
   - **Location**: Application layer interfaces
   - **Usage**: Database security optimization engine

### Architectural Analysis

#### Current Usage Patterns
```csharp
// Domain enum usage (CANONICAL)
CulturalEventType eventType = CulturalEventType.VesakDayBuddhist;
var correlation = new CulturalEventCorrelation(eventType, DateTime.Now, 4.5);

// Application record usage (CONFLICTING)
var eventInstance = new CulturalEventType(
    "evt-001",
    "Vesak Day 2024",
    CulturalSignificance.Sacred,
    DateTime.Parse("2024-05-23"),
    DateTime.Parse("2024-05-23")
);
```

#### Root Cause
- **Semantic Confusion**: Same name for different concepts
- **Architectural Violation**: Application layer defining domain concepts
- **Clean Architecture Principle**: Domain should define all business entities

## Decision

### 1. Rename Application Record → `CulturalEventInstance`

**Rationale:**
- **Semantic Clarity**: "Instance" clearly indicates a specific occurrence
- **Domain Distinction**: Separates event type (classification) from event instance (occurrence)
- **Clean Architecture**: Preserves domain enum as canonical type classifier

### 2. Establish Clear Architectural Relationship

```csharp
// Domain Layer (unchanged)
public enum CulturalEventType { VesakDayBuddhist, DiwaliHindu, ... }

// Application Layer (renamed and enhanced)
public record CulturalEventInstance(
    string EventId,
    string EventName,
    CulturalEventType EventType,        // ← References domain enum
    CulturalSignificance Significance,
    DateTime StartDate,
    DateTime EndDate);
```

### 3. Migration Strategy

#### Phase 1: Systematic Rename
1. **Rename record**: `CulturalEventType` → `CulturalEventInstance`
2. **Add EventType property**: Link to domain enum
3. **Update all 106+ references** to use new name

#### Phase 2: Architectural Enhancement
1. **Strengthen relationship**: Instance must reference type enum
2. **Validation logic**: Ensure instance aligns with type characteristics
3. **Domain consistency**: Validate significance levels match event type

## Implementation Plan

### Immediate Actions (Critical Path)

1. **Rename Application Record**
   ```csharp
   // Before
   public record CulturalEventType(...)

   // After
   public record CulturalEventInstance(
       string EventId,
       string EventName,
       CulturalEventType EventType,     // References domain enum
       CulturalSignificance Significance,
       DateTime StartDate,
       DateTime EndDate);
   ```

2. **Update All References**
   - Search/replace in 106+ locations
   - Ensure proper namespace usage
   - Add EventType property where missing

3. **Validation Enhancement**
   ```csharp
   public record CulturalEventInstance
   {
       // ... properties

       public CulturalEventInstance
       {
           // Validation: Ensure significance aligns with event type
           if (EventType == CulturalEventType.VesakDayBuddhist &&
               Significance != CulturalSignificance.Sacred)
           {
               throw new ArgumentException("Vesak Day must have Sacred significance");
           }
       }
   }
   ```

### Files Requiring Updates

Based on grep analysis, primary files:
- `IDatabaseSecurityOptimizationEngine.cs` (source definition)
- `CoreMonitoringTypes.cs` (AlertSuppressionContext)
- `PerformanceCulturalEvent.cs` (CulturalEventCorrelation)
- `CulturalCalendarSynchronizationService.cs` (multiple usage)
- 100+ additional files with ambiguous references

## Architectural Benefits

### 1. Clean Architecture Compliance
- **Domain drives design**: Enum remains canonical classifier
- **Application layer**: Focuses on use cases, not domain definition
- **Dependency direction**: Application → Domain (correct)

### 2. Semantic Clarity
- **CulturalEventType**: "What kind of event is this?" (VesakDayBuddhist)
- **CulturalEventInstance**: "When is this specific event?" (Vesak Day 2024)

### 3. Type Safety
- **Compile-time validation**: EventType must be valid enum value
- **Business rule enforcement**: Significance must align with type
- **Performance optimization**: Enum-based classifications for queries

### 4. Maintainability
- **Single source of truth**: Domain enum for all type classifications
- **Extensibility**: New event types added to enum, instances reference them
- **Consistency**: All event-related logic uses same type system

## Risk Mitigation

### 1. Breaking Changes
- **Systematic approach**: Update all references simultaneously
- **Automated validation**: Scripts to verify complete migration
- **Compilation gates**: Ensure zero CS0104 errors

### 2. Data Consistency
- **Migration scripts**: Update existing data to include EventType
- **Default values**: Provide reasonable defaults for existing instances
- **Validation**: Ensure all instances have valid EventType references

### 3. Integration Impact
- **API contracts**: Update DTOs to use new naming
- **Database mappings**: Update EF Core configurations
- **Test updates**: Ensure all tests use new naming

## Success Criteria

1. **Zero CS0104 errors**: Complete elimination of ambiguous references
2. **Clean architecture**: Proper dependency direction maintained
3. **Semantic clarity**: Clear distinction between type and instance
4. **Type safety**: Strong relationship between enum and record
5. **Performance**: No degradation in query performance

## Long-term Vision

### Phase 3: Rich Domain Models
```csharp
// Future enhancement: Rich domain model
public class CulturalEvent : IAggregateRoot
{
    public CulturalEventType Type { get; private set; }
    public CulturalEventInstance Instance { get; private set; }
    public IReadOnlyList<CulturalEventRule> Rules { get; private set; }

    public void ValidateInstance()
    {
        // Business rules validation
        foreach (var rule in Rules.Where(r => r.AppliesToType(Type)))
        {
            rule.Validate(Instance);
        }
    }
}
```

## Decision Rationale

This decision resolves the immediate crisis while establishing a foundation for future architectural improvements. The rename to `CulturalEventInstance` creates semantic clarity and maintains Clean Architecture principles.

**Key architectural principle**: The domain layer owns all business concepts. Application layer should orchestrate, not define.

---

**Next Steps**: Execute systematic rename with zero-tolerance for compilation errors.