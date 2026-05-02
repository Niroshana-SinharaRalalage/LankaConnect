/**
 * Slice S3 — CanvasEditorTitleEditor tests.
 *
 * Inline editable layout name in the canvas-editor header. Commits via the
 * existing `PUT /api/venue-layouts/{id}` (UpdateLayoutCommand with `name`
 * only) — Slice 5 Chunk 4 already covered the endpoint, so S3 reuses it
 * rather than introducing a redundant `PATCH /name` surface.
 *
 * Architect requirement (Slice S3): "Commits via dedicated PATCH (not via
 * batch-update — naming is independent of structural edits and shouldn't
 * share a concurrency token)." The existing PUT endpoint satisfies the
 * spirit (own If-Match handling, separate from /batch). Documented in
 * docs/MASTER_TODO_SEATING_MVP.md S3 run history.
 */

import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { CanvasEditorTitleEditor } from '../CanvasEditorTitleEditor';
import { ApiError } from '@/infrastructure/api/client/api-errors';

const updateLayoutMock = vi.fn();
vi.mock('@/infrastructure/api/repositories/venue-layouts.repository', () => ({
  venueLayoutsRepository: {
    updateLayout: (...args: unknown[]) => updateLayoutMock(...args),
  },
}));

vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

function renderEditor(
  overrides: Partial<React.ComponentProps<typeof CanvasEditorTitleEditor>> = {},
) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const props: React.ComponentProps<typeof CanvasEditorTitleEditor> = {
    layoutId: 'layout-1',
    eventId: 'event-1',
    currentName: 'Theater Classic',
    rowVersion: 12345,
    ...overrides,
  };
  return {
    ...render(
      <QueryClientProvider client={client}>
        <CanvasEditorTitleEditor {...props} />
      </QueryClientProvider>,
    ),
    client,
  };
}

beforeEach(() => {
  updateLayoutMock.mockReset();
  vi.mocked(toast.success).mockReset();
  vi.mocked(toast.error).mockReset();
});

describe('CanvasEditorTitleEditor — Slice S3', () => {
  it('renders the current layout name in an editable input', () => {
    renderEditor();
    const input = screen.getByTestId(
      'canvas-editor-layout-name-input',
    ) as HTMLInputElement;
    expect(input.value).toBe('Theater Classic');
  });

  it('commits the new name on Enter via PUT /api/venue-layouts/{id} with the row version', async () => {
    updateLayoutMock.mockResolvedValueOnce(undefined);
    renderEditor();
    const input = screen.getByTestId(
      'canvas-editor-layout-name-input',
    ) as HTMLInputElement;

    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'My Custom Theater' } });
    fireEvent.keyDown(input, { key: 'Enter' });

    await waitFor(() => {
      expect(updateLayoutMock).toHaveBeenCalledWith(
        'layout-1',
        12345,
        { name: 'My Custom Theater' },
      );
    });
    expect(vi.mocked(toast.success)).toHaveBeenCalledWith('Layout renamed');
  });

  it('commits the new name on blur via PUT', async () => {
    updateLayoutMock.mockResolvedValueOnce(undefined);
    renderEditor();
    const input = screen.getByTestId(
      'canvas-editor-layout-name-input',
    ) as HTMLInputElement;

    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'Renamed' } });
    fireEvent.blur(input);

    await waitFor(() => {
      expect(updateLayoutMock).toHaveBeenCalledWith(
        'layout-1',
        12345,
        { name: 'Renamed' },
      );
    });
  });

  it('does not commit when the name is unchanged', async () => {
    renderEditor();
    const input = screen.getByTestId(
      'canvas-editor-layout-name-input',
    ) as HTMLInputElement;
    fireEvent.focus(input);
    fireEvent.blur(input);
    await new Promise((r) => setTimeout(r, 0));
    expect(updateLayoutMock).not.toHaveBeenCalled();
  });

  it('reverts on Escape and does not commit', async () => {
    renderEditor();
    const input = screen.getByTestId(
      'canvas-editor-layout-name-input',
    ) as HTMLInputElement;

    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'Aborted Edit' } });
    fireEvent.keyDown(input, { key: 'Escape' });
    fireEvent.blur(input);

    await new Promise((r) => setTimeout(r, 0));
    expect(updateLayoutMock).not.toHaveBeenCalled();
    expect(input.value).toBe('Theater Classic');
  });

  it('reverts to the current name and toasts on empty submission', async () => {
    renderEditor();
    const input = screen.getByTestId(
      'canvas-editor-layout-name-input',
    ) as HTMLInputElement;

    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: '   ' } });
    fireEvent.blur(input);

    await waitFor(() => {
      expect(vi.mocked(toast.error)).toHaveBeenCalledWith(
        'Layout name is required',
      );
    });
    expect(updateLayoutMock).not.toHaveBeenCalled();
    expect(input.value).toBe('Theater Classic');
  });

  it('toasts the architect-prescribed 409 message and reverts when PUT returns Conflict', async () => {
    updateLayoutMock.mockRejectedValueOnce(
      new ApiError('Layout was modified by someone else.', 409),
    );
    renderEditor();
    const input = screen.getByTestId(
      'canvas-editor-layout-name-input',
    ) as HTMLInputElement;

    fireEvent.focus(input);
    fireEvent.change(input, { target: { value: 'Stale Edit' } });
    fireEvent.blur(input);

    await waitFor(() => {
      expect(vi.mocked(toast.error)).toHaveBeenCalledWith(
        expect.stringContaining('modified externally'),
      );
    });
    expect(input.value).toBe('Theater Classic');
  });

  it('caps the input at 200 characters via maxLength', () => {
    renderEditor();
    const input = screen.getByTestId(
      'canvas-editor-layout-name-input',
    ) as HTMLInputElement;
    expect(input.maxLength).toBe(200);
  });

  it('disables editing while the parent flags a structural save in flight', () => {
    renderEditor({ disabled: true });
    const input = screen.getByTestId(
      'canvas-editor-layout-name-input',
    ) as HTMLInputElement;
    expect(input).toBeDisabled();
  });

  it('syncs to the new currentName when the layout refetches and the field is not focused', () => {
    const { rerender, client } = renderEditor();
    const input = screen.getByTestId(
      'canvas-editor-layout-name-input',
    ) as HTMLInputElement;
    expect(input.value).toBe('Theater Classic');

    rerender(
      <QueryClientProvider client={client}>
        <CanvasEditorTitleEditor
          layoutId="layout-1"
          eventId="event-1"
          currentName="Theater With Balcony"
          rowVersion={12346}
        />
      </QueryClientProvider>,
    );
    expect(input.value).toBe('Theater With Balcony');
  });
});
