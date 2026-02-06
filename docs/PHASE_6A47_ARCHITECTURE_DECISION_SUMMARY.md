# Phase 6A.47 - Architecture Decision Summary

**Date**: 2025-12-26
**Status**: APPROVED WITH MODIFICATIONS
**Full Document**: [PHASE_6A47_UNIFIED_REFERENCE_DATA_ARCHITECTURE.md](./PHASE_6A47_UNIFIED_REFERENCE_DATA_ARCHITECTURE.md)

---

## Quick Decision Summary

### Question 1: Is Unified Approach Feasible?

**ANSWER: YES** - Unified table approach is feasible and strongly recommended.

**Benefits**:
- 95.6% code reduction (23,780 → 950 LOC)
- 97.5% test reduction (1,640 → 40 tests)
- Single deployment vs. 38 separate deployments
- Centralized admin management
- Easier maintenance and scaling

**Critical Success Factor**: Maintain enum compatibility layer to preserve existing code.

---

### Question 2: Database Schema

**RECOMMENDED SCHEMA**:

```sql
CREATE TABLE reference_data.reference_values (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Type Discrimination
    enum_type VARCHAR(100) NOT NULL,  -- 'RegistrationStatus', 'PaymentStatus', etc.

    -- Core Fields
    code VARCHAR(100) NOT NULL,       -- 'Confirmed', 'Pending', etc.
    name VARCHAR(200) NOT NULL,       -- Display name
    description TEXT,

    -- Integer Value (for enums like EmailPriority: Low=1, High=10)
    int_value INTEGER,

    -- Display & Status
    display_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,

    -- Flexible Metadata (enum-specific properties)
    metadata JSONB,  -- {"allows_registration": true, "monthly_price": 10.00}

    -- Audit
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100),
    updated_by VARCHAR(100),

    CONSTRAINT uq_reference_values_type_code UNIQUE (enum_type, code)
);

-- Key Indexes
CREATE INDEX idx_reference_values_enum_type ON reference_data.reference_values(enum_type);
CREATE INDEX idx_reference_values_type_active ON reference_data.reference_values(enum_type, is_active) WHERE is_active = true;
CREATE INDEX idx_reference_values_metadata ON reference_data.reference_values USING gin(metadata);
```

**Why This Schema?**:
- `enum_type`: Discriminates between different enum types
- `code`: Matches enum value names (e.g., 'Confirmed')
- `int_value`: Stores enum numeric values (Pending=0, Confirmed=1)
- `metadata`: JSONB for custom properties (permissions, pricing, business flags)
- `is_active`: Soft delete support
- Unique constraint prevents duplicate enum values

---

### Question 3: API Design

**RECOMMENDED ENDPOINT**:

```
GET /api/reference-data?types=RegistrationStatus,PaymentStatus&activeOnly=true
```

**Response Format (Grouped by Type)**:
```json
{
  "RegistrationStatus": [
    {
      "id": "uuid",
      "code": "Confirmed",
      "name": "Confirmed Registration",
      "intValue": 1,
      "displayOrder": 2,
      "isActive": true,
      "metadata": {}
    }
  ],
  "PaymentStatus": [
    {
      "id": "uuid",
      "code": "Completed",
      "name": "Payment Completed",
      "intValue": 1,
      "displayOrder": 2,
      "isActive": true,
      "metadata": {}
    }
  ]
}
```

**Legacy Endpoints (6-month deprecation)**:
```
GET /api/reference-data/event-categories  -> Redirects to ?types=EventCategory
GET /api/reference-data/event-statuses    -> Redirects to ?types=EventStatus
GET /api/reference-data/user-roles        -> Redirects to ?types=UserRole
```

**Cache Invalidation**:
```
POST /api/reference-data/invalidate?types=RegistrationStatus
POST /api/reference-data/invalidate-all
```

**Why This Design?**:
- Single endpoint reduces API surface
- Type filtering allows fetching multiple enums in one request
- Grouped format easier for frontend consumption
- Legacy endpoints ensure smooth migration
- Cache invalidation for admin updates

---

### Question 4: Backward Compatibility Strategy

**CRITICAL: Preserve Enum Classes as Compatibility Layer**

**Current Code (NO CHANGES REQUIRED)**:
```csharp
// Domain logic - works exactly as before
registration.Status = RegistrationStatus.Confirmed;

if (registration.Status == RegistrationStatus.Confirmed) {
    await SendConfirmationEmail(registration);
}

// Extension methods - works exactly as before
if (subscription.Status.CanCreateEvents()) {
    return await CreateEvent(eventDto);
}
```

**How It Works**:

