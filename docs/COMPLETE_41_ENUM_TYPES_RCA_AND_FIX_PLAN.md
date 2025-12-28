# Complete 41 Enum Types - Root Cause Analysis & Fix Plan

**Created**: 2025-12-28
**Phase**: 6A.47 Post-Mortem Analysis
**Status**: Critical - Systematic Fix Required

---

## Executive Summary

**THE PROBLEM**: Phase 6A.47 only migrated 2 enum types (EventCategory, EventStatus) to the reference data API when there are **41 enum types** in the unified `reference_values` table. This incomplete migration leaves 39 enum types still hardcoded in the frontend.

**ROOT CAUSE**: Incomplete analysis - I only searched for dropdown usage of EventCategory/EventStatus and failed to audit ALL 41 enum types systematically.

**IMPACT**:
- 39 enum types remain hardcoded in frontend (Currency, BadgePosition, UserRole, etc.)
- Future enum changes require frontend code changes instead of database updates
- Inconsistent data management across the application

---

## Section 1: Complete Enum Type Inventory (All 41 Types)

### Source: Migration File
`src/LankaConnect.Infrastructure/Data/Migrations/20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues.cs`

### Complete List of 41 Enum Types

| # | Enum Type | Tier | Values Count | Backend Location | Frontend Location |
|---|-----------|------|--------------|------------------|-------------------|
| 1 | **EventCategory** | 1 | 8 | `Domain/Events/Enums/EventCategory.cs` | `web/src/infrastructure/api/types/events.types.ts` ✅ |
| 2 | **EventStatus** | 1 | 8 | `Domain/Events/Enums/EventStatus.cs` | `web/src/infrastructure/api/types/events.types.ts` ✅ |
| 3 | **UserRole** | 1 | 6 | NOT FOUND IN DOMAIN | `web/src/infrastructure/api/types/auth.types.ts` |
| 4 | **RegistrationStatus** | 1 | 4 | `Domain/Events/Enums/RegistrationStatus.cs` | `web/src/infrastructure/api/types/events.types.ts` |
| 5 | **PaymentStatus** | 1 | 4 | `Domain/Events/Enums/PaymentStatus.cs` | `web/src/infrastructure/api/types/events.types.ts` |
| 6 | **PricingType** | 1 | 3 | `Domain/Events/Enums/PricingType.cs` | `web/src/infrastructure/api/types/events.types.ts` |
| 7 | **SubscriptionStatus** | 1 | 5 | `Domain/Users/Enums/SubscriptionStatus.cs` | `web/src/infrastructure/api/types/subscription.types.ts` |
| 8 | **EmailStatus** | 1 | 11 | `Domain/Communications/Enums/EmailStatus.cs` | NOT IN FRONTEND |
| 9 | **EmailType** | 1 | 9 | `Domain/Communications/Enums/EmailType.cs` | NOT IN FRONTEND |
| 10 | **EmailDeliveryStatus** | 1 | 8 | `Domain/Communications/Enums/EmailDeliveryStatus.cs` | NOT IN FRONTEND |
| 11 | **EmailPriority** | 1 | 4 | `Domain/Communications/Enums/EmailPriority.cs` | NOT IN FRONTEND |
| 12 | **Currency** | 1 | 6 | `Domain/Shared/Enums/Currency.cs` | `web/src/infrastructure/api/types/events.types.ts` |
| 13 | **NotificationType** | 1 | 8 | `Domain/Notifications/Enums/NotificationType.cs` | NOT IN FRONTEND |
| 14 | **IdentityProvider** | 1 | 2 | `Domain/Users/Enums/IdentityProvider.cs` | NOT IN FRONTEND |
| 15 | **SignUpItemCategory** | 1 | 4 | NOT FOUND IN DOMAIN | `web/src/infrastructure/api/types/events.types.ts` |
| 16 | **SignUpType** | 1 | 2 | NOT FOUND IN DOMAIN | `web/src/infrastructure/api/types/events.types.ts` |
| 17 | **AgeCategory** | 1 | 2 | `Domain/Events/Enums/AgeCategory.cs` | `web/src/infrastructure/api/types/events.types.ts` |
| 18 | **Gender** | 1 | 3 | `Domain/Events/Enums/Gender.cs` | `web/src/infrastructure/api/types/events.types.ts` |
| 19 | **EventType** | 2 | 10 | `Domain/Events/Enums/EventType.cs` | NOT IN FRONTEND |
| 20 | **SriLankanLanguage** | 2 | 3 | `Domain/Communications/Enums/SriLankanLanguage.cs` | NOT IN FRONTEND |
| 21 | **CulturalBackground** | 2 | 8 | `Domain/Communications/Enums/CulturalBackground.cs` | NOT IN FRONTEND |
| 22 | **ReligiousContext** | 2 | 10 | `Domain/Communications/Enums/ReligiousContext.cs` | NOT IN FRONTEND |
| 23 | **GeographicRegion** | 2 | 35 | `Domain/Common/Enums/GeographicRegion.cs` | NOT IN FRONTEND |
| 24 | **BuddhistFestival** | 2 | 11 | `Domain/Communications/Enums/BuddhistFestival.cs` | NOT IN FRONTEND |
| 25 | **HinduFestival** | 2 | 10 | `Domain/Communications/Enums/HinduFestival.cs` | NOT IN FRONTEND |
| 26 | **CalendarSystem** | 3 | 4 | `Domain/Communications/Enums/CalendarSystem.cs` | NOT IN FRONTEND |
| 27 | **FederatedProvider** | 3 | 3 | `Domain/Users/Enums/FederatedProvider.cs` | NOT IN FRONTEND |
| 28 | **ProficiencyLevel** | 3 | 5 | `Domain/Users/Enums/ProficiencyLevel.cs` | NOT IN FRONTEND |
| 29 | **BusinessCategory** | 3 | 9 | `Domain/Business/Enums/BusinessCategory.cs` | NOT IN FRONTEND |
| 30 | **BusinessStatus** | 3 | 4 | `Domain/Business/Enums/BusinessStatus.cs` | NOT IN FRONTEND |
| 31 | **ReviewStatus** | 3 | 4 | `Domain/Business/Enums/ReviewStatus.cs` | NOT IN FRONTEND |
| 32 | **ServiceType** | 3 | 4 | `Domain/Business/Enums/ServiceType.cs` | NOT IN FRONTEND |
| 33 | **ForumCategory** | 4 | 5 | `Domain/Community/Enums/ForumCategory.cs` | NOT IN FRONTEND |
| 34 | **TopicStatus** | 4 | 4 | `Domain/Community/Enums/TopicStatus.cs` | NOT IN FRONTEND |
| 35 | **WhatsAppMessageStatus** | 4 | 5 | `Domain/Communications/Enums/WhatsAppMessageStatus.cs` | NOT IN FRONTEND |
| 36 | **WhatsAppMessageType** | 4 | 4 | `Domain/Communications/Enums/WhatsAppMessageType.cs` | NOT IN FRONTEND |
| 37 | **CulturalCommunity** | 3 | 5 | `Domain/Communications/Enums/CulturalCommunity.cs` | NOT IN FRONTEND |
| 38 | **PassPurchaseStatus** | 4 | 5 | `Domain/Events/Enums/PassPurchaseStatus.cs` | NOT IN FRONTEND |
| 39 | **CulturalConflictLevel** | 3 | 5 | `Domain/Events/Enums/CulturalConflictLevel.cs` | NOT IN FRONTEND |
| 40 | **PoyadayType** | 3 | 3 | `Domain/Events/Enums/PoyadayType.cs` | NOT IN FRONTEND |
| 41 | **BadgePosition** | 4 | 4 | `Domain/Badges/Enums/BadgePosition.cs` | `web/src/infrastructure/api/types/badges.types.ts` |

