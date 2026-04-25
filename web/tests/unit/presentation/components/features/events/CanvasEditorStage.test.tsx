/**
 * Slice 8 S8.2 — CanvasEditorStage tests.
 *
 * Konva can't run in jsdom (HTMLCanvasElement is unsupported), so we mock
 * react-konva to render plain divs with the props we want to assert. The
 * mocks here intentionally match the SeatPicker test file so the editor
 * canvas reuses the same testing story as the reader canvas.
 */

import React from 'react';
import { describe, it, expect, vi, beforeAll, afterAll, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';

// Map from rendered Group DOM node → its Konva drag handlers, so drag tests
// can invoke onDragEnd/onDragMove directly with a fake konva event.
interface FakeKonvaNode {
  x: () => number;
  y: () => number;
  scaleX: () => number;
  scaleY: () => number;
  rotation: () => number;
  position: (p: { x: number; y: number }) => void;
}

const elementHandlers = new WeakMap<
  HTMLDivElement,
  {
    onDragEnd?: (e: { target: FakeKonvaNode }) => void;
    onDragMove?: (e: { target: FakeKonvaNode }) => void;
    onTransformEnd?: (e: { target: FakeKonvaNode }) => void;
  }
>();

// jsdom defaults clientWidth/clientHeight to 0. The stage's responsive useEffect
// reads those and refuses to mount the Konva <Stage/> if either is 0, so we
// stub getters on the element prototype for the duration of these tests.
let originalClientWidth: PropertyDescriptor | undefined;
let originalClientHeight: PropertyDescriptor | undefined;
beforeAll(() => {
  originalClientWidth = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'clientWidth');
  originalClientHeight = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'clientHeight');
  Object.defineProperty(HTMLElement.prototype, 'clientWidth', {
    configurable: true,
    get() {
      return 800;
    },
  });
  Object.defineProperty(HTMLElement.prototype, 'clientHeight', {
    configurable: true,
    get() {
      return 500;
    },
  });
});
afterAll(() => {
  if (originalClientWidth) {
    Object.defineProperty(HTMLElement.prototype, 'clientWidth', originalClientWidth);
  }
  if (originalClientHeight) {
    Object.defineProperty(HTMLElement.prototype, 'clientHeight', originalClientHeight);
  }
});

// ResizeObserver is not implemented in jsdom — stub it so the component's
// size-observing useEffect doesn't throw. We manually fire container sizing
// via `Object.defineProperty` on the wrapping div in tests.
class MockResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
}
(globalThis as unknown as { ResizeObserver: typeof MockResizeObserver }).ResizeObserver =
  MockResizeObserver;

