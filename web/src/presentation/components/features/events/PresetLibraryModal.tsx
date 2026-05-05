/**
 * Slice 6 Chunk S6.7 + Slice 8 S8.10: PresetLibraryModal
 *
 * Two-tab picker for the canvas editor's "Choose a layout" / "Change layout"
 * flow:
 *
 *   - **Built-in** (default tab) — 8 industry-standard layout presets in a
 *     responsive grid. Click → onSelect(preset). [Slice 6]
 *   - **Mine** — every layout the calling user has saved as a template via
 *     S8.9b's "Save as Template" flow. Click → onSelectMine(template). [S8.10]
 *
 * Thumbnails for built-in presets are static SVG files under
 * /public/layouts/presets/. User templates have no thumbnail today — we render
 * a small layout-type chip + capacity badge instead. The modal stays a pure
 * presentational component; both lists are owned by their respective React
 * Query hooks (useLayoutPresets / useUserTemplates) and both `onSelect*`
 * callbacks are driven by the parent (SeatingLayoutPicker).
 */

'use client';

import React, { useState, useCallback } from 'react';
import Image from 'next/image';
import toast from 'react-hot-toast';
import { AlertCircle, Loader2, Layers, Trash2 } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/presentation/components/ui/Dialog';
import { Button } from '@/presentation/components/ui/Button';
import { ConfirmDialog } from '@/presentation/components/ui/ConfirmDialog';
import {
  useLayoutPresets,
  useUserTemplates,
  useDeleteUserTemplate,
} from '@/presentation/hooks/useVenueLayouts';
import { ApiError } from '@/infrastructure/api/client/api-errors';
import type {
  LayoutPresetDto,
  VenueLayoutDto,
} from '@/infrastructure/api/types/events.types';

type ActiveTab = 'builtin' | 'mine';

export interface PresetLibraryModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /**
   * Fired when the organizer confirms a built-in preset. The parent should
   * POST /api/venue-layouts/from-preset with the id and handle the 201
   * response. Slice 6.
   */
  onSelect: (preset: LayoutPresetDto) => void | Promise<void>;
  /**
   * Slice 8 S8.10: fired when the organizer picks one of their saved
   * templates from the "Mine" tab. The parent should POST
   * /api/venue-layouts/from-template and handle the 201 response. When omitted,
   * the Mine tab still renders + lists templates but card clicks are inert
   * (parents that don't expose the apply-template flow get a read-only view).
   */
  onSelectMine?: (template: VenueLayoutDto) => void | Promise<void>;
  /**
   * Shows a spinner on the chosen built-in preset card while the parent
   * mutation is in flight. Pass the parent mutation's `isPending` here.
   */
  isSelecting?: boolean;
  /**
   * Which preset id is being confirmed — restricts the spinner to that card.
   */
  selectingPresetId?: string | null;
  /** Slice 8 S8.10: same shape as `isSelecting`, but for the "Mine" tab. */
  isSelectingMine?: boolean;
  /** Slice 8 S8.10: which template id is being applied. */
  selectingMineId?: string | null;
}