---

## Section 2: Usage Analysis for Each Enum Type

### Category A: UI Dropdown Usage (MUST Migrate to API)

These enum types are used in dropdowns/selects and currently have hardcoded arrays.

| Enum Type | Current Hardcoded Location | Priority | Action Required |
|-----------|---------------------------|----------|-----------------|
| **EventCategory** | `EventEditForm.tsx` lines 387-396 | ✅ DONE | Already migrated in Phase 6A.47 |
| **Currency** | `EventEditForm.tsx` lines 765-767, 820-822, 870-872, 1017-1019 | **HIGH** | Create `useCurrencies()` hook |
| **UserRole** | NOT IN UI (server-side only) | LOW | No dropdown - used for authorization |
| **RegistrationStatus** | NOT IN UI DROPDOWN | LOW | Used for display labels only |
| **BadgePosition** | DEPRECATED - UI uses `BadgeLocationConfigDto` | N/A | Already migrated to percentage positioning |

**Action Items**:
1. ✅ EventCategory - Already done
2. 🔴 Currency - Create specialized hook `useCurrencies()` for dropdowns
3. ⚪ BadgePosition - Deprecated, no action needed

---

### Category B: Display Labels Only (MUST Migrate to API)

These enum types are used for showing enum value names (e.g., mapping 0 → "Active") but NOT in dropdowns.

