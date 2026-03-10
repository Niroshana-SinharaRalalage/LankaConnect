'use client';

import * as React from 'react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { VideoPlayer } from '@/presentation/components/features/landing/VideoPlayer';
import { useFeaturedEvents } from '@/presentation/hooks/useEvents';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useGeolocation } from '@/presentation/hooks/useGeolocation';
import { useCommunityStats } from '@/presentation/hooks/useStats';
import { usePublishedNewsletters } from '@/presentation/hooks/useNewsletters';
import { HeroLeftContent } from '@/presentation/components/features/landing/HeroLeftContent';
import { EventScroller } from '@/presentation/components/features/landing/EventScroller';
import { BelowBannerContent } from '@/presentation/components/features/landing/BelowBannerContent';

/**
 * Landing Page Variant 4: Background Video
 * Video sits BEHIND the banner gradient. The banner gradient fades from
 * opaque (left, for text) to transparent (right, revealing the video).
 * No visible border/container — the video shows through where the banner thins out.
 */
export default function LandingPage4() {
  const { user } = useAuthStore();
  const isAnonymous = !user?.userId;
  const { latitude, longitude, loading: locationLoading } = useGeolocation(isAnonymous);

  const { data: featuredEvents, isLoading: eventsLoading, error: eventsError } = useFeaturedEvents(
    user?.userId,
    isAnonymous ? latitude ?? undefined : undefined,
    isAnonymous ? longitude ?? undefined : undefined
  );

  const { data: stats, isLoading: statsLoading } = useCommunityStats();
  const { data: newsletters, isLoading: newslettersLoading } = usePublishedNewsletters();

  const events = featuredEvents || [];
  const scrollLoading = eventsLoading || (isAnonymous && locationLoading);

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Hero Section */}
      <div className="relative overflow-hidden">
        {/* Layer 1: Banner gradient base */}
        <div className="absolute inset-0 bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800" />

        {/* Decorative pattern */}
        <div className="absolute inset-0 opacity-10">
          <div className="absolute inset-0" style={{ backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")` }} />
        </div>

        {/* Layer 2: Video — behind the banner, right ~70%, no visible border */}
        <div className="absolute top-0 bottom-0 right-0 hidden lg:block" style={{ width: '70%' }}>
          <VideoPlayer />
        </div>

        {/* Layer 3: Gradient overlay ON TOP of video — banner colors that fade to transparent */}
        {/* This blends the video seamlessly into the banner — no hard edge */}
        <div className="absolute inset-0 pointer-events-none hidden lg:block" style={{
          background: `linear-gradient(to right,
            rgba(234,88,12,1) 0%,
            rgba(190,50,35,1) 15%,
            rgba(159,18,57,1) 28%,
            rgba(159,18,57,0.7) 38%,
            rgba(100,40,55,0.4) 48%,
            transparent 60%)`,
        }} />
        {/* Top edge — blend banner color over video top */}
        <div className="absolute top-0 left-0 right-0 h-20 pointer-events-none hidden lg:block" style={{
          background: 'linear-gradient(to bottom, rgba(159,18,57,0.6) 0%, transparent 100%)',
        }} />
        {/* Bottom edge — blend banner color over video bottom */}
        <div className="absolute bottom-0 left-0 right-0 h-24 pointer-events-none hidden lg:block" style={{
          background: 'linear-gradient(to top, rgba(6,95,70,0.6) 0%, transparent 100%)',
        }} />
        {/* Right edge — subtle fade at the page edge */}
        <div className="absolute right-0 top-0 bottom-0 w-12 pointer-events-none hidden lg:block" style={{
          background: 'linear-gradient(to left, rgba(6,95,70,0.5) 0%, transparent 100%)',
        }} />

        {/* Layer 4: Content — on top of everything */}
        <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 md:py-24">
          <div className="max-w-xl">
            <HeroLeftContent stats={stats} statsLoading={statsLoading} />
          </div>

          {/* Event scroller at the bottom */}
          <div className="mt-10 lg:mt-14">
            <EventScroller events={events} isLoading={scrollLoading} />
          </div>
        </div>
      </div>

      <BelowBannerContent newsletters={newsletters} newslettersLoading={newslettersLoading} />
      <Footer />
    </div>
  );
}
