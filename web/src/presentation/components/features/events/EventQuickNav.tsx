'use client';

import React from 'react';

export interface EventQuickNavPill {
  /** DOM `id` of the section this pill scrolls to. Must match the target's `id=`. */
  id: string;
  /** Visible label text. */
  label: string;
  /** Leading icon — typically a `lucide-react` icon at h-3.5 w-3.5. */
  icon: React.ReactNode;
  /** Whether this pill should render. False entries are filtered out. */
  show: boolean;
  /**
   * Visual emphasis. `'primary'` renders a solid-fill brand-orange CTA used
   * for the page's main conversion action (Register / RSVP). All other pills
   * stay `'default'` (outlined). Phase 6A.155 — added so the Register pill no
   * longer reads as one of N identical secondary actions.
   *
   * Mode C (NoRegistration) events suppress the registration pill entirely
   * and render `RegistrationStatusHint variant='pill'` ahead of this
   * component, so the primary emphasis only ever fires on Mode A
   * (DetailedAttendees) / Mode B (HeadCount*) / ExternalPaid events.
   */
  emphasis?: 'primary' | 'default';
}

export interface EventQuickNavProps {
  pills: EventQuickNavPill[];
}

const BRAND_ORANGE = '#FF7900';
// Slightly darker shade for primary-pill hover so the user gets clear
// affordance feedback without leaving the brand palette.
const BRAND_ORANGE_HOVER = '#E56C00';

/**
 * Phase 8YB.4 — quick-navigation pill row for the public event details page.
 *
 * Renders one button per pill where `show=true`, filtering the rest out.
 * Returns `null` when no pill is visible so the parent doesn't get an empty
 * container in its layout.
 *
 * Phase 6A.155: pills now accept `emphasis: 'primary' | 'default'`. Primary
 * pills (the Register/RSVP CTA) render solid-filled brand orange with white
 * text and bolder/larger sizing so the page's conversion action visually
 * dominates the row. Default pills keep the original outlined treatment.
 *
 * Why a component: previously inline in `events/[id]/page.tsx`, the descriptor
 * array kept growing as new action surfaces were added (donations, sponsors,
 * collections, add-ons, signup lists/forms, volunteers, albums). Lifting it
 * here gives every future pill a single insertion point and lets the
 * visibility logic be unit-tested without mocking React Query.
 *
 * The component intentionally does NOT own its outer flex container — the
 * parent wraps this row alongside other siblings (e.g. the non-clickable
 * `RegistrationStatusHint` status pill on Mode C events) inside the same
 * `flex flex-wrap gap-2` strip. Rendering as a fragment preserves that.
 */
export const EventQuickNav: React.FC<EventQuickNavProps> = ({ pills }) => {
  const visible = pills.filter((p) => p.show);
  if (visible.length === 0) return null;

  return (
    <>
      {visible.map((pill) => {
        const emphasis = pill.emphasis ?? 'default';
        const isPrimary = emphasis === 'primary';

        // Class names are kept as static literals (no template string with
        // dynamic class names) so Tailwind's content scanner picks them up.
        const className = isPrimary
          ? 'inline-flex items-center gap-2 px-5 py-2.5 text-sm font-semibold rounded-md border text-white shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2'
          : 'inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-md border text-neutral-700 bg-white hover:text-white hover:border-transparent transition-colors';

        return (
          <button
            key={pill.id}
            type="button"
            data-emphasis={emphasis}
            aria-label={isPrimary ? `${pill.label} for this event` : undefined}
            onClick={() => {
              try {
                const target = document.getElementById(pill.id);
                if (target) {
                  target.scrollIntoView({ behavior: 'smooth', block: 'start' });
                } else {
                  // Anchor missing — log but never throw; the page may still
                  // be mounting or the section conditionally hidden.
                  console.warn(
                    `[EventQuickNav] scroll target #${pill.id} not found in DOM`,
                  );
                }
              } catch (err) {
                // Defensive: getElementById/scrollIntoView shouldn't throw,
                // but jsdom + Safari edge cases have surprised us before.
                console.error('[EventQuickNav] scroll-to-anchor failed', {
                  pillId: pill.id,
                  err,
                });
              }
            }}
            className={className}
            style={
              isPrimary
                ? {
                    backgroundColor: BRAND_ORANGE,
                    borderColor: BRAND_ORANGE,
                  }
                : { borderColor: BRAND_ORANGE }
            }
            onMouseEnter={(e) => {
              if (isPrimary) {
                e.currentTarget.style.backgroundColor = BRAND_ORANGE_HOVER;
                e.currentTarget.style.borderColor = BRAND_ORANGE_HOVER;
              } else {
                e.currentTarget.style.backgroundColor = BRAND_ORANGE;
              }
            }}
            onMouseLeave={(e) => {
              if (isPrimary) {
                e.currentTarget.style.backgroundColor = BRAND_ORANGE;
                e.currentTarget.style.borderColor = BRAND_ORANGE;
              } else {
                e.currentTarget.style.backgroundColor = 'white';
                e.currentTarget.style.color = '';
              }
            }}
          >
            {pill.icon}
            {pill.label}
          </button>
        );
      })}
    </>
  );
};

export default EventQuickNav;
