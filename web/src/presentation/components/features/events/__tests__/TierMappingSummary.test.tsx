/**
 * Slice S4 — TierMappingSummary tests.
 *
 * Hook-level data is mocked through the venue-layouts repository to keep the
 * test focused on rendering branches:
 *   - loading state
 *   - error state
 *   - happy-path (no blockers, no warnings, fully mapped tier)
 *   - blockers + warnings render with correct counts and copy
 *   - over-capacity row gets the danger styling
 *   - unmapped tier renders "unmapped" placeholder
 *   - "no active tiers yet" empty state
 */

import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { TierMappingSummary } from '../TierMappingSummary';
import type { PublishReadinessReportDto } from '@/infrastructure/api/types/events.types';

const getReadinessMock = vi.fn();
vi.mock('@/infrastructure/api/repositories/venue-layouts.repository', () => ({
  venueLayoutsRepository: {
    getLayoutPublishReadiness: (...args: unknown[]) => getReadinessMock(...args),
  },
}));

function renderSummary(layoutId = 'layout-1', compact = false) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <TierMappingSummary layoutId={layoutId} compact={compact} />
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  getReadinessMock.mockReset();
});

const happyReport: PublishReadinessReportDto = {
  isPublishReady: true,
  blockers: [],
  warnings: [],
  tierSummary: [
    {
      tierId: 'tier-1',
      tierName: 'VIP',
      tierCapacity: 30,
      mappedZones: [
        { id: 'zone-a', name: 'Front Row', enabledSeatCount: 20 },
      ],
      mappedTables: [],
      totalEnabledSeats: 20,
    },
  ],
};

const reportWithIssues: PublishReadinessReportDto = {
  isPublishReady: false,
  blockers: [
    {
      code: 'ZoneUnmapped',
      message: "Zone 'Main Floor' has 200 seats but no tier mapping.",
      shapeId: 'zone-1',
      shapeName: 'Main Floor',
      tierId: null,
      tierName: null,
    },
  ],
  warnings: [
    {
      code: 'TierWithoutMapping',
      message:
        "Tier 'VIP' is active but isn't mapped to any zone or table. Buyers in this tier won't be able to choose a seat.",
      shapeId: null,
      shapeName: null,
      tierId: 'tier-1',
      tierName: 'VIP',
    },
  ],
  tierSummary: [
    {
      tierId: 'tier-1',
      tierName: 'VIP',
      tierCapacity: 30,
      mappedZones: [],
      mappedTables: [],
      totalEnabledSeats: 0,
    },
    {
      tierId: 'tier-2',
      tierName: 'Basic',
      tierCapacity: 20,
      mappedZones: [
        { id: 'zone-x', name: 'Rear', enabledSeatCount: 50 }, // > 20 → over capacity
      ],
      mappedTables: [],
      totalEnabledSeats: 50,
    },
  ],
};

describe('TierMappingSummary — Slice S4', () => {
  it('renders loading state initially', () => {
    getReadinessMock.mockImplementation(() => new Promise(() => {})); // never resolves
    renderSummary();
    expect(screen.getByTestId('tier-mapping-summary-loading')).toBeInTheDocument();
  });

  it('renders the publish-ready status when no blockers', async () => {
    getReadinessMock.mockResolvedValueOnce(happyReport);
    renderSummary();
    await waitFor(() => {
      expect(screen.getByTestId('tier-mapping-summary-status')).toHaveTextContent(
        'Layout is publish-ready.',
      );
    });
    expect(screen.queryByTestId('tier-mapping-summary-blockers')).not.toBeInTheDocument();
    expect(screen.queryByTestId('tier-mapping-summary-warnings')).not.toBeInTheDocument();
  });

  it('renders blockers, warnings, and the count in the status line when issues exist', async () => {
    getReadinessMock.mockResolvedValueOnce(reportWithIssues);
    renderSummary();
    await waitFor(() => {
      expect(screen.getByTestId('tier-mapping-summary-status')).toHaveTextContent(
        '1 blocker to fix before publishing.',
      );
    });
    expect(
      screen.getByTestId('tier-mapping-summary-blockers'),
    ).toHaveTextContent("Zone 'Main Floor' has 200 seats but no tier mapping.");
    expect(
      screen.getByTestId('tier-mapping-summary-warnings'),
    ).toHaveTextContent("Tier 'VIP' is active but isn't mapped");
  });

  it('renders unmapped placeholder for tiers with no mappings', async () => {
    getReadinessMock.mockResolvedValueOnce(reportWithIssues);
    renderSummary();
    await waitFor(() =>
      expect(screen.getByTestId('tier-mapping-summary-table')).toBeInTheDocument(),
    );
    const vipRow = screen.getByTestId('tier-mapping-summary-row-tier-1');
    expect(vipRow).toHaveTextContent('unmapped');
    expect(vipRow).toHaveTextContent('0 / 30');
  });

  it('flags over-capacity rows with red text', async () => {
    getReadinessMock.mockResolvedValueOnce(reportWithIssues);
    renderSummary();
    await waitFor(() =>
      expect(screen.getByTestId('tier-mapping-summary-table')).toBeInTheDocument(),
    );
    const overCapacityCell = screen
      .getByTestId('tier-mapping-summary-row-tier-2')
      .querySelector('td:last-child');
    expect(overCapacityCell?.className).toContain('text-red-600');
    expect(overCapacityCell).toHaveTextContent('50 / 20');
  });

  it('renders empty-tiers state when there are no active tiers yet', async () => {
    getReadinessMock.mockResolvedValueOnce({
      isPublishReady: false,
      blockers: [
        {
          code: 'LayoutEmpty',
          message: 'Layout has no zones or tables.',
          shapeId: null,
          shapeName: null,
          tierId: null,
          tierName: null,
        },
      ],
      warnings: [],
      tierSummary: [],
    } satisfies PublishReadinessReportDto);
    renderSummary();
    await waitFor(() => {
      expect(screen.getByTestId('tier-mapping-summary-no-tiers')).toBeInTheDocument();
    });
    expect(screen.getByTestId('tier-mapping-summary-no-tiers')).toHaveTextContent(
      'No active ticket tiers yet',
    );
  });

  it('renders error state on repository failure', async () => {
    getReadinessMock.mockRejectedValueOnce(new Error('boom'));
    renderSummary();
    await waitFor(() => {
      expect(screen.getByTestId('tier-mapping-summary-error')).toBeInTheDocument();
    });
  });
});
