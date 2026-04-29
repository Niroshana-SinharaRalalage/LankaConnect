/**
 * Slice 6 Chunk S6.9: SeatingLayoutPicker (revised in Slice 9.4 — atomic apply)
 *
 * Bridges the preset-library modal with the event's venue layout. Rendered
 * only when the event has been saved (we have an `eventId`). For in-progress
 * event creation the parent shows a "save first" placeholder — creating a
 * layout attached to a nonexistent event would be wrong.
 *
 * Flow:
 *   1. Read current layout via useVenueLayoutByEvent(eventId).
 *   2a. No layout yet → "Choose a layout" button opens PresetLibraryModal.
 *   2b. Layout exists → LayoutPreview + capacity + "Change layout" button
 *       (the latter goes through ConfirmDialog before re-opening the modal,
 *       per Slice 9.4 — the swap detaches the current layout, which becomes
 *       an orphan candidate for future cleanup).
 *   3. Preset pick → useApplyPresetToEvent — single transaction creates the
 *      layout AND flips event seatingMode + VenueLayoutId in one shot. No
 *      orphan-on-partial-failure (Slice 9.2 design).
 *   4. Template pick → useApplyTemplateToEvent — mirror.
 *
 * Errors surface inline (component does not throw). Logs go via console so
 * dev observability still works; production errors bubble through the
 * standard apiClient interceptor.
 */

'use client';

import React, { useState, useCallback } from 'react';
import { Armchair, Loader2, AlertCircle } from 'lucide-react';
import {
  useLayoutPresets, // imported for type-check awareness; used through modal
  useVenueLayoutByEvent,
  useApplyPresetToEvent,
  useApplyTemplateToEvent,
} from '@/presentation/hooks/useVenueLayouts';
import { Button } from '@/presentation/components/ui/Button';
import { ConfirmDialog } from '@/presentation/components/ui/ConfirmDialog';
import { PresetLibraryModal } from './PresetLibraryModal';
import { LayoutPreview } from './LayoutPreview';
import { CanvasEditorModal } from './CanvasEditorModal';
import type { LayoutPresetDto, VenueLayoutDto } from '@/infrastructure/api/types/events.types';

// Re-export the imported hook so unused-import linting does not fire.
// The hook is consumed inside <PresetLibraryModal/>; this keeps the
// dependency graph explicit at this integration boundary.
void useLayoutPresets;

export interface SeatingLayoutPickerProps {
  eventId: string;
  /** Optional callback fired after a preset is picked and attached. */
  onLayoutChanged?: (layout: VenueLayoutDto) => void;
}

