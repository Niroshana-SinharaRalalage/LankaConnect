'use client';

import * as React from 'react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Monitor } from 'lucide-react';
import { useFeaturedEvents } from '@/presentation/hooks/useEvents';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useGeolocation } from '@/presentation/hooks/useGeolocation';
import { useCommunityStats } from '@/presentation/hooks/useStats';
import { usePublishedNewsletters } from '@/presentation/hooks/useNewsletters';
import { HeroLeftContent } from '@/presentation/components/features/landing/HeroLeftContent';
import { EventScroller } from '@/presentation/components/features/landing/EventScroller';
import { BelowBannerContent } from '@/presentation/components/features/landing/BelowBannerContent';

/**
 * Landing Page Variant 2: TV Display
 * Angled TV/monitor mockup with placeholder for future video clips
 */
export default function LandingPage2() {
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

            {/* Right - TV Display + Event Scroller */}
            <div className="relative hidden lg:flex flex-col gap-3">
              {/* TV Screen Mockup */}
              <div className="relative ml-auto" style={{ perspective: '800px', width: '85%' }}>
                <div
                  className="relative rounded-lg overflow-hidden shadow-2xl border-4 border-gray-800"
                  style={{ transform: 'rotateY(-18deg) rotateX(3deg)', transformStyle: 'preserve-3d', transformOrigin: 'right center' }}
                >
                  <div className="bg-gray-900 p-2">
                    <div className="flex items-center justify-center pb-1">
                      <div className="w-2 h-2 rounded-full bg-gray-600" />
                    </div>
                    <div className="relative w-full aspect-video bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900 rounded overflow-hidden">
                      <div className="absolute inset-0 bg-gradient-to-r from-orange-600/30 via-rose-600/20 to-emerald-600/30 animate-pulse" />
                      <div className="absolute inset-0 flex flex-col items-center justify-center text-white">
                        <Monitor className="w-12 h-12 mb-3 opacity-40" />
                        <p className="text-lg font-bold opacity-80 tracking-wider">EVENT HIGHLIGHTS</p>
                        <p className="text-sm opacity-50 mt-1">Coming Soon</p>
                        <div className="flex gap-3 mt-4">
                          {[3, 2, 1].map((num) => (
                            <div key={num} className="w-8 h-8 rounded-full border-2 border-white/30 flex items-center justify-center text-white/40 text-sm font-mono">{num}</div>
                          ))}
                        </div>
                      </div>
                      <div className="absolute inset-0 pointer-events-none opacity-10" style={{ backgroundImage: 'repeating-linear-gradient(0deg, transparent, transparent 2px, rgba(0,0,0,0.3) 2px, rgba(0,0,0,0.3) 4px)' }} />
                      <div className="absolute inset-0 bg-gradient-to-br from-white/5 via-transparent to-transparent pointer-events-none" />
                    </div>
                  </div>
                  <div className="bg-gray-800 h-2 flex items-center justify-center">
                    <div className="w-16 h-1 bg-gray-600 rounded" />
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
