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
 * 2-column grid. Right side: video fills a large area.
 * Shading zone around the video uses banner-matching colors
 * so there is NO sharp border — video blends seamlessly into the banner.
 * Inner area is crystal clear, outer area fades into banner gradient.
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
      <div className="relative overflow-hidden bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800">
        {/* Decorative Background */}
        <div className="absolute inset-0 opacity-10">
          <div className="absolute inset-0" style={{ backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")` }} />
        </div>
        <div className="absolute inset-0 overflow-hidden">
          <div className="absolute -top-24 -left-24 w-96 h-96 bg-orange-400/20 rounded-full blur-3xl" />
          <div className="absolute -bottom-24 -right-24 w-96 h-96 bg-emerald-400/20 rounded-full blur-3xl" />
        </div>

        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 md:py-24">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 items-start">
            <HeroLeftContent stats={stats} statsLoading={statsLoading} />

            {/* Right — Video area + Event Scroller */}
            <div className="relative hidden lg:flex flex-col gap-3">
              {/* Outer area — invisible container, no border, no background */}
              <div className="relative w-full" style={{ aspectRatio: '16 / 11' }}>
                {/* Video fills the outer area */}
                <div className="absolute inset-0 overflow-hidden rounded-lg">
                  <VideoPlayer />
                </div>

                {/* Shading zone overlays — use banner gradient colors for seamless blend */}
                {/* Left shading — rose-800 (#9f1239) matching banner center-right area */}
                <div className="absolute left-0 top-0 bottom-0 pointer-events-none rounded-l-lg" style={{
                  width: '30%',
                  background: 'linear-gradient(to right, rgba(159,18,57,1) 0%, rgba(159,18,57,0.7) 30%, rgba(159,18,57,0.3) 60%, transparent 100%)',
                }} />
                {/* Right shading — emerald-800 (#065f46) matching banner right area */}
                <div className="absolute right-0 top-0 bottom-0 pointer-events-none rounded-r-lg" style={{
                  width: '15%',
                  background: 'linear-gradient(to left, rgba(6,95,70,1) 0%, rgba(6,95,70,0.5) 40%, transparent 100%)',
                }} />
                {/* Top shading — blend of rose/emerald */}
                <div className="absolute top-0 left-0 right-0 pointer-events-none rounded-t-lg" style={{
                  height: '20%',
                  background: 'linear-gradient(to bottom, rgba(120,25,55,0.9) 0%, rgba(120,25,55,0.4) 50%, transparent 100%)',
                }} />
                {/* Bottom shading */}
                <div className="absolute bottom-0 left-0 right-0 pointer-events-none rounded-b-lg" style={{
                  height: '20%',
                  background: 'linear-gradient(to top, rgba(50,70,60,0.9) 0%, rgba(50,70,60,0.4) 50%, transparent 100%)',
                }} />
              </div>

              <EventScroller events={events} isLoading={scrollLoading} />
            </div>
          </div>
        </div>
      </div>

      <BelowBannerContent newsletters={newsletters} newslettersLoading={newslettersLoading} />
      <Footer />
    </div>
  );
}
