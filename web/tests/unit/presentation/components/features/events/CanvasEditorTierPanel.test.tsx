/**
 * Slice 8 S8.7 — CanvasEditorTierPanel tests.
 *
 * Purely presentational — the panel never calls the API itself. It renders
 * the tiers + assigned set it is given and dispatches onToggleTier on
 * checkbox change. Empty / loading / template states get dedicated
 * exhibits.
 */

import React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';

import { CanvasEditorTierPanel } from '@/presentation/components/features/events/CanvasEditorTierPanel';
import type { TicketTierDto } from '@/infrastructure/api/types/events.types';

function tier(id: string, name: string): TicketTierDto {
  return {
    id,
    name,
    adultPriceAmount: 0,
    adultPriceCurrency: 'USD' as unknown as TicketTierDto['adultPriceCurrency'],
    hasChildPricing: false,
    capacity: 100,
    reservedCount: 0,
    availableQuantity: 100,
    maxPerUser: 10,
    sortOrder: 0,
    isActive: true,
  } as TicketTierDto;
}

describe('CanvasEditorTierPanel', () => {
  it('renders the section heading with screen-reader-friendly description', () => {
    render(
      <CanvasEditorTierPanel
        tiers={[tier('t1', 'VIP')]}
        assignedTierIds={[]}
        onToggleTier={vi.fn()}
      />,
    );
    expect(
      screen.getByRole('region', { name: /ticket tier mapping/i }),
    ).toBeInTheDocument();
  });

  it('shows a template hint when isTemplateLayout=true and hides the list', () => {
    render(
      <CanvasEditorTierPanel
        tiers={[tier('t1', 'VIP')]}
        assignedTierIds={[]}
        onToggleTier={vi.fn()}
        isTemplateLayout
      />,
    );
    expect(screen.getByTestId('tier-panel-template-hint')).toBeInTheDocument();
    expect(screen.queryByTestId('tier-panel-list')).toBeNull();
  });

  it('shows a loading placeholder while tiersLoading=true', () => {
    render(
      <CanvasEditorTierPanel
        tiers={[]}
        assignedTierIds={[]}
        onToggleTier={vi.fn()}
        tiersLoading
      />,
    );
    expect(screen.getByTestId('tier-panel-loading')).toBeInTheDocument();
    expect(screen.queryByTestId('tier-panel-list')).toBeNull();
  });

  it('shows an empty-state hint when no tiers exist', () => {
    render(
      <CanvasEditorTierPanel
        tiers={[]}
        assignedTierIds={[]}
        onToggleTier={vi.fn()}
      />,
    );
    expect(screen.getByTestId('tier-panel-empty')).toBeInTheDocument();
  });

  it('renders a checkbox per tier with assigned tiers pre-checked', () => {
    render(
      <CanvasEditorTierPanel
        tiers={[tier('t1', 'VIP'), tier('t2', 'Plus'), tier('t3', 'Basic')]}
        assignedTierIds={['t1', 't3']}
        onToggleTier={vi.fn()}
      />,
    );
    expect(screen.getByTestId('tier-panel-checkbox-t1')).toBeChecked();
    expect(screen.getByTestId('tier-panel-checkbox-t2')).not.toBeChecked();
    expect(screen.getByTestId('tier-panel-checkbox-t3')).toBeChecked();
  });

  it('fires onToggleTier with the tierId when a checkbox is clicked', () => {
    const onToggleTier = vi.fn();
    render(
      <CanvasEditorTierPanel
        tiers={[tier('t1', 'VIP'), tier('t2', 'Plus')]}
        assignedTierIds={['t1']}
        onToggleTier={onToggleTier}
      />,
    );
    fireEvent.click(screen.getByTestId('tier-panel-checkbox-t2'));
    expect(onToggleTier).toHaveBeenCalledWith('t2');
    fireEvent.click(screen.getByTestId('tier-panel-checkbox-t1'));
    expect(onToggleTier).toHaveBeenCalledWith('t1');
  });
});
