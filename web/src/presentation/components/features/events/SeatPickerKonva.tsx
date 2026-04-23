/**
 * Slice 7 Chunk S7.1: SeatPickerKonva — the Konva implementation.
 *
 * This file contains the actual react-konva render code. It is NEVER imported
 * directly by callers — always go through [SeatPicker.tsx](./SeatPicker.tsx),
 * which uses Next.js dynamic() with `ssr: false` to keep the ~180 KB Konva
 * bundle out of the server-rendered HTML and code-splits it into its own chunk.
 *
 * S7.1 scope is intentionally minimal: the Stage renders the layout's canvas
 * background. S7.2 adds zones / tables / decorations / seats. S7.3 adds tier
 * filtering + per-attendee selection. S7.4 wires the existing hold hooks.
 * S7.5 adds mobile pinch/zoom. Keeping the chunks small de-risks the shift
 * from the DOM-based SeatSelector to a canvas-based picker.
 *
 * Existing SeatSelector is NOT touched in this chunk — SeatPicker is
 * side-by-side so the registration flow keeps working while the rewrite is
 * staged.
 */

'use client';

import React from 'react';
import { Stage, Layer, Rect } from 'react-konva';
import type { VenueLayoutDto } from '@/infrastructure/api/types/events.types';

export interface SeatPickerKonvaProps {
  layout: VenueLayoutDto;
  /**
   * Width of the rendered canvas in CSS pixels. The Konva stage scales the
   * layout's virtual canvas (typically 1200×800) to fit.
   */
  width?: number;
  /**
   * Height of the rendered canvas in CSS pixels. Defaults to preserve the
   * layout's aspect ratio given `width`.
   */
  height?: number;
}

const DEFAULT_WIDTH = 960;

export function SeatPickerKonva({ layout, width = DEFAULT_WIDTH, height }: SeatPickerKonvaProps) {
  const canvasWidth = layout.canvas?.width ?? 1200;
  const canvasHeight = layout.canvas?.height ?? 800;
  const background = layout.canvas?.backgroundColor ?? '#ffffff';

  const aspect = canvasWidth / canvasHeight;
  const stageWidth = width;
  const stageHeight = height ?? Math.round(stageWidth / aspect);
  const scale = stageWidth / canvasWidth;

  return (
    <Stage
      width={stageWidth}
      height={stageHeight}
      scaleX={scale}
      scaleY={scale}
      data-testid="seat-picker-stage"
    >
      <Layer data-testid="seat-picker-background-layer">
        <Rect
          x={0}
          y={0}
          width={canvasWidth}
          height={canvasHeight}
          fill={background}
          stroke="#e5e7eb"
          strokeWidth={2}
        />
      </Layer>
    </Stage>
  );
}

export default SeatPickerKonva;
