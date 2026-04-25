/**
 * Slice 8 S8.2+S8.3: CanvasEditorStage — Konva implementation for the editor.
 *
 * Wrapped by {@link CanvasEditor} via Next.js `dynamic()` with `ssr: false`.
 * Never import this file directly from a page — Konva touches `window` at
 * import time.
 *
 * S8.2 — read-only rendering: zones (rect + curve), tables (round + rect),
 * decorations (stage / aisle / door / wall / text / image / dance-floor),
 * light dotted grid, canvas background.
 *
 * S8.3 — interaction: click to select, drag to move, snap-to-50px grid,
 * empty-canvas click deselects. Dragging an item emits `onGeometryChange`
 * with a new geometry JSON so the parent (CanvasEditor) can maintain a
 * draft state that diverges from the persisted layout until Save.
 *
 * S8.3+ — alignment guides: during drag, dashed blue lines appear when the
 * dragged item's center aligns (within `ALIGN_TOLERANCE`) with another
 * item's center on either axis. Guides disappear on drag end.
 *
 * Seats are NOT rendered in the editor — they are auto-generated per
 * zone/table by the domain and organizers don't position them individually.
 */

'use client';

import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type Konva from 'konva';
import {
  Stage,
  Layer,
  Rect,
  Circle,
  Text,
  Path,
  Group,
  Line,
  Transformer,
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
} from '@/presentation/utils/layoutGeometry';
import {
  EDITOR_GRID,
  MIN_SHAPE_DIMENSION,
  snapToGrid,
  snapRotation,
  refKey,
  itemCenter,
  applyDragToGeometry,
  applyResizeToGeometry,
  applyRadiusToGeometry,
  applyRotationToGeometry,
  resolveGeometry,
  type CanvasItemRef,
} from '@/presentation/utils/canvasEditorGeometry';

const DEFAULT_CANVAS_WIDTH = 1000;
const DEFAULT_CANVAS_HEIGHT = 800;
const DEFAULT_BACKGROUND = '#FAFAF9';
const GRID_COLOR = '#E5E7EB';
const SELECTION_COLOR = '#2563EB';
const ALIGN_TOLERANCE = 3; // canvas units — considered "aligned" within this distance
const SCALE_CHANGE_EPSILON = 0.001;
const ROTATION_CHANGE_EPSILON = 0.1;

function findLayoutItem(
  layout: VenueLayoutDto,
  ref: CanvasItemRef,
): VenueZoneDto | VenueTableDto | VenueDecorationDto | null {
  if (ref.kind === 'zone') {
    return layout.zones?.find((z) => z.id === ref.id) ?? null;
  }
  if (ref.kind === 'table') {
    return layout.tables?.find((t) => t.id === ref.id) ?? null;
  }
  return layout.decorations?.find((d) => d.id === ref.id) ?? null;
}

export interface CanvasEditorStageProps {
  layout: VenueLayoutDto;
  className?: string;
  /** Currently selected item, or null. When undefined the stage renders read-only. */
  selected?: CanvasItemRef | null;
  /** Called when the user clicks a shape (ref) or the empty canvas (null). */
  onSelect?: (ref: CanvasItemRef | null) => void;
  /**
   * Per-item geometry overrides — keyed by refKey(ref). The stage renders
   * each item's geometry from this map first, then falls back to the
   * persisted layout. Matches the drag-while-unsaved flow.
   */
  draftGeometryByKey?: Record<string, string>;
  /** Called on drag end with the snapped geometry JSON for the moved item. */
  onGeometryChange?: (ref: CanvasItemRef, geometryJson: string) => void;
}

