'use client';

import * as React from 'react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Film } from 'lucide-react';
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
 * Wide cinema/theater screen with dark surround shading and curtain effect
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

            {/* Right - Theater Screen + Event Scroller */}
            <div className="relative hidden lg:flex flex-col gap-3">
              {/* Theater Screen */}
              <div className="relative" style={{ perspective: '1000px' }}>
                <div
                  className="relative rounded-xl overflow-hidden"
                  style={{ transform: 'rotateY(-6deg)', transformStyle: 'preserve-3d', transformOrigin: 'center center' }}
                >
                  {/* Dark surround / theater wall */}
                  <div className="bg-gradient-to-b from-gray-950 via-gray-900 to-gray-950 p-6 pt-4 pb-5 rounded-xl shadow-[0_0_60px_rgba(0,0,0,0.6)]">
                    {/* Curtain drapes - left */}
                    <div className="absolute left-0 top-0 bottom-0 w-6 z-10" style={{
                      background: 'linear-gradient(90deg, #7f1d1d 0%, #991b1b 40%, #450a0a 100%)',
                      borderRadius: '12px 0 0 12px',
                    }}>
                      <div className="absolute inset-0 opacity-30" style={{
                        backgroundImage: 'repeating-linear-gradient(180deg, transparent, transparent 8px, rgba(0,0,0,0.3) 8px, rgba(0,0,0,0.3) 16px)',
                      }} />
                    </div>
                    {/* Curtain drapes - right */}
                    <div className="absolute right-0 top-0 bottom-0 w-6 z-10" style={{
                      background: 'linear-gradient(270deg, #7f1d1d 0%, #991b1b 40%, #450a0a 100%)',
                      borderRadius: '0 12px 12px 0',
                    }}>
                      <div className="absolute inset-0 opacity-30" style={{
                        backgroundImage: 'repeating-linear-gradient(180deg, transparent, transparent 8px, rgba(0,0,0,0.3) 8px, rgba(0,0,0,0.3) 16px)',
                      }} />
                    </div>

                    {/* Top curtain valance */}
                    <div className="absolute left-6 right-6 top-0 h-3 z-10" style={{
                      background: 'linear-gradient(180deg, #7f1d1d 0%, #991b1b 60%, transparent 100%)',
                      borderRadius: '8px 8px 0 0',
                    }} />

                    {/* Screen area */}
                    <div className="relative w-full aspect-video bg-black rounded-sm overflow-hidden ml-2 mr-2">
                      {/* Ambient glow behind screen */}
                      <div className="absolute -inset-2 bg-gradient-to-r from-orange-500/10 via-rose-500/10 to-emerald-500/10 blur-xl" />

                      {/* Screen content */}
                      <div className="relative w-full h-full bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900 overflow-hidden">
                        <div className="absolute inset-0 bg-gradient-to-r from-orange-600/20 via-rose-600/15 to-emerald-600/20 animate-pulse" />

                        <div className="absolute inset-0 flex flex-col items-center justify-center text-white">
                          <Film className="w-14 h-14 mb-3 opacity-30" />
                          <p className="text-xl font-bold opacity-80 tracking-widest uppercase">Now Showing</p>
                          <p className="text-sm opacity-50 mt-2">Event Highlights Coming Soon</p>
                          <div className="flex gap-2 mt-4">
                            <div className="w-2 h-2 rounded-full bg-white/20 animate-pulse" />
                            <div className="w-2 h-2 rounded-full bg-white/30 animate-pulse" style={{ animationDelay: '0.3s' }} />
                            <div className="w-2 h-2 rounded-full bg-white/20 animate-pulse" style={{ animationDelay: '0.6s' }} />
                          </div>
                        </div>

                        {/* Film grain */}
                        <div className="absolute inset-0 pointer-events-none opacity-[0.03]" style={{
                          backgroundImage: 'url("data:image/svg+xml,%3Csvg viewBox=\'0 0 256 256\' xmlns=\'http://www.w3.org/2000/svg\'%3E%3Cfilter id=\'n\'%3E%3CfeTurbulence type=\'fractalNoise\' baseFrequency=\'0.9\' numOctaves=\'4\' stitchTiles=\'stitch\'/%3E%3C/filter%3E%3Crect width=\'100%25\' height=\'100%25\' filter=\'url(%23n)\'/%3E%3C/svg%3E")',
                        }} />

                        {/* Vignette effect */}
                        <div className="absolute inset-0 pointer-events-none" style={{
                          background: 'radial-gradient(ellipse at center, transparent 50%, rgba(0,0,0,0.4) 100%)',
                        }} />
                      </div>
                    </div>
                  </div>
                </div>
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
