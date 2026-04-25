/**
 * Slice 6 Chunk S6.8: LayoutPreview
 *
 * Read-only renderer for a VenueLayoutDto. Draws zones, tables, decorations,
 * and seats onto a single SVG scaled to fit its container. Zero interaction
 * — picking/zooming/panning live in SeatPicker (Slice 7) and the canvas
 * editor (Slice 8).
 *
 * Intentional SVG-not-react-konva choice (scoped to Slice 6): the plan called
 * for react-konva but this preview is static, so adding a 180KB dependency
 * for a rendering surface that needs no interactivity would be scope creep.
 * Slice 7's SeatPicker brings react-konva; if a future iteration wants to
 * share that renderer for the preview, the swap is internal — the prop
 * contract (`layout: VenueLayoutDto`) stays identical.
 *
 * Geometry parsing is tolerant: the domain persists `"{}"` as the default,
 * and organizers can later hand-edit geometry JSON. Malformed or missing
 * fields degrade to a sensible placeholder rather than crashing the whole
 * preview.
 */

'use client';

import React from 'react';
import type {
  VenueLayoutDto,
  VenueZoneDto,
  VenueTableDto,
  VenueDecorationDto,
  SeatDto,
} from '@/infrastructure/api/types/events.types';
import {
  parseRectGeom,
  parseCurveGeom,
  parseRoundTableGeom,
  parseRectTableGeom,
  decorationStyle,
} from '@/presentation/utils/layoutGeometry';

export interface LayoutPreviewProps {
  layout: VenueLayoutDto;
  /**
   * Optional className for the outer wrapper. Use it to set max-width /
   * aspect ratio (e.g., `max-w-3xl aspect-[3/2]`). The internal SVG always
   * preserves the layout's canvas aspect ratio via viewBox.
   */
  className?: string;
  /**
   * When true, draws every seat as a tiny dot. Costly on very large layouts
   * (500+ seats) so the caller can disable it for thumbnail-size previews.
   */
  showSeats?: boolean;
  /** Accessible label; falls back to the layout's name. */
  ariaLabel?: string;
}

// Geometry interfaces + parsers moved to '@/presentation/utils/layoutGeometry'
// so SeatPickerKonva (Slice 7) and LayoutPreview stay behaviorally identical.

const DEFAULT_CANVAS_WIDTH = 1200;
const DEFAULT_CANVAS_HEIGHT = 800;

export function LayoutPreview({
  layout,
  className,
  showSeats = true,
  ariaLabel,
}: LayoutPreviewProps) {
  const width = layout.canvas?.width ?? DEFAULT_CANVAS_WIDTH;
  const height = layout.canvas?.height ?? DEFAULT_CANVAS_HEIGHT;
  const background = layout.canvas?.backgroundColor ?? '#ffffff';

  return (
    <div
      data-testid="layout-preview"
      className={className ?? 'w-full aspect-[3/2] max-w-3xl'}
    >
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox={`0 0 ${width} ${height}`}
        role="img"
        aria-label={ariaLabel ?? `Layout preview: ${layout.name}`}
        className="w-full h-full block"
        preserveAspectRatio="xMidYMid meet"
      >
        <rect
          x={0}
          y={0}
          width={width}
          height={height}
          fill={background}
          stroke="#e5e7eb"
          strokeWidth={2}
          data-testid="preview-canvas"
        />

        <g data-testid="preview-decorations">
          {(layout.decorations ?? []).map((d) => (
            <DecorationShape key={d.id} decoration={d} />
          ))}
        </g>

        <g data-testid="preview-zones">
          {(layout.zones ?? []).map((z) => (
            <ZoneShape key={z.id} zone={z} showSeats={showSeats} />
          ))}
        </g>

        <g data-testid="preview-tables">
          {(layout.tables ?? []).map((t) => (
            <TableShape key={t.id} table={t} showSeats={showSeats} />
          ))}
        </g>
      </svg>
    </div>
  );
}

