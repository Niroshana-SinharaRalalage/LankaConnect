# Enum Migration Status Summary

**Last Updated**: 2025-12-28
**Phase**: 6A.47-6A.50
**Master RCA**: [COMPLETE_41_ENUM_TYPES_RCA_AND_FIX_PLAN.md](./COMPLETE_41_ENUM_TYPES_RCA_AND_FIX_PLAN.md)

---

## Quick Stats

| Metric | Count | Status |
|--------|-------|--------|
| **Total Enum Types** | **41** | ✅ All inventoried |
| Migrated to API (Phase 6A.47) | 2 | EventCategory, EventStatus |
| Needs Immediate Fix | 1 | Currency (4 dropdowns hardcoded) |
| Needs Display Labels | 8 | RegistrationStatus, PaymentStatus, etc. |
| Backend Only (Deferred) | 27 | Email, Business, Forum, WhatsApp, etc. |
| Business Logic Only (Keep) | 3 | EventStatus, Currency, PaymentStatus (comparisons) |

---

## Migration Progress

```
Total Progress: 2 / 41 enums = 4.9%

Category Breakdown:
├─ UI Dropdowns (3 total)
│  ├─ ✅ EventCategory (DONE Phase 6A.47)
│  ├─ 🔴 Currency (FIX Phase 6A.48) ← BLOCKER
│  └─ ⚪ BadgePosition (DEPRECATED)
│
├─ Display Labels (8 total)
│  ├─ ✅ EventStatus (DONE Phase 6A.47)
│  ├─ ⚠️ RegistrationStatus (Phase 6A.49)
│  ├─ ⚠️ PaymentStatus (Phase 6A.49)
│  ├─ ⚠️ PricingType (Phase 6A.49)
│  ├─ ⚠️ SubscriptionStatus (Phase 6A.49)
│  ├─ ⚠️ AgeCategory (Phase 6A.49)
│  ├─ ⚠️ Gender (Phase 6A.49)
│  └─ ⚠️ SignUpItemCategory (Phase 6A.49)
│
├─ Business Logic (Keep as TypeScript enums)
│  ├─ EventStatus (comparisons: status === EventStatus.Active)
│  ├─ Currency (assignments: currency: Currency.USD)
│  ├─ PaymentStatus (checks: status === PaymentStatus.Completed)
│  └─ ... (all enums used for type safety)
│
└─ Backend Only (27 total) - DEFERRED
   ├─ Email System: EmailStatus, EmailType, EmailDeliveryStatus, EmailPriority
   ├─ Notifications: NotificationType
   ├─ Auth: IdentityProvider, FederatedProvider
   ├─ Cultural: SriLankanLanguage, CulturalBackground, ReligiousContext,
   │            GeographicRegion, BuddhistFestival, HinduFestival,
   │            CalendarSystem, CulturalCommunity, CulturalConflictLevel
   ├─ Business: BusinessCategory, BusinessStatus, ReviewStatus, ServiceType
   ├─ Community: ForumCategory, TopicStatus
   ├─ WhatsApp: WhatsAppMessageStatus, WhatsAppMessageType
   └─ Other: EventType, ProficiencyLevel, PassPurchaseStatus, PoyadayType
```

---

## Critical Issue: Currency Dropdown

### The Problem
❌ **4 hardcoded Currency dropdowns in `EventEditForm.tsx`**

**Locations**:
1. Line 765-767: Single pricing currency
2. Line 820-822: Adult pricing currency
3. Line 870-872: Child pricing currency
4. Line 1017-1019: Group pricing currency

**Current Code** (WRONG):
```typescript
<select>
  <option value={Currency.USD}>USD ($)</option>
  <option value={Currency.LKR}>LKR (Rs)</option>
</select>
```

**Issue**: Only shows 2 of 6 currencies (missing GBP, EUR, CAD, AUD)

### The Fix (Phase 6A.48)

**Step 1**: Create `useCurrencies()` hook
```typescript
// web/src/presentation/hooks/useCurrencies.ts
import { useReferenceData } from './useReferenceData';
import { toDropdownOptions } from '@/infrastructure/api/utils/enum-mappers';

export function useCurrencies() {
  const { data, isLoading, error } = useReferenceData(['Currency']);

  return {
    currencies: data?.Currency || [],
    currencyOptions: toDropdownOptions(data?.Currency),
    isLoading,
    error,
  };
}
```

**Step 2**: Replace hardcoded dropdowns
```typescript
// In EventEditForm.tsx
const { currencyOptions, isLoading: isLoadingCurrencies } = useCurrencies();

// Replace all 4 dropdowns with:
<select>
  {isLoadingCurrencies ? (
    <option>Loading currencies...</option>
  ) : (
    currencyOptions.map(option => (
      <option key={option.value} value={option.value}>
        {option.label}
      </option>
    ))
  )}
</select>
```

**Estimated Time**: 1 hour

---

## Phase Plan

### ✅ Phase 6A.47 (COMPLETED)
- Migrated EventCategory dropdown to API
- Migrated EventStatus display labels to API
- Created reference data infrastructure
- **Status**: Complete

