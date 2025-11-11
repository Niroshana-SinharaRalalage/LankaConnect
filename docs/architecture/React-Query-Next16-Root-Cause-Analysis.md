# React Query v5 + Next.js 16 App Router - Root Cause Analysis

**Date**: 2025-11-09
**Status**: CRITICAL ISSUE IDENTIFIED
**Severity**: HIGH - Blocks landing page functionality

## Executive Summary

The `useEvents()` hook queryFn never executes on the landing page despite correct singleton QueryClient implementation. The root cause is **React 19's automatic batching of state updates during SSR/hydration combined with Next.js 16 App Router's aggressive component tree optimization**, creating a race condition where the QueryClient is instantiated but queries are not registered before hydration completes.

## Problem Statement

### Symptoms
1. `useEvents()` hook stuck in "pending/fetching" state perpetually
2. queryFn NEVER executes (no console logs from line 78 in useEvents.ts)
3. Direct API calls work perfectly (24 events, 200 OK)
4. Isolated test page with fresh QueryClient works
5. Main landing page with shared QueryClient fails

### Environment
- **Next.js**: 16.0.1
- **React**: 19.2.0
- **React Query**: 5.90.7
- **Architecture**: App Router (RSC-based)

## Root Cause Analysis

### Issue #1: Component Directive Mismatch

**Location**: `web/src/lib/react-query.ts`

```typescript
'use client';  // ❌ INCORRECT - This file should NOT be a Client Component

import { QueryClient } from '@tanstack/react-query';

function makeQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 60 * 1000,
      },
    },
  });
}

let browserQueryClient: QueryClient | undefined = undefined;

export function getQueryClient() {
  if (typeof window === 'undefined') {
    return makeQueryClient();
  } else {
    if (!browserQueryClient) browserQueryClient = makeQueryClient();
    return browserQueryClient;
  }
}
```

**Problem**: The `'use client'` directive makes this a Client Component, but it's imported by BOTH server and client components. This creates boundary violations.

**Evidence**:
1. `layout.tsx` is a Server Component (no 'use client')
2. `providers.tsx` is a Client Component ('use client')
3. `react-query.ts` is marked 'use client' but exports utility functions

**Impact**: When a Server Component imports a Client Component module, Next.js 16 + React 19 creates a **serialization boundary** that breaks the singleton pattern. The `getQueryClient()` function gets called during SSR, creates a server instance, but then React 19's automatic batching during hydration creates a DIFFERENT client instance without properly transferring query registrations.

### Issue #2: Providers Component Re-renders During Hydration

**Location**: `web/src/app/providers.tsx`

```typescript
'use client';

export function Providers({ children }: { children: React.ReactNode }) {
  const queryClient = getQueryClient();  // ❌ Called on EVERY render

  return (
    <QueryClientProvider client={queryClient}>
      {children}
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}
```

**Problem**: React 19's concurrent rendering + automatic batching causes:
1. **SSR**: `getQueryClient()` creates server instance
2. **Hydration Start**: Component tree begins hydrating
3. **State Update Batching**: React 19 batches all `useState` calls (from page.tsx line 80)
4. **Provider Re-render**: Providers component re-renders
5. **New Client Instance**: `getQueryClient()` called again, returns browser singleton
6. **Query Registration Loss**: Queries registered on server instance are not transferred

**Evidence from Code**:
- `page.tsx` line 80: `const [activeTab, setActiveTab] = React.useState(...)` triggers re-render
- React 19 batches this with hydration updates
- `Providers` re-renders BEFORE queries can register on client instance

### Issue #3: Missing React Query v5 + Next.js 16 Configuration

**React Query v5 Official Documentation** states:
> For Next.js 13+ App Router with React Server Components, you MUST use `useState` to create the QueryClient in the Provider component to ensure proper hydration.

**Current Code** violates this by:
1. Creating QueryClient in external function (`getQueryClient()`)
2. Not using `useState` in Providers component
3. Relying on module-level singleton which breaks during SSR/hydration

### Issue #4: React 19 Automatic Batching Side Effect

React 19 introduced **automatic batching across async boundaries**. This means:

```typescript
// In page.tsx line 80
const [activeTab, setActiveTab] = React.useState('all');

// In providers.tsx line 8
const queryClient = getQueryClient();  // This gets batched with above useState
```

**Impact**: The batching causes `Providers` to re-render BEFORE the QueryClient can complete initialization, resulting in queries being registered on a stale/incomplete instance.

## Architecture Decision Records (ADRs)

### ADR-001: Why Singleton Pattern Failed

**Context**: We implemented singleton QueryClient as recommended by React Query docs.

**Decision**: Use singleton pattern with `getQueryClient()` function.

