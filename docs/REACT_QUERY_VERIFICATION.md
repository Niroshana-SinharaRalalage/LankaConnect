# React Query Fix Verification Guide

## Quick Verification (2 minutes)

### Step 1: Check Files Were Updated

```bash
# Verify react-query.ts has no 'use client' directive
grep -n "use client" web/src/lib/react-query.ts
# Expected: NO RESULTS (should be empty)

# Verify providers.tsx uses useState
grep -n "useState" web/src/app/providers.tsx
# Expected: Line 3 (import) and line 26 (usage)

# Verify makeQueryClient exists
grep -n "makeQueryClient" web/src/lib/react-query.ts
# Expected: Line 26 (function definition)
```

### Step 2: Start Development Server

```bash
cd web
npm run dev
```

Wait for:
```
✓ Ready in 2.5s
○ Local:        http://localhost:3000
```

### Step 3: Open Landing Page

Navigate to: http://localhost:3000

**Open Browser DevTools Console** (F12)

### Step 4: Verify Console Logs

You MUST see these logs in order:

```
1. 🔄 useEvents queryFn called with filters: {status: 2, startDateFrom: "..."}
2. ✅ useEvents queryFn SUCCESS: 24 events
3. 🔍 allFeedItems calculation: {events: 24, isLoading: false, error: null}
4. ✅ Using API events: 24
```

**If you see these logs**: ✅ FIX IS WORKING!

**If you don't see log #1**: ❌ queryFn still not executing (issue persists)

### Step 5: Visual Verification

Check the landing page:

1. **Feed Section**: Should show 24+ event cards
2. **Loading State**: Should NOT show infinite spinner
3. **Event Titles**: Should show real event names (not just "Sample Event")
4. **Event Locations**: Should show real cities (Los Angeles, New York, etc.)
5. **Metro Selector**: Should work and filter events

### Step 6: React Query DevTools

Look for the React Query DevTools panel (bottom-left corner of page)

Click to open, then verify:

- Query Key: `["events", "list", {status: 2, startDateFrom: "..."}]`
- Status: **success** (green badge)
- Fetch Status: **idle**
- Data: Array[24] with event objects
- Last Updated: Recent timestamp

**If DevTools shows "pending"**: ❌ Issue persists

## Detailed Verification (10 minutes)

### 1. Code Review

**File: `web/src/lib/react-query.ts`**

Expected structure:
```typescript
// NO 'use client' directive at top
import { QueryClient } from '@tanstack/react-query';

export function makeQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 60 * 1000,
        refetchOnWindowFocus: true,
        retry: 1,
      },
    },
  });
}

// NO getQueryClient function
// NO browserQueryClient variable
```

**File: `web/src/app/providers.tsx`**

Expected structure:
```typescript
'use client';

import { useState } from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { makeQueryClient } from '@/lib/react-query';

export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(() => makeQueryClient());

  return (
    <QueryClientProvider client={queryClient}>
      {children}
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}
```

### 2. Functional Testing

#### Test A: Events Load from API

1. Open landing page
2. Wait 3 seconds
3. Scroll to feed section
4. Count visible event cards

**Expected**: 24+ events visible
**Pass Criteria**: Events show real data (not "Sample Event")

#### Test B: Metro Area Filtering

1. Click metro area selector
2. Select "Los Angeles Metro"
3. Observe feed items

**Expected**: Only LA-area events visible
**Pass Criteria**: Filter works without errors

#### Test C: Tab Filtering

1. Click "Events" tab
2. Observe feed items

**Expected**: Only event-type items visible
**Pass Criteria**: Filter works without errors

#### Test D: Query Refetch

1. Open React Query DevTools
2. Find the events query
3. Click "Refetch" button

**Expected**: Query re-executes successfully
**Pass Criteria**: Console shows queryFn logs again

### 3. Performance Testing

#### Test E: Query Initialization Time

Open DevTools Console and run:
```javascript
performance.mark('query-start');
// Reload page
// After events load, run:
performance.mark('query-end');
performance.measure('query-time', 'query-start', 'query-end');
console.log(performance.getEntriesByName('query-time')[0].duration);
```

**Expected**: < 500ms
**Pass Criteria**: Query executes quickly without hanging

#### Test F: Memory Leak Check

1. Open DevTools Memory tab
2. Take heap snapshot
3. Reload page 5 times
4. Take another heap snapshot
5. Compare sizes

**Expected**: < 10MB increase
**Pass Criteria**: No significant memory growth

### 4. Regression Testing

Test these pages to ensure no breaking changes:

#### Test G: Dashboard Page

Navigate to: http://localhost:3000/dashboard

**Expected**: Page loads without errors
**Pass Criteria**: No console errors, page renders correctly

#### Test H: Profile Page

Navigate to: http://localhost:3000/profile

**Expected**: Page loads without errors
**Pass Criteria**: No console errors, page renders correctly

#### Test I: Test Page

Navigate to: http://localhost:3000/test-simple

**Expected**: Page loads and shows events
**Pass Criteria**: Query executes (this page has its own QueryClient)

### 5. Error Handling Testing

#### Test J: API Failure Simulation

