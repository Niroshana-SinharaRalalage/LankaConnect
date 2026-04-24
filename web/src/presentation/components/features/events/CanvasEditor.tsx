/**
 * Slice 8 S8.2+S8.3: CanvasEditor — SSR-safe wrapper + editor state.
 *
 * Konva touches `window` at import time, so the actual stage lives in
 * {@link CanvasEditorStage} and we lazy-load it via `next/dynamic` with
 * `ssr: false`. This file is the only module pages / modals should import.
 *
 * S8.3 lifts editor state to this wrapper so S8.6 undo/redo can sit at the
 * same level without another refactor:
 *   - `selected: CanvasItemRef | null` — currently selected shape, or null.
 *   - `draftGeometryByKey: Record<string, string>` — in-progress geometry
 *     overrides per item, keyed by refKey(ref). Seeds empty; each drag
 *     end adds an entry. Save (S8.8) will materialize these into a
 *     `PUT /api/venue-layouts/{id}/batch` payload and clear the draft.
 */

'use client';

import React, { useCallback, useState } from 'react';
import dynamic from 'next/dynamic';
import type { VenueLayoutDto } from '@/infrastructure/api/types/events.types';
import type { CanvasEditorStageProps } from './CanvasEditorStage';
import { refKey, type CanvasItemRef } from '@/presentation/utils/canvasEditorGeometry';

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
  const [selected, setSelected] = useState<CanvasItemRef | null>(null);
  const [draftGeometryByKey, setDraftGeometryByKey] = useState<Record<string, string>>({});

  const handleGeometryChange = useCallback(
    (ref: CanvasItemRef, geometryJson: string) => {
      setDraftGeometryByKey((prev) => ({
        ...prev,
        [refKey(ref)]: geometryJson,
      }));
    },
    [],
  );

  return (
    <CanvasEditorStage
      layout={layout}
      className={className}
      selected={selected}
      onSelect={setSelected}
      draftGeometryByKey={draftGeometryByKey}
      onGeometryChange={handleGeometryChange}
    />
  );
}

export default CanvasEditor;