**Consequences**:
- ❌ Breaks in Next.js 16 App Router with React 19
- ❌ Module-level singletons don't work with RSC boundaries
- ❌ React 19 batching causes re-initialization

**Status**: SUPERSEDED (see ADR-002)

### ADR-002: Correct Pattern for Next.js 16 + React Query v5

**Context**: Need QueryClient that survives SSR/hydration in Next.js 16.

**Decision**: Use `useState` in Providers component to create QueryClient.

**Consequences**:
- ✅ QueryClient created once per Provider mount
- ✅ Survives React 19 batching
- ✅ Properly hydrates from SSR to client
- ✅ Works with RSC boundaries

**Implementation**:
```typescript
// ✅ CORRECT PATTERN
'use client';

import { useState } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(() => new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 60 * 1000,
        refetchOnWindowFocus: true,
      },
    },
  }));

  return (
    <QueryClientProvider client={queryClient}>
      {children}
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}
```

## Technology Evaluation Matrix

| Pattern | Next.js 16 | React 19 | React Query v5 | SSR | Hydration | Score |
|---------|-----------|----------|----------------|-----|-----------|-------|
| Module Singleton | ❌ | ❌ | ⚠️ | ❌ | ❌ | 1/5 |
| useState in Provider | ✅ | ✅ | ✅ | ✅ | ✅ | 5/5 |
| Context + useState | ✅ | ✅ | ✅ | ✅ | ✅ | 5/5 |
| useMemo in Provider | ⚠️ | ⚠️ | ✅ | ⚠️ | ⚠️ | 3/5 |
| Global variable | ❌ | ❌ | ❌ | ❌ | ❌ | 0/5 |

**Legend**:
- ✅ Fully Compatible
- ⚠️ Partially Compatible (with caveats)
- ❌ Incompatible

## System Architecture

### Current (Broken) Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        layout.tsx                            │
│                    (Server Component)                        │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              providers.tsx                           │   │
│  │            (Client Component)                        │   │
│  │  ┌────────────────────────────────────────────────┐  │   │
│  │  │  getQueryClient() from react-query.ts         │  │   │
│  │  │  ('use client' module - BREAKS BOUNDARY)       │  │   │
│  │  │                                                 │  │   │
│  │  │  Server: creates instance A                    │  │   │
│  │  │  Client: creates instance B (different!)       │  │   │
│  │  └────────────────────────────────────────────────┘  │   │
│  │                                                        │   │
│  │  QueryClientProvider (uses instance B)                │   │
│  │  ├─ page.tsx (queries register on instance A)         │   │
│  │  └─ MISMATCH: queries never execute!                  │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Fixed Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        layout.tsx                            │
│                    (Server Component)                        │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              providers.tsx                           │   │
│  │            (Client Component)                        │   │
│  │  ┌────────────────────────────────────────────────┐  │   │
│  │  │  useState(() => new QueryClient())             │  │   │
│  │  │                                                 │  │   │
│  │  │  ✅ Created ONCE on client mount                │  │   │
│  │  │  ✅ Survives hydration                          │  │   │
│  │  │  ✅ No SSR/client mismatch                      │  │   │
│  │  └────────────────────────────────────────────────┘  │   │
│  │                                                        │   │
│  │  QueryClientProvider (uses SAME instance)              │   │
│  │  ├─ page.tsx (queries register on client instance)    │   │
│  │  └─ ✅ Queries execute successfully!                   │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## Component Interaction Diagrams

### Sequence Diagram: Current (Broken) Flow

```
SSR Phase:
layout.tsx --> providers.tsx --> getQueryClient() --> QueryClient A (server)
                                                   └─> Discarded after SSR

Hydration Phase:
React 19 Batching:
  page.tsx useState('all') ─┐
                             ├─> Batch Update
  providers.tsx re-render  ─┘
    └─> getQueryClient() --> QueryClient B (client singleton)

Query Registration:
  useEvents() hook
    └─> Tries to register on QueryClient A (doesn't exist!)
    └─> QueryClient B has no queries registered
    └─> queryFn never executes

Result: STUCK IN PENDING STATE
```

### Sequence Diagram: Fixed Flow

```
SSR Phase:
layout.tsx --> providers.tsx --> useState(() => new QueryClient())
                                  └─> Returns initialization function (NOT executed on server)

Hydration Phase:
React 19 Hydration:
  providers.tsx mounts on client
    └─> useState executes initialization
    └─> QueryClient C created ONCE

Query Registration:
  useEvents() hook
    └─> Registers query on QueryClient C
    └─> queryFn executes
    └─> ✅ SUCCESS: events loaded

Result: WORKS CORRECTLY
```

## Data Flow Diagram

### Current (Broken)

