# Phase 6A.55: JSONB Nullable Enum Fix - MASTER PLAN (ON HOLD)

**Date Created**: 2025-12-26
**Status**: 📋 **ON HOLD** - Pending Phase 6A.47 enum migration completion
**Priority**: P1 - HIGH (User-facing HTTP 500 errors)
**Consolidated By**: Current Session + System-Architect Agent (a44cfb1)
**Estimated Time**: 9 hours (will be revised post-migration)

---

## ⚠️ IMPORTANT: WHY THIS PLAN IS ON HOLD

### Dependency Discovery
During planning, we discovered that **Phase 6A.47 (Reference Data Migration)** is migrating **ALL 35 backend enums to database**, including:
- ✅ EventCategory enum → EventCategoriesRef table
- ✅ EventStatus enum → EventStatusesRef table
- ✅ **AgeCategory enum → AgeCategoriesRef table** ⚠️ **CRITICAL DEPENDENCY**
- ✅ Gender enum → GendersRef table
- ✅ 31 other enums

### Impact on Phase 6A.55
This plan was designed assuming `AgeCategory` remains an enum. Once Phase 6A.47 completes:
- **Domain Model Changes**: `AttendeeDetails.AgeCategory` → `AttendeeDetails.AgeCategoryId` (Guid FK)
- **JSONB Structure Changes**: `{"ageCategory": 1}` → `{"ageCategoryId": "guid"}`
- **Query Handler Changes**: Direct enum access → JOIN to reference table
- **DTO Changes**: `AgeCategory? AgeCategory` → Needs revision based on new structure
- **Database Constraints**: CHECK constraints for enum values → FK constraints

### Decision
**Wait for Phase 6A.47 to complete**, then **revise this entire plan** to work with the new reference data structure. This avoids:
- ❌ 9 hours of wasted work that would be invalidated
- ❌ Need to revert all Phase 6A.55 changes
- ❌ Confusion from incompatible database structures

---

## 📋 Current Plan Documents (Pre-Enum Migration Version)

All documents below were created assuming `AgeCategory` remains an enum. **These will be revised after Phase 6A.47 completes.**

### 1. Executive Summary
**File**: `PHASE_6A55_EXECUTIVE_SUMMARY_PRE_ENUM_MIGRATION.md`
**Content**: Overview of the 5-phase comprehensive fix plan
**Status**: ⚠️ DRAFT - Will be revised post-migration

### 2. Comprehensive Fix Plan (System-Architect)
**File**: `PHASE_6A55_JSONB_INTEGRITY_PERMANENT_FIX_PLAN_PRE_ENUM_MIGRATION.md`
**Content**: Detailed 5-phase implementation with:
- Phase 1: Detection & Analysis (1 hour)
- Phase 2: Defensive Code Changes - TDD (4 hours)
- Phase 3: Database Cleanup (2 hours)
- Phase 4: Database Constraints (1 hour)
- Phase 5: Monitoring & Documentation (1 hour)
**Status**: ⚠️ DRAFT - Will be revised post-migration

### 3. Alternative Comprehensive Fix Plan
**File**: `PHASE_6A55_COMPREHENSIVE_FIX_PLAN_PRE_ENUM_MIGRATION.md`
**Content**: Query handler-focused fix plan
**Status**: ⚠️ DRAFT - Will be revised post-migration

### 4. Root Cause Analysis
**File**: `PHASE_6A55_ATTENDEES_TAB_HTTP_500_RCA.md`
**Content**: RCA for HTTP 500 errors on `/attendees` endpoint
**Status**: ✅ VALID - RCA remains accurate regardless of enum vs FK

### 5. Dependency Analysis
**File**: `PHASE_6A55_DEPENDENCY_ANALYSIS.md`
**Content**: Analysis of dependency on Phase 6A.47
**Status**: ✅ COMPLETE - Led to ON HOLD decision

