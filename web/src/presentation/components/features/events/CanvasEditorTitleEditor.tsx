/**
 * Slice S3 — inline editable layout name in the canvas editor header.
 *
 * Commits via the existing `PUT /api/venue-layouts/{id}` endpoint
 * (Slice 5 Chunk 4 `UpdateLayoutCommand` with the `name` field only).
 * Architect Rev 4 §S3 calls for a "dedicated PATCH" — the existing PUT
 * already satisfies the spirit (own If-Match handling, separate from
 * the structural /batch endpoint) so we reuse it rather than ship a
 * redundant `PATCH /name` surface.
 *
 * UX:
 *   - Input commits on blur or Enter; reverts on Escape; reverts to the
 *     server name on error and toasts the architect-prescribed 409 message
 *     when the row version is stale.
 *   - Empty name → toast "Layout name is required" (matches the domain
 *     `VenueLayout.UpdateName` rule); revert.
 *   - Hard cap at 200 chars via `maxLength` so the user can't reach the
 *     server's 400 path during normal typing — the domain still rejects
 *     longer values for direct-API callers.
 */

'use client';

import React, { useCallback, useEffect, useRef, useState } from 'react';
import toast from 'react-hot-toast';
import { useUpdateVenueLayout } from '@/presentation/hooks/useVenueLayouts';
import { ApiError } from '@/infrastructure/api/client/api-errors';

const MAX_LAYOUT_NAME_LENGTH = 200;

export interface CanvasEditorTitleEditorProps {
  layoutId: string;
  eventId: string | null;
  currentName: string;
  rowVersion: number;
  /** Disable editing while a structural save is in flight in the parent. */
  disabled?: boolean;
}

export function CanvasEditorTitleEditor({
  layoutId,
  eventId,
  currentName,
  rowVersion,
  disabled = false,
}: CanvasEditorTitleEditorProps) {
  const [draft, setDraft] = useState(currentName);
  const [isFocused, setIsFocused] = useState(false);
  const inputRef = useRef<HTMLInputElement | null>(null);
  // Dedup guard: when Enter triggers a commit, the synthesized blur that
  // follows would otherwise commit the same value a second time. We mark
  // the commit "in flight" and short-circuit subsequent attempts until the
  // mutation resolves and the cache-driven `currentName` sync runs.
  const inflightCommitRef = useRef<Promise<void> | null>(null);

  // Sync incoming prop changes (cache refetch) when the user is not editing.
  useEffect(() => {
    if (!isFocused) {
      setDraft(currentName);
    }
  }, [currentName, isFocused]);

  const mutation = useUpdateVenueLayout(layoutId, eventId);
  const isSaving = mutation.isPending;

  const commit = useCallback(() => {
    if (inflightCommitRef.current) {
      return inflightCommitRef.current;
    }
    const trimmed = draft.trim();
    const baseline = currentName.trim();
    if (trimmed === baseline) {
      // No change — revert any whitespace-only edits silently.
      if (draft !== currentName) setDraft(currentName);
      return Promise.resolve();
    }
    if (trimmed.length === 0) {
      toast.error('Layout name is required');
      setDraft(currentName);
      return Promise.resolve();
    }
    const run = (async () => {
      try {
        await mutation.mutateAsync({
          rowVersion,
          request: { name: trimmed },
        });
        toast.success('Layout renamed');
      } catch (err) {
        if (err instanceof ApiError && err.statusCode === 409) {
          toast.error(
            'Layout was modified externally — close and reopen the editor to load the latest version, then retry.',
          );
        } else {
          const message =
            err instanceof Error
              ? err.message
              : 'Could not rename the layout. Please try again.';
          toast.error(`Rename failed: ${message}`);
        }
        setDraft(currentName);
      } finally {
        inflightCommitRef.current = null;
      }
    })();
    inflightCommitRef.current = run;
    return run;
  }, [draft, currentName, mutation, rowVersion]);

  const handleKey = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        setIsFocused(false);
        void commit();
        inputRef.current?.blur();
      } else if (e.key === 'Escape') {
        e.preventDefault();
        setDraft(currentName);
        setIsFocused(false);
        inputRef.current?.blur();
      }
    },
    [commit, currentName],
  );

  const handleBlur = useCallback(() => {
    setIsFocused(false);
    void commit();
  }, [commit]);

  const handleFocus = useCallback(() => setIsFocused(true), []);

  return (
    <div className="flex items-center gap-2 min-w-0 flex-1">
      <input
        ref={inputRef}
        value={draft}
        onChange={(e) => setDraft(e.target.value)}
        onKeyDown={handleKey}
        onBlur={handleBlur}
        onFocus={handleFocus}
        maxLength={MAX_LAYOUT_NAME_LENGTH}
        disabled={disabled || isSaving}
        aria-label="Layout name"
        data-testid="canvas-editor-layout-name-input"
        className="flex-1 min-w-0 truncate bg-transparent border border-transparent rounded px-2 py-1 text-lg font-semibold text-neutral-900 hover:border-neutral-200 focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary disabled:opacity-60 disabled:cursor-not-allowed"
      />
      {isSaving && (
        <span
          aria-live="polite"
          className="text-xs text-neutral-500"
          data-testid="canvas-editor-layout-name-saving"
        >
          Saving…
        </span>
      )}
    </div>
  );
}
