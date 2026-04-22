import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';

import { signUpKeys, useReorderSignUpItems } from '@/presentation/hooks/useEventSignUps';
import {
  SignUpKind,
  SignUpItemCategory,
  SignUpItemType,
  SignUpType,
  type SignUpListDto,
  type SignUpItemDto,
} from '@/infrastructure/api/types/events.types';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';

/**
 * Phase 6A.132 UX follow-up 5 — optimistic update must reach the cache
 * entry that the calling component is actually subscribed to.
 *
 * Bug reported 2026-04-22 after UX follow-up 4 shipped: "It takes about
 * 4 seconds to move one item up/down with the arrow button click."
 *
 * Root cause: `SignUpManagementSection` subscribes via
 * `useEventSignUps(eventId, kind)` which stores data under a kind-filtered
 * cache key (`['signups', 'list', eventId, { kind: 'Items' }]`). But
 * `useReorderSignUpItems` optimistically called `queryClient.setQueryData`
 * with the unfiltered key (`['signups', 'list', eventId]`) — a completely
 * different cache entry that no component was listening to. The UI did NOT
 * reflect the reorder until `onSettled`'s `invalidateQueries` triggered a
 * refetch (prefix match covers both entries) — adding the full PUT round
 * trip + GET refetch latency (~1-4s depending on network + cold start)
 * before the user saw anything move.
 *
 * Fix: `setQueriesData` / `getQueriesData` with a prefix filter so BOTH
 * the unfiltered and any kind-filtered cache entries receive the
 * optimistic update instantly.
 */

vi.mock('@/infrastructure/api/repositories/events.repository', () => ({
  eventsRepository: {
    reorderSignUpItems: vi.fn(),
  },
}));

const makeItem = (id: string, desc: string, displayOrder: number): SignUpItemDto =>
  ({
    id,
    itemDescription: desc,
    itemType: SignUpItemType.Quantity,
    targetQuantity: 10,
    committedQuantity: 0,
    remainingQuantity: 10,
    itemCategory: SignUpItemCategory.Mandatory,
    notes: null,
    commitments: [],
    isFullyCommitted: false,
    isOpenItem: false,
    displayOrder,
  } as SignUpItemDto);

const makeList = (items: SignUpItemDto[]): SignUpListDto =>
  ({
    id: 'list-1',
    category: 'Food',
    description: 'Potluck',
    signUpType: SignUpType.Predefined,
    hasMandatoryItems: true,
    hasPreferredItems: false,
    hasSuggestedItems: false,
    hasOpenItems: false,
    items,
    commitments: [],
    predefinedItems: [],
    commitmentCount: 0,
  } as SignUpListDto);

function makeWrapperAndClient() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const wrapper = ({ children }: { children: React.ReactNode }) =>
    React.createElement(QueryClientProvider, { client }, children);
  return { client, wrapper };
}