| Enum Type | Usage Pattern | Priority | Action Required |
|-----------|--------------|----------|-----------------|
| **EventStatus** | Display label in event cards | ✅ DONE | Already migrated in Phase 6A.47 |
| **PaymentStatus** | Display payment state | MEDIUM | Use `getNameFromIntValue()` utility |
| **PricingType** | Display pricing model | LOW | Use `getNameFromIntValue()` utility |
| **RegistrationStatus** | Display registration state | MEDIUM | Use `getNameFromIntValue()` utility |
| **SubscriptionStatus** | Display subscription state | MEDIUM | Use `getNameFromIntValue()` utility |
| **AgeCategory** | Display "Adult" / "Child" | LOW | Use `getNameFromIntValue()` utility |
| **Gender** | Display gender labels | LOW | Use `getNameFromIntValue()` utility |
| **SignUpItemCategory** | Display sign-up category | LOW | Use `getNameFromIntValue()` utility |
| **SignUpType** | Display sign-up type | LOW | Use `getNameFromIntValue()` utility |

**Action Items**:
1. Create utility functions to get display names from API data
2. Replace hardcoded string mappings with API lookups

---

### Category C: Business Logic Only (CORRECT - Keep as-is)

These enum types are used for comparisons, validation, and type safety. They should NOT be changed.

| Enum Type | Usage Pattern | Keep As-Is? | Rationale |
|-----------|--------------|-------------|-----------|
| **EventStatus** | `status === EventStatus.Active` | ✅ YES | Type-safe comparisons |
| **Currency** | `currency: Currency.USD` | ✅ YES | Type-safe assignments |
| **PaymentStatus** | `paymentStatus === PaymentStatus.Completed` | ✅ YES | Business logic checks |
| **RegistrationStatus** | `status === RegistrationStatus.Confirmed` | ✅ YES | State comparisons |
| **PricingType** | `pricingType === PricingType.GroupTiered` | ✅ YES | Pricing logic |
| **AgeCategory** | `ageCategory: AgeCategory.Adult` | ✅ YES | Type-safe form data |
| **Gender** | `gender: Gender.Male` | ✅ YES | Type-safe form data |
| **SignUpItemCategory** | `category === SignUpItemCategory.Mandatory` | ✅ YES | Business rules |
| **SignUpType** | `type === SignUpType.Predefined` | ✅ YES | Feature flags |
| **BadgePosition** | DEPRECATED (use `BadgeLocationConfigDto`) | N/A | Replaced by percentage model |

**Usage Statistics**:
- Currency enum: **48 usages** across frontend (type-safe comparisons, form defaults)
- BadgePosition enum: **25 usages** across frontend (DEPRECATED, being phased out)

**NO ACTION NEEDED**: These TypeScript enums provide compile-time type safety and should remain.

---

### Category D: Not Used in Frontend (Backend Only)

These enum types exist in the database but are not currently used in frontend code.

