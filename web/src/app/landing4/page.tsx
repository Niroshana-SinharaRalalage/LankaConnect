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
 * Video plays behind the ENTIRE hero section as a full-bleed background.
 * Left side has gradient shading for text readability.
 * Right/center shows the video crystal clear.
 * Event scroller sits at the bottom of the hero over the video.
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

      {/* Hero Section — video is the full background */}
      <div className="relative overflow-hidden min-h-[520px] lg:min-h-[600px]">
        {/* Full-bleed background video */}
        <div className="absolute inset-0 bg-black">
          <VideoPlayer className="w-full h-full object-cover" />
        </div>

        {/* Shading overlay: dark on left (for text), transparent on center-right (clear video) */}
        <div className="absolute inset-0 pointer-events-none" style={{
          background: 'linear-gradient(to right, rgba(0,0,0,0.85) 0%, rgba(0,0,0,0.7) 25%, rgba(0,0,0,0.3) 50%, transparent 65%)',
        }} />

        {/* Top edge fade */}
        <div className="absolute top-0 left-0 right-0 h-24 pointer-events-none" style={{
          background: 'linear-gradient(to bottom, rgba(0,0,0,0.5) 0%, transparent 100%)',
        }} />
        {/* Bottom edge fade */}
        <div className="absolute bottom-0 left-0 right-0 h-32 pointer-events-none" style={{
          background: 'linear-gradient(to top, rgba(0,0,0,0.6) 0%, transparent 100%)',
        }} />
        {/* Right edge fade (soft blend into page edge) */}
        <div className="absolute right-0 top-0 bottom-0 w-16 pointer-events-none" style={{
          background: 'linear-gradient(to left, rgba(0,0,0,0.4) 0%, transparent 100%)',
        }} />

        {/* Content layer on top of video + shading */}
        <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 md:py-24">
          <div className="max-w-xl">
            <HeroLeftContent stats={stats} statsLoading={statsLoading} />
          </div>

          {/* Event scroller at the bottom of the hero */}
          <div className="mt-10 lg:mt-16">
            <EventScroller events={events} isLoading={scrollLoading} />
          </div>
        </div>
      </div>

      <BelowBannerContent newsletters={newsletters} newslettersLoading={newslettersLoading} />
      <Footer />
    </div>
  );
}
