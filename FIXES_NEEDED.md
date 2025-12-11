# Session 12 Follow-up Fixes

## Issues Identified

### Issue 1: No Create Event Button in /events Page ✅
**Status**: Needs Fix  
**Problem**: The main events listing page lacks a "Create Event" button for authorized users  
**Expected Behavior**: EventOrganizer, Admin, and AdminManager should see a "Create Event" button  
**Location**: `web/src/app/events/page.tsx`

**Changes Required**:
1. Add imports:
   - `useRouter` from 'next/navigation'  
   - `Button` component  
   - `Plus` icon from 'lucide-react'  
   - `isAdmin` from role-helpers  
   - `UserRole` type

2. Add router hook: `const router = useRouter();`

3. Add authorization check (around line 157):
```typescript
const canUserCreateEvents = user && (
  user.role === UserRole.EventOrganizer ||
  user.role === UserRole.Admin ||
  user.role === UserRole.AdminManager
);
```

4. Update page header layout (around line 168-177):
```typescript
<div className="flex items-center justify-between">
  <div className="flex-1">
    <h1 className="text-4xl font-bold text-white mb-4">
      Discover Events
    </h1>
    <p className="text-lg text-white/90 max-w-2xl">
      Find cultural, community, and social events relevant to you
    </p>
  </div>
  {canUserCreateEvents && (
    <Button
      onClick={() => router.push('/events/create')}
      className="flex items-center gap-2"
      style={{ background: '#FF7900' }}
    >
      <Plus className="h-5 w-5" />
      Create Event
    </Button>
  )}
</div>
```

---

### Issue 2: EventOrganizer Redirected to Login ✅
**Status**: Needs Fix  
**Problem**: Event Organizer role redirected to login when accessing `/events/create` or `/events/[id]/manage-signups`  
**Root Cause**: Auth guard checks only `isAuthenticated && user?.userId`, doesn't validate specific roles  

**Location 1**: `web/src/app/events/create/page.tsx` (lines 27-31, 34-46)  
**Changes Required**:
1. Import role helpers at top
2. Replace authentication check:

```typescript
// CURRENT (WRONG):
useEffect(() => {
  if (!isAuthenticated || !user?.userId) {
    router.push('/login?redirect=' + encodeURIComponent('/events/create'));
  }
}, [isAuthenticated, user, router]);

// FIXED (CORRECT):
useEffect(() => {
  if (!isAuthenticated || !user?.userId) {
    router.push('/login?redirect=' + encodeURIComponent('/events/create'));
    return;
  }
  
  // Check if user has permission to create events
  const canCreate = user.role === UserRole.EventOrganizer || 
                     user.role === UserRole.Admin || 
                     user.role === UserRole.AdminManager;
  
  if (!canCreate) {
    // Redirect to dashboard with error message
    router.push('/dashboard?error=unauthorized');
  }
}, [isAuthenticated, user, router]);
```

**Location 2**: `web/src/app/events/[id]/manage-signups/page.tsx` (line 61)  
**Changes Required**:
1. Update organizer check to include all authorized roles:

```typescript
// CURRENT (WRONG):
const isOrganizer = user?.userId === event?.organizerId;

// FIXED (CORRECT):  
const isOrganizer = user?.userId === event?.organizerId ||
                     user?.role === UserRole.Admin ||
                     user?.role === UserRole.AdminManager;
```

---

### Issue 3: Duplicate Routes vs Existing Dashboard Tabs ✅
**Status**: Architectural Decision Needed  
**Problem**: `/events/my-events` duplicates "My Created Events" tab in dashboard

**Existing Functionality** (in `/dashboard`):
- "My Registered Events" tab (line 334-343) - Shows events user registered for
- "My Created Events" tab (line 344-355) - Shows events user created (EventOrganizer/Admin)

**New Routes Created** (Session 12):
- `/events/my-events` - DUPLICATE of "My Created Events" tab
- `/events/create` - UNIQUE, should be kept
- `/events/[id]/manage-signups` - UNIQUE, should be kept

**Recommended Solution**:

**Option A**: Remove `/events/my-events` route entirely  
- Delete `web/src/app/events/my-events/page.tsx` (415 lines)
- Remove `getMyEvents()` from events.repository.ts
- Remove `useMyEvents()` hook from useEvents.ts
- Users access created events via Dashboard → "My Created Events" tab
- Add "Manage Sign-Ups" button to dashboard event cards

**Option B**: Keep `/events/my-events` but remove duplicate tab  
- Keep the new route (more detailed organizer dashboard)
- Remove "My Created Events" tab from `/dashboard`
- Update "Create Event" buttons to link to `/events/my-events` instead of dashboard

**Recommendation**: Option A (Remove /events/my-events)  
**Reason**: Dashboard tabs are the established pattern, users expect to find their events there

---

## Implementation Steps

1. **Fix /events page** (Add Create Event button)
2. **Fix /events/create auth** (Allow EventOrganizer/Admin/AdminManager)
3. **Fix /events/[id]/manage-signups auth** (Allow EventOrganizer/Admin/AdminManager)
4. **Decide on Option A or B** for duplicate route
5. **Test all changes** (0 TypeScript errors)
6. **Commit fixes** with detailed message
7. **Update documentation** (PROGRESS_TRACKER.md, STREAMLINED_ACTION_PLAN.md)

---

## Files to Modify

1. `web/src/app/events/page.tsx` - Add Create Event button
2. `web/src/app/events/create/page.tsx` - Fix auth guard
3. `web/src/app/events/[id]/manage-signups/page.tsx` - Fix organizer check
4. `web/src/app/events/my-events/page.tsx` - DELETE (if Option A) or KEEP (if Option B)
5. `web/src/infrastructure/api/repositories/events.repository.ts` - Maybe remove getMyEvents()
6. `web/src/presentation/hooks/useEvents.ts` - Maybe remove useMyEvents()
7. `web/src/app/(dashboard)/dashboard/page.tsx` - Maybe add Manage Sign-Ups button to event cards

