import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';

import { signUpKeys, useEventSignUps } from '@/presentation/hooks/useEventSignUps';
import { SignUpKind } from '@/infrastructure/api/types/events.types';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';

/**
 * Phase 7D.1 Step 19: kind-filtered useEventSignUps + separate query keys.
 *
 * These tests pin two behaviours:
 *   1. Query keys differ between `Items`, `Volunteers`, and the unfiltered
 *      default so the React Query cache never cross-pollutes the three sets.
 *   2. The hook forwards the kind through to the repository layer, which
 *      in turn attaches `?kind=...` to the API request (covered in the
 *      repository's own unit test).
 */
vi.mock('@/infrastructure/api/repositories/events.repository', () => ({
  eventsRepository: {
    getEventSignUpLists: vi.fn(),
  },
}));

function makeWrapper() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return ({ children }: { children: React.ReactNode }) =>
    React.createElement(QueryClientProvider, { client }, children);
}

describe('signUpKeys.list', () => {
  it('returns distinct keys for Items, Volunteers, and the unfiltered default', () => {
    const eventId = '4378a7d9-280e-4322-9ca2-a17e27061ae8';

    const defaultKey = signUpKeys.list(eventId);
    const itemsKey = signUpKeys.list(eventId, SignUpKind.Items);
    const volunteersKey = signUpKeys.list(eventId, SignUpKind.Volunteers);

    expect(defaultKey).not.toEqual(itemsKey);
    expect(itemsKey).not.toEqual(volunteersKey);
    expect(defaultKey).not.toEqual(volunteersKey);
  });

  it('encodes kind into the key so cache keys serialize deterministically', () => {
    const key = signUpKeys.list('evt-1', SignUpKind.Volunteers);
    expect(key).toContain('evt-1');
    expect(JSON.stringify(key)).toContain('Volunteers');
  });
});

describe('useEventSignUps — Phase 7D.1 kind filter', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('calls the repository without a kind when kind is omitted (legacy behaviour preserved)', async () => {
    vi.mocked(eventsRepository.getEventSignUpLists).mockResolvedValueOnce([]);

    const { result } = renderHook(() => useEventSignUps('evt-legacy'), {
      wrapper: makeWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(eventsRepository.getEventSignUpLists).toHaveBeenCalledWith('evt-legacy', undefined);
  });

  it('forwards SignUpKind.Volunteers when provided as the second positional arg', async () => {
    vi.mocked(eventsRepository.getEventSignUpLists).mockResolvedValueOnce([]);

    const { result } = renderHook(() => useEventSignUps('evt-vol', SignUpKind.Volunteers), {
      wrapper: makeWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(eventsRepository.getEventSignUpLists).toHaveBeenCalledWith(
      'evt-vol',
      SignUpKind.Volunteers
    );
  });

  it('accepts UseQueryOptions in the legacy second-arg slot when no kind is supplied', async () => {
    vi.mocked(eventsRepository.getEventSignUpLists).mockResolvedValueOnce([]);

    const { result } = renderHook(
      () => useEventSignUps('evt-opts', { staleTime: 0 }),
      { wrapper: makeWrapper() }
    );

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(eventsRepository.getEventSignUpLists).toHaveBeenCalledWith('evt-opts', undefined);
  });
});