export function CanvasEditorStage({
  layout,
  className,
  selected = null,
  onSelect,
  draftGeometryByKey = {},
  onGeometryChange,
}: CanvasEditorStageProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const [containerSize, setContainerSize] = useState({ width: 0, height: 0 });
  const [activeGuides, setActiveGuides] = useState<{ xLines: number[]; yLines: number[] }>({
    xLines: [],
    yLines: [],
  });

  // Slice 8 S8.4: ref map + Transformer attached to the selected shape's
  // Group. Handled imperatively because Konva's Transformer is imperative.
  const groupRefs = useRef(new Map<string, Konva.Group>());
  const transformerRef = useRef<Konva.Transformer | null>(null);

  const registerGroupRef = useCallback(
    (key: string, node: Konva.Group | null) => {
      if (node) groupRefs.current.set(key, node);
      else groupRefs.current.delete(key);
    },
    [],
  );

  // Compute whether the currently selected item can be transformed, and
  // what anchor set / keep-ratio behavior the Transformer should use.
  const transformerConfig = useMemo(() => {
    if (!selected) return null;
    const layoutItem = findLayoutItem(layout, selected);
    if (!layoutItem) return null;
    if (selected.kind === 'zone') {
      const zone = layoutItem as VenueZoneDto;
      if (zone.shape === 'Curve') return null; // curve geometry has no width/height
      return {
        enabledAnchors: [
          'top-left',
          'top-center',
          'top-right',
          'middle-right',
          'bottom-right',
          'bottom-center',
          'bottom-left',
          'middle-left',
        ],
        rotateEnabled: true,
        keepRatio: false,
      };
    }
    if (selected.kind === 'table') {
      const table = layoutItem as VenueTableDto;
      if (table.shape === 'Round') {
        return {
          enabledAnchors: ['top-left', 'top-right', 'bottom-left', 'bottom-right'],
          rotateEnabled: false, // circular shape — rotation is meaningless
          keepRatio: true,
        };
      }
      return {
        enabledAnchors: [
          'top-left',
          'top-center',
          'top-right',
          'middle-right',
          'bottom-right',
          'bottom-center',
          'bottom-left',
          'middle-left',
        ],
        rotateEnabled: true,
        keepRatio: false,
      };
    }
    // decoration
    return {
      enabledAnchors: [
        'top-left',
        'top-center',
        'top-right',
        'middle-right',
        'bottom-right',
        'bottom-center',
        'bottom-left',
        'middle-left',
      ],
      rotateEnabled: true,
      keepRatio: false,
    };
  }, [selected, layout]);

  // Attach Transformer to the selected node whenever selection changes.
  useEffect(() => {
    const tr = transformerRef.current;
    if (!tr) return;
    if (selected && transformerConfig) {
      const node = groupRefs.current.get(refKey(selected));
      if (node) {
        tr.nodes([node]);
        tr.getLayer()?.batchDraw();
        return;
      }
    }
    tr.nodes([]);
    tr.getLayer()?.batchDraw();
  }, [selected, transformerConfig, layout, draftGeometryByKey]);

  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;
    const update = () => {
      setContainerSize({ width: el.clientWidth, height: el.clientHeight });
    };
    update();
    const ro = new ResizeObserver(update);
    ro.observe(el);
    return () => ro.disconnect();
  }, []);

  const canvasWidth = layout.canvas?.width ?? DEFAULT_CANVAS_WIDTH;
  const canvasHeight = layout.canvas?.height ?? DEFAULT_CANVAS_HEIGHT;
  const background = layout.canvas?.backgroundColor ?? DEFAULT_BACKGROUND;

  const scale =
    containerSize.width === 0 || containerSize.height === 0
      ? 1
      : Math.min(
          containerSize.width / canvasWidth,
          containerSize.height / canvasHeight,
        ) * 0.98;

  const offsetX = (containerSize.width - canvasWidth * scale) / 2;
  const offsetY = (containerSize.height - canvasHeight * scale) / 2;

  const isInteractive = Boolean(onSelect && onGeometryChange);

  // Precompute all item centers so alignment-guide detection during drag is O(n).
  const itemCentersByKey = useMemo(() => {
    const map = new Map<string, { x: number; y: number }>();
    for (const z of layout.zones ?? []) {
      const g = resolveGeometry('zone', z, draftGeometryByKey);
      const c = itemCenter('zone', g, z.shape as string | undefined);
      if (c) map.set(refKey({ kind: 'zone', id: z.id }), c);
    }
    for (const t of layout.tables ?? []) {
      const g = resolveGeometry('table', t, draftGeometryByKey);
      const c = itemCenter('table', g, t.shape as string);
      if (c) map.set(refKey({ kind: 'table', id: t.id }), c);
    }
    for (const d of layout.decorations ?? []) {
      const g = resolveGeometry('decoration', d, draftGeometryByKey);
      const c = itemCenter('decoration', g);
      if (c) map.set(refKey({ kind: 'decoration', id: d.id }), c);
    }
    return map;
  }, [layout.zones, layout.tables, layout.decorations, draftGeometryByKey]);

  // During drag, compute alignment guides against every other item.
  const updateGuidesDuringDrag = (ownKey: string, current: { x: number; y: number }) => {
    if (!isInteractive) return;
    const xLines = new Set<number>();
    const yLines = new Set<number>();
    for (const [key, center] of itemCentersByKey.entries()) {
      if (key === ownKey) continue;
      if (Math.abs(center.x - current.x) <= ALIGN_TOLERANCE) xLines.add(center.x);
      if (Math.abs(center.y - current.y) <= ALIGN_TOLERANCE) yLines.add(center.y);
    }
    setActiveGuides({ xLines: [...xLines], yLines: [...yLines] });
  };

  const clearGuides = () => setActiveGuides({ xLines: [], yLines: [] });

  const handleBackgroundClick = () => {
    if (!isInteractive) return;
    onSelect?.(null);
  };

  const commitDrag = (
    ref: CanvasItemRef,
    shapeHint: string | undefined,
    geometry: string | null | undefined,
    snappedCenter: { x: number; y: number },
  ) => {
    const next = applyDragToGeometry(ref.kind, geometry, snappedCenter, shapeHint);
    if (next) onGeometryChange?.(ref, next);
  };

  const commitTransform = (ref: CanvasItemRef, newGeometry: string) => {
    onGeometryChange?.(ref, newGeometry);
  };

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
            <Rect
              x={0}
              y={0}
              width={canvasWidth}
              height={canvasHeight}
              fill={background}
              stroke="#D1D5DB"
              strokeWidth={1 / scale}
              onClick={handleBackgroundClick}
              onTap={handleBackgroundClick}
              data-testid="canvas-background"
            />
            <Grid width={canvasWidth} height={canvasHeight} scale={scale} />
          </Layer>

          <Layer>
            {(layout.decorations ?? []).map((d) => {
              const ref: CanvasItemRef = { kind: 'decoration', id: d.id };
              const key = refKey(ref);
              return (
                <DecorationShape
                  key={d.id}
                  decoration={d}
                  geometryOverride={draftGeometryByKey[key]}
                  selected={selected?.kind === 'decoration' && selected.id === d.id}
                  onSelect={onSelect ? () => onSelect(ref) : undefined}
                  onDragMove={
                    isInteractive
                      ? (c) => updateGuidesDuringDrag(key, c)
                      : undefined
                  }
                  onDragEnd={
                    onGeometryChange
                      ? (snapped, geometry) => {
                          clearGuides();
                          commitDrag(ref, undefined, geometry, snapped);
                        }
                      : undefined
                  }
                  registerRef={
                    isInteractive ? (node) => registerGroupRef(key, node) : undefined
                  }
                  onTransformEnd={
                    onGeometryChange ? (newGeom) => commitTransform(ref, newGeom) : undefined
                  }
                />
              );
            })}
          </Layer>

          <Layer>
            {(layout.zones ?? []).map((z) => {
              const ref: CanvasItemRef = { kind: 'zone', id: z.id };
              const key = refKey(ref);
              return (
                <ZoneShape
                  key={z.id}
                  zone={z}
                  geometryOverride={draftGeometryByKey[key]}
                  selected={selected?.kind === 'zone' && selected.id === z.id}
                  onSelect={onSelect ? () => onSelect(ref) : undefined}
                  onDragMove={
                    isInteractive
                      ? (c) => updateGuidesDuringDrag(key, c)
                      : undefined
                  }
                  onDragEnd={
                    onGeometryChange
                      ? (snapped, geometry) => {
                          clearGuides();
                          commitDrag(
                            ref,
                            z.shape as string | undefined,
                            geometry,
                            snapped,
                          );
                        }
                      : undefined
                  }
                  registerRef={
                    isInteractive ? (node) => registerGroupRef(key, node) : undefined
                  }
                  onTransformEnd={
                    onGeometryChange ? (newGeom) => commitTransform(ref, newGeom) : undefined
                  }
                />
              );
            })}
          </Layer>

          <Layer>
            {(layout.tables ?? []).map((t) => {
              const ref: CanvasItemRef = { kind: 'table', id: t.id };
              const key = refKey(ref);
              return (
                <TableShape
                  key={t.id}
                  table={t}
                  geometryOverride={draftGeometryByKey[key]}
                  selected={selected?.kind === 'table' && selected.id === t.id}
                  onSelect={onSelect ? () => onSelect(ref) : undefined}
                  onDragMove={
                    isInteractive
                      ? (c) => updateGuidesDuringDrag(key, c)
                      : undefined
                  }
                  onDragEnd={
                    onGeometryChange
                      ? (snapped, geometry) => {
                          clearGuides();
                          commitDrag(ref, t.shape as string, geometry, snapped);
                        }
                      : undefined
                  }
                  registerRef={
                    isInteractive ? (node) => registerGroupRef(key, node) : undefined
                  }
                  onTransformEnd={
                    onGeometryChange ? (newGeom) => commitTransform(ref, newGeom) : undefined
                  }
                />
              );
            })}
            {/* S8.4: single Transformer that imperatively attaches to the
             * selected shape's Group (via groupRefs) in the useEffect above. */}
            {isInteractive && transformerConfig && (
              <Transformer
                ref={transformerRef}
                enabledAnchors={transformerConfig.enabledAnchors}
                rotateEnabled={transformerConfig.rotateEnabled}
                keepRatio={transformerConfig.keepRatio}
                rotationSnaps={[0, 15, 30, 45, 60, 75, 90, 105, 120, 135, 150, 165, 180,
                  195, 210, 225, 240, 255, 270, 285, 300, 315, 330, 345]}
                anchorSize={10}
                anchorStroke={SELECTION_COLOR}
                anchorFill="#FFFFFF"
                borderStroke={SELECTION_COLOR}
                borderDash={[4, 4]}
                boundBoxFunc={(oldBox, newBox) => {
                  if (
                    newBox.width < MIN_SHAPE_DIMENSION ||
                    newBox.height < MIN_SHAPE_DIMENSION
                  ) {
                    return oldBox;
                  }
                  return newBox;
                }}
                data-testid="canvas-editor-transformer"
              />
            )}
          </Layer>

          {/* Alignment guides drawn on top so they're visible over items. */}
          {isInteractive && (
            <Layer listening={false}>
              {activeGuides.xLines.map((x) => (
                <Line
                  key={`xg-${x}`}
                  points={[x, 0, x, canvasHeight]}
                  stroke={SELECTION_COLOR}
                  strokeWidth={1 / scale}
                  dash={[6, 4]}
                />
              ))}
              {activeGuides.yLines.map((y) => (
                <Line
                  key={`yg-${y}`}
                  points={[0, y, canvasWidth, y]}
                  stroke={SELECTION_COLOR}
                  strokeWidth={1 / scale}
                  dash={[6, 4]}
                />
              ))}
            </Layer>
          )}
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
  for (let x = EDITOR_GRID; x < width; x += EDITOR_GRID) cols.push(x);
  const rows: number[] = [];
  for (let y = EDITOR_GRID; y < height; y += EDITOR_GRID) rows.push(y);
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

