/**
 * Slice 8 S8.3: pure helpers for the canvas editor's drag-and-snap logic.
 *
 * The goal of separating these from the Konva view is so the shape-specific
 * math (rect center, round-table center, rotated rect center) is testable
 * without mounting any components — and so S8.6's undo/redo can diff
 * serialized geometry strings without caring about Konva internals.
 */

import {
  DecorationKind,
  TableShape,
  ZoneShape,
  type VenueZoneDto,
  type VenueTableDto,
  type VenueDecorationDto,
} from '@/infrastructure/api/types/events.types';
import {
  parseRectGeom,
  parseCurveGeom,
  parseRoundTableGeom,
  parseRectTableGeom,
} from '@/presentation/utils/layoutGeometry';

/** Snap grid in canvas units — matches the dotted overlay in CanvasEditorStage. */
export const EDITOR_GRID = 50;

/** Rotation snap in degrees — 15° increments give organizer-friendly angles. */
export const EDITOR_ROTATION_STEP = 15;

/** Minimum shape dimension in canvas units after resize snap. Prevents
 * degenerate collapsed shapes that confuse the renderer. */
export const MIN_SHAPE_DIMENSION = 50;

/** Snap a scalar value to the nearest multiple of the grid. */
export function snapToGrid(value: number, grid: number = EDITOR_GRID): number {
  return Math.round(value / grid) * grid;
}

/** Snap a rotation in degrees to the nearest step, wrapping into [0, 360). */
export function snapRotation(
  degrees: number,
  step: number = EDITOR_ROTATION_STEP,
): number {
  const snapped = Math.round(degrees / step) * step;
  // Normalize to [0, 360) so equality checks don't trip over -0 / 360.
  const mod = ((snapped % 360) + 360) % 360;
  return mod;
}

/**
 * Item kinds the canvas editor treats as draggable. Seats are not editable
 * individually — they follow the parent zone/table via generated positions.
 */
export type CanvasItemKind = 'zone' | 'table' | 'decoration';

export interface CanvasItemRef {
  kind: CanvasItemKind;
  id: string;
}

/** Stable string key for using an ItemRef in a Map or object. */
export function refKey(ref: CanvasItemRef): string {
  return `${ref.kind}:${ref.id}`;
}

/**
 * Compute the canvas-space center of an item (used as the Konva Group's
 * x/y so the item can be dragged as a unit regardless of its underlying
 * geometry shape). Returns null when the geometry is malformed.
 */
export function itemCenter(
  kind: CanvasItemKind,
  geometry: string | null | undefined,
  shapeHint?: string,
): { x: number; y: number } | null {
  if (kind === 'zone') {
    if (shapeHint === 'Curve') {
      const g = parseCurveGeom(geometry);
      if (!g) return null;
      return { x: g.centerX, y: g.centerY };
    }
    const g = parseRectGeom(geometry);
    if (!g) return null;
    return { x: g.x + g.width / 2, y: g.y + g.height / 2 };
  }
  if (kind === 'table') {
    if (shapeHint === 'Round') {
      const g = parseRoundTableGeom(geometry);
      if (!g) return null;
      return { x: g.centerX, y: g.centerY };
    }
    const g = parseRectTableGeom(geometry);
    if (!g) return null;
    return { x: g.centerX, y: g.centerY };
  }
  // decoration
  const g = parseRectGeom(geometry);
  if (!g) return null;
  return { x: g.x + g.width / 2, y: g.y + g.height / 2 };
}

/**
 * Given an item's current geometry and a new desired center (typically from
 * a drag end, already snapped), produce a JSON geometry string with the new
 * position. Returns null when the geometry is malformed — callers should
 * drop the drag rather than corrupt the record.
 *
 * Rotation and size are preserved; only the position changes.
 */
