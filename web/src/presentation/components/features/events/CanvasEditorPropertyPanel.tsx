/**
 * Slice 8 S8.5a: CanvasEditorPropertyPanel.
 *
 * Right-hand property panel that surfaces the selected shape's editable
 * dimensions. Today this covers geometry only (width / height / rotation
 * for rect-geometry shapes; radius for round tables). Non-geometry fields
 * (zone name, color, table label, decoration label) land in S8.5b
 * alongside the add-item toolbar.
 *
 * Fully controlled: the panel never maintains its own "draft" state — each
 * number input dispatches an onGeometryChange as soon as it blurs / the
 * user hits Enter. Reuses the draft model from S8.3 (CanvasEditor owns
 * `draftGeometryByKey`); the panel emits a new geometry JSON and the
 * parent appends it to the draft. This keeps the data flow identical
 * whether the change came from a drag, a Transformer handle, or a number
 * input — so S8.6 undo/redo will only need to subscribe to one stream.
 *
 * Accessibility:
 * - Each input has a <label> and an id so screen readers announce the
 *   relationship.
 * - Enter submits the value; tab/blur also commits.
 * - When nothing is selected the panel renders a helpful empty state.
 */

'use client';

import React, { useEffect, useState } from 'react';
import {
  applyResizeToGeometry,
  applyRadiusToGeometry,
  applyRotationToGeometry,
  readGeometryDimensions,
  refKey,
  resolveGeometry,
  snapToGrid,
  snapRotation,
  MIN_SHAPE_DIMENSION,
  type CanvasItemRef,
} from '@/presentation/utils/canvasEditorGeometry';
import type {
  TicketTierDto,
  VenueLayoutDto,
  VenueZoneDto,
  VenueTableDto,
  VenueDecorationDto,
} from '@/infrastructure/api/types/events.types';
import { CanvasEditorTierPanel } from './CanvasEditorTierPanel';
import { resolveTierAssignments } from '@/presentation/utils/canvasEditorGeometry';

const MIN_RADIUS = MIN_SHAPE_DIMENSION / 2;

export interface CanvasEditorPropertyPanelProps {
  layout: VenueLayoutDto;
  selected: CanvasItemRef | null;
  draftGeometryByKey: Record<string, string>;
  onGeometryChange: (ref: CanvasItemRef, geometryJson: string) => void;
  /** Slice 8 S8.7: tier-assignment props. All optional so the panel
   * degrades gracefully when the editor is rendered without tier data. */
  tiers?: TicketTierDto[];
  tiersLoading?: boolean;
  draftTierAssignmentsByKey?: Record<string, string[]>;
  onToggleTierAssignment?: (ref: CanvasItemRef, tierId: string) => void;
  /**
   * Slice 9.5: seat-generation overrides for zones, keyed by zone id.
   * The panel surfaces a Rows + Seats-per-row subsection whenever a zone
   * is selected AND it currently has zero seats (regenerating a populated
   * zone is destructive — gated by the UI to require an explicit empty
   * state). Optional so older callers degrade gracefully.
   */
  seatGenByZoneId?: Record<string, { rowCount: number; seatsPerRow: number }>;
  onSeatGenChange?: (
    zoneId: string,
    next: { rowCount: number; seatsPerRow: number } | null,
  ) => void;
  className?: string;
}

function findItem(
  layout: VenueLayoutDto,
  ref: CanvasItemRef,
): VenueZoneDto | VenueTableDto | VenueDecorationDto | null {
  if (ref.kind === 'zone') return layout.zones?.find((z) => z.id === ref.id) ?? null;
  if (ref.kind === 'table') return layout.tables?.find((t) => t.id === ref.id) ?? null;
  return layout.decorations?.find((d) => d.id === ref.id) ?? null;
}

function getShapeHint(
  ref: CanvasItemRef,
  item: VenueZoneDto | VenueTableDto | VenueDecorationDto,
): string | undefined {
  if (ref.kind === 'zone') return (item as VenueZoneDto).shape;
  if (ref.kind === 'table') return (item as VenueTableDto).shape;
  return undefined;
}

function friendlyLabel(
  ref: CanvasItemRef,
  item: VenueZoneDto | VenueTableDto | VenueDecorationDto,
): string {
  if (ref.kind === 'zone') return (item as VenueZoneDto).name;
  if (ref.kind === 'table') return (item as VenueTableDto).label;
  const d = item as VenueDecorationDto;
  return d.label ?? d.kind;
}

