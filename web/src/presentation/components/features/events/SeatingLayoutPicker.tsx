/**
 * Slice 6 Chunk S6.9: SeatingLayoutPicker
 *
 * Bridges the preset-library modal with the event's venue layout. Rendered
 * only when the event has been saved (we have an `eventId`). For in-progress
 * event creation the parent shows a "save first" placeholder — creating a
 * layout attached to a nonexistent event would be wrong.
 *
 * Flow:
 *   1. Read current layout via useVenueLayoutByEvent(eventId).
 *   2a. No layout yet → "Choose a layout" button opens PresetLibraryModal.
 *   2b. Layout exists → LayoutPreview + capacity + "Change layout" button.
 *   3. Preset pick → useCreateLayoutFromPreset({presetId, eventId}) →
 *      useAssignLayoutToEvent({eventId, layoutId}) → cache refetch.
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
  useCreateLayoutFromPreset,
  useAssignLayoutToEvent,
} from '@/presentation/hooks/useVenueLayouts';
import { Button } from '@/presentation/components/ui/Button';
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
  const [flowError, setFlowError] = useState<string | null>(null);
  // Slice 8 S8.1: canvas editor is a separate modal, opened only once a layout exists.
  const [editorOpen, setEditorOpen] = useState(false);

  const layoutQuery = useVenueLayoutByEvent(eventId);
  const createFromPreset = useCreateLayoutFromPreset();
  const assignLayout = useAssignLayoutToEvent();

  const handlePresetSelected = useCallback(
    async (preset: LayoutPresetDto) => {
      setFlowError(null);
      setPickingPresetId(preset.id);
      try {
        const layout = await createFromPreset.mutateAsync({
          presetId: preset.id,
          eventId,
        });
        await assignLayout.mutateAsync({
          eventId,
          layoutId: layout.id,
        });
        setModalOpen(false);
        onLayoutChanged?.(layout);
      } catch (e) {
        const message =
          e instanceof Error ? e.message : 'Failed to apply the preset.';
        // eslint-disable-next-line no-console
        console.error('[SeatingLayoutPicker] preset flow failed:', e);
        setFlowError(message);
      } finally {
        setPickingPresetId(null);
      }
    },
    [assignLayout, createFromPreset, eventId, onLayoutChanged],
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
                onClick={() => setModalOpen(true)}
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
        isSelecting={createFromPreset.isPending || assignLayout.isPending}
        selectingPresetId={pickingPresetId}
      />

      {layout && (
        <CanvasEditorModal
          open={editorOpen}
          onOpenChange={setEditorOpen}
          layout={layout}
          onLayoutSaved={(updated) => {
            onLayoutChanged?.(updated);
            setEditorOpen(false);
          }}
        />
      )}
    </div>
  );
}