export function applyDragToGeometry(
  kind: CanvasItemKind,
  geometry: string | null | undefined,
  newCenter: { x: number; y: number },
  shapeHint?: string,
): string | null {
  if (kind === 'zone') {
    if (shapeHint === 'Curve') {
      const g = parseCurveGeom(geometry);
      if (!g) return null;
      return JSON.stringify({
        centerX: newCenter.x,
        centerY: newCenter.y,
        radius: g.radius,
        startAngleDeg: g.startAngleDeg,
        sweepAngleDeg: g.sweepAngleDeg,
      });
    }
    const g = parseRectGeom(geometry);
    if (!g) return null;
    return JSON.stringify({
      x: newCenter.x - g.width / 2,
      y: newCenter.y - g.height / 2,
      width: g.width,
      height: g.height,
      rotation: g.rotation,
    });
  }
  if (kind === 'table') {
    if (shapeHint === 'Round') {
      const g = parseRoundTableGeom(geometry);
      if (!g) return null;
      return JSON.stringify({
        centerX: newCenter.x,
        centerY: newCenter.y,
        radius: g.radius,
      });
    }
    const g = parseRectTableGeom(geometry);
    if (!g) return null;
    return JSON.stringify({
      centerX: newCenter.x,
      centerY: newCenter.y,
      width: g.width,
      height: g.height,
      rotation: g.rotation,
    });
  }
  // decoration
  const g = parseRectGeom(geometry);
  if (!g) return null;
  return JSON.stringify({
    x: newCenter.x - g.width / 2,
    y: newCenter.y - g.height / 2,
    width: g.width,
    height: g.height,
    rotation: g.rotation,
  });
}

/**
 * S8.4: given an item's current geometry and new width/height (already
 * snapped to the editor grid), produce a new geometry JSON with size
 * updated while keeping the item's center and rotation. Round tables
 * ignore width/height — their size comes from `applyRadiusToGeometry`.
 * Returns null when the geometry is malformed.
 */
export function applyResizeToGeometry(
  kind: CanvasItemKind,
  geometry: string | null | undefined,
  newSize: { width: number; height: number },
  shapeHint?: string,
): string | null {
  if (kind === 'zone') {
    if (shapeHint === 'Curve') {
      // Curve zone resize is not supported in S8.4 — the geometry model
      // uses radius + sweep, not width/height. Caller should surface a
      // no-op rather than a corrupted record.
      return null;
    }
    const g = parseRectGeom(geometry);
    if (!g) return null;
    const cx = g.x + g.width / 2;
    const cy = g.y + g.height / 2;
    return JSON.stringify({
      x: cx - newSize.width / 2,
      y: cy - newSize.height / 2,
      width: newSize.width,
      height: newSize.height,
      rotation: g.rotation,
    });
  }
  if (kind === 'table') {
    if (shapeHint === 'Round') {
      // Round table: intentionally ignore width/height. Caller should use
      // applyRadiusToGeometry with keepRatio.
      return null;
    }
    const g = parseRectTableGeom(geometry);
    if (!g) return null;
    return JSON.stringify({
      centerX: g.centerX,
      centerY: g.centerY,
      width: newSize.width,
      height: newSize.height,
      rotation: g.rotation,
    });
  }
  // decoration
  const g = parseRectGeom(geometry);
  if (!g) return null;
  const cx = g.x + g.width / 2;
  const cy = g.y + g.height / 2;
  return JSON.stringify({
    x: cx - newSize.width / 2,
    y: cy - newSize.height / 2,
    width: newSize.width,
    height: newSize.height,
    rotation: g.rotation,
  });
}

/**
 * S8.4: update a round table's radius while keeping its center.
 * Round-only; callers check shape before invoking.
 */
export function applyRadiusToGeometry(
  geometry: string | null | undefined,
  newRadius: number,
): string | null {
  const g = parseRoundTableGeom(geometry);
  if (!g) return null;
  return JSON.stringify({
    centerX: g.centerX,
    centerY: g.centerY,
    radius: newRadius,
  });
}

/**
 * S8.4: update rotation (degrees) preserving everything else. Curve zones
 * don't carry a rotation (it's implicit in startAngleDeg/sweepAngleDeg),
 * so they return null — callers should treat as a no-op. Round tables
 * likewise have no meaningful rotation (circular). Returns null for
 * malformed geometry.
 */
export function applyRotationToGeometry(
  kind: CanvasItemKind,
  geometry: string | null | undefined,
  rotationDeg: number,
  shapeHint?: string,
): string | null {
  if (kind === 'zone') {
    if (shapeHint === 'Curve') return null;
    const g = parseRectGeom(geometry);
    if (!g) return null;
    return JSON.stringify({
      x: g.x,
      y: g.y,
      width: g.width,
      height: g.height,
      rotation: rotationDeg,
    });
  }
  if (kind === 'table') {
    if (shapeHint === 'Round') return null;
    const g = parseRectTableGeom(geometry);
    if (!g) return null;
    return JSON.stringify({
      centerX: g.centerX,
      centerY: g.centerY,
      width: g.width,
      height: g.height,
      rotation: rotationDeg,
    });
  }
  // decoration
  const g = parseRectGeom(geometry);
  if (!g) return null;
  return JSON.stringify({
    x: g.x,
    y: g.y,
    width: g.width,
    height: g.height,
    rotation: rotationDeg,
  });
}

