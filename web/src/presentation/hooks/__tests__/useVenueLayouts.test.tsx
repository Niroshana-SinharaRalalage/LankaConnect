/**
 * Slice 5 Chunk 11 — useVenueLayouts mutation hook tests.
 *
 * Focus: verifying each mutation hook (a) calls through to the repository with
 * the expected arguments, and (b) invalidates the correct set of query caches
 * on success. The repository is fully mocked — these are wiring tests, not
 * end-to-end network tests (staging smoke scripts cover that path).
 */

import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

vi.mock('@/infrastructure/api/repositories/venue-layouts.repository', () => ({
  venueLayoutsRepository: {
    updateLayout: vi.fn(),
    deleteLayout: vi.fn(),
    batchUpdateLayout: vi.fn(),
    updateZone: vi.fn(),
    deleteZone: vi.fn(),
    addTable: vi.fn(),
    updateTable: vi.fn(),
    deleteTable: vi.fn(),
    addDecoration: vi.fn(),
    updateDecoration: vi.fn(),
    deleteDecoration: vi.fn(),
    assignTier: vi.fn(),
    removeTierAssignment: vi.fn(),
    // Other methods used by peers we don't touch — stubs to keep the import shape intact.
    createLayout: vi.fn(),
    getLayout: vi.fn(),
    getLayoutByEvent: vi.fn(),
    generateSeats: vi.fn(),
    assignLayoutToEvent: vi.fn(),
    getSeatAvailability: vi.fn(),
    holdSeats: vi.fn(),
    releaseSeats: vi.fn(),
  },
}));

import { venueLayoutsRepository } from '@/infrastructure/api/repositories/venue-layouts.repository';
import {
  venueLayoutKeys,
  useUpdateVenueLayout,
  useDeleteVenueLayout,
  useBatchUpdateVenueLayout,
  useUpdateZone,
  useDeleteZone,
  useAddTable,
  useUpdateTable,
  useDeleteTable,
  useAddDecoration,
  useUpdateDecoration,
  useDeleteDecoration,
  useAssignTier,
  useRemoveTierAssignment,
} from '../useVenueLayouts';
import { eventKeys } from '../useEvents';
import { AssignableKind } from '@/infrastructure/api/types/events.types';

const LAYOUT_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const EVENT_ID = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const ZONE_ID = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
const TABLE_ID = 'dddddddd-dddd-dddd-dddd-dddddddddddd';
const DECORATION_ID = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
const TIER_ID = 'ffffffff-ffff-ffff-ffff-ffffffffffff';
const ASSIGNABLE_ID = '11111111-1111-1111-1111-111111111111';
const ROW_VERSION = 7;

function makeWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
  const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');
  const removeSpy = vi.spyOn(queryClient, 'removeQueries');

  const wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );

  return { wrapper, invalidateSpy, removeSpy, queryClient };
}

