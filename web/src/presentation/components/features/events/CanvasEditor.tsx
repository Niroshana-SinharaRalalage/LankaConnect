/**
 * Slice 8 S8.2–S8.6: CanvasEditor — SSR-safe wrapper + editor state + history.
 *
 * Konva touches `window` at import time, so the interactive stage lives in
 * {@link CanvasEditorStage} and we lazy-load it via `next/dynamic` with
 * `ssr: false`. This file is the only module pages / modals should import.
 *
 * S8.6 consolidates the three draft slices (geometry overrides, additions,
 * deletions) into a single `DraftState` managed by `useEditorHistory`, a
 * bounded undo/redo stack with a 50-step architect-spec cap. Every
 * state-mutating path — add, delete, drag, transform, property-panel edit,
 * arrow-key nudge — routes through one `commit(reducer)` call so undo/redo
 * covers the full surface.
 *
 * Keyboard shortcuts registered while the editor is mounted:
 *   Ctrl/Cmd+Z        undo
 *   Ctrl/Cmd+Shift+Z  redo
 *   Ctrl/Cmd+Y        redo (Windows convention)
 *   Delete / Backspace delete the selected item
 *   Arrow keys        nudge the selected item (10px; Shift = 1px fine)
 * Shortcuts are suppressed while focus is inside an input / textarea /
 * select / contenteditable so typing in the property panel feels natural.
 *
 * Selection is NOT part of the history stack — standard design-tool UX.
 * After an undo/redo leaves the selected item outside the effective
 * layout, a useEffect clears selection.
 */

'use client';

import React, { useCallback, useEffect, useMemo, useState } from 'react';
import dynamic from 'next/dynamic';
import type {
  DecorationKind,
  VenueLayoutDto,
  VenueZoneDto,
  VenueTableDto,
  VenueDecorationDto,
} from '@/infrastructure/api/types/events.types';
import type { CanvasEditorStageProps } from './CanvasEditorStage';
import { CanvasEditorPropertyPanel } from './CanvasEditorPropertyPanel';
import { CanvasEditorToolbar } from './CanvasEditorToolbar';
import {
  applyDragToGeometry,
  composeBatchPayload,
  countDraftChanges,
  createDecorationDraft,
  createRectTableDraft,
  createRoundTableDraft,
  createZoneDraft,
  itemCenter,
  refKey,
  resolveGeometry,
  resolveTierAssignments,
  toggleTierAssignment,
  type CanvasEditorDraftState,
  type CanvasItemRef,
} from '@/presentation/utils/canvasEditorGeometry';
import type { BatchLayoutPayload } from '@/infrastructure/api/types/events.types';
import { useEditorHistory } from '@/presentation/hooks/useEditorHistory';
import { useTicketTiers } from '@/presentation/hooks/useTicketTiers';

const CanvasEditorStage = dynamic<CanvasEditorStageProps>(
  () => import('./CanvasEditorStage').then((m) => m.CanvasEditorStage),
  {
    ssr: false,
    loading: () => (
      <div
        className="flex items-center justify-center h-full text-sm text-neutral-500"
        data-testid="canvas-editor-loading"
      >
        Loading editor…
      </div>
    ),
  },
);

/**
 * Slice 8 S8.8b: summary the editor pushes to its parent (Modal) after
 * every draft mutation so the parent can gate the Save button + invoke
 * the batch save without owning the history reducer.
 *
 *   `hasChanges`        — false when the draft is identical to the baseline
 *                         layout (Save button stays disabled).
 *   `changesCount`      — user-perceived change count for the Save label /
 *                         confirmation copy. See `countDraftChanges`.
 *   `composeSavePayload`— closure capturing the *current* draft. Calling it
 *                         later returns a fresh `BatchLayoutPayload`. The
 *                         parent should call this on Save click, not store
 *                         the payload upfront, so undo/redo right before
 *                         Save reflects in the request body.
 */
export interface CanvasEditorDraftSummary {
  hasChanges: boolean;
  changesCount: number;
  composeSavePayload: () => BatchLayoutPayload;
}

export interface CanvasEditorProps {
  layout: VenueLayoutDto;
  className?: string;
  /**
   * Slice 8 S8.8b: invoked whenever the editor's draft changes (including
   * undo/redo, add/delete, drag, resize, rotate, property-panel edit, and
   * tier-assignment toggle). Idempotent on identical drafts. The parent
   * uses this to gate the Save button in the modal footer.
   */
  onDraftChange?: (summary: CanvasEditorDraftSummary) => void;
}

type DraftState = CanvasEditorDraftState;

