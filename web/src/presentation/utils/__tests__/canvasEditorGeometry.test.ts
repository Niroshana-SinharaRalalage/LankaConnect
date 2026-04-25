/**
 * Slice 8 S8.3 — pure-function tests for the canvas editor's drag-and-snap math.
 * Covers snap-to-grid, item-center computation across all shape kinds,
 * applying a new center back into the geometry JSON, and the draft-override
 * resolution helper that CanvasEditorStage uses to merge in-progress moves
 * on top of the persisted layout.
 */

import { describe, it, expect } from 'vitest';

import {
  EDITOR_GRID,
  EDITOR_ROTATION_STEP,
  MIN_SHAPE_DIMENSION,
  snapToGrid,
  snapRotation,
  refKey,
  itemCenter,
  applyDragToGeometry,
  applyResizeToGeometry,
  applyRadiusToGeometry,
  applyRotationToGeometry,
  readGeometryDimensions,
  resolveGeometry,
  resolveTierAssignments,
  toggleTierAssignment,
  collectItemCenters,
  generateClientId,
  nextZoneColor,
  createZoneDraft,
  createRoundTableDraft,
  createRectTableDraft,
  createDecorationDraft,
  composeBatchPayload,
  countDraftChanges,
  type CanvasEditorDraftState,
} from '../canvasEditorGeometry';
import {
  TableShape,
  ZoneShape,
  DecorationKind,
  type VenueLayoutDto,
  type VenueZoneDto,
  type VenueTableDto,
  type VenueDecorationDto,
} from '@/infrastructure/api/types/events.types';

describe('snapToGrid', () => {
  it('uses 50px as the default grid', () => {
    expect(EDITOR_GRID).toBe(50);
    expect(snapToGrid(23)).toBe(0);
    expect(snapToGrid(27)).toBe(50);
    expect(snapToGrid(99)).toBe(100);
  });

  it('snaps exact multiples unchanged', () => {
    expect(snapToGrid(100)).toBe(100);
    expect(snapToGrid(0)).toBe(0);
    expect(snapToGrid(-50)).toBe(-50);
  });

  it('accepts a custom grid size', () => {
    expect(snapToGrid(17, 10)).toBe(20);
    expect(snapToGrid(17, 5)).toBe(15);
  });
});

describe('refKey', () => {
  it('produces stable string keys across kinds', () => {
    expect(refKey({ kind: 'zone', id: 'abc' })).toBe('zone:abc');
    expect(refKey({ kind: 'table', id: 'xyz' })).toBe('table:xyz');
    expect(refKey({ kind: 'decoration', id: '123' })).toBe('decoration:123');
  });
});

describe('itemCenter', () => {
  it('computes center of a rect zone as top-left + half dimensions', () => {
    const geom = JSON.stringify({ x: 100, y: 200, width: 400, height: 300 });
    expect(itemCenter('zone', geom, ZoneShape.Rect)).toEqual({ x: 300, y: 350 });
  });

  it('uses center directly for curve zones', () => {
    const geom = JSON.stringify({
      centerX: 500,
      centerY: 600,
      radius: 150,
      startAngleDeg: 180,
      sweepAngleDeg: 180,
    });
    expect(itemCenter('zone', geom, ZoneShape.Curve)).toEqual({ x: 500, y: 600 });
  });

  it('uses centerX/centerY for round tables', () => {
    const geom = JSON.stringify({ centerX: 400, centerY: 400, radius: 40 });
    expect(itemCenter('table', geom, TableShape.Round)).toEqual({ x: 400, y: 400 });
  });

  it('uses centerX/centerY for rect tables', () => {
    const geom = JSON.stringify({ centerX: 300, centerY: 300, width: 120, height: 60 });
    expect(itemCenter('table', geom, TableShape.Rect)).toEqual({ x: 300, y: 300 });
  });

  it('computes center of a rect decoration as top-left + half dimensions', () => {
    const geom = JSON.stringify({ x: 0, y: 0, width: 200, height: 100 });
    expect(itemCenter('decoration', geom)).toEqual({ x: 100, y: 50 });
  });

  it('returns null for malformed geometry', () => {
    expect(itemCenter('zone', 'not-json', ZoneShape.Rect)).toBeNull();
    expect(itemCenter('table', null, TableShape.Round)).toBeNull();
    expect(itemCenter('decoration', undefined)).toBeNull();
  });
});