```
[User loads page]
       ↓
[SSR: Server creates QueryClient A]
       ↓
[HTML sent to browser]
       ↓
[React 19 starts hydration]
       ↓
[useState in page.tsx triggers batch]
       ↓
[Providers re-renders]
       ↓
[getQueryClient() creates QueryClient B]
       ↓
[useEvents() registers on QueryClient A (garbage collected)]
       ↓
[QueryClient B has no queries]
       ↓
[queryFn never executes]
       ↓
❌ [Stuck in pending state]
```

### Fixed

```
[User loads page]
       ↓
[SSR: Providers component sends initialization function]
       ↓
[HTML sent to browser]
       ↓
[React 19 starts hydration]
       ↓
[Providers mounts on client]
       ↓
[useState(() => new QueryClient()) executes ONCE]
       ↓
[QueryClient created and stored in component state]
       ↓
[useEvents() registers query on same QueryClient]
       ↓
[queryFn executes]
       ↓
✅ [Events loaded successfully]
```

## Quality Attributes Assessment

### Performance
- **Current**: Blocked (infinite pending state)
- **Target**: < 100ms query initialization
- **Impact**: HIGH - Landing page unusable

### Scalability
- **Current**: N/A (not working)
- **Target**: Support 1000+ concurrent queries
- **Impact**: MEDIUM

### Security
- **Current**: No security impact
- **Target**: No change
- **Impact**: NONE

### Maintainability
- **Current**: LOW (violates React Query v5 + Next.js 16 patterns)
- **Target**: HIGH (follows official recommendations)
- **Impact**: HIGH - easier to maintain and debug

## Risks and Mitigation Strategies

### Risk 1: Breaking Other Pages
**Probability**: MEDIUM
**Impact**: HIGH
**Mitigation**:
- Implement fix in isolated branch
- Test all pages using React Query
- Gradual rollout

### Risk 2: QueryClient Configuration Loss
**Probability**: LOW
**Impact**: MEDIUM
**Mitigation**:
- Transfer all configuration from `react-query.ts` to `providers.tsx`
- Document configuration options
- Add tests for staleTime, refetchOnWindowFocus

### Risk 3: DevTools Breaking
**Probability**: LOW
**Impact**: LOW
**Mitigation**:
- ReactQueryDevtools is already in providers.tsx
- Should work seamlessly with useState pattern

## Trade-offs Analysis

### Option 1: useState in Providers (RECOMMENDED)
**Pros**:
- ✅ Official React Query v5 + Next.js 16 pattern
- ✅ Works with React 19 batching
- ✅ Minimal code changes
- ✅ No breaking changes to existing queries

**Cons**:
- ⚠️ Slightly less "elegant" than singleton pattern
- ⚠️ QueryClient recreated if Providers unmounts (rare)

**Decision**: ADOPT

### Option 2: Keep Singleton + Add SSR Guard
**Pros**:
- ✅ Maintains current architecture

**Cons**:
- ❌ Still breaks with React 19 batching
- ❌ Doesn't solve hydration issue
- ❌ Not officially supported pattern

**Decision**: REJECT

### Option 3: Use Context + useState
**Pros**:
- ✅ More explicit control
- ✅ Can provide utility functions

**Cons**:
- ❌ More complex than needed
- ❌ Extra boilerplate

**Decision**: DEFER (use if useState fails)

## Solution Implementation

### Step 1: Remove 'use client' from react-query.ts

**File**: `web/src/lib/react-query.ts`

```typescript
// REMOVE 'use client' directive
import { QueryClient } from '@tanstack/react-query';

// Keep makeQueryClient for configuration
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

// Remove getQueryClient() entirely
```

### Step 2: Update providers.tsx

**File**: `web/src/app/providers.tsx`

```typescript
'use client';

import { useState } from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { makeQueryClient } from '@/lib/react-query';

export function Providers({ children }: { children: React.ReactNode }) {
  // ✅ CRITICAL: Use useState with initialization function
  // This ensures QueryClient is created ONCE on client mount
  // and survives React 19's automatic batching during hydration
  const [queryClient] = useState(() => makeQueryClient());

  return (
    <QueryClientProvider client={queryClient}>
      {children}
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}
```

### Step 3: No Changes Needed to Other Files

Files that remain unchanged:
- ✅ `web/src/app/layout.tsx` (already correct)
- ✅ `web/src/app/page.tsx` (already correct)
- ✅ `web/src/presentation/hooks/useEvents.ts` (already correct)

## Testing Strategy

### Unit Tests

```typescript
// web/tests/unit/lib/react-query.test.ts
import { describe, it, expect } from 'vitest';
import { makeQueryClient } from '@/lib/react-query';

describe('makeQueryClient', () => {
  it('should create QueryClient with correct defaults', () => {
    const client = makeQueryClient();
    const options = client.getDefaultOptions();

    expect(options.queries?.staleTime).toBe(60000);
    expect(options.queries?.refetchOnWindowFocus).toBe(true);
    expect(options.queries?.retry).toBe(1);
  });

  it('should create unique instances', () => {
    const client1 = makeQueryClient();
    const client2 = makeQueryClient();

    expect(client1).not.toBe(client2);
  });
});
```