export function PresetLibraryModal({
  open,
  onOpenChange,
  onSelect,
  onSelectMine,
  isSelecting = false,
  selectingPresetId = null,
  isSelectingMine = false,
  selectingMineId = null,
}: PresetLibraryModalProps) {
  const [activeTab, setActiveTab] = useState<ActiveTab>('builtin');

  // Built-in preset list — fetched lazily when the modal first opens.
  const presetsQuery = useLayoutPresets({ enabled: open });
  // User templates — only fetched when the user actually opens the Mine tab,
  // saving a request for the common case where the user only wants a preset.
  const templatesQuery = useUserTemplates({
    enabled: open && activeTab === 'mine',
  });

  const [focusedId, setFocusedId] = useState<string | null>(null);

  // Slice 8 S8.11: per-card delete state. The whole "queued for delete"
  // template lives at modal scope so the ConfirmDialog can render outside
  // the card's <li> + survive a single render cycle without flicker.
  const [templateToDelete, setTemplateToDelete] = useState<VenueLayoutDto | null>(null);
  const deleteTemplate = useDeleteUserTemplate();

  const handlePickPreset = useCallback(
    async (preset: LayoutPresetDto) => {
      try {
        await onSelect(preset);
      } catch (e) {
        // Parent owns error surface; swallow here so the modal stays open.
        // eslint-disable-next-line no-console
        console.error('[PresetLibraryModal] onSelect threw:', e);
      }
    },
    [onSelect],
  );

  const handlePickTemplate = useCallback(
    async (template: VenueLayoutDto) => {
      if (!onSelectMine) return;
      try {
        await onSelectMine(template);
      } catch (e) {
        // eslint-disable-next-line no-console
        console.error('[PresetLibraryModal] onSelectMine threw:', e);
      }
    },
    [onSelectMine],
  );

  /**
   * Slice 8 S8.11: confirm-then-delete handler. Fired by the ConfirmDialog
   * after the user clicks "Delete". Wraps the mutation in try/catch so the
   * dialog stays open + we can surface an actionable toast for the
   * structural-edit-rejected case (held seats / pending registrations) the
   * backend's `DeleteLayoutCommand` returns 422 for.
   */
  const handleConfirmDeleteTemplate = useCallback(async () => {
    if (!templateToDelete) return;
    try {
      await deleteTemplate.mutateAsync({
        layoutId: templateToDelete.id,
        rowVersion: templateToDelete.rowVersion,
      });
      toast.success(`Template deleted — "${templateToDelete.name}"`);
      setTemplateToDelete(null);
    } catch (err) {
      if (err instanceof ApiError && err.statusCode === 422) {
        toast.error(
          "This template is still in use — it has held seats or pending reservations. Resolve those first.",
        );
        return;
      }
      const message =
        err instanceof Error
          ? err.message
          : 'Could not delete the template. Please try again.';
      toast.error(`Delete failed: ${message}`);
    }
  }, [templateToDelete, deleteTemplate]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-5xl">
        <DialogHeader>
          <DialogTitle>Choose a venue layout</DialogTitle>
          <DialogDescription>
            Pick a starting point for your seating. You can customize any preset
            after it&apos;s created.
          </DialogDescription>
        </DialogHeader>

        {/* Tab bar — simple state-driven buttons; no new dep. Aria roles
            mirror the WAI-ARIA tab pattern so screen readers + keyboard users
            still navigate properly. */}
        <div role="tablist" aria-label="Layout source" className="flex gap-1 border-b border-neutral-200 mb-3">
          <button
            type="button"
            role="tab"
            aria-selected={activeTab === 'builtin'}
            data-testid="preset-modal-tab-builtin"
            className={[
              'px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors',
              activeTab === 'builtin'
                ? 'border-primary-600 text-primary-700'
                : 'border-transparent text-neutral-500 hover:text-neutral-700',
            ].join(' ')}
            onClick={() => setActiveTab('builtin')}
          >
            Built-in presets
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={activeTab === 'mine'}
            data-testid="preset-modal-tab-mine"
            className={[
              'px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors',
              activeTab === 'mine'
                ? 'border-primary-600 text-primary-700'
                : 'border-transparent text-neutral-500 hover:text-neutral-700',
            ].join(' ')}
            onClick={() => setActiveTab('mine')}
          >
            My templates
          </button>
        </div>

        {activeTab === 'builtin' && (
          <div role="tabpanel" data-testid="preset-modal-tabpanel-builtin">
            {presetsQuery.isLoading && (
              <div
                className="flex items-center justify-center py-16 text-neutral-500"
                data-testid="preset-modal-loading"
              >
                <Loader2 className="w-5 h-5 animate-spin mr-2" />
                Loading presets…
              </div>
            )}

            {presetsQuery.isError && (
              <div
                className="flex flex-col items-center gap-3 py-10 text-red-700"
                data-testid="preset-modal-error"
              >
                <AlertCircle className="w-6 h-6" />
                <p className="text-sm">
                  Could not load presets
                  {presetsQuery.error?.message ? ` — ${presetsQuery.error.message}` : ''}.
                </p>
                <Button variant="outline" onClick={() => presetsQuery.refetch()}>
                  Try again
                </Button>
              </div>
            )}

            {presetsQuery.data && presetsQuery.data.length === 0 && (
              <p className="py-10 text-center text-neutral-500">
                No presets are available.
              </p>
            )}

            {presetsQuery.data && presetsQuery.data.length > 0 && (
              <ul
                className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 py-2"
                data-testid="preset-modal-grid"
              >
                {presetsQuery.data.map((p) => {
                  const isThisSelecting = isSelecting && selectingPresetId === p.id;
                  const disabled = isSelecting && !isThisSelecting;
                  return (
                    <li key={p.id}>
                      <button
                        type="button"
                        className={[
                          'w-full text-left rounded-lg border bg-white overflow-hidden',
                          'transition-shadow hover:shadow-md focus:outline-none',
                          'focus:ring-2 focus:ring-primary-500 focus:border-primary-500',
                          focusedId === p.id ? 'border-primary-500' : 'border-neutral-200',
                          disabled ? 'opacity-60 cursor-not-allowed' : '',
                        ].join(' ')}
                        onClick={() => handlePickPreset(p)}
                        onFocus={() => setFocusedId(p.id)}
                        onBlur={() => setFocusedId(null)}
                        disabled={disabled}
                        aria-busy={isThisSelecting}
                        data-testid={`preset-card-${p.id}`}
                      >
                        <div className="relative aspect-[3/2] bg-neutral-50 border-b border-neutral-100">
                          {/*
                            next/image cannot pre-optimize SVG — we use unoptimized
                            to serve the raw file. Alt text comes from the backend
                            preset name so screen readers announce it.
                           */}
                          <Image
                            src={p.thumbnailUrl}
                            alt={p.name}
                            fill
                            unoptimized
                            className="object-contain"
                          />
                          {isThisSelecting && (
                            <div
                              className="absolute inset-0 bg-white/70 flex items-center justify-center"
                              data-testid={`preset-card-spinner-${p.id}`}
                            >
                              <Loader2 className="w-6 h-6 animate-spin text-primary-600" />
                            </div>
                          )}
                        </div>
                        <div className="p-3">
                          <div className="flex items-start justify-between gap-2">
                            <h3 className="text-sm font-semibold text-neutral-900 leading-snug">
                              {p.name}
                            </h3>
                            <span className="shrink-0 rounded-full bg-primary-50 text-primary-700 text-xs font-medium px-2 py-0.5">
                              {p.totalCapacity} seats
                            </span>
                          </div>
                          <p className="mt-1 text-xs text-neutral-600 leading-snug">
                            {p.description}
                          </p>
                          <p className="mt-2 text-[11px] uppercase tracking-wide text-neutral-400">
                            {p.layoutType}
                          </p>
                        </div>
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>
        )}

        {/* Slice 8 S8.11: per-template delete confirmation. Lives at modal
            scope so it survives card re-renders and so we don't nest dialogs
            inside <li>s. variant="danger" matches the destructive intent. */}
        <ConfirmDialog
          open={templateToDelete !== null}
          onOpenChange={(next) => {
            if (!next && !deleteTemplate.isPending) setTemplateToDelete(null);
          }}
          title="Delete this template?"
          description={
            templateToDelete
              ? `"${templateToDelete.name}" will be permanently removed. This cannot be undone — you'll need to rebuild it from scratch if you change your mind.`
              : ''
          }
          confirmLabel="Delete"
          cancelLabel="Keep template"
          onConfirm={handleConfirmDeleteTemplate}
          variant="danger"
          isLoading={deleteTemplate.isPending}
        />

        {activeTab === 'mine' && (
          <div role="tabpanel" data-testid="preset-modal-tabpanel-mine">
            {templatesQuery.isLoading && (
              <div
                className="flex items-center justify-center py-16 text-neutral-500"
                data-testid="mine-modal-loading"
              >
                <Loader2 className="w-5 h-5 animate-spin mr-2" />
                Loading your templates…
              </div>
            )}

            {templatesQuery.isError && (
              <div
                className="flex flex-col items-center gap-3 py-10 text-red-700"
                data-testid="mine-modal-error"
              >
                <AlertCircle className="w-6 h-6" />
                <p className="text-sm">
                  Could not load your templates
                  {templatesQuery.error?.message ? ` — ${templatesQuery.error.message}` : ''}.
                </p>
                <Button variant="outline" onClick={() => templatesQuery.refetch()}>
                  Try again
                </Button>
              </div>
            )}

            {templatesQuery.data && templatesQuery.data.length === 0 && (
              <div
                className="py-10 text-center text-neutral-500"
                data-testid="mine-modal-empty"
              >
                <Layers className="w-8 h-8 mx-auto mb-2 text-neutral-400" aria-hidden="true" />
                <p className="text-sm">No saved templates yet.</p>
                <p className="text-xs text-neutral-400 mt-1">
                  Open a layout in the canvas editor and click{' '}
                  <span className="font-medium">Save as Template</span> to add one.
                </p>
              </div>
            )}

            {templatesQuery.data && templatesQuery.data.length > 0 && (
              <ul
                className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 py-2"
                data-testid="mine-modal-grid"
              >
                {templatesQuery.data.map((t) => {
                  const isThisSelecting = isSelectingMine && selectingMineId === t.id;
                  const disabled = isSelectingMine && !isThisSelecting;
                  return (
                    <li key={t.id} className="relative">
                      <button
                        type="button"
                        className={[
                          'w-full text-left rounded-lg border bg-white overflow-hidden',
                          'transition-shadow hover:shadow-md focus:outline-none',
                          'focus:ring-2 focus:ring-primary-500 focus:border-primary-500',
                          focusedId === t.id ? 'border-primary-500' : 'border-neutral-200',
                          disabled ? 'opacity-60 cursor-not-allowed' : '',
                          !onSelectMine ? 'cursor-default' : '',
                        ].join(' ')}
                        onClick={() => handlePickTemplate(t)}
                        onFocus={() => setFocusedId(t.id)}
                        onBlur={() => setFocusedId(null)}
                        disabled={disabled}
                        aria-busy={isThisSelecting}
                        data-testid={`mine-card-${t.id}`}
                      >
                        <div className="relative aspect-[3/2] bg-gradient-to-br from-primary-50 to-neutral-50 border-b border-neutral-100 flex items-center justify-center">
                          <Layers className="w-10 h-10 text-primary-300" aria-hidden="true" />
                          {isThisSelecting && (
                            <div
                              className="absolute inset-0 bg-white/70 flex items-center justify-center"
                              data-testid={`mine-card-spinner-${t.id}`}
                            >
                              <Loader2 className="w-6 h-6 animate-spin text-primary-600" />
                            </div>
                          )}
                        </div>
                        <div className="p-3">
                          <div className="flex items-start justify-between gap-2 pr-8">
                            <h3 className="text-sm font-semibold text-neutral-900 leading-snug">
                              {t.name}
                            </h3>
                            <span className="shrink-0 rounded-full bg-primary-50 text-primary-700 text-xs font-medium px-2 py-0.5">
                              {t.totalCapacity} seats
                            </span>
                          </div>
                          <p className="mt-2 text-[11px] uppercase tracking-wide text-neutral-400">
                            {t.layoutType}
                          </p>
                        </div>
                      </button>
                      {/* Slice 8 S8.11: Delete action — sibling <button> so we
                          don't nest interactive elements (invalid HTML). The
                          card-select button never fires because the click
                          stays on the Delete button's subtree. */}
                      <button
                        type="button"
                        aria-label={`Delete template ${t.name}`}
                        className={[
                          'absolute bottom-3 right-3 inline-flex items-center justify-center',
                          'h-7 w-7 rounded-md border border-neutral-200 bg-white text-neutral-500',
                          'hover:text-red-600 hover:border-red-300 hover:bg-red-50',
                          'focus:outline-none focus:ring-2 focus:ring-red-500',
                          'transition-colors',
                          disabled ? 'opacity-40 cursor-not-allowed' : '',
                        ].join(' ')}
                        onClick={(e) => {
                          e.stopPropagation();
                          setTemplateToDelete(t);
                        }}
                        disabled={disabled}
                        data-testid={`mine-card-delete-${t.id}`}
                      >
                        <Trash2 className="w-4 h-4" aria-hidden="true" />
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