// ───────────────────────────── Zones ─────────────────────────────

function ZoneShape({
  zone,
  showSeats,
}: {
  zone: VenueZoneDto;
  showSeats: boolean;
}) {
  switch (zone.shape) {
    case 'Curve': {
      const geom = parseCurveGeom(zone.geometry);
      if (!geom) return <ZonePlaceholder zone={zone} />;
      return (
        <g data-testid={`zone-${zone.id}`}>
          <ArcPath
            centerX={geom.centerX}
            centerY={geom.centerY}
            radius={geom.radius}
            startAngleDeg={geom.startAngleDeg}
            sweepAngleDeg={geom.sweepAngleDeg}
            fill={zone.color}
            opacity={0.25}
            stroke={zone.color}
            strokeWidth={3}
          />
          {showSeats && <SeatDots seats={zone.seats} color={zone.color} />}
        </g>
      );
    }
    case 'Polygon': {
      // Polygon geometry not used by any Slice 6 preset; render a marker.
      return <ZonePlaceholder zone={zone} />;
    }
    case 'Rect':
    default: {
      const geom = parseRectGeom(zone.geometry);
      if (!geom) return <ZonePlaceholder zone={zone} />;
      return (
        <g data-testid={`zone-${zone.id}`}>
          <rect
            x={geom.x}
            y={geom.y}
            width={geom.width}
            height={geom.height}
            rx={6}
            fill={zone.color}
            fillOpacity={0.18}
            stroke={zone.color}
            strokeWidth={3}
            transform={
              geom.rotation ? `rotate(${geom.rotation} ${geom.x + geom.width / 2} ${geom.y + geom.height / 2})` : undefined
            }
          />
          {zone.name && (
            <text
              x={geom.x + geom.width / 2}
              y={geom.y + Math.min(28, geom.height / 2)}
              fontSize={Math.max(14, Math.min(26, geom.width / 24))}
              fontFamily="Inter, system-ui, sans-serif"
              fontWeight={600}
              fill={zone.color}
              textAnchor="middle"
              dominantBaseline="hanging"
            >
              {zone.name}
            </text>
          )}
          {showSeats && <SeatDots seats={zone.seats} color={zone.color} />}
        </g>
      );
    }
  }
}

function ZonePlaceholder({ zone }: { zone: VenueZoneDto }) {
  return (
    <g data-testid={`zone-placeholder-${zone.id}`}>
      <text
        x={40}
        y={40}
        fontSize={16}
        fill="#9ca3af"
        fontFamily="Inter, system-ui, sans-serif"
      >
        {zone.name}
      </text>
    </g>
  );
}

// ───────────────────────────── Tables ─────────────────────────────

function TableShape({
  table,
  showSeats,
}: {
  table: VenueTableDto;
  showSeats: boolean;
}) {
  if (table.shape === 'Round') {
    const geom = parseRoundTableGeom(table.geometry);
    if (!geom) return null;
    return (
      <g data-testid={`table-${table.id}`}>
        <circle
          cx={geom.centerX}
          cy={geom.centerY}
          r={geom.radius}
          fill="#fde68a"
          stroke="#b45309"
          strokeWidth={2}
        />
        <text
          x={geom.centerX}
          y={geom.centerY + 5}
          fontSize={14}
          fontFamily="Inter, system-ui, sans-serif"
          fontWeight={600}
          fill="#92400e"
          textAnchor="middle"
        >
          {table.label}
        </text>
        {showSeats && <RadialSeatDots table={table} geom={geom} />}
      </g>
    );
  }
  const geom = parseRectTableGeom(table.geometry);
  if (!geom) return null;
  const x = geom.centerX - geom.width / 2;
  const y = geom.centerY - geom.height / 2;
  return (
    <g data-testid={`table-${table.id}`}>
      <rect
        x={x}
        y={y}
        width={geom.width}
        height={geom.height}
        rx={4}
        fill="#fee2e2"
        stroke="#b91c1c"
        strokeWidth={2}
        transform={
          geom.rotation ? `rotate(${geom.rotation} ${geom.centerX} ${geom.centerY})` : undefined
        }
      />
      <text
        x={geom.centerX}
        y={geom.centerY + 4}
        fontSize={14}
        fontFamily="Inter, system-ui, sans-serif"
        fontWeight={600}
        fill="#7f1d1d"
        textAnchor="middle"
      >
        {table.label}
      </text>
    </g>
  );
}