describe('useReorderSignUpItems optimistic update — cache-key coverage', () => {
  const eventId = 'evt-1';
  const signupId = 'list-1';

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('optimistically reorders the Items-kind-filtered cache entry (the one SignUpManagementSection subscribes to)', async () => {
    const { client, wrapper } = makeWrapperAndClient();
    const initialItems = [
      makeItem('a', 'Rice', 0),
      makeItem('b', 'Curry', 1),
      makeItem('c', 'Salad', 2),
    ];
    const initialList = makeList(initialItems);

    // Seed the kind-filtered cache exactly like SignUpManagementSection does.
    const itemsKey = signUpKeys.list(eventId, SignUpKind.Items);
    client.setQueryData<SignUpListDto[]>(itemsKey, [initialList]);

    // Keep the PUT pending so onSettled doesn't fire and overwrite via refetch.
    vi.mocked(eventsRepository.reorderSignUpItems).mockImplementation(
      () => new Promise(() => {}) // never resolves
    );

    const { result } = renderHook(() => useReorderSignUpItems(), { wrapper });

    act(() => {
      result.current.mutate({
        eventId,
        signupId,
        orderedItemIds: ['b', 'a', 'c'], // swap Rice and Curry
      });
    });

    // Optimistic update must propagate to the kind-filtered cache immediately.
    await waitFor(() => {
      const cached = client.getQueryData<SignUpListDto[]>(itemsKey);
      expect(cached).toBeDefined();
      expect(cached![0].items.map((i) => i.id)).toEqual(['b', 'a', 'c']);
      // displayOrder must also be dense 0..N-1 so downstream ordering is stable.
      expect(cached![0].items.map((i) => i.displayOrder)).toEqual([0, 1, 2]);
    });
  });

  it('(regression guard) still updates the unfiltered cache entry for legacy callers', async () => {
    const { client, wrapper } = makeWrapperAndClient();
    const initialList = makeList([
      makeItem('a', 'Rice', 0),
      makeItem('b', 'Curry', 1),
    ]);

    const unfilteredKey = signUpKeys.list(eventId);
    client.setQueryData<SignUpListDto[]>(unfilteredKey, [initialList]);

    vi.mocked(eventsRepository.reorderSignUpItems).mockImplementation(
      () => new Promise(() => {})
    );

    const { result } = renderHook(() => useReorderSignUpItems(), { wrapper });

    act(() => {
      result.current.mutate({
        eventId,
        signupId,
        orderedItemIds: ['b', 'a'],
      });
    });

    await waitFor(() => {
      const cached = client.getQueryData<SignUpListDto[]>(unfilteredKey);
      expect(cached).toBeDefined();
      expect(cached![0].items.map((i) => i.id)).toEqual(['b', 'a']);
    });
  });

  it('updates BOTH unfiltered and kind-filtered cache entries in a single mutation (organizer switches views mid-session)', async () => {
    const { client, wrapper } = makeWrapperAndClient();
    const initialList = makeList([
      makeItem('a', 'Rice', 0),
      makeItem('b', 'Curry', 1),
      makeItem('c', 'Salad', 2),
    ]);

    const unfilteredKey = signUpKeys.list(eventId);
    const itemsKey = signUpKeys.list(eventId, SignUpKind.Items);
    const volunteersKey = signUpKeys.list(eventId, SignUpKind.Volunteers);

    // Seed all three cache entries — realistic mid-session state when a user
    // has navigated across the manage tabs and each view cached its own data.
    client.setQueryData<SignUpListDto[]>(unfilteredKey, [initialList]);
    client.setQueryData<SignUpListDto[]>(itemsKey, [initialList]);
    client.setQueryData<SignUpListDto[]>(volunteersKey, [initialList]);

    vi.mocked(eventsRepository.reorderSignUpItems).mockImplementation(
      () => new Promise(() => {})
    );

    const { result } = renderHook(() => useReorderSignUpItems(), { wrapper });

    act(() => {
      result.current.mutate({
        eventId,
        signupId,
        orderedItemIds: ['c', 'a', 'b'],
      });
    });

    await waitFor(() => {
      expect(
        client.getQueryData<SignUpListDto[]>(unfilteredKey)![0].items.map((i) => i.id)
      ).toEqual(['c', 'a', 'b']);
      expect(
        client.getQueryData<SignUpListDto[]>(itemsKey)![0].items.map((i) => i.id)
      ).toEqual(['c', 'a', 'b']);
      expect(
        client.getQueryData<SignUpListDto[]>(volunteersKey)![0].items.map((i) => i.id)
      ).toEqual(['c', 'a', 'b']);
    });
  });

  it('rolls back ALL previously-updated cache entries on error (not just the unfiltered one)', async () => {
    const { client, wrapper } = makeWrapperAndClient();
    const initialItems = [
      makeItem('a', 'Rice', 0),
      makeItem('b', 'Curry', 1),
      makeItem('c', 'Salad', 2),
    ];
    const initialList = makeList(initialItems);

    const unfilteredKey = signUpKeys.list(eventId);
    const itemsKey = signUpKeys.list(eventId, SignUpKind.Items);
    client.setQueryData<SignUpListDto[]>(unfilteredKey, [initialList]);
    client.setQueryData<SignUpListDto[]>(itemsKey, [initialList]);

    vi.mocked(eventsRepository.reorderSignUpItems).mockRejectedValueOnce(
      new Error('server said no')
    );

    const { result } = renderHook(() => useReorderSignUpItems(), { wrapper });

    act(() => {
      result.current.mutate({
        eventId,
        signupId,
        orderedItemIds: ['c', 'b', 'a'],
      });
    });

    await waitFor(() => expect(result.current.isError).toBe(true));

    // Both cache entries must be restored to the original order.
    const unfiltered = client.getQueryData<SignUpListDto[]>(unfilteredKey);
    const itemsCached = client.getQueryData<SignUpListDto[]>(itemsKey);
    expect(unfiltered![0].items.map((i) => i.id)).toEqual(['a', 'b', 'c']);
    expect(itemsCached![0].items.map((i) => i.id)).toEqual(['a', 'b', 'c']);
  });
});
