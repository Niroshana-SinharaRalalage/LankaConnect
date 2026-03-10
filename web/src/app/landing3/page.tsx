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
 * 2-column layout. Right side has a large outer container (screen boundary).
 * Inside: clear video (inner rectangle) with dark theater surround + curtains
 * shading from clear video edge outward to the screen boundary.
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

        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 md:py-24">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 items-start">
            <HeroLeftContent stats={stats} statsLoading={statsLoading} />

            {/* Right — Theater screen + Event Scroller */}
            <div className="relative hidden lg:flex flex-col gap-3">
              {/* Outer rectangle — screen boundary with theater surround */}
              <div className="relative w-full rounded-xl overflow-hidden shadow-[0_0_60px_rgba(0,0,0,0.5)]" style={{ aspectRatio: '16 / 10' }}>
                {/* Dark theater wall (fills outer rectangle) */}
                <div className="absolute inset-0 bg-gradient-to-b from-gray-950 via-gray-900 to-gray-950" />

                {/* Inner rectangle — clear video, no overlays */}
                <div className="absolute rounded-sm overflow-hidden z-[1]" style={{
                  top: '8%', bottom: '8%', left: '10%', right: '10%',
                }}>
                  <VideoPlayer />
                </div>

                {/* Gradient shading: transitions from clear video outward to dark theater surround */}
                {/* Top shading zone */}
                <div className="absolute left-0 right-0 top-0 z-[2] pointer-events-none" style={{
                  height: '16%',
                  background: 'linear-gradient(to bottom, rgba(3,7,18,0.95) 0%, rgba(3,7,18,0.5) 60%, transparent 100%)',
                }} />
                {/* Bottom shading zone */}
                <div className="absolute left-0 right-0 bottom-0 z-[2] pointer-events-none" style={{
                  height: '16%',
                  background: 'linear-gradient(to top, rgba(3,7,18,0.95) 0%, rgba(3,7,18,0.5) 60%, transparent 100%)',
                }} />
                {/* Left shading zone */}
                <div className="absolute left-0 top-0 bottom-0 z-[2] pointer-events-none" style={{
                  width: '16%',
                  background: 'linear-gradient(to right, rgba(3,7,18,0.95) 0%, rgba(3,7,18,0.5) 55%, transparent 100%)',
                }} />
                {/* Right shading zone */}
                <div className="absolute right-0 top-0 bottom-0 z-[2] pointer-events-none" style={{
                  width: '16%',
                  background: 'linear-gradient(to left, rgba(3,7,18,0.95) 0%, rgba(3,7,18,0.5) 55%, transparent 100%)',
                }} />

                {/* Curtain drapes — left */}
                <div className="absolute left-0 top-0 bottom-0 w-5 z-[3]" style={{
                  background: 'linear-gradient(90deg, #7f1d1d 0%, #991b1b 40%, #450a0a 100%)',
                }}>
                  <div className="absolute inset-0 opacity-30" style={{
                    backgroundImage: 'repeating-linear-gradient(180deg, transparent, transparent 8px, rgba(0,0,0,0.3) 8px, rgba(0,0,0,0.3) 16px)',
                  }} />
                </div>
                {/* Curtain drapes — right */}
                <div className="absolute right-0 top-0 bottom-0 w-5 z-[3]" style={{
                  background: 'linear-gradient(270deg, #7f1d1d 0%, #991b1b 40%, #450a0a 100%)',
                }}>
                  <div className="absolute inset-0 opacity-30" style={{
                    backgroundImage: 'repeating-linear-gradient(180deg, transparent, transparent 8px, rgba(0,0,0,0.3) 8px, rgba(0,0,0,0.3) 16px)',
                  }} />
                </div>
                {/* Top curtain valance */}
                <div className="absolute top-0 left-5 right-5 h-3 z-[3]" style={{
                  background: 'linear-gradient(180deg, #7f1d1d 0%, #991b1b 60%, transparent 100%)',
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