| Enum Type | Backend Usage | Frontend Status | Migration Priority |
|-----------|--------------|-----------------|-------------------|
| EmailStatus | Email processing system | NOT IN FRONTEND | DEFER |
| EmailType | Email categorization | NOT IN FRONTEND | DEFER |
| EmailDeliveryStatus | Email tracking | NOT IN FRONTEND | DEFER |
| EmailPriority | Email queue priority | NOT IN FRONTEND | DEFER |
| NotificationType | Notification system | NOT IN FRONTEND | DEFER |
| IdentityProvider | Authentication backend | NOT IN FRONTEND | DEFER |
| EventType | Event classification | NOT IN FRONTEND | DEFER |
| SriLankanLanguage | Language preferences | NOT IN FRONTEND | DEFER |
| CulturalBackground | User profiles | NOT IN FRONTEND | DEFER |
| ReligiousContext | Cultural intelligence | NOT IN FRONTEND | DEFER |
| GeographicRegion | Location metadata | NOT IN FRONTEND | DEFER |
| BuddhistFestival | Cultural calendar | NOT IN FRONTEND | DEFER |
| HinduFestival | Cultural calendar | NOT IN FRONTEND | DEFER |
| CalendarSystem | Date conversion | NOT IN FRONTEND | DEFER |
| FederatedProvider | OAuth providers | NOT IN FRONTEND | DEFER |
| ProficiencyLevel | Language skills | NOT IN FRONTEND | DEFER |
| BusinessCategory | Business directory | NOT IN FRONTEND | DEFER |
| BusinessStatus | Business verification | NOT IN FRONTEND | DEFER |
| ReviewStatus | Review moderation | NOT IN FRONTEND | DEFER |
| ServiceType | Business services | NOT IN FRONTEND | DEFER |
| ForumCategory | Community forums | NOT IN FRONTEND | DEFER |
| TopicStatus | Forum topics | NOT IN FRONTEND | DEFER |
| WhatsAppMessageStatus | WhatsApp integration | NOT IN FRONTEND | DEFER |
| WhatsAppMessageType | WhatsApp integration | NOT IN FRONTEND | DEFER |
| CulturalCommunity | Cultural grouping | NOT IN FRONTEND | DEFER |
| PassPurchaseStatus | Pass system | NOT IN FRONTEND | DEFER |
| CulturalConflictLevel | Conflict detection | NOT IN FRONTEND | DEFER |
| PoyadayType | Buddhist calendar | NOT IN FRONTEND | DEFER |

**Action**: Document for future migration when these features are built in frontend.

---

## Section 3: Current Hardcoded Usage Audit

### Hardcoded Dropdown Arrays Found

#### File: `web/src/presentation/components/features/events/EventEditForm.tsx`

**Lines 387-396: EventCategory dropdown**
```typescript
const categoryOptions = [
  { value: EventCategory.Religious, label: 'Religious' },
  { value: EventCategory.Cultural, label: 'Cultural' },
  { value: EventCategory.Community, label: 'Community' },
  { value: EventCategory.Educational, label: 'Educational' },
  { value: EventCategory.Social, label: 'Social' },
  { value: EventCategory.Business, label: 'Business' },
  { value: EventCategory.Charity, label: 'Charity' },
  { value: EventCategory.Entertainment, label: 'Entertainment' },
];
```
**Status**: ✅ Already fixed in Phase 6A.47

**Lines 765-767, 820-822, 870-872, 1017-1019: Currency dropdown (4 locations)**
```typescript
<select>
  <option value={Currency.USD}>USD ($)</option>
  <option value={Currency.LKR}>LKR (Rs)</option>
</select>
```
**Status**: 🔴 NEEDS FIX - Hardcoded, only shows 2 of 6 currencies

---

### Enum Usage Statistics

| Enum Type | Frontend Usages | Dropdown Usages | Display Label Usages | Business Logic Usages |
|-----------|----------------|-----------------|---------------------|---------------------|
| EventCategory | ~30 | 1 (FIXED) | ~5 | ~24 |
| EventStatus | ~25 | 0 | ~10 | ~15 |
| Currency | **48** | **4 dropdowns** | ~8 | ~36 |
| BadgePosition | 25 | 0 (DEPRECATED) | 0 | 25 (being phased out) |
| RegistrationStatus | ~15 | 0 | ~8 | ~7 |
| PaymentStatus | ~12 | 0 | ~6 | ~6 |
| PricingType | ~8 | 0 | ~4 | ~4 |
| AgeCategory | ~10 | 0 | ~5 | ~5 |
| Gender | ~8 | 0 | ~4 | ~4 |
| SignUpItemCategory | ~6 | 0 | ~3 | ~3 |
| SignUpType | ~4 | 0 | ~2 | ~2 |
| SubscriptionStatus | ~5 | 0 | ~3 | ~2 |

---

## Section 4: Priority Matrix

### High Priority (User-Facing Impact)

| Enum Type | Impact | Frequency | Complexity | Priority Score | Fix Order |
|-----------|--------|-----------|------------|----------------|-----------|
| **Currency** | HIGH | HIGH (4 dropdowns) | LOW | **10/10** | 🔴 **FIX NOW** |

### Medium Priority (Internal Systems)

| Enum Type | Impact | Frequency | Complexity | Priority Score | Fix Order |
|-----------|--------|-----------|------------|----------------|-----------|
| RegistrationStatus | MEDIUM | MEDIUM | LOW | 6/10 | Phase 6A.48 |
| PaymentStatus | MEDIUM | MEDIUM | LOW | 6/10 | Phase 6A.48 |
| SubscriptionStatus | MEDIUM | LOW | LOW | 5/10 | Phase 6A.49 |

