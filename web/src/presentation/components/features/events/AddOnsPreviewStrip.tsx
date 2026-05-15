'use client';

import { useMemo } from 'react';
import { ShoppingBag, ChevronRight, Package } from 'lucide-react';
import { useAddOnDefinitions } from '@/presentation/hooks/useAddOns';
import type { AddOnConfigurationDto, AddOnDefinitionDto } from '@/infrastructure/api/types/events.types';

interface AddOnsPreviewStripProps {
  eventId: string;
  addOnConfig?: AddOnConfigurationDto | null;
}

/**
 * Phase 6A.145 Commit 5 — public-page preview strip for event add-ons.
 * Renders BETWEEN the event details grid and the Event Media collapsible so the
 * operator-visible add-ons appear prominently above the fold.
 *
 * Architecture (architect-locked option (a)): READ-ONLY preview. Clicking a card
 * scrolls to the full `<AddOnSelector>` section at the bottom (id="add-ons") and
 * trusts existing CSS `scroll-margin-top` for sticky-header offset. No purchase
 * state lives here — the bottom section is the single source of truth.
 *
 * Layout: horizontal scroll strip with `overflow-x-auto snap-x snap-mandatory`.
 * Auto-engages scrolling only when cards overflow (~3 cards fit on a typical
 * 1280px container; more triggers horizontal scroll). Hidden entirely when no
 * active add-ons exist (no empty placeholder per architect H-1 "independent hide").
 */
export function AddOnsPreviewStrip({ eventId, addOnConfig }: AddOnsPreviewStripProps) {
  const enabled = addOnConfig?.isEnabled === true;
  const { data: definitions } = useAddOnDefinitions(eventId, enabled);

  const activeDefinitions = useMemo(
    () => (definitions ?? []).filter((d) => d.isActive).sort((a, b) => a.sortOrder - b.sortOrder),
    [definitions]
  );

  if (!enabled || activeDefinitions.length === 0) return null;

  const handleCardClick = () => {
    document.getElementById('add-ons')?.scrollIntoView({ behavior: 'smooth' });
  };

  return (
    <section
      aria-label="Event add-ons preview"
      data-testid="addons-preview-strip"
      className="my-6"
    >
      <div className="flex items-center gap-2 mb-3">
        <ShoppingBag className="h-5 w-5 text-emerald-600" />
        <h3 className="text-base font-semibold text-neutral-800">Event Add-Ons</h3>
        <span className="text-xs text-neutral-500">({activeDefinitions.length})</span>
        <button
          type="button"
          onClick={handleCardClick}
          className="ml-auto text-xs text-emerald-700 hover:underline flex items-center gap-1"
        >
          Browse all
          <ChevronRight className="h-3 w-3" />
        </button>
      </div>

      <div
        className="flex gap-3 overflow-x-auto snap-x snap-mandatory pb-2 -mx-1 px-1"
        role="region"
        aria-label="Add-ons scroller"
      >
        {activeDefinitions.map((d: AddOnDefinitionDto) => (
          <button
            key={d.id}
            type="button"
            onClick={handleCardClick}
            data-testid={`addon-preview-card-${d.id}`}
            className="flex-none w-56 snap-start rounded-lg border border-neutral-200 bg-white text-left transition-shadow hover:shadow-md hover:border-emerald-300"
            aria-label={`View add-on ${d.name}`}
          >
            <div className="aspect-[16/9] w-full overflow-hidden rounded-t-lg bg-neutral-50 flex items-center justify-center">
              {d.imageUrl ? (
                /* eslint-disable-next-line @next/next/no-img-element */
                <img
                  src={d.imageUrl}
                  alt={d.name}
                  loading="lazy"
                  className="h-full w-full object-cover"
                />
              ) : (
                <Package className="h-10 w-10 text-neutral-300" />
              )}
            </div>
            <div className="p-3">
              <p className="font-medium text-sm text-neutral-900 line-clamp-1">{d.name}</p>
              {d.description && (
                <p className="text-xs text-neutral-500 line-clamp-1 mt-0.5">{d.description}</p>
              )}
              <p className="text-sm font-semibold text-emerald-700 mt-1">
                {d.price === 0 ? 'Free' : `$${d.price.toFixed(2)}`}
              </p>
            </div>
          </button>
        ))}
      </div>
    </section>
  );
}
