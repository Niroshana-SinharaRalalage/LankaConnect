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
import { render, screen, waitFor } from '@testing-library/react';

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

// 2. Mock react-konva. Stage/Layer/Rect become plain divs so assertions can
//    read DOM attributes without touching the real canvas API. We surface
//    props as data-* attributes rather than spreading them, so a consumer's
//    `data-testid` can't clobber the mock's own testid.
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
  Rect: (rest: Record<string, unknown>) =>
    React.createElement('div', {
      'data-testid': 'mock-rect',
      'data-x': String(rest.x ?? ''),
      'data-y': String(rest.y ?? ''),
      'data-width': String(rest.width ?? ''),
      'data-height': String(rest.height ?? ''),
      'data-fill': String(rest.fill ?? ''),
    }),
}));

import { SeatPicker } from '@/presentation/components/features/events/SeatPicker';
import type { VenueLayoutDto } from '@/infrastructure/api/types/events.types';

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
  it('resolves the dynamic Konva chunk and renders a Stage + Layer + background Rect', async () => {
    render(<SeatPicker layout={baseLayout()} width={600} />);

    // Wait for React.lazy to resolve the Konva chunk.
    await waitFor(() =>
      expect(screen.getByTestId('mock-stage')).toBeInTheDocument(),
    );
    expect(screen.getByTestId('mock-layer')).toBeInTheDocument();
    expect(screen.getByTestId('mock-rect')).toBeInTheDocument();
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

    const rect = await screen.findByTestId('mock-rect');
    expect(rect.getAttribute('data-fill')).toBe('#111827');
    expect(rect.getAttribute('data-width')).toBe('1000');
    expect(rect.getAttribute('data-height')).toBe('500');
  });

  it('falls back to 1200x800 #ffffff when canvas is missing', async () => {
    const layout = baseLayout();
    delete (layout as unknown as Record<string, unknown>).canvas;
    render(<SeatPicker layout={layout} width={960} />);

    const rect = await screen.findByTestId('mock-rect');
    expect(rect.getAttribute('data-fill')).toBe('#ffffff');
    expect(rect.getAttribute('data-width')).toBe('1200');
    expect(rect.getAttribute('data-height')).toBe('800');
  });
});