### Low Priority (Future Features)

All Category D enums (backend-only) can be deferred until their frontend features are implemented.

---

## Section 5: Implementation Strategy

### Phase 6A.48: Currency Dropdown Migration (IMMEDIATE)

**Scope**: Migrate hardcoded Currency dropdown to API

**Steps**:
1. ✅ API endpoint already exists: `GET /api/reference-data?types=Currency`
2. 🔴 Create `useCurrencies()` custom hook
3. 🔴 Replace 4 hardcoded dropdowns in `EventEditForm.tsx`
4. 🔴 Update tests

**Files to Change**:
- `web/src/presentation/hooks/useCurrencies.ts` (NEW)
- `web/src/presentation/components/features/events/EventEditForm.tsx` (4 locations)

**Estimated Effort**: 1 hour

---

### Phase 6A.49: Display Label Migration (LOW PRIORITY)

**Scope**: Replace hardcoded enum label mappings with API lookups

**Enums to Fix**:
- RegistrationStatus
- PaymentStatus
- PricingType
- SubscriptionStatus
- AgeCategory
- Gender
- SignUpItemCategory
- SignUpType

**Strategy**: Create generic utility functions
```typescript
// web/src/infrastructure/api/utils/enum-display.ts
export function useEnumDisplayName(enumType: string, intValue: number): string | undefined {
  const { data } = useReferenceData([enumType]);
  return getNameFromIntValue(data?.[enumType], intValue);
}
```

**Estimated Effort**: 2-3 hours

---

### Phase 6A.50: Future Feature Enums (DEFERRED)

**Scope**: Migrate backend-only enums when their frontend features are built

**Enums**:
- EmailStatus, EmailType, EmailDeliveryStatus, EmailPriority (Email system UI)
- NotificationType (Notification preferences UI)
- GeographicRegion, SriLankanLanguage (User profile enhancements)
- BuddhistFestival, HinduFestival, ReligiousContext (Cultural calendar UI)
- BusinessCategory, BusinessStatus, ReviewStatus, ServiceType (Business directory UI)
- ForumCategory, TopicStatus (Community forums UI)
- WhatsAppMessageStatus, WhatsAppMessageType (WhatsApp integration UI)

**Trigger**: When feature development begins for these modules

---

## Section 6: Gap Analysis

### What's in reference_values Table?

✅ All 41 enum types are seeded in migration
✅ All intValues correctly mapped
✅ All codes match backend C# enum names
✅ All display names are user-friendly

### What's Missing?

❌ Frontend TypeScript definitions for 27 backend-only enums
❌ API hooks for Currency dropdown
❌ Display label utilities for 8 enums (RegistrationStatus, PaymentStatus, etc.)

### Data Integrity Check

**Query to verify all 41 types exist**:
```sql
SELECT enum_type, COUNT(*) as value_count
FROM reference_data.reference_values
GROUP BY enum_type
ORDER BY enum_type;
```

**Expected Results**:
- EventCategory: 8
- EventStatus: 8
- UserRole: 6
- Currency: 6
- (... 37 more types)

**Total**: 41 enum types

---

## Section 7: Verification Plan

### Automated Verification

**Step 1: Database Verification**
```bash
# Verify all 41 enum types exist in database
psql -h localhost -U postgres -d lankaconnect_dev \
  -c "SELECT enum_type, COUNT(*) FROM reference_data.reference_values GROUP BY enum_type ORDER BY enum_type;"
```

**Expected**: 41 rows

---

**Step 2: Backend Enum Files Verification**
```bash
# Count backend enum files
find src/LankaConnect.Domain -name "*.cs" -path "*/Enums/*" | wc -l
```

**Expected**: 55 files (some domains have multiple enums)

---

**Step 3: Frontend Enum Files Verification**
```bash
# Find frontend enum definitions
grep -r "export enum" web/src/infrastructure/api/types/ | wc -l
```

**Expected**: ~15 enums (only user-facing enums)

---

**Step 4: Hardcoded Dropdown Detection**
```bash
# Find remaining hardcoded option arrays
grep -r "const.*Options.*=.*\[" web/src --include="*.tsx" --include="*.ts" | \
  grep -v "node_modules" | grep -v ".next"
```

