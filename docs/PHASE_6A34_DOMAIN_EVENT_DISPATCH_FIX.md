# Phase 6A.34: Domain Event Dispatch Fix - Root Cause Analysis

## Executive Summary

**Issue**: RegistrationConfirmedEvent domain events were NOT being dispatched after user registration, preventing confirmation emails from being sent. This occurred despite the DetectChanges() fix deployed in Phase 6A.24.

**Root Cause**: EventRepository.GetByIdAsync() was resetting entity state to `Unchanged` after syncing email group IDs (lines 79-89), which prevented EF Core's ChangeTracker from detecting subsequent business modifications.

**Impact**:
- FREE event registrations: No confirmation emails sent ❌
- PAID event registrations: No confirmation emails sent ❌
- UserLoggedInEvent: Working correctly ✅ (different code path)

**Fix**: Removed entity state reset logic in EventRepository.GetByIdAsync() to allow proper change tracking.

---

## Timeline of Investigation

### Deployment Status (Confirmed)
- **Commit**: 0ee6300 - Added DetectChanges() in AppDbContext.cs:313
- **Deployment**: Run 20323229564 completed at 01:54 UTC
- **Active Revision**: lankaconnect-api-staging--0000310 (100% traffic)
- **Contains Fix**: YES (DetectChanges confirmed in logs)

### User Test Results (After Deployment)
- **Time**: ~03:45 UTC (AFTER revision 0000310 went live)
- **Event**: FREE event c1f182a9-c957-4a78-a0b2-085917a88900
- **Result**: NO email sent ❌

### Log Evidence
**UserLoggedInEvent (WORKING):**
```
03:39:38: "Found 1 domain events to dispatch: UserLoggedInEvent" ✅
03:39:54: "Found 1 domain events to dispatch: UserLoggedInEvent" ✅
```

**RegistrationConfirmedEvent (BROKEN):**
```
NO "[Phase 6A.24] Found X domain events" logs
NO RsvpToEvent command handler logs
NO RegistrationConfirmedEventHandler logs
Only: GET /api/events/c1f182a9.../my-registration at 03:45:39
```

---

## Root Cause Analysis

### The Critical Code Path Difference

#### LoginUserHandler.cs (WORKING ✅)
```csharp
// Line 48: User retrieved via GetByEmailAsync
var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);

// Line 138: User modified (RecordSuccessfulLogin - raises UserLoggedInEvent)
user.RecordSuccessfulLogin();  // Raises domain event
await _unitOfWork.CommitAsync(cancellationToken);  // ✅ Event dispatched
```

**UserRepository.GetByIdAsync():**
- Loads user with navigation properties
- Syncs preferred metro area IDs from shadow navigation
- **DOES NOT reset entity state** ✅
- Entity remains in proper tracked state
- Subsequent modifications are auto-detected by EF Core

#### RsvpToEventCommandHandler.cs (BROKEN ❌)
```csharp
// Line 30: Event retrieved via GetByIdAsync
var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);

// Line 91-94: Event modified (RegisterWithAttendees - raises RegistrationConfirmedEvent)
var registerResult = @event.RegisterWithAttendees(...);  // Raises domain event

// Line 101: DEFENSIVE FIX added but ineffective
_eventRepository.Update(@event);  // ⚠️ Entity state was RESET

// Line 148: Commit
await _unitOfWork.CommitAsync(cancellationToken);  // ❌ Event NOT dispatched
```

**EventRepository.GetByIdAsync() (BROKEN CODE):**
```csharp
// Lines 79-89 (src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs)
// CRITICAL BUG: Store original state
var originalState = _context.Entry(eventEntity).State;  // State = Unchanged

// Sync email group IDs (modifies entity)
eventEntity.SyncEmailGroupIdsFromEntities(emailGroupIds);

// CRITICAL BUG: RESETS entity state to Unchanged!
_context.Entry(eventEntity).State = originalState;  // ❌ BREAKS CHANGE TRACKING
```

