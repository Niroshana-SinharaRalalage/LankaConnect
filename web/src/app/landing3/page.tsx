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
 * Landing Page Variant 3: Theater Screen
 * Large cinema screen spans behind the hero. The clear video is center-right.
 * Dark theater surround + curtain edges frame the screen.
 * Hero text overlays the dark shading area on the left.
 * Event scroller sits at the bottom over the screen.
 */
export default function LandingPage3() {
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

      {/* Hero Section — theater screen as full background */}
      <div className="relative overflow-hidden min-h-[520px] lg:min-h-[600px]">
        {/* Dark theater wall base */}
        <div className="absolute inset-0 bg-gradient-to-b from-gray-950 via-gray-900 to-gray-950" />

        {/* Full-bleed video behind everything */}
        <div className="absolute inset-0">
          <VideoPlayer className="w-full h-full object-cover" />
        </div>

        {/* Theater shading: dark surround on left for text, clear center-right for video */}
        <div className="absolute inset-0 pointer-events-none" style={{
          background: 'linear-gradient(to right, rgba(10,10,10,0.92) 0%, rgba(10,10,10,0.8) 25%, rgba(0,0,0,0.35) 50%, transparent 65%)',
        }} />

        {/* Top edge — dark theater ceiling */}
        <div className="absolute top-0 left-0 right-0 h-20 pointer-events-none" style={{
          background: 'linear-gradient(to bottom, rgba(5,5,5,0.8) 0%, rgba(5,5,5,0.3) 50%, transparent 100%)',
        }} />
        {/* Bottom edge — dark theater floor */}
        <div className="absolute bottom-0 left-0 right-0 h-28 pointer-events-none" style={{
          background: 'linear-gradient(to top, rgba(5,5,5,0.8) 0%, rgba(5,5,5,0.3) 50%, transparent 100%)',
        }} />
        {/* Right edge fade */}
        <div className="absolute right-0 top-0 bottom-0 w-16 pointer-events-none" style={{
          background: 'linear-gradient(to left, rgba(5,5,5,0.6) 0%, transparent 100%)',
        }} />

        {/* Curtain drapes — left */}
        <div className="absolute left-0 top-0 bottom-0 w-8 z-10" style={{
          background: 'linear-gradient(90deg, #7f1d1d 0%, #991b1b 50%, rgba(69,10,10,0.6) 80%, transparent 100%)',
        }}>
          <div className="absolute inset-0 opacity-30" style={{
            backgroundImage: 'repeating-linear-gradient(180deg, transparent, transparent 10px, rgba(0,0,0,0.3) 10px, rgba(0,0,0,0.3) 20px)',
          }} />
        </div>
        {/* Curtain drapes — right */}
        <div className="absolute right-0 top-0 bottom-0 w-8 z-10" style={{
          background: 'linear-gradient(270deg, #7f1d1d 0%, #991b1b 50%, rgba(69,10,10,0.6) 80%, transparent 100%)',
        }}>
          <div className="absolute inset-0 opacity-30" style={{
            backgroundImage: 'repeating-linear-gradient(180deg, transparent, transparent 10px, rgba(0,0,0,0.3) 10px, rgba(0,0,0,0.3) 20px)',
          }} />
        </div>
        {/* Top curtain valance */}
        <div className="absolute top-0 left-8 right-8 h-4 z-10" style={{
          background: 'linear-gradient(180deg, #7f1d1d 0%, #991b1b 60%, transparent 100%)',
        }} />

        {/* Content layer on top */}
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
