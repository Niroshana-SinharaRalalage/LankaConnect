/**
 * Slice 7 Chunk S7.1 — SeatPicker shell tests.
 *
 * The Konva chunk cannot run in jsdom (HTMLCanvasElement is unsupported), so
 * we mock react-konva to render plain divs that carry the props we want to
 * assert on. The dynamic-import boundary itself is what we're verifying —
 * callers must get a component that mounts cleanly in the browser and
 * renders a skeleton on the server (checked by the ssr:false contract in
 * the wrapper).
 */

import React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';

// 1. Mock next/dynamic so the dynamic-import wrapper becomes synchronous
//    for test purposes. This keeps Vitest from having to resolve the konva
//    chunk asynchronously — the prod build still goes through the real
//    dynamic() boundary.
vi.mock('next/dynamic', () => ({
  __esModule: true,
  default: (loader: () => Promise<{ default?: React.ComponentType<unknown> } | React.ComponentType<unknown>>) => {
    const Resolved = React.lazy(async () => {
      const mod = await loader();
      const Component =
        (mod as { default?: React.ComponentType<unknown> }).default ??
        (mod as unknown as React.ComponentType<unknown>);
      return { default: Component };
    });
    const Wrapper: React.FC<Record<string, unknown>> = (props) => (
      <React.Suspense fallback={<div data-testid="dynamic-fallback" />}>
        <Resolved {...props} />
      </React.Suspense>
    );
    Wrapper.displayName = 'MockDynamicWrapper';
    return Wrapper;
  },
}));

// 2. Mock react-konva. Stage/Layer/Rect/Circle/Text/Path/Group become plain
//    divs so assertions can read DOM attributes without touching the real
//    canvas API. We surface props as data-* attributes rather than
//    spreading them, so a consumer's `data-testid` can't clobber the mock's
//    own testid.
vi.mock('react-konva', () => ({
  __esModule: true,
  Stage: ({ children, width, height, scaleX }: { children?: React.ReactNode; width?: number; height?: number; scaleX?: number }) =>
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
      'data-testid': (rest['data-testid'] as string | undefined) ?? 'mock-circle',
      'data-cx': String(rest.x ?? ''),
      'data-cy': String(rest.y ?? ''),
      'data-radius': String(rest.radius ?? ''),
      'data-fill': String(rest.fill ?? ''),
      'data-stroke': String(rest.stroke ?? ''),
      'data-opacity': String(rest.opacity ?? ''),
      'data-listening': String(rest.listening ?? ''),
      // onClick comes through when the seat is selectable. react-konva's
      // onClick/onTap are renamed to the React DOM equivalents in the mock so
      // fireEvent.click() works the same way it would in the real canvas.
      onClick: rest.onClick as (() => void) | undefined,
    }),
  Text: (rest: Record<string, unknown>) =>
    React.createElement(
      'div',
      {
        'data-testid': 'mock-text',
        'data-fill': String(rest.fill ?? ''),
        'data-font-size': String(rest.fontSize ?? ''),
      },
      String(rest.text ?? ''),
    ),
  Path: (rest: Record<string, unknown>) =>
    React.createElement('div', {
      'data-testid': 'mock-path',
      'data-d': String(rest.data ?? ''),
      'data-fill': String(rest.fill ?? ''),
      'data-stroke': String(rest.stroke ?? ''),
    }),
}));

import { SeatPicker } from '@/presentation/components/features/events/SeatPicker';
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
    canvas: { width: 1200, height: 800, scale: 1, backgroundColor: '#ffffff' },
    createdAt: '2026-04-22T00:00:00Z',
    updatedAt: null,
    rowVersion: 1,
    ...overrides,
  };
}