**Expected**:
- DateRangeFilter.tsx (non-enum options - OK)
- EventEditForm.tsx (Currency dropdown - NEEDS FIX)

---

### Manual Verification Checklist

#### Currency Dropdown Verification
- [ ] Navigate to `/events/{id}/edit` page
- [ ] Check "Ticket Price Currency" dropdown
- [ ] Verify all 6 currencies appear (USD, LKR, GBP, EUR, CAD, AUD)
- [ ] Verify labels match database names
- [ ] Test form submission with each currency

#### EventCategory Dropdown Verification (Already Fixed)
- [ ] Navigate to `/events/{id}/edit` page
- [ ] Check "Event Category" dropdown
- [ ] Verify all 8 categories appear
- [ ] Verify labels match database names

#### Display Label Verification
- [ ] Navigate to events list page
- [ ] Verify EventStatus labels display correctly (Draft, Published, Active, etc.)
- [ ] Check payment status labels in registration details
- [ ] Verify subscription status labels in user profile

---

## Section 8: Lessons Learned

### What Went Wrong in Phase 6A.47

1. **Incomplete Scope Analysis**: Only searched for EventCategory/EventStatus dropdowns without auditing ALL 41 enum types
2. **No Systematic Inventory**: Failed to create a comprehensive enum type inventory before starting
3. **No Usage Pattern Analysis**: Didn't categorize enums by usage pattern (dropdown vs display vs business logic)
4. **No Priority Matrix**: Treated all enums equally instead of prioritizing by user impact

### Prevention Measures for Future

1. ✅ **Create Comprehensive Inventory FIRST**: Count and list ALL items before starting migration
2. ✅ **Categorize by Usage Pattern**: Separate UI dropdowns from display labels from business logic
3. ✅ **Build Priority Matrix**: Focus on high-impact, user-facing items first
4. ✅ **Verify Completeness**: Always check "Are there more?" before declaring done

### Documentation Requirements

1. ✅ This RCA document captures the COMPLETE scope
2. ✅ Phase summaries MUST reference this master inventory
3. ✅ Future enum additions MUST update the inventory table
4. ✅ Deferred items MUST have clear trigger conditions

---

## Section 9: Next Steps Summary

### Immediate Action (Phase 6A.48)
🔴 **FIX CURRENCY DROPDOWN** - 1 hour
- Create `useCurrencies()` hook
- Replace 4 hardcoded dropdowns in EventEditForm.tsx
- Update tests
- Verify all 6 currencies appear

### Short-term Action (Phase 6A.49)
⚠️ **DISPLAY LABEL UTILITIES** - 2-3 hours
- Create `useEnumDisplayName()` generic utility
- Replace hardcoded label mappings for 8 enums
- Update components to use API data

### Long-term Strategy (Phase 6A.50+)
📋 **DEFERRED ENUMS** - As needed
- Migrate backend-only enums when frontend features are built
- Use this RCA as the master checklist
- Update inventory table as features are implemented

---

## Section 10: Final Checklist

### Before Calling ANY Enum Migration "Complete"

- [ ] All 41 enum types inventoried and categorized
- [ ] High-priority UI dropdowns migrated to API
- [ ] Display label utilities created and tested
- [ ] Business logic enums verified (keep as TypeScript enums)
- [ ] Backend-only enums documented for future migration
- [ ] Database verification query run (41 enum types exist)
- [ ] Frontend grep for hardcoded dropdowns (only non-enum options remain)
- [ ] Manual UI testing for all migrated dropdowns
- [ ] Documentation updated with completion status
- [ ] Phase summary documents link to this RCA

---

## Appendix A: Complete Enum Type Details

### Tier 1: Critical Core Enums (18 types)