1. Open DevTools Network tab
2. Enable "Offline" mode
3. Reload landing page

**Expected**: Error state shown, fallback to cached data
**Pass Criteria**: No infinite loading, user sees error message

#### Test K: Query Cancellation

1. Open landing page
2. Immediately navigate away (before query completes)
3. Return to landing page

**Expected**: Query executes normally on return
**Pass Criteria**: No stale queries, fresh data loads

## Troubleshooting

### Issue: queryFn Still Not Executing

**Symptoms**:
- No console logs from useEvents queryFn
- Infinite loading spinner
- React Query DevTools shows "pending"

**Possible Causes**:
1. Dev server not restarted after file changes
2. Browser cache not cleared
3. TypeScript compilation errors

**Solutions**:
```bash
# 1. Hard restart dev server
cd web
pkill -f "next dev"  # or Ctrl+C
npm run dev

# 2. Clear Next.js cache
rm -rf .next
npm run dev

# 3. Clear browser cache
# In Chrome: Ctrl+Shift+Delete > Clear cache
# Or use Incognito mode
```

### Issue: TypeScript Errors

**Symptoms**:
- Red squiggly lines in IDE
- Build fails with type errors

**Solutions**:
```bash
# Check if errors are related to our changes
cd web
npx tsc --noEmit | grep "react-query\|providers"

# If no results, errors are pre-existing
# If results show, review file changes
```

### Issue: React Hydration Errors

**Symptoms**:
- Console shows "Hydration failed"
- Content flashes/re-renders

**Solutions**:
```bash
# Verify 'use client' directives are correct
grep -r "use client" web/src/app/ web/src/lib/

# Should only show:
# - providers.tsx: 'use client'
# - page.tsx: 'use client'
# - NOT react-query.ts
```

### Issue: Multiple QueryClient Instances

**Symptoms**:
- React Query DevTools shows duplicate queries
- Queries execute multiple times

**Solutions**:
```typescript
// Add to providers.tsx for debugging
const [queryClient] = useState(() => {
  const client = makeQueryClient();
  console.log('🔧 QueryClient created:', client);
  return client;
});

// Should only log ONCE per page load
```

## Success Criteria Checklist

- [ ] No 'use client' in react-query.ts
- [ ] useState used in providers.tsx
- [ ] Console shows queryFn execution logs
- [ ] Events array populated (24+ items)
- [ ] Feed displays real event data
- [ ] Metro area filtering works
- [ ] Tab filtering works
- [ ] React Query DevTools shows "success"
- [ ] No infinite loading states
- [ ] No TypeScript errors in changed files
- [ ] No React hydration errors
- [ ] Dashboard/Profile pages work
- [ ] Performance < 500ms
- [ ] No memory leaks

## Automated Test Script

Copy this into browser console for quick verification:

```javascript
// Quick Verification Script
(function() {
  const results = {
    queryFnExecuted: false,
    eventsLoaded: false,
    queryStatus: null,
    errors: []
  };

  // Check console logs
  const originalLog = console.log;
  console.log = function(...args) {
    if (args[0]?.includes('useEvents queryFn called')) {
      results.queryFnExecuted = true;
    }
    if (args[0]?.includes('useEvents queryFn SUCCESS')) {
      results.eventsLoaded = true;
    }
    originalLog.apply(console, args);
  };

  // Check React Query DevTools
  setTimeout(() => {
    const devtools = document.querySelector('[data-tanstack-query-devtools]');
    if (devtools) {
      const queryState = window.__REACT_QUERY_DEVTOOLS_GLOBAL_HOOK__?.queries?.get?.('events');
      results.queryStatus = queryState?.state?.status || 'unknown';
    }

    // Report
    console.log('=== VERIFICATION RESULTS ===');
    console.log('QueryFn Executed:', results.queryFnExecuted ? '✅' : '❌');
    console.log('Events Loaded:', results.eventsLoaded ? '✅' : '❌');
    console.log('Query Status:', results.queryStatus);
    console.log('Errors:', results.errors.length === 0 ? '✅ None' : results.errors);

    if (results.queryFnExecuted && results.eventsLoaded && results.queryStatus === 'success') {
      console.log('\n🎉 FIX IS WORKING! All checks passed.');
    } else {
      console.log('\n⚠️ Some checks failed. Review above.');
    }
  }, 3000);
})();
```

## Expected Timeline

- **Immediate** (0s): Page loads, shows loading spinner
- **< 100ms**: QueryClient initializes
- **< 200ms**: queryFn executes (console log appears)
- **< 500ms**: API responds with 24 events
- **< 600ms**: Feed renders with real data
- **< 700ms**: Loading spinner disappears

**Total Time to Interactive**: < 1 second

## Contact

If verification fails after following this guide:

1. Capture full console logs (copy/paste)
2. Take screenshot of React Query DevTools
3. Run `git status` and share output
4. Check if any files were not saved
5. Review documentation in `docs/architecture/React-Query-Next16-Root-Cause-Analysis.md`

---

**Last Updated**: 2025-11-09
**Fix Version**: 1.0.0
**Compatible With**: Next.js 16.0.1, React 19.2.0, React Query 5.90.7
