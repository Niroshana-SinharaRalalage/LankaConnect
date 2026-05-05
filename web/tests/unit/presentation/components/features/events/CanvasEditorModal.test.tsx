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

// Slice 8 S8.9b: stub the save-as-template mutation so the test can
// resolve / reject and verify dispatched variables (sourceLayoutId +
// templateName) without spinning up a real client.
const saveAsTemplateMutateAsyncSpy = vi.fn();
let saveAsTemplateIsPending = false;
const useSaveAsTemplateMock = vi.fn(() => ({
  mutateAsync: saveAsTemplateMutateAsyncSpy,
  isPending: saveAsTemplateIsPending,
}));

vi.mock('@/presentation/hooks/useVenueLayouts', () => ({
  useBatchUpdateVenueLayout: (...args: unknown[]) => useBatchUpdateMock(...args),
  useSaveLayoutAsTemplate: () => useSaveAsTemplateMock(),
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
  saveAsTemplateIsPending = false;
  // Default: mutations resolve successfully — individual tests override.
  mutateAsyncSpy.mockResolvedValue(undefined);
  saveAsTemplateMutateAsyncSpy.mockResolvedValue({
    id: 'new-template-id',
    name: 'Theater Classic (Template)',
    isTemplate: true,
    eventId: null,
  });
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

// ──────────────────────── S8.9a: warn-before-close ────────────────────────

describe('CanvasEditorModal — warn before close (S8.9a)', () => {
  it('closes immediately when X clicked and no draft changes', () => {
    const onOpenChange = vi.fn();
    render(<CanvasEditorModal open onOpenChange={onOpenChange} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('canvas-editor-close'));
    expect(onOpenChange).toHaveBeenCalledWith(false);
    // No discard prompt when clean.
    expect(screen.queryByTestId('canvas-editor-discard-confirm')).toBeNull();
  });

  it('closes immediately when footer Close clicked and no draft changes', () => {
    const onOpenChange = vi.fn();
    render(<CanvasEditorModal open onOpenChange={onOpenChange} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('canvas-editor-cancel'));
    expect(onOpenChange).toHaveBeenCalledWith(false);
    expect(screen.queryByTestId('canvas-editor-discard-confirm')).toBeNull();
  });

  it('shows discard confirm when X clicked with unsaved changes', () => {
    const onOpenChange = vi.fn();
    render(<CanvasEditorModal open onOpenChange={onOpenChange} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('stub-mark-dirty'));
    fireEvent.click(screen.getByTestId('canvas-editor-close'));
    // Modal does NOT close yet — confirm dialog appears instead.
    expect(onOpenChange).not.toHaveBeenCalledWith(false);
    expect(screen.getByText(/discard unsaved changes/i)).toBeInTheDocument();
  });

  it('shows discard confirm when footer Close clicked with unsaved changes', () => {
    const onOpenChange = vi.fn();
    render(<CanvasEditorModal open onOpenChange={onOpenChange} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('stub-mark-dirty'));
    fireEvent.click(screen.getByTestId('canvas-editor-cancel'));
    expect(onOpenChange).not.toHaveBeenCalledWith(false);
    expect(screen.getByText(/discard unsaved changes/i)).toBeInTheDocument();
  });

  it('shows discard confirm when Radix Dialog backdrop / Esc fires onOpenChange(false) with unsaved changes', () => {
    // Radix's Dialog calls our onOpenChange handler with `false` for backdrop
    // click and Esc — the modal must intercept that path too. Test the
    // intercept by triggering the Dialog's primitive close path, which we
    // route through `canvas-editor-close` (the same handler).
    const onOpenChange = vi.fn();
    render(<CanvasEditorModal open onOpenChange={onOpenChange} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('stub-mark-dirty'));
    // Press Escape on the dialog — Radix's onOpenChange(false) should be guarded.
    fireEvent.keyDown(screen.getByTestId('canvas-editor-modal'), {
      key: 'Escape',
      code: 'Escape',
    });
    // Modal still open (parent never told to close); discard prompt visible.
    expect(onOpenChange).not.toHaveBeenCalledWith(false);
    expect(screen.getByText(/discard unsaved changes/i)).toBeInTheDocument();
  });

  it('confirming discard closes the modal', async () => {
    const onOpenChange = vi.fn();
    render(<CanvasEditorModal open onOpenChange={onOpenChange} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('stub-mark-dirty'));
    fireEvent.click(screen.getByTestId('canvas-editor-close'));
    // Discard button labeled (per ConfirmDialog confirmLabel below). The
    // ConfirmDialog wraps onConfirm in an internal async/finally that
    // toggles isPending, so we wait for those state updates to flush.
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: /discard/i }));
    });
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });

  it('canceling discard keeps the modal open and dismisses the prompt', async () => {
    const onOpenChange = vi.fn();
    render(<CanvasEditorModal open onOpenChange={onOpenChange} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('stub-mark-dirty'));
    fireEvent.click(screen.getByTestId('canvas-editor-close'));
    // Keep editing button — uses the ConfirmDialog cancel slot.
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: /keep editing/i }));
    });
    expect(onOpenChange).not.toHaveBeenCalledWith(false);
    // Prompt is gone.
    expect(screen.queryByText(/discard unsaved changes/i)).toBeNull();
  });

  it('does not show discard prompt on the post-Save-as-Template path', async () => {
    // Save-as-Template doesn't change the editor's draft, so the dirty flag
    // stays true — but the action's success shouldn't pop the discard prompt
    // because we don't close the modal on save-as-template (user keeps editing).
    const onOpenChange = vi.fn();
    render(<CanvasEditorModal open onOpenChange={onOpenChange} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('stub-mark-dirty'));
    fireEvent.click(screen.getByTestId('canvas-editor-save-as-template'));
    await act(async () => {
      fireEvent.click(screen.getByTestId('save-as-template-confirm'));
    });
    // The save-as-template flow keeps the modal open + does NOT trigger
    // the discard guard.
    expect(onOpenChange).not.toHaveBeenCalledWith(false);
    expect(screen.queryByText(/discard unsaved changes/i)).toBeNull();
  });

  it('does not show discard prompt on the post-Save close path', async () => {
    // After a successful save, the modal closes itself — no dirty state at
    // that point (mutateAsync resolved → onLayoutSaved + onOpenChange(false)
    // run together). The discard guard must not trip on this path.
    const onOpenChange = vi.fn();
    render(<CanvasEditorModal open onOpenChange={onOpenChange} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('stub-mark-dirty'));
    await act(async () => {
      fireEvent.click(screen.getByTestId('canvas-editor-save'));
    });
    await waitFor(() => expect(onOpenChange).toHaveBeenCalledWith(false));
    expect(screen.queryByText(/discard unsaved changes/i)).toBeNull();
  });
});

