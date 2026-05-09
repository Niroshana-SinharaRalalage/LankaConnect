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
}

export interface EventQuickNavProps {
  pills: EventQuickNavPill[];
}

/**
 * Phase 8YB.4 — quick-navigation pill row for the public event details page.
 *
 * Renders one orange-bordered scroll-anchor button per pill where `show=true`,
 * filtering the rest out. Returns `null` when no pill is visible so the parent
 * doesn't get an empty container in its layout.
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
      {visible.map((pill) => (
        <button
          key={pill.id}
          type="button"
          onClick={() => {
            const target = document.getElementById(pill.id);
            if (target) {
              target.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
          }}
          className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-md border text-neutral-700 bg-white hover:text-white hover:border-transparent transition-colors"
          style={{ borderColor: '#FF7900' }}
          onMouseEnter={(e) => {
            e.currentTarget.style.backgroundColor = '#FF7900';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.backgroundColor = 'white';
            e.currentTarget.style.color = '';
          }}
        >
          {pill.icon}
          {pill.label}
        </button>
      ))}
    </>
  );
};

export default EventQuickNav;
