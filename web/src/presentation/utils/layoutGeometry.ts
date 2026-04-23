/**
 * Slice 6 / Slice 7: shared geometry parsers for VenueLayoutDto rendering.
 *
 * The domain persists geometry as JSON strings (shape-specific schemas —
 * see VenueZone.Geometry, VenueTable.Geometry, VenueDecoration.Geometry).
 * Both LayoutPreview (SVG) and SeatPickerKonva (react-konva) need to read
 * those blobs, and they must degrade identically when geometry is missing
 * or malformed (organizers can hand-edit these fields in the canvas editor,
 * and the domain default is `"{}"`).
 *
 * Keeping the parsers in one file means:
 *   - One shared contract for what "valid geometry" means per shape.
 *   - A future shape addition (e.g. Ellipse) touches one file, not two.
 *   - Tolerant parsing never throws — callers fall back to placeholders.
 */

export interface RectGeom {
  x: number;
  y: number;
  width: number;
  height: number;
  rotation?: number;
}

export interface CurveGeom {
  centerX: number;
  centerY: number;
  radius: number;
  startAngleDeg: number;
  sweepAngleDeg: number;
  rowCount?: number;
}

export interface RoundTableGeom {
  centerX: number;
  centerY: number;
  radius: number;
}

export interface RectTableGeom {
  centerX: number;
  centerY: number;
  width: number;
  height: number;
  rotation?: number;
}

/**
 * Best-effort JSON.parse that never throws. Returns `null` for:
 *   - null / undefined / empty input
 *   - non-object payloads (strings, numbers, arrays)
 *   - syntactically invalid JSON
 */
function tryParse<T extends object>(raw: string | null | undefined): T | null {
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw);
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) return null;
    return parsed as T;
  } catch {
    return null;
  }
}

/** Returns a rect-shape payload or null if missing/invalid. */
export function parseRectGeom(raw: string | null | undefined): RectGeom | null {
  const parsed = tryParse<RectGeom>(raw);
  if (!parsed) return null;
  if (
    typeof parsed.x !== 'number' ||
    typeof parsed.y !== 'number' ||
    typeof parsed.width !== 'number' ||
    typeof parsed.height !== 'number'
  ) {
    return null;
  }
  return parsed;
}

/** Returns a curve-shape payload or null. Curve draws an arc. */
export function parseCurveGeom(raw: string | null | undefined): CurveGeom | null {
  const parsed = tryParse<CurveGeom>(raw);
  if (!parsed) return null;
  if (
    typeof parsed.centerX !== 'number' ||
    typeof parsed.centerY !== 'number' ||
    typeof parsed.radius !== 'number' ||
    typeof parsed.startAngleDeg !== 'number' ||
    typeof parsed.sweepAngleDeg !== 'number'
  ) {
    return null;
  }
  return parsed;
}

/** Returns a round-table payload or null. */
export function parseRoundTableGeom(raw: string | null | undefined): RoundTableGeom | null {
  const parsed = tryParse<RoundTableGeom>(raw);
  if (!parsed) return null;
  if (
    typeof parsed.centerX !== 'number' ||
    typeof parsed.centerY !== 'number' ||
    typeof parsed.radius !== 'number'
  ) {
    return null;
  }
  return parsed;
}

/** Returns a rect-table payload or null. */
export function parseRectTableGeom(raw: string | null | undefined): RectTableGeom | null {
  const parsed = tryParse<RectTableGeom>(raw);
  if (!parsed) return null;
  if (
    typeof parsed.centerX !== 'number' ||
    typeof parsed.centerY !== 'number' ||
    typeof parsed.width !== 'number' ||
    typeof parsed.height !== 'number'
  ) {
    return null;
  }
  return parsed;
}

/**
 * Shared color palette for decoration kinds. Both LayoutPreview and the
 * SeatPicker's Konva renderer use these so the organizer's preview and the
 * attendee-facing picker stay visually consistent.
 */
export const decorationStyle = (kind: string): {
  fill: string;
  stroke: string;
  label: string | null;
  labelColor: string;
} => {
  switch (kind) {
    case 'Stage':
      return { fill: '#1f2937', stroke: '#111827', label: 'STAGE', labelColor: '#f9fafb' };
    case 'DanceFloor':
      return { fill: '#fef3c7', stroke: '#f59e0b', label: 'Dance Floor', labelColor: '#b45309' };
    case 'Aisle':
      return { fill: '#e5e7eb', stroke: '#9ca3af', label: null, labelColor: '#4b5563' };
    case 'Door':
      return { fill: '#dbeafe', stroke: '#3b82f6', label: 'Door', labelColor: '#1e40af' };
    case 'Wall':
      return { fill: '#9ca3af', stroke: '#4b5563', label: null, labelColor: '#111827' };
    case 'Text':
      return { fill: 'transparent', stroke: '#9ca3af', label: null, labelColor: '#111827' };
    case 'Image':
      return { fill: '#f3f4f6', stroke: '#9ca3af', label: 'Image', labelColor: '#4b5563' };
    default:
      return { fill: '#f3f4f6', stroke: '#9ca3af', label: null, labelColor: '#4b5563' };
  }
};