vi.mock('react-konva', () => ({
  __esModule: true,
  Stage: ({
    children,
    width,
    height,
    scaleX,
  }: {
    children?: React.ReactNode;
    width?: number;
    height?: number;
    scaleX?: number;
  }) =>
    React.createElement(
      'div',
      {
        'data-testid': 'mock-stage',
        'data-width': String(width ?? ''),
        'data-height': String(height ?? ''),
        'data-scale': String(scaleX ?? ''),
      },
      children,
    ),
  Layer: ({ children }: { children?: React.ReactNode }) =>
    React.createElement('div', { 'data-testid': 'mock-layer' }, children),
  Group: React.forwardRef<HTMLDivElement, { children?: React.ReactNode; [key: string]: unknown }>(
    function MockGroup(props, outerRef) {
      const {
        children,
        rotation,
        x,
        y,
        draggable,
        onClick,
        onDragEnd,
        onDragMove,
        onTransformEnd,
      } = props;
      return React.createElement(
        'div',
        {
          'data-testid': 'mock-group',
          'data-rotation': String(rotation ?? 0),
          'data-x': String(x ?? ''),
          'data-y': String(y ?? ''),
          'data-draggable': String(draggable ?? false),
          'data-drag-end': onDragEnd ? 'yes' : 'no',
          'data-transform-end': onTransformEnd ? 'yes' : 'no',
          onClick: onClick as (() => void) | undefined,
          ref: (node: HTMLDivElement | null) => {
            if (!node) return;
            elementHandlers.set(node, {
              onDragEnd: onDragEnd as
                | ((e: { target: FakeKonvaNode }) => void)
                | undefined,
              onDragMove: onDragMove as
                | ((e: { target: FakeKonvaNode }) => void)
                | undefined,
              onTransformEnd: onTransformEnd as
                | ((e: { target: FakeKonvaNode }) => void)
                | undefined,
            });
            if (typeof outerRef === 'function') outerRef(node);
            else if (outerRef && 'current' in outerRef)
              (outerRef as React.MutableRefObject<HTMLDivElement | null>).current = node;
          },
        },
        children as React.ReactNode,
      );
    },
  ),
  Rect: (rest: Record<string, unknown>) =>
    React.createElement('div', {
      'data-testid': (rest['data-testid'] as string | undefined) ?? 'mock-rect',
      'data-x': String(rest.x ?? ''),
      'data-y': String(rest.y ?? ''),
      'data-width': String(rest.width ?? ''),
      'data-height': String(rest.height ?? ''),
      'data-fill': String(rest.fill ?? ''),
      'data-stroke': String(rest.stroke ?? ''),
      'data-dash': String((rest.dash as unknown) ?? ''),
      onClick: rest.onClick as (() => void) | undefined,
    }),
  Circle: (rest: Record<string, unknown>) =>
    React.createElement('div', {
      'data-testid': 'mock-circle',
      'data-cx': String(rest.x ?? ''),
      'data-cy': String(rest.y ?? ''),
      'data-radius': String(rest.radius ?? ''),
      'data-fill': String(rest.fill ?? ''),
      'data-stroke': String(rest.stroke ?? ''),
    }),
  Text: (rest: Record<string, unknown>) =>
    React.createElement(
      'div',
      {
        'data-testid': 'mock-text',
        'data-fill': String(rest.fill ?? ''),
      },
      String(rest.text ?? ''),
    ),
  Path: (rest: Record<string, unknown>) =>
    React.createElement('div', {
      'data-testid': 'mock-path',
      'data-d': String(rest.data ?? ''),
      'data-fill': String(rest.fill ?? ''),
    }),
  Line: (rest: Record<string, unknown>) =>
    React.createElement('div', {
      'data-testid': 'mock-line',
      'data-dash': String((rest.dash as unknown) ?? ''),
    }),
  Transformer: React.forwardRef<
    { nodes: (n?: unknown[]) => unknown[]; getLayer: () => { batchDraw: () => void } },
    Record<string, unknown>
  >(function MockTransformer(rest, ref) {
    // Expose a Konva-shaped imperative handle so the stage's useEffect can
    // call `tr.nodes([...])` + `tr.getLayer().batchDraw()` without booting
    // the real Konva runtime.
    React.useImperativeHandle(ref, () => ({
      nodes: () => [],
      getLayer: () => ({ batchDraw: () => {} }),
    }));
    return React.createElement('div', {
      'data-testid': (rest['data-testid'] as string | undefined) ?? 'mock-transformer',
      'data-rotate-enabled': String(rest.rotateEnabled ?? ''),
      'data-keep-ratio': String(rest.keepRatio ?? ''),
      'data-enabled-anchors': Array.isArray(rest.enabledAnchors)
        ? (rest.enabledAnchors as string[]).join(',')
        : '',
    });
  }),
}));

import { CanvasEditorStage } from '@/presentation/components/features/events/CanvasEditorStage';
import {
  DecorationKind,
  TableShape,
  ZoneShape,
  type VenueLayoutDto,
} from '@/infrastructure/api/types/events.types';

function baseLayout(overrides: Partial<VenueLayoutDto> = {}): VenueLayoutDto {
  return {
    id: 'layout-1',
    name: 'Test Layout',
    eventId: null,
    layoutType: 'Theater',
    isTemplate: true,
    createdByUserId: 'user-1',
    totalCapacity: 0,
    zones: [],
    tables: [],
    decorations: [],
    canvas: { width: 1200, height: 800, scale: 1, backgroundColor: '#FFFFFF' },
    createdAt: '2026-04-22T00:00:00Z',
    updatedAt: null,
    rowVersion: 1,
    ...overrides,
  };
}