1. **EventCategory** (8 values): Religious, Cultural, Community, Educational, Social, Business, Charity, Entertainment
2. **EventStatus** (8 values): Draft, Published, Active, Postponed, Cancelled, Completed, Archived, UnderReview
3. **UserRole** (6 values): GeneralUser, BusinessOwner, EventOrganizer, EventOrganizerAndBusinessOwner, Admin, AdminManager
4. **RegistrationStatus** (4 values): Registered, CheckedIn, Cancelled, WaitListed
5. **PaymentStatus** (4 values): Pending, Completed, Failed, Refunded
6. **PricingType** (3 values): Free, Paid, Donation
7. **SubscriptionStatus** (5 values): FreeTrial, ActivePaid, Cancelled, Expired, PendingPayment
8. **EmailStatus** (11 values): Pending, Queued, Sending, Sent, Delivered, Failed, Bounced, Rejected, QueuedWithCulturalDelay, PermanentlyFailed, CulturalEventNotification
9. **EmailType** (9 values): Welcome, EmailVerification, PasswordReset, BusinessNotification, EventNotification, Newsletter, Marketing, Transactional, CulturalEventNotification
10. **EmailDeliveryStatus** (8 values): Pending, Queued, Sending, Sent, Delivered, Failed, Bounced, Rejected
11. **EmailPriority** (4 values): Low, Normal, High, Critical
12. **Currency** (6 values): USD, LKR, GBP, EUR, CAD, AUD
13. **NotificationType** (8 values): RoleUpgradeApproved, RoleUpgradeRejected, FreeTrialExpiring, FreeTrialExpired, SubscriptionPaymentSucceeded, SubscriptionPaymentFailed, System, Event
14. **IdentityProvider** (2 values): Local, EntraExternal
15. **SignUpItemCategory** (4 values): Mandatory, Preferred (deprecated), Suggested, Open
16. **SignUpType** (2 values): Open, Predefined
17. **AgeCategory** (2 values): Adult, Child
18. **Gender** (3 values): Male, Female, Other

### Tier 2: Cultural & Event Features (7 types)

19. **EventType** (10 values): Community, Religious, Cultural, Educational, Social, Business, Workshop, Festival, Ceremony, Celebration
20. **SriLankanLanguage** (3 values): Sinhala, Tamil, English
21. **CulturalBackground** (8 values): SinhalaBuddhist, TamilHindu, TamilSriLankan, SriLankanMuslim, SriLankanChristian, Burgher, Malay, Other
22. **ReligiousContext** (10 values): None, BuddhistPoyaday, Ramadan, HinduFestival, ChristianSabbath, VesakDay, Deepavali, Eid, Christmas, GeneralReligiousObservance
23. **GeographicRegion** (35 values): SriLanka, provinces, countries, cities - see migration for full list
24. **BuddhistFestival** (11 values): Vesak, Poson, Esala, Vap, Ill, Unduvap, Duruthu, Navam, Medin, Bak, GeneralPoyaday
25. **HinduFestival** (10 values): Deepavali, ThaiPusam, MahaShivaratri, Holi, NavRatri, Dussehra, KarthikaiDeepam, PangalThiruvizha, VelFestival, Other

### Tier 3: Extended Features (9 types)

26. **CalendarSystem** (4 values): Gregorian, Buddhist, Hindu, Islamic
27. **FederatedProvider** (3 values): MicrosoftEntra, Google, Facebook
28. **ProficiencyLevel** (5 values): Native, Fluent, Intermediate, Basic, None
29. **BusinessCategory** (9 values): Restaurant, Retail, Services, Cultural, Religious, Education, Healthcare, Entertainment, Other
30. **BusinessStatus** (4 values): Active, Pending, Suspended, Closed
31. **ReviewStatus** (4 values): Pending, Approved, Rejected, Flagged
32. **ServiceType** (4 values): DineIn, Takeaway, Delivery, Catering
33. **CulturalCommunity** (5 values): SinhalaBuddhist, TamilHindu, Muslim, Christian, Other
34. **CulturalConflictLevel** (5 values): None, Low, Medium, High, Critical
35. **PoyadayType** (3 values): FullMoon, NewMoon, QuarterMoon

### Tier 4: Specialized Systems (6 types)

36. **ForumCategory** (5 values): General, Cultural, Events, Business, Support
37. **TopicStatus** (4 values): Open, Closed, Pinned, Archived
38. **WhatsAppMessageStatus** (5 values): Pending, Sent, Delivered, Read, Failed
39. **WhatsAppMessageType** (4 values): Text, Image, Document, Template
40. **PassPurchaseStatus** (5 values): Pending, Completed, Failed, Refunded, Expired
41. **BadgePosition** (4 values): TopLeft, TopRight, BottomLeft, BottomRight (DEPRECATED)

---

**END OF RCA DOCUMENT**

**STATUS**: ✅ Complete 41-enum inventory and analysis
**NEXT ACTION**: Implement Phase 6A.48 (Currency dropdown fix)
**OWNER**: System Architect / Development Team
**REVIEW DATE**: 2025-12-28