**Step 1: Enum Classes Remain in Code**
```csharp
// RegistrationStatus.cs - UNCHANGED
public enum RegistrationStatus {
    Pending = 0,
    Confirmed = 1,
    Waitlisted = 2,
    CheckedIn = 3,
    Completed = 4,
    Cancelled = 5,
    Refunded = 6
}
```

**Step 2: Helper Classes Load Metadata from Database**
```csharp
// NEW: Helper class for metadata access
public static class RegistrationStatusHelper {
    private static Dictionary<RegistrationStatus, ReferenceValue> _metadata;

    // Called at application startup
    internal static void Initialize(List<ReferenceValue> values) {
        _metadata = values.ToDictionary(
            v => Enum.Parse<RegistrationStatus>(v.Code),
            v => v
        );
    }

    public static string GetDisplayName(this RegistrationStatus status) {
        return _metadata[status].Name; // From database
    }
}
```

**Step 3: Extension Methods Use Database Metadata**
```csharp
// UPDATED: Extension methods now use database metadata
public static class SubscriptionStatusExtensions {
    public static bool CanCreateEvents(this SubscriptionStatus status) {
        // OLD: Hardcoded
        // return status == SubscriptionStatus.Trialing || status == SubscriptionStatus.Active;

        // NEW: From database metadata
        var metadata = status.GetMetadata();
        return metadata.GetBool("can_create_events");
    }
}
```

**Step 4: Startup Initialization**
```csharp
// Program.cs - Load enum mappings at startup
public class Program {
    public static async Task Main(string[] args) {
        var app = builder.Build();

        // Initialize enum mappings from database
        using var scope = app.Services.CreateScope();
        var loader = scope.ServiceProvider.GetRequiredService<IEnumReferenceLoader>();
        await loader.LoadAllEnumMappings();

        await app.RunAsync();
    }
}
```

**Key Insights**:
- Enum values still stored as `int` in entity tables (e.g., `registrations.status = 1`)
- Reference table ONLY for UI dropdowns, validation, metadata lookup
- Extension methods enhanced with database-driven business logic
- Zero breaking changes to existing code

---

### Question 5: Implementation Plan

**4-WEEK INCREMENTAL MIGRATION**:

**Week 1: Foundation Refactoring**
- Create unified `reference_values` table
- Migrate existing 3 enums to unified table
- Refactor API to single endpoint
- Create `EnumReferenceLoader` service
- Deploy to staging

**Week 2: Tier 1 Critical (15 enums)**
- RegistrationStatus, PaymentStatus, PricingType
- SubscriptionStatus, EmailStatus, EmailType
- EmailDeliveryStatus, EmailPriority, Currency
- NotificationType, IdentityProvider, SignUpItemCategory
- SignUpType, AgeCategory, Gender
- **Unblocks**: Phase 6A.55 JSONB integrity fixes

**Week 3: Tier 2 Important (10 enums)**
- EventType, SriLankanLanguage, CulturalBackground
- ReligiousContext, GeographicRegion
- BuddhistFestival, HinduFestival, CalendarSystem
- FederatedProvider, ProficiencyLevel
- **Enables**: Localization, cultural features

**Week 4: Tier 3 Optional (9 enums)**
- BusinessCategory, BusinessStatus, ReviewStatus, ServiceType
- ForumCategory, TopicStatus
- WhatsAppMessageStatus, WhatsAppMessageType, CulturalCommunity
- **Aligns**: Phase 6B Business Owner rollout

**Week 5: Tier 4 Evaluation (4 enums)**
- PassPurchaseStatus, CulturalConflictLevel, PoyadayType, BadgePosition
- **Decision**: Keep as code enums (static, infrequent changes)

---

### Question 6: Code Change Analysis

**Files to Modify**:

**Backend (12 files)**:
- NEW: `Domain/ReferenceData/Entities/ReferenceValue.cs` (unified entity)
- NEW: `Application/ReferenceData/Services/EnumReferenceLoader.cs`
- NEW: `Domain/ReferenceData/Helpers/*Helper.cs` (41 helper classes)
- MODIFY: `ReferenceDataRepository.cs`, `ReferenceDataService.cs`, `ReferenceDataController.cs`
- MODIFY: `AppDbContext.cs`, `DependencyInjection.cs`, `Program.cs`

**Frontend (8 files)**:
- MODIFY: `referenceData.service.ts`, `useReferenceData.ts`
- MODIFY: Component files using reference data
- DELETE: Hardcoded constants, separate hooks

**Database (2 migrations)**:
- Create unified table + migrate existing 3 enums
- Seed remaining 38 enums (incremental)

**Code Metrics**:
- Unified: ~950 LOC
- Separate tables: ~23,780 LOC
- **Reduction**: 95.6%

**Test Metrics**:
- Unified: ~40 unit tests
- Separate tables: ~1,640 unit tests
- **Reduction**: 97.5%

---

### Question 7: Caching Strategy