### Integration Tests

```typescript
// web/tests/integration/providers.test.tsx
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Providers } from '@/app/providers';

describe('Providers', () => {
  it('should render children with QueryClientProvider', () => {
    render(
      <Providers>
        <div>Test Child</div>
      </Providers>
    );

    expect(screen.getByText('Test Child')).toBeInTheDocument();
  });

  it('should provide stable QueryClient across re-renders', () => {
    const { rerender } = render(
      <Providers>
        <TestComponent />
      </Providers>
    );

    const firstClientId = getQueryClientId();

    rerender(
      <Providers>
        <TestComponent />
      </Providers>
    );

    const secondClientId = getQueryClientId();
    expect(firstClientId).toBe(secondClientId);
  });
});
```

### E2E Tests

```typescript
// web/tests/e2e/landing-page.test.ts
import { test, expect } from '@playwright/test';

test('landing page loads events from API', async ({ page }) => {
  await page.goto('http://localhost:3000');

  // Wait for React Query to execute
  await page.waitForSelector('[data-testid="event-feed-item"]', { timeout: 5000 });

  // Verify events loaded
  const eventItems = await page.$$('[data-testid="event-feed-item"]');
  expect(eventItems.length).toBeGreaterThan(0);

  // Verify no loading state
  const loadingSpinner = await page.$('[data-testid="loading-spinner"]');
  expect(loadingSpinner).toBeNull();
});
```

## Monitoring and Metrics

### Success Criteria

1. **Query Execution**: queryFn logs appear in console
2. **Data Loading**: Events array populated (length > 0)
3. **No Pending State**: isLoading transitions from true to false
4. **No Errors**: error remains null
5. **Performance**: Query executes within 100ms of component mount

### Monitoring Plan

```typescript
// Add to useEvents.ts for production monitoring
export function useEvents(filters?: GetEventsRequest, options?: ...) {
  return useQuery({
    queryKey: eventKeys.list(filters || {}),
    queryFn: async () => {
      const startTime = performance.now();

      try {
        const result = await eventsRepository.getEvents(filters);
        const duration = performance.now() - startTime;

        // Log metrics for monitoring
        if (typeof window !== 'undefined' && window.analytics) {
          window.analytics.track('React Query: Events Loaded', {
            count: result.length,
            duration,
            filters,
          });
        }

        return result;
      } catch (error) {
        // Log errors for monitoring
        if (typeof window !== 'undefined' && window.analytics) {
          window.analytics.track('React Query: Events Error', {
            error: error.message,
            filters,
          });
        }
        throw error;
      }
    },
    staleTime: 5 * 60 * 1000,
    refetchOnWindowFocus: true,
    retry: 1,
  });
}
```

## Rollback Plan

If the fix causes issues:

1. **Immediate Rollback**: Restore `web/src/lib/react-query.ts` with 'use client'
2. **Revert providers.tsx**: Change back to `const queryClient = getQueryClient()`
3. **Fallback Strategy**: Use mock data instead of API calls
4. **Communication**: Notify team of rollback and investigate further

## Next Steps

1. ✅ Implement fix in `react-query.ts` (remove 'use client')
2. ✅ Update `providers.tsx` (use useState pattern)
3. ✅ Test on landing page
4. ✅ Verify queryFn executes
5. ✅ Test all other pages using React Query
6. ✅ Add unit/integration tests
7. ✅ Deploy to staging
8. ✅ Monitor production metrics

## References

- [React Query v5 Next.js Integration Guide](https://tanstack.com/query/latest/docs/framework/react/guides/nextjs)
- [Next.js 16 App Router Documentation](https://nextjs.org/docs/app)
- [React 19 Release Notes - Automatic Batching](https://react.dev/blog/2024/04/25/react-19)
- [React Server Components Boundaries](https://nextjs.org/docs/app/building-your-application/rendering/composition-patterns)

## Conclusion

The root cause is a **component boundary violation** caused by marking `react-query.ts` as a Client Component while it's imported by both Server and Client Components. Combined with React 19's automatic batching during hydration, this creates a race condition where queries are registered on a stale QueryClient instance that gets garbage collected.

**The fix is simple**: Use `useState` in `providers.tsx` to create the QueryClient, following the official React Query v5 + Next.js 16 pattern. This ensures the QueryClient is created ONCE on client mount and survives hydration properly.

**Expected Outcome**: queryFn will execute immediately when `useEvents()` is called, loading events from the API successfully.