export function SeatingLayoutPicker({
  eventId,
  onLayoutChanged,
}: SeatingLayoutPickerProps) {
  const [modalOpen, setModalOpen] = useState(false);
  const [pickingPresetId, setPickingPresetId] = useState<string | null>(null);
  // Slice 8 S8.10: when the user picks from the "Mine" tab.
  const [pickingTemplateId, setPickingTemplateId] = useState<string | null>(null);
  const [flowError, setFlowError] = useState<string | null>(null);
  // Slice 8 S8.1: canvas editor is a separate modal, opened only once a layout exists.
  const [editorOpen, setEditorOpen] = useState(false);
  // Slice 9.4: confirm-before-replace guard for "Change layout" when one is attached.
  const [changeConfirmOpen, setChangeConfirmOpen] = useState(false);

  const layoutQuery = useVenueLayoutByEvent(eventId);
  const applyPreset = useApplyPresetToEvent();
  const applyTemplate = useApplyTemplateToEvent();

  const handlePresetSelected = useCallback(
    async (preset: LayoutPresetDto) => {
      setFlowError(null);
      setPickingPresetId(preset.id);
      try {
        // Slice 9.2: single atomic call. Old two-step (createFromPreset+assign)
        // could 400 on the assign with "Zone must be mapped to a ticket tier"
        // because preset zones come without tier_assignments. The new
        // apply-preset endpoint persists the layout AND flips the event's
        // seatingMode in one transaction — no partial-failure orphans.
        const layout = await applyPreset.mutateAsync({
          presetId: preset.id,
          eventId,
        });
        setModalOpen(false);
        onLayoutChanged?.(layout);
      } catch (e) {
        const message =
          e instanceof Error ? e.message : 'Failed to apply the preset.';
        // eslint-disable-next-line no-console
        console.error('[SeatingLayoutPicker] preset apply failed:', e);
        setFlowError(message);
      } finally {
        setPickingPresetId(null);
      }
    },
    [applyPreset, eventId, onLayoutChanged],
  );

  /**
   * Slice 8 S8.10 / Slice 9.4: apply one of the user's saved templates.
   * Single atomic call (Slice 9.2 apply-template endpoint) replaces the old
   * createFromTemplate+assign two-step.
   */
  const handleTemplateSelected = useCallback(
    async (template: VenueLayoutDto) => {
      setFlowError(null);
      setPickingTemplateId(template.id);
      try {
        const layout = await applyTemplate.mutateAsync({
          sourceTemplateId: template.id,
          eventId,
          // Backend defaults LayoutName to source.Name when null/whitespace.
          // The user can rename via the canvas editor's property panel.
          layoutName: null,
        });
        setModalOpen(false);
        onLayoutChanged?.(layout);
      } catch (e) {
        const message =
          e instanceof Error ? e.message : 'Failed to apply the template.';
        // eslint-disable-next-line no-console
        console.error('[SeatingLayoutPicker] template apply failed:', e);
        setFlowError(message);
      } finally {
        setPickingTemplateId(null);
      }
    },
    [applyTemplate, eventId, onLayoutChanged],
  );

  const layout = layoutQuery.data ?? null;
  const isBusy = layoutQuery.isLoading;

  return (
    <div className="space-y-3" data-testid="seating-layout-picker">
      {isBusy && (
        <div
          className="flex items-center gap-2 text-sm text-neutral-500"
          data-testid="layout-picker-loading"
        >
          <Loader2 className="w-4 h-4 animate-spin" />
          Loading layout…
        </div>
      )}

      {!isBusy && !layout && (
        <div
          className="rounded-md border border-dashed border-primary-200 bg-primary-50/30 px-4 py-4 flex items-start gap-3"
          data-testid="layout-picker-empty"
        >
          <Armchair className="w-5 h-5 text-primary-600 mt-0.5 shrink-0" aria-hidden="true" />
          <div className="flex-1">
            <p className="text-sm font-medium text-neutral-900">No layout chosen yet</p>
            <p className="text-sm text-neutral-600 mt-1">
              Pick a preset to get started — you can customize it later.
            </p>
            <Button
              type="button"
              className="mt-3"
              onClick={() => setModalOpen(true)}
              data-testid="layout-picker-choose"
            >
              Choose a layout
            </Button>
          </div>
        </div>
      )}

      {!isBusy && layout && (
        <div
          className="rounded-md border border-neutral-200 bg-white p-4 space-y-3"
          data-testid="layout-picker-preview"
        >
          <div className="flex items-start justify-between gap-4">
            <div>
              <p className="text-sm font-medium text-neutral-900">{layout.name}</p>
              <p className="text-xs text-neutral-500 mt-0.5">
                {layout.layoutType} · {layout.totalCapacity} seats
              </p>
            </div>
            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => setEditorOpen(true)}
                data-testid="layout-picker-customize"
              >
                Customize
              </Button>
              <Button
                type="button"
                variant="outline"
                onClick={() => setChangeConfirmOpen(true)}
                data-testid="layout-picker-change"
              >
                Change layout
              </Button>
            </div>
          </div>
          <div className="rounded border border-neutral-100 bg-neutral-50/60 p-2">
            <LayoutPreview
              layout={layout}
              className="w-full aspect-[3/2] max-h-72"
              showSeats={layout.totalCapacity <= 200}
            />
          </div>
        </div>
      )}

      {flowError && (
        <div
          className="flex items-start gap-2 text-sm text-red-700 bg-red-50 border border-red-200 rounded-md px-3 py-2"
          role="alert"
          data-testid="layout-picker-error"
        >
          <AlertCircle className="w-4 h-4 mt-0.5 shrink-0" />
          <span>{flowError}</span>
        </div>
      )}

      <PresetLibraryModal
        open={modalOpen}
        onOpenChange={(v) => {
          if (!v) setFlowError(null);
          setModalOpen(v);
        }}
        onSelect={handlePresetSelected}
        isSelecting={applyPreset.isPending}
        selectingPresetId={pickingPresetId}
        onSelectMine={handleTemplateSelected}
        isSelectingMine={applyTemplate.isPending}
        selectingMineId={pickingTemplateId}
      />

      {/*
        Slice 9.4: confirm-before-replace guard. The user has clicked
        "Change layout" while a layout is already attached — picking a new
        preset will detach the current one (it becomes an orphan that
        future cleanup migrations remove). Confirming opens the picker
        modal; cancelling is a no-op. Reuses the existing ConfirmDialog
        primitive (same pattern as save-as-template + warn-before-close).
      */}
      <ConfirmDialog
        open={changeConfirmOpen}
        onOpenChange={setChangeConfirmOpen}
        title="Replace current seating layout?"
        description="Changing the seating layout will discard the current one, including any zones, seats, or tier mappings you have configured. This cannot be undone."
        confirmLabel="Replace layout"
        cancelLabel="Keep current layout"
        variant="danger"
        onConfirm={() => {
          setChangeConfirmOpen(false);
          setModalOpen(true);
        }}
      />

      {layout && (
        <CanvasEditorModal
          open={editorOpen}
          onOpenChange={setEditorOpen}
          layout={layout}
          onLayoutSaved={() => {
            // Slice 8 S8.8b: the modal closes itself on success and the
            // batch-save mutation invalidates the layout cache. Pass the
            // pre-save layout reference upward so listeners that key off
            // the prop fire — the actual fresh data flows in via React
            // Query refetch, not through this callback.
            onLayoutChanged?.(layout);
          }}
        />
      )}
    </div>
  );
}