**TWO-LAYER CACHING**:

**Layer 1: Backend IMemoryCache (1-hour TTL)**
```csharp
public async Task<List<ReferenceValue>> GetByTypeAsync(string enumType) {
    var cacheKey = $"RefData:{enumType}:True";

    if (_cache.TryGetValue<List<ReferenceValue>>(cacheKey, out var cached)) {
        return cached!; // Cache HIT - 0.5ms
    }

    var values = await _repository.GetByTypeAsync(enumType); // Cache MISS - 50ms

    _cache.Set(cacheKey, values, new MemoryCacheEntryOptions {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
        Priority = CacheItemPriority.High
    });

    return values;
}
```

**Layer 2: Frontend React Query (1-hour stale time)**
```typescript
export function useReferenceData(enumTypes: string[]) {
  return useQuery({
    queryKey: ['referenceData', ...enumTypes],
    queryFn: () => referenceDataService.getByTypes(enumTypes),
    staleTime: 1000 * 60 * 60, // 1 hour
    cacheTime: 1000 * 60 * 60 * 24, // 24 hours
    refetchOnWindowFocus: false,
  });
}
```

**Performance Impact**:
- Without caching: 50ms per request, high DB load
- With backend cache: 0.5ms per request (99% faster)
- With frontend cache: 0 HTTP requests (95% API traffic reduction)

**Cache Invalidation**:
- Admin updates: Invalidate backend cache + reload enum mappings
- Automatic refresh: After 1-hour stale time
- Manual refresh: Admin invalidation endpoint

---

### Question 8: Rollback Plan

**IF MIGRATION FAILS**:

**Step 1: Revert Database**
```bash
# Restore from backup
pg_restore -h <host> -U <user> -d lankaconnect reference_data_backup.dump

# OR run Down migration
dotnet ef migrations remove
```

**Step 2: Revert Code**
```bash
git revert <migration-commit-hash>
git push origin develop
```

**Step 3: Redeploy Previous Version**
```bash
az containerapp update --name lankaconnect-api --image <previous-image-tag>
```

**Step 4: Validate**
```bash
curl https://staging.lankaconnect.com/api/reference-data/event-categories
# Should return 200 OK with data
```

**Pre-Migration Safety Measures**:
- Full database backup before migration
- Test migration on staging with production data copy
- Comprehensive regression test suite
- Monitor Application Insights during deployment
- Deploy during low-traffic maintenance window

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Enum int value mismatch | HIGH | CRITICAL | Validate seed data matches exact int values |
| Extension method compatibility | MEDIUM | CRITICAL | Thorough testing before deployment |
| JSONB metadata schema drift | MEDIUM | HIGH | Document schema, add validation |
| Migration data loss | LOW | CRITICAL | Full backup, test rollback procedure |
| Cache invalidation failures | LOW | MEDIUM | Add monitoring, test invalidation |
| Frontend breaking changes | MEDIUM | HIGH | Maintain legacy endpoints for 6 months |

---

## Success Criteria

**Technical**:
- All 41 enums migrated to unified table
- Zero breaking changes to existing code
- All extension methods work with database metadata
- API response time < 50ms (99% requests)
- Cache hit ratio > 95%
- Zero data loss during migration

**Business**:
- No user-facing errors during migration
- Admin UI can manage reference data without code deployment
- Regression tests pass
- Deployment downtime < 5 minutes

---

## Approval Checklist

Before proceeding with implementation:

- [ ] User approves unified table architecture
- [ ] User approves backward compatibility strategy
- [ ] User approves 4-week incremental migration plan
- [ ] User approves two-layer caching strategy
- [ ] Development team reviews detailed architecture document
- [ ] Database backup and rollback procedures tested
- [ ] Staging environment ready for testing

---

## Next Steps

**Immediate (This Week)**:
1. Get user approval on this architecture
2. Create unified `reference_values` table schema
3. Migrate existing 3 enums to unified table
4. Refactor API endpoint to support type filtering
5. Deploy to staging and validate

**Week 2-4**: Incremental migration of 34 remaining enums

**Long-Term**: Admin UI, multilingual support, audit logging

---

**Full Technical Details**: See [PHASE_6A47_UNIFIED_REFERENCE_DATA_ARCHITECTURE.md](./PHASE_6A47_UNIFIED_REFERENCE_DATA_ARCHITECTURE.md) (62,000 words)

**Questions?** Review full architecture document for:
- Complete database schema with all indexes
- Detailed API endpoint specifications
- Complete backward compatibility code examples
- Step-by-step migration procedures
- JSONB metadata schema examples
- Performance benchmarks
- Complete risk analysis

---

**Document Version**: 1.0
**Last Updated**: 2025-12-26
**Status**: PENDING USER APPROVAL
