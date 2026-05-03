/**
 * Slice S4 — read-only tier-mapping summary surfaced through
 * `GET /api/venue-layouts/{id}/publish-readiness`.
 *
 * Renders three sections (each absent if empty):
 *   1. Blockers — must fix before publish (red).
 *   2. Warnings — should review (amber).
 *   3. Per-tier mapping table — every active tier, its capacity, and the
 *      zones/tables currently mapped to it with their enabled-seat counts.
 *
 * Mounts in two places:
 *   - SeatingLayoutPicker summary (read-only, always visible after a layout
 *     is attached) — gives the organiser an at-a-glance view of what's
 *     wired up before they click "Customize".
 *   - CanvasEditorModal sidebar (light mode) — same data, lighter chrome,
 *     so editing in the canvas keeps validation context visible.
 *
 * Distinct from the strict publish gate (`POST /events/{id}/publish` →
 * 422 on first blocker). This component is informational; the publish
 * action is gated by the API and the underlying button uses the same
 * report data to disable itself when blockers exist.
 */

'use client';

import React from 'react';
import { useLayoutPublishReadiness } from '@/presentation/hooks/useVenueLayouts';
import type {
  PublishReadinessIssueDto,
  TierMappingSummaryDto,
} from '@/infrastructure/api/types/events.types';

export interface TierMappingSummaryProps {
  layoutId: string;
  /** Compact mode renders a tighter layout for the canvas-editor sidebar. */
  compact?: boolean;
}

export function TierMappingSummary({
  layoutId,
  compact = false,
}: TierMappingSummaryProps) {
  const { data, isLoading, isError } = useLayoutPublishReadiness(layoutId);

  if (isLoading) {
    return (
      <div
        className="text-sm text-neutral-500 italic"
        data-testid="tier-mapping-summary-loading"
      >
        Loading publish readiness…
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div
        className="text-sm text-red-600"
        data-testid="tier-mapping-summary-error"
      >
        Could not load publish readiness for this layout.
      </div>
    );
  }

  const { blockers, warnings, tierSummary, isPublishReady } = data;
  const containerClass = compact ? 'space-y-3' : 'space-y-4';

  return (
    <div className={containerClass} data-testid="tier-mapping-summary">
      <div
        className={`flex items-center gap-2 text-sm font-medium ${
          isPublishReady ? 'text-emerald-700' : 'text-neutral-700'
        }`}
        data-testid="tier-mapping-summary-status"
      >
        <span
          className={`inline-block w-2 h-2 rounded-full ${
            isPublishReady ? 'bg-emerald-500' : 'bg-amber-500'
          }`}
          aria-hidden="true"
        />
        {isPublishReady
          ? 'Layout is publish-ready.'
          : `${blockers.length} blocker${blockers.length === 1 ? '' : 's'} to fix before publishing.`}
      </div>

      {blockers.length > 0 && (
        <IssueList
          title="Blockers"
          variant="blocker"
          issues={blockers}
          testId="tier-mapping-summary-blockers"
          compact={compact}
        />
      )}

      {warnings.length > 0 && (
        <IssueList
          title="Warnings"
          variant="warning"
          issues={warnings}
          testId="tier-mapping-summary-warnings"
          compact={compact}
        />
      )}

      {tierSummary.length > 0 ? (
        <TierTable summaries={tierSummary} compact={compact} />
      ) : (
        <p
          className="text-sm text-neutral-500"
          data-testid="tier-mapping-summary-no-tiers"
        >
          No active ticket tiers yet — add tiers to map them to zones or tables.
        </p>
      )}
    </div>
  );
}

interface IssueListProps {
  title: string;
  variant: 'blocker' | 'warning';
  issues: PublishReadinessIssueDto[];
  testId: string;
  compact: boolean;
}

function IssueList({ title, variant, issues, testId, compact }: IssueListProps) {
  const tone =
    variant === 'blocker'
      ? 'bg-red-50 border-red-200 text-red-800'
      : 'bg-amber-50 border-amber-200 text-amber-800';
  return (
    <div
      className={`rounded-md border ${tone} ${compact ? 'p-2' : 'p-3'}`}
      data-testid={testId}
    >
      <div className="text-xs font-semibold uppercase tracking-wide mb-1">
        {title} ({issues.length})
      </div>
      <ul className="list-disc pl-5 space-y-1 text-sm">
        {issues.map((issue, idx) => (
          <li key={`${issue.code}-${issue.shapeId ?? issue.tierId ?? idx}`}>
            {issue.message}
          </li>
        ))}
      </ul>
    </div>
  );
}

interface TierTableProps {
  summaries: TierMappingSummaryDto[];
  compact: boolean;
}

function TierTable({ summaries, compact }: TierTableProps) {
  return (
    <div
      className="border border-neutral-200 rounded-md overflow-hidden"
      data-testid="tier-mapping-summary-table"
    >
      <table className="w-full text-sm">
        <thead className="bg-neutral-50 text-xs uppercase tracking-wide text-neutral-600">
          <tr>
            <th className={`text-left ${compact ? 'p-2' : 'p-3'}`}>Tier</th>
            <th className={`text-left ${compact ? 'p-2' : 'p-3'}`}>Mapped to</th>
            <th className={`text-right ${compact ? 'p-2' : 'p-3'}`}>
              Seats / Capacity
            </th>
          </tr>
        </thead>
        <tbody>
          {summaries.map((tier) => {
            const overCapacity = tier.totalEnabledSeats > tier.tierCapacity;
            return (
              <tr
                key={tier.tierId}
                className="border-t border-neutral-200"
                data-testid={`tier-mapping-summary-row-${tier.tierId}`}
              >
                <td className={`align-top font-medium ${compact ? 'p-2' : 'p-3'}`}>
                  {tier.tierName}
                </td>
                <td className={`align-top ${compact ? 'p-2' : 'p-3'}`}>
                  {tier.mappedZones.length === 0 && tier.mappedTables.length === 0 ? (
                    <span className="text-neutral-400 italic">unmapped</span>
                  ) : (
                    <ul className="space-y-0.5">
                      {tier.mappedZones.map((z) => (
                        <li key={`zone-${z.id}`}>
                          <span className="text-xs uppercase tracking-wide text-neutral-500 mr-2">
                            zone
                          </span>
                          {z.name}{' '}
                          <span className="text-neutral-500">({z.enabledSeatCount} seats)</span>
                        </li>
                      ))}
                      {tier.mappedTables.map((t) => (
                        <li key={`table-${t.id}`}>
                          <span className="text-xs uppercase tracking-wide text-neutral-500 mr-2">
                            table
                          </span>
                          {t.name}{' '}
                          <span className="text-neutral-500">({t.enabledSeatCount} seats)</span>
                        </li>
                      ))}
                    </ul>
                  )}
                </td>
                <td
                  className={`align-top text-right ${compact ? 'p-2' : 'p-3'} ${
                    overCapacity ? 'text-red-600 font-medium' : ''
                  }`}
                >
                  {tier.totalEnabledSeats} / {tier.tierCapacity}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
