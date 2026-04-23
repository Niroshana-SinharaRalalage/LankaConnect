/**
 * Slice 7 Chunk S7.1–S7.2: SeatPickerKonva — the Konva implementation.
 *
 * Wrapped by [SeatPicker.tsx](./SeatPicker.tsx) via Next.js dynamic() with
 * `ssr: false`. Never import this file directly from a page.
 *
 * S7.1 shipped the Stage + background Rect. S7.2 adds structural shapes:
 * zones (rect / curve / polygon — polygon degrades to placeholder text),
 * tables (round / rect / square), decorations (stage / dance-floor /
 * aisle / door / wall / text / image).
 *
 * Seat rendering (status color coding, tier filter, selection) lands in S7.3
 * alongside the tier model. Keeping the chunk small de-risks each step.
 */

'use client';

import React from 'react';
import {
  Stage,
  Layer,
  Rect,
  Circle,
  Text,
  Path,
  Group,
} from 'react-konva';
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
  type RoundTableGeom,
} from '@/presentation/utils/layoutGeometry';

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
      {/* Background + decorations */}
      <Layer>
        <Rect
          x={0}
          y={0}
          width={canvasWidth}
          height={canvasHeight}
          fill={background}
          stroke="#e5e7eb"
          strokeWidth={2}
        />
        {(layout.decorations ?? []).map((d) => (
          <DecorationShape key={d.id} decoration={d} />
        ))}
      </Layer>

      {/* Zones */}
      <Layer>
        {(layout.zones ?? []).map((z) => (
          <ZoneShape key={z.id} zone={z} />
        ))}
      </Layer>

      {/* Tables */}
      <Layer>
        {(layout.tables ?? []).map((t) => (
          <TableShape key={t.id} table={t} />
        ))}
      </Layer>
    </Stage>
  );
}

export default SeatPickerKonva;

// ─────────────────────────── Zones ───────────────────────────

function ZoneShape({ zone }: { zone: VenueZoneDto }) {
  switch (zone.shape) {
    case 'Curve':
      return <CurveZone zone={zone} />;
    case 'Polygon':
      return <ZonePlaceholder zone={zone} />;
    case 'Rect':
    default:
      return <RectZone zone={zone} />;
  }
}

function RectZone({ zone }: { zone: VenueZoneDto }) {
  const geom = parseRectGeom(zone.geometry);
  if (!geom) return <ZonePlaceholder zone={zone} />;
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
        strokeWidth={3}
      />
      {zone.name && (
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
      )}
    </Group>
  );
}

function CurveZone({ zone }: { zone: VenueZoneDto }) {
  const geom = parseCurveGeom(zone.geometry);
  if (!geom) return <ZonePlaceholder zone={zone} />;
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
    <Path
      data={d}
      fill={zone.color}
      opacity={0.25}
      stroke={zone.color}
      strokeWidth={3}
    />
  );
}

function ZonePlaceholder({ zone }: { zone: VenueZoneDto }) {
  return (
    <Text
      x={40}
      y={40}
      text={zone.name}
      fontSize={16}
      fontFamily="Inter, system-ui, sans-serif"
      fill="#9ca3af"
    />
  );
}

// ─────────────────────────── Tables ───────────────────────────

function TableShape({ table }: { table: VenueTableDto }) {
  if (table.shape === 'Round') {
    const geom = parseRoundTableGeom(table.geometry);
    if (!geom) return null;
    return <RoundTable table={table} geom={geom} />;
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
        fill="#fee2e2"
        stroke="#b91c1c"
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
        fill="#7f1d1d"
      />
    </Group>
  );
}

function RoundTable({ table, geom }: { table: VenueTableDto; geom: RoundTableGeom }) {
  return (
    <Group>
      <Circle
        x={geom.centerX}
        y={geom.centerY}
        radius={geom.radius}
        fill="#fde68a"
        stroke="#b45309"
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
        fill="#92400e"
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
