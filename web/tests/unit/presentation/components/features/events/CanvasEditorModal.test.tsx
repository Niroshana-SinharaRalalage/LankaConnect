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
import { act, render, screen, fireEvent, waitFor } from '@testing-library/react';
import type { BatchLayoutPayload } from '@/infrastructure/api/types/events.types';
import { ApiError } from '@/infrastructure/api/client/api-errors';

vi.mock('@/infrastructure/api/repositories/venue-layouts.repository', () => ({
  venueLayoutsRepository: {
    recordCanvasEditorOpened: vi.fn(() => Promise.resolve()),
    recordCanvasEditorSaved: vi.fn(() => Promise.resolve()),
  },
}));

// Slice 8 S8.8b: mock react-hot-toast so error / 409 paths are observable in
// tests without booting a real Toaster. The modal uses `toast.error` for
// both ApiError 409 and other failures.
const toastErrorMock = vi.fn();
const toastSuccessMock = vi.fn();
vi.mock('react-hot-toast', () => ({
  default: {
    error: (...args: unknown[]) => toastErrorMock(...args),
    success: (...args: unknown[]) => toastSuccessMock(...args),
  },
}));

// Slice 8 S8.8b: stub the React Query batch-update hook so individual tests
// can resolve / reject the mutation and verify the dispatched variables
// (rowVersion + composed payload) without spinning up a real client.
const mutateAsyncSpy = vi.fn();
let mutationIsPending = false;
const useBatchUpdateMock = vi.fn((..._args: unknown[]) => ({
  mutateAsync: mutateAsyncSpy,
  isPending: mutationIsPending,
}));
vi.mock('@/presentation/hooks/useVenueLayouts', () => ({
  useBatchUpdateVenueLayout: (...args: unknown[]) => useBatchUpdateMock(...args),
}));

// Slice 8 S8.8b: upgrade the CanvasEditor stub to expose draft-change
// triggers + a payload reporter so the modal's Save flow is testable.
// The real CanvasEditor calls onDraftChange after every history mutation;
// the stub exposes click targets that simulate that for the tests below.
const stubComposedPayload: BatchLayoutPayload = {
  name: null,
  canvas: null,
  zones: [],
  tables: [],
  decorations: [],
};

vi.mock('@/presentation/components/features/events/CanvasEditor', () => {
  return {
    CanvasEditor: ({
      layout,
      onDraftChange,
    }: {
      layout: { id: string };
      onDraftChange?: (summary: {
        hasChanges: boolean;
        changesCount: number;
        composeSavePayload: () => BatchLayoutPayload;
      }) => void;
    }) => {
      const markDirty = () =>
        onDraftChange?.({
          hasChanges: true,
          changesCount: 3,
          composeSavePayload: () => stubComposedPayload,
        });
      const markClean = () =>
        onDraftChange?.({
          hasChanges: false,
          changesCount: 0,
          composeSavePayload: () => stubComposedPayload,
        });
      return React.createElement(
        'div',
        { 'data-testid': 'canvas-editor-stub', 'data-layout-id': layout.id },
        [
          React.createElement(
            'button',
            { key: 'd', 'data-testid': 'stub-mark-dirty', onClick: markDirty, type: 'button' },
            'mark-dirty',
          ),
          React.createElement(
            'button',
            { key: 'c', 'data-testid': 'stub-mark-clean', onClick: markClean, type: 'button' },
            'mark-clean',
          ),
        ],
      );
    },
  };
});

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
  mutationIsPending = false;
  // Default: mutation resolves successfully — individual tests override.
  mutateAsyncSpy.mockResolvedValue(undefined);
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

// ──────────────────────────── S8.8b: Save flow ────────────────────────────