// ─────────────────────── S8.9b: Save-as-Template flow ───────────────────────

describe('CanvasEditorModal — Save as personal template (S8.9b)', () => {
  it('renders a Save-as-Template button in the footer', () => {
    render(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);
    expect(screen.getByTestId('canvas-editor-save-as-template')).toBeInTheDocument();
  });

  it('Save-as-Template button is enabled even with no draft changes', () => {
    // User can save the current persisted state as a template at any time —
    // unlike Save (atomic batch), there's no "must have changes" gate.
    render(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);
    expect(screen.getByTestId('canvas-editor-save-as-template')).not.toBeDisabled();
  });

  it('opens a name-prompt dialog with default "<source.name> (Template)" on click', () => {
    render(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('canvas-editor-save-as-template'));
    const input = screen.getByTestId('save-as-template-name-input') as HTMLInputElement;
    expect(input).toBeInTheDocument();
    expect(input.value).toBe('Theater Classic (Template)');
  });

  it('confirm submits useSaveLayoutAsTemplate with the source layout id + entered name', async () => {
    render(
      <CanvasEditorModal
        open
        onOpenChange={vi.fn()}
        layout={fakeLayout({ id: 'source-L1', name: 'Source Hall' })}
      />,
    );
    fireEvent.click(screen.getByTestId('canvas-editor-save-as-template'));
    const input = screen.getByTestId('save-as-template-name-input') as HTMLInputElement;
    fireEvent.change(input, { target: { value: 'My Renamed Template' } });
    await act(async () => {
      fireEvent.click(screen.getByTestId('save-as-template-confirm'));
    });
    expect(saveAsTemplateMutateAsyncSpy).toHaveBeenCalledTimes(1);
    expect(saveAsTemplateMutateAsyncSpy).toHaveBeenCalledWith({
      sourceLayoutId: 'source-L1',
      templateName: 'My Renamed Template',
    });
  });

  it('shows a success toast and closes the prompt on successful save', async () => {
    render(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('canvas-editor-save-as-template'));
    await act(async () => {
      fireEvent.click(screen.getByTestId('save-as-template-confirm'));
    });
    await waitFor(() => expect(toastSuccessMock).toHaveBeenCalledTimes(1));
    expect(toastSuccessMock.mock.calls[0][0]).toMatch(/template saved|saved as template/i);
    // Prompt is gone.
    expect(screen.queryByTestId('save-as-template-name-input')).toBeNull();
  });

  it('keeps the editor modal open on success (only the prompt closes)', async () => {
    const onOpenChange = vi.fn();
    render(<CanvasEditorModal open onOpenChange={onOpenChange} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('canvas-editor-save-as-template'));
    await act(async () => {
      fireEvent.click(screen.getByTestId('save-as-template-confirm'));
    });
    await waitFor(() => expect(toastSuccessMock).toHaveBeenCalled());
    expect(onOpenChange).not.toHaveBeenCalledWith(false);
  });

  it('shows a 403-specific error toast on Forbidden response', async () => {
    saveAsTemplateMutateAsyncSpy.mockRejectedValueOnce(new ApiError('Forbidden', 403));
    render(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('canvas-editor-save-as-template'));
    await act(async () => {
      fireEvent.click(screen.getByTestId('save-as-template-confirm'));
    });
    await waitFor(() => expect(toastErrorMock).toHaveBeenCalledTimes(1));
    expect(toastErrorMock.mock.calls[0][0]).toMatch(/permission|allowed|forbidden/i);
  });

  it('shows a generic error toast on other errors', async () => {
    saveAsTemplateMutateAsyncSpy.mockRejectedValueOnce(new ApiError('Internal Server Error', 500));
    render(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('canvas-editor-save-as-template'));
    await act(async () => {
      fireEvent.click(screen.getByTestId('save-as-template-confirm'));
    });
    await waitFor(() => expect(toastErrorMock).toHaveBeenCalledTimes(1));
  });

  it('cancel on the prompt does not fire the mutation', () => {
    render(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('canvas-editor-save-as-template'));
    fireEvent.click(screen.getByTestId('save-as-template-cancel'));
    expect(saveAsTemplateMutateAsyncSpy).not.toHaveBeenCalled();
    expect(screen.queryByTestId('save-as-template-name-input')).toBeNull();
  });

  it('disables Confirm button when the name input is empty / whitespace', () => {
    render(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);
    fireEvent.click(screen.getByTestId('canvas-editor-save-as-template'));
    const input = screen.getByTestId('save-as-template-name-input');
    fireEvent.change(input, { target: { value: '   ' } });
    expect(screen.getByTestId('save-as-template-confirm')).toBeDisabled();
  });

  it('shows "Saving..." on confirm button while mutation is pending', () => {
    // Open the prompt first (with isPending=false so the trigger button
    // isn't disabled), then flip the pending flag and re-render to mirror
    // what happens after the user clicks Save Template — mid-flight the
    // confirm button must reflect the pending state.
    const { rerender } = render(
      <CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />,
    );
    fireEvent.click(screen.getByTestId('canvas-editor-save-as-template'));
    expect(screen.getByTestId('save-as-template-confirm')).not.toBeDisabled();

    saveAsTemplateIsPending = true;
    rerender(<CanvasEditorModal open onOpenChange={vi.fn()} layout={fakeLayout()} />);
    const confirmBtn = screen.getByTestId('save-as-template-confirm');
    expect(confirmBtn).toBeDisabled();
    expect(confirmBtn).toHaveTextContent(/saving/i);
  });
});
