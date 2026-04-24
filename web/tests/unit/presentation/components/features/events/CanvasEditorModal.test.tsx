/**
 * Slice 8 S8.1 — CanvasEditorModal shell tests.
 *
 * Focus: shell renders when open, fires the `layout.canvas_editor_opened`
 * metric exactly once per open, respects close control, does not emit when
 * closed. The canvas stage itself is a later-chunk concern — this file
 * guards the shell contract so those chunks can replace the placeholder
 * without regressing the metric or the open/close wiring.
 */

import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';

vi.mock('@/infrastructure/api/repositories/venue-layouts.repository', () => ({
  venueLayoutsRepository: {
    recordCanvasEditorOpened: vi.fn(() => Promise.resolve()),
    recordCanvasEditorSaved: vi.fn(() => Promise.resolve()),
  },
}));

// The CanvasEditor wrapper dynamically imports Konva which touches `window` at
// import time; stub it in tests so we can assert the modal wiring without
// booting the canvas stage. Child components cover their own concerns in
// CanvasEditorStage-focused tests.
vi.mock('@/presentation/components/features/events/CanvasEditor', () => ({
  CanvasEditor: ({ layout }: { layout: { id: string } }) =>
    React.createElement(
      'div',
      { 'data-testid': 'canvas-editor-stub', 'data-layout-id': layout.id },
      'canvas-stub',
    ),
}));

import { CanvasEditorModal } from '@/presentation/components/features/events/CanvasEditorModal';
import { venueLayoutsRepository } from '@/infrastructure/api/repositories/venue-layouts.repository';
import type { VenueLayoutDto } from '@/infrastructure/api/types/events.types';

const recordOpenedMock = venueLayoutsRepository.recordCanvasEditorOpened as unknown as ReturnType<
  typeof vi.fn
>;

function fakeLayout(overrides: Partial<VenueLayoutDto> = {}): VenueLayoutDto {
  return {
    id: 'layout-id-1',
    name: 'Theater Classic',
    layoutType: 'Theater',
    totalCapacity: 200,
    isTemplate: false,
    rowVersion: 1,
    zones: [],
    tables: [],
    decorations: [],
    ...overrides,
  } as VenueLayoutDto;
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('CanvasEditorModal', () => {
  it('renders nothing when closed', () => {
    render(
      <CanvasEditorModal open={false} onOpenChange={vi.fn()} layout={fakeLayout()} />,
    );
    expect(screen.queryByTestId('canvas-editor-modal')).toBeNull();
  });

  it('renders modal content with layout summary when open', () => {
    render(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);

    expect(screen.getByTestId('canvas-editor-modal')).toBeInTheDocument();
    expect(screen.getByText(/Customize layout — Theater Classic/)).toBeInTheDocument();
    expect(screen.getByText(/Theater · 200 seats · 0 zones/)).toBeInTheDocument();
    expect(screen.getByTestId('canvas-editor-body')).toBeInTheDocument();
    // Stage is stubbed in this test file; CanvasEditorStage has its own tests.
    expect(screen.getByTestId('canvas-editor-stub')).toBeInTheDocument();
    expect(screen.getByTestId('canvas-editor-stub')).toHaveAttribute(
      'data-layout-id',
      'layout-id-1',
    );
  });

  it('fires layout.canvas_editor_opened metric once when opened', () => {
    render(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);

    expect(recordOpenedMock).toHaveBeenCalledTimes(1);
    expect(recordOpenedMock).toHaveBeenCalledWith('layout-id-1');
  });

  it('does not fire metric when opened with the same layout after re-render', () => {
    const { rerender } = render(
      <CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />,
    );
    expect(recordOpenedMock).toHaveBeenCalledTimes(1);

    // No prop changes — re-render should not re-emit.
    rerender(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);
    expect(recordOpenedMock).toHaveBeenCalledTimes(1);
  });

  it('fires metric again when modal is closed and re-opened', () => {
    const { rerender } = render(
      <CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />,
    );
    expect(recordOpenedMock).toHaveBeenCalledTimes(1);

    rerender(<CanvasEditorModal open={false} onOpenChange={vi.fn()} layout={fakeLayout()} />);
    rerender(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);

    expect(recordOpenedMock).toHaveBeenCalledTimes(2);
  });

  it('invokes onOpenChange(false) when the close button is clicked', () => {
    const onOpenChange = vi.fn();
    render(<CanvasEditorModal open onOpenChange={onOpenChange} layout={fakeLayout()} />);

    fireEvent.click(screen.getByTestId('canvas-editor-close'));
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });

  it('invokes onOpenChange(false) when the footer Close button is clicked', () => {
    const onOpenChange = vi.fn();
    render(<CanvasEditorModal open onOpenChange={onOpenChange} layout={fakeLayout()} />);

    fireEvent.click(screen.getByTestId('canvas-editor-cancel'));
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });

  it('does not fire metric when initially closed', () => {
    render(
      <CanvasEditorModal open={false} onOpenChange={vi.fn()} layout={fakeLayout()} />,
    );
    expect(recordOpenedMock).not.toHaveBeenCalled();
  });
});
