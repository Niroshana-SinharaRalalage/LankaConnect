/**
 * Slice 8 S8.5b — CanvasEditorToolbar tests.
 *
 * Pure presentation component — callbacks, disabled state, and the
 * decoration-kind select. No canvas / Konva / network concerns.
 */

import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';

import { CanvasEditorToolbar } from '@/presentation/components/features/events/CanvasEditorToolbar';
import { DecorationKind } from '@/infrastructure/api/types/events.types';

function mount(overrides: Partial<Parameters<typeof CanvasEditorToolbar>[0]> = {}) {
  const props = {
    onAddZone: vi.fn(),
    onAddRoundTable: vi.fn(),
    onAddRectTable: vi.fn(),
    onAddDecoration: vi.fn(),
    onDeleteSelected: vi.fn(),
    canDelete: false,
    ...overrides,
  };
  render(<CanvasEditorToolbar {...props} />);
  return props;
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('CanvasEditorToolbar', () => {
  it('renders all add buttons plus decoration kind select + delete', () => {
    mount();
    expect(screen.getByTestId('canvas-editor-toolbar')).toBeInTheDocument();
    expect(screen.getByTestId('toolbar-add-zone')).toBeInTheDocument();
    expect(screen.getByTestId('toolbar-add-round-table')).toBeInTheDocument();
    expect(screen.getByTestId('toolbar-add-rect-table')).toBeInTheDocument();
    expect(screen.getByTestId('toolbar-decoration-kind')).toBeInTheDocument();
    expect(screen.getByTestId('toolbar-add-decoration')).toBeInTheDocument();
    expect(screen.getByTestId('toolbar-delete')).toBeInTheDocument();
  });

  it('fires onAddZone when the Zone button is clicked', () => {
    const { onAddZone } = mount();
    fireEvent.click(screen.getByTestId('toolbar-add-zone'));
    expect(onAddZone).toHaveBeenCalledTimes(1);
  });

  it('fires onAddRoundTable / onAddRectTable for their buttons', () => {
    const { onAddRoundTable, onAddRectTable } = mount();
    fireEvent.click(screen.getByTestId('toolbar-add-round-table'));
    fireEvent.click(screen.getByTestId('toolbar-add-rect-table'));
    expect(onAddRoundTable).toHaveBeenCalledTimes(1);
    expect(onAddRectTable).toHaveBeenCalledTimes(1);
  });

  it('fires onAddDecoration with the currently-selected kind from the dropdown', () => {
    const { onAddDecoration } = mount();
    const select = screen.getByTestId('toolbar-decoration-kind');
    fireEvent.change(select, { target: { value: DecorationKind.Aisle } });
    fireEvent.click(screen.getByTestId('toolbar-add-decoration'));
    expect(onAddDecoration).toHaveBeenCalledWith(DecorationKind.Aisle);
  });

  it('defaults to DecorationKind.Stage on first add', () => {
    const { onAddDecoration } = mount();
    fireEvent.click(screen.getByTestId('toolbar-add-decoration'));
    expect(onAddDecoration).toHaveBeenCalledWith(DecorationKind.Stage);
  });

  it('Delete is disabled when canDelete=false', () => {
    mount({ canDelete: false });
    expect(screen.getByTestId('toolbar-delete')).toBeDisabled();
  });

  it('Delete fires onDeleteSelected when canDelete=true', () => {
    const { onDeleteSelected } = mount({ canDelete: true });
    const btn = screen.getByTestId('toolbar-delete');
    expect(btn).not.toBeDisabled();
    fireEvent.click(btn);
    expect(onDeleteSelected).toHaveBeenCalledTimes(1);
  });

  it('renders all 7 decoration kinds in the select', () => {
    mount();
    const options = screen
      .getByTestId('toolbar-decoration-kind')
      .querySelectorAll('option');
    const values = Array.from(options).map((o) => o.value);
    expect(values).toEqual([
      'Stage',
      'DanceFloor',
      'Aisle',
      'Door',
      'Wall',
      'Text',
      'Image',
    ]);
  });

  it('has role="toolbar" for screen readers', () => {
    mount();
    expect(screen.getByRole('toolbar', { name: /canvas editor toolbar/i })).toBeInTheDocument();
  });
});
