/**
 * Slice 6 Chunk S6.8 — LayoutPreview tests.
 *
 * Focus: the read-only renderer projects VenueLayoutDto structures onto SVG
 * shapes. Geometry is JSON-encoded; the preview parses it tolerantly so bad
 * data degrades gracefully rather than crashing the page.
 */

import React from 'react';
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';

import { LayoutPreview } from '@/presentation/components/features/events/LayoutPreview';
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

describe('LayoutPreview', () => {
  it('renders an SVG with the layout canvas dimensions', () => {
    const layout = baseLayout();
    const { container } = render(<LayoutPreview layout={layout} />);

    const svg = container.querySelector('svg');
    expect(svg).not.toBeNull();
    expect(svg!.getAttribute('viewBox')).toBe('0 0 1200 800');
  });

  it('falls back to 1200×800 when canvas is missing', () => {
    const layout = baseLayout();
    delete (layout as unknown as Record<string, unknown>).canvas;
    const { container } = render(<LayoutPreview layout={layout} />);

    expect(container.querySelector('svg')!.getAttribute('viewBox')).toBe(
      '0 0 1200 800',
    );
  });

  it('uses the layout name in the default aria-label', () => {
    const layout = baseLayout({ name: 'Grand Ballroom' });
    render(<LayoutPreview layout={layout} />);

    expect(
      screen.getByRole('img', { name: /Layout preview: Grand Ballroom/ }),
    ).toBeInTheDocument();
  });

  it('renders rect zones as <rect> elements with the zone color', () => {
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
    render(<LayoutPreview layout={layout} />);

    const zone = screen.getByTestId('zone-z1');
    const rect = zone.querySelector('rect');
    expect(rect).not.toBeNull();
    expect(rect!.getAttribute('x')).toBe('100');
    expect(rect!.getAttribute('width')).toBe('1000');
    expect(rect!.getAttribute('stroke')).toBe('#3b82f6');
    expect(screen.getByText('Orchestra')).toBeInTheDocument();
  });

  it('renders curve zones as <path> arcs', () => {
    const layout = baseLayout({
      zones: [
        {
          id: 'zc',
          name: 'Front Curved',
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
    render(<LayoutPreview layout={layout} />);

    const zone = screen.getByTestId('zone-zc');
    expect(zone.querySelector('path')).not.toBeNull();
  });

  it('renders round tables as <circle> with the label text', () => {
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
    render(<LayoutPreview layout={layout} />);

    const table = screen.getByTestId('table-t1');
    const circle = table.querySelector('circle');
    expect(circle).not.toBeNull();
    expect(circle!.getAttribute('cx')).toBe('140');
    expect(circle!.getAttribute('r')).toBe('55');
    expect(screen.getByText('T1')).toBeInTheDocument();
  });

  it('renders rect tables as <rect> centered on the declared geometry', () => {
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
    render(<LayoutPreview layout={layout} />);

    const table = screen.getByTestId('table-t2');
    const rect = table.querySelector('rect');
    expect(rect).not.toBeNull();
    // centerX=600, width=500 → x = 350
    expect(rect!.getAttribute('x')).toBe('350');
    expect(rect!.getAttribute('width')).toBe('500');
    expect(screen.getByText('Head')).toBeInTheDocument();
  });

  it('renders stage decorations with the STAGE label', () => {
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
    render(<LayoutPreview layout={layout} />);

    expect(screen.getByTestId('decoration-d1')).toBeInTheDocument();
    expect(screen.getByText('STAGE')).toBeInTheDocument();
  });

  it('tolerates missing / malformed geometry by skipping the shape quietly', () => {
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
      tables: [
        {
          id: 'bad-table',
          venueLayoutId: 'layout-1',
          label: 'X',
          shape: TableShape.Round,
          geometry: '',
          capacity: 8,
          sortOrder: 0,
          enabledSeatCount: 0,
          seats: [],
        },
      ],
    });

    const { container } = render(<LayoutPreview layout={layout} />);

    // Placeholder fallback for the zone; table bails out silently.
    expect(screen.getByTestId('zone-placeholder-bad')).toBeInTheDocument();
    expect(screen.queryByTestId('table-bad-table')).not.toBeInTheDocument();
    // The render must not throw — container is populated.
    expect(container.querySelector('svg')).not.toBeNull();
  });

  it('skips seat dots when showSeats=false', () => {
    const layout = baseLayout({
      tables: [
        {
          id: 't1',
          venueLayoutId: 'layout-1',
          label: 'T1',
          shape: TableShape.Round,
          geometry: '{"centerX":140,"centerY":170,"radius":55}',
          capacity: 2,
          sortOrder: 0,
          enabledSeatCount: 0,
          seats: [
            { id: 's1', row: 'T1', number: 1, label: 'T1-S1', sortOrder: 0, isEnabled: true, isAccessible: false },
            { id: 's2', row: 'T1', number: 2, label: 'T1-S2', sortOrder: 1, isEnabled: true, isAccessible: false },
          ] as never,
        },
      ],
    });
    const { container } = render(<LayoutPreview layout={layout} showSeats={false} />);

    // Only the table circle should be present (no radial seat dots).
    const circles = container.querySelectorAll('circle');
    expect(circles.length).toBe(1);
  });
});