function expectKey(
  invalidateSpy: ReturnType<typeof vi.spyOn>,
  key: readonly unknown[]
) {
  expect(invalidateSpy).toHaveBeenCalledWith(
    expect.objectContaining({ queryKey: key })
  );
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('useUpdateVenueLayout', () => {
  it('calls updateLayout and invalidates detail+byEvent', async () => {
    (venueLayoutsRepository.updateLayout as any).mockResolvedValueOnce(undefined);
    const { wrapper, invalidateSpy } = makeWrapper();

    const { result } = renderHook(() => useUpdateVenueLayout(LAYOUT_ID, EVENT_ID), {
      wrapper,
    });

    await act(async () => {
      await result.current.mutateAsync({
        rowVersion: ROW_VERSION,
        request: { name: 'New name' },
      });
    });

    expect(venueLayoutsRepository.updateLayout).toHaveBeenCalledWith(
      LAYOUT_ID,
      ROW_VERSION,
      { name: 'New name' }
    );

    await waitFor(() => {
      expectKey(invalidateSpy, venueLayoutKeys.detail(LAYOUT_ID));
      expectKey(invalidateSpy, venueLayoutKeys.byEvent(EVENT_ID));
    });
  });

  it('skips byEvent invalidation when layout has no event', async () => {
    (venueLayoutsRepository.updateLayout as any).mockResolvedValueOnce(undefined);
    const { wrapper, invalidateSpy } = makeWrapper();

    const { result } = renderHook(() => useUpdateVenueLayout(LAYOUT_ID, null), {
      wrapper,
    });

    await act(async () => {
      await result.current.mutateAsync({
        rowVersion: ROW_VERSION,
        request: { name: 'X' },
      });
    });

    await waitFor(() => {
      expectKey(invalidateSpy, venueLayoutKeys.detail(LAYOUT_ID));
    });
    // byEvent / seatAvailability must NOT be touched for template layouts.
    const byEventKey = venueLayoutKeys.byEvent(EVENT_ID);
    expect(
      invalidateSpy.mock.calls.some(
        ([arg]) =>
          Array.isArray((arg as any)?.queryKey) &&
          JSON.stringify((arg as any).queryKey) === JSON.stringify(byEventKey)
      )
    ).toBe(false);
  });
});

describe('useDeleteVenueLayout', () => {
  it('removes detail cache and invalidates event + seatAvailability + event-detail', async () => {
    (venueLayoutsRepository.deleteLayout as any).mockResolvedValueOnce(undefined);
    const { wrapper, invalidateSpy, removeSpy } = makeWrapper();

    const { result } = renderHook(() => useDeleteVenueLayout(LAYOUT_ID, EVENT_ID), {
      wrapper,
    });

    await act(async () => {
      await result.current.mutateAsync({ rowVersion: ROW_VERSION });
    });

    expect(venueLayoutsRepository.deleteLayout).toHaveBeenCalledWith(
      LAYOUT_ID,
      ROW_VERSION
    );

    await waitFor(() => {
      expect(removeSpy).toHaveBeenCalledWith(
        expect.objectContaining({ queryKey: venueLayoutKeys.detail(LAYOUT_ID) })
      );
      expectKey(invalidateSpy, venueLayoutKeys.byEvent(EVENT_ID));
      expectKey(invalidateSpy, venueLayoutKeys.seatAvailability(EVENT_ID));
      expectKey(invalidateSpy, eventKeys.detail(EVENT_ID));
    });
  });
});

describe('useBatchUpdateVenueLayout', () => {
  it('invalidates detail+byEvent+seatAvailability (batch can regenerate seats)', async () => {
    (venueLayoutsRepository.batchUpdateLayout as any).mockResolvedValueOnce(undefined);
    const { wrapper, invalidateSpy } = makeWrapper();

    const { result } = renderHook(
      () => useBatchUpdateVenueLayout(LAYOUT_ID, EVENT_ID),
      { wrapper }
    );

    await act(async () => {
      await result.current.mutateAsync({
        rowVersion: ROW_VERSION,
        payload: { name: 'X', zones: [] },
      });
    });

    await waitFor(() => {
      expectKey(invalidateSpy, venueLayoutKeys.detail(LAYOUT_ID));
      expectKey(invalidateSpy, venueLayoutKeys.byEvent(EVENT_ID));
      expectKey(invalidateSpy, venueLayoutKeys.seatAvailability(EVENT_ID));
    });
  });
});

describe('useUpdateZone / useDeleteZone', () => {
  it('useUpdateZone forwards zoneId+rowVersion+request', async () => {
    (venueLayoutsRepository.updateZone as any).mockResolvedValueOnce(undefined);
    const { wrapper, invalidateSpy } = makeWrapper();

    const { result } = renderHook(() => useUpdateZone(LAYOUT_ID, EVENT_ID), {
      wrapper,
    });

    await act(async () => {
      await result.current.mutateAsync({
        zoneId: ZONE_ID,
        rowVersion: ROW_VERSION,
        request: { name: 'Front' },
      });
    });

    expect(venueLayoutsRepository.updateZone).toHaveBeenCalledWith(
      LAYOUT_ID,
      ZONE_ID,
      ROW_VERSION,
      { name: 'Front' }
    );

    await waitFor(() => {
      expectKey(invalidateSpy, venueLayoutKeys.detail(LAYOUT_ID));
    });
  });

  it('useDeleteZone invalidates seatAvailability because cascade removes seats', async () => {
    (venueLayoutsRepository.deleteZone as any).mockResolvedValueOnce(undefined);
    const { wrapper, invalidateSpy } = makeWrapper();

    const { result } = renderHook(() => useDeleteZone(LAYOUT_ID, EVENT_ID), {
      wrapper,
    });

    await act(async () => {
      await result.current.mutateAsync({
        zoneId: ZONE_ID,
        rowVersion: ROW_VERSION,
      });
    });

    await waitFor(() => {
      expectKey(invalidateSpy, venueLayoutKeys.seatAvailability(EVENT_ID));
    });
  });
});

describe('useAddTable / useUpdateTable / useDeleteTable', () => {
  it('useAddTable forwards request and returns AddTableResponse', async () => {
    (venueLayoutsRepository.addTable as any).mockResolvedValueOnce({
      tableId: TABLE_ID,
    });
    const { wrapper, invalidateSpy } = makeWrapper();

    const { result } = renderHook(() => useAddTable(LAYOUT_ID, EVENT_ID), {
      wrapper,
    });

    let response: { tableId: string } | undefined;
    await act(async () => {
      response = await result.current.mutateAsync({
        rowVersion: ROW_VERSION,
        request: { label: 'T1', shape: 'Round', capacity: 8, sortOrder: 1 },
      });
    });

    expect(response?.tableId).toBe(TABLE_ID);
    expect(venueLayoutsRepository.addTable).toHaveBeenCalledWith(
      LAYOUT_ID,
      ROW_VERSION,
      expect.objectContaining({ label: 'T1' })
    );
    await waitFor(() =>
      expectKey(invalidateSpy, venueLayoutKeys.seatAvailability(EVENT_ID))
    );
  });

  it('useUpdateTable forwards tableId+rowVersion+request', async () => {
    (venueLayoutsRepository.updateTable as any).mockResolvedValueOnce(undefined);
    const { wrapper } = makeWrapper();

    const { result } = renderHook(() => useUpdateTable(LAYOUT_ID), { wrapper });

    await act(async () => {
      await result.current.mutateAsync({
        tableId: TABLE_ID,
        rowVersion: ROW_VERSION,
        request: { capacity: 10 },
      });
    });

    expect(venueLayoutsRepository.updateTable).toHaveBeenCalledWith(
      LAYOUT_ID,
      TABLE_ID,
      ROW_VERSION,
      { capacity: 10 }
    );
  });

  it('useDeleteTable forwards tableId+rowVersion', async () => {
    (venueLayoutsRepository.deleteTable as any).mockResolvedValueOnce(undefined);
    const { wrapper } = makeWrapper();

    const { result } = renderHook(() => useDeleteTable(LAYOUT_ID), { wrapper });

    await act(async () => {
      await result.current.mutateAsync({
        tableId: TABLE_ID,
        rowVersion: ROW_VERSION,
      });
    });

    expect(venueLayoutsRepository.deleteTable).toHaveBeenCalledWith(
      LAYOUT_ID,
      TABLE_ID,
      ROW_VERSION
    );
  });
});

describe('useAddDecoration / useUpdateDecoration / useDeleteDecoration', () => {
  it('useAddDecoration returns AddDecorationResponse and invalidates detail', async () => {
    (venueLayoutsRepository.addDecoration as any).mockResolvedValueOnce({
      decorationId: DECORATION_ID,
    });
    const { wrapper, invalidateSpy } = makeWrapper();

    const { result } = renderHook(() => useAddDecoration(LAYOUT_ID, EVENT_ID), {
      wrapper,
    });

    let response: { decorationId: string } | undefined;
    await act(async () => {
      response = await result.current.mutateAsync({
        rowVersion: ROW_VERSION,
        request: { kind: 'Stage', sortOrder: 1 },
      });
    });

    expect(response?.decorationId).toBe(DECORATION_ID);
    await waitFor(() =>
      expectKey(invalidateSpy, venueLayoutKeys.detail(LAYOUT_ID))
    );
  });

  it('useUpdateDecoration forwards decorationId+rowVersion+request', async () => {
    (venueLayoutsRepository.updateDecoration as any).mockResolvedValueOnce(
      undefined
    );
    const { wrapper } = makeWrapper();

    const { result } = renderHook(() => useUpdateDecoration(LAYOUT_ID), {
      wrapper,
    });

    await act(async () => {
      await result.current.mutateAsync({
        decorationId: DECORATION_ID,
        rowVersion: ROW_VERSION,
        request: { label: 'Main Stage' },
      });
    });

    expect(venueLayoutsRepository.updateDecoration).toHaveBeenCalledWith(
      LAYOUT_ID,
      DECORATION_ID,
      ROW_VERSION,
      { label: 'Main Stage' }
    );
  });

  it('useDeleteDecoration forwards decorationId+rowVersion', async () => {
    (venueLayoutsRepository.deleteDecoration as any).mockResolvedValueOnce(
      undefined
    );
    const { wrapper } = makeWrapper();

    const { result } = renderHook(() => useDeleteDecoration(LAYOUT_ID), {
      wrapper,
    });

    await act(async () => {
      await result.current.mutateAsync({
        decorationId: DECORATION_ID,
        rowVersion: ROW_VERSION,
      });
    });

    expect(venueLayoutsRepository.deleteDecoration).toHaveBeenCalledWith(
      LAYOUT_ID,
      DECORATION_ID,
      ROW_VERSION
    );
  });
});

describe('useAssignTier / useRemoveTierAssignment', () => {
  it('useAssignTier forwards request', async () => {
    (venueLayoutsRepository.assignTier as any).mockResolvedValueOnce(undefined);
    const { wrapper, invalidateSpy } = makeWrapper();

    const { result } = renderHook(() => useAssignTier(LAYOUT_ID, EVENT_ID), {
      wrapper,
    });

    await act(async () => {
      await result.current.mutateAsync({
        rowVersion: ROW_VERSION,
        request: {
          tierId: TIER_ID,
          kind: AssignableKind.Zone,
          assignableId: ASSIGNABLE_ID,
        },
      });
    });

    expect(venueLayoutsRepository.assignTier).toHaveBeenCalledWith(
      LAYOUT_ID,
      ROW_VERSION,
      expect.objectContaining({ tierId: TIER_ID, kind: 'Zone' })
    );
    await waitFor(() =>
      expectKey(invalidateSpy, venueLayoutKeys.detail(LAYOUT_ID))
    );
  });

  it('useRemoveTierAssignment forwards composite key', async () => {
    (venueLayoutsRepository.removeTierAssignment as any).mockResolvedValueOnce(
      undefined
    );
    const { wrapper } = makeWrapper();

    const { result } = renderHook(() => useRemoveTierAssignment(LAYOUT_ID), {
      wrapper,
    });

    await act(async () => {
      await result.current.mutateAsync({
        tierId: TIER_ID,
        kind: AssignableKind.Table,
        assignableId: ASSIGNABLE_ID,
        rowVersion: ROW_VERSION,
      });
    });

    expect(venueLayoutsRepository.removeTierAssignment).toHaveBeenCalledWith(
      LAYOUT_ID,
      TIER_ID,
      AssignableKind.Table,
      ASSIGNABLE_ID,
      ROW_VERSION
    );
  });
});

describe('Error propagation', () => {
  it('surfaces repository rejection to mutateAsync', async () => {
    (venueLayoutsRepository.updateLayout as any).mockRejectedValueOnce(
      new Error('409 Conflict')
    );
    const { wrapper } = makeWrapper();

    const { result } = renderHook(() => useUpdateVenueLayout(LAYOUT_ID, EVENT_ID), {
      wrapper,
    });

    await expect(
      result.current.mutateAsync({
        rowVersion: ROW_VERSION,
        request: { name: 'X' },
      })
    ).rejects.toThrow('409 Conflict');
  });
});