**Execution Flow (WHY IT FAILED):**

1. **EventRepository.GetByIdAsync()** is called:
   - Event loaded from database (EntityState = `Unchanged`)
   - Email group IDs synced (line 83)
   - **Entity state RESET to `Unchanged`** (line 89) ❌

2. **RsvpToEventCommandHandler** modifies event:
   - `@event.RegisterWithAttendees()` raises RegistrationConfirmedEvent
   - Domain event added to `_domainEvents` collection ✅
   - `_eventRepository.Update(@event)` called (line 101)
   - **BUT entity state remains `Unchanged`** ❌

3. **AppDbContext.CommitAsync()** executes:
   - `DetectChanges()` called (line 313) ✅
   - `ChangeTracker.Entries<BaseEntity>()` queried (line 316)
   - **FINDS ZERO modified Event entities** ❌ (state = `Unchanged`)
   - No domain events collected
   - **RegistrationConfirmedEvent NEVER dispatched** ❌

---

## Why UserLoggedInEvent Works

**UserRepository.GetByIdAsync()** (lines 25-54):
```csharp
public override async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    var user = await _dbSet
        .AsSplitQuery()
        .Include(u => u.CulturalInterests)
        .Include(u => u.Languages)
        .Include(u => u.ExternalLogins)
        .Include("_preferredMetroAreaEntities")  // Shadow navigation
        .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    if (user != null)
    {
        var metroAreasCollection = _context.Entry(user).Collection("_preferredMetroAreaEntities");
        var metroAreaEntities = metroAreasCollection.CurrentValue as IEnumerable<Domain.Events.MetroArea>;

        if (metroAreaEntities != null)
        {
            var metroAreaIds = metroAreaEntities.Select(m => m.Id).ToList();
            user.SyncPreferredMetroAreaIdsFromEntities(metroAreaIds);
            // ✅ NO STATE RESET - Entity remains properly tracked
        }
    }

    return user;
}
```

**Key Difference**: UserRepository does NOT reset entity state after sync.

**Result**:
1. User loaded (EntityState = `Unchanged`)
2. Metro area IDs synced (NO state change)
3. `user.RecordSuccessfulLogin()` called
4. EF Core auto-detects modification ✅
5. `DetectChanges()` finds modified User ✅
6. UserLoggedInEvent dispatched ✅

---

## The Fix

### Code Change

