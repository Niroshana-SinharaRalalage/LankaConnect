'use client';

import { useMemo } from 'react';
import { Handshake, ChevronRight } from 'lucide-react';
import { useEventSponsors } from '@/presentation/hooks/useSponsors';
import type { SponsorConfigurationDto, SponsorDto } from '@/infrastructure/api/types/events.types';

interface SponsorsPreviewStripProps {
  eventId: string;
  sponsorConfig?: SponsorConfigurationDto | null;
}

/**
 * Phase 6A.145 Commit 5 — public-page preview strip for sponsors with images.
 * Renders right AFTER the AddOnsPreviewStrip (per user's R2 layout request).
 *
 * Architectural decisions (locked):
 * - Sponsors are shown ONLY when they have an imageUrl AND are in a confirmed state
 *   (Completed for money, RecordedItem for item). This is the user's R2 intent —
 *   sponsors who paid past the threshold get their logo displayed.
 * - Sort: descending by amount/estValue (largest contributors first). Item sponsors
 *   without estValue sort last by createdAt desc.
 * - Click → scrolls to the full `<SponsorSection>` at the bottom (id="sponsors").
 * - Section hidden when no eligible sponsors.
 * - Endpoint requires organizer auth (`GET /sponsors`) so this strip only renders
 *   data for users who can already see the full management view. For anonymous
 *   visitors, the query 403s and we hide the strip gracefully (architect H-1
 *   "independent hide"). A future commit can add a public sponsors-with-images
 *   endpoint if anonymous visibility is needed.
 */
export function SponsorsPreviewStrip({ eventId, sponsorConfig }: SponsorsPreviewStripProps) {
  const enabled = sponsorConfig?.isEnabled === true;
  const { data: sponsorsResponse } = useEventSponsors(eventId, enabled);

  const eligibleSponsors = useMemo(() => {
    const sponsors = sponsorsResponse?.sponsors ?? [];
    return sponsors
      .filter((s: SponsorDto) => {
        if (!s.imageUrl) return false;
        if (s.sponsorType === 'Money') return s.status === 'Completed';
        if (s.sponsorType === 'Item') return s.status === 'RecordedItem';
        return false;
      })
      .sort((a: SponsorDto, b: SponsorDto) => {
        const aValue = a.amount ?? a.estimatedValue ?? 0;
        const bValue = b.amount ?? b.estimatedValue ?? 0;
        if (bValue !== aValue) return bValue - aValue;
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      });
  }, [sponsorsResponse]);

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
        {eligibleSponsors.map((s: SponsorDto) => (
          <button
            key={s.id}
            type="button"
            onClick={handleCardClick}
            data-testid={`sponsor-preview-card-${s.id}`}
            className="flex-none w-56 snap-start rounded-lg border border-neutral-200 bg-white text-left transition-shadow hover:shadow-md hover:border-indigo-300"
            aria-label={`Sponsor: ${s.sponsorOrganization || s.sponsorName}`}
          >
            <div className="aspect-[16/9] w-full overflow-hidden rounded-t-lg bg-neutral-50 flex items-center justify-center">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={s.imageUrl!}
                alt={s.sponsorOrganization || s.sponsorName}
                loading="lazy"
                className="h-full w-full object-contain p-2"
              />
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