/**
 * S8.5a: extract the dimensions the property panel needs to display and
 * let the organizer edit. Returns null for malformed geometry so the
 * panel can show a read-only fallback rather than empty inputs.
 *
 * The shape-hint parameter controls which dimension fields come back:
 *   - Rect zone / rect table / decoration → { width, height, rotation }
 *   - Round table → { radius }
 *   - Curve zone → { radius } (the only dimension the organizer can see
 *     without rebuilding the curve geometry — editing is out of scope
 *     for Slice 8)
 */
export interface ReadableDimensions {
  width?: number;
  height?: number;
  radius?: number;
  rotation?: number;
}

export function readGeometryDimensions(
  kind: CanvasItemKind,
  geometry: string | null | undefined,
  shapeHint?: string,
): ReadableDimensions | null {
  if (kind === 'zone') {
    if (shapeHint === 'Curve') {
      const g = parseCurveGeom(geometry);
      if (!g) return null;
      return { radius: g.radius };
    }
    const g = parseRectGeom(geometry);
    if (!g) return null;
    return { width: g.width, height: g.height, rotation: g.rotation ?? 0 };
  }
  if (kind === 'table') {
    if (shapeHint === 'Round') {
      const g = parseRoundTableGeom(geometry);
      if (!g) return null;
      return { radius: g.radius };
    }
    const g = parseRectTableGeom(geometry);
    if (!g) return null;
    return { width: g.width, height: g.height, rotation: g.rotation ?? 0 };
  }
  const g = parseRectGeom(geometry);
  if (!g) return null;
  return { width: g.width, height: g.height, rotation: g.rotation ?? 0 };
}

/**
 * Resolve the effective geometry for an item, preferring a local draft
 * override (from in-progress editor moves) over the persisted geometry.
 */
export function resolveGeometry<T extends { id: string; geometry?: string | null }>(
  kind: CanvasItemKind,
  item: T,
  draftGeometryByKey: Record<string, string>,
): string | null | undefined {
  const override = draftGeometryByKey[refKey({ kind, id: item.id })];
  return override ?? item.geometry;
}

// ─────────────────────────── S8.5b: draft factories ───────────────────────────

/**
 * Generate a random v4-ish UUID. Uses crypto.randomUUID when available
 * (Node 16+ / modern browsers) and falls back to a Math.random shim —
 * sufficient because these IDs only live in the client draft until
 * PUT /batch assigns server IDs on save (S8.8).
 */
export function generateClientId(): string {
  const g = globalThis as unknown as { crypto?: { randomUUID?: () => string } };
  if (g.crypto?.randomUUID) return g.crypto.randomUUID();
  const h = () => Math.floor(Math.random() * 0x10000).toString(16).padStart(4, '0');
  return `${h()}${h()}-${h()}-4${h().slice(1)}-${((Math.random() * 4) | 8).toString(16)}${h().slice(1)}-${h()}${h()}${h()}`;
}

/** Default dimensions for newly-added items — one grid cell beyond the
 * MIN so there's slack for seat generators. */
const DEFAULT_ZONE_WIDTH = 400;
const DEFAULT_ZONE_HEIGHT = 200;
const DEFAULT_RECT_TABLE_WIDTH = 200;
const DEFAULT_RECT_TABLE_HEIGHT = 100;
const DEFAULT_ROUND_TABLE_RADIUS = 50;
const DEFAULT_DECORATION_WIDTH = 300;
const DEFAULT_DECORATION_HEIGHT = 100;
const DEFAULT_ROUND_CAPACITY = 8;
const DEFAULT_RECT_CAPACITY = 10;

const DEFAULT_ZONE_PALETTE = [
  '#3B82F6', // blue
  '#10B981', // emerald
  '#F59E0B', // amber
  '#8B5CF6', // violet
  '#EC4899', // pink
  '#06B6D4', // cyan
];

/** Pick a color from the palette based on how many zones already exist. */
export function nextZoneColor(existingZoneCount: number): string {
  return DEFAULT_ZONE_PALETTE[existingZoneCount % DEFAULT_ZONE_PALETTE.length];
}

export interface DraftFactoryContext {
  layoutId: string;
  center: { x: number; y: number };
  nextSortOrder: number;
  /** Number of existing items of the same kind — used for auto-labels + color rotation. */
  indexForLabel: number;
}