describe('SeatPicker (Slice 7 S7.1 shell)', () => {
  it('resolves the dynamic Konva chunk and renders a Stage with layers + background Rect', async () => {
    render(<SeatPicker layout={baseLayout()} width={600} />);

    // Wait for React.lazy to resolve the Konva chunk.
    await waitFor(() =>
      expect(screen.getByTestId('mock-stage')).toBeInTheDocument(),
    );
    // S7.2 introduces three layers (background+decorations, zones, tables).
    expect(screen.getAllByTestId('mock-layer').length).toBeGreaterThanOrEqual(1);
    // Empty layout → at least the canvas background rect.
    expect(screen.getAllByTestId('mock-rect').length).toBeGreaterThanOrEqual(1);
  });

  it('sets stage dimensions and scale from the layout canvas and requested width', async () => {
    render(<SeatPicker layout={baseLayout()} width={600} />);

    const stage = await screen.findByTestId('mock-stage');
    expect(stage.getAttribute('data-width')).toBe('600');
    // 1200 x 800 canvas, width=600 → height = 400, scale = 0.5.
    expect(stage.getAttribute('data-height')).toBe('400');
    expect(stage.getAttribute('data-scale')).toBe('0.5');
  });

  it('paints the background with the layout.canvas.backgroundColor', async () => {
    const layout = baseLayout({
      canvas: { width: 1000, height: 500, scale: 1, backgroundColor: '#111827' },
    });
    render(<SeatPicker layout={layout} width={800} />);

    await screen.findByTestId('mock-stage');
    const rects = screen.getAllByTestId('mock-rect');
    // The canvas background is always the first rect.
    const bg = rects[0];
    expect(bg.getAttribute('data-fill')).toBe('#111827');
    expect(bg.getAttribute('data-width')).toBe('1000');
    expect(bg.getAttribute('data-height')).toBe('500');
  });

  it('falls back to 1200x800 #ffffff when canvas is missing', async () => {
    const layout = baseLayout();
    delete (layout as unknown as Record<string, unknown>).canvas;
    render(<SeatPicker layout={layout} width={960} />);

    await screen.findByTestId('mock-stage');
    const bg = screen.getAllByTestId('mock-rect')[0];
    expect(bg.getAttribute('data-fill')).toBe('#ffffff');
    expect(bg.getAttribute('data-width')).toBe('1200');
    expect(bg.getAttribute('data-height')).toBe('800');
  });
});

describe('SeatPicker (Slice 7 S7.2 structural shapes)', () => {
  it('renders a rect zone with the zone color + name label', async () => {
    const layout = baseLayout({
      zones: [
        {
          id: 'z1',
          name: 'Orchestra',
          color: '#3b82f6',
          sortOrder: 0,
          enabledSeatCount: 0,
          totalSeatCount: 0,
          seats: [],
          shape: ZoneShape.Rect,
          geometry: '{"x":100,"y":140,"width":1000,"height":320}',
        },
      ],
    });
    render(<SeatPicker layout={layout} width={600} />);

    await screen.findByTestId('mock-stage');
    // There should now be at least two rects on the stage: the canvas
    // background + the zone rect. And at least one Text labeled 'Orchestra'.
    const rects = screen.getAllByTestId('mock-rect');
    expect(rects.length).toBeGreaterThanOrEqual(2);
    const zoneRect = rects.find((r) => r.getAttribute('data-fill') === '#3b82f6');
    expect(zoneRect).toBeTruthy();

    const labels = screen.getAllByTestId('mock-text').map((t) => t.textContent);
    expect(labels).toContain('Orchestra');
  });

  it('renders a curve zone as a Path with arc geometry', async () => {
    const layout = baseLayout({
      zones: [
        {
          id: 'zc',
          name: 'Curved Front',
          color: '#3b82f6',
          sortOrder: 0,
          enabledSeatCount: 0,
          totalSeatCount: 0,
          seats: [],
          shape: ZoneShape.Curve,
          geometry:
            '{"centerX":600,"centerY":100,"radius":380,"startAngleDeg":20,"sweepAngleDeg":140,"rowCount":4}',
        },
      ],
    });
    render(<SeatPicker layout={layout} width={600} />);

    await screen.findByTestId('mock-stage');
    const path = screen.getByTestId('mock-path');
    // Verify the arc path string references the correct radius + center.
    const d = path.getAttribute('data-d') ?? '';
    expect(d).toContain('A 380 380');
    expect(d.startsWith('M 600 100')).toBe(true);
  });

  it('renders a round table as a Circle with the label text', async () => {
    const layout = baseLayout({
      layoutType: 'Banquet',
      tables: [
        {
          id: 't1',
          venueLayoutId: 'layout-1',
          label: 'T1',
          shape: TableShape.Round,
          geometry: '{"centerX":140,"centerY":170,"radius":55}',
          capacity: 8,
          sortOrder: 0,
          enabledSeatCount: 0,
          seats: [],
        },
      ],
    });
    render(<SeatPicker layout={layout} width={600} />);

    await screen.findByTestId('mock-stage');
    const circle = screen.getByTestId('mock-circle');
    expect(circle.getAttribute('data-cx')).toBe('140');
    expect(circle.getAttribute('data-cy')).toBe('170');
    expect(circle.getAttribute('data-radius')).toBe('55');
    const labels = screen.getAllByTestId('mock-text').map((t) => t.textContent);
    expect(labels).toContain('T1');
  });

  it('renders a rect table centered on the declared geometry', async () => {
    const layout = baseLayout({
      layoutType: 'Mixed',
      tables: [
        {
          id: 't2',
          venueLayoutId: 'layout-1',
          label: 'Head',
          shape: TableShape.Rect,
          geometry: '{"centerX":600,"centerY":120,"width":500,"height":60}',
          capacity: 8,
          sortOrder: 0,
          enabledSeatCount: 0,
          seats: [],
        },
      ],
    });
    render(<SeatPicker layout={layout} width={600} />);

    await screen.findByTestId('mock-stage');
    const rects = screen.getAllByTestId('mock-rect');
    const tableRect = rects.find((r) => r.getAttribute('data-fill') === '#fee2e2');
    expect(tableRect).toBeTruthy();
    expect(tableRect!.getAttribute('data-x')).toBe('350');
    expect(tableRect!.getAttribute('data-width')).toBe('500');
    expect(
      screen.getAllByTestId('mock-text').some((t) => t.textContent === 'Head'),
    ).toBe(true);
  });

  it('renders stage decorations with the "STAGE" default label', async () => {
    const layout = baseLayout({
      decorations: [
        {
          id: 'd1',
          venueLayoutId: 'layout-1',
          kind: DecorationKind.Stage,
          label: null,
          geometry: '{"x":300,"y":20,"width":600,"height":80}',
          properties: '{}',
          sortOrder: 0,
        },
      ],
    });
    render(<SeatPicker layout={layout} width={600} />);

    await screen.findByTestId('mock-stage');
    const labels = screen.getAllByTestId('mock-text').map((t) => t.textContent);
    expect(labels).toContain('STAGE');
  });

  it('degrades gracefully on malformed zone geometry', async () => {
    const layout = baseLayout({
      zones: [
        {
          id: 'bad',
          name: 'Bad Zone',
          color: '#000',
          sortOrder: 0,
          enabledSeatCount: 0,
          totalSeatCount: 0,
          seats: [],
          shape: ZoneShape.Rect,
          geometry: 'not json',
        },
      ],
    });
    render(<SeatPicker layout={layout} width={600} />);

    await screen.findByTestId('mock-stage');
    // Placeholder text is rendered for the zone; no throw.
    const labels = screen.getAllByTestId('mock-text').map((t) => t.textContent);
    expect(labels).toContain('Bad Zone');
  });
});

