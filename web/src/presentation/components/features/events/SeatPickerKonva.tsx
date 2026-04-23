/**
 * Slice 7 Chunk S7.1–S7.2: SeatPickerKonva — the Konva implementation.
 *
 * Wrapped by [SeatPicker.tsx](./SeatPicker.tsx) via Next.js dynamic() with
 * `ssr: false`. Never import this file directly from a page.
 *
 * S7.1 shipped the Stage + background Rect. S7.2 adds structural shapes:
 * zones (rect / curve / polygon — polygon degrades to placeholder text),
 * tables (round / rect / square), decorations (stage / dance-floor /
 * aisle / door / wall / text / image).
 *
 * Seat rendering (status color coding, tier filter, selection) lands in S7.3
 * alongside the tier model. Keeping the chunk small de-risks each step.
 */

'use client';

import React, { useMemo } from 'react';
import {
  Stage,
  Layer,
  Rect,
  Circle,
  Text,
  Path,
  Group,
} from 'react-konva';
import type {
  VenueLayoutDto,
  VenueZoneDto,
  VenueTableDto,
  VenueDecorationDto,
  SeatAvailabilityDto,
  SeatDto,
  SeatStatus,
} from '@/infrastructure/api/types/events.types';
import {
  parseRectGeom,
  parseCurveGeom,
  parseRoundTableGeom,
  parseRectTableGeom,
  decorationStyle,
  computeRectZoneSeatPositions,
  computeRoundTableSeatPositions,
  computeRectTableSeatPositions,
  type RoundTableGeom,
  type ComputedSeatPoint,
} from '@/presentation/utils/layoutGeometry';

/**
 * Runtime state per seat, merged from the optional `availability` prop onto
 * the structural SeatDto. If `availability` is omitted all seats default to
 * Available — useful for organizer-side preview where the picker shows the
 * whole inventory regardless of holds.
 */
interface SeatRenderState {
  id: string;
  row: string;
  number: number;
  label: string;
  isEnabled: boolean;
  status: SeatStatus;
  zoneId?: string;
  tableId?: string;
  zoneColor: string;
  angleDeg?: number | null;
}

export interface SeatPickerKonvaProps {
  layout: VenueLayoutDto;
  /**
   * Optional runtime status per seat (from useSeatAvailability). When absent,
   * all seats are treated as Available and tier colors are used.
   */
  availability?: SeatAvailabilityDto[];
  /**
   * Seats the current context is allowed to pick (e.g. seats mapped to the
   * attendee's chosen ticket tier via tier_assignments). Any seat NOT in
   * this set is rendered grayed + non-selectable. When undefined the picker
   * is unfiltered — every Available seat is clickable.
   */
  eligibleSeatIds?: ReadonlySet<string>;
  /** Seats currently selected by the user. Rendered with the selected color. */
  selectedSeatIds?: ReadonlySet<string>;
  /**
   * Fires when a selectable seat is clicked. The parent owns the selection
   * model; this component just relays interaction events.
   */
  onSeatClick?: (seatId: string) => void;
  /**
   * Width of the rendered canvas in CSS pixels. The Konva stage scales the
   * layout's virtual canvas (typically 1200×800) to fit.
   */
  width?: number;
  /**
   * Height of the rendered canvas in CSS pixels. Defaults to preserve the
   * layout's aspect ratio given `width`.
   */
  height?: number;
}

const DEFAULT_WIDTH = 960;

// Seat color palette — consistent with SeatSelector's legend copy so an
// attendee who has seen one style recognizes the other during the rollout.
const SEAT_COLORS = {
  selected: '#10b981', // emerald — what the attendee has picked
  selectedStroke: '#047857',
  held: '#f59e0b', // amber — on hold (possibly by someone else or pending confirm)
  reserved: '#ef4444', // red — confirmed reservation, not selectable
  disabled: '#d1d5db', // gray — structurally disabled
  filtered: '#e5e7eb', // very pale gray — outside the tier filter
  strokeLight: '#1f2937',
} as const;

