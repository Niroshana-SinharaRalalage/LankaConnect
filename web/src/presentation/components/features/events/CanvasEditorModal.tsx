/**
 * Slice 8 S8.1 + S8.8b: CanvasEditorModal — host for the canvas editor with
 * the Save button and atomic batch-save wiring.
 *
 * Mounted behind the "Customize" button in {@link SeatingLayoutPicker}.
 *
 * Metrics:
 *   - `layout.canvas_editor_opened` fires from this component on open
 *     (one event per open, even when the same layout is re-opened) via
 *     {@link venueLayoutsRepository.recordCanvasEditorOpened}.
 *   - `layout.canvas_editor_saved` fires from the BACKEND on a successful
 *     `PUT /api/venue-layouts/{id}/batch` commit (Slice 8 S8.8a). The
 *     frontend deliberately does NOT call
 *     `venueLayoutsRepository.recordCanvasEditorSaved` so the dashboard
 *     doesn't double-count.
 *
 * Save flow (S8.8b):
 *   1. CanvasEditor pushes a {@link CanvasEditorDraftSummary} via its
 *      `onDraftChange` callback after every history mutation.
 *   2. We store the latest summary in `draftSummaryRef` so click handlers
 *      always read the freshest payload-composer closure.
 *   3. Save click → `useBatchUpdateVenueLayout` mutateAsync — on success
 *      we invoke `onLayoutSaved` and close the modal; on `ApiError`
 *      statusCode 409 we keep the modal open and toast a 409-specific
 *      message ("Layout was modified externally — close and reopen to load
 *      the latest"); on any other error we keep the modal open and show
 *      the underlying error message.
 *   4. While `mutation.isPending`, Save is disabled and reads "Saving…".
 *
 * Tier-assignment persistence is intentionally NOT wired in S8.8b — the
 * `BatchLayoutPayload` schema doesn't carry tier assignments, and the
 * slice-4 single-tier endpoints belong to a separate save loop (S8.8c).
 */

'use client';

import React, { useCallback, useEffect, useRef, useState } from 'react';
import { X } from 'lucide-react';
import toast from 'react-hot-toast';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/presentation/components/ui/Dialog';
import { Button } from '@/presentation/components/ui/Button';
import { ConfirmDialog } from '@/presentation/components/ui/ConfirmDialog';
import { venueLayoutsRepository } from '@/infrastructure/api/repositories/venue-layouts.repository';
import {
  useBatchUpdateVenueLayout,
  useSaveLayoutAsTemplate,
} from '@/presentation/hooks/useVenueLayouts';
import { ApiError } from '@/infrastructure/api/client/api-errors';
import { CanvasEditor, type CanvasEditorDraftSummary } from './CanvasEditor';
import type { VenueLayoutDto } from '@/infrastructure/api/types/events.types';

export interface CanvasEditorModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  layout: VenueLayoutDto;
  /**
   * Optional — invoked exactly once after the canvas editor commits its
   * changes via `PUT /batch` and before the modal closes. The host
   * should refetch the layout from its own queries (the mutation has
   * already invalidated the relevant React Query scopes). Slice 8 S8.8b.
   */
  onLayoutSaved?: () => void;
}

