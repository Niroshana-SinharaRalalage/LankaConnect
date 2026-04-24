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
  snapToGrid,
  refKey,
  itemCenter,
  applyDragToGeometry,
  resolveGeometry,
  collectItemCenters,
} from '../canvasEditorGeometry';
import {
  TableShape,
  ZoneShape,
  DecorationKind,
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