describe('applyDragToGeometry', () => {
  it('rect zone: new x/y derived from center preserving width/height/rotation', () => {
    const before = JSON.stringify({ x: 100, y: 100, width: 200, height: 100, rotation: 15 });
    const after = applyDragToGeometry('zone', before, { x: 500, y: 500 }, ZoneShape.Rect);
    expect(after).not.toBeNull();
    expect(JSON.parse(after!)).toEqual({
      x: 400, // 500 - 200/2
      y: 450, // 500 - 100/2
      width: 200,
      height: 100,
      rotation: 15,
    });
  });

  it('curve zone: center fields replaced, radius and arc preserved', () => {
    const before = JSON.stringify({
      centerX: 100,
      centerY: 100,
      radius: 150,
      startAngleDeg: 180,
      sweepAngleDeg: 180,
    });
    const after = applyDragToGeometry('zone', before, { x: 900, y: 800 }, ZoneShape.Curve);
    expect(JSON.parse(after!)).toEqual({
      centerX: 900,
      centerY: 800,
      radius: 150,
      startAngleDeg: 180,
      sweepAngleDeg: 180,
    });
  });

  it('round table: centerX/Y replaced, radius preserved', () => {
    const before = JSON.stringify({ centerX: 100, centerY: 200, radius: 50 });
    const after = applyDragToGeometry('table', before, { x: 300, y: 300 }, TableShape.Round);
    expect(JSON.parse(after!)).toEqual({
      centerX: 300,
      centerY: 300,
      radius: 50,
    });
  });

  it('rect table: centerX/Y replaced, width/height/rotation preserved', () => {
    const before = JSON.stringify({
      centerX: 100,
      centerY: 100,
      width: 200,
      height: 80,
      rotation: 30,
    });
    const after = applyDragToGeometry('table', before, { x: 400, y: 400 }, TableShape.Rect);
    expect(JSON.parse(after!)).toEqual({
      centerX: 400,
      centerY: 400,
      width: 200,
      height: 80,
      rotation: 30,
    });
  });

  it('decoration: same treatment as rect zone', () => {
    const before = JSON.stringify({ x: 50, y: 50, width: 100, height: 40, rotation: 0 });
    const after = applyDragToGeometry('decoration', before, { x: 500, y: 500 });
    expect(JSON.parse(after!)).toEqual({
      x: 450,
      y: 480,
      width: 100,
      height: 40,
      rotation: 0,
    });
  });

  it('returns null for malformed geometry — caller should drop the drag', () => {
    expect(applyDragToGeometry('zone', 'junk', { x: 1, y: 1 }, ZoneShape.Rect)).toBeNull();
    expect(applyDragToGeometry('table', null, { x: 1, y: 1 }, TableShape.Round)).toBeNull();
  });
});

describe('resolveGeometry', () => {
  const zone: VenueZoneDto = {
    id: 'z1',
    name: 'Zone 1',
    color: '#fff',
    sortOrder: 0,
    enabledSeatCount: 0,
    totalSeatCount: 0,
    seats: [],
    geometry: '{"original":true}',
    shape: ZoneShape.Rect,
  };

  it('falls back to the persisted geometry when no draft override exists', () => {
    expect(resolveGeometry('zone', zone, {})).toBe('{"original":true}');
  });

  it('prefers the draft override when present', () => {
    expect(resolveGeometry('zone', zone, { 'zone:z1': '{"draft":true}' })).toBe(
      '{"draft":true}',
    );
  });

  it('keys draft by kind and id so a zone and table with the same id do not collide', () => {
    const table: VenueTableDto = {
      id: 'z1', // same id as the zone above — contrived but legal
      venueLayoutId: 'L',
      label: 'T',
      shape: TableShape.Round,
      geometry: '{"tableOriginal":true}',
      capacity: 8,
      sortOrder: 0,
      enabledSeatCount: 8,
      seats: [],
    };
    const drafts = { 'zone:z1': '{"zoneDraft":true}' };
    expect(resolveGeometry('zone', zone, drafts)).toBe('{"zoneDraft":true}');
    // Same id 'z1' but `kind:table` key — no collision, falls back.
    expect(resolveGeometry('table', table, drafts)).toBe('{"tableOriginal":true}');
  });
});