export function SeatPickerKonva({
  layout,
  availability,
  eligibleSeatIds,
  selectedSeatIds,
  onSeatClick,
  width = DEFAULT_WIDTH,
  height,
}: SeatPickerKonvaProps) {
  const canvasWidth = layout.canvas?.width ?? 1200;
  const canvasHeight = layout.canvas?.height ?? 800;
  const background = layout.canvas?.backgroundColor ?? '#ffffff';

  const aspect = canvasWidth / canvasHeight;
  const stageWidth = width;
  const stageHeight = height ?? Math.round(stageWidth / aspect);
  const scale = stageWidth / canvasWidth;

  // Merge availability onto the layout's structural seats so the renderer
  // sees a single SeatRenderState shape. useMemo keeps this stable across
  // renders unless the inputs actually change.
  const availabilityById = useMemo(() => {
    const map = new Map<string, SeatAvailabilityDto>();
    for (const a of availability ?? []) map.set(a.id, a);
    return map;
  }, [availability]);

  return (
    <Stage
      width={stageWidth}
      height={stageHeight}
      scaleX={scale}
      scaleY={scale}
      data-testid="seat-picker-stage"
    >
      {/* Background + decorations */}
      <Layer>
        <Rect
          x={0}
          y={0}
          width={canvasWidth}
          height={canvasHeight}
          fill={background}
          stroke="#e5e7eb"
          strokeWidth={2}
        />
        {(layout.decorations ?? []).map((d) => (
          <DecorationShape key={d.id} decoration={d} />
        ))}
      </Layer>

      {/* Zones — structural shape + their seats */}
      <Layer>
        {(layout.zones ?? []).map((z) => (
          <ZoneShape
            key={z.id}
            zone={z}
            availabilityById={availabilityById}
            eligibleSeatIds={eligibleSeatIds}
            selectedSeatIds={selectedSeatIds}
            onSeatClick={onSeatClick}
          />
        ))}
      </Layer>

      {/* Tables — structural shape + their seats */}
      <Layer>
        {(layout.tables ?? []).map((t) => (
          <TableShape
            key={t.id}
            table={t}
            availabilityById={availabilityById}
            eligibleSeatIds={eligibleSeatIds}
            selectedSeatIds={selectedSeatIds}
            onSeatClick={onSeatClick}
          />
        ))}
      </Layer>
    </Stage>
  );
}

export default SeatPickerKonva;

// ─────────────────────────── Zones ───────────────────────────

interface ZoneShapeProps {
  zone: VenueZoneDto;
  availabilityById: Map<string, SeatAvailabilityDto>;
  eligibleSeatIds?: ReadonlySet<string>;
  selectedSeatIds?: ReadonlySet<string>;
  onSeatClick?: (seatId: string) => void;
}

function ZoneShape(props: ZoneShapeProps) {
  switch (props.zone.shape) {
    case 'Curve':
      return <CurveZone {...props} />;
    case 'Polygon':
      return <ZonePlaceholder zone={props.zone} />;
    case 'Rect':
    default:
      return <RectZone {...props} />;
  }
}

function RectZone({
  zone,
  availabilityById,
  eligibleSeatIds,
  selectedSeatIds,
  onSeatClick,
}: ZoneShapeProps) {
  const geom = parseRectGeom(zone.geometry);
  if (!geom) return <ZonePlaceholder zone={zone} />;
  const labelFontSize = Math.max(14, Math.min(26, geom.width / 24));
  const rotationOrigin =
    geom.rotation ? { x: geom.x + geom.width / 2, y: geom.y + geom.height / 2 } : null;

  const seatPoints = useMemo(
    () => computeRectZoneSeatPositions(zone.seats, geom),
    [zone.seats, geom.x, geom.y, geom.width, geom.height],
  );
  const seatStates = useMemo(
    () => mergeZoneSeatStates(zone, availabilityById),
    [zone, availabilityById],
  );

  return (
    <Group rotation={geom.rotation ?? 0} x={rotationOrigin?.x ?? 0} y={rotationOrigin?.y ?? 0}>
      <Rect
        x={geom.x - (rotationOrigin?.x ?? 0)}
        y={geom.y - (rotationOrigin?.y ?? 0)}
        width={geom.width}
        height={geom.height}
        cornerRadius={6}
        fill={zone.color}
        opacity={0.18}
        stroke={zone.color}
        strokeWidth={3}
      />
      {zone.name && (
        <Text
          x={geom.x - (rotationOrigin?.x ?? 0)}
          y={geom.y - (rotationOrigin?.y ?? 0) + Math.min(12, geom.height / 2)}
          width={geom.width}
          align="center"
          text={zone.name}
          fontSize={labelFontSize}
          fontStyle="bold"
          fontFamily="Inter, system-ui, sans-serif"
          fill={zone.color}
        />
      )}
      <SeatDots
        points={seatPoints}
        states={seatStates}
        eligibleSeatIds={eligibleSeatIds}
        selectedSeatIds={selectedSeatIds}
        onSeatClick={onSeatClick}
        originShift={rotationOrigin}
      />
    </Group>
  );
}

