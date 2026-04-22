/**
 * Slice 5 Chunk 11 — venue-layouts.repository unit tests.
 *
 * Focus: verifying URL construction + `If-Match` header plumbing on every
 * mutating endpoint added in Chunks 4–10. The underlying `apiClient` is fully
 * mocked so these tests don't touch the network; they assert the repository
 * wires rowVersion into `If-Match` exactly once per call.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';

vi.mock('../../client/api-client', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  },
}));

import { apiClient } from '../../client/api-client';
import { venueLayoutsRepository } from '../venue-layouts.repository';
import { AssignableKind } from '../../types/events.types';

const mockGet = apiClient.get as ReturnType<typeof vi.fn>;
const mockPost = apiClient.post as ReturnType<typeof vi.fn>;
const mockPut = apiClient.put as ReturnType<typeof vi.fn>;
const mockPatch = apiClient.patch as ReturnType<typeof vi.fn>;
const mockDelete = apiClient.delete as ReturnType<typeof vi.fn>;

const LAYOUT_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const ZONE_ID = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const TABLE_ID = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
const DECORATION_ID = 'dddddddd-dddd-dddd-dddd-dddddddddddd';
const TIER_ID = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
const ASSIGNABLE_ID = 'ffffffff-ffff-ffff-ffff-ffffffffffff';
const ROW_VERSION = 42;

const ifMatchHeader = { headers: { 'If-Match': '42' } };

beforeEach(() => {
  vi.clearAllMocks();
});

describe('VenueLayoutsRepository — Slice 5 layout-level mutations', () => {
  it('updateLayout sends PUT with If-Match header', async () => {
    mockPut.mockResolvedValueOnce(undefined);

    await venueLayoutsRepository.updateLayout(LAYOUT_ID, ROW_VERSION, {
      name: 'New name',
    });

    expect(mockPut).toHaveBeenCalledWith(
      `/venue-layouts/${LAYOUT_ID}`,
      { name: 'New name' },
      ifMatchHeader
    );
  });

  it('deleteLayout sends DELETE with If-Match header', async () => {
    mockDelete.mockResolvedValueOnce(undefined);

    await venueLayoutsRepository.deleteLayout(LAYOUT_ID, ROW_VERSION);

    expect(mockDelete).toHaveBeenCalledWith(
      `/venue-layouts/${LAYOUT_ID}`,
      ifMatchHeader
    );
  });

  it('batchUpdateLayout sends PUT /batch with If-Match header', async () => {
    mockPut.mockResolvedValueOnce(undefined);
    const payload = { name: 'Updated', zones: [], tables: [], decorations: [] };

    await venueLayoutsRepository.batchUpdateLayout(LAYOUT_ID, ROW_VERSION, payload);

    expect(mockPut).toHaveBeenCalledWith(
      `/venue-layouts/${LAYOUT_ID}/batch`,
      payload,
      ifMatchHeader
    );
  });
});

describe('VenueLayoutsRepository — zone mutations', () => {
  it('updateZone sends PATCH with If-Match', async () => {
    mockPatch.mockResolvedValueOnce(undefined);

    await venueLayoutsRepository.updateZone(LAYOUT_ID, ZONE_ID, ROW_VERSION, {
      name: 'Front',
    });

    expect(mockPatch).toHaveBeenCalledWith(
      `/venue-layouts/${LAYOUT_ID}/zones/${ZONE_ID}`,
      { name: 'Front' },
      ifMatchHeader
    );
  });

  it('deleteZone sends DELETE with If-Match', async () => {
    mockDelete.mockResolvedValueOnce(undefined);

    await venueLayoutsRepository.deleteZone(LAYOUT_ID, ZONE_ID, ROW_VERSION);

    expect(mockDelete).toHaveBeenCalledWith(
      `/venue-layouts/${LAYOUT_ID}/zones/${ZONE_ID}`,
      ifMatchHeader
    );
  });
});

describe('VenueLayoutsRepository — table mutations', () => {
  it('addTable sends POST and returns AddTableResponse', async () => {
    mockPost.mockResolvedValueOnce({ tableId: TABLE_ID });

    const result = await venueLayoutsRepository.addTable(LAYOUT_ID, ROW_VERSION, {
      label: 'T1',
      shape: 'Round',
      capacity: 8,
      sortOrder: 1,
    });

    expect(result.tableId).toBe(TABLE_ID);
    expect(mockPost).toHaveBeenCalledWith(
      `/venue-layouts/${LAYOUT_ID}/tables`,
      expect.objectContaining({ label: 'T1', capacity: 8 }),
      ifMatchHeader
    );
  });

  it('updateTable sends PATCH', async () => {
    mockPatch.mockResolvedValueOnce(undefined);

    await venueLayoutsRepository.updateTable(LAYOUT_ID, TABLE_ID, ROW_VERSION, {
      capacity: 10,
    });

    expect(mockPatch).toHaveBeenCalledWith(
      `/venue-layouts/${LAYOUT_ID}/tables/${TABLE_ID}`,
      { capacity: 10 },
      ifMatchHeader
    );
  });

  it('deleteTable sends DELETE', async () => {
    mockDelete.mockResolvedValueOnce(undefined);

    await venueLayoutsRepository.deleteTable(LAYOUT_ID, TABLE_ID, ROW_VERSION);

    expect(mockDelete).toHaveBeenCalledWith(
      `/venue-layouts/${LAYOUT_ID}/tables/${TABLE_ID}`,
      ifMatchHeader
    );
  });
});

describe('VenueLayoutsRepository — decoration mutations', () => {
  it('addDecoration sends POST and returns AddDecorationResponse', async () => {
    mockPost.mockResolvedValueOnce({ decorationId: DECORATION_ID });

    const result = await venueLayoutsRepository.addDecoration(LAYOUT_ID, ROW_VERSION, {
      kind: 'Stage',
      sortOrder: 1,
    });

    expect(result.decorationId).toBe(DECORATION_ID);
    expect(mockPost).toHaveBeenCalledWith(
      `/venue-layouts/${LAYOUT_ID}/decorations`,
      expect.objectContaining({ kind: 'Stage' }),
      ifMatchHeader
    );
  });

  it('updateDecoration sends PATCH', async () => {
    mockPatch.mockResolvedValueOnce(undefined);

    await venueLayoutsRepository.updateDecoration(
      LAYOUT_ID,
      DECORATION_ID,
      ROW_VERSION,
      { label: 'Main Stage' }
    );

    expect(mockPatch).toHaveBeenCalledWith(
      `/venue-layouts/${LAYOUT_ID}/decorations/${DECORATION_ID}`,
      { label: 'Main Stage' },
      ifMatchHeader
    );
  });

  it('deleteDecoration sends DELETE', async () => {
    mockDelete.mockResolvedValueOnce(undefined);

    await venueLayoutsRepository.deleteDecoration(
      LAYOUT_ID,
      DECORATION_ID,
      ROW_VERSION
    );

    expect(mockDelete).toHaveBeenCalledWith(
      `/venue-layouts/${LAYOUT_ID}/decorations/${DECORATION_ID}`,
      ifMatchHeader
    );
  });
});

describe('VenueLayoutsRepository — tier assignments', () => {
  it('assignTier sends POST', async () => {
    mockPost.mockResolvedValueOnce(undefined);

    await venueLayoutsRepository.assignTier(LAYOUT_ID, ROW_VERSION, {
      tierId: TIER_ID,
      kind: AssignableKind.Zone,
      assignableId: ASSIGNABLE_ID,
    });

    expect(mockPost).toHaveBeenCalledWith(
      `/venue-layouts/${LAYOUT_ID}/tier-assignments`,
      expect.objectContaining({ tierId: TIER_ID, kind: 'Zone' }),
      ifMatchHeader
    );
  });

  it('removeTierAssignment sends DELETE with full composite path', async () => {
    mockDelete.mockResolvedValueOnce(undefined);

    await venueLayoutsRepository.removeTierAssignment(
      LAYOUT_ID,
      TIER_ID,
      AssignableKind.Table,
      ASSIGNABLE_ID,
      ROW_VERSION
    );

    expect(mockDelete).toHaveBeenCalledWith(
      `/venue-layouts/${LAYOUT_ID}/tier-assignments/${TIER_ID}/Table/${ASSIGNABLE_ID}`,
      ifMatchHeader
    );
  });
});

describe('VenueLayoutsRepository — If-Match header sanity', () => {
  it('stringifies large rowVersion values correctly', async () => {
    mockPut.mockResolvedValueOnce(undefined);
    const largeRowVersion = 2147483647; // int max

    await venueLayoutsRepository.updateLayout(LAYOUT_ID, largeRowVersion, {
      name: 'X',
    });

    expect(mockPut).toHaveBeenCalledWith(
      expect.any(String),
      expect.any(Object),
      { headers: { 'If-Match': '2147483647' } }
    );
  });

  it('does not swallow errors from the underlying client', async () => {
    mockPut.mockRejectedValueOnce(new Error('409 Conflict'));

    await expect(
      venueLayoutsRepository.updateLayout(LAYOUT_ID, ROW_VERSION, { name: 'X' })
    ).rejects.toThrow('409 Conflict');
  });

  it('read endpoints (no If-Match) still work — getLayout', async () => {
    mockGet.mockResolvedValueOnce({ id: LAYOUT_ID });

    await venueLayoutsRepository.getLayout(LAYOUT_ID);

    expect(mockGet).toHaveBeenCalledWith(`/venue-layouts/${LAYOUT_ID}`);
  });
});

describe('VenueLayoutsRepository — Slice 6 preset library', () => {
  it('listPresets GETs /venue-layouts/presets without headers', async () => {
    mockGet.mockResolvedValueOnce([{ id: 'theater-classic' }]);

    const presets = await venueLayoutsRepository.listPresets();

    expect(mockGet).toHaveBeenCalledWith('/venue-layouts/presets');
    expect(presets).toEqual([{ id: 'theater-classic' }]);
  });

  it('createFromPreset POSTs /venue-layouts/from-preset with the raw request body', async () => {
    mockPost.mockResolvedValueOnce({ id: LAYOUT_ID });

    await venueLayoutsRepository.createFromPreset({
      presetId: 'banquet-round-8',
      eventId: null,
    });

    expect(mockPost).toHaveBeenCalledWith(
      '/venue-layouts/from-preset',
      { presetId: 'banquet-round-8', eventId: null },
    );
  });

  it('createFromPreset passes eventId through when attaching to an event', async () => {
    mockPost.mockResolvedValueOnce({ id: LAYOUT_ID });

    await venueLayoutsRepository.createFromPreset({
      presetId: 'theater-classic',
      eventId: 'event-123',
    });

    expect(mockPost).toHaveBeenCalledWith(
      '/venue-layouts/from-preset',
      { presetId: 'theater-classic', eventId: 'event-123' },
    );
  });

  it('createFromPreset propagates API errors', async () => {
    mockPost.mockRejectedValueOnce(new Error('404 Not Found'));

    await expect(
      venueLayoutsRepository.createFromPreset({ presetId: 'bogus' }),
    ).rejects.toThrow('404 Not Found');
  });
});