export function CanvasEditorPropertyPanel({
  layout,
  selected,
  draftGeometryByKey,
  onGeometryChange,
  tiers,
  tiersLoading,
  draftTierAssignmentsByKey,
  onToggleTierAssignment,
  seatGenByZoneId,
  onSeatGenChange,
  className,
}: CanvasEditorPropertyPanelProps) {
  return (
    <aside
      className={
        className ??
        'w-72 border-l border-neutral-200 bg-white overflow-y-auto flex flex-col'
      }
      data-testid="canvas-editor-property-panel"
      aria-label="Selected item properties"
    >
      <div className="px-4 py-3 border-b border-neutral-200">
        <p className="text-sm font-semibold text-neutral-900">Properties</p>
        <p className="text-xs text-neutral-500 mt-0.5">
          {selected ? 'Edit the selected item.' : 'Select a shape to edit it.'}
        </p>
      </div>

      <div className="flex-1 px-4 py-3">
        {!selected && (
          <p
            className="text-sm text-neutral-500"
            data-testid="property-panel-empty"
          >
            Click a zone, table, or decoration on the canvas to see its
            properties here.
          </p>
        )}
        {selected && (
          <SelectedItemEditor
            layout={layout}
            selected={selected}
            draftGeometryByKey={draftGeometryByKey}
            onGeometryChange={onGeometryChange}
            tiers={tiers}
            tiersLoading={tiersLoading}
            draftTierAssignmentsByKey={draftTierAssignmentsByKey}
            onToggleTierAssignment={onToggleTierAssignment}
            seatGenByZoneId={seatGenByZoneId}
            onSeatGenChange={onSeatGenChange}
          />
        )}
      </div>
    </aside>
  );
}

function SelectedItemEditor({
  layout,
  selected,
  draftGeometryByKey,
  onGeometryChange,
  tiers,
  tiersLoading,
  draftTierAssignmentsByKey,
  onToggleTierAssignment,
  seatGenByZoneId,
  onSeatGenChange,
}: {
  layout: VenueLayoutDto;
  selected: CanvasItemRef;
  draftGeometryByKey: Record<string, string>;
  onGeometryChange: (ref: CanvasItemRef, geometryJson: string) => void;
  tiers?: TicketTierDto[];
  tiersLoading?: boolean;
  draftTierAssignmentsByKey?: Record<string, string[]>;
  onToggleTierAssignment?: (ref: CanvasItemRef, tierId: string) => void;
  seatGenByZoneId?: Record<string, { rowCount: number; seatsPerRow: number }>;
  onSeatGenChange?: (
    zoneId: string,
    next: { rowCount: number; seatsPerRow: number } | null,
  ) => void;
}) {
  const item = findItem(layout, selected);
  if (!item) {
    return (
      <p className="text-sm text-amber-700" data-testid="property-panel-missing">
        That item is no longer on the canvas.
      </p>
    );
  }

  const shapeHint = getShapeHint(selected, item);
  const geometry = resolveGeometry(
    selected.kind,
    item as { id: string; geometry?: string | null },
    draftGeometryByKey,
  );
  const dims = readGeometryDimensions(selected.kind, geometry, shapeHint);

  if (!dims) {
    return (
      <p className="text-sm text-amber-700" data-testid="property-panel-bad-geometry">
        This item&apos;s geometry is invalid and can&apos;t be edited here.
      </p>
    );
  }

  const isRound = selected.kind === 'table' && shapeHint === 'Round';
  const isCurve = selected.kind === 'zone' && shapeHint === 'Curve';
  const canEditRect = !isRound && !isCurve;
  const canEditRotation = canEditRect;

  const handleWidthCommit = (raw: string) => {
    const parsed = Number(raw);
    if (!Number.isFinite(parsed)) return;
    const newWidth = Math.max(MIN_SHAPE_DIMENSION, snapToGrid(parsed));
    const newGeom = applyResizeToGeometry(
      selected.kind,
      geometry,
      { width: newWidth, height: dims.height ?? MIN_SHAPE_DIMENSION },
      shapeHint,
    );
    if (newGeom) onGeometryChange(selected, newGeom);
  };

  const handleHeightCommit = (raw: string) => {
    const parsed = Number(raw);
    if (!Number.isFinite(parsed)) return;
    const newHeight = Math.max(MIN_SHAPE_DIMENSION, snapToGrid(parsed));
    const newGeom = applyResizeToGeometry(
      selected.kind,
      geometry,
      { width: dims.width ?? MIN_SHAPE_DIMENSION, height: newHeight },
      shapeHint,
    );
    if (newGeom) onGeometryChange(selected, newGeom);
  };

  const handleRotationCommit = (raw: string) => {
    const parsed = Number(raw);
    if (!Number.isFinite(parsed)) return;
    const snapped = snapRotation(parsed);
    const newGeom = applyRotationToGeometry(
      selected.kind,
      geometry,
      snapped,
      shapeHint,
    );
    if (newGeom) onGeometryChange(selected, newGeom);
  };

  const handleRadiusCommit = (raw: string) => {
    const parsed = Number(raw);
    if (!Number.isFinite(parsed)) return;
    const newRadius = Math.max(MIN_RADIUS, snapToGrid(parsed));
    const newGeom = applyRadiusToGeometry(geometry, newRadius);
    if (newGeom) onGeometryChange(selected, newGeom);
  };

  return (
    <div className="space-y-4">
      <div>
        <p className="text-xs uppercase tracking-wide text-neutral-500">
          {selected.kind}
        </p>
        <p
          className="text-sm font-medium text-neutral-900 mt-0.5"
          data-testid="property-panel-item-label"
        >
          {friendlyLabel(selected, item)}
        </p>
      </div>

      {isCurve && (
        <p
          className="text-sm text-neutral-600 bg-neutral-50 border border-neutral-200 rounded-md p-3"
          data-testid="property-panel-curve-hint"
        >
          Curve geometry uses a center, radius, and sweep angle. Editing is
          coming in a future release — for now, drag the zone to reposition.
        </p>
      )}

      {canEditRect && (
        <>
          <NumberField
            id="prop-width"
            label="Width"
            value={dims.width ?? 0}
            step={50}
            min={MIN_SHAPE_DIMENSION}
            suffix="px"
            onCommit={handleWidthCommit}
          />
          <NumberField
            id="prop-height"
            label="Height"
            value={dims.height ?? 0}
            step={50}
            min={MIN_SHAPE_DIMENSION}
            suffix="px"
            onCommit={handleHeightCommit}
          />
        </>
      )}

      {isRound && (
        <NumberField
          id="prop-radius"
          label="Radius"
          value={dims.radius ?? 0}
          step={50}
          min={MIN_RADIUS}
          suffix="px"
          onCommit={handleRadiusCommit}
        />
      )}

      {canEditRotation && (
        <NumberField
          id="prop-rotation"
          label="Rotation"
          value={dims.rotation ?? 0}
          step={15}
          min={0}
          max={345}
          suffix="°"
          onCommit={handleRotationCommit}
        />
      )}

      {/* Slice 8 S8.7: tier mapping for zones + tables only (decorations
          have no tier concept). Hidden when the caller didn't wire the
          tier handlers so the panel stays backward-compatible. */}
      {(selected.kind === 'zone' || selected.kind === 'table') &&
        onToggleTierAssignment && (
          <CanvasEditorTierPanel
            tiers={tiers ?? []}
            tiersLoading={tiersLoading}
            isTemplateLayout={!layout.eventId}
            assignedTierIds={resolveTierAssignments(
              selected.kind,
              item as { id: string; ticketTierIds?: string[] | null },
              draftTierAssignmentsByKey ?? {},
            )}
            onToggleTier={(tierId) => onToggleTierAssignment(selected, tierId)}
          />
        )}

      {/* Slice 9.5: theater-style seat generation for zones with no seats.
          Surfaced only when the selected zone is empty (existing seats are
          read from `item.totalSeatCount`) — regenerating populated zones is
          destructive and the UI gates that path. */}
      {selected.kind === 'zone' && onSeatGenChange && (
        <ZoneSeatGenerationSection
          zone={item as VenueZoneDto}
          seatGen={seatGenByZoneId?.[selected.id] ?? null}
          onChange={(next) => onSeatGenChange(selected.id, next)}
        />
      )}
    </div>
  );
}

