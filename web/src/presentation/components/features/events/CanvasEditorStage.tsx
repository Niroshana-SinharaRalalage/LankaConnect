/**
 * Slice 8 S8.2: CanvasEditorStage — Konva implementation for the editor surface.
 *
 * Wrapped by {@link CanvasEditor} via Next.js `dynamic()` with `ssr: false`. Never
 * import this file directly from a page — Konva touches `window` at import time.
 *
 * Today (S8.2) this is a read-only renderer: zones (rect + curve), tables
 * (round + rect), decorations (stage / dance-floor / aisle / door / wall /
 * text / image), a light dotted grid overlay, and a background matching
 * the layout's canvas config. No interaction yet — drag/resize/rotate land
 * in S8.3–S8.4, toolbar/property-panel in S8.5, undo/redo in S8.6.
 *
 * Seats are intentionally NOT rendered in the editor — they are auto-generated
 * per zone/table by the domain and organizers don't position individual seats.
 * The editor manipulates structural shapes; the registration-side SeatPicker
 * is the surface that shows seats.
 *
 * Reuses the parser / geometry helpers from `layoutGeometry.ts` so the editor
 * and the SeatPicker interpret geometry identically.
 */

'use client';

import React, { useEffect, useRef, useState } from 'react';
import { Stage, Layer, Rect, Circle, Text, Path, Group, Line } from 'react-konva';
import type {
  VenueLayoutDto,
  VenueZoneDto,
  VenueTableDto,
  VenueDecorationDto,
} from '@/infrastructure/api/types/events.types';
import {
  parseRectGeom,
  parseCurveGeom,
  parseRoundTableGeom,
  parseRectTableGeom,
  decorationStyle,
} from '@/presentation/utils/layoutGeometry';

const DEFAULT_CANVAS_WIDTH = 1000;
const DEFAULT_CANVAS_HEIGHT = 800;
const DEFAULT_BACKGROUND = '#FAFAF9';
const GRID_SPACING = 50;
const GRID_COLOR = '#E5E7EB';

export interface CanvasEditorStageProps {
  layout: VenueLayoutDto;
  /** Optional className on the wrapping div. */
  className?: string;
}

export function CanvasEditorStage({ layout, className }: CanvasEditorStageProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const [containerSize, setContainerSize] = useState({ width: 0, height: 0 });

  // Watch the container size so the Konva stage scales responsively as the
  // modal resizes (user drags viewport, device rotation, etc).
  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;
    const update = () => {
      setContainerSize({
        width: el.clientWidth,
        height: el.clientHeight,
      });
    };
    update();
    const ro = new ResizeObserver(update);
    ro.observe(el);
    return () => ro.disconnect();
  }, []);

  const canvasWidth = layout.canvas?.width ?? DEFAULT_CANVAS_WIDTH;
  const canvasHeight = layout.canvas?.height ?? DEFAULT_CANVAS_HEIGHT;
  const background = layout.canvas?.backgroundColor ?? DEFAULT_BACKGROUND;

  // Fit-scale so the whole canvas is visible within the container, no matter
  // the aspect ratio. 0.98 pads the canvas inside the container edges.
  const scale =
    containerSize.width === 0 || containerSize.height === 0
      ? 1
      : Math.min(
          containerSize.width / canvasWidth,
          containerSize.height / canvasHeight,
        ) * 0.98;

  const offsetX = (containerSize.width - canvasWidth * scale) / 2;
  const offsetY = (containerSize.height - canvasHeight * scale) / 2;

  return (
    <div
      ref={containerRef}
      className={className}
      data-testid="canvas-editor-stage"
      style={{ width: '100%', height: '100%', overflow: 'hidden' }}
    >
      {containerSize.width > 0 && containerSize.height > 0 && (
        <Stage
          width={containerSize.width}
          height={containerSize.height}
          scaleX={scale}
          scaleY={scale}
          x={offsetX}
          y={offsetY}
        >
          <Layer>
            {/* Canvas background */}
            <Rect
              x={0}
              y={0}
              width={canvasWidth}
              height={canvasHeight}
              fill={background}
              stroke="#D1D5DB"
              strokeWidth={1 / scale}
              listening={false}
            />
            {/* Light grid overlay — hints at where snap-to-grid will land in S8.3 */}
            <Grid width={canvasWidth} height={canvasHeight} scale={scale} />
          </Layer>

          <Layer>
            {/* Decorations drawn below zones so stage/aisle labels do not cover zone interiors */}
            {(layout.decorations ?? []).map((d) => (
              <DecorationShape key={d.id} decoration={d} />
            ))}
          </Layer>

          <Layer>
            {(layout.zones ?? []).map((z) => (
              <ZoneShape key={z.id} zone={z} />
            ))}
          </Layer>

          <Layer>
            {(layout.tables ?? []).map((t) => (
              <TableShape key={t.id} table={t} />
            ))}
          </Layer>
        </Stage>
      )}

      {containerSize.width === 0 && (
        <div className="flex items-center justify-center h-full text-sm text-neutral-500">
          Preparing canvas…
        </div>
      )}
    </div>
  );
}