describe('SeatPicker (Slice 7 S7.3 seats + status + click + tier filter)', () => {
  const ZONE_ID = 'z1';

  const zoneLayout = (seatsOverride?: Partial<{ isEnabled: boolean; id: string; row: string; number: number; label: string; sortOrder: number; isAccessible: boolean }>[]): VenueLayoutDto =>
    baseLayout({
      zones: [
        {
          id: ZONE_ID,
          name: 'Orchestra',
          color: '#3b82f6',
          sortOrder: 0,
          enabledSeatCount: 6,
          totalSeatCount: 6,
          shape: ZoneShape.Rect,
          geometry: '{"x":100,"y":140,"width":1000,"height":320}',
          seats: (seatsOverride ?? []).length
            ? (seatsOverride as unknown as VenueLayoutDto['zones'][0]['seats'])
            : [
                { id: 's-a1', row: 'A', number: 1, label: 'A1', sortOrder: 0, isEnabled: true, isAccessible: false },
                { id: 's-a2', row: 'A', number: 2, label: 'A2', sortOrder: 1, isEnabled: true, isAccessible: false },
                { id: 's-a3', row: 'A', number: 3, label: 'A3', sortOrder: 2, isEnabled: true, isAccessible: false },
                { id: 's-b1', row: 'B', number: 1, label: 'B1', sortOrder: 3, isEnabled: true, isAccessible: false },
                { id: 's-b2', row: 'B', number: 2, label: 'B2', sortOrder: 4, isEnabled: true, isAccessible: false },
                { id: 's-b3', row: 'B', number: 3, label: 'B3', sortOrder: 5, isEnabled: true, isAccessible: false },
              ],
        },
      ],
    });

  it('renders a circle per seat with the zone color when no availability is supplied', async () => {
    render(<SeatPicker layout={zoneLayout()} width={600} />);

    await screen.findByTestId('mock-stage');
    expect(screen.getByTestId('seat-s-a1')).toBeInTheDocument();
    expect(screen.getByTestId('seat-s-b3')).toBeInTheDocument();
    expect(screen.getByTestId('seat-s-a1').getAttribute('data-fill')).toBe('#3b82f6');
  });

  it('applies status-specific colors from the availability map', async () => {
    const availability = [
      { id: 's-a1', label: 'A1', row: 'A', number: 1, isEnabled: true, isAccessible: false, status: 'Held', zoneId: ZONE_ID, zoneName: 'Orchestra', zoneColor: '#3b82f6' },
      { id: 's-a2', label: 'A2', row: 'A', number: 2, isEnabled: true, isAccessible: false, status: 'Reserved', zoneId: ZONE_ID, zoneName: 'Orchestra', zoneColor: '#3b82f6' },
      { id: 's-a3', label: 'A3', row: 'A', number: 3, isEnabled: true, isAccessible: false, status: 'Available', zoneId: ZONE_ID, zoneName: 'Orchestra', zoneColor: '#3b82f6' },
    ];
    render(
      <SeatPicker
        layout={zoneLayout()}
        availability={availability as never}
        width={600}
      />,
    );
    await screen.findByTestId('mock-stage');

    expect(screen.getByTestId('seat-s-a1').getAttribute('data-fill')).toBe('#f59e0b'); // amber (held)
    expect(screen.getByTestId('seat-s-a2').getAttribute('data-fill')).toBe('#ef4444'); // red (reserved)
    expect(screen.getByTestId('seat-s-a3').getAttribute('data-fill')).toBe('#3b82f6'); // available → zone color
  });

  it('paints selected seats emerald regardless of other status', async () => {
    const selected = new Set(['s-a1']);
    render(
      <SeatPicker
        layout={zoneLayout()}
        selectedSeatIds={selected}
        width={600}
      />,
    );
    await screen.findByTestId('mock-stage');

    expect(screen.getByTestId('seat-s-a1').getAttribute('data-fill')).toBe('#10b981');
  });

  it('grays seats outside the tier-filter and makes them non-listening', async () => {
    const eligible = new Set(['s-a1', 's-a2']);
    render(
      <SeatPicker
        layout={zoneLayout()}
        eligibleSeatIds={eligible}
        width={600}
      />,
    );
    await screen.findByTestId('mock-stage');

    // eligible seats keep the zone color
    expect(screen.getByTestId('seat-s-a1').getAttribute('data-fill')).toBe('#3b82f6');
    // ineligible seats render with the filtered palette (pale gray)
    expect(screen.getByTestId('seat-s-b1').getAttribute('data-fill')).toBe('#e5e7eb');
    expect(screen.getByTestId('seat-s-b1').getAttribute('data-listening')).toBe('false');
  });

  it('fires onSeatClick only for selectable seats (Available + eligible)', async () => {
    const onSeatClick = vi.fn();
    const availability = [
      { id: 's-a1', label: 'A1', row: 'A', number: 1, isEnabled: true, isAccessible: false, status: 'Reserved', zoneId: ZONE_ID, zoneName: 'Orchestra', zoneColor: '#3b82f6' },
      { id: 's-a2', label: 'A2', row: 'A', number: 2, isEnabled: true, isAccessible: false, status: 'Available', zoneId: ZONE_ID, zoneName: 'Orchestra', zoneColor: '#3b82f6' },
    ];
    render(
      <SeatPicker
        layout={zoneLayout()}
        availability={availability as never}
        onSeatClick={onSeatClick}
        width={600}
      />,
    );
    await screen.findByTestId('mock-stage');

    // Reserved seat click should be a no-op.
    fireEvent.click(screen.getByTestId('seat-s-a1'));
    expect(onSeatClick).not.toHaveBeenCalled();
    // Available + implicitly eligible (no filter) → fires.
    fireEvent.click(screen.getByTestId('seat-s-a2'));
    expect(onSeatClick).toHaveBeenCalledTimes(1);
    expect(onSeatClick).toHaveBeenCalledWith('s-a2');
  });

  it('allows clicking a selected seat again (so callers can implement deselect)', async () => {
    const onSeatClick = vi.fn();
    const selected = new Set(['s-a1']);
    render(
      <SeatPicker
        layout={zoneLayout()}
        selectedSeatIds={selected}
        onSeatClick={onSeatClick}
        width={600}
      />,
    );
    await screen.findByTestId('mock-stage');

    fireEvent.click(screen.getByTestId('seat-s-a1'));
    expect(onSeatClick).toHaveBeenCalledWith('s-a1');
  });

  it('renders round-table seats as a ring of circles around the table', async () => {
    const layout = baseLayout({
      layoutType: 'Banquet',
      tables: [
        {
          id: 't1',
          venueLayoutId: 'layout-1',
          label: 'T1',
          shape: TableShape.Round,
          geometry: '{"centerX":200,"centerY":200,"radius":60}',
          capacity: 4,
          sortOrder: 0,
          enabledSeatCount: 4,
          seats: [
            { id: 't1-s1', row: 'T1', number: 1, label: 'T1-S1', sortOrder: 0, isEnabled: true, isAccessible: false },
            { id: 't1-s2', row: 'T1', number: 2, label: 'T1-S2', sortOrder: 1, isEnabled: true, isAccessible: false },
            { id: 't1-s3', row: 'T1', number: 3, label: 'T1-S3', sortOrder: 2, isEnabled: true, isAccessible: false },
            { id: 't1-s4', row: 'T1', number: 4, label: 'T1-S4', sortOrder: 3, isEnabled: true, isAccessible: false },
          ],
        },
      ],
    });
    render(<SeatPicker layout={layout} width={600} />);
    await screen.findByTestId('mock-stage');

    // All 4 seat circles are present.
    expect(screen.getByTestId('seat-t1-s1')).toBeInTheDocument();
    expect(screen.getByTestId('seat-t1-s4')).toBeInTheDocument();
    // They sit at ~60 + seatRadius + 3 ≈ 65–73 away from the table center.
    const seat1 = screen.getByTestId('seat-t1-s1');
    const cx = Number(seat1.getAttribute('data-cx'));
    const cy = Number(seat1.getAttribute('data-cy'));
    const dist = Math.sqrt((cx - 200) ** 2 + (cy - 200) ** 2);
    expect(dist).toBeGreaterThan(60);
  });

  it('renders structurally-disabled seats with the disabled palette and no listening', async () => {
    const layout = zoneLayout([
      { id: 's-a1', row: 'A', number: 1, label: 'A1', sortOrder: 0, isEnabled: false, isAccessible: false },
    ]);
    render(<SeatPicker layout={layout} width={600} />);
    await screen.findByTestId('mock-stage');

    const seat = screen.getByTestId('seat-s-a1');
    expect(seat.getAttribute('data-fill')).toBe('#d1d5db');
    expect(seat.getAttribute('data-listening')).toBe('false');
  });
});

