'use client';

/**
 * FeaturedNewslettersCarousel
 *
 * Auto-scrolling marquee of published newsletters.
 * Marquee pattern adapted from EventScroller.tsx.
 * Data: usePublishedNewsletters() — up to 8 most recent Active newsletters.
 * Pauses on hover. Fades edges. Links each card to /newsletters/{id}.
 */

import * as React from 'react';
import Link from 'next/link';
import { Mail, ArrowRight } from 'lucide-react';
import { usePublishedNewsletters } from '@/presentation/hooks/useNewsletters';
import type { NewsletterDto } from '@/infrastructure/api/types/newsletters.types';

/** Strip HTML tags for safe plain-text preview */
function stripHtml(html: string): string {
  return html.replace(/<[^>]*>/g, '').trim();
}

/** Format date — falls back to createdAt if publishedAt is null */
function formatDate(newsletter: NewsletterDto): string {
  const raw = newsletter.publishedAt ?? newsletter.createdAt;
  return new Date(raw).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function NewsletterScrollerCard({ newsletter }: { newsletter: NewsletterDto }) {
  const excerpt = stripHtml(newsletter.description ?? '');
  const preview = excerpt.length > 65 ? excerpt.slice(0, 65) + '…' : excerpt;

  return (
    <Link
      href={`/newsletters/${newsletter.id}`}
      className="group relative min-w-[200px] w-[200px] h-32 rounded-2xl overflow-hidden flex-shrink-0 cursor-pointer ring-2 ring-white/20 hover:ring-[#FF7900]/60 transition-all hover:-translate-y-1 hover:scale-[1.02] shadow-lg hover:shadow-xl"
      style={{ background: 'linear-gradient(145deg, #1a1a2e 0%, #16213e 60%, #0f3460 100%)' }}
    >
      {/* Orange top accent bar */}
      <div className="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-[#FF7900] to-[#8B1538]" />

      <div className="absolute inset-0 p-3 pt-4 flex flex-col justify-between">
        {/* Title */}
        <h3 className="text-white font-bold text-sm leading-tight line-clamp-2 drop-shadow-lg">
          {newsletter.title}
        </h3>

        {/* Description excerpt */}
        {preview && (
          <p className="text-white/60 text-[11px] leading-tight line-clamp-2 mt-1">
            {preview}
          </p>
        )}

        {/* Date */}
        <div className="flex items-center gap-1 mt-auto">
          <Mail className="h-3 w-3 text-[#FF7900] flex-shrink-0" />
          <span className="text-white/70 text-[10px]">{formatDate(newsletter)}</span>
        </div>
      </div>
    </Link>
  );
}

export function FeaturedNewslettersCarousel() {
  const { data: allNewsletters, isLoading } = usePublishedNewsletters();
  const newsletters = allNewsletters?.slice(0, 8) ?? [];

  // Render nothing if no newsletters and not loading
  if (!isLoading && newsletters.length === 0) return null;

  return (
    <div className="mt-6">
      {/* Section header */}
      <div className="flex items-center justify-between mb-3">
        <span className="text-white/70 text-xs font-medium uppercase tracking-wider">
          Featured Newsletters
        </span>
        <Link
          href="/newsletters"
          className="flex items-center gap-1 text-[#FF7900] hover:text-[#E66D00] text-xs font-semibold transition-colors"
        >
          View All <ArrowRight className="h-3 w-3" />
        </Link>
      </div>

      {isLoading ? (
        <div className="flex gap-3 overflow-hidden">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="min-w-[200px] h-32 rounded-2xl animate-pulse bg-white/10 ring-2 ring-white/20" />
          ))}
        </div>
      ) : (
        <div className="nl-scroller-container overflow-hidden relative">
          {/* Left fade edge */}
          <div className="absolute left-0 top-0 bottom-0 w-12 bg-gradient-to-r from-black/40 to-transparent z-10 pointer-events-none rounded-l-2xl" />
          {/* Right fade edge */}
          <div className="absolute right-0 top-0 bottom-0 w-12 bg-gradient-to-l from-black/40 to-transparent z-10 pointer-events-none rounded-r-2xl" />

          <div className="flex gap-3 nl-animate-marquee">
            {[...newsletters, ...newsletters, ...newsletters].map((nl, index) => (
              <NewsletterScrollerCard key={`${nl.id}-${index}`} newsletter={nl} />
            ))}
          </div>
        </div>
      )}

      <style jsx>{`
        @keyframes nl-marquee {
          0%   { transform: translateX(0); }
          100% { transform: translateX(-33.33%); }
        }
        .nl-animate-marquee {
          animation: nl-marquee 12s linear infinite;
        }
        .nl-animate-marquee:hover {
          animation-play-state: paused;
        }
      `}</style>
    </div>
  );
}