describe('CanvasEditorModal — Save flow (S8.8b)', () => {
  it('renders a Save button in the footer when the modal is open', () => {
    render(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);
    expect(screen.getByTestId('canvas-editor-save')).toBeInTheDocument();
  });

  it('disables Save by default (no draft changes yet)', () => {
    render(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);
    expect(screen.getByTestId('canvas-editor-save')).toBeDisabled();
  });

  it('enables Save once the editor reports hasChanges=true', () => {
    render(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('stub-mark-dirty'));
    expect(screen.getByTestId('canvas-editor-save')).not.toBeDisabled();
  });

  it('disables Save again when the editor reports hasChanges=false (full undo)', () => {
    render(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('stub-mark-dirty'));
    expect(screen.getByTestId('canvas-editor-save')).not.toBeDisabled();
    fireEvent.click(screen.getByTestId('stub-mark-clean'));
    expect(screen.getByTestId('canvas-editor-save')).toBeDisabled();
  });

  it('calls useBatchUpdateVenueLayout with layoutId + eventId on mount', () => {
    render(
      <CanvasEditorModal
        open
        onOpenChange={vi.fn()}
        layout={fakeLayout({ id: 'L1', eventId: 'E1' })}
      />,
    );
    expect(useBatchUpdateMock).toHaveBeenCalledWith('L1', 'E1');
  });

  it('dispatches mutateAsync with the rowVersion + composed payload on Save click', async () => {
    render(
      <CanvasEditorModal
        open
        onOpenChange={vi.fn()}
        layout={fakeLayout({ rowVersion: 17 })}
      />,
    );
    fireEvent.click(screen.getByTestId('stub-mark-dirty'));
    await act(async () => {
      fireEvent.click(screen.getByTestId('canvas-editor-save'));
    });
    expect(mutateAsyncSpy).toHaveBeenCalledTimes(1);
    expect(mutateAsyncSpy).toHaveBeenCalledWith({
      rowVersion: 17,
      payload: stubComposedPayload,
    });
  });

  it('closes the modal on successful save', async () => {
    const onOpenChange = vi.fn();
    render(<CanvasEditorModal open onOpenChange={onOpenChange} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('stub-mark-dirty'));
    await act(async () => {
      fireEvent.click(screen.getByTestId('canvas-editor-save'));
    });
    await waitFor(() => expect(onOpenChange).toHaveBeenCalledWith(false));
  });

  it('invokes onLayoutSaved on successful save', async () => {
    const onLayoutSaved = vi.fn();
    render(
      <CanvasEditorModal
        open
        onOpenChange={vi.fn()}
        layout={fakeLayout()}
        onLayoutSaved={onLayoutSaved}
      />,
    );
    fireEvent.click(screen.getByTestId('stub-mark-dirty'));
    await act(async () => {
      fireEvent.click(screen.getByTestId('canvas-editor-save'));
    });
    await waitFor(() => expect(onLayoutSaved).toHaveBeenCalledTimes(1));
  });

  it('shows a 409-specific toast and keeps modal open on Conflict response', async () => {
    mutateAsyncSpy.mockRejectedValueOnce(new ApiError('Layout was modified by someone else.', 409));
    const onOpenChange = vi.fn();
    render(<CanvasEditorModal open onOpenChange={onOpenChange} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('stub-mark-dirty'));
    await act(async () => {
      fireEvent.click(screen.getByTestId('canvas-editor-save'));
    });
    await waitFor(() => expect(toastErrorMock).toHaveBeenCalledTimes(1));
    expect(toastErrorMock.mock.calls[0][0]).toMatch(/modified|reload/i);
    expect(onOpenChange).not.toHaveBeenCalledWith(false);
  });

  it('shows a generic toast and keeps modal open on other errors', async () => {
    mutateAsyncSpy.mockRejectedValueOnce(new ApiError('Internal Server Error', 500));
    const onOpenChange = vi.fn();
    render(<CanvasEditorModal open onOpenChange={onOpenChange} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('stub-mark-dirty'));
    await act(async () => {
      fireEvent.click(screen.getByTestId('canvas-editor-save'));
    });
    await waitFor(() => expect(toastErrorMock).toHaveBeenCalledTimes(1));
    expect(onOpenChange).not.toHaveBeenCalledWith(false);
  });

  it('does not fire venueLayoutsRepository.recordCanvasEditorSaved (backend handler is canonical emitter)', async () => {
    const { venueLayoutsRepository } = await import(
      '@/infrastructure/api/repositories/venue-layouts.repository'
    );
    render(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('stub-mark-dirty'));
    await act(async () => {
      fireEvent.click(screen.getByTestId('canvas-editor-save'));
    });
    expect(venueLayoutsRepository.recordCanvasEditorSaved).not.toHaveBeenCalled();
  });

  it('disables Save while a mutation is in flight (isPending)', () => {
    mutationIsPending = true;
    render(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('stub-mark-dirty'));
    expect(screen.getByTestId('canvas-editor-save')).toBeDisabled();
    expect(screen.getByTestId('canvas-editor-save')).toHaveTextContent(/saving/i);
  });
});