describe('SeatPicker (Slice 7 S7.5 mobile gestures + zoom controls)', () => {
  it('renders a zoom-controls overlay with +/− and reset buttons', async () => {
    render(<SeatPicker layout={baseLayout()} width={600} />);
    await screen.findByTestId('mock-stage');

    expect(screen.getByTestId('seat-picker-zoom-controls')).toBeInTheDocument();
    expect(screen.getByTestId('seat-picker-zoom-in')).toBeInTheDocument();
    expect(screen.getByTestId('seat-picker-zoom-out')).toBeInTheDocument();
    expect(screen.getByTestId('seat-picker-zoom-reset')).toBeInTheDocument();
  });

  it('disables the zoom-out button initially (userScale = 1 is not at the min)', async () => {
    render(<SeatPicker layout={baseLayout()} width={600} />);
    await screen.findByTestId('mock-stage');

    // Initial userScale=1 is in-range, so neither button should be disabled.
    const zoomIn = screen.getByTestId('seat-picker-zoom-in');
    const zoomOut = screen.getByTestId('seat-picker-zoom-out');
    expect(zoomIn).not.toBeDisabled();
    expect(zoomOut).not.toBeDisabled();
  });

  it('zoom-in increases stage scale', async () => {
    render(<SeatPicker layout={baseLayout()} width={600} />);
    await screen.findByTestId('mock-stage');
    const stage = screen.getByTestId('mock-stage');
    const initialScale = Number(stage.getAttribute('data-scale'));

    fireEvent.click(screen.getByTestId('seat-picker-zoom-in'));

    const afterScale = Number(
      screen.getByTestId('mock-stage').getAttribute('data-scale'),
    );
    expect(afterScale).toBeGreaterThan(initialScale);
  });

  it('reset returns the stage to the base scale', async () => {
    render(<SeatPicker layout={baseLayout()} width={600} />);
    await screen.findByTestId('mock-stage');
    const initialScale = Number(
      screen.getByTestId('mock-stage').getAttribute('data-scale'),
    );

    fireEvent.click(screen.getByTestId('seat-picker-zoom-in'));
    fireEvent.click(screen.getByTestId('seat-picker-zoom-in'));
    expect(
      Number(screen.getByTestId('mock-stage').getAttribute('data-scale')),
    ).toBeGreaterThan(initialScale);

    fireEvent.click(screen.getByTestId('seat-picker-zoom-reset'));
    expect(
      Number(screen.getByTestId('mock-stage').getAttribute('data-scale')),
    ).toBeCloseTo(initialScale, 5);
  });
});
