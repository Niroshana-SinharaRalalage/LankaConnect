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
}: {
  layout: VenueLayoutDto;
  selected: CanvasItemRef;
  draftGeometryByKey: Record<string, string>;
  onGeometryChange: (ref: CanvasItemRef, geometryJson: string) => void;
  tiers?: TicketTierDto[];
  tiersLoading?: boolean;
  draftTierAssignmentsByKey?: Record<string, string[]>;
  onToggleTierAssignment?: (ref: CanvasItemRef, tierId: string) => void;
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
