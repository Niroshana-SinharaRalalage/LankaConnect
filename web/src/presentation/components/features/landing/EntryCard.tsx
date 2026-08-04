'use client';

/**
 * EntryCard — a sub-brand entry point on the LankaConnect umbrella landing page.
 *
 * Extracted from the inline LankaEvents card in app/page.tsx when LankaSeyla was
 * added, so the two cards stay peers by construction instead of by discipline.
 * The whole look is driven by `brandColor`: gradient, border and all four shadow
 * states derive from it, so a new sub-brand is a five-line call.
 *
 * See docs/superpowers/specs/2026-08-04-lankaseyla-landing-entry-design.md
 */

import React from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { ArrowUpRight } from 'lucide-react';

export interface EntryCardProps {
  /** Drives the gradient, border and every shadow state. */
  brandColor: string;
  /** 110×110 square asset, rendered at 55×55. */
  logoSrc: string;
  logoAlt: string;
  name: string;
  /** The pill beside the name, e.g. "Event Planner". */
  badge: string;
  tagline: string;
  href: string;
  /** External destinations open in a new tab and drop the window.opener handle. */
  external?: boolean;
  /**
   * The green pulsing "LIVE" dot. Only meaningful for destinations we host —
   * on an external card it would assert liveness we cannot observe.
   */
  live?: boolean;
}

type CardState = 'rest' | 'hover' | 'active';

/**
 * The five-layer shadow stack, per interaction state. The `0 Npx 0 <color>` layer
 * is the card's "lip" — shrinking it on press is what produces the button-press
 * feel, so the three states have to stay in proportion to each other.
 */
function shadowFor(state: CardState, color: string): string {
  switch (state) {
    case 'hover':
      return [
        `inset 0 1px 0 rgba(255,255,255,0.22)`,
        `inset 0 -1px 0 ${color}70`,
        `0 9px 0 ${color}60`,
        `0 16px 48px ${color}35`,
        `0 4px 8px rgba(0,0,0,0.5)`,
      ].join(', ');
    case 'active':
      return [
        `inset 0 1px 0 rgba(255,255,255,0.10)`,
        `inset 0 -1px 0 ${color}40`,
        `0 1px 0 ${color}55`,
        `0 3px 12px ${color}18`,
        `0 1px 2px rgba(0,0,0,0.5)`,
      ].join(', ');
    case 'rest':
    default:
      return [
        `inset 0 1px 0 rgba(255,255,255,0.18)`,
        `inset 0 -1px 0 ${color}60`,
        `0 6px 0 ${color}55`,
        `0 10px 32px ${color}22`,
        `0 2px 4px rgba(0,0,0,0.45)`,
      ].join(', ');
  }
}

const TRANSFORM_FOR: Record<CardState, string> = {
  rest: '',
  hover: 'translateY(-3px)',
  active: 'translateY(5px)',
};

/** "LankaEvents" -> "lanka-events", so the test id tracks the brand name. */
function slugify(name: string): string {
  return name.replace(/([a-z0-9])([A-Z])/g, '$1-$2').toLowerCase();
}

export function EntryCard({
  brandColor,
  logoSrc,
  logoAlt,
  name,
  badge,
  tagline,
  href,
  external = false,
  live = false,
}: EntryCardProps) {
  const applyState = (el: HTMLDivElement, state: CardState) => {
    el.style.transform = TRANSFORM_FOR[state];
    el.style.boxShadow = shadowFor(state, brandColor);
  };

  return (
    <Link
      href={href}
      className="block w-full rounded-2xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/70 focus-visible:ring-offset-2 focus-visible:ring-offset-black"
      style={{ maxWidth: '520px' }}
      data-testid={`entry-card-${slugify(name)}`}
      {...(external ? { target: '_blank', rel: 'noopener noreferrer' } : {})}
    >
      <div
        className="relative flex h-full items-start gap-5 px-6 py-5 rounded-2xl cursor-pointer select-none"
        style={{
          background: `linear-gradient(175deg, ${brandColor}30 0%, ${brandColor}14 60%, ${brandColor}06 100%)`,
          border: `1px solid ${brandColor}55`,
          boxShadow: shadowFor('rest', brandColor),
          minHeight: '50px',
          transition: 'transform 0.10s ease, box-shadow 0.10s ease',
        }}
        onMouseEnter={e => applyState(e.currentTarget, 'hover')}
        onMouseLeave={e => applyState(e.currentTarget, 'rest')}
        onMouseDown={e => applyState(e.currentTarget, 'active')}
        onMouseUp={e => applyState(e.currentTarget, 'hover')}
      >
        {/* Logo — no container box, just the image */}
        <Image
          src={logoSrc}
          alt={logoAlt}
          width={55}
          height={55}
          className="object-contain flex-shrink-0"
        />

        {/* Text */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1.5">
            <span className="font-bold text-2xl text-white leading-tight">{name}</span>
            <span
              className="text-[12px] font-semibold px-2.5 py-0.5 rounded-full leading-none"
              style={{
                background: `${brandColor}25`,
                color: brandColor,
                border: `1px solid ${brandColor}40`,
              }}
            >
              {badge}
            </span>
          </div>
          <div className="card-tagline text-white/65 uppercase leading-tight text-left">
            {tagline}
          </div>
        </div>

        {/* Right side */}
        <div className="flex flex-col items-end gap-2 flex-shrink-0 pt-1">
          {live && (
            <div className="flex items-center gap-1">
              <span className="w-1.5 h-1.5 rounded-full bg-green-400 animate-pulse" />
              <span className="text-green-400 text-[10px] font-bold uppercase tracking-wide">
                Live
              </span>
            </div>
          )}
          <ArrowUpRight className="h-5 w-5" style={{ color: brandColor }} />
        </div>
      </div>
    </Link>
  );
}

export default EntryCard;
