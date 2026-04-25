/**
 * Slice 8 S8.1: CanvasEditorModal shell.
 *
 * Mounted behind the "Customize" button in {@link SeatingLayoutPicker}. Today
 * the modal is a shell — it only emits the architect-spec
 * `layout.canvas_editor_opened` metric via
 * `venueLayoutsRepository.recordCanvasEditorOpened(layoutId)` and shows a
 * placeholder for the editor surface that Slices S8.2–S8.9 will fill in.
 *
 * The layout passed in is the current venue layout for the event; the modal
 * does not load its own copy. Future chunks will introduce local edit state
 * (command-pattern undo/redo) that diverges from `layout` until Save.
 *
 * Designed to be fully replaceable by an interactive react-konva stage in
 * later chunks without changing the props contract.
 */

'use client';

import React, { useEffect, useRef } from 'react';
import { X } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/presentation/components/ui/Dialog';
import { Button } from '@/presentation/components/ui/Button';
import { venueLayoutsRepository } from '@/infrastructure/api/repositories/venue-layouts.repository';
import { CanvasEditor } from './CanvasEditor';
import type { VenueLayoutDto } from '@/infrastructure/api/types/events.types';

export interface CanvasEditorModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  layout: VenueLayoutDto;
  /**
   * Optional — invoked after the editor commits changes via PUT /batch
   * (wired in S8.8). Shell today never invokes this.
   */
  onLayoutSaved?: (layout: VenueLayoutDto) => void;
}

export function CanvasEditorModal({
  open,
  onOpenChange,
  layout,
  onLayoutSaved,
}: CanvasEditorModalProps) {
  // Suppress unused-var linting until S8.8 wires onLayoutSaved.
  void onLayoutSaved;

  // Fire the architect-spec `layout.canvas_editor_opened` metric the first
  // time this modal transitions to open for a given layout. Re-open of the
  // same layout (e.g., user closes and re-opens) emits a fresh event, which
  // is the desired dashboard behavior (counts editor openings, not sessions).
  const lastEmittedForLayoutRef = useRef<string | null>(null);
  useEffect(() => {
    if (!open) {
      lastEmittedForLayoutRef.current = null;
      return;
    }
    if (lastEmittedForLayoutRef.current === layout.id) return;
    lastEmittedForLayoutRef.current = layout.id;
    void venueLayoutsRepository.recordCanvasEditorOpened(layout.id);
  }, [open, layout.id]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="max-w-5xl w-[95vw] h-[90vh] flex flex-col p-0"
        data-testid="canvas-editor-modal"
      >
        <DialogHeader className="flex-row items-start justify-between gap-4 p-4 border-b border-neutral-200 mb-0 space-y-0">
          <div className="flex-1 min-w-0">
            <DialogTitle className="truncate">Customize layout — {layout.name}</DialogTitle>
            <DialogDescription>
              {layout.layoutType} · {layout.totalCapacity} seats · {layout.zones?.length ?? 0}{' '}
              zone{(layout.zones?.length ?? 0) === 1 ? '' : 's'}
            </DialogDescription>
          </div>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            aria-label="Close canvas editor"
            onClick={() => onOpenChange(false)}
            data-testid="canvas-editor-close"
          >
            <X className="w-4 h-4" aria-hidden="true" />
          </Button>
        </DialogHeader>

        <div
          className="flex-1 bg-neutral-50 overflow-hidden"
          data-testid="canvas-editor-body"
        >
          <CanvasEditor layout={layout} className="w-full h-full" />
        </div>

        <div className="flex items-center justify-end gap-3 p-4 border-t border-neutral-200">
          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            data-testid="canvas-editor-cancel"
          >
            Close
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

export default CanvasEditorModal;