### 6. Phase Consolidation Analysis
**File**: `PHASE_CONSOLIDATION_ANALYSIS.md`
**Content**: Analysis of 3 agents' work, phase number conflicts
**Status**: ✅ COMPLETE - Resolved conflicts, chose Phase 6A.55

---

## 🎯 What We Know (Won't Change After Migration)

### Root Cause (CONFIRMED)
**Database contains JSONB records with null `AgeCategory` values**, causing EF Core materialization failures.

**How It Happened**:
1. Phase 6A.43 (Dec 2025): Refactored from `Age` (int) → `AgeCategory` (enum)
2. Migration transformed existing records, but missed some during transition period
3. Result: JSONB has `{"ageCategory": null}` but domain expects non-nullable value

### Affected Endpoints (CONFIRMED)
1. `GET /api/events/{id}/attendees` - 🔴 CRITICAL
2. `GET /api/events/registrations/{id}` - 🔴 CRITICAL
3. `GET /api/events/{id}/my-registration/ticket` - 🟡 HIGH
4. `GET /api/events/{id}/export` - 🟡 HIGH (delegates to #1)

### User Impact (CONFIRMED)
- ❌ HTTP 500 errors when viewing attendee lists
- ❌ Registration state "flipping" on page refresh
- ❌ CSV/Excel export failures
- ❌ Ticket viewing failures

---

## 🔄 What Will Change After Phase 6A.47

### Domain Model
**Before (Enum)**:
```csharp
public class AttendeeDetails : ValueObject
{
    public string Name { get; }
    public AgeCategory AgeCategory { get; }  // Enum: Adult = 1, Child = 2
    public Gender? Gender { get; }
}
```

**After (Reference Data - Expected)**:
```csharp
public class AttendeeDetails : ValueObject
{
    public string Name { get; }
    public Guid AgeCategoryId { get; }  // FK to AgeCategoriesRef table
    public Guid? GenderId { get; }      // FK to GendersRef table
}
```

### JSONB Structure
**Before**:
```json
{
  "attendees": [
    { "name": "John Doe", "ageCategory": 1, "gender": 1 }
  ]
}
```

**After (Expected)**:
```json
{
  "attendees": [
    {
      "name": "John Doe",
      "ageCategoryId": "550e8400-e29b-41d4-a716-446655440000",
      "genderId": "660e8400-e29b-41d4-a716-446655440000"
    }
  ]
}
```

### Query Handlers
**Before (Direct Access)**:
```csharp
var adultCount = registration.Attendees.Count(a => a.AgeCategory == AgeCategory.Adult);
```

**After (JOIN Required - Expected)**:
```csharp
var adultCount = registration.Attendees
    .Count(a => a.AgeCategory != null && a.AgeCategory.Code == "Adult");
```

### DTOs
**Before (Nullable Enum)**:
```csharp
public record AttendeeDetailsDto
{
    public string Name { get; init; } = string.Empty;
    public AgeCategory? AgeCategory { get; init; }
    public Gender? Gender { get; init; }
}
```

**After (String or Object - TBD)**:
```csharp
// Option 1: String (simple)
public record AttendeeDetailsDto
{
    public string Name { get; init; } = string.Empty;
    public string? AgeCategory { get; init; }  // "Adult" or "Child"
    public string? Gender { get; init; }
}

// Option 2: Reference object (rich)
public record AttendeeDetailsDto
{
    public string Name { get; init; } = string.Empty;
    public AgeCategoryRefDto? AgeCategory { get; init; }
    public GenderRefDto? Gender { get; init; }
}
```

---

## 📋 Revision Checklist (For Post-Migration)

When Phase 6A.47 completes, I will:

### Step 1: Analyze Phase 6A.47 Changes (1 hour)
- [ ] Read Phase 6A.47 implementation details
- [ ] Understand new `AgeCategoriesRef` table structure
- [ ] Understand how `AttendeeDetails` domain model changed
- [ ] Understand new JSONB format
- [ ] Identify data migration strategy used

### Step 2: Revise Domain & Infrastructure Layer (2 hours)
- [ ] Update `AttendeeDetails` value object (if not done in 6A.47)
- [ ] Update EF Core configuration for new FK relationship
- [ ] Create/verify migration for JSONB data transformation
- [ ] Update repository methods if needed

### Step 3: Revise DTOs (1 hour)
- [ ] Decide DTO structure (string vs object)
- [ ] Update `AttendeeDetailsDto`
- [ ] Update `TicketAttendeeDto`
- [ ] Update `EventAttendeeDto`
- [ ] Update AutoMapper profiles

### Step 4: Revise Query Handlers (3 hours)
- [ ] Update `GetEventAttendeesQueryHandler` with JOIN
- [ ] Update `GetRegistrationByIdQueryHandler` with JOIN
- [ ] Update `GetTicketQueryHandler` with JOIN
- [ ] Update `ExportEventAttendeesQueryHandler` (if needed)
- [ ] Add null checks for missing reference data

### Step 5: Revise Database Scripts (2 hours)
- [ ] ~~CHECK constraints for enum values~~ DELETE (no longer relevant)
- [ ] FK constraints to `AgeCategoriesRef` table
- [ ] Data cleanup script for null/orphaned FKs
- [ ] Backup strategy for corrupted data

### Step 6: Revise Tests (2 hours)
- [ ] Update integration tests with reference data seeding
- [ ] Test with null `AgeCategoryId` scenarios
- [ ] Test with orphaned FK scenarios (deleted reference data)
- [ ] Update unit tests

### Step 7: Revise Monitoring (1 hour)
- [ ] ~~Enum null detection~~ DELETE
- [ ] FK integrity monitoring
- [ ] Orphaned FK detection
- [ ] Reference data cache metrics

### Step 8: Update Documentation (1 hour)
- [ ] Revise all 6A.55 documents with actual changes
- [ ] Update PROGRESS_TRACKER.md
- [ ] Update STREAMLINED_ACTION_PLAN.md
- [ ] Create Phase 6A.55 summary after completion

**Revised Estimated Time**: 13 hours (increased due to reference data complexity)

---

## 🎯 Current State of Work

### ✅ Completed (Won't Be Reverted)
1. **TicketDto.cs**: Made `AgeCategory` nullable in `TicketAttendeeDto`
   - File: `src/LankaConnect.Application/Events/Common/TicketDto.cs`
   - Change: `public AgeCategory? AgeCategory { get; init; }`
   - Status: ✅ LOCAL CHANGES (not committed)
   - Post-Migration: Will need to change to FK or string

2. **AttendeeDetailsDto.cs**: Made `AgeCategory` nullable (Phase 6A.48)
   - File: `src/LankaConnect.Application/Events/Common/RegistrationDetailsDto.cs`
   - Change: `public AgeCategory? AgeCategory { get; init; }`
   - Status: ✅ COMMITTED (0daa9168) but NOT DEPLOYED
   - Post-Migration: Will need to change to FK or string

### ⏸️ ON HOLD (Not Started)
1. Query handler fixes (4 handlers)
2. Database cleanup scripts
3. Database constraints
4. TDD integration tests
5. Monitoring setup
6. ADR-006 documentation

---

## 📊 Timeline & Coordination

### Current Status: WAITING
**Waiting For**: Phase 6A.47 (Reference Data Migration) to complete

**Phase 6A.47 Scope** (from PHASE_6A47_IMPLEMENTATION_TRACKER.md):
- Phase 5.1: Infrastructure Setup (Day 1)
- Phase 5.2: EventCategory Migration (Day 1-2)
- Phase 5.3: User Preferences Migration (Day 2)
- Phase 5.4: Frontend Updates (Day 3)
- Phase 5.5: Testing (Day 3-4)
- **Includes**: AgeCategory, Gender, and 33 other enums

**Estimated Wait Time**: 3-5 days (based on Phase 6A.47 plan)

### When Phase 6A.47 Completes
**User will notify me** → I will:
1. Read Phase 6A.47 implementation details
2. Revise ALL Phase 6A.55 documents
3. Create revised implementation plan
4. Get user approval
5. Execute revised Phase 6A.55

---

## 🔗 Related Documents

### Planning Documents (Current Session)
- `PHASE_6A55_MASTER_PLAN_ON_HOLD.md` - This document
- `PHASE_CONSOLIDATION_ANALYSIS.md` - Agent coordination analysis
- `PHASE_6A55_DEPENDENCY_ANALYSIS.md` - Dependency on Phase 6A.47

### Implementation Plans (Pre-Migration - Will Be Revised)
- `PHASE_6A55_EXECUTIVE_SUMMARY_PRE_ENUM_MIGRATION.md`
- `PHASE_6A55_JSONB_INTEGRITY_PERMANENT_FIX_PLAN_PRE_ENUM_MIGRATION.md`
- `PHASE_6A55_COMPREHENSIVE_FIX_PLAN_PRE_ENUM_MIGRATION.md`

### Root Cause Analysis (Remains Valid)
- `PHASE_6A55_ATTENDEES_TAB_HTTP_500_RCA.md`
- `REGISTRATION_STATE_FLIPPING_RCA.md`
- `REGISTRATION_STATE_FLIPPING_FIX_PLAN.md`
- `JSONB_DATA_INTEGRITY_PREVENTION_STRATEGY.md`

### Reference Data Migration (Dependency)
- `PHASE_6A47_IMPLEMENTATION_TRACKER.md`

---

## 📝 Notes for Future Me (Post-Migration)

### Questions to Ask When Resuming
1. How did Phase 6A.47 handle JSONB data migration?
   - Did they migrate existing `{"ageCategory": 1}` to `{"ageCategoryId": "guid"}`?
   - Or did they add backward compatibility?

2. What is the new DTO structure?
   - Are reference data returned as strings ("Adult") or objects?
   - Is there a standard pattern across all reference data?

3. How are JOINs handled?
   - Do query handlers use `.Include(r => r.AgeCategory)`?
   - Or manual projection with reference data service?

4. What is the caching strategy?
   - IMemoryCache for reference data?
   - What is the TTL?

5. What happened to enum values in existing code?
   - Are enums deleted entirely?
   - Or kept for backward compatibility?

### Key Files to Review Post-Migration
1. `src/LankaConnect.Domain/ReferenceData/Entities/AgeCategoryRef.cs`
2. `src/LankaConnect.Domain/Events/ValueObjects/AttendeeDetails.cs`
3. `src/LankaConnect.Application/ReferenceData/Services/ReferenceDataService.cs`
4. EF Core migrations for reference data tables
5. Data migration scripts for JSONB transformation

---

## ✅ Summary

### What We Did
1. ✅ Consulted system-architect for comprehensive RCA
2. ✅ Created 5-phase permanent fix plan
3. ✅ Consolidated work from 3 agents (resolved phase conflicts)
4. ✅ Discovered critical dependency on Phase 6A.47
5. ✅ Made smart decision to WAIT rather than waste effort
6. ✅ Documented comprehensive plan for future revision

### What We're Waiting For
- Phase 6A.47: Reference Data Migration (AgeCategory enum → database table)

### What Happens Next
1. User notifies me when Phase 6A.47 is complete
2. I review Phase 6A.47 changes
3. I revise all Phase 6A.55 plans to work with new structure
4. I present revised plan for approval
5. I execute revised Phase 6A.55

### Why This is the Right Approach
- ✅ Avoids 9+ hours of wasted work
- ✅ Ensures permanent fix works with new architecture
- ✅ Follows "no shortcuts" principle (wait for proper foundation)
- ✅ Coordinates properly with other agents
- ✅ Comprehensive documentation for future revision

---

**Status**: 📋 ON HOLD - Comprehensive plan documented, waiting for Phase 6A.47 completion

**Next Action**: User to notify when Phase 6A.47 (enum migration) is complete
