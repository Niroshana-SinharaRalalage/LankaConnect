/**
 * Slice 8 S8.3: pure helpers for the canvas editor's drag-and-snap logic.
 *
 * The goal of separating these from the Konva view is so the shape-specific
 * math (rect center, round-table center, rotated rect center) is testable
 * without mounting any components — and so S8.6's undo/redo can diff
 * serialized geometry strings without caring about Konva internals.
 */

import type {
  VenueZoneDto,
  VenueTableDto,
  VenueDecorationDto,
} from '@/infrastructure/api/types/events.types';
import {
  parseRectGeom,
  parseCurveGeom,
  parseRoundTableGeom,
  parseRectTableGeom,
} from '@/presentation/utils/layoutGeometry';

/** Snap grid in canvas units — matches the dotted overlay in CanvasEditorStage. */
export const EDITOR_GRID = 50;

/** Snap a scalar value to the nearest multiple of the grid. */
export function snapToGrid(value: number, grid: number = EDITOR_GRID): number {
  return Math.round(value / grid) * grid;
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