const INITIAL_DRAFT: DraftState = {
  geometryByKey: {},
  additions: { zones: [], tables: [], decorations: [] },
  deletions: new Set<string>(),
  tierAssignmentsByKey: {},
  seatGenByZoneId: {},
};

const DEFAULT_CANVAS_WIDTH = 1000;
const DEFAULT_CANVAS_HEIGHT = 800;

// Arrow-key nudge distances. The default step is half a grid cell so users
// can step off the snap rails while still landing on a clean number;
// Shift = 1px for precision tweaks.
const NUDGE_STEP = 10;
const NUDGE_STEP_FINE = 1;

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

function isTypingTarget(el: Element | null): boolean {
  if (!el) return false;
  const tag = el.tagName;
  if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return true;
  if ((el as HTMLElement).isContentEditable) return true;
  return false;
}

export function CanvasEditor({ layout, className, onDraftChange }: CanvasEditorProps) {
  const [selected, setSelected] = useState<CanvasItemRef | null>(null);
  const history = useEditorHistory<DraftState>(INITIAL_DRAFT);
  const draft = history.present;

  // S8.8b: surface the draft summary up to the modal so it can gate Save.
  // Recomputed on every draft / layout change. The composer closure captures
  // the current draft so the parent gets a fresh payload at click time.
  useEffect(() => {
    if (!onDraftChange) return;
    const changesCount = countDraftChanges({ baseline: layout, draft });
    onDraftChange({
      hasChanges: changesCount > 0,
      changesCount,
      composeSavePayload: () => composeBatchPayload({ baseline: layout, draft }),
    });
  }, [layout, draft, onDraftChange]);

  const effectiveLayout = useMemo<VenueLayoutDto>(() => {
    const isDeleted = (kind: CanvasItemRef['kind'], id: string) =>
      draft.deletions.has(refKey({ kind, id }));
    return {
      ...layout,
      zones: [
        ...(layout.zones ?? []).filter((z) => !isDeleted('zone', z.id)),
        ...draft.additions.zones,
      ],
      tables: [
        ...(layout.tables ?? []).filter((t) => !isDeleted('table', t.id)),
        ...draft.additions.tables,
      ],
      decorations: [
        ...(layout.decorations ?? []).filter((d) => !isDeleted('decoration', d.id)),
        ...draft.additions.decorations,
      ],
    };
  }, [layout, draft.additions, draft.deletions]);

  // Clear selection when an undo / redo / delete removes the selected item
  // from the effective layout. Without this, the property panel would show
  // a "missing" warning forever.
  useEffect(() => {
    if (!selected) return;
    if (findItem(effectiveLayout, selected) === null) {
      setSelected(null);
    }
  }, [selected, effectiveLayout]);

  const handleGeometryChange = useCallback(
    (ref: CanvasItemRef, geometryJson: string) => {
      history.commit((prev) => ({
        ...prev,
        geometryByKey: { ...prev.geometryByKey, [refKey(ref)]: geometryJson },
      }));
    },
    [history],
  );

  // Slice 8 S8.7: fetch the event's ticket tiers when the layout is
  // attached to an event. Template layouts skip the fetch (useTicketTiers
  // is query-gated by !!eventId).
  const tiersQuery = useTicketTiers(layout.eventId ?? undefined);

  const handleToggleTierAssignment = useCallback(
    (ref: CanvasItemRef, tierId: string) => {
      if (ref.kind === 'decoration') return; // decorations have no tier concept
      history.commit((prev) => {
        const key = refKey(ref);
        // Resolve the current tier set via draft override → persisted fallback.
        const item = findItem(effectiveLayout, ref);
        const persisted =
          item && (ref.kind === 'zone' || ref.kind === 'table')
            ? resolveTierAssignments(
                ref.kind,
                item as { id: string; ticketTierIds?: string[] | null },
                prev.tierAssignmentsByKey,
              )
            : [];
        const next = toggleTierAssignment(persisted, tierId);
        return {
          ...prev,
          tierAssignmentsByKey: { ...prev.tierAssignmentsByKey, [key]: next },
        };
      });
    },
    [history, effectiveLayout],
  );

  /**
   * Slice 9.5 — seat-gen handler. Sets / clears the per-zone override in the
   * canvas-editor draft. Passing `null` removes the entry (the panel emits
   * `null` whenever either input is empty/zero, signalling "don't generate").
   * Empty entries — entries where rowCount=0 OR seatsPerRow=0 — are also
   * pruned here so `composeBatchPayload` doesn't emit half-baked nulls.
   */
  const handleSeatGenChange = useCallback(
    (
      zoneId: string,
      next: { rowCount: number; seatsPerRow: number } | null,
    ) => {
      history.commit((prev) => {
        const without = { ...prev.seatGenByZoneId };
        if (
          next === null ||
          next.rowCount <= 0 ||
          next.seatsPerRow <= 0
        ) {
          delete without[zoneId];
          return { ...prev, seatGenByZoneId: without };
        }
        return {
          ...prev,
          seatGenByZoneId: { ...prev.seatGenByZoneId, [zoneId]: next },
        };
      });
    },
    [history],
  );

  const factoryCenter = useMemo(
    () => ({
      x: (layout.canvas?.width ?? DEFAULT_CANVAS_WIDTH) / 2,
      y: (layout.canvas?.height ?? DEFAULT_CANVAS_HEIGHT) / 2,
    }),
    [layout.canvas?.width, layout.canvas?.height],
  );

  const handleAddZone = useCallback(() => {
    const existing = effectiveLayout.zones?.length ?? 0;
    const newZone = createZoneDraft({
      layoutId: layout.id,
      center: factoryCenter,
      nextSortOrder: existing,
      indexForLabel: existing,
    });
    history.commit((prev) => ({
      ...prev,
      additions: { ...prev.additions, zones: [...prev.additions.zones, newZone] },
    }));
    setSelected({ kind: 'zone', id: newZone.id });
  }, [effectiveLayout.zones, layout.id, factoryCenter, history]);

  const handleAddRoundTable = useCallback(() => {
    const existing = effectiveLayout.tables?.length ?? 0;
    const newTable = createRoundTableDraft({
      layoutId: layout.id,
      center: factoryCenter,
      nextSortOrder: existing,
      indexForLabel: existing,
    });
    history.commit((prev) => ({
      ...prev,
      additions: { ...prev.additions, tables: [...prev.additions.tables, newTable] },
    }));
    setSelected({ kind: 'table', id: newTable.id });
  }, [effectiveLayout.tables, layout.id, factoryCenter, history]);

  const handleAddRectTable = useCallback(() => {
    const existing = effectiveLayout.tables?.length ?? 0;
    const newTable = createRectTableDraft({
      layoutId: layout.id,
      center: factoryCenter,
      nextSortOrder: existing,
      indexForLabel: existing,
    });
    history.commit((prev) => ({
      ...prev,
      additions: { ...prev.additions, tables: [...prev.additions.tables, newTable] },
    }));
    setSelected({ kind: 'table', id: newTable.id });
  }, [effectiveLayout.tables, layout.id, factoryCenter, history]);

  const handleAddDecoration = useCallback(
    (kind: DecorationKind) => {
      const existing = effectiveLayout.decorations?.length ?? 0;
      const newDec = createDecorationDraft(
        {
          layoutId: layout.id,
          center: factoryCenter,
          nextSortOrder: existing,
          indexForLabel: existing,
        },
        kind,
      );
      history.commit((prev) => ({
        ...prev,
        additions: {
          ...prev.additions,
          decorations: [...prev.additions.decorations, newDec],
        },
      }));
      setSelected({ kind: 'decoration', id: newDec.id });
    },
    [effectiveLayout.decorations, layout.id, factoryCenter, history],
  );

  const handleDeleteSelected = useCallback(() => {
    if (!selected) return;
    const key = refKey(selected);
    history.commit((prev) => {
      const isAddition =
        (selected.kind === 'zone' &&
          prev.additions.zones.some((z) => z.id === selected.id)) ||
        (selected.kind === 'table' &&
          prev.additions.tables.some((t) => t.id === selected.id)) ||
        (selected.kind === 'decoration' &&
          prev.additions.decorations.some((d) => d.id === selected.id));
      const nextAdditions = isAddition
        ? {
            zones: prev.additions.zones.filter((z) => z.id !== selected.id),
            tables: prev.additions.tables.filter((t) => t.id !== selected.id),
            decorations: prev.additions.decorations.filter(
              (d) => d.id !== selected.id,
            ),
          }
        : prev.additions;
      const nextDeletions = new Set(prev.deletions);
      if (!isAddition) nextDeletions.add(key);
      const nextGeometry = { ...prev.geometryByKey };
      delete nextGeometry[key];
      // Drop any pending tier-assignment override for the deleted item so
      // S8.8's save diff doesn't resurrect assignments for a tombstoned id.
      const nextTierAssignments = { ...prev.tierAssignmentsByKey };
      delete nextTierAssignments[key];
      // Slice 9.5 — drop pending seat-gen override too (only zones can have one).
      const nextSeatGen = { ...prev.seatGenByZoneId };
      if (selected.kind === 'zone') delete nextSeatGen[selected.id];
      return {
        geometryByKey: nextGeometry,
        additions: nextAdditions,
        deletions: nextDeletions,
        tierAssignmentsByKey: nextTierAssignments,
        seatGenByZoneId: nextSeatGen,
      };
    });
    setSelected(null);
  }, [selected, history]);

  // Arrow-key fine positioning: move the selected item by (dx, dy) without
  // snapping — the nudge is the escape hatch from grid snap. Commits go
  // through history so Ctrl+Z undoes them.
  const nudgeSelected = useCallback(
    (dx: number, dy: number) => {
      if (!selected) return;
      const item = findItem(effectiveLayout, selected);
      if (!item) return;
      const shapeHint = getShapeHint(selected, item);
      const geometry = resolveGeometry(
        selected.kind,
        item as { id: string; geometry?: string | null },
        draft.geometryByKey,
      );
      const center = itemCenter(selected.kind, geometry, shapeHint);
      if (!center) return;
      const newCenter = { x: center.x + dx, y: center.y + dy };
      const nextGeom = applyDragToGeometry(
        selected.kind,
        geometry,
        newCenter,
        shapeHint,
      );
      if (!nextGeom) return;
      history.commit((prev) => ({
        ...prev,
        geometryByKey: { ...prev.geometryByKey, [refKey(selected)]: nextGeom },
      }));
    },
    [selected, effectiveLayout, draft.geometryByKey, history],
  );

  // Global keyboard shortcuts. Registered once when the editor mounts; the
  // isTypingTarget guard keeps typing in the property panel natural.
  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (isTypingTarget(document.activeElement)) return;
      const mod = e.ctrlKey || e.metaKey;
      // Undo / Redo
      if (mod && e.key.toLowerCase() === 'z' && !e.shiftKey) {
        e.preventDefault();
        history.undo();
        return;
      }
      if (
        (mod && e.key.toLowerCase() === 'z' && e.shiftKey) ||
        (mod && e.key.toLowerCase() === 'y')
      ) {
        e.preventDefault();
        history.redo();
        return;
      }
      // Delete
      if (e.key === 'Delete' || e.key === 'Backspace') {
        if (selected) {
          e.preventDefault();
          handleDeleteSelected();
        }
        return;
      }
      // Arrow nudge
      const step = e.shiftKey ? NUDGE_STEP_FINE : NUDGE_STEP;
      if (e.key === 'ArrowLeft') {
        if (selected) {
          e.preventDefault();
          nudgeSelected(-step, 0);
        }
      } else if (e.key === 'ArrowRight') {
        if (selected) {
          e.preventDefault();
          nudgeSelected(step, 0);
        }
      } else if (e.key === 'ArrowUp') {
        if (selected) {
          e.preventDefault();
          nudgeSelected(0, -step);
        }
      } else if (e.key === 'ArrowDown') {
        if (selected) {
          e.preventDefault();
          nudgeSelected(0, step);
        }
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [history, selected, handleDeleteSelected, nudgeSelected]);

  return (
    <div
      className={className ?? 'flex flex-col h-full w-full'}
      data-testid="canvas-editor-layout"
    >
      <CanvasEditorToolbar
        onAddZone={handleAddZone}
        onAddRoundTable={handleAddRoundTable}
        onAddRectTable={handleAddRectTable}
        onAddDecoration={handleAddDecoration}
        onDeleteSelected={handleDeleteSelected}
        canDelete={selected !== null}
        onUndo={history.undo}
        onRedo={history.redo}
        canUndo={history.canUndo}
        canRedo={history.canRedo}
      />
      <div className="flex flex-1 min-h-0">
        <div className="flex-1 min-w-0" data-testid="canvas-editor-canvas-slot">
          <CanvasEditorStage
            layout={effectiveLayout}
            className="w-full h-full"
            selected={selected}
            onSelect={setSelected}
            draftGeometryByKey={draft.geometryByKey}
            onGeometryChange={handleGeometryChange}
          />
        </div>
        <CanvasEditorPropertyPanel
          layout={effectiveLayout}
          selected={selected}
          draftGeometryByKey={draft.geometryByKey}
          onGeometryChange={handleGeometryChange}
          tiers={tiersQuery.data}
          tiersLoading={tiersQuery.isLoading}
          draftTierAssignmentsByKey={draft.tierAssignmentsByKey}
          onToggleTierAssignment={handleToggleTierAssignment}
          seatGenByZoneId={draft.seatGenByZoneId}
          onSeatGenChange={handleSeatGenChange}
        />
      </div>
    </div>
  );
}

export default CanvasEditor;