interface ZoneSeatGenerationSectionProps {
  zone: VenueZoneDto;
  seatGen: { rowCount: number; seatsPerRow: number } | null;
  onChange: (next: { rowCount: number; seatsPerRow: number } | null) => void;
}

/**
 * Slice 9.5 — seat-generation UI for empty zones. Two number inputs for
 * rows + seats-per-row. The handler emits a per-zone override into the
 * canvas-editor draft state (`seatGenByZoneId`) which the
 * <c>composeBatchPayload</c> forwards to the backend's
 * <c>VenueLayout.GenerateTheaterSeats</c> on save.
 *
 * For zones that already have seats we render a hint instead of inputs —
 * regenerating is destructive (clears existing seats) and we don't expose
 * that path in this slice. A future "Regenerate seats" button can be
 * added with an explicit confirmation dialog if organisers need it.
 */
function ZoneSeatGenerationSection({
  zone,
  seatGen,
  onChange,
}: ZoneSeatGenerationSectionProps) {
  const existingSeats = zone.totalSeatCount ?? zone.seats?.length ?? 0;

  if (existingSeats > 0) {
    return (
      <div
        className="rounded-md border border-neutral-200 bg-neutral-50 p-3 text-xs text-neutral-600"
        data-testid="property-panel-seat-gen-existing"
      >
        This zone already has {existingSeats} seats. Editing seat layout for
        an existing zone is coming in a future release.
      </div>
    );
  }

  // Slice S1 (Architect Rev 4): per-input commits MUST preserve the partner
  // value. Old code called onChange(null) when the current input was 0 or
  // empty, which combined with the over-eager pruner in CanvasEditor.tsx
  // wiped any previously-typed value. Now: only clear the full entry when
  // BOTH fields are explicitly empty / non-positive. Partial state lives in
  // the draft; composeBatchPayload prunes at save time.
  const handleRowsCommit = (raw: string) => {
    const trimmed = raw.trim();
    const partner = seatGen?.seatsPerRow ?? 0;
    if (trimmed === '') {
      // User cleared the field — full-clear only if partner is also empty.
      if (partner <= 0) onChange(null);
      else onChange({ rowCount: 0, seatsPerRow: partner });
      return;
    }
    const parsed = Number(trimmed);
    if (!Number.isFinite(parsed) || parsed <= 0) {
      // Treat as cleared (same logic as empty input).
      if (partner <= 0) onChange(null);
      else onChange({ rowCount: 0, seatsPerRow: partner });
      return;
    }
    onChange({ rowCount: Math.floor(parsed), seatsPerRow: partner });
  };

  const handleSeatsPerRowCommit = (raw: string) => {
    const trimmed = raw.trim();
    const partner = seatGen?.rowCount ?? 0;
    if (trimmed === '') {
      if (partner <= 0) onChange(null);
      else onChange({ rowCount: partner, seatsPerRow: 0 });
      return;
    }
    const parsed = Number(trimmed);
    if (!Number.isFinite(parsed) || parsed <= 0) {
      if (partner <= 0) onChange(null);
      else onChange({ rowCount: partner, seatsPerRow: 0 });
      return;
    }
    onChange({ rowCount: partner, seatsPerRow: Math.floor(parsed) });
  };

  const totalSeats =
    (seatGen?.rowCount ?? 0) > 0 && (seatGen?.seatsPerRow ?? 0) > 0
      ? (seatGen?.rowCount ?? 0) * (seatGen?.seatsPerRow ?? 0)
      : 0;

  return (
    <div
      className="space-y-3 pt-3 border-t border-neutral-200"
      data-testid="property-panel-seat-gen"
    >
      <div>
        <p className="text-xs uppercase tracking-wide text-neutral-500">Seats</p>
        <p className="text-xs text-neutral-500 mt-0.5">
          Lay out theater-style seats in this zone.
        </p>
      </div>
      <NumberField
        id="prop-zone-rows"
        label="Rows"
        value={seatGen?.rowCount ?? 0}
        step={1}
        min={0}
        max={100}
        onCommit={handleRowsCommit}
      />
      <NumberField
        id="prop-zone-seats-per-row"
        label="Seats per row"
        value={seatGen?.seatsPerRow ?? 0}
        step={1}
        min={0}
        max={100}
        onCommit={handleSeatsPerRowCommit}
      />
      {totalSeats > 0 && (
        <p
          className="text-xs text-neutral-700 bg-primary-50/50 border border-primary-200 rounded-md px-2 py-1.5"
          data-testid="property-panel-seat-gen-total"
        >
          {totalSeats} seats will be generated on Save.
        </p>
      )}
    </div>
  );
}