describe('collectItemCenters', () => {
  it('collects centers for zones, tables, decorations honoring draft overrides', () => {
    const zones: VenueZoneDto[] = [
      {
        id: 'z1',
        name: 'Z',
        color: '#fff',
        sortOrder: 0,
        enabledSeatCount: 0,
        totalSeatCount: 0,
        seats: [],
        shape: ZoneShape.Rect,
        geometry: JSON.stringify({ x: 0, y: 0, width: 100, height: 100 }),
      },
    ];
    const tables: VenueTableDto[] = [
      {
        id: 't1',
        venueLayoutId: 'L',
        label: 'T',
        shape: TableShape.Round,
        geometry: JSON.stringify({ centerX: 300, centerY: 300, radius: 40 }),
        capacity: 8,
        sortOrder: 0,
        enabledSeatCount: 8,
        seats: [],
      },
    ];
    const decorations: VenueDecorationDto[] = [
      {
        id: 'd1',
        venueLayoutId: 'L',
        kind: DecorationKind.Stage,
        geometry: JSON.stringify({ x: 100, y: 100, width: 200, height: 50 }),
        properties: '{}',
        sortOrder: 0,
      },
    ];
    const drafts = {
      // Override the zone position: top-left (400,400), size 100x100 → center (450,450).
      'zone:z1': JSON.stringify({ x: 400, y: 400, width: 100, height: 100 }),
    };

    const centers = collectItemCenters(zones, tables, decorations, drafts);
    expect(centers).toEqual([
      { ref: { kind: 'zone', id: 'z1' }, center: { x: 450, y: 450 } },
      { ref: { kind: 'table', id: 't1' }, center: { x: 300, y: 300 } },
      { ref: { kind: 'decoration', id: 'd1' }, center: { x: 200, y: 125 } },
    ]);
  });

  it('skips items with malformed geometry — rendering surfaces the error, not center math', () => {
    const zones: VenueZoneDto[] = [
      {
        id: 'z1',
        name: 'Z',
        color: '#fff',
        sortOrder: 0,
        enabledSeatCount: 0,
        totalSeatCount: 0,
        seats: [],
        shape: ZoneShape.Rect,
        geometry: 'broken',
      },
    ];
    expect(collectItemCenters(zones, [], [], {})).toEqual([]);
  });
});

// ─────────────────────────── S8.4 resize + rotate helpers ───────────────────────────

describe('snapRotation', () => {
  it('uses a 15° default step', () => {
    expect(EDITOR_ROTATION_STEP).toBe(15);
    expect(snapRotation(7)).toBe(0);
    expect(snapRotation(8)).toBe(15);
    expect(snapRotation(24)).toBe(30);
  });

  it('normalizes into [0, 360)', () => {
    expect(snapRotation(360)).toBe(0);
    expect(snapRotation(-15)).toBe(345);
    expect(snapRotation(-1)).toBe(0); // -1 rounds to 0
    expect(snapRotation(-8)).toBe(345); // -8 rounds to -15 → 345
    expect(snapRotation(720)).toBe(0);
  });

  it('accepts a custom step', () => {
    expect(snapRotation(47, 10)).toBe(50);
    expect(snapRotation(47, 5)).toBe(45);
  });
});

describe('MIN_SHAPE_DIMENSION', () => {
  it('is declared at the grid size so resize never collapses below one cell', () => {
    expect(MIN_SHAPE_DIMENSION).toBe(50);
    expect(MIN_SHAPE_DIMENSION).toBe(EDITOR_GRID);
  });
});

describe('applyResizeToGeometry', () => {
  it('rect zone: width/height change, center + rotation preserved', () => {
    const before = JSON.stringify({
      x: 100,
      y: 100,
      width: 400,
      height: 200,
      rotation: 30,
    });
    const after = applyResizeToGeometry(
      'zone',
      before,
      { width: 200, height: 100 },
      ZoneShape.Rect,
    );
    expect(after).not.toBeNull();
    const parsed = JSON.parse(after!);
    // Center was (300, 200). New size 200x100 → top-left (200, 150).
    expect(parsed).toEqual({
      x: 200,
      y: 150,
      width: 200,
      height: 100,
      rotation: 30,
    });
  });

  it('rect table: width/height change, center + rotation preserved', () => {
    const before = JSON.stringify({
      centerX: 500,
      centerY: 300,
      width: 100,
      height: 50,
      rotation: 90,
    });
    const after = applyResizeToGeometry(
      'table',
      before,
      { width: 250, height: 100 },
      TableShape.Rect,
    );
    expect(JSON.parse(after!)).toEqual({
      centerX: 500,
      centerY: 300,
      width: 250,
      height: 100,
      rotation: 90,
    });
  });

  it('decoration: treated the same as rect zone', () => {
    const before = JSON.stringify({ x: 0, y: 0, width: 100, height: 100, rotation: 0 });
    const after = applyResizeToGeometry('decoration', before, { width: 200, height: 50 });
    expect(JSON.parse(after!)).toEqual({
      x: -50,
      y: 25,
      width: 200,
      height: 50,
      rotation: 0,
    });
  });

  it('round table: resize via width/height is rejected — must use radius helper', () => {
    const before = JSON.stringify({ centerX: 100, centerY: 100, radius: 40 });
    expect(
      applyResizeToGeometry('table', before, { width: 100, height: 100 }, TableShape.Round),
    ).toBeNull();
  });

  it('curve zone: resize is rejected — geometry model is radius + sweep', () => {
    const before = JSON.stringify({
      centerX: 100,
      centerY: 100,
      radius: 50,
      startAngleDeg: 180,
      sweepAngleDeg: 180,
    });
    expect(
      applyResizeToGeometry('zone', before, { width: 200, height: 200 }, ZoneShape.Curve),
    ).toBeNull();
  });

  it('returns null for malformed geometry', () => {
    expect(
      applyResizeToGeometry('zone', 'junk', { width: 100, height: 100 }, ZoneShape.Rect),
    ).toBeNull();
  });
});

