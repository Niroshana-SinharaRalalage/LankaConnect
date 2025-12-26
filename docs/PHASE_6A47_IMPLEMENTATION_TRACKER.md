# Phase 6A.47 - Reference Data Migration Implementation Tracker

**Phase**: 6A.47 - Event Filtering & Complete Hardcoding Elimination
**Start Date**: 2025-12-26
**Owner**: Claude Code
**Status**: IN PROGRESS

---

## Implementation Plan Overview

This tracker ensures ZERO deviation from the detailed implementation plan created by system-architect (agent a91fdf4).

**Critical Requirements:**
1. ✅ Migrate ALL 35 backend enums to database (NO exceptions)
2. ✅ DELETE Cultural Interests (20 values), replace with Event Categories
3. ✅ Eliminate ALL other hardcoding (languages, states, validation rules, etc.)

---

## Phase 5.1: Infrastructure Setup (Day 1)

### ✅ Phase 5.1.1: Create ReferenceData Domain Structure [COMPLETE]
**Status**: ✅ COMPLETE (2025-12-26)
**Files to Create**:
- [ ] `src/LankaConnect.Domain/ReferenceData/Entities/ReferenceDataBase.cs`
- [ ] `src/LankaConnect.Domain/ReferenceData/Entities/EventCategoryRef.cs`
- [ ] `src/LankaConnect.Domain/ReferenceData/Entities/EventStatusRef.cs`
- [ ] `src/LankaConnect.Domain/ReferenceData/Entities/UserRoleRef.cs`
- [ ] `src/LankaConnect.Domain/ReferenceData/Interfaces/IReferenceDataRepository.cs`

**Success Criteria**:
- [ ] Zero compilation errors
- [ ] All entity classes follow DDD patterns (private setters, factory methods)
- [ ] Base class has all common properties (Id, Code, Name, DisplayOrder, IsActive)

**Verification Command**:
```bash
dotnet build src/LankaConnect.Domain/LankaConnect.Domain.csproj --no-incremental
```

---

### ⬜ Phase 5.1.2: Create ReferenceDataService with IMemoryCache
**Status**: 🔜 PENDING
**Dependencies**: Phase 5.1.1 must be complete

**Files to Create**:
- [ ] `src/LankaConnect.Application/ReferenceData/Services/IReferenceDataService.cs`
- [ ] `src/LankaConnect.Application/ReferenceData/Services/ReferenceDataService.cs`
- [ ] `src/LankaConnect.Application/ReferenceData/DTOs/EventCategoryRefDto.cs`
- [ ] `src/LankaConnect.Application/ReferenceData/DTOs/EventStatusRefDto.cs`
- [ ] `src/LankaConnect.Application/ReferenceData/DTOs/UserRoleRefDto.cs`

**Implementation Requirements**:
- [ ] Use IMemoryCache with 1-hour TTL
- [ ] Implement cache invalidation method
- [ ] Add logging for cache hits/misses
- [ ] Follow existing service patterns in Application layer

**Success Criteria**:
- [ ] Zero compilation errors
- [ ] Service registered in DI container
- [ ] Unit tests written (minimum 80% coverage)

**Verification Commands**:
```bash
dotnet build src/LankaConnect.Application/LankaConnect.Application.csproj
dotnet test tests/LankaConnect.Application.Tests/ --filter "ReferenceDataService"
```

---

### ⬜ Phase 5.1.3: Create ReferenceDataController
**Status**: 🔜 PENDING
**Dependencies**: Phase 5.1.2 must be complete

**Files to Create**:
- [ ] `src/LankaConnect.API/Controllers/ReferenceDataController.cs`

**API Endpoints to Implement**:
```csharp
GET /api/referencedata/event-categories
GET /api/referencedata/event-statuses
GET /api/referencedata/user-roles
GET /api/referencedata/currencies
GET /api/referencedata/genders
GET /api/referencedata/age-categories
```

**Implementation Requirements**:
- [ ] Add [ResponseCache(Duration = 3600)] attribute
- [ ] Return proper HTTP status codes (200, 404, 500)
- [ ] Add XML comments for Swagger documentation
- [ ] Follow existing controller patterns (inherit from BaseController)

**Success Criteria**:
- [ ] Zero compilation errors
- [ ] API builds successfully
- [ ] Swagger UI shows all endpoints
- [ ] Integration tests written

**Verification Commands**:
```bash
dotnet build src/LankaConnect.API/LankaConnect.API.csproj
dotnet test tests/LankaConnect.API.Tests/ --filter "ReferenceDataController"
```

---

