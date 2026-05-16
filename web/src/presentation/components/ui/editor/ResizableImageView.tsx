/**
 * Phase 6A.147 — React NodeView for the resizable image node.
 *
 * Renders the <img> wrapped in a positioned container with a single SE-corner
 * drag handle. Aspect ratio is preserved (height: auto). Width is clamped to
 * [MIN_WIDTH_PX, parent contentDOM clientWidth] on pointerup.
 *
 * Interaction model:
 *   - Mouse + touch unified via Pointer Events API (single code path).
 *   - Pointer capture prevents the drag from being interrupted by ProseMirror's
 *     own selection logic when the pointer leaves the handle.
 *   - pointermove updates are throttled to one per animation frame via rAF.
 *   - On pointerup, the final width is committed via updateAttributes (so it
 *     enters the TipTap history stack — undo/redo round-trip).
 *
 * Keyboard a11y:
 *   - When the image node is the current ProseMirror selection, Shift+Arrow
 *     keys nudge the width by ±KEYBOARD_STEP_PX. Plain Arrow keys are left to
 *     ProseMirror so caret navigation stays intact.
 *
 * Observability:
 *   - Pointer handler is wrapped in try/catch; failures log to console.error
 *     so a drag bug in the wild leaves a trace without crashing the editor.
 *   - When NEXT_PUBLIC_DEBUG_EDITOR=1, every committed resize logs
 *     {src, oldWidth, newWidth} via console.debug.
 */
'use client';

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { NodeViewWrapper, type NodeViewProps } from '@tiptap/react';

const MIN_WIDTH_PX = 50;
const KEYBOARD_STEP_PX = 10;

function clampWidth(desired: number, max: number): number {
  if (!Number.isFinite(desired)) return MIN_WIDTH_PX;
  if (desired < MIN_WIDTH_PX) return MIN_WIDTH_PX;
  if (max > 0 && desired > max) return max;
  return Math.round(desired);
}

function isDebugEnabled(): boolean {
  if (typeof process === 'undefined') return false;
  return process.env.NEXT_PUBLIC_DEBUG_EDITOR === '1';
}

