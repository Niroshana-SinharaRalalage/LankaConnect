/**
 * Slice 6 Chunk S6.7: PresetLibraryModal
 *
 * 8 industry-standard layout presets in a responsive grid. The organizer
 * clicks a card → modal calls onSelect(presetId) → parent calls
 * useCreateLayoutFromPreset mutation → layout materializes on the server.
 *
 * Thumbnails are static SVG files served from /public/layouts/presets/ so the
 * react-konva bundle stays lazy-loaded (architect soft-issue B1). No canvas
 * rendering in this modal.
 *
 * Integration point: SeatingSection (S6.9) mounts this modal behind a
 * "Choose a layout" button. This file is a pure presentational component —
 * it owns no layout state, makes no POST — delegated entirely to the parent.
 */

'use client';

import React, { useState, useCallback } from 'react';
import Image from 'next/image';
import { AlertCircle, Loader2 } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/presentation/components/ui/Dialog';
import { Button } from '@/presentation/components/ui/Button';
import { useLayoutPresets } from '@/presentation/hooks/useVenueLayouts';
import type { LayoutPresetDto } from '@/infrastructure/api/types/events.types';

export interface PresetLibraryModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /**
   * Fired when the organizer confirms a preset. The parent should POST
   * /api/venue-layouts/from-preset with the id and handle the 201 response.
   */
  onSelect: (preset: LayoutPresetDto) => void | Promise<void>;
  /**
   * Shows a spinner on the chosen card while the parent mutation is in flight.
   * Pass the parent mutation's `isPending` (or equivalent) here.
   */
  isSelecting?: boolean;
  /**
   * Which preset id is being confirmed — lets us restrict the spinner to the
   * clicked card rather than showing it everywhere.
   */
  selectingPresetId?: string | null;
}

export function PresetLibraryModal({
  open,
  onOpenChange,
  onSelect,
  isSelecting = false,
  selectingPresetId = null,
}: PresetLibraryModalProps) {
  const { data: presets, isLoading, isError, error, refetch } = useLayoutPresets({
    enabled: open, // only fetch when modal is open
  });

  const [focusedId, setFocusedId] = useState<string | null>(null);

  const handlePick = useCallback(
    async (preset: LayoutPresetDto) => {
      try {
        await onSelect(preset);
      } catch (e) {
        // Parent owns error surface; swallow here so the modal stays open.
        // Logging is still useful for dev observability.
        // eslint-disable-next-line no-console
        console.error('[PresetLibraryModal] onSelect threw:', e);
      }
    },
    [onSelect],
  );

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

        {isLoading && (
          <div
            className="flex items-center justify-center py-16 text-neutral-500"
            data-testid="preset-modal-loading"
          >
            <Loader2 className="w-5 h-5 animate-spin mr-2" />
            Loading presets…
          </div>
        )}

        {isError && (
          <div
            className="flex flex-col items-center gap-3 py-10 text-red-700"
            data-testid="preset-modal-error"
          >
            <AlertCircle className="w-6 h-6" />
            <p className="text-sm">
              Could not load presets{error?.message ? ` — ${error.message}` : ''}.
            </p>
            <Button variant="outline" onClick={() => refetch()}>
              Try again
            </Button>
          </div>
        )}

        {presets && presets.length === 0 && (
          <p className="py-10 text-center text-neutral-500">
            No presets are available.
          </p>
        )}

        {presets && presets.length > 0 && (
          <ul
            className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 py-2"
            data-testid="preset-modal-grid"
          >
            {presets.map((p) => {
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
                    onClick={() => handlePick(p)}
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
      </DialogContent>
    </Dialog>
  );
}