export function createZoneDraft(ctx: DraftFactoryContext): VenueZoneDto {
  const { center, nextSortOrder, indexForLabel } = ctx;
  return {
    id: generateClientId(),
    name: `Zone ${indexForLabel + 1}`,
    color: nextZoneColor(indexForLabel),
    sortOrder: nextSortOrder,
    enabledSeatCount: 0,
    totalSeatCount: 0,
    seats: [],
    shape: ZoneShape.Rect,
    geometry: JSON.stringify({
      x: snapToGrid(center.x - DEFAULT_ZONE_WIDTH / 2),
      y: snapToGrid(center.y - DEFAULT_ZONE_HEIGHT / 2),
      width: DEFAULT_ZONE_WIDTH,
      height: DEFAULT_ZONE_HEIGHT,
    }),
    ticketTierIds: [],
  };
}

export function createRoundTableDraft(ctx: DraftFactoryContext): VenueTableDto {
  const { layoutId, center, nextSortOrder, indexForLabel } = ctx;
  return {
    id: generateClientId(),
    venueLayoutId: layoutId,
    label: `Table ${indexForLabel + 1}`,
    shape: TableShape.Round,
    geometry: JSON.stringify({
      centerX: snapToGrid(center.x),
      centerY: snapToGrid(center.y),
      radius: DEFAULT_ROUND_TABLE_RADIUS,
    }),
    capacity: DEFAULT_ROUND_CAPACITY,
    sortOrder: nextSortOrder,
    enabledSeatCount: DEFAULT_ROUND_CAPACITY,
    seats: [],
    ticketTierIds: [],
  };
}

export function createRectTableDraft(ctx: DraftFactoryContext): VenueTableDto {
  const { layoutId, center, nextSortOrder, indexForLabel } = ctx;
  return {
    id: generateClientId(),
    venueLayoutId: layoutId,
    label: `Table ${indexForLabel + 1}`,
    shape: TableShape.Rect,
    geometry: JSON.stringify({
      centerX: snapToGrid(center.x),
      centerY: snapToGrid(center.y),
      width: DEFAULT_RECT_TABLE_WIDTH,
      height: DEFAULT_RECT_TABLE_HEIGHT,
    }),
    capacity: DEFAULT_RECT_CAPACITY,
    sortOrder: nextSortOrder,
    enabledSeatCount: DEFAULT_RECT_CAPACITY,
    seats: [],
    ticketTierIds: [],
  };
}

export function createDecorationDraft(
  ctx: DraftFactoryContext,
  kind: DecorationKind,
): VenueDecorationDto {
  const { layoutId, center, nextSortOrder } = ctx;
  return {
    id: generateClientId(),
    venueLayoutId: layoutId,
    kind,
    label: null,
    geometry: JSON.stringify({
      x: snapToGrid(center.x - DEFAULT_DECORATION_WIDTH / 2),
      y: snapToGrid(center.y - DEFAULT_DECORATION_HEIGHT / 2),
      width: DEFAULT_DECORATION_WIDTH,
      height: DEFAULT_DECORATION_HEIGHT,
    }),
    properties: '{}',
    sortOrder: nextSortOrder,
  };
}

/**
 * Collect all item centers for a layout given any draft overrides — used by
 * the alignment-guide detector in CanvasEditorStage. Skips malformed
 * geometries silently rather than surfacing them here; rendering code
 * shows a placeholder for those.
 */
export function collectItemCenters(
  zones: readonly VenueZoneDto[],
  tables: readonly VenueTableDto[],
  decorations: readonly VenueDecorationDto[],
  draftGeometryByKey: Record<string, string>,
): Array<{ ref: CanvasItemRef; center: { x: number; y: number } }> {
  const out: Array<{ ref: CanvasItemRef; center: { x: number; y: number } }> = [];
  for (const z of zones) {
    const g = resolveGeometry('zone', z, draftGeometryByKey);
    const c = itemCenter('zone', g, z.shape as string | undefined);
    if (c) out.push({ ref: { kind: 'zone', id: z.id }, center: c });
  }
  for (const t of tables) {
    const g = resolveGeometry('table', t, draftGeometryByKey);
    const c = itemCenter('table', g, t.shape as string);
    if (c) out.push({ ref: { kind: 'table', id: t.id }, center: c });
  }
  for (const d of decorations) {
    const g = resolveGeometry('decoration', d, draftGeometryByKey);
    const c = itemCenter('decoration', g);
    if (c) out.push({ ref: { kind: 'decoration', id: d.id }, center: c });
  }
  return out;
}