function CurveZone({ zone }: ZoneShapeProps) {
  const geom = parseCurveGeom(zone.geometry);
  if (!geom) return <ZonePlaceholder zone={zone} />;
  const { centerX, centerY, radius, startAngleDeg, sweepAngleDeg } = geom;
  const toRad = (d: number) => (d * Math.PI) / 180;
  const endAngleDeg = startAngleDeg + sweepAngleDeg;
  const start = {
    x: centerX + radius * Math.cos(toRad(startAngleDeg)),
    y: centerY + radius * Math.sin(toRad(startAngleDeg)),
  };
  const end = {
    x: centerX + radius * Math.cos(toRad(endAngleDeg)),
    y: centerY + radius * Math.sin(toRad(endAngleDeg)),
  };
  const largeArc = Math.abs(sweepAngleDeg) > 180 ? 1 : 0;
  const sweepFlag = sweepAngleDeg >= 0 ? 1 : 0;
  const d = `M ${centerX} ${centerY} L ${start.x} ${start.y} A ${radius} ${radius} 0 ${largeArc} ${sweepFlag} ${end.x} ${end.y} Z`;
  return (
    <Path
      data={d}
      fill={zone.color}
      opacity={0.25}
      stroke={zone.color}
      strokeWidth={3}
    />
  );
}

function ZonePlaceholder({ zone }: { zone: VenueZoneDto }) {
  return (
    <Text
      x={40}
      y={40}
      text={zone.name}
      fontSize={16}
      fontFamily="Inter, system-ui, sans-serif"
      fill="#9ca3af"
    />
  );
}

// ─────────────────────────── Tables ───────────────────────────

interface TableShapeProps {
  table: VenueTableDto;
  availabilityById: Map<string, SeatAvailabilityDto>;
  eligibleSeatIds?: ReadonlySet<string>;
  selectedSeatIds?: ReadonlySet<string>;
  onSeatClick?: (seatId: string) => void;
}

function TableShape(props: TableShapeProps) {
  if (props.table.shape === 'Round') {
    const geom = parseRoundTableGeom(props.table.geometry);
    if (!geom) return null;
    return <RoundTable {...props} geom={geom} />;
  }
  const geom = parseRectTableGeom(props.table.geometry);
  if (!geom) return null;
  const { table, availabilityById, eligibleSeatIds, selectedSeatIds, onSeatClick } = props;
  const x = geom.centerX - geom.width / 2;
  const y = geom.centerY - geom.height / 2;

  const seatPoints = useMemo(
    () => computeRectTableSeatPositions(table.seats, geom),
    [table.seats, geom.centerX, geom.centerY, geom.width, geom.height],
  );
  const seatStates = useMemo(
    () => mergeTableSeatStates(table, availabilityById),
    [table, availabilityById],
  );
  return (
    <Group>
      <Group
        rotation={geom.rotation ?? 0}
        x={geom.rotation ? geom.centerX : 0}
        y={geom.rotation ? geom.centerY : 0}
      >
        <Rect
          x={geom.rotation ? -geom.width / 2 : x}
          y={geom.rotation ? -geom.height / 2 : y}
          width={geom.width}
          height={geom.height}
          cornerRadius={4}
          fill="#fee2e2"
          stroke="#b91c1c"
          strokeWidth={2}
        />
        <Text
          x={geom.rotation ? -geom.width / 2 : x}
          y={(geom.rotation ? -geom.height / 2 : y) + geom.height / 2 - 7}
          width={geom.width}
          align="center"
          text={table.label}
          fontSize={14}
          fontStyle="bold"
          fontFamily="Inter, system-ui, sans-serif"
          fill="#7f1d1d"
        />
      </Group>
      {/* Seats are drawn in the outer (unrotated) Group so they sit flat
          on the canvas even when the table is rotated — matches common
          venue-map conventions (seats readable regardless of table angle). */}
      <SeatDots
        points={seatPoints}
        states={seatStates}
        eligibleSeatIds={eligibleSeatIds}
        selectedSeatIds={selectedSeatIds}
        onSeatClick={onSeatClick}
      />
    </Group>
  );
}