function RadialSeatDots({
  table,
  geom,
}: {
  table: VenueTableDto;
  geom: import('@/presentation/utils/layoutGeometry').RoundTableGeom;
}) {
  if (table.seats.length === 0) return null;
  const seatR = Math.max(4, geom.radius * 0.12);
  const ringR = geom.radius + seatR + 2;
  const step = 360 / table.seats.length;
  return (
    <g>
      {table.seats.map((_, i) => {
        const angleRad = ((step * i) * Math.PI) / 180;
        const sx = geom.centerX + ringR * Math.cos(angleRad);
        const sy = geom.centerY + ringR * Math.sin(angleRad);
        return (
          <circle
            key={i}
            cx={sx}
            cy={sy}
            r={seatR}
            fill="#b45309"
            opacity={0.9}
          />
        );
      })}
    </g>
  );
}

// ───────────────────────────── Decorations ─────────────────────────────

function DecorationShape({ decoration }: { decoration: VenueDecorationDto }) {
  const geom = parseRectGeom(decoration.geometry);
  if (!geom) return null;
  const { fill, stroke, label, labelColor } = decorationStyle(decoration.kind);
  return (
    <g data-testid={`decoration-${decoration.id}`}>
      <rect
        x={geom.x}
        y={geom.y}
        width={geom.width}
        height={geom.height}
        rx={decoration.kind === 'Stage' ? 10 : 4}
        fill={fill}
        stroke={stroke}
        strokeWidth={2}
        strokeDasharray={decoration.kind === 'DanceFloor' ? '10 6' : undefined}
        transform={
          geom.rotation ? `rotate(${geom.rotation} ${geom.x + geom.width / 2} ${geom.y + geom.height / 2})` : undefined
        }
      />
      {(decoration.label ?? label) && (
        <text
          x={geom.x + geom.width / 2}
          y={geom.y + geom.height / 2 + 6}
          fontSize={Math.max(14, Math.min(28, geom.width / 22))}
          fontFamily="Inter, system-ui, sans-serif"
          fontWeight={600}
          fill={labelColor}
          textAnchor="middle"
        >
          {decoration.label ?? label}
        </text>
      )}
    </g>
  );
}

// decorationStyle moved to '@/presentation/utils/layoutGeometry'.

// ───────────────────────────── Shared ─────────────────────────────

function SeatDots({ seats, color }: { seats: SeatDto[]; color: string }) {
  if (seats.length === 0) return null;
  // The domain generator does not currently write per-seat X/Y for theater
  // zones — only Row + Number. Skipping dot rendering when we have no
  // canvas-space coordinates keeps the preview honest.
  const placed = seats.filter((s) => s.x !== null && s.y !== null);
  if (placed.length === 0) return null;
  return (
    <g>
      {placed.map((s) => (
        <circle key={s.id} cx={s.x!} cy={s.y!} r={4} fill={color} opacity={0.85} />
      ))}
    </g>
  );
}

function ArcPath({
  centerX,
  centerY,
  radius,
  startAngleDeg,
  sweepAngleDeg,
  ...rest
}: {
  centerX: number;
  centerY: number;
  radius: number;
  startAngleDeg: number;
  sweepAngleDeg: number;
  fill?: string;
  opacity?: number;
  stroke?: string;
  strokeWidth?: number;
}) {
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
  return <path d={d} {...rest} />;
}
