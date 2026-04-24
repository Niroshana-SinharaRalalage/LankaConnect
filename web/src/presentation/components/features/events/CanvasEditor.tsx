/**
 * Slice 8 S8.2: CanvasEditor — SSR-safe wrapper for the Konva editor stage.
 *
 * Konva touches `window` at import time, so the actual stage lives in
 * {@link CanvasEditorStage} and we lazy-load it via `next/dynamic` with
 * `ssr: false`. This file is the only module pages / modals should import.
 *
 * Today (S8.2) the stage renders the layout read-only. Future chunks wire
 * in interactions (drag, resize, rotation, toolbar, undo/redo, save).
 */

'use client';

import React from 'react';
import dynamic from 'next/dynamic';
import type { VenueLayoutDto } from '@/infrastructure/api/types/events.types';
import type { CanvasEditorStageProps } from './CanvasEditorStage';

// Dynamic import keeps Konva + react-konva (~180 KB gz) out of the SSR bundle
// and defers the client-side fetch until the editor actually mounts.
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

export function CanvasEditor({ layout, className }: CanvasEditorProps) {
  return <CanvasEditorStage layout={layout} className={className} />;
}

export default CanvasEditor;
