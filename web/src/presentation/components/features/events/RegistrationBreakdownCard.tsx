'use client';

import type {
  RegistrationBreakdownDto,
  BreakdownPairDto,
} from '@/infrastructure/api/types/events.types';

/**
 * Phase 7F-E.2 (architect-approved 2026-05-01): renders the shared
 * `RegistrationBreakdown` projection on the event-detail "You're Registered!" card.
 *
 * Architect rule: when `BreakdownPair.captured == false`, render "N/A" — explicit
 * absence > silent omission. This is the same data shape consumed by the email
 * (Slice 7F-E.3) and PDF (Slice 7F-E.4a), so the user sees consistent rendering
 * everywhere.
 *
 * Layout per user-mandated format:
 *   For each tier (or single row if non-tiered):
 *     Tier: VIP                     ← only when tiered
 *     Number of attendees: 2
 *     Adult/Child: 1/1   (or N/A)
 *     Male/Female: N/A   (or 1/1)
 */
interface RegistrationBreakdownCardProps {
  breakdown: RegistrationBreakdownDto;
  /** Optional lead-attendee name shown above the rows for Mode B registrations. */
  leadAttendeeName?: string | null;
}

export function RegistrationBreakdownCard({
  breakdown,
  leadAttendeeName,
}: RegistrationBreakdownCardProps) {
  if (!breakdown || breakdown.rows.length === 0) {
    return null;
  }

  return (
    <div className="space-y-3">
      {leadAttendeeName && (
        <div className="text-sm">
          <span className="font-medium text-neutral-700">Lead Attendee:</span>{' '}
          <span className="text-neutral-900">{leadAttendeeName}</span>
        </div>
      )}

      <div className="text-sm">
        <span className="font-medium text-neutral-700">Total attendees:</span>{' '}
        <span className="text-neutral-900">{breakdown.totalAttendees}</span>
      </div>

      <div className="space-y-2">
        {breakdown.rows.map((row, idx) => (
          <div
            key={`${row.tierName ?? 'untiered'}-${idx}`}
            className="rounded-md border border-neutral-200 bg-white p-3 text-sm space-y-1"
            data-testid={`breakdown-row-${idx}`}
          >
            {row.tierName && (
              <div>
                <span className="font-medium text-neutral-700">Tier:</span>{' '}
                <span className="text-neutral-900">{row.tierName}</span>
              </div>
            )}
            <div>
              <span className="font-medium text-neutral-700">Number of attendees:</span>{' '}
              <span className="text-neutral-900">{row.count}</span>
            </div>
            <div>
              <span className="font-medium text-neutral-700">Adult/Child:</span>{' '}
              <span className="text-neutral-900">{formatPair(row.age)}</span>
            </div>
            <div>
              <span className="font-medium text-neutral-700">Male/Female:</span>{' '}
              <span className="text-neutral-900">{formatPair(row.gender)}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

/**
 * Pure formatting helper exported for reuse + testing. Returns "N/A" when the mode
 * doesn't capture this axis (architect rule: absence is explicit, not silent).
 */
export function formatPair(pair: BreakdownPairDto): string {
  if (!pair.captured) return 'N/A';
  return `${pair.left}/${pair.right}`;
}
