'use client';

import * as React from 'react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Play } from 'lucide-react';
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
 * No TV or theater — video plays directly in the right area with vignette shading
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

            {/* Right - Background Video Area + Event Scroller */}
            <div className="relative hidden lg:flex flex-col gap-3">
              {/* Video Area - no frame, just the video with shading */}
              <div className="relative w-full aspect-video rounded-2xl overflow-hidden shadow-2xl">
                {/* Video placeholder background */}
                <div className="absolute inset-0 bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900">
                  {/* Animated color wash simulating video */}
                  <div className="absolute inset-0 overflow-hidden">
                    <div className="absolute inset-0 bg-gradient-to-r from-orange-600/25 via-rose-600/20 to-emerald-600/25 animate-pulse" />
                    <div
                      className="absolute inset-0 opacity-20"
                      style={{
                        background: 'radial-gradient(circle at 30% 40%, rgba(255,121,0,0.3) 0%, transparent 50%), radial-gradient(circle at 70% 60%, rgba(139,21,56,0.3) 0%, transparent 50%)',
                        animation: 'float 6s ease-in-out infinite',
                      }}
                    />
                  </div>

                  {/* Play button overlay */}
                  <div className="absolute inset-0 flex flex-col items-center justify-center text-white z-10">
                    <div className="w-20 h-20 rounded-full bg-white/10 backdrop-blur-sm border-2 border-white/30 flex items-center justify-center mb-4 hover:bg-white/20 transition-colors cursor-pointer">
                      <Play className="w-8 h-8 ml-1 opacity-80" />
                    </div>
                    <p className="text-lg font-bold opacity-80 tracking-wider">EVENT HIGHLIGHTS</p>
                    <p className="text-sm opacity-50 mt-1">Video Coming Soon</p>
                  </div>
                </div>

                {/* Heavy vignette shading around all edges */}
                <div className="absolute inset-0 pointer-events-none" style={{
                  background: 'radial-gradient(ellipse at center, transparent 30%, rgba(0,0,0,0.5) 80%, rgba(0,0,0,0.8) 100%)',
                }} />

                {/* Top edge fade into banner */}
                <div className="absolute top-0 left-0 right-0 h-16 bg-gradient-to-b from-black/50 to-transparent pointer-events-none" />
                {/* Bottom edge fade */}
                <div className="absolute bottom-0 left-0 right-0 h-16 bg-gradient-to-t from-black/50 to-transparent pointer-events-none" />
                {/* Left edge fade */}
                <div className="absolute left-0 top-0 bottom-0 w-12 bg-gradient-to-r from-black/40 to-transparent pointer-events-none" />
                {/* Right edge fade */}
                <div className="absolute right-0 top-0 bottom-0 w-12 bg-gradient-to-l from-black/40 to-transparent pointer-events-none" />
              </div>

              <EventScroller events={events} isLoading={scrollLoading} />
            </div>
          </div>
        </div>
      </div>

      <BelowBannerContent newsletters={newsletters} newslettersLoading={newslettersLoading} />
      <Footer />

      {/* Float animation for video placeholder */}
      <style jsx>{`
        @keyframes float {
          0%, 100% { transform: scale(1) translate(0, 0); }
          33% { transform: scale(1.05) translate(2%, -2%); }
          66% { transform: scale(1.02) translate(-1%, 1%); }
        }
      `}</style>
    </div>
  );
}