interface NumberFieldProps {
  id: string;
  label: string;
  value: number;
  step: number;
  min?: number;
  max?: number;
  suffix?: string;
  onCommit: (raw: string) => void;
}

function NumberField({
  id,
  label,
  value,
  step,
  min,
  max,
  suffix,
  onCommit,
}: NumberFieldProps) {
  const [local, setLocal] = useState<string>(String(value));

  // Sync from prop when the underlying geometry changes externally (drag,
  // transformer, another panel edit). We only overwrite local state if the
  // user isn't currently typing — which we approximate by comparing the
  // numeric round-trip.
  useEffect(() => {
    const numericLocal = Number(local);
    if (!Number.isFinite(numericLocal) || numericLocal !== value) {
      setLocal(String(value));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [value]);

  const commit = () => {
    if (String(value) !== local) onCommit(local);
  };

  return (
    <div>
      <label
        htmlFor={id}
        className="block text-xs font-medium text-neutral-700 mb-1"
      >
        {label}
        {suffix && <span className="text-neutral-400 ml-1">({suffix})</span>}
      </label>
      <input
        id={id}
        type="number"
        className="block w-full rounded-md border border-neutral-300 px-2 py-1.5 text-sm text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent"
        value={local}
        step={step}
        min={min}
        max={max}
        onChange={(e) => setLocal(e.target.value)}
        onBlur={commit}
        onKeyDown={(e) => {
          if (e.key === 'Enter') {
            e.preventDefault();
            commit();
            (e.target as HTMLInputElement).blur();
          }
        }}
        data-testid={`property-panel-${id}`}
      />
    </div>
  );
}

export default CanvasEditorPropertyPanel;
