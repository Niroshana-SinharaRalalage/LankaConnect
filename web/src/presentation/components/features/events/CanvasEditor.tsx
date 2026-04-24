/**
 * Slice 8 S8.2+S8.3+S8.5a+S8.5b: CanvasEditor — SSR-safe wrapper + editor state.
 *
 * Konva touches `window` at import time, so the interactive stage lives in
 * {@link CanvasEditorStage} and we lazy-load it via `next/dynamic` with
 * `ssr: false`. This file is the only module pages / modals should import.
 *
 * Editor state owned by this wrapper:
 *   - `selected: CanvasItemRef | null` — currently selected shape.
 *   - `draftGeometryByKey: Record<string, string>` — in-progress geometry
 *     overrides per item keyed by refKey(ref). Each drag / Transformer
 *     commit / property-panel commit appends to this. Save (S8.8) will
 *     materialize it into PUT /batch and clear the draft.
 *   - `draftAdditions` — net-new items created by toolbar "Add" buttons.
 *     Merged into the effective layout for rendering + property panel.
 *     S8.8 will POST these via the batch endpoint.
 *   - `draftDeletions: Set<refKey>` — items queued for deletion. Filtered
 *     out of the effective layout for rendering. S8.8 will DELETE these.
 *
 * Save (S8.8) translates (draftAdditions, draftGeometryByKey, draftDeletions)
 * into a single `PUT /api/venue-layouts/{id}/batch` payload.
 */

'use client';

import React, { useCallback, useMemo, useState } from 'react';
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
  createDecorationDraft,
  createRectTableDraft,
  createRoundTableDraft,
  createZoneDraft,
  refKey,
  type CanvasItemRef,
} from '@/presentation/utils/canvasEditorGeometry';

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

export interface CanvasEditorProps {
  layout: VenueLayoutDto;
  className?: string;
}

interface DraftAdditions {
  zones: VenueZoneDto[];
  tables: VenueTableDto[];
  decorations: VenueDecorationDto[];
}

const emptyAdditions: DraftAdditions = { zones: [], tables: [], decorations: [] };

const DEFAULT_CANVAS_WIDTH = 1000;
const DEFAULT_CANVAS_HEIGHT = 800;

export function CanvasEditor({ layout, className }: CanvasEditorProps) {
  const [selected, setSelected] = useState<CanvasItemRef | null>(null);
  const [draftGeometryByKey, setDraftGeometryByKey] = useState<Record<string, string>>({});
  const [draftAdditions, setDraftAdditions] = useState<DraftAdditions>(emptyAdditions);
  const [draftDeletions, setDraftDeletions] = useState<Set<string>>(new Set());

  // Compose the effective layout: persisted items minus deletions, plus draft additions.
  const effectiveLayout = useMemo<VenueLayoutDto>(() => {
    const isDeleted = (kind: CanvasItemRef['kind'], id: string) =>
      draftDeletions.has(refKey({ kind, id }));
    return {
      ...layout,
      zones: [
        ...(layout.zones ?? []).filter((z) => !isDeleted('zone', z.id)),
        ...draftAdditions.zones,
      ],
      tables: [
        ...(layout.tables ?? []).filter((t) => !isDeleted('table', t.id)),
        ...draftAdditions.tables,
      ],
      decorations: [
        ...(layout.decorations ?? []).filter((d) => !isDeleted('decoration', d.id)),
        ...draftAdditions.decorations,
      ],
    };
  }, [layout, draftAdditions, draftDeletions]);

  const handleGeometryChange = useCallback(
    (ref: CanvasItemRef, geometryJson: string) => {
      setDraftGeometryByKey((prev) => ({
        ...prev,
        [refKey(ref)]: geometryJson,
      }));
    },
    [],
  );

  // Common context for draft factories: layout id + canvas center + sort orders.
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
    setDraftAdditions((prev) => ({ ...prev, zones: [...prev.zones, newZone] }));
    setSelected({ kind: 'zone', id: newZone.id });
  }, [effectiveLayout.zones, layout.id, factoryCenter]);

  const handleAddRoundTable = useCallback(() => {
    const existing = effectiveLayout.tables?.length ?? 0;
    const newTable = createRoundTableDraft({
      layoutId: layout.id,
      center: factoryCenter,
      nextSortOrder: existing,
      indexForLabel: existing,
    });
    setDraftAdditions((prev) => ({ ...prev, tables: [...prev.tables, newTable] }));
    setSelected({ kind: 'table', id: newTable.id });
  }, [effectiveLayout.tables, layout.id, factoryCenter]);

  const handleAddRectTable = useCallback(() => {
    const existing = effectiveLayout.tables?.length ?? 0;
    const newTable = createRectTableDraft({
      layoutId: layout.id,
      center: factoryCenter,
      nextSortOrder: existing,
      indexForLabel: existing,
    });
    setDraftAdditions((prev) => ({ ...prev, tables: [...prev.tables, newTable] }));
    setSelected({ kind: 'table', id: newTable.id });
  }, [effectiveLayout.tables, layout.id, factoryCenter]);

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
      setDraftAdditions((prev) => ({
        ...prev,
        decorations: [...prev.decorations, newDec],
      }));
      setSelected({ kind: 'decoration', id: newDec.id });
    },
    [effectiveLayout.decorations, layout.id, factoryCenter],
  );

  const handleDeleteSelected = useCallback(() => {
    if (!selected) return;
    const key = refKey(selected);
    // If it was a net-new addition, just drop it from additions (revert).
    const isAddition =
      (selected.kind === 'zone' && draftAdditions.zones.some((z) => z.id === selected.id)) ||
      (selected.kind === 'table' && draftAdditions.tables.some((t) => t.id === selected.id)) ||
      (selected.kind === 'decoration' &&
        draftAdditions.decorations.some((d) => d.id === selected.id));
    if (isAddition) {
      setDraftAdditions((prev) => ({
        zones: prev.zones.filter((z) => z.id !== selected.id),
        tables: prev.tables.filter((t) => t.id !== selected.id),
        decorations: prev.decorations.filter((d) => d.id !== selected.id),
      }));
    } else {
      setDraftDeletions((prev) => {
        const next = new Set(prev);
        next.add(key);
        return next;
      });
    }
    // Always drop the in-progress geometry override + selection.
    setDraftGeometryByKey((prev) => {
      if (!(key in prev)) return prev;
      const next = { ...prev };
      delete next[key];
      return next;
    });
    setSelected(null);
  }, [selected, draftAdditions]);

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
      />
      <div className="flex flex-1 min-h-0">
        <div className="flex-1 min-w-0" data-testid="canvas-editor-canvas-slot">
          <CanvasEditorStage
            layout={effectiveLayout}
            className="w-full h-full"
            selected={selected}
            onSelect={setSelected}
            draftGeometryByKey={draftGeometryByKey}
            onGeometryChange={handleGeometryChange}
          />
        </div>
        <CanvasEditorPropertyPanel
          layout={effectiveLayout}
          selected={selected}
          draftGeometryByKey={draftGeometryByKey}
          onGeometryChange={handleGeometryChange}
        />
      </div>
    </div>
  );
}

export default CanvasEditor;
