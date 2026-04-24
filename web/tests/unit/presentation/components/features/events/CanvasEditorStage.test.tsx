/**
 * Slice 8 S8.2 — CanvasEditorStage tests.
 *
 * Konva can't run in jsdom (HTMLCanvasElement is unsupported), so we mock
 * react-konva to render plain divs with the props we want to assert. The
 * mocks here intentionally match the SeatPicker test file so the editor
 * canvas reuses the same testing story as the reader canvas.
 */

import React from 'react';
import { describe, it, expect, vi, beforeAll, afterAll } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';

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
  Group: ({ children, rotation }: { children?: React.ReactNode; rotation?: number }) =>
    React.createElement(
      'div',
      { 'data-testid': 'mock-group', 'data-rotation': String(rotation ?? 0) },
      children,
    ),
  Rect: (rest: Record<string, unknown>) =>
    React.createElement('div', {
      'data-testid': 'mock-rect',
      'data-x': String(rest.x ?? ''),
      'data-y': String(rest.y ?? ''),
      'data-width': String(rest.width ?? ''),
      'data-height': String(rest.height ?? ''),
      'data-fill': String(rest.fill ?? ''),
      'data-stroke': String(rest.stroke ?? ''),
      'data-dash': String((rest.dash as unknown) ?? ''),
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
    // Background rect has the layout's canvas size and color.
    const rects = screen.getAllByTestId('mock-rect');
    const bg = rects.find(
      (r) =>
        r.getAttribute('data-width') === '1000' &&
        r.getAttribute('data-height') === '800' &&
        r.getAttribute('data-fill') === '#FFEBEB',
    );
    expect(bg).toBeDefined();
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