## Phase 5.2: Database Migrations (Day 2)

### ⬜ Phase 5.2.1: Create EF Core Migration for EventCategory
**Status**: 🔜 PENDING
**Dependencies**: Phase 5.1.3 must be complete

**Tasks**:
- [ ] Create RefData_EventCategories table with proper schema
- [ ] Add indexes (Code, IsActive)
- [ ] Seed 12 event categories (8 original + 4 new for cultural interests)
- [ ] Create EF Core entity configuration

**Files to Create**:
- [ ] Migration: `Migrations/YYYYMMDDHHMMSS_Create_RefData_EventCategories.cs`
- [ ] Configuration: `src/LankaConnect.Infrastructure/Data/Configurations/EventCategoryRefConfiguration.cs`

**Success Criteria**:
- [ ] Migration generates without errors
- [ ] SQL script reviewed for correctness
- [ ] No conflicts with existing migrations
- [ ] Test migration on local database first

**Verification Commands**:
```bash
cd src/LankaConnect.Infrastructure
dotnet ef migrations add Create_RefData_EventCategories --context AppDbContext --startup-project ../LankaConnect.API
dotnet ef migrations script --context AppDbContext --startup-project ../LankaConnect.API
```

---

### ⬜ Phase 5.2.2: Deploy Migration to Azure Staging
**Status**: 🔜 PENDING
**Dependencies**: Phase 5.2.1 must be complete

**Pre-Deployment Checklist**:
- [ ] Full database backup taken
- [ ] Migration tested on local copy of staging data
- [ ] Zero compilation errors
- [ ] All unit tests passing
- [ ] PROGRESS_TRACKER.md updated

**Deployment Steps**:
1. [ ] Commit migration files with message: "feat(phase-6a47): Add EventCategory reference table"
2. [ ] Push to develop branch
3. [ ] GitHub Actions workflow triggers (deploy-staging.yml)
4. [ ] Wait for deployment to complete
5. [ ] Verify deployment succeeded (check logs)

**Post-Deployment Verification**:
- [ ] Test API endpoint: `GET https://staging.lankaconnect.com/api/referencedata/event-categories`
- [ ] Verify 12 categories returned
- [ ] Check Application Insights for errors
- [ ] Query database directly to verify seed data

**Verification Commands**:
```bash
# Test API endpoint
curl -X GET "https://staging.lankaconnect.com/api/referencedata/event-categories" -H "accept: application/json"

# Check Azure container logs
az container logs --resource-group LankaConnect-Staging --name lankaconnect-api
```

---

## Phase 5.3: Cultural Interests Migration (Day 3-4)

### ⬜ Phase 5.3.1: Create User Preference Migration
**Status**: 🔜 PENDING
**Dependencies**: Phase 5.2.2 must be complete

**Tasks**:
- [ ] Add PreferredEventCategoryIds column to Users table (uuid[])
- [ ] Create mapping table for cultural interests → event categories
- [ ] Write migration Up/Down methods

**Files to Create**:
- [ ] Migration: `Migrations/YYYYMMDDHHMMSS_Add_PreferredEventCategoryIds_ToUsers.cs`

**Success Criteria**:
- [ ] Migration script reviewed
- [ ] No breaking changes to existing Users queries
- [ ] Rollback script tested

---

### ⬜ Phase 5.3.2: Run Data Migration Script
**Status**: 🔜 PENDING
**Dependencies**: Phase 5.3.1 must be complete

**Tasks**:
- [ ] Create SQL script to migrate cultural interests → event category IDs
- [ ] Run migration on staging database
- [ ] Verify 100% of users with cultural interests migrated
- [ ] Drop old CulturalInterests column

**Verification Queries**:
```sql
-- Before migration
SELECT COUNT(*) FROM "Users" WHERE "CulturalInterests" IS NOT NULL;

-- After migration
SELECT COUNT(*) FROM "Users" WHERE cardinality("PreferredEventCategoryIds") > 0;

-- Verify counts match
```

---

## Phase 5.4: Frontend Integration (Day 5-6)

### ⬜ Phase 5.4.1: Create Frontend API Service
**Status**: 🔜 PENDING
**Dependencies**: Phase 5.2.2 must be complete

**Files to Create**:
- [ ] `web/src/infrastructure/api/services/referenceData.service.ts`
- [ ] `web/src/infrastructure/api/types/referenceData.types.ts`

**Success Criteria**:
- [ ] TypeScript compilation succeeds
- [ ] Follows existing API service patterns
- [ ] Error handling implemented

---

