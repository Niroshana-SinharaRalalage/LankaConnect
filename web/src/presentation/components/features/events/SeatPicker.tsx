/**
 * Slice 7 Chunk S7.1: SeatPicker — SSR-safe dynamic wrapper.
 *
 * Loads [SeatPickerKonva](./SeatPickerKonva.tsx) via Next.js `dynamic()` with
 * `ssr: false` and a skeleton fallback. The react-konva / konva bundle
 * (~180 KB gz) therefore:
 *   • never runs on the server (konva needs the DOM / HTMLCanvasElement),
 *   • lands in its own code-split chunk the browser fetches only when the
 *     picker mounts (i.e. when an attendee opens the seat-selection step),
 *   • stays out of the shared bundle that non-seated pages load.
 *
 * Keep this wrapper thin — its only job is the dynamic boundary. Real render
 * logic lives in SeatPickerKonva so that any regression in the Konva code
 * never changes the wrapper's SSR guarantees.
 *
 * Scope boundary: S7.1 ships the shell only. Visuals (zones, tables,
 * decorations, seats), tier filter, selection model, hold timer, mobile
 * pinch/zoom, and email/PDF hooks land in S7.2 – S7.8. SeatSelector remains
 * the production picker until S7.6 swaps the registration call site over.
 */

'use client';

import React from 'react';
import dynamic from 'next/dynamic';
import { Armchair } from 'lucide-react';
import type { SeatPickerKonvaProps } from './SeatPickerKonva';

/**
 * Skeleton rendered while the Konva chunk is fetched. Matches the standard
 * aspect ratio used by the LayoutPreview so the page doesn't jump when the
 * real canvas lands.
 */
function SeatPickerSkeleton() {
  return (
    <div
      data-testid="seat-picker-skeleton"
      className="w-full aspect-[3/2] max-w-4xl rounded-md border border-dashed border-neutral-300 bg-neutral-50 flex flex-col items-center justify-center gap-2 text-neutral-500"
      aria-busy="true"
      aria-label="Loading seat picker"
    >
      <Armchair className="w-6 h-6 animate-pulse" aria-hidden="true" />
      <span className="text-sm">Loading seat picker…</span>
    </div>
  );
}

/**
 * Next.js `dynamic()` boundary. `ssr: false` keeps konva out of the server
 * bundle; `loading` shows a skeleton the first time the chunk is fetched.
 */
const SeatPickerKonva = dynamic(
  () => import('./SeatPickerKonva').then((m) => m.SeatPickerKonva),
  {
    ssr: false,
    loading: () => <SeatPickerSkeleton />,
  },
);

export type { SeatPickerKonvaProps as SeatPickerProps };

export function SeatPicker(props: SeatPickerKonvaProps) {
  return <SeatPickerKonva {...props} />;
}

export default SeatPicker;
