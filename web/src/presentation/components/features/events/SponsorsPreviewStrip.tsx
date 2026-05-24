'use client';

import { Handshake, ChevronRight } from 'lucide-react';
// Phase 6A.150 — switched from useEventSponsors (auth-required, returns full
// PII) to usePublicEventSponsors. The new public endpoint is [AllowAnonymous]
// and returns a sanitized DTO so anonymous visitors don't trigger the
// auth-redirect chain. Backend already filters to sponsors-with-images +
// confirmed status and pre-sorts by contribution magnitude.
import { usePublicEventSponsors } from '@/presentation/hooks/useSponsors';
import type { SponsorConfigurationDto, PublicSponsorDto } from '@/infrastructure/api/types/events.types';

interface SponsorsPreviewStripProps {
  eventId: string;
  sponsorConfig?: SponsorConfigurationDto | null;
}

/**
 * Phase 6A.145 Commit 5 — public-page preview strip for confirmed sponsors.
 * Renders right AFTER the AddOnsPreviewStrip (per user's R2 layout request).
 *
 * Architectural decisions (locked):
 * - Sponsors are shown when they're in a confirmed state (Money/Completed or
 *   Item/RecordedItem) regardless of whether they uploaded a logo.
 * - Missing-logo sponsors render with an initials placeholder card so the
 *   design stays consistent.
 * - Sort: descending by amount/estValue (largest contributors first).
 * - Click → scrolls to the full `<SponsorSection>` at the bottom (id="sponsors").
 * - Section hidden when no eligible sponsors.
 *
 * **Phase 6A.150 update**: this strip now consumes the new
 * `[AllowAnonymous] GET /api/events/{id}/sponsors/public` endpoint via
 * `usePublicEventSponsors`. The endpoint pre-filters to confirmed sponsors
 * AND pre-sorts by contribution magnitude server-side, so the client-side
 * filter + sort that previously lived here is gone — the response already
 * contains exactly the rows we want, in the order we want. The full-PII
 * variant (useEventSponsors → /sponsors) remains for the organizer-only
 * management view. Fixes the production redirect-to-login bug where
 * anonymous visitors hit the [Authorize] endpoint, got 401, and the
 * api-client + AuthProvider chain pushed them to /login.
 *
 * **Phase 6A.148.W5.D10 update**: ImageUrl filter removed. Operator UAT
 * surfaced that bundled-at-registration sponsorships (which often skip the
 * optional image upload) were silently hidden. Now ALL confirmed sponsors
 * appear; missing-logo entries render with an initials placeholder.
 */

/**
 * Render initials for the sponsor — first letter of organisation, or sponsor name.
 * Falls back to "?" if both are empty (shouldn't happen, but defensive).
 */
function getSponsorInitials(s: PublicSponsorDto): string {
  const source = s.sponsorOrganization || s.sponsorName || '';
  const words = source.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return '?';
  if (words.length === 1) return words[0]!.slice(0, 2).toUpperCase();
  return (words[0]![0]! + words[1]![0]!).toUpperCase();
}
export function SponsorsPreviewStrip({ eventId, sponsorConfig }: SponsorsPreviewStripProps) {
  const enabled = sponsorConfig?.isEnabled === true;
  const { data: sponsorsResponse } = usePublicEventSponsors(eventId, enabled);
  const eligibleSponsors = sponsorsResponse?.sponsors ?? [];

  if (!enabled || eligibleSponsors.length === 0) return null;

  const handleCardClick = () => {
    document.getElementById('sponsors')?.scrollIntoView({ behavior: 'smooth' });
  };

  return (
    <section
      aria-label="Event sponsors preview"
      data-testid="sponsors-preview-strip"
      className="mt-6 pt-6 border-t border-neutral-200"
    >
      <div className="flex items-center gap-2 mb-4">
        <Handshake className="h-6 w-6 text-indigo-600" />
        <h3 className="text-xl font-semibold text-neutral-900">Sponsors</h3>
        <span className="text-sm text-neutral-500 font-medium">({eligibleSponsors.length})</span>
        <button
          type="button"
          onClick={handleCardClick}
          className="ml-auto text-sm text-indigo-700 hover:underline flex items-center gap-1 font-medium"
        >
          Sponsor this event
          <ChevronRight className="h-4 w-4" />
        </button>
      </div>

      <div
        className="flex gap-3 overflow-x-auto snap-x snap-mandatory pb-2 px-1 [justify-content:safe_center]"
        role="region"
        aria-label="Sponsors scroller"
      >
        {eligibleSponsors.map((s: PublicSponsorDto) => (
          <button
            key={s.id}
            type="button"
            onClick={handleCardClick}
            data-testid={`sponsor-preview-card-${s.id}`}
            className="flex-none w-64 snap-start rounded-lg border border-neutral-200 bg-white text-left transition-shadow hover:shadow-md hover:border-indigo-300"
            aria-label={`Sponsor: ${s.sponsorOrganization || s.sponsorName}`}
          >
            <div className="aspect-[4/3] w-full overflow-hidden rounded-t-lg bg-neutral-50 flex items-center justify-center">
              {s.imageUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={s.imageUrl}
                  alt={s.sponsorOrganization || s.sponsorName}
                  loading="lazy"
                  className="h-full w-full object-contain"
                />
              ) : (
                // W5.D10 placeholder for sponsors without an uploaded logo —
                // initials on indigo gradient keep the strip visually consistent.
                <div
                  className="h-full w-full flex items-center justify-center bg-gradient-to-br from-indigo-100 to-amber-100"
                  aria-hidden="true"
                >
                  <span className="text-2xl font-semibold text-indigo-700">
                    {getSponsorInitials(s)}
                  </span>
                </div>
              )}
            </div>
            <div className="p-3">
              <p className="font-medium text-sm text-neutral-900 line-clamp-1">
                {s.sponsorOrganization || s.sponsorName}
              </p>
              {s.sponsorOrganization && s.sponsorName !== s.sponsorOrganization && (
                <p className="text-xs text-neutral-500 line-clamp-1 mt-0.5">{s.sponsorName}</p>
              )}
              {s.sponsorType === 'Item' && s.itemName && (
                <p className="text-sm text-indigo-700 mt-1 line-clamp-1">{s.itemName}</p>
              )}
            </div>
          </button>
        ))}
      </div>
    </section>
  );
}
