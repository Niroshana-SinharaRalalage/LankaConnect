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
 * Video sits behind the hero like a cinema screen, covering the right ~60%.
 * Dark theater surround with curtain drapes frame the video.
 * Left edge of the video blends into the banner via dark shading.
 * Hero text sits on top of the darker left area.
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

      {/* Hero Section */}
      <div className="relative overflow-hidden bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800">
        {/* Decorative Background */}
        <div className="absolute inset-0 opacity-10">
          <div className="absolute inset-0" style={{ backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")` }} />
        </div>
        <div className="absolute inset-0 overflow-hidden">
          <div className="absolute -top-24 -left-24 w-96 h-96 bg-orange-400/20 rounded-full blur-3xl" />
          <div className="absolute -bottom-24 -right-24 w-96 h-96 bg-emerald-400/20 rounded-full blur-3xl" />
        </div>

        {/* Theater screen — positioned behind hero, covering right ~60% */}
        <div className="absolute top-0 bottom-0 right-0 hidden lg:block" style={{ width: '65%' }}>
          {/* Dark theater wall base behind the video */}
          <div className="absolute inset-0 bg-gray-950" />

          {/* Video — crystal clear */}
          <div className="absolute inset-0">
            <VideoPlayer />
          </div>

          {/* Left edge fade: blends video into the banner gradient */}
          <div className="absolute left-0 top-0 bottom-0 pointer-events-none" style={{
            width: '40%',
            background: 'linear-gradient(to right, rgba(155,28,56,0.95) 0%, rgba(100,20,40,0.7) 30%, rgba(20,20,20,0.4) 60%, transparent 100%)',
          }} />
          {/* Top edge — dark theater ceiling */}
          <div className="absolute top-0 left-0 right-0 h-16 pointer-events-none" style={{
            background: 'linear-gradient(to bottom, rgba(10,10,10,0.7) 0%, rgba(10,10,10,0.2) 60%, transparent 100%)',
          }} />
          {/* Bottom edge — dark theater floor */}
          <div className="absolute bottom-0 left-0 right-0 h-20 pointer-events-none" style={{
            background: 'linear-gradient(to top, rgba(10,10,10,0.7) 0%, rgba(10,10,10,0.2) 60%, transparent 100%)',
          }} />
          {/* Right edge fade */}
          <div className="absolute right-0 top-0 bottom-0 w-10 pointer-events-none" style={{
            background: 'linear-gradient(to left, rgba(10,10,10,0.5) 0%, transparent 100%)',
          }} />

          {/* Curtain drape — right edge */}
          <div className="absolute right-0 top-0 bottom-0 w-6 z-[2]" style={{
            background: 'linear-gradient(270deg, #7f1d1d 0%, #991b1b 50%, transparent 100%)',
          }}>
            <div className="absolute inset-0 opacity-30" style={{
              backgroundImage: 'repeating-linear-gradient(180deg, transparent, transparent 10px, rgba(0,0,0,0.3) 10px, rgba(0,0,0,0.3) 20px)',
            }} />
          </div>

          {/* Top curtain valance */}
          <div className="absolute top-0 left-0 right-0 h-4 z-[2]" style={{
            background: 'linear-gradient(180deg, #7f1d1d 0%, #991b1b 50%, transparent 100%)',
          }} />
        </div>

        {/* Content layer — on top of everything */}
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