export function ResizableImageView(props: NodeViewProps) {
  const { node, updateAttributes, selected, editor, getPos } = props;

  const wrapperRef = useRef<HTMLSpanElement>(null);
  const imgRef = useRef<HTMLImageElement>(null);
  const dragStateRef = useRef<{
    startX: number;
    startWidth: number;
    maxWidth: number;
    rafId: number | null;
  } | null>(null);

  // Local live-width during drag so the <img> resizes smoothly without
  // dispatching a transaction per pointermove. Committed to the node's `width`
  // attribute on pointerup.
  const [liveWidth, setLiveWidth] = useState<number | null>(null);

  const persistedWidth = node.attrs.width as number | null;
  const displayWidth = liveWidth ?? persistedWidth;

  const computeMaxWidth = useCallback(() => {
    const wrapper = wrapperRef.current;
    if (!wrapper) return 0;
    // Walk up to the nearest block container to get a stable max width.
    const block = wrapper.closest('p, div, td, th, li, blockquote') as HTMLElement | null;
    return block?.clientWidth ?? wrapper.parentElement?.clientWidth ?? 0;
  }, []);

  const commitWidth = useCallback(
    (next: number | null) => {
      try {
        updateAttributes({ width: next });
        if (isDebugEnabled()) {
          // eslint-disable-next-line no-console
          console.debug('[ResizableImage] commit', {
            src: node.attrs.src,
            oldWidth: persistedWidth,
            newWidth: next,
          });
        }
      } catch (err) {
        // eslint-disable-next-line no-console
        console.error('[ResizableImage] commit failed:', err);
      }
    },
    [updateAttributes, node.attrs.src, persistedWidth],
  );

  const onHandlePointerDown = useCallback(
    (e: React.PointerEvent<HTMLSpanElement>) => {
      try {
        if (!imgRef.current) return;
        e.preventDefault();
        e.stopPropagation();
        const handle = e.currentTarget;
        handle.setPointerCapture(e.pointerId);

        const startWidth = imgRef.current.clientWidth || imgRef.current.naturalWidth || MIN_WIDTH_PX;
        const maxWidth = computeMaxWidth();
        dragStateRef.current = {
          startX: e.clientX,
          startWidth,
          maxWidth,
          rafId: null,
        };
        setLiveWidth(startWidth);
      } catch (err) {
        // eslint-disable-next-line no-console
        console.error('[ResizableImage] pointerdown failed:', err);
      }
    },
    [computeMaxWidth],
  );

  const onHandlePointerMove = useCallback((e: React.PointerEvent<HTMLSpanElement>) => {
    const state = dragStateRef.current;
    if (!state) return;
    try {
      const delta = e.clientX - state.startX;
      const desired = state.startWidth + delta;
      const next = clampWidth(desired, state.maxWidth);

      if (state.rafId !== null) return;
      state.rafId = window.requestAnimationFrame(() => {
        state.rafId = null;
        setLiveWidth(next);
      });
    } catch (err) {
      // eslint-disable-next-line no-console
      console.error('[ResizableImage] pointermove failed:', err);
    }
  }, []);

  const finishDrag = useCallback(
    (e: React.PointerEvent<HTMLSpanElement>) => {
      const state = dragStateRef.current;
      if (!state) return;
      try {
        if (state.rafId !== null) {
          window.cancelAnimationFrame(state.rafId);
          state.rafId = null;
        }
        if (e.currentTarget.hasPointerCapture(e.pointerId)) {
          e.currentTarget.releasePointerCapture(e.pointerId);
        }
        const final = liveWidth ?? state.startWidth;
        const clamped = clampWidth(final, state.maxWidth);
        commitWidth(clamped);
      } catch (err) {
        // eslint-disable-next-line no-console
        console.error('[ResizableImage] pointerup failed:', err);
      } finally {
        dragStateRef.current = null;
        setLiveWidth(null);
      }
    },
    [commitWidth, liveWidth],
  );

  // Keyboard a11y: Shift+Arrow nudges width when the image node is selected.
  useEffect(() => {
    if (!selected || typeof window === 'undefined') return;
    const onKeyDown = (e: KeyboardEvent) => {
      if (!e.shiftKey) return;
      if (e.key !== 'ArrowLeft' && e.key !== 'ArrowRight') return;
      try {
        const max = computeMaxWidth();
        const current =
          persistedWidth ?? imgRef.current?.clientWidth ?? imgRef.current?.naturalWidth ?? MIN_WIDTH_PX;
        const delta = e.key === 'ArrowRight' ? KEYBOARD_STEP_PX : -KEYBOARD_STEP_PX;
        const next = clampWidth(current + delta, max);
        e.preventDefault();
        commitWidth(next);
      } catch (err) {
        // eslint-disable-next-line no-console
        console.error('[ResizableImage] keyboard resize failed:', err);
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [selected, persistedWidth, computeMaxWidth, commitWidth]);

  const isEditable = editor?.isEditable ?? true;

  const imgStyle = useMemo(() => {
    if (!displayWidth) return undefined;
    return { width: `${displayWidth}px`, height: 'auto' as const };
  }, [displayWidth]);

  return (
    <NodeViewWrapper
      as="span"
      ref={wrapperRef}
      className={`resizable-image-wrapper${selected ? ' is-selected' : ''}`}
      data-drag-handle
    >
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img
        ref={imgRef}
        src={node.attrs.src as string}
        alt={(node.attrs.alt as string) ?? ''}
        title={(node.attrs.title as string) ?? undefined}
        width={persistedWidth ?? undefined}
        style={imgStyle}
        draggable={false}
      />
      {isEditable && selected && (
        <span
          role="slider"
          aria-label="Resize image"
          aria-valuemin={MIN_WIDTH_PX}
          aria-valuenow={displayWidth ?? imgRef.current?.naturalWidth ?? MIN_WIDTH_PX}
          tabIndex={0}
          className="resize-handle resize-handle-se"
          onPointerDown={onHandlePointerDown}
          onPointerMove={onHandlePointerMove}
          onPointerUp={finishDrag}
          onPointerCancel={finishDrag}
          data-testid={`resize-handle-${getPos?.()}`}
        />
      )}
    </NodeViewWrapper>
  );
}
