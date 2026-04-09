'use client';

/**
 * FeaturedNewslettersCarousel
 *
 * Full-width auto-scrolling marquee of published newsletters.
 * Displayed as a standalone section below the hero, centered.
 * Shows ~6 cards at a time on desktop. Pauses on hover.
 */

import * as React from 'react';
import Link from 'next/link';
import { Mail, ArrowRight } from 'lucide-react';
import { usePublishedNewsletters } from '@/presentation/hooks/useNewsletters';
import type { NewsletterDto } from '@/infrastructure/api/types/newsletters.types';

function stripHtml(html: string): string {
  return html.replace(/<[^>]*>/g, '').trim();
}

function formatDate(newsletter: NewsletterDto): string {
  const raw = newsletter.publishedAt ?? newsletter.createdAt;
  return new Date(raw).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function NewsletterScrollerCard({ newsletter }: { newsletter: NewsletterDto }) {
  const excerpt = stripHtml(newsletter.description ?? '');
  const preview = excerpt.length > 75 ? excerpt.slice(0, 75) + '…' : excerpt;

  return (
    <Link
      href={`/newsletters/${newsletter.id}`}
      className="group relative min-w-[210px] w-[210px] h-36 rounded-2xl overflow-hidden flex-shrink-0 cursor-pointer ring-2 ring-white/20 hover:ring-[#FF7900]/70 transition-all hover:-translate-y-1 hover:scale-[1.02] shadow-lg hover:shadow-xl"
      style={{ background: 'linear-gradient(145deg, rgba(0,0,0,0.45) 0%, rgba(0,0,0,0.30) 100%)', backdropFilter: 'blur(4px)' }}
    >
      {/* Orange top accent bar */}
      <div className="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-[#FF7900] to-[#8B1538]" />

      <div className="absolute inset-0 p-3 pt-4 flex flex-col justify-between">
        <h3 className="text-white font-bold text-sm leading-tight line-clamp-2 drop-shadow-lg">
          {newsletter.title}
        </h3>
        {preview && (
          <p className="text-white/65 text-[11px] leading-snug line-clamp-2 mt-1">
            {preview}
          </p>
        )}
        <div className="flex items-center gap-1 mt-auto">
          <Mail className="h-3 w-3 text-[#FF7900] flex-shrink-0" />
          <span className="text-white/70 text-[10px]">{formatDate(newsletter)}</span>
        </div>
      </div>
    </Link>
  );
}

function NewsletterVerticalCard({ newsletter }: { newsletter: NewsletterDto }) {
  const excerpt = stripHtml(newsletter.description ?? '');
  const preview = excerpt.length > 60 ? excerpt.slice(0, 60) + '…' : excerpt;

  return (
    <Link
      href={`/newsletters/${newsletter.id}`}
      className="group flex flex-col gap-1 px-3 py-2.5 rounded-lg bg-white/10 hover:bg-white/20 transition-all border border-white/10 hover:border-white/25 cursor-pointer"
    >
      <div className="flex items-start gap-2">
        <div className="w-1 h-full min-h-[14px] rounded-full bg-gradient-to-b from-[#FF7900] to-[#8B1538] flex-shrink-0 mt-0.5" />
        <h3 className="text-white text-sm font-semibold leading-snug line-clamp-2 flex-1">
          {newsletter.title}
        </h3>
      </div>
      {preview && (
        <p className="text-white/55 text-[11px] leading-snug line-clamp-1 pl-3">
          {preview}
        </p>
      )}
      <div className="flex items-center gap-1 pl-3">
        <Mail className="h-2.5 w-2.5 text-[#FF7900] flex-shrink-0" />
        <span className="text-white/60 text-[10px]">{formatDate(newsletter)}</span>
      </div>
    </Link>
  );
}

interface FeaturedNewslettersCarouselProps {
  variant?: 'horizontal' | 'vertical';
  maxItems?: number;
}

export function FeaturedNewslettersCarousel({ variant = 'horizontal', maxItems = 6 }: FeaturedNewslettersCarouselProps) {
  const { data: allNewsletters, isLoading } = usePublishedNewsletters();
  const newsletters = allNewsletters?.slice(0, maxItems) ?? [];

  if (!isLoading && newsletters.length === 0) return null;

  if (variant === 'vertical') {
    return (
      <div className="flex flex-col gap-2 w-full">
        {isLoading ? (
          <>
            {[...Array(4)].map((_, i) => (
              <div key={i} className="h-[78px] rounded-lg animate-pulse bg-white/10 border border-white/10" />
            ))}
          </>
        ) : (
          newsletters.map((nl) => (
            <NewsletterVerticalCard key={nl.id} newsletter={nl} />
          ))
        )}
      </div>
    );
  }

  return (
    <div className="w-full pt-2 pb-8 px-4">
      {/* Section header — centered */}
      <div className="max-w-7xl mx-auto flex items-center justify-between mb-5 px-4">
        <span className="text-white/80 text-sm font-semibold uppercase tracking-widest">
          Featured Newsletters
        </span>
        <Link
          href="/newsletters"
          className="flex items-center gap-1 text-[#FF7900] hover:text-[#E66D00] text-sm font-semibold transition-colors"
        >
          View All <ArrowRight className="h-4 w-4" />
        </Link>
      </div>

      {isLoading ? (
        <div className="max-w-7xl mx-auto flex gap-4 overflow-hidden px-4">
          {[...Array(6)].map((_, i) => (
            <div key={i} className="min-w-[210px] h-36 rounded-2xl animate-pulse bg-white/15 ring-2 ring-white/10" />
          ))}
        </div>
      ) : (
        <div className="nl-scroller-container overflow-hidden relative">
          {/* Left fade */}
          <div className="absolute left-0 top-0 bottom-0 w-12 bg-gradient-to-r from-black/15 to-transparent z-10 pointer-events-none" />
          {/* Right fade */}
          <div className="absolute right-0 top-0 bottom-0 w-12 bg-gradient-to-l from-black/15 to-transparent z-10 pointer-events-none" />

          <div className="flex gap-4 nl-animate-marquee px-4">
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
          animation: nl-marquee 18s linear infinite;
        }
        .nl-animate-marquee:hover {
          animation-play-state: paused;
        }
      `}</style>
    </div>
  );
}
