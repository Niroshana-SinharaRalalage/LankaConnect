# ADR: Hybrid Enum Strategy for Reference Data Management

**Status**: ✅ APPROVED
**Date**: 2025-12-27
**Architect**: System-architect agent a2d590d
**Context**: Phase 6A.47 - Reference Data Migration

---

## Context and Problem Statement

We migrated 41 enum types to a unified `reference_values` database table to eliminate hardcoding and enable runtime configurability. However, the application codebase still contains C# enum definitions and frontend hardcoded constants, creating architectural inconsistency.

**Key Question**: Should we keep C# enums for type safety or remove them entirely in favor of database-driven values?

---

## Decision Drivers

1. **Type Safety**: Compile-time checking prevents bugs in domain logic
2. **Runtime Flexibility**: Reference data should be configurable without redeployment
3. **DDD Principles**: Domain model integrity must be preserved
4. **Performance**: Minimize database calls for frequently accessed data
5. **Developer Experience**: Clear distinction between domain logic and operational data

---

## Considered Options

### Option A: Remove All Enums (Pure Database Approach)
**Description**: Delete all C# enum definitions, use string-based types everywhere

**Pros**:
- Complete runtime flexibility
- Single source of truth (database)
- No sync issues between code and database

**Cons**:
- ❌ Loss of compile-time type safety
- ❌ Increased runtime errors (typos, invalid values)
- ❌ Domain logic becomes fragile
- ❌ Violates DDD principles for core domain enums

### Option B: Keep All Enums (Pure Code Approach)
**Description**: Keep C# enums, use database only for display purposes

**Pros**:
- Full type safety
- Domain logic protected
- No breaking changes

**Cons**:
- ❌ Defeats purpose of database migration
- ❌ Still requires deployments for reference data changes
- ❌ Sync issues between enum values and database

### Option C: Generate Enums from Database at Compile Time
**Description**: Scaffold C# enums from database during build

**Pros**:
- Type safety maintained
- Database is source of truth

**Cons**:
- ❌ Complex build process
- ❌ Requires database access during compilation
- ❌ Doesn't solve runtime flexibility for operational data