export function CanvasEditorModal({
  open,
  onOpenChange,
  layout,
  onLayoutSaved,
}: CanvasEditorModalProps) {
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

  const [hasChanges, setHasChanges] = useState(false);
  const [discardConfirmOpen, setDiscardConfirmOpen] = useState(false);
  const draftSummaryRef = useRef<CanvasEditorDraftSummary | null>(null);

  const handleDraftChange = useCallback((summary: CanvasEditorDraftSummary) => {
    draftSummaryRef.current = summary;
    setHasChanges(summary.hasChanges);
  }, []);

  const mutation = useBatchUpdateVenueLayout(layout.id, layout.eventId ?? null);
  const isSaving = mutation.isPending;

  // Slice 8 S8.9b: save-as-personal-template mutation. Independent of the
  // batch-save flow — fires from a separate footer button + name-prompt
  // dialog and never closes the editor (user keeps editing the source).
  const saveAsTemplateMutation = useSaveLayoutAsTemplate();
  const [saveAsTemplateOpen, setSaveAsTemplateOpen] = useState(false);
  const [templateName, setTemplateName] = useState('');
  const isSavingAsTemplate = saveAsTemplateMutation.isPending;

  const openSaveAsTemplate = useCallback(() => {
    setTemplateName(`${layout.name} (Template)`);
    setSaveAsTemplateOpen(true);
  }, [layout.name]);

  const cancelSaveAsTemplate = useCallback(() => {
    if (isSavingAsTemplate) return; // don't yank the prompt mid-flight
    setSaveAsTemplateOpen(false);
  }, [isSavingAsTemplate]);

  const submitSaveAsTemplate = useCallback(async () => {
    const trimmed = templateName.trim();
    if (trimmed.length === 0) return;
    try {
      await saveAsTemplateMutation.mutateAsync({
        sourceLayoutId: layout.id,
        templateName: trimmed,
      });
      toast.success('Saved as template — find it in your Templates list.');
      setSaveAsTemplateOpen(false);
    } catch (err) {
      if (err instanceof ApiError && err.statusCode === 403) {
        toast.error(
          "You don't have permission to save this layout as a template.",
        );
        return;
      }
      const message =
        err instanceof Error
          ? err.message
          : 'Could not save the template. Please try again.';
      toast.error(`Save as template failed: ${message}`);
    }
  }, [templateName, saveAsTemplateMutation, layout.id]);

  // Slice 8 S8.9a: dirty-close guard. Any close path (X / footer Close /
  // Esc / backdrop click) routes through this. When the draft has unsaved
  // changes AND no save is in flight, we show a ConfirmDialog instead of
  // closing — losing organizer work to a misclick was the v1 footgun.
  // During an in-flight save we let the close go through; the mutation
  // completes in the background and the cache invalidates.
  const attemptClose = useCallback(() => {
    if (hasChanges && !isSaving) {
      setDiscardConfirmOpen(true);
      return;
    }
    onOpenChange(false);
  }, [hasChanges, isSaving, onOpenChange]);

  const confirmDiscard = useCallback(() => {
    setDiscardConfirmOpen(false);
    onOpenChange(false);
  }, [onOpenChange]);

  const handleSave = useCallback(async () => {
    const summary = draftSummaryRef.current;
    if (!summary || !summary.hasChanges) return;

    const payload = summary.composeSavePayload();
    try {
      await mutation.mutateAsync({ rowVersion: layout.rowVersion, payload });
      onLayoutSaved?.();
      // Bypass the dirty-close guard — the save just succeeded and the
      // batch mutation has already invalidated the cache. The hasChanges
      // state may still be `true` synchronously here because the
      // CanvasEditor's `onDraftChange` push happens in a useEffect on the
      // next layout/draft tick; calling onOpenChange directly avoids a
      // false-positive discard prompt.
      onOpenChange(false);
    } catch (err) {
      if (err instanceof ApiError && err.statusCode === 409) {
        toast.error(
          'Layout was modified externally. Close the editor and reopen to load the latest version, then redo your changes.',
        );
        return;
      }
      const message =
        err instanceof Error ? err.message : 'Could not save the canvas. Please try again.';
      toast.error(`Save failed: ${message}`);
    }
  }, [mutation, layout.rowVersion, onLayoutSaved, onOpenChange]);

  const saveDisabled = !hasChanges || isSaving;

  // Radix Dialog backdrop click + Esc both fire onOpenChange(false) — we
  // intercept the close direction (next === false) so the dirty guard runs;
  // re-opens are passed through unchanged (next === true).
  const handleDialogOpenChange = useCallback(
    (next: boolean) => {
      if (next === false) {
        attemptClose();
        return;
      }
      onOpenChange(next);
    },
    [attemptClose, onOpenChange],
  );

  return (
    <>
      <Dialog open={open} onOpenChange={handleDialogOpenChange}>
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
              onClick={attemptClose}
              data-testid="canvas-editor-close"
            >
              <X className="w-4 h-4" aria-hidden="true" />
            </Button>
          </DialogHeader>

          <div
            className="flex-1 bg-neutral-50 overflow-hidden"
            data-testid="canvas-editor-body"
          >
            <CanvasEditor
              layout={layout}
              className="w-full h-full"
              onDraftChange={handleDraftChange}
            />
          </div>

          <div className="flex items-center justify-end gap-3 p-4 border-t border-neutral-200">
            <Button
              type="button"
              variant="outline"
              onClick={openSaveAsTemplate}
              disabled={isSavingAsTemplate}
              className="mr-auto"
              data-testid="canvas-editor-save-as-template"
            >
              Save as Template
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={attemptClose}
              data-testid="canvas-editor-cancel"
            >
              Close
            </Button>
            <Button
              type="button"
              onClick={handleSave}
              disabled={saveDisabled}
              data-testid="canvas-editor-save"
            >
              {isSaving ? 'Saving…' : 'Save'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={discardConfirmOpen}
        onOpenChange={setDiscardConfirmOpen}
        title="Discard unsaved changes?"
        description="You have edits in the canvas editor that haven't been saved. Closing now will lose them."
        confirmLabel="Discard"
        cancelLabel="Keep editing"
        onConfirm={confirmDiscard}
        variant="warning"
      />

      {/* Slice 8 S8.9b: Save-as-Template name prompt. Inline Dialog (not
          ConfirmDialog) so we can host an input field. The submit button is
          disabled when the trimmed name is empty so the backend's
          NotEmpty(TemplateName) gate doesn't trip on an avoidable misclick. */}
      <Dialog open={saveAsTemplateOpen} onOpenChange={setSaveAsTemplateOpen}>
        <DialogContent className="max-w-md" data-testid="save-as-template-dialog">
          <DialogHeader>
            <DialogTitle>Save layout as template</DialogTitle>
            <DialogDescription>
              The new template will appear under your Templates and can be
              re-applied to future events. Tier mappings are not copied.
            </DialogDescription>
          </DialogHeader>
          <div className="py-2">
            <label
              htmlFor="save-as-template-name-input"
              className="block text-sm font-medium mb-1"
            >
              Template name
            </label>
            <input
              id="save-as-template-name-input"
              type="text"
              className="w-full border border-neutral-300 rounded-md px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
              value={templateName}
              onChange={(e) => setTemplateName(e.target.value)}
              maxLength={200}
              disabled={isSavingAsTemplate}
              data-testid="save-as-template-name-input"
              autoFocus
            />
          </div>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={cancelSaveAsTemplate}
              disabled={isSavingAsTemplate}
              data-testid="save-as-template-cancel"
            >
              Cancel
            </Button>
            <Button
              type="button"
              onClick={submitSaveAsTemplate}
              disabled={
                isSavingAsTemplate || templateName.trim().length === 0
              }
              data-testid="save-as-template-confirm"
            >
              {isSavingAsTemplate ? 'Saving…' : 'Save Template'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

export default CanvasEditorModal;