// ─────────────────────────── Shape common props ───────────────────────────

interface DraggableShapeProps {
  selected?: boolean;
  onSelect?: () => void;
  /** Called during drag with the current center in canvas space. */
  onDragMove?: (center: { x: number; y: number }) => void;
  /** Called at drag end with the snapped center and the effective geometry JSON. */
  onDragEnd?: (snappedCenter: { x: number; y: number }, geometry: string | undefined) => void;
  geometryOverride?: string;
  /** Slice 8 S8.4: register the Konva Group node so the Transformer can
   * imperatively attach to it when this shape is selected. */
  registerRef?: (node: Konva.Group | null) => void;
  /** Slice 8 S8.4: fires when the Transformer finishes a resize or rotate.
   * Emits the already-snapped new geometry JSON (or null to drop). */
  onTransformEnd?: (newGeometry: string) => void;
}

type KonvaDragEvent = Konva.KonvaEventObject<DragEvent>;
type KonvaTransformEvent = Konva.KonvaEventObject<Event>;

// ─────────────────────────── Zones ───────────────────────────

function ZoneShape({
  zone,
  geometryOverride,
  selected,
  onSelect,
  onDragMove,
  onDragEnd,
  registerRef,
  onTransformEnd,
}: DraggableShapeProps & { zone: VenueZoneDto }) {
  const geometry = geometryOverride ?? zone.geometry;

  if (zone.shape === 'Curve') {
    const g = parseCurveGeom(geometry);
    if (!g) return null;
    const { centerX, centerY, radius, startAngleDeg, sweepAngleDeg } = g;
    const toRad = (d: number) => (d * Math.PI) / 180;
    const endAngleDeg = startAngleDeg + sweepAngleDeg;
    const startPt = {
      x: radius * Math.cos(toRad(startAngleDeg)),
      y: radius * Math.sin(toRad(startAngleDeg)),
    };
    const endPt = {
      x: radius * Math.cos(toRad(endAngleDeg)),
      y: radius * Math.sin(toRad(endAngleDeg)),
    };
    const largeArc = Math.abs(sweepAngleDeg) > 180 ? 1 : 0;
    const sweepFlag = sweepAngleDeg >= 0 ? 1 : 0;
    // Path drawn relative to Group (0,0) so the Group can own position.
    const d = `M 0 0 L ${startPt.x} ${startPt.y} A ${radius} ${radius} 0 ${largeArc} ${sweepFlag} ${endPt.x} ${endPt.y} Z`;
    const canDrag = Boolean(onDragEnd);
    return (
      <Group
        ref={registerRef}
        x={centerX}
        y={centerY}
        draggable={canDrag}
        onClick={onSelect}
        onTap={onSelect}
        onDragMove={(e: KonvaDragEvent) => onDragMove?.({ x: e.target.x(), y: e.target.y() })}
        onDragEnd={(e: KonvaDragEvent) => {
          const snapped = { x: snapToGrid(e.target.x()), y: snapToGrid(e.target.y()) };
          e.target.position(snapped);
          onDragEnd?.(snapped, geometry ?? undefined);
        }}
      >
        <Path data={d} fill={zone.color} opacity={0.25} stroke={zone.color} strokeWidth={2} />
        <Text
          x={-radius}
          y={radius / 3}
          width={radius * 2}
          align="center"
          text={zone.name}
          fontSize={16}
          fontStyle="bold"
          fontFamily="Inter, system-ui, sans-serif"
          fill={zone.color}
        />
        {selected && (
          <Circle
            x={0}
            y={0}
            radius={radius + 6}
            stroke={SELECTION_COLOR}
            strokeWidth={2}
            dash={[4, 4]}
          />
        )}
      </Group>
    );
  }

  const g = parseRectGeom(geometry);
  if (!g) {
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
  const centerX = g.x + g.width / 2;
  const centerY = g.y + g.height / 2;
  const canDrag = Boolean(onDragEnd);
  const labelFontSize = Math.max(14, Math.min(26, g.width / 24));
  const handleTransformEnd = (e: KonvaTransformEvent) => {
    const node = e.target;
    const scaleX = node.scaleX();
    const scaleY = node.scaleY();
    const rotation = node.rotation();
    const sizeChanged =
      Math.abs(scaleX - 1) > SCALE_CHANGE_EPSILON ||
      Math.abs(scaleY - 1) > SCALE_CHANGE_EPSILON;
    const rotationChanged =
      Math.abs(rotation - (g.rotation ?? 0)) > ROTATION_CHANGE_EPSILON;
    let updated: string | null = null;
    if (sizeChanged) {
      const newWidth = Math.max(MIN_SHAPE_DIMENSION, snapToGrid(g.width * scaleX));
      const newHeight = Math.max(MIN_SHAPE_DIMENSION, snapToGrid(g.height * scaleY));
      updated = applyResizeToGeometry(
        'zone',
        geometry,
        { width: newWidth, height: newHeight },
        'Rect',
      );
      node.scaleX(1);
      node.scaleY(1);
    }
    if (rotationChanged) {
      const snapped = snapRotation(rotation);
      node.rotation(snapped);
      updated = applyRotationToGeometry('zone', updated ?? geometry, snapped, 'Rect');
    }
    if (updated) onTransformEnd?.(updated);
  };
  return (
    <Group
      ref={registerRef}
      x={centerX}
      y={centerY}
      rotation={g.rotation ?? 0}
      draggable={canDrag}
      onClick={onSelect}
      onTap={onSelect}
      onDragMove={(e: KonvaDragEvent) => onDragMove?.({ x: e.target.x(), y: e.target.y() })}
      onDragEnd={(e: KonvaDragEvent) => {
        const snapped = { x: snapToGrid(e.target.x()), y: snapToGrid(e.target.y()) };
        e.target.position(snapped);
        onDragEnd?.(snapped, geometry ?? undefined);
      }}
      onTransformEnd={handleTransformEnd}
    >
      <Rect
        x={-g.width / 2}
        y={-g.height / 2}
        width={g.width}
        height={g.height}
        cornerRadius={6}
        fill={zone.color}
        opacity={0.18}
        stroke={zone.color}
        strokeWidth={2}
      />
      <Text
        x={-g.width / 2}
        y={-g.height / 2 + Math.min(12, g.height / 2)}
        width={g.width}
        align="center"
        text={zone.name}
        fontSize={labelFontSize}
        fontStyle="bold"
        fontFamily="Inter, system-ui, sans-serif"
        fill={zone.color}
      />
      {selected && (
        <Rect
          x={-g.width / 2 - 4}
          y={-g.height / 2 - 4}
          width={g.width + 8}
          height={g.height + 8}
          stroke={SELECTION_COLOR}
          strokeWidth={2}
          dash={[4, 4]}
          cornerRadius={8}
        />
      )}
    </Group>
  );
}

// ─────────────────────────── Tables ───────────────────────────

function TableShape({
  table,
  geometryOverride,
  selected,
  onSelect,
  onDragMove,
  onDragEnd,
  registerRef,
  onTransformEnd,
}: DraggableShapeProps & { table: VenueTableDto }) {
  const geometry = geometryOverride ?? table.geometry;
  const canDrag = Boolean(onDragEnd);

  if (table.shape === 'Round') {
    const g = parseRoundTableGeom(geometry);
    if (!g) return null;
    const handleRoundTransformEnd = (e: KonvaTransformEvent) => {
      const node = e.target;
      const scaleX = node.scaleX();
      // keepRatio is true → scaleX === scaleY.
      if (Math.abs(scaleX - 1) < SCALE_CHANGE_EPSILON) return;
      const minRadius = MIN_SHAPE_DIMENSION / 2;
      const newRadius = Math.max(minRadius, snapToGrid(g.radius * scaleX));
      const updated = applyRadiusToGeometry(geometry, newRadius);
      node.scaleX(1);
      node.scaleY(1);
      if (updated) onTransformEnd?.(updated);
    };
    return (
      <Group
        ref={registerRef}
        x={g.centerX}
        y={g.centerY}
        draggable={canDrag}
        onClick={onSelect}
        onTap={onSelect}
        onDragMove={(e: KonvaDragEvent) => onDragMove?.({ x: e.target.x(), y: e.target.y() })}
        onDragEnd={(e: KonvaDragEvent) => {
          const snapped = { x: snapToGrid(e.target.x()), y: snapToGrid(e.target.y()) };
          e.target.position(snapped);
          onDragEnd?.(snapped, geometry ?? undefined);
        }}
        onTransformEnd={handleRoundTransformEnd}
      >
        <Circle
          x={0}
          y={0}
          radius={g.radius}
          fill="#FDE68A"
          stroke="#B45309"
          strokeWidth={2}
        />
        <Text
          x={-g.radius}
          y={-7}
          width={g.radius * 2}
          align="center"
          text={table.label}
          fontSize={14}
          fontStyle="bold"
          fontFamily="Inter, system-ui, sans-serif"
          fill="#92400E"
        />
        {selected && (
          <Circle
            x={0}
            y={0}
            radius={g.radius + 6}
            stroke={SELECTION_COLOR}
            strokeWidth={2}
            dash={[4, 4]}
          />
        )}
      </Group>
    );
  }

  const g = parseRectTableGeom(geometry);
  if (!g) return null;
  const handleRectTableTransformEnd = (e: KonvaTransformEvent) => {
    const node = e.target;
    const scaleX = node.scaleX();
    const scaleY = node.scaleY();
    const rotation = node.rotation();
    const sizeChanged =
      Math.abs(scaleX - 1) > SCALE_CHANGE_EPSILON ||
      Math.abs(scaleY - 1) > SCALE_CHANGE_EPSILON;
    const rotationChanged =
      Math.abs(rotation - (g.rotation ?? 0)) > ROTATION_CHANGE_EPSILON;
    let updated: string | null = null;
    if (sizeChanged) {
      const newWidth = Math.max(MIN_SHAPE_DIMENSION, snapToGrid(g.width * scaleX));
      const newHeight = Math.max(MIN_SHAPE_DIMENSION, snapToGrid(g.height * scaleY));
      updated = applyResizeToGeometry(
        'table',
        geometry,
        { width: newWidth, height: newHeight },
        'Rect',
      );
      node.scaleX(1);
      node.scaleY(1);
    }
    if (rotationChanged) {
      const snapped = snapRotation(rotation);
      node.rotation(snapped);
      updated = applyRotationToGeometry('table', updated ?? geometry, snapped, 'Rect');
    }
    if (updated) onTransformEnd?.(updated);
  };
  return (
    <Group
      ref={registerRef}
      x={g.centerX}
      y={g.centerY}
      rotation={g.rotation ?? 0}
      draggable={canDrag}
      onClick={onSelect}
      onTap={onSelect}
      onDragMove={(e: KonvaDragEvent) => onDragMove?.({ x: e.target.x(), y: e.target.y() })}
      onDragEnd={(e: KonvaDragEvent) => {
        const snapped = { x: snapToGrid(e.target.x()), y: snapToGrid(e.target.y()) };
        e.target.position(snapped);
        onDragEnd?.(snapped, geometry ?? undefined);
      }}
      onTransformEnd={handleRectTableTransformEnd}
    >
      <Rect
        x={-g.width / 2}
        y={-g.height / 2}
        width={g.width}
        height={g.height}
        cornerRadius={4}
        fill="#FEE2E2"
        stroke="#B91C1C"
        strokeWidth={2}
      />
      <Text
        x={-g.width / 2}
        y={-7}
        width={g.width}
        align="center"
        text={table.label}
        fontSize={14}
        fontStyle="bold"
        fontFamily="Inter, system-ui, sans-serif"
        fill="#7F1D1D"
      />
      {selected && (
        <Rect
          x={-g.width / 2 - 4}
          y={-g.height / 2 - 4}
          width={g.width + 8}
          height={g.height + 8}
          stroke={SELECTION_COLOR}
          strokeWidth={2}
          dash={[4, 4]}
          cornerRadius={6}
        />
      )}
    </Group>
  );
}

// ─────────────────────────── Decorations ───────────────────────────

function DecorationShape({
  decoration,
  geometryOverride,
  selected,
  onSelect,
  onDragMove,
  onDragEnd,
  registerRef,
  onTransformEnd,
}: DraggableShapeProps & { decoration: VenueDecorationDto }) {
  const geometry = geometryOverride ?? decoration.geometry;
  const g = parseRectGeom(geometry);
  if (!g) return null;
  const style = decorationStyle(decoration.kind);
  const labelText = decoration.label ?? style.label;
  const centerX = g.x + g.width / 2;
  const centerY = g.y + g.height / 2;
  const canDrag = Boolean(onDragEnd);
  const handleDecorationTransformEnd = (e: KonvaTransformEvent) => {
    const node = e.target;
    const scaleX = node.scaleX();
    const scaleY = node.scaleY();
    const rotation = node.rotation();
    const sizeChanged =
      Math.abs(scaleX - 1) > SCALE_CHANGE_EPSILON ||
      Math.abs(scaleY - 1) > SCALE_CHANGE_EPSILON;
    const rotationChanged =
      Math.abs(rotation - (g.rotation ?? 0)) > ROTATION_CHANGE_EPSILON;
    let updated: string | null = null;
    if (sizeChanged) {
      const newWidth = Math.max(MIN_SHAPE_DIMENSION, snapToGrid(g.width * scaleX));
      const newHeight = Math.max(MIN_SHAPE_DIMENSION, snapToGrid(g.height * scaleY));
      updated = applyResizeToGeometry('decoration', geometry, {
        width: newWidth,
        height: newHeight,
      });
      node.scaleX(1);
      node.scaleY(1);
    }
    if (rotationChanged) {
      const snapped = snapRotation(rotation);
      node.rotation(snapped);
      updated = applyRotationToGeometry('decoration', updated ?? geometry, snapped);
    }
    if (updated) onTransformEnd?.(updated);
  };
  return (
    <Group
      ref={registerRef}
      x={centerX}
      y={centerY}
      rotation={g.rotation ?? 0}
      draggable={canDrag}
      onClick={onSelect}
      onTap={onSelect}
      onDragMove={(e: KonvaDragEvent) => onDragMove?.({ x: e.target.x(), y: e.target.y() })}
      onDragEnd={(e: KonvaDragEvent) => {
        const snapped = { x: snapToGrid(e.target.x()), y: snapToGrid(e.target.y()) };
        e.target.position(snapped);
        onDragEnd?.(snapped, geometry ?? undefined);
      }}
      onTransformEnd={handleDecorationTransformEnd}
    >
      <Rect
        x={-g.width / 2}
        y={-g.height / 2}
        width={g.width}
        height={g.height}
        cornerRadius={decoration.kind === 'Stage' ? 10 : 4}
        fill={style.fill}
        stroke={style.stroke}
        strokeWidth={2}
        dash={decoration.kind === 'DanceFloor' ? [10, 6] : undefined}
      />
      {labelText && (
        <Text
          x={-g.width / 2}
          y={-10}
          width={g.width}
          align="center"
          text={labelText}
          fontSize={Math.max(14, Math.min(28, g.width / 22))}
          fontStyle="bold"
          fontFamily="Inter, system-ui, sans-serif"
          fill={style.labelColor}
        />
      )}
      {selected && (
        <Rect
          x={-g.width / 2 - 4}
          y={-g.height / 2 - 4}
          width={g.width + 8}
          height={g.height + 8}
          stroke={SELECTION_COLOR}
          strokeWidth={2}
          dash={[4, 4]}
          cornerRadius={decoration.kind === 'Stage' ? 12 : 6}
        />
      )}
    </Group>
  );
}

export default CanvasEditorStage;