function RoundTable({
  table,
  availabilityById,
  eligibleSeatIds,
  selectedSeatIds,
  onSeatClick,
  geom,
}: TableShapeProps & { geom: RoundTableGeom }) {
  const seatPoints = useMemo(
    () => computeRoundTableSeatPositions(
      table.seats.map((s) => ({ id: s.id, number: s.number, angleDeg: null })),
      geom,
    ),
    [table.seats, geom.centerX, geom.centerY, geom.radius],
  );
  const seatStates = useMemo(
    () => mergeTableSeatStates(table, availabilityById),
    [table, availabilityById],
  );
  return (
    <Group>
      <Circle
        x={geom.centerX}
        y={geom.centerY}
        radius={geom.radius}
        fill="#fde68a"
        stroke="#b45309"
        strokeWidth={2}
      />
      <Text
        x={geom.centerX - geom.radius}
        y={geom.centerY - 7}
        width={geom.radius * 2}
        align="center"
        text={table.label}
        fontSize={14}
        fontStyle="bold"
        fontFamily="Inter, system-ui, sans-serif"
        fill="#92400e"
      />
      <SeatDots
        points={seatPoints}
        states={seatStates}
        eligibleSeatIds={eligibleSeatIds}
        selectedSeatIds={selectedSeatIds}
        onSeatClick={onSeatClick}
      />
    </Group>
  );
}

// ─────────────────────────── Seats ───────────────────────────

function mergeZoneSeatStates(
  zone: VenueZoneDto,
  availabilityById: Map<string, SeatAvailabilityDto>,
): SeatRenderState[] {
  return zone.seats.map((s) => seatStateFromZoneSeat(s, zone, availabilityById));
}

function mergeTableSeatStates(
  table: VenueTableDto,
  availabilityById: Map<string, SeatAvailabilityDto>,
): SeatRenderState[] {
  return table.seats.map((s) => seatStateFromTableSeat(s, table, availabilityById));
}

function seatStateFromZoneSeat(
  s: SeatDto,
  zone: VenueZoneDto,
  availabilityById: Map<string, SeatAvailabilityDto>,
): SeatRenderState {
  const a = availabilityById.get(s.id);
  return {
    id: s.id,
    row: s.row,
    number: s.number,
    label: s.label,
    isEnabled: s.isEnabled,
    status: !s.isEnabled ? 'Disabled' : (a?.status as SeatStatus | undefined) ?? 'Available',
    zoneId: zone.id,
    zoneColor: zone.color,
  };
}

function seatStateFromTableSeat(
  s: SeatDto,
  table: VenueTableDto,
  availabilityById: Map<string, SeatAvailabilityDto>,
): SeatRenderState {
  const a = availabilityById.get(s.id);
  return {
    id: s.id,
    row: s.row,
    number: s.number,
    label: s.label,
    isEnabled: s.isEnabled,
    status: !s.isEnabled ? 'Disabled' : (a?.status as SeatStatus | undefined) ?? 'Available',
    tableId: table.id,
    // Table seats reuse the zone-colored palette when they're zone-scoped
    // through tier assignments; the plain table case uses the table stroke.
    zoneColor: a?.zoneColor ?? '#b45309',
  };
}

/**
 * Render a list of seats as Konva Circles with status-aware fill, stroke,
 * opacity, and click handling. Used by both zone + table paths so color and
 * interaction semantics stay identical across the picker.
 *
 * Interaction rules:
 *   - Disabled / Reserved / tier-filtered → non-selectable, no pointer cursor.
 *   - Held by someone else (no selectedSeatIds hit) → amber, not selectable.
 *     (Held-by-us is represented as Selected in our render model.)
 *   - Available + (eligible or no filter) → clickable; pointer cursor.
 *   - Selected → emerald, still clickable so users can deselect.
 */