**File**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Repositories\EventRepository.cs`

**Lines Changed**: 74-86 (removed lines 79-80, 85-89)

**BEFORE (Phase 6A.33 - BROKEN):**
```csharp
if (emailGroupEntities != null)
{
    // Extract IDs and sync to domain's _emailGroupIds list
    var emailGroupIds = emailGroupEntities.Select(eg => eg.Id).ToList();

    // CRITICAL FIX Phase 6A.33: Store original state before sync
    var originalState = _context.Entry(eventEntity).State;

    // Sync the email group IDs from shadow navigation to domain list
    eventEntity.SyncEmailGroupIdsFromEntities(emailGroupIds);

    // CRITICAL FIX Phase 6A.33: Reset entity state to prevent UPDATE conflicts
    // The sync modifies _emailGroupIds which EF Core detects as a change
    // But this is infrastructure hydration, not a business modification
    // Reset state to what it was before sync (Unchanged for tracked entities)
    _context.Entry(eventEntity).State = originalState;  // ❌ CAUSED BUG
}
```

**AFTER (Phase 6A.34 - FIXED):**
```csharp
if (emailGroupEntities != null)
{
    // Extract IDs and sync to domain's _emailGroupIds list
    var emailGroupIds = emailGroupEntities.Select(eg => eg.Id).ToList();

    // CRITICAL FIX Phase 6A.34: Sync email group IDs from shadow navigation to domain list
    // This is infrastructure hydration for read operations
    // IMPORTANT: Do NOT reset entity state after sync - it breaks domain event detection
    // The state reset was preventing ChangeTracker.DetectChanges() from finding modified Event entities
    // in AppDbContext.CommitAsync(), which blocked RegistrationConfirmedEvent dispatch
    // Pattern: Unlike Phase 6A.33 approach, we let EF Core maintain proper change tracking state
    eventEntity.SyncEmailGroupIdsFromEntities(emailGroupIds);  // ✅ NO STATE RESET
}
```

### Rationale

**Why Phase 6A.33 added state reset:**
- Concern: "Prevent UPDATE conflicts"
- Assumption: Sync operation should not trigger entity modification
- Intent: Treat sync as "infrastructure hydration, not business modification"

**Why this was WRONG:**
1. Entity state management is EF Core's responsibility
2. Manually resetting state breaks change tracking
3. Subsequent business modifications (RegisterWithAttendees) are not detected
4. Domain events are never collected in CommitAsync()
5. UserRepository proved this pattern is unnecessary

**Correct Pattern:**
- Let EF Core manage entity state naturally
- Sync operation is part of entity hydration
- Business modifications will be auto-detected via DetectChanges()
- No manual state management needed

---

## Verification Plan

### Test Case 1: FREE Event Registration
1. User registers for FREE event
2. **Expected Logs**:
   ```
   [Phase 6A.24] Found 1 domain events to dispatch: RegistrationConfirmedEvent
   [Phase 6A.24] Dispatching domain event: RegistrationConfirmedEvent
   [Phase 6A.24] Successfully dispatched domain event: RegistrationConfirmedEvent
   Handling RegistrationConfirmedEvent for Event {EventId}, User {UserId}
   RSVP confirmation email sent successfully to {Email}
   ```
3. **Expected Result**: User receives confirmation email ✅

### Test Case 2: PAID Event Registration
1. User registers for PAID event
2. **Expected Logs**:
   ```
   [Phase 6A.24] Found 1 domain events to dispatch: RegistrationConfirmedEvent
   Handling RegistrationConfirmedEvent for Event {EventId}, User {UserId}
   Skipping email for paid event - waiting for payment completion
   ```
3. User completes Stripe payment
4. **Expected Logs**:
   ```
   [Phase 6A.24] Found 1 domain events to dispatch: PaymentCompletedEvent
   Handling PaymentCompletedEvent for Registration {RegistrationId}
   Payment confirmation email sent successfully to {Email}
   ```
5. **Expected Result**: User receives email after payment ✅

### Test Case 3: Login (Regression Test)
1. User logs in
2. **Expected Logs**:
   ```
   [Phase 6A.24] Found 1 domain events to dispatch: UserLoggedInEvent
   ```
3. **Expected Result**: No regression ✅

---

## Architecture Decision Record

### ADR-010: Entity State Management in Repository GetById Methods

**Context**:
- Repositories load entities with shadow navigations for many-to-many relationships
- Domain layer uses `List<Guid>` for business logic
- Infrastructure layer syncs domain list from shadow navigation entities
- State reset was preventing domain event dispatch

**Decision**:
**DO NOT manually reset entity state after syncing shadow navigation data.**

**Rationale**:
1. EF Core's change tracking is designed to handle entity state automatically
2. Manual state management is error-prone and breaks change detection
3. Sync operations are part of entity hydration, not separate state transitions
4. UserRepository pattern (no state reset) proves this approach is correct
5. Domain event dispatch depends on proper change tracking state

**Consequences**:
- **Positive**: Domain events are properly dispatched ✅
- **Positive**: Code is simpler and follows EF Core best practices ✅
- **Positive**: Consistent pattern across all repositories ✅
- **Negative**: Entity may be marked as Modified by sync operation (acceptable) ⚠️
- **Mitigation**: If sync-only loads cause unnecessary updates, use `AsNoTracking()` for read-only queries

**Pattern for All Repositories**:
```csharp
public override async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    var entity = await _dbSet
        .Include("_shadowNavigationProperty")  // Load shadow navigation
        .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    if (entity != null)
    {
        var shadowCollection = _context.Entry(entity).Collection("_shadowNavigationProperty");
        var shadowEntities = shadowCollection.CurrentValue as IEnumerable<TNavigationEntity>;

        if (shadowEntities != null)
        {
            var ids = shadowEntities.Select(e => e.Id).ToList();
            entity.SyncIdsFromEntities(ids);  // ✅ NO STATE RESET
        }
    }

    return entity;
}
```

---

## Related Issues

### Phase 6A.24: DetectChanges() Fix
- **Issue**: Domain events not collected before SaveChanges
- **Fix**: Added `ChangeTracker.DetectChanges()` at AppDbContext.cs:313
- **Status**: Working for User aggregate, but NOT for Event aggregate
- **Reason**: EventRepository state reset blocked change detection

### Phase 6A.33: Email Groups Integration
- **Issue**: Junction table not populated for event email groups
- **Fix**: Added shadow navigation sync in EventRepository.GetByIdAsync()
- **Side Effect**: Introduced state reset that broke domain event dispatch ❌
- **Resolution**: Phase 6A.34 removed state reset

---

## Lessons Learned

### 1. Trust EF Core's Change Tracking
**Don't**: Manually manage entity state unless absolutely necessary
**Do**: Let EF Core detect changes automatically via DetectChanges()

### 2. Test Domain Events End-to-End
**Don't**: Assume domain event dispatch works if DetectChanges() is called
**Do**: Verify domain events are actually dispatched and handlers are invoked

### 3. Pattern Consistency Across Repositories
**Don't**: Implement different patterns for similar operations in different repositories
**Do**: Follow established patterns (e.g., UserRepository for shadow navigation sync)

### 4. Avoid Premature Optimization
**Don't**: Add complex state management to "prevent UPDATE conflicts" without evidence
**Do**: Start with simple approach, optimize only if performance issues occur

### 5. Monitor Production Logs
**Don't**: Assume fixes work based on deployment success
**Do**: Verify via production logs that domain events are dispatched

---

## Next Steps

1. **Deploy Phase 6A.34 fix** to staging environment
2. **Test FREE event registration** and verify email sent
3. **Test PAID event registration** and verify email sent after payment
4. **Monitor production logs** for domain event dispatch confirmation
5. **Document pattern** in repository development guidelines
6. **Review all repositories** for similar state reset anti-patterns

---

## Files Modified

### c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Repositories\EventRepository.cs
- **Lines 74-86**: Removed entity state reset after email group sync
- **Rationale**: Allow EF Core to maintain proper change tracking state
- **Impact**: RegistrationConfirmedEvent will now be dispatched correctly

---

## Build Status

✅ Build successful (0 errors, 0 warnings)
✅ All tests passing (existing test suite)
✅ No breaking changes to public APIs
✅ Backward compatible with existing code

---

## Deployment Checklist

- [x] Root cause identified and documented
- [x] Fix implemented and tested locally
- [x] Build successful
- [x] Architecture decision recorded (ADR-010)
- [ ] Deploy to staging environment
- [ ] Verify FREE event registration email sent
- [ ] Verify PAID event registration email sent after payment
- [ ] Verify UserLoggedInEvent still works (regression test)
- [ ] Monitor production logs for domain event dispatch
- [ ] Update PROGRESS_TRACKER.md
- [ ] Update STREAMLINED_ACTION_PLAN.md
- [ ] Update TASK_SYNCHRONIZATION_STRATEGY.md

---

**Phase 6A.34 Status**: Implementation Complete - Ready for Deployment Testing
**Next Phase**: Phase 6A.35 - TBD
**Blocked By**: None
**Blocking**: Email confirmation functionality for event registrations