### Option D: Hybrid Approach ✅ SELECTED
**Description**: Distinguish between domain enums (keep as C# enum) and operational reference data (database-driven)

**Pros**:
- ✅ Type safety for domain logic
- ✅ Runtime flexibility for operational data
- ✅ Follows DDD principles
- ✅ Clear architectural boundaries
- ✅ Best of both worlds

**Cons**:
- Requires clear categorization of each type
- Developers must understand which approach to use

---

## Decision Outcome

**CHOSEN**: Option D - Hybrid Approach

We will maintain TWO distinct patterns:

### 1. Domain Enums (Keep as C# Enum) - 3 Types

**Criteria**: Core domain logic, used in business rules, state machines, or authorization

**Types**:
- `UserRole` - Authorization logic, permission checks
- `EventStatus` - State machine (Draft→Published→Active→Completed)
- `EventCategory` - Business rules, event classification logic

**Usage Pattern**:
```csharp
// Domain logic uses enum
public bool CanCreateEvent(UserRole role)
{
    return role == UserRole.EventOrganizer
        || role == UserRole.EventOrganizerAndBusinessOwner
        || role == UserRole.Admin;
}

// Map to reference data for display
public async Task<ReferenceValueDto> GetRoleDisplay(UserRole role)
{
    return await _referenceDataService.GetByTypeAndCodeAsync("UserRole", role.ToString());
}
```

**Database Relationship**:
- Database `reference_values` table contains display metadata (name, description, icons)
- C# enum provides type safety and domain logic
- Extension methods map between enum and reference data

### 2. Reference Data (Database-Driven) - 38 Types

**Criteria**: Operational data, external data, frequently changing data, display-only data

**Types** (38 total):
- **Email**: EmailStatus, EmailType, EmailDeliveryStatus, EmailPriority (19 values)
- **Currency**: Currency (6 values)
- **Geography**: GeographicRegion (35 values), SriLankanLanguage (3 values)
- **Cultural**: BuddhistFestival (11), HinduFestival (10), CulturalBackground (8), CulturalCommunity (5), CulturalConflictLevel (5), ReligiousContext (10)
- **Business**: BusinessCategory (9), BusinessStatus (4), ServiceType (4), ReviewStatus (4)
- **Events**: EventType (10), RegistrationStatus (4), PaymentStatus (4), PricingType (3), PassPurchaseStatus (5)
- **System**: NotificationType (8), IdentityProvider (2), FederatedProvider (3), CalendarSystem (4), BadgePosition (4), PoyadayType (3)
- **Sign-ups**: SignUpItemCategory (4), SignUpType (2), AgeCategory (2), Gender (3)
- **Subscriptions**: SubscriptionStatus (5), ProficiencyLevel (5)
- **Forum**: ForumCategory (5), TopicStatus (4)
- **WhatsApp**: WhatsAppMessageStatus (5), WhatsAppMessageType (4)

**Usage Pattern**:
```csharp
// Fetch from database via service
var emailStatuses = await _referenceDataService.GetByTypeAsync("EmailStatus");

// Frontend fetches via API
const { data: emailStatuses } = useReferenceData(['EmailStatus']);
```

**No C# Enums**: These types exist ONLY in database, loaded dynamically at runtime

---

## Implementation Strategy

### Backend Implementation

**Step 1: Mark Domain Enums**
```csharp
// Add attribute to domain enums
[ReferenceDataType("UserRole")]
public enum UserRole
{
    GeneralUser = 1,
    BusinessOwner = 2,
    EventOrganizer = 3,
    // ...
}
```

**Step 2: Create Extension Methods**
```csharp
public static class UserRoleExtensions
{
    public static async Task<ReferenceValueDto> ToReferenceValueAsync(
        this UserRole role,
        IReferenceDataService service)
    {
        return await service.GetByTypeAndCodeAsync("UserRole", role.ToString());
    }

    public static UserRole FromReferenceValue(Guid referenceValueId,
        IReferenceDataService service)
    {
        var refData = await service.GetByIdAsync(referenceValueId);
        return Enum.Parse<UserRole>(refData.Code);
    }
}
```

**Step 3: Update Service Layer**
```csharp
public class ReferenceDataService : IReferenceDataService
{
    // For domain enums - return typed results
    public async Task<IReadOnlyList<UserRoleDto>> GetUserRolesAsync()
    {
        // Fetch from cache/database
        // Map to domain enum + metadata
    }

    // For reference data - return generic results
    public async Task<IReadOnlyList<ReferenceValueDto>> GetByTypesAsync(
        IEnumerable<string> types)
    {
        // Fetch from cache/database
        // Return raw reference data
    }
}
```

### Frontend Implementation

**Step 1: Create Typed Hooks**
```typescript
// For domain enums - strongly typed
export const useUserRoles = () => {
    return useQuery<UserRoleDto[]>(
        ['user-roles'],
        () => api.getUserRoles(),
        { staleTime: 3600000 }
    );
};

// For reference data - generic
export const useReferenceData = (types: string[]) => {
    return useQuery<ReferenceValue[]>(
        ['reference-data', ...types],
        () => api.getReferenceData(types),
        { staleTime: 3600000 }
    );
};
```

**Step 2: Replace Hardcoded Constants**
```typescript
// BEFORE (hardcoded)
const CULTURAL_INTERESTS = [ ... ];

// AFTER (API-driven)
const { data: culturalInterests } = useCulturalInterests();
```

---

## Consequences

### Positive Consequences

1. **Type Safety Preserved**: Domain logic uses compile-time checked enums
2. **Runtime Flexibility**: Operational data configurable without deployment
3. **DDD Compliance**: Domain model integrity maintained
4. **Performance**: IMemoryCache reduces database calls
5. **Clear Boundaries**: Developers know which pattern to use

### Negative Consequences

1. **Two Patterns**: Developers must understand when to use each approach
2. **Mapping Logic**: Need extension methods to map between enum and reference data
3. **Documentation**: Must clearly document which types are domain vs reference

### Mitigation Strategies

1. **Clear Documentation**: This ADR + inline code comments
2. **Code Reviews**: Ensure new enums are categorized correctly
3. **Naming Convention**: Domain enums in `Domain/Enums/`, reference data via API only
4. **Developer Guide**: Update onboarding docs with examples

---

## Related Decisions

- [PHASE_6A47_UNIFIED_REFERENCE_DATA_ARCHITECTURE.md](./PHASE_6A47_UNIFIED_REFERENCE_DATA_ARCHITECTURE.md) - Database schema design
- [PHASE_6A47_COMPLETION_PLAN_APPROVED.md](./PHASE_6A47_COMPLETION_PLAN_APPROVED.md) - Implementation plan

---

## Decision Matrix

| Type | Category | Keep C# Enum? | Database? | Rationale |
|------|----------|---------------|-----------|-----------|
| UserRole | Domain | ✅ Yes | ✅ Yes (metadata) | Authorization logic requires type safety |
| EventStatus | Domain | ✅ Yes | ✅ Yes (metadata) | State machine logic |
| EventCategory | Domain | ✅ Yes | ✅ Yes (metadata) | Business rule classification |
| EmailStatus | Reference | ❌ No | ✅ Yes | Operational data, changes without deployment |
| Currency | Reference | ❌ No | ✅ Yes | External data, updated frequently |
| GeographicRegion | Reference | ❌ No | ✅ Yes | External data, 35+ regions |
| BuddhistFestival | Reference | ❌ No | ✅ Yes | Calendar data, cultural context |
| (35 others) | Reference | ❌ No | ✅ Yes | See categorization above |

---

## Approval

- **Architect**: ✅ APPROVED (agent a2d590d, 2025-12-27)
- **Rationale**: Hybrid approach balances type safety with flexibility
- **Concerns Addressed**: DDD principles preserved, clear categorization provided
- **Next Steps**: Implement Phase 2 (Cultural Interests API endpoint)

---

## Revision History

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-12-27 | 1.0 | Initial ADR creation | Claude Sonnet 4.5 |

---

**Last Updated**: 2025-12-27
**Status**: APPROVED - Ready for Implementation