// ─────────────────────────── Grid ───────────────────────────

function Grid({ width, height, scale }: { width: number; height: number; scale: number }) {
  const cols: number[] = [];
  for (let x = GRID_SPACING; x < width; x += GRID_SPACING) cols.push(x);
  const rows: number[] = [];
  for (let y = GRID_SPACING; y < height; y += GRID_SPACING) rows.push(y);
  const strokeWidth = 0.5 / scale;
  return (
    <Group listening={false} opacity={0.6}>
      {cols.map((x) => (
        <Line
          key={`c${x}`}
          points={[x, 0, x, height]}
          stroke={GRID_COLOR}
          strokeWidth={strokeWidth}
          dash={[2, 4]}
        />
      ))}
      {rows.map((y) => (
        <Line
          key={`r${y}`}
          points={[0, y, width, y]}
          stroke={GRID_COLOR}
          strokeWidth={strokeWidth}
          dash={[2, 4]}
        />
      ))}
    </Group>
  );
}

// ─────────────────────────── Zones ───────────────────────────

function ZoneShape({ zone }: { zone: VenueZoneDto }) {
  if (zone.shape === 'Curve') {
    const geom = parseCurveGeom(zone.geometry);
    if (!geom) return null;
    const { centerX, centerY, radius, startAngleDeg, sweepAngleDeg } = geom;
    const toRad = (d: number) => (d * Math.PI) / 180;
    const endAngleDeg = startAngleDeg + sweepAngleDeg;
    const start = {
      x: centerX + radius * Math.cos(toRad(startAngleDeg)),
      y: centerY + radius * Math.sin(toRad(startAngleDeg)),
    };
    const end = {
      x: centerX + radius * Math.cos(toRad(endAngleDeg)),
      y: centerY + radius * Math.sin(toRad(endAngleDeg)),
    };
    const largeArc = Math.abs(sweepAngleDeg) > 180 ? 1 : 0;
    const sweepFlag = sweepAngleDeg >= 0 ? 1 : 0;
    const d = `M ${centerX} ${centerY} L ${start.x} ${start.y} A ${radius} ${radius} 0 ${largeArc} ${sweepFlag} ${end.x} ${end.y} Z`;
    return (
      <Group>
        <Path data={d} fill={zone.color} opacity={0.25} stroke={zone.color} strokeWidth={2} />
        <Text
          x={centerX - radius}
          y={centerY + radius / 3}
          width={radius * 2}
          align="center"
          text={zone.name}
          fontSize={16}
          fontStyle="bold"
          fontFamily="Inter, system-ui, sans-serif"
          fill={zone.color}
        />
      </Group>
    );
  }

  // Rect / Polygon fall back to rect rendering; polygon geometry is
  // undefined in today's presets — rendering a placeholder-free rect keeps
  // the canvas useful even if someone hand-crafts polygon geometry.
  const geom = parseRectGeom(zone.geometry);
  if (!geom) {
    return (
      <Text
        x={40}
        y={40}
        text={`⚠ zone ${zone.name} has invalid geometry`}
        fontSize={14}
        fontFamily="Inter, system-ui, sans-serif"
        fill="#9ca3af"
      />
    );
  }
  const labelFontSize = Math.max(14, Math.min(26, geom.width / 24));
  const rotationOrigin =
    geom.rotation ? { x: geom.x + geom.width / 2, y: geom.y + geom.height / 2 } : null;
  return (
    <Group rotation={geom.rotation ?? 0} x={rotationOrigin?.x ?? 0} y={rotationOrigin?.y ?? 0}>
      <Rect
        x={geom.x - (rotationOrigin?.x ?? 0)}
        y={geom.y - (rotationOrigin?.y ?? 0)}
        width={geom.width}
        height={geom.height}
        cornerRadius={6}
        fill={zone.color}
        opacity={0.18}
        stroke={zone.color}
        strokeWidth={2}
      />
      <Text
        x={geom.x - (rotationOrigin?.x ?? 0)}
        y={geom.y - (rotationOrigin?.y ?? 0) + Math.min(12, geom.height / 2)}
        width={geom.width}
        align="center"
        text={zone.name}
        fontSize={labelFontSize}
        fontStyle="bold"
        fontFamily="Inter, system-ui, sans-serif"
        fill={zone.color}
      />
    </Group>
  );
}