### 🔴 Phase 6A.48 (IMMEDIATE - 1 hour)
**Goal**: Fix Currency dropdown blocker
- [ ] Create `useCurrencies()` custom hook
- [ ] Replace 4 hardcoded Currency dropdowns
- [ ] Update tests
- [ ] Verify all 6 currencies display

**Deliverable**: Currency dropdown shows all 6 currencies dynamically

### ⚠️ Phase 6A.49 (SHORT-TERM - 2-3 hours)
**Goal**: Migrate display labels to API
- [ ] Create `useEnumDisplayName()` generic utility
- [ ] Replace RegistrationStatus label mapping
- [ ] Replace PaymentStatus label mapping
- [ ] Replace PricingType label mapping
- [ ] Replace SubscriptionStatus label mapping
- [ ] Replace AgeCategory label mapping
- [ ] Replace Gender label mapping
- [ ] Replace SignUpItemCategory label mapping
- [ ] Replace SignUpType label mapping

**Deliverable**: All display labels use API data

### 📋 Phase 6A.50+ (DEFERRED - As Features Built)
**Goal**: Migrate backend-only enums when frontend features are implemented

**Trigger Conditions**:
- Email system UI → Migrate EmailStatus, EmailType, EmailDeliveryStatus, EmailPriority
- User profile enhancements → Migrate SriLankanLanguage, CulturalBackground, ProficiencyLevel
- Cultural calendar UI → Migrate BuddhistFestival, HinduFestival, ReligiousContext, CalendarSystem
- Business directory UI → Migrate BusinessCategory, BusinessStatus, ReviewStatus, ServiceType
- Community forums UI → Migrate ForumCategory, TopicStatus
- WhatsApp integration UI → Migrate WhatsAppMessageStatus, WhatsAppMessageType
- Geographic features → Migrate GeographicRegion
- Notification preferences → Migrate NotificationType

**Deliverable**: All 41 enums fully migrated

---

## Testing Checklist

### After Phase 6A.48 (Currency Fix)
- [ ] Navigate to `/events/{id}/edit`
- [ ] Check single pricing currency dropdown
- [ ] Verify all 6 currencies appear: USD, LKR, GBP, EUR, CAD, AUD
- [ ] Check adult pricing currency dropdown (dual pricing mode)
- [ ] Check child pricing currency dropdown (dual pricing mode)
- [ ] Check group pricing currency dropdown (group pricing mode)
- [ ] Test form submission with each currency
- [ ] Verify backend receives correct currency intValue

### After Phase 6A.49 (Display Labels)
- [ ] Navigate to events list
- [ ] Verify EventStatus labels display correctly
- [ ] Navigate to event registration details
- [ ] Verify PaymentStatus labels display correctly
- [ ] Verify RegistrationStatus labels display correctly
- [ ] Navigate to user profile
- [ ] Verify SubscriptionStatus labels display correctly
- [ ] Check AgeCategory and Gender labels in attendee lists

---

## Success Metrics

| Metric | Target | Current | Status |
|--------|--------|---------|--------|
| Enum types migrated | 41 | 2 | 🔴 4.9% |
| Hardcoded dropdowns | 0 | 4 (Currency) | 🔴 Blocker |
| Hardcoded label maps | 0 | 8 | ⚠️ Medium priority |
| Backend-only enums documented | 27 | 27 | ✅ Complete |
| Build status | 0 errors | 0 errors | ✅ Green |

---

## Key Learnings

### What Went Wrong in Phase 6A.47
1. ❌ Only migrated 2 of 41 enum types
2. ❌ No comprehensive inventory before starting
3. ❌ No usage pattern analysis
4. ❌ Missed Currency dropdown (4 hardcoded instances)

### Prevention Measures
1. ✅ Created [COMPLETE_41_ENUM_TYPES_RCA_AND_FIX_PLAN.md](./COMPLETE_41_ENUM_TYPES_RCA_AND_FIX_PLAN.md)
2. ✅ Categorized all 41 enums by usage pattern
3. ✅ Built priority matrix based on user impact
4. ✅ Documented trigger conditions for deferred enums

### Process Improvements
- **ALWAYS** create comprehensive inventory FIRST
- **ALWAYS** categorize by usage pattern (dropdown / display / logic)
- **ALWAYS** verify completeness with grep searches
- **ALWAYS** check "Are there more?" before declaring done

---

## Quick Reference

### ✅ Migration Complete
- EventCategory (dropdown)
- EventStatus (display labels)

### 🔴 Immediate Action Required
- Currency (4 hardcoded dropdowns)

### ⚠️ Short-term Action
- RegistrationStatus, PaymentStatus, PricingType, SubscriptionStatus
- AgeCategory, Gender, SignUpItemCategory, SignUpType

### 📋 Deferred (Backend Only)
- 27 enum types for future features

### ✅ Keep As-Is (Business Logic)
- All TypeScript enums (provide compile-time type safety)

---

**For detailed analysis, see**: [COMPLETE_41_ENUM_TYPES_RCA_AND_FIX_PLAN.md](./COMPLETE_41_ENUM_TYPES_RCA_AND_FIX_PLAN.md)