function SeatDots({
  points,
  states,
  eligibleSeatIds,
  selectedSeatIds,
  onSeatClick,
  originShift,
}: {
  points: ComputedSeatPoint[];
  states: SeatRenderState[];
  eligibleSeatIds?: ReadonlySet<string>;
  selectedSeatIds?: ReadonlySet<string>;
  onSeatClick?: (seatId: string) => void;
  originShift?: { x: number; y: number } | null;
}) {
  if (points.length === 0 || states.length === 0) return null;
  const stateById = new Map(states.map((s) => [s.id, s]));

  const ox = originShift?.x ?? 0;
  const oy = originShift?.y ?? 0;

  return (
    <Group>
      {points.map((p) => {
        const state = stateById.get(p.seatId);
        if (!state) return null;
        const isSelected = selectedSeatIds?.has(p.seatId) === true;
        const isEligible = eligibleSeatIds ? eligibleSeatIds.has(p.seatId) : true;
        const { fill, stroke, opacity, selectable } = resolveSeatStyle(
          state,
          isSelected,
          isEligible,
        );

        const handleClick = selectable && onSeatClick
          ? () => onSeatClick(p.seatId)
          : undefined;

        const stageCursor = (visible: boolean) => (e: { target: { getStage: () => { container: () => HTMLDivElement } | null } }) => {
          const stage = e.target.getStage();
          if (stage) stage.container().style.cursor = visible ? 'pointer' : 'default';
        };

        return (
          <Circle
            key={p.seatId}
            x={p.x - ox}
            y={p.y - oy}
            radius={p.r}
            fill={fill}
            stroke={stroke}
            strokeWidth={isSelected ? 2 : 1}
            opacity={opacity}
            listening={selectable || isSelected}
            onClick={handleClick}
            onTap={handleClick}
            onMouseEnter={selectable ? stageCursor(true) : undefined}
            onMouseLeave={selectable ? stageCursor(false) : undefined}
            data-testid={`seat-${p.seatId}`}
          />
        );
      })}
    </Group>
  );
}

function resolveSeatStyle(
  state: SeatRenderState,
  isSelected: boolean,
  isEligible: boolean,
): { fill: string; stroke: string; opacity: number; selectable: boolean } {
  if (isSelected) {
    return {
      fill: SEAT_COLORS.selected,
      stroke: SEAT_COLORS.selectedStroke,
      opacity: 1,
      selectable: true,
    };
  }
  if (!state.isEnabled || state.status === 'Disabled') {
    return { fill: SEAT_COLORS.disabled, stroke: '#9ca3af', opacity: 0.5, selectable: false };
  }
  if (state.status === 'Reserved') {
    return { fill: SEAT_COLORS.reserved, stroke: '#b91c1c', opacity: 0.85, selectable: false };
  }
  if (state.status === 'Held') {
    return { fill: SEAT_COLORS.held, stroke: '#b45309', opacity: 0.85, selectable: false };
  }
  if (!isEligible) {
    return { fill: SEAT_COLORS.filtered, stroke: '#9ca3af', opacity: 0.6, selectable: false };
  }
  // Available + eligible.
  return { fill: state.zoneColor, stroke: state.zoneColor, opacity: 0.85, selectable: true };
}

// ─────────────────────────── Decorations ───────────────────────────

function DecorationShape({ decoration }: { decoration: VenueDecorationDto }) {
  const geom = parseRectGeom(decoration.geometry);
  if (!geom) return null;
  const style = decorationStyle(decoration.kind);
  const rotationOrigin =
    geom.rotation ? { x: geom.x + geom.width / 2, y: geom.y + geom.height / 2 } : null;
  const labelText = decoration.label ?? style.label;
  return (
    <Group rotation={geom.rotation ?? 0} x={rotationOrigin?.x ?? 0} y={rotationOrigin?.y ?? 0}>
      <Rect
        x={geom.x - (rotationOrigin?.x ?? 0)}
        y={geom.y - (rotationOrigin?.y ?? 0)}
        width={geom.width}
        height={geom.height}
        cornerRadius={decoration.kind === 'Stage' ? 10 : 4}
        fill={style.fill}
        stroke={style.stroke}
        strokeWidth={2}
        dash={decoration.kind === 'DanceFloor' ? [10, 6] : undefined}
      />
      {labelText && (
        <Text
          x={geom.x - (rotationOrigin?.x ?? 0)}
          y={geom.y - (rotationOrigin?.y ?? 0) + geom.height / 2 - 10}
          width={geom.width}
          align="center"
          text={labelText}
          fontSize={Math.max(14, Math.min(28, geom.width / 22))}
          fontStyle="bold"
          fontFamily="Inter, system-ui, sans-serif"
          fill={style.labelColor}
        />
      )}
    </Group>
  );
}