// ─────────────────────────── Tables ───────────────────────────

function TableShape({ table }: { table: VenueTableDto }) {
  if (table.shape === 'Round') {
    const geom = parseRoundTableGeom(table.geometry);
    if (!geom) return null;
    return (
      <Group>
        <Circle
          x={geom.centerX}
          y={geom.centerY}
          radius={geom.radius}
          fill="#FDE68A"
          stroke="#B45309"
          strokeWidth={2}
        />
        <Text
          x={geom.centerX - geom.radius}
          y={geom.centerY - 7}
          width={geom.radius * 2}
          align="center"
          text={table.label}
          fontSize={14}
          fontStyle="bold"
          fontFamily="Inter, system-ui, sans-serif"
          fill="#92400E"
        />
      </Group>
    );
  }

  const geom = parseRectTableGeom(table.geometry);
  if (!geom) return null;
  const x = geom.centerX - geom.width / 2;
  const y = geom.centerY - geom.height / 2;
  return (
    <Group
      rotation={geom.rotation ?? 0}
      x={geom.rotation ? geom.centerX : 0}
      y={geom.rotation ? geom.centerY : 0}
    >
      <Rect
        x={geom.rotation ? -geom.width / 2 : x}
        y={geom.rotation ? -geom.height / 2 : y}
        width={geom.width}
        height={geom.height}
        cornerRadius={4}
        fill="#FEE2E2"
        stroke="#B91C1C"
        strokeWidth={2}
      />
      <Text
        x={geom.rotation ? -geom.width / 2 : x}
        y={(geom.rotation ? -geom.height / 2 : y) + geom.height / 2 - 7}
        width={geom.width}
        align="center"
        text={table.label}
        fontSize={14}
        fontStyle="bold"
        fontFamily="Inter, system-ui, sans-serif"
        fill="#7F1D1D"
      />
    </Group>
  );
}

// ─────────────────────────── Decorations ───────────────────────────

function DecorationShape({ decoration }: { decoration: VenueDecorationDto }) {
  const geom = parseRectGeom(decoration.geometry);
  if (!geom) return null;
  const style = decorationStyle(decoration.kind);
  const rotationOrigin =
    geom.rotation ? { x: geom.x + geom.width / 2, y: geom.y + geom.height / 2 } : null;
  const labelText = decoration.label ?? style.label;
  return (
    <Group rotation={geom.rotation ?? 0} x={rotationOrigin?.x ?? 0} y={rotationOrigin?.y ?? 0}>
      <Rect
        x={geom.x - (rotationOrigin?.x ?? 0)}
        y={geom.y - (rotationOrigin?.y ?? 0)}
        width={geom.width}
        height={geom.height}
        cornerRadius={decoration.kind === 'Stage' ? 10 : 4}
        fill={style.fill}
        stroke={style.stroke}
        strokeWidth={2}
        dash={decoration.kind === 'DanceFloor' ? [10, 6] : undefined}
      />
      {labelText && (
        <Text
          x={geom.x - (rotationOrigin?.x ?? 0)}
          y={geom.y - (rotationOrigin?.y ?? 0) + geom.height / 2 - 10}
          width={geom.width}
          align="center"
          text={labelText}
          fontSize={Math.max(14, Math.min(28, geom.width / 22))}
          fontStyle="bold"
          fontFamily="Inter, system-ui, sans-serif"
          fill={style.labelColor}
        />
      )}
    </Group>
  );
}

export default CanvasEditorStage;