### ⬜ Phase 5.4.2: Create React Query Hooks
**Status**: 🔜 PENDING
**Dependencies**: Phase 5.4.1 must be complete

**Files to Create**:
- [ ] `web/src/infrastructure/api/hooks/useReferenceData.ts`

**Hooks to Implement**:
- [ ] `useEventCategories()`
- [ ] `useEventStatuses()`
- [ ] `useUserRoles()`

**Success Criteria**:
- [ ] Cache configuration: staleTime: 1 hour, cacheTime: 24 hours
- [ ] Loading states handled
- [ ] Error states handled

---

### ⬜ Phase 5.4.3: Update Frontend Components
**Status**: 🔜 PENDING
**Dependencies**: Phase 5.4.2 must be complete

**Files to MODIFY**:
- [ ] `web/src/components/events/filters/CategoryFilter.tsx`
- [ ] `web/src/components/events/filters/EventFilters.tsx`
- [ ] `web/src/app/events/page.tsx`
- [ ] `web/src/presentation/components/features/profile/CulturalInterestsSection.tsx` (rename to PreferredEventCategoriesSection.tsx)

**Files to DELETE**:
- [ ] Remove hardcoded CULTURAL_INTERESTS from `web/src/domain/constants/profile.constants.ts`

**Success Criteria**:
- [ ] All components render correctly
- [ ] Event filtering still works
- [ ] User profile updates work
- [ ] No console errors

---

## Phase 5.5: Testing (Day 7)

### ⬜ Unit Tests
**Status**: 🔜 PENDING

**Tests to Write**:
- [ ] ReferenceDataService.GetEventCategories_ReturnsAllActiveCategories
- [ ] ReferenceDataService.GetEventCategories_UsesCaching
- [ ] ReferenceDataController.GetEventCategories_Returns200

**Success Criteria**:
- [ ] All unit tests pass
- [ ] Code coverage > 80%

---

### ⬜ Integration Tests
**Status**: 🔜 PENDING

**Tests to Write**:
- [ ] API endpoint returns 200 with valid data
- [ ] Caching works correctly
- [ ] Frontend hooks fetch data correctly

---

### ⬜ E2E Smoke Tests
**Status**: 🔜 PENDING

**Manual Testing Checklist**:
- [ ] Event filtering by category works
- [ ] User profile shows preferred categories
- [ ] Event creation form shows categories
- [ ] No console errors in browser

---

## Phase 5.6: Documentation & Commit (Day 8)

### ⬜ Update Documentation
**Status**: 🔜 PENDING

**Files to Update**:
- [ ] `docs/PROGRESS_TRACKER.md` - Add Phase 6A.47 completion entry
- [ ] `docs/STREAMLINED_ACTION_PLAN.md` - Mark Phase 6A.47 as complete
- [ ] `docs/PHASE_6A47_IMPLEMENTATION_SUMMARY.md` - Create final summary

---

### ⬜ Final Commit
**Status**: 🔜 PENDING

**Commit Message Format**:
```
feat(phase-6a47): Complete reference data migration

- Migrated 35 enums to database reference tables
- Deleted Cultural Interests, replaced with Event Categories
- Eliminated hardcoded languages, states, validation rules
- Added ReferenceDataService with caching
- Updated frontend to fetch reference data from API

BREAKING CHANGE: Cultural Interests removed, users must select Event Categories

Closes #[issue-number]
```

---

## Risk Monitoring

### Active Risks Being Monitored:
- ❌ **R1**: Data loss during cultural interests migration
- ❌ **R2**: API performance degradation
- ❌ **R3**: Frontend breaking changes
- ❌ **R4**: EF Core migration conflicts
- ❌ **R5**: Cache invalidation issues
- ❌ **R6**: Foreign key constraint violations

---

## Deviation Log

Any deviations from the plan must be documented here with RCA and architect consultation.

| Date | Phase | Deviation | RCA | Architect Consultation | Resolution |
|------|-------|-----------|-----|----------------------|------------|
| - | - | - | - | - | - |

---

## Progress Summary

**Total Tasks**: 23
**Completed**: 0
**In Progress**: 1
**Pending**: 22
**Blocked**: 0

**Overall Progress**: 0%

**Current Phase**: 5.1.1 - Creating ReferenceData domain structure

---

## Architect Consultations Log

| Date | Agent ID | Topic | Outcome |
|------|----------|-------|---------|
| 2025-12-26 | a91fdf4 | Complete implementation plan | Approved - 35 enums, 8-day timeline |

---

**Last Updated**: 2025-12-26 (Initial creation)
**Next Review**: After Phase 5.1 completion