// All tests mount via plain render() — clientWidth/Height are stubbed on the
// HTMLElement prototype above so the stage's responsive useEffect reads
// non-zero sizes immediately on mount.
function mountWithSize(element: React.ReactElement) {
  return render(element);
}

describe('CanvasEditorStage (Slice 8 S8.2)', () => {
  it('mounts the Konva Stage once the container has a size', async () => {
    render(<CanvasEditorStage layout={baseLayout()} />);
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
  });

  it('sets Stage width/height from the observed container size', async () => {
    mountWithSize(<CanvasEditorStage layout={baseLayout()} />);
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    const stage = screen.getByTestId('mock-stage');
    // Stage width matches container (component scales the canvas model).
    expect(Number(stage.getAttribute('data-width'))).toBeGreaterThan(0);
    expect(Number(stage.getAttribute('data-height'))).toBeGreaterThan(0);
  });

  it('renders a canvas background rect', async () => {
    mountWithSize(
      <CanvasEditorStage layout={baseLayout({ canvas: { width: 1000, height: 800, scale: 1, backgroundColor: '#FFEBEB' } })} />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    // Background rect carries its own testid (set by the stage so the click
    // handler for deselect can find it) and has the layout's canvas size + color.
    const bg = screen.getByTestId('canvas-background');
    expect(bg.getAttribute('data-width')).toBe('1000');
    expect(bg.getAttribute('data-height')).toBe('800');
    expect(bg.getAttribute('data-fill')).toBe('#FFEBEB');
  });

  it('renders a grid overlay with dashed lines', async () => {
    mountWithSize(<CanvasEditorStage layout={baseLayout()} />);
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    // Grid spacing = 50px, canvas 1200×800 → 23 verticals + 15 horizontals = 38.
    const lines = screen.getAllByTestId('mock-line');
    expect(lines.length).toBeGreaterThan(10);
    expect(lines.every((l) => l.getAttribute('data-dash') !== '')).toBe(true);
  });

  it('renders a rect zone with its name label', async () => {
    mountWithSize(
      <CanvasEditorStage
        layout={baseLayout({
          zones: [
            {
              id: 'zone-1',
              name: 'Orchestra',
              color: '#3B82F6',
              sortOrder: 0,
              enabledSeatCount: 0,
              totalSeatCount: 0,
              seats: [],
              shape: ZoneShape.Rect,
              geometry: JSON.stringify({ x: 100, y: 100, width: 400, height: 300 }),
            },
          ],
        })}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    expect(screen.getByText('Orchestra')).toBeInTheDocument();
  });

  it('renders a curve zone as a Path', async () => {
    mountWithSize(
      <CanvasEditorStage
        layout={baseLayout({
          zones: [
            {
              id: 'zone-curved',
              name: 'Mezzanine',
              color: '#A855F7',
              sortOrder: 0,
              enabledSeatCount: 0,
              totalSeatCount: 0,
              seats: [],
              shape: ZoneShape.Curve,
              geometry: JSON.stringify({
                centerX: 500,
                centerY: 500,
                radius: 200,
                startAngleDeg: 180,
                sweepAngleDeg: 180,
              }),
            },
          ],
        })}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    const paths = screen.getAllByTestId('mock-path');
    expect(paths.length).toBe(1);
    expect(paths[0].getAttribute('data-fill')).toBe('#A855F7');
  });

  it('renders a round table as a Circle with its label', async () => {
    mountWithSize(
      <CanvasEditorStage
        layout={baseLayout({
          tables: [
            {
              id: 'table-1',
              venueLayoutId: 'layout-1',
              label: 'T1',
              shape: TableShape.Round,
              geometry: JSON.stringify({ centerX: 200, centerY: 200, radius: 40 }),
              capacity: 8,
              sortOrder: 0,
              enabledSeatCount: 8,
              seats: [],
            },
          ],
        })}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    expect(screen.getAllByTestId('mock-circle').length).toBeGreaterThan(0);
    expect(screen.getByText('T1')).toBeInTheDocument();
  });

  it('renders a rect table as a Rect with its label', async () => {
    mountWithSize(
      <CanvasEditorStage
        layout={baseLayout({
          tables: [
            {
              id: 'table-rect',
              venueLayoutId: 'layout-1',
              label: 'Head Table',
              shape: TableShape.Rect,
              geometry: JSON.stringify({ centerX: 400, centerY: 400, width: 200, height: 80 }),
              capacity: 10,
              sortOrder: 0,
              enabledSeatCount: 10,
              seats: [],
            },
          ],
        })}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    expect(screen.getByText('Head Table')).toBeInTheDocument();
  });

  it('renders a stage decoration with its label', async () => {
    mountWithSize(
      <CanvasEditorStage
        layout={baseLayout({
          decorations: [
            {
              id: 'dec-stage',
              venueLayoutId: 'layout-1',
              kind: DecorationKind.Stage,
              label: 'Main Stage',
              geometry: JSON.stringify({ x: 300, y: 50, width: 400, height: 100 }),
              properties: '{}',
              sortOrder: 0,
            },
          ],
        })}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    expect(screen.getByText('Main Stage')).toBeInTheDocument();
  });

  it('skips a zone with malformed geometry but shows a warning placeholder', async () => {
    mountWithSize(
      <CanvasEditorStage
        layout={baseLayout({
          zones: [
            {
              id: 'zone-bad',
              name: 'Broken',
              color: '#ef4444',
              sortOrder: 0,
              enabledSeatCount: 0,
              totalSeatCount: 0,
              seats: [],
              shape: ZoneShape.Rect,
              geometry: 'not-valid-json',
            },
          ],
        })}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    expect(screen.getByText(/invalid geometry/)).toBeInTheDocument();
  });
});

// ─────────────────────────── S8.3 interaction tests ───────────────────────────

function layoutWithOneZoneAndTable(): VenueLayoutDto {
  return baseLayout({
    zones: [
      {
        id: 'z1',
        name: 'Orchestra',
        color: '#3B82F6',
        sortOrder: 0,
        enabledSeatCount: 0,
        totalSeatCount: 0,
        seats: [],
        shape: ZoneShape.Rect,
        geometry: JSON.stringify({ x: 100, y: 100, width: 400, height: 200 }),
      },
    ],
    tables: [
      {
        id: 't1',
        venueLayoutId: 'layout-1',
        label: 'T1',
        shape: TableShape.Round,
        geometry: JSON.stringify({ centerX: 700, centerY: 600, radius: 40 }),
        capacity: 8,
        sortOrder: 0,
        enabledSeatCount: 8,
        seats: [],
      },
    ],
  });
}

function getDraggableGroup(center: { x: number; y: number }): HTMLDivElement {
  const groups = screen.getAllByTestId('mock-group') as HTMLDivElement[];
  // The draggable Group is the one whose data-x/data-y match the item center.
  const match = groups.find(
    (g) =>
      g.getAttribute('data-draggable') === 'true' &&
      Number(g.getAttribute('data-x')) === center.x &&
      Number(g.getAttribute('data-y')) === center.y,
  );
  if (!match) {
    throw new Error(
      `No draggable Group with center (${center.x},${center.y}) — found: ${groups
        .map(
          (g) =>
            `${g.getAttribute('data-x')},${g.getAttribute('data-y')} draggable=${g.getAttribute(
              'data-draggable',
            )}`,
        )
        .join(' | ')}`,
    );
  }
  return match;
}

function fakeKonvaEvent(x: number, y: number) {
  return {
    target: {
      x: () => x,
      y: () => y,
      scaleX: () => 1,
      scaleY: () => 1,
      rotation: () => 0,
      position: vi.fn(),
    },
  };
}

describe('CanvasEditorStage S8.3 interactions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('read-only mode: shapes are not draggable when onSelect/onGeometryChange omitted', async () => {
    render(<CanvasEditorStage layout={layoutWithOneZoneAndTable()} />);
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    const groups = screen.getAllByTestId('mock-group') as HTMLDivElement[];
    // No Group should be draggable in read-only mode.
    expect(groups.every((g) => g.getAttribute('data-draggable') === 'false')).toBe(true);
  });

  it('interactive mode: zone Group becomes draggable and positions at item center', async () => {
    const onSelect = vi.fn();
    const onGeometryChange = vi.fn();
    render(
      <CanvasEditorStage
        layout={layoutWithOneZoneAndTable()}
        selected={null}
        onSelect={onSelect}
        onGeometryChange={onGeometryChange}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    // Rect zone center = (100+200, 100+100) = (300, 200).
    const zoneGroup = getDraggableGroup({ x: 300, y: 200 });
    expect(zoneGroup.getAttribute('data-draggable')).toBe('true');
  });

  it('click on a zone fires onSelect with the zone ref', async () => {
    const onSelect = vi.fn();
    render(
      <CanvasEditorStage
        layout={layoutWithOneZoneAndTable()}
        selected={null}
        onSelect={onSelect}
        onGeometryChange={vi.fn()}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    const zoneGroup = getDraggableGroup({ x: 300, y: 200 });
    fireEvent.click(zoneGroup);
    expect(onSelect).toHaveBeenCalledWith({ kind: 'zone', id: 'z1' });
  });

  it('click on the background Rect deselects (onSelect(null))', async () => {
    const onSelect = vi.fn();
    render(
      <CanvasEditorStage
        layout={layoutWithOneZoneAndTable()}
        selected={{ kind: 'zone', id: 'z1' }}
        onSelect={onSelect}
        onGeometryChange={vi.fn()}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    const bg = screen.getByTestId('canvas-background');
    fireEvent.click(bg);
    expect(onSelect).toHaveBeenCalledWith(null);
  });

  it('drag end on a rect zone snaps center to 50px and emits onGeometryChange', async () => {
    const onGeometryChange = vi.fn();
    render(
      <CanvasEditorStage
        layout={layoutWithOneZoneAndTable()}
        selected={null}
        onSelect={vi.fn()}
        onGeometryChange={onGeometryChange}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    const zoneGroup = getDraggableGroup({ x: 300, y: 200 });
    const handlers = elementHandlers.get(zoneGroup);
    expect(handlers?.onDragEnd).toBeDefined();

    // Simulate a drag end at (237, 154) — should snap to (250, 150).
    handlers?.onDragEnd?.(fakeKonvaEvent(237, 154));

    expect(onGeometryChange).toHaveBeenCalledTimes(1);
    const [ref, geometryJson] = onGeometryChange.mock.calls[0];
    expect(ref).toEqual({ kind: 'zone', id: 'z1' });
    const parsed = JSON.parse(geometryJson);
    // 400×200 rect, new center = (250,150) → top-left (250-200, 150-100) = (50, 50).
    // Rotation was undefined in the input; JSON.stringify omits undefined keys.
    expect(parsed).toEqual({
      x: 50,
      y: 50,
      width: 400,
      height: 200,
    });
  });

  it('drag end on a round table snaps centerX/Y and preserves radius', async () => {
    const onGeometryChange = vi.fn();
    render(
      <CanvasEditorStage
        layout={layoutWithOneZoneAndTable()}
        selected={null}
        onSelect={vi.fn()}
        onGeometryChange={onGeometryChange}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    const tableGroup = getDraggableGroup({ x: 700, y: 600 });
    const handlers = elementHandlers.get(tableGroup);

    // Drag to (319, 481) → snap to (300, 500).
    handlers?.onDragEnd?.(fakeKonvaEvent(319, 481));

    expect(onGeometryChange).toHaveBeenCalledTimes(1);
    const [ref, geometryJson] = onGeometryChange.mock.calls[0];
    expect(ref).toEqual({ kind: 'table', id: 't1' });
    expect(JSON.parse(geometryJson)).toEqual({ centerX: 300, centerY: 500, radius: 40 });
  });

  it('renders draft override instead of persisted geometry when draftGeometryByKey has an entry', async () => {
    const onGeometryChange = vi.fn();
    const drafts = {
      'zone:z1': JSON.stringify({ x: 500, y: 500, width: 100, height: 100 }),
    };
    render(
      <CanvasEditorStage
        layout={layoutWithOneZoneAndTable()}
        selected={null}
        onSelect={vi.fn()}
        draftGeometryByKey={drafts}
        onGeometryChange={onGeometryChange}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    // Draft shifts the zone; draggable Group now sits at the new center (550, 550).
    expect(() => getDraggableGroup({ x: 550, y: 550 })).not.toThrow();
  });

  it('selected prop causes the zone Group to contain a dashed selection Rect', async () => {
    const onSelect = vi.fn();
    render(
      <CanvasEditorStage
        layout={layoutWithOneZoneAndTable()}
        selected={{ kind: 'zone', id: 'z1' }}
        onSelect={onSelect}
        onGeometryChange={vi.fn()}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    const zoneGroup = getDraggableGroup({ x: 300, y: 200 });
    // Selection halo is a Rect with dashed stroke inside the Group.
    const rectsInGroup = zoneGroup.querySelectorAll('[data-testid="mock-rect"]');
    const haloRect = Array.from(rectsInGroup).find(
      (r) =>
        r.getAttribute('data-stroke') === '#2563EB' &&
        r.getAttribute('data-dash') !== '',
    );
    expect(haloRect).toBeDefined();
  });
});

// ─────────────────────────── S8.4 Transformer tests ───────────────────────────

function makeTransformEvent(
  values: Partial<{ scaleX: number; scaleY: number; rotation: number }>,
) {
  return {
    target: {
      x: () => 0,
      y: () => 0,
      scaleX: () => values.scaleX ?? 1,
      scaleY: () => values.scaleY ?? 1,
      rotation: () => values.rotation ?? 0,
      position: vi.fn(),
    },
  };
}

describe('CanvasEditorStage S8.4 Transformer wiring', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('Transformer is not rendered when nothing is selected', async () => {
    render(
      <CanvasEditorStage
        layout={layoutWithOneZoneAndTable()}
        selected={null}
        onSelect={vi.fn()}
        onGeometryChange={vi.fn()}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    expect(screen.queryByTestId('canvas-editor-transformer')).toBeNull();
  });

  it('Transformer is not rendered in read-only mode even if selected is set', async () => {
    render(
      <CanvasEditorStage
        layout={layoutWithOneZoneAndTable()}
        selected={{ kind: 'zone', id: 'z1' }}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    expect(screen.queryByTestId('canvas-editor-transformer')).toBeNull();
  });

  it('rect zone selected: Transformer has 8 anchors, rotateEnabled=true, keepRatio=false', async () => {
    render(
      <CanvasEditorStage
        layout={layoutWithOneZoneAndTable()}
        selected={{ kind: 'zone', id: 'z1' }}
        onSelect={vi.fn()}
        onGeometryChange={vi.fn()}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    const tr = screen.getByTestId('canvas-editor-transformer');
    expect(tr.getAttribute('data-rotate-enabled')).toBe('true');
    expect(tr.getAttribute('data-keep-ratio')).toBe('false');
    const anchors = (tr.getAttribute('data-enabled-anchors') ?? '').split(',');
    expect(anchors).toHaveLength(8);
    expect(anchors).toContain('top-left');
    expect(anchors).toContain('middle-right');
  });

  it('round table selected: Transformer has 4 corner anchors, rotateEnabled=false, keepRatio=true', async () => {
    render(
      <CanvasEditorStage
        layout={layoutWithOneZoneAndTable()}
        selected={{ kind: 'table', id: 't1' }}
        onSelect={vi.fn()}
        onGeometryChange={vi.fn()}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    const tr = screen.getByTestId('canvas-editor-transformer');
    expect(tr.getAttribute('data-rotate-enabled')).toBe('false');
    expect(tr.getAttribute('data-keep-ratio')).toBe('true');
    const anchors = (tr.getAttribute('data-enabled-anchors') ?? '').split(',');
    expect(anchors).toHaveLength(4);
    expect(anchors).toEqual(['top-left', 'top-right', 'bottom-left', 'bottom-right']);
  });

  it('curve zone selected: Transformer is not rendered (resize not supported on curves)', async () => {
    const layout = baseLayout({
      zones: [
        {
          id: 'curve-1',
          name: 'Mezzanine',
          color: '#A855F7',
          sortOrder: 0,
          enabledSeatCount: 0,
          totalSeatCount: 0,
          seats: [],
          shape: ZoneShape.Curve,
          geometry: JSON.stringify({
            centerX: 500,
            centerY: 500,
            radius: 200,
            startAngleDeg: 180,
            sweepAngleDeg: 180,
          }),
        },
      ],
    });
    render(
      <CanvasEditorStage
        layout={layout}
        selected={{ kind: 'zone', id: 'curve-1' }}
        onSelect={vi.fn()}
        onGeometryChange={vi.fn()}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    expect(screen.queryByTestId('canvas-editor-transformer')).toBeNull();
  });

  it('rect zone transform end: scaleX=2 emits geometry with width doubled + snapped', async () => {
    const onGeometryChange = vi.fn();
    render(
      <CanvasEditorStage
        layout={layoutWithOneZoneAndTable()}
        selected={{ kind: 'zone', id: 'z1' }}
        onSelect={vi.fn()}
        onGeometryChange={onGeometryChange}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    const zoneGroup = getDraggableGroup({ x: 300, y: 200 });
    const handlers = elementHandlers.get(zoneGroup);
    expect(handlers?.onTransformEnd).toBeDefined();

    // Original zone: 400x200, center (300, 200). Drag right handle: scaleX=2.
    handlers?.onTransformEnd?.(makeTransformEvent({ scaleX: 2, scaleY: 1 }));

    expect(onGeometryChange).toHaveBeenCalledTimes(1);
    const [ref, geometryJson] = onGeometryChange.mock.calls[0];
    expect(ref).toEqual({ kind: 'zone', id: 'z1' });
    // 400 * 2 = 800 (snap still 800). Center unchanged → top-left (300-400, 200-100) = (-100, 100).
    // Height unchanged = 200.
    expect(JSON.parse(geometryJson)).toEqual({
      x: -100,
      y: 100,
      width: 800,
      height: 200,
    });
  });

  it('rect zone transform end: rotation=47 snaps to 45 and emits updated geometry', async () => {
    const onGeometryChange = vi.fn();
    render(
      <CanvasEditorStage
        layout={layoutWithOneZoneAndTable()}
        selected={{ kind: 'zone', id: 'z1' }}
        onSelect={vi.fn()}
        onGeometryChange={onGeometryChange}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    const zoneGroup = getDraggableGroup({ x: 300, y: 200 });
    const handlers = elementHandlers.get(zoneGroup);

    handlers?.onTransformEnd?.(makeTransformEvent({ rotation: 47 }));

    expect(onGeometryChange).toHaveBeenCalledTimes(1);
    const [, geometryJson] = onGeometryChange.mock.calls[0];
    expect(JSON.parse(geometryJson)).toEqual({
      x: 100,
      y: 100,
      width: 400,
      height: 200,
      rotation: 45,
    });
  });

  it('round table transform end: scaleX=1.5 emits new radius, snapped to grid', async () => {
    const onGeometryChange = vi.fn();
    render(
      <CanvasEditorStage
        layout={layoutWithOneZoneAndTable()}
        selected={{ kind: 'table', id: 't1' }}
        onSelect={vi.fn()}
        onGeometryChange={onGeometryChange}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    const tableGroup = getDraggableGroup({ x: 700, y: 600 });
    const handlers = elementHandlers.get(tableGroup);

    // Original radius 40; scaleX=1.5 → 60, snap-to-50 → 50. Min floor is 25.
    handlers?.onTransformEnd?.(makeTransformEvent({ scaleX: 1.5, scaleY: 1.5 }));

    expect(onGeometryChange).toHaveBeenCalledTimes(1);
    const [ref, geometryJson] = onGeometryChange.mock.calls[0];
    expect(ref).toEqual({ kind: 'table', id: 't1' });
    expect(JSON.parse(geometryJson)).toEqual({
      centerX: 700,
      centerY: 600,
      radius: 50,
    });
  });

  it('rect zone transform end: no-op when scale=1 and rotation=0 (no spurious commit)', async () => {
    const onGeometryChange = vi.fn();
    render(
      <CanvasEditorStage
        layout={layoutWithOneZoneAndTable()}
        selected={{ kind: 'zone', id: 'z1' }}
        onSelect={vi.fn()}
        onGeometryChange={onGeometryChange}
      />,
    );
    await waitFor(() => expect(screen.getByTestId('mock-stage')).toBeInTheDocument());
    const zoneGroup = getDraggableGroup({ x: 300, y: 200 });
    const handlers = elementHandlers.get(zoneGroup);

    handlers?.onTransformEnd?.(makeTransformEvent({}));
    expect(onGeometryChange).not.toHaveBeenCalled();
  });
});
