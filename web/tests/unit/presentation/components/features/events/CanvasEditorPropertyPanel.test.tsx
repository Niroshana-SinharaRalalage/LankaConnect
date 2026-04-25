/**
 * Slice 8 S8.5a — CanvasEditorPropertyPanel tests.
 *
 * Focus: selection → editable dimension inputs → onGeometryChange emission
 * with the same geometry JSON shape the stage already produces. The panel
 * is fully controlled (no internal state beyond the typing buffer), so
 * tests drive it by props + simulating user input on the number fields.
 */

import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';

import { CanvasEditorPropertyPanel } from '@/presentation/components/features/events/CanvasEditorPropertyPanel';
import {
  DecorationKind,
  TableShape,
  ZoneShape,
  type VenueLayoutDto,
} from '@/infrastructure/api/types/events.types';
import type { CanvasItemRef } from '@/presentation/utils/canvasEditorGeometry';

function layoutFixture(): VenueLayoutDto {
  return {
    id: 'layout-1',
    name: 'Test',
    eventId: null,
    layoutType: 'Theater',
    isTemplate: true,
    createdByUserId: 'u1',
    totalCapacity: 0,
    createdAt: '2026-04-24T00:00:00Z',
    updatedAt: null,
    rowVersion: 1,
    canvas: { width: 1000, height: 800, scale: 1, backgroundColor: '#fff' },
    zones: [
      {
        id: 'z-rect',
        name: 'Orchestra',
        color: '#3B82F6',
        sortOrder: 0,
        enabledSeatCount: 0,
        totalSeatCount: 0,
        seats: [],
        shape: ZoneShape.Rect,
        geometry: JSON.stringify({ x: 100, y: 100, width: 400, height: 200, rotation: 0 }),
      },
      {
        id: 'z-curve',
        name: 'Mezzanine',
        color: '#A855F7',
        sortOrder: 1,
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
    tables: [
      {
        id: 't-round',
        venueLayoutId: 'layout-1',
        label: 'T1',
        shape: TableShape.Round,
        geometry: JSON.stringify({ centerX: 300, centerY: 300, radius: 40 }),
        capacity: 8,
        sortOrder: 0,
        enabledSeatCount: 8,
        seats: [],
      },
      {
        id: 't-rect',
        venueLayoutId: 'layout-1',
        label: 'Head',
        shape: TableShape.Rect,
        geometry: JSON.stringify({
          centerX: 600,
          centerY: 300,
          width: 200,
          height: 80,
          rotation: 30,
        }),
        capacity: 10,
        sortOrder: 1,
        enabledSeatCount: 10,
        seats: [],
      },
    ],
    decorations: [
      {
        id: 'd1',
        venueLayoutId: 'layout-1',
        kind: DecorationKind.Stage,
        label: 'Main Stage',
        geometry: JSON.stringify({ x: 300, y: 50, width: 400, height: 100, rotation: 0 }),
        properties: '{}',
        sortOrder: 0,
      },
    ],
  };
}

function mount(
  selected: CanvasItemRef | null,
  drafts: Record<string, string> = {},
  onGeometryChange: (ref: CanvasItemRef, g: string) => void = vi.fn(),
  layout: VenueLayoutDto = layoutFixture(),
) {
  render(
    <CanvasEditorPropertyPanel
      layout={layout}
      selected={selected}
      draftGeometryByKey={drafts}
      onGeometryChange={onGeometryChange}
    />,
  );
  return onGeometryChange;
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('CanvasEditorPropertyPanel — empty / missing states', () => {
  it('renders an empty state when nothing is selected', () => {
    mount(null);
    expect(screen.getByTestId('canvas-editor-property-panel')).toBeInTheDocument();
    expect(screen.getByTestId('property-panel-empty')).toBeInTheDocument();
  });

  it('renders a warning when the selected item no longer exists', () => {
    mount({ kind: 'zone', id: 'does-not-exist' });
    expect(screen.getByTestId('property-panel-missing')).toBeInTheDocument();
  });
});

describe('CanvasEditorPropertyPanel — rect zone edits', () => {
  it('shows width, height, rotation for a rect zone', () => {
    mount({ kind: 'zone', id: 'z-rect' });
    expect(screen.getByTestId('property-panel-prop-width')).toHaveValue(400);
    expect(screen.getByTestId('property-panel-prop-height')).toHaveValue(200);
    expect(screen.getByTestId('property-panel-prop-rotation')).toHaveValue(0);
    expect(screen.getByTestId('property-panel-item-label')).toHaveTextContent('Orchestra');
  });

  it('commits snapped width on blur', () => {
    const onGeometryChange = vi.fn();
    mount({ kind: 'zone', id: 'z-rect' }, {}, onGeometryChange);
    const input = screen.getByTestId('property-panel-prop-width');
    fireEvent.change(input, { target: { value: '623' } });
    fireEvent.blur(input);
    expect(onGeometryChange).toHaveBeenCalledTimes(1);
    const [, geomJson] = onGeometryChange.mock.calls[0];
    const parsed = JSON.parse(geomJson);
    // 623 snaps to 600. Center of the original rect was (300, 200); keeping that
    // center with new width 600 → top-left (0, 100).
    expect(parsed.width).toBe(600);
    expect(parsed.height).toBe(200);
    expect(parsed.x).toBe(0);
    expect(parsed.y).toBe(100);
  });

  it('clamps width to MIN_SHAPE_DIMENSION when user enters something tiny', () => {
    const onGeometryChange = vi.fn();
    mount({ kind: 'zone', id: 'z-rect' }, {}, onGeometryChange);
    const input = screen.getByTestId('property-panel-prop-width');
    fireEvent.change(input, { target: { value: '5' } });
    fireEvent.blur(input);
    const [, geomJson] = onGeometryChange.mock.calls[0];
    expect(JSON.parse(geomJson).width).toBe(50);
  });

  it('commits snapped rotation (15° step) on blur', () => {
    const onGeometryChange = vi.fn();
    mount({ kind: 'zone', id: 'z-rect' }, {}, onGeometryChange);
    const input = screen.getByTestId('property-panel-prop-rotation');
    fireEvent.change(input, { target: { value: '47' } });
    fireEvent.blur(input);
    const [, geomJson] = onGeometryChange.mock.calls[0];
    expect(JSON.parse(geomJson).rotation).toBe(45);
  });

  it('Enter key commits the value and blurs', () => {
    const onGeometryChange = vi.fn();
    mount({ kind: 'zone', id: 'z-rect' }, {}, onGeometryChange);
    const input = screen.getByTestId('property-panel-prop-width') as HTMLInputElement;
    fireEvent.change(input, { target: { value: '300' } });
    fireEvent.keyDown(input, { key: 'Enter' });
    expect(onGeometryChange).toHaveBeenCalledTimes(1);
    expect(JSON.parse(onGeometryChange.mock.calls[0][1]).width).toBe(300);
  });

  it('same value after typing does not emit a commit', () => {
    const onGeometryChange = vi.fn();
    mount({ kind: 'zone', id: 'z-rect' }, {}, onGeometryChange);
    const input = screen.getByTestId('property-panel-prop-width');
    fireEvent.change(input, { target: { value: '400' } });
    fireEvent.blur(input);
    expect(onGeometryChange).not.toHaveBeenCalled();
  });
});

describe('CanvasEditorPropertyPanel — round table edits', () => {
  it('shows radius only for a round table; hides width/height/rotation', () => {
    mount({ kind: 'table', id: 't-round' });
    expect(screen.getByTestId('property-panel-prop-radius')).toHaveValue(40);
    expect(screen.queryByTestId('property-panel-prop-width')).toBeNull();
    expect(screen.queryByTestId('property-panel-prop-height')).toBeNull();
    expect(screen.queryByTestId('property-panel-prop-rotation')).toBeNull();
    expect(screen.getByTestId('property-panel-item-label')).toHaveTextContent('T1');
  });

  it('commits snapped radius with minimum floor of MIN_SHAPE_DIMENSION/2 (25)', () => {
    const onGeometryChange = vi.fn();
    mount({ kind: 'table', id: 't-round' }, {}, onGeometryChange);
    const input = screen.getByTestId('property-panel-prop-radius');
    fireEvent.change(input, { target: { value: '5' } });
    fireEvent.blur(input);
    const [, geomJson] = onGeometryChange.mock.calls[0];
    expect(JSON.parse(geomJson)).toEqual({ centerX: 300, centerY: 300, radius: 25 });
  });

  it('commits a sensible round-table radius on blur', () => {
    const onGeometryChange = vi.fn();
    mount({ kind: 'table', id: 't-round' }, {}, onGeometryChange);
    const input = screen.getByTestId('property-panel-prop-radius');
    fireEvent.change(input, { target: { value: '74' } });
    fireEvent.blur(input);
    const [, geomJson] = onGeometryChange.mock.calls[0];
    // 74 snaps to 50 (50px grid). That's ≥ min 25 so it sticks.
    expect(JSON.parse(geomJson).radius).toBe(50);
  });
});

describe('CanvasEditorPropertyPanel — rect table edits', () => {
  it('shows width, height, rotation for a rect table', () => {
    mount({ kind: 'table', id: 't-rect' });
    expect(screen.getByTestId('property-panel-prop-width')).toHaveValue(200);
    expect(screen.getByTestId('property-panel-prop-height')).toHaveValue(80);
    expect(screen.getByTestId('property-panel-prop-rotation')).toHaveValue(30);
    expect(screen.queryByTestId('property-panel-prop-radius')).toBeNull();
  });
});

describe('CanvasEditorPropertyPanel — decoration edits', () => {
  it('shows width, height, rotation for a decoration', () => {
    mount({ kind: 'decoration', id: 'd1' });
    expect(screen.getByTestId('property-panel-prop-width')).toHaveValue(400);
    expect(screen.getByTestId('property-panel-prop-height')).toHaveValue(100);
    expect(screen.getByTestId('property-panel-prop-rotation')).toHaveValue(0);
    expect(screen.getByTestId('property-panel-item-label')).toHaveTextContent('Main Stage');
  });
});

describe('CanvasEditorPropertyPanel — curve zone is read-only with a hint', () => {
  it('shows an explanatory hint instead of number inputs for curve zones', () => {
    mount({ kind: 'zone', id: 'z-curve' });
    expect(screen.getByTestId('property-panel-curve-hint')).toBeInTheDocument();
    expect(screen.queryByTestId('property-panel-prop-width')).toBeNull();
    expect(screen.queryByTestId('property-panel-prop-rotation')).toBeNull();
  });
});

describe('CanvasEditorPropertyPanel — reads draft override over persisted geometry', () => {
  it('shows the draft width when draftGeometryByKey has an entry', () => {
    const drafts = {
      'zone:z-rect': JSON.stringify({ x: 0, y: 0, width: 800, height: 200 }),
    };
    mount({ kind: 'zone', id: 'z-rect' }, drafts);
    expect(screen.getByTestId('property-panel-prop-width')).toHaveValue(800);
  });

  it('further edits commit on top of the draft (not the persisted value)', () => {
    const onGeometryChange = vi.fn();
    const drafts = {
      'zone:z-rect': JSON.stringify({ x: 0, y: 0, width: 800, height: 200 }),
    };
    mount({ kind: 'zone', id: 'z-rect' }, drafts, onGeometryChange);
    const input = screen.getByTestId('property-panel-prop-width');
    fireEvent.change(input, { target: { value: '500' } });
    fireEvent.blur(input);
    const [, geomJson] = onGeometryChange.mock.calls[0];
    // Draft center was (400, 100). Resizing to width 500 keeps that center
    // → top-left x = 400 - 250 = 150.
    expect(JSON.parse(geomJson)).toMatchObject({
      x: 150,
      y: 0,
      width: 500,
      height: 200,
    });
  });
});

// ─────────────────────────── S8.7 tier panel integration ───────────────────────────

describe('CanvasEditorPropertyPanel — tier panel integration', () => {
  const fakeTiers: Array<{ id: string; name: string } & Record<string, unknown>> = [
    { id: 't-vip', name: 'VIP' },
    { id: 't-plus', name: 'Plus' },
  ];

  it('does not render the tier panel for decorations', () => {
    const onToggle = vi.fn();
    render(
      <CanvasEditorPropertyPanel
        layout={layoutFixture()}
        selected={{ kind: 'decoration', id: 'd1' }}
        draftGeometryByKey={{}}
        onGeometryChange={vi.fn()}
        tiers={fakeTiers as unknown as Parameters<typeof CanvasEditorPropertyPanel>[0]['tiers']}
        onToggleTierAssignment={onToggle}
      />,
    );
    expect(screen.queryByTestId('canvas-editor-tier-panel')).toBeNull();
  });

  it('does not render the tier panel when onToggleTierAssignment is omitted', () => {
    render(
      <CanvasEditorPropertyPanel
        layout={layoutFixture()}
        selected={{ kind: 'zone', id: 'z-rect' }}
        draftGeometryByKey={{}}
        onGeometryChange={vi.fn()}
      />,
    );
    expect(screen.queryByTestId('canvas-editor-tier-panel')).toBeNull();
  });

  it('renders tier panel with template hint when layout has no eventId', () => {
    const onToggle = vi.fn();
    // layoutFixture() sets eventId: null → template layout.
    render(
      <CanvasEditorPropertyPanel
        layout={layoutFixture()}
        selected={{ kind: 'zone', id: 'z-rect' }}
        draftGeometryByKey={{}}
        onGeometryChange={vi.fn()}
        tiers={fakeTiers as unknown as Parameters<typeof CanvasEditorPropertyPanel>[0]['tiers']}
        onToggleTierAssignment={onToggle}
      />,
    );
    expect(screen.getByTestId('canvas-editor-tier-panel')).toBeInTheDocument();
    expect(screen.getByTestId('tier-panel-template-hint')).toBeInTheDocument();
  });

  it('renders tier checkboxes when the layout is attached to an event', () => {
    const onToggle = vi.fn();
    const eventLayout = { ...layoutFixture(), eventId: 'evt-123' };
    render(
      <CanvasEditorPropertyPanel
        layout={eventLayout}
        selected={{ kind: 'zone', id: 'z-rect' }}
        draftGeometryByKey={{}}
        onGeometryChange={vi.fn()}
        tiers={fakeTiers as unknown as Parameters<typeof CanvasEditorPropertyPanel>[0]['tiers']}
        onToggleTierAssignment={onToggle}
      />,
    );
    expect(screen.getByTestId('tier-panel-checkbox-t-vip')).not.toBeChecked();
    expect(screen.getByTestId('tier-panel-checkbox-t-plus')).not.toBeChecked();
  });

  it('clicking a tier checkbox fires onToggleTierAssignment with the ref + tierId', () => {
    const onToggle = vi.fn();
    const eventLayout = { ...layoutFixture(), eventId: 'evt-123' };
    render(
      <CanvasEditorPropertyPanel
        layout={eventLayout}
        selected={{ kind: 'zone', id: 'z-rect' }}
        draftGeometryByKey={{}}
        onGeometryChange={vi.fn()}
        tiers={fakeTiers as unknown as Parameters<typeof CanvasEditorPropertyPanel>[0]['tiers']}
        onToggleTierAssignment={onToggle}
      />,
    );
    fireEvent.click(screen.getByTestId('tier-panel-checkbox-t-vip'));
    expect(onToggle).toHaveBeenCalledWith({ kind: 'zone', id: 'z-rect' }, 't-vip');
  });

  it('honors the draftTierAssignmentsByKey override when rendering checkbox state', () => {
    const onToggle = vi.fn();
    const eventLayout = { ...layoutFixture(), eventId: 'evt-123' };
    render(
      <CanvasEditorPropertyPanel
        layout={eventLayout}
        selected={{ kind: 'zone', id: 'z-rect' }}
        draftGeometryByKey={{}}
        onGeometryChange={vi.fn()}
        tiers={fakeTiers as unknown as Parameters<typeof CanvasEditorPropertyPanel>[0]['tiers']}
        draftTierAssignmentsByKey={{ 'zone:z-rect': ['t-vip'] }}
        onToggleTierAssignment={onToggle}
      />,
    );
    expect(screen.getByTestId('tier-panel-checkbox-t-vip')).toBeChecked();
    expect(screen.getByTestId('tier-panel-checkbox-t-plus')).not.toBeChecked();
  });
});