describe('applyRadiusToGeometry', () => {
  it('updates radius, keeps centerX/Y', () => {
    const before = JSON.stringify({ centerX: 400, centerY: 300, radius: 40 });
    const after = applyRadiusToGeometry(before, 60);
    expect(JSON.parse(after!)).toEqual({ centerX: 400, centerY: 300, radius: 60 });
  });

  it('returns null for non-round geometry', () => {
    expect(applyRadiusToGeometry('not-json', 50)).toBeNull();
  });
});

describe('applyRotationToGeometry', () => {
  it('rect zone: rotation replaced, everything else preserved', () => {
    const before = JSON.stringify({
      x: 100,
      y: 100,
      width: 400,
      height: 200,
      rotation: 0,
    });
    const after = applyRotationToGeometry('zone', before, 45, ZoneShape.Rect);
    expect(JSON.parse(after!)).toEqual({
      x: 100,
      y: 100,
      width: 400,
      height: 200,
      rotation: 45,
    });
  });

  it('rect table: rotation replaced', () => {
    const before = JSON.stringify({
      centerX: 500,
      centerY: 300,
      width: 200,
      height: 80,
      rotation: 0,
    });
    const after = applyRotationToGeometry('table', before, 90, TableShape.Rect);
    expect(JSON.parse(after!)).toEqual({
      centerX: 500,
      centerY: 300,
      width: 200,
      height: 80,
      rotation: 90,
    });
  });

  it('decoration: rotation replaced', () => {
    const before = JSON.stringify({ x: 0, y: 0, width: 100, height: 40, rotation: 0 });
    const after = applyRotationToGeometry('decoration', before, 30);
    expect(JSON.parse(after!)).toEqual({
      x: 0,
      y: 0,
      width: 100,
      height: 40,
      rotation: 30,
    });
  });

  it('curve zone: rejected — curve has no rotation field', () => {
    const before = JSON.stringify({
      centerX: 100,
      centerY: 100,
      radius: 50,
      startAngleDeg: 180,
      sweepAngleDeg: 180,
    });
    expect(applyRotationToGeometry('zone', before, 45, ZoneShape.Curve)).toBeNull();
  });

  it('round table: rejected — circular shape has no meaningful rotation', () => {
    const before = JSON.stringify({ centerX: 100, centerY: 100, radius: 40 });
    expect(applyRotationToGeometry('table', before, 45, TableShape.Round)).toBeNull();
  });
});

describe('readGeometryDimensions', () => {
  it('rect zone returns width/height/rotation', () => {
    const g = JSON.stringify({ x: 100, y: 200, width: 400, height: 300, rotation: 45 });
    expect(readGeometryDimensions('zone', g, ZoneShape.Rect)).toEqual({
      width: 400,
      height: 300,
      rotation: 45,
    });
  });

  it('rect zone without rotation defaults to 0 for display', () => {
    const g = JSON.stringify({ x: 100, y: 200, width: 400, height: 300 });
    expect(readGeometryDimensions('zone', g, ZoneShape.Rect)).toEqual({
      width: 400,
      height: 300,
      rotation: 0,
    });
  });

  it('curve zone returns only radius (width/height do not apply)', () => {
    const g = JSON.stringify({
      centerX: 500,
      centerY: 500,
      radius: 200,
      startAngleDeg: 180,
      sweepAngleDeg: 180,
    });
    expect(readGeometryDimensions('zone', g, ZoneShape.Curve)).toEqual({ radius: 200 });
  });

  it('round table returns only radius', () => {
    const g = JSON.stringify({ centerX: 400, centerY: 400, radius: 40 });
    expect(readGeometryDimensions('table', g, TableShape.Round)).toEqual({ radius: 40 });
  });

  it('rect table returns width/height/rotation', () => {
    const g = JSON.stringify({
      centerX: 300,
      centerY: 300,
      width: 200,
      height: 80,
      rotation: 90,
    });
    expect(readGeometryDimensions('table', g, TableShape.Rect)).toEqual({
      width: 200,
      height: 80,
      rotation: 90,
    });
  });

  it('decoration returns width/height/rotation', () => {
    const g = JSON.stringify({ x: 0, y: 0, width: 300, height: 60, rotation: 30 });
    expect(readGeometryDimensions('decoration', g)).toEqual({
      width: 300,
      height: 60,
      rotation: 30,
    });
  });

  it('returns null for malformed geometry', () => {
    expect(readGeometryDimensions('zone', 'junk', ZoneShape.Rect)).toBeNull();
    expect(readGeometryDimensions('table', null, TableShape.Round)).toBeNull();
  });
});

// ─────────────────────────── S8.5b draft factories ───────────────────────────

describe('generateClientId', () => {
  it('returns a non-empty unique string each call', () => {
    const a = generateClientId();
    const b = generateClientId();
    expect(a.length).toBeGreaterThan(0);
    expect(a).not.toBe(b);
  });
});

describe('nextZoneColor', () => {
  it('rotates through the palette based on existing zone count', () => {
    const first = nextZoneColor(0);
    const seventh = nextZoneColor(6); // palette size is 6, so this wraps
    expect(first).toBe(seventh);
  });

  it('is a hex color', () => {
    expect(nextZoneColor(0)).toMatch(/^#[0-9A-Fa-f]{6}$/);
  });
});

describe('createZoneDraft', () => {
  it('creates a rect zone centered on the requested point with sensible defaults', () => {
    const z = createZoneDraft({
      layoutId: 'L1',
      center: { x: 500, y: 500 },
      nextSortOrder: 3,
      indexForLabel: 0,
    });
    expect(z.shape).toBe(ZoneShape.Rect);
    expect(z.name).toBe('Zone 1');
    expect(z.sortOrder).toBe(3);
    expect(z.color).toMatch(/^#/);
    const g = JSON.parse(z.geometry!);
    expect(g.width).toBe(400);
    expect(g.height).toBe(200);
    // Center (500, 500) with size 400x200 → top-left (300, 400), both grid-aligned.
    expect(g.x).toBe(300);
    expect(g.y).toBe(400);
    expect(z.id.length).toBeGreaterThan(0);
    expect(z.seats).toEqual([]);
    expect(z.enabledSeatCount).toBe(0);
    expect(z.ticketTierIds).toEqual([]);
  });

  it('auto-labels incrementally across indexes', () => {
    expect(createZoneDraft({
      layoutId: 'L1', center: { x: 0, y: 0 }, nextSortOrder: 0, indexForLabel: 0,
    }).name).toBe('Zone 1');
    expect(createZoneDraft({
      layoutId: 'L1', center: { x: 0, y: 0 }, nextSortOrder: 0, indexForLabel: 4,
    }).name).toBe('Zone 5');
  });
});

describe('createRoundTableDraft', () => {
  it('creates a round table at the snapped center with default radius + capacity', () => {
    const t = createRoundTableDraft({
      layoutId: 'L1',
      center: { x: 523, y: 477 },
      nextSortOrder: 2,
      indexForLabel: 1,
    });
    expect(t.shape).toBe(TableShape.Round);
    expect(t.label).toBe('Table 2');
    expect(t.capacity).toBe(8);
    expect(t.enabledSeatCount).toBe(8);
    expect(t.sortOrder).toBe(2);
    const g = JSON.parse(t.geometry);
    // 523 snaps to 500, 477 snaps to 500.
    expect(g.centerX).toBe(500);
    expect(g.centerY).toBe(500);
    expect(g.radius).toBe(50);
    expect(t.venueLayoutId).toBe('L1');
    expect(t.seats).toEqual([]);
  });
});

describe('createRectTableDraft', () => {
  it('creates a rect table with default width/height/capacity', () => {
    const t = createRectTableDraft({
      layoutId: 'L2',
      center: { x: 400, y: 300 },
      nextSortOrder: 0,
      indexForLabel: 0,
    });
    expect(t.shape).toBe(TableShape.Rect);
    expect(t.label).toBe('Table 1');
    expect(t.capacity).toBe(10);
    const g = JSON.parse(t.geometry);
    expect(g.centerX).toBe(400);
    expect(g.centerY).toBe(300);
    expect(g.width).toBe(200);
    expect(g.height).toBe(100);
    expect(t.venueLayoutId).toBe('L2');
  });
});

describe('createDecorationDraft', () => {
  it('creates a Stage decoration centered on the requested point', () => {
    const d = createDecorationDraft(
      {
        layoutId: 'L1',
        center: { x: 500, y: 100 },
        nextSortOrder: 0,
        indexForLabel: 0,
      },
      DecorationKind.Stage,
    );
    expect(d.kind).toBe(DecorationKind.Stage);
    const g = JSON.parse(d.geometry);
    expect(g.width).toBe(300);
    expect(g.height).toBe(100);
    expect(g.x).toBe(350); // 500 - 150
    expect(g.y).toBe(50); // 100 - 50
    expect(d.venueLayoutId).toBe('L1');
    expect(d.properties).toBe('{}');
    expect(d.label).toBeNull();
  });

  it('honors any DecorationKind', () => {
    const aisle = createDecorationDraft(
      {
        layoutId: 'L1',
        center: { x: 0, y: 0 },
        nextSortOrder: 0,
        indexForLabel: 0,
      },
      DecorationKind.Aisle,
    );
    expect(aisle.kind).toBe(DecorationKind.Aisle);
  });
});

// ─────────────────────────── S8.7 tier assignment helpers ───────────────────────────

describe('resolveTierAssignments', () => {
  it('returns the persisted ticketTierIds when no draft override exists', () => {
    const zone = {
      id: 'z1',
      ticketTierIds: ['tier-a', 'tier-b'],
    };
    expect(resolveTierAssignments('zone', zone, {})).toEqual(['tier-a', 'tier-b']);
  });

  it('returns an empty array when ticketTierIds is null/undefined and no override', () => {
    const zone = { id: 'z1' } as { id: string; ticketTierIds?: string[] | null };
    expect(resolveTierAssignments('zone', zone, {})).toEqual([]);
  });

  it('prefers the draft override over the persisted array', () => {
    const zone = { id: 'z1', ticketTierIds: ['tier-a'] };
    expect(
      resolveTierAssignments('zone', zone, { 'zone:z1': ['tier-b', 'tier-c'] }),
    ).toEqual(['tier-b', 'tier-c']);
  });

  it('keys by kind+id so a zone and table with the same id do not collide', () => {
    const zone = { id: 'same', ticketTierIds: ['tier-zone'] };
    const table = { id: 'same', ticketTierIds: ['tier-table'] };
    const drafts = { 'zone:same': ['override-z'] };
    expect(resolveTierAssignments('zone', zone, drafts)).toEqual(['override-z']);
    expect(resolveTierAssignments('table', table, drafts)).toEqual(['tier-table']);
  });
});

describe('toggleTierAssignment', () => {
  it('adds the tier when absent', () => {
    expect(toggleTierAssignment(['a', 'b'], 'c')).toEqual(['a', 'b', 'c']);
  });

  it('removes the tier when present', () => {
    expect(toggleTierAssignment(['a', 'b', 'c'], 'b')).toEqual(['a', 'c']);
  });

  it('does not mutate the input list', () => {
    const input = ['a', 'b'];
    toggleTierAssignment(input, 'c');
    expect(input).toEqual(['a', 'b']);
  });

  it('round-trips when the same tier is toggled twice', () => {
    const once = toggleTierAssignment(['a'], 'b');
    expect(toggleTierAssignment(once, 'b')).toEqual(['a']);
  });
});

// ────────────────────── S8.8b: payload composer + counter ──────────────────────

const RECT_GEOM_50_50 = JSON.stringify({ x: 0, y: 0, width: 50, height: 50 });
const RECT_GEOM_100_100 = JSON.stringify({ x: 100, y: 100, width: 50, height: 50 });
const ROUND_TABLE_GEOM = JSON.stringify({ centerX: 200, centerY: 200, radius: 30 });

function emptyDraft(): CanvasEditorDraftState {
  return {
    geometryByKey: {},
    additions: { zones: [], tables: [], decorations: [] },
    deletions: new Set<string>(),
    tierAssignmentsByKey: {},
  };
}

function fakeZone(id: string, name: string): VenueZoneDto {
  return {
    id,
    name,
    color: '#abc',
    sortOrder: 0,
    enabledSeatCount: 0,
    totalSeatCount: 0,
    seats: [],
    shape: ZoneShape.Rect,
    geometry: RECT_GEOM_50_50,
    ticketTierIds: [],
  };
}

function fakeTable(id: string, label: string): VenueTableDto {
  return {
    id,
    venueLayoutId: 'L1',
    venueZoneId: null,
    label,
    shape: TableShape.Round,
    geometry: ROUND_TABLE_GEOM,
    capacity: 6,
    sortOrder: 0,
    enabledSeatCount: 6,
    seats: [],
    ticketTierIds: [],
  };
}

function fakeDecoration(id: string, kind: DecorationKind = DecorationKind.Stage): VenueDecorationDto {
  return {
    id,
    venueLayoutId: 'L1',
    kind,
    label: 'Main Stage',
    geometry: RECT_GEOM_50_50,
    properties: '{}',
    sortOrder: 0,
  };
}

function fakeBaseline(overrides: Partial<VenueLayoutDto> = {}): VenueLayoutDto {
  return {
    id: 'L1',
    name: 'Theater',
    layoutType: 'Theater',
    isTemplate: false,
    createdByUserId: 'U1',
    totalCapacity: 100,
    createdAt: '2026-04-25T00:00:00Z',
    rowVersion: 5,
    zones: [],
    tables: [],
    decorations: [],
    ...overrides,
  } as VenueLayoutDto;
}

describe('composeBatchPayload', () => {
  it('returns null name + null canvas (editor has no UI for them in S8.8b)', () => {
    const payload = composeBatchPayload({
      baseline: fakeBaseline(),
      draft: emptyDraft(),
    });
    expect(payload.name).toBeNull();
    expect(payload.canvas).toBeNull();
  });

  it('keeps every existing item with its id when nothing was edited', () => {
    const z = fakeZone('z1', 'Orchestra');
    const t = fakeTable('t1', 'Table A');
    const d = fakeDecoration('d1');
    const payload = composeBatchPayload({
      baseline: fakeBaseline({ zones: [z], tables: [t], decorations: [d] }),
      draft: emptyDraft(),
    });
    expect(payload.zones).toHaveLength(1);
    expect(payload.zones![0]).toMatchObject({ id: 'z1', name: 'Orchestra', shape: ZoneShape.Rect });
    expect(payload.tables).toHaveLength(1);
    expect(payload.tables![0]).toMatchObject({ id: 't1', label: 'Table A', shape: TableShape.Round, capacity: 6 });
    expect(payload.decorations).toHaveLength(1);
    expect(payload.decorations![0]).toMatchObject({ id: 'd1', kind: DecorationKind.Stage });
  });

  it('omits an existing zone that was deleted (server treats as removal)', () => {
    const kept = fakeZone('keep', 'Kept');
    const removed = fakeZone('drop', 'Removed');
    const draft = emptyDraft();
    const mutableDeletions = new Set(draft.deletions);
    mutableDeletions.add(refKey({ kind: 'zone', id: 'drop' }));
    const payload = composeBatchPayload({
      baseline: fakeBaseline({ zones: [kept, removed] }),
      draft: { ...draft, deletions: mutableDeletions },
    });
    expect(payload.zones).toHaveLength(1);
    expect(payload.zones![0].id).toBe('keep');
  });

  it('emits a draft addition with id=null so the server creates it', () => {
    const newZone: VenueZoneDto = {
      id: 'client-uuid-1',
      name: 'Brand New',
      color: '#fff',
      sortOrder: 1,
      enabledSeatCount: 0,
      totalSeatCount: 0,
      seats: [],
      shape: ZoneShape.Rect,
      geometry: RECT_GEOM_100_100,
      ticketTierIds: [],
    };
    const draft = emptyDraft();
    draft.additions.zones.push(newZone);

    const payload = composeBatchPayload({
      baseline: fakeBaseline(),
      draft,
    });
    expect(payload.zones).toHaveLength(1);
    expect(payload.zones![0]).toMatchObject({
      id: null,
      name: 'Brand New',
      color: '#fff',
      sortOrder: 1,
      shape: ZoneShape.Rect,
      geometry: RECT_GEOM_100_100,
    });
  });

  it('applies a draft geometry override on an existing item', () => {
    const z = fakeZone('z1', 'Orchestra');
    const draft = emptyDraft();
    draft.geometryByKey[refKey({ kind: 'zone', id: 'z1' })] = RECT_GEOM_100_100;

    const payload = composeBatchPayload({
      baseline: fakeBaseline({ zones: [z] }),
      draft,
    });
    expect(payload.zones![0].geometry).toBe(RECT_GEOM_100_100);
    expect(payload.zones![0].id).toBe('z1');
  });

  it('preserves table fields including zoneId and capacity', () => {
    const t: VenueTableDto = {
      ...fakeTable('t1', 'VIP Table'),
      venueZoneId: 'zone-of-table',
      capacity: 12,
    };
    const payload = composeBatchPayload({
      baseline: fakeBaseline({ tables: [t] }),
      draft: emptyDraft(),
    });
    expect(payload.tables![0]).toMatchObject({
      id: 't1',
      label: 'VIP Table',
      shape: TableShape.Round,
      capacity: 12,
      zoneId: 'zone-of-table',
    });
  });

  it('preserves decoration kind, label, and properties', () => {
    const d: VenueDecorationDto = {
      ...fakeDecoration('d1', DecorationKind.Door),
      label: 'Side Door',
      properties: '{"size":"wide"}',
    };
    const payload = composeBatchPayload({
      baseline: fakeBaseline({ decorations: [d] }),
      draft: emptyDraft(),
    });
    expect(payload.decorations![0]).toMatchObject({
      id: 'd1',
      kind: DecorationKind.Door,
      label: 'Side Door',
      properties: '{"size":"wide"}',
    });
  });

  it('falls back to ZoneShape.Rect for legacy zones with no shape field', () => {
    const legacy = { ...fakeZone('z1', 'Legacy'), shape: undefined } as VenueZoneDto;
    const payload = composeBatchPayload({
      baseline: fakeBaseline({ zones: [legacy] }),
      draft: emptyDraft(),
    });
    expect(payload.zones![0].shape).toBe(ZoneShape.Rect);
  });

  it('handles a mixed draft (delete + override + addition) atomically', () => {
    const kept = fakeZone('keep', 'Kept');
    const removed = fakeZone('drop', 'Removed');
    const moved = fakeZone('move', 'Moved');
    const newZone: VenueZoneDto = {
      ...fakeZone('client-uuid', 'Brand New'),
      sortOrder: 5,
    };

    const draft = emptyDraft();
    const mutableDeletions = new Set(draft.deletions);
    mutableDeletions.add(refKey({ kind: 'zone', id: 'drop' }));
    draft.geometryByKey[refKey({ kind: 'zone', id: 'move' })] = RECT_GEOM_100_100;
    draft.additions.zones.push(newZone);

    const payload = composeBatchPayload({
      baseline: fakeBaseline({ zones: [kept, removed, moved] }),
      draft: { ...draft, deletions: mutableDeletions },
    });

    expect(payload.zones).toHaveLength(3); // kept + moved + new
    const ids = payload.zones!.map((z) => z.id);
    expect(ids).toContain('keep');
    expect(ids).toContain('move');
    expect(ids).toContain(null); // new addition
    expect(ids).not.toContain('drop');

    const movedOut = payload.zones!.find((z) => z.id === 'move');
    expect(movedOut?.geometry).toBe(RECT_GEOM_100_100);
  });
});

describe('countDraftChanges', () => {
  it('returns 0 for an unchanged draft', () => {
    expect(
      countDraftChanges({
        baseline: fakeBaseline({ zones: [fakeZone('z1', 'X')] }),
        draft: emptyDraft(),
      }),
    ).toBe(0);
  });

  it('counts each baseline deletion as 1 change', () => {
    const draft = emptyDraft();
    const mutableDeletions = new Set(draft.deletions);
    mutableDeletions.add(refKey({ kind: 'zone', id: 'z1' }));
    mutableDeletions.add(refKey({ kind: 'table', id: 't1' }));

    expect(
      countDraftChanges({
        baseline: fakeBaseline({
          zones: [fakeZone('z1', 'X')],
          tables: [fakeTable('t1', 'T')],
        }),
        draft: { ...draft, deletions: mutableDeletions },
      }),
    ).toBe(2);
  });

  it('counts each addition as 1 change', () => {
    const draft = emptyDraft();
    draft.additions.zones.push(fakeZone('cli-1', 'New'));
    draft.additions.tables.push(fakeTable('cli-2', 'NewT'));
    draft.additions.decorations.push(fakeDecoration('cli-3'));
    expect(
      countDraftChanges({ baseline: fakeBaseline(), draft }),
    ).toBe(3);
  });

  it('counts geometry overrides on baseline items only', () => {
    const draft = emptyDraft();
    // override on a baseline item (counts) + override on a new addition (does not count separately
    // — the addition itself is the change)
    draft.geometryByKey[refKey({ kind: 'zone', id: 'baseline-zone' })] = RECT_GEOM_100_100;
    const newZone = fakeZone('client-uuid', 'New');
    draft.additions.zones.push(newZone);
    draft.geometryByKey[refKey({ kind: 'zone', id: newZone.id })] = RECT_GEOM_100_100;

    expect(
      countDraftChanges({
        baseline: fakeBaseline({ zones: [fakeZone('baseline-zone', 'X')] }),
        draft,
      }),
    ).toBe(2); // 1 override on baseline + 1 addition
  });

  it('does not double-count a baseline override that was then deleted', () => {
    const draft = emptyDraft();
    draft.geometryByKey[refKey({ kind: 'zone', id: 'z1' })] = RECT_GEOM_100_100;
    const mutableDeletions = new Set(draft.deletions);
    mutableDeletions.add(refKey({ kind: 'zone', id: 'z1' }));

    expect(
      countDraftChanges({
        baseline: fakeBaseline({ zones: [fakeZone('z1', 'X')] }),
        draft: { ...draft, deletions: mutableDeletions },
      }),
    ).toBe(1); // only the deletion
  });

  it('ignores tier-assignment overrides (not persisted in S8.8b)', () => {
    const draft = emptyDraft();
    draft.tierAssignmentsByKey[refKey({ kind: 'zone', id: 'z1' })] = ['tier-1'];
    expect(
      countDraftChanges({
        baseline: fakeBaseline({ zones: [fakeZone('z1', 'X')] }),
        draft,
      }),
    ).toBe(0);
  });

  it('sums all change kinds together', () => {
    const draft = emptyDraft();
    const mutableDeletions = new Set(draft.deletions);
    mutableDeletions.add(refKey({ kind: 'zone', id: 'drop' }));
    draft.geometryByKey[refKey({ kind: 'zone', id: 'move' })] = RECT_GEOM_100_100;
    draft.additions.zones.push(fakeZone('cli-1', 'New'));

    expect(
      countDraftChanges({
        baseline: fakeBaseline({
          zones: [fakeZone('drop', 'X'), fakeZone('move', 'Y')],
        }),
        draft: { ...draft, deletions: mutableDeletions },
      }),
    ).toBe(3); // 1 delete + 1 override + 1 add
  });
});
