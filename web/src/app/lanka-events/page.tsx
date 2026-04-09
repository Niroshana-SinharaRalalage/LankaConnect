'use client';

import * as React from 'react';
import { LankaEventsHeader } from '@/presentation/components/layout/LankaEventsHeader';
import Footer from '@/presentation/components/layout/Footer';
import { FeaturedNewslettersCarousel } from '@/presentation/components/features/newsletters/FeaturedNewslettersCarousel';
import { Sparkles, ArrowRight, Calendar } from 'lucide-react';
import { useFeaturedEvents } from '@/presentation/hooks/useEvents';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useGeolocation } from '@/presentation/hooks/useGeolocation';

export default function LankaEventsHome() {
  const { user } = useAuthStore();

  const isAnonymous = !user?.userId;
  const { latitude, longitude, loading: locationLoading } = useGeolocation(isAnonymous);

  const { data: featuredEvents, isLoading: eventsLoading, error: eventsError } = useFeaturedEvents(
    user?.userId,
    isAnonymous ? latitude ?? undefined : undefined,
    isAnonymous ? longitude ?? undefined : undefined
  );

  return (
    // Single continuous gradient — no dark gap before footer
    <div className="min-h-screen bg-gradient-to-br from-orange-600 via-rose-800 to-emerald-900">
      <LankaEventsHeader />

      {/* ── Hero Section ─────────────────────────────────────────────────── */}
      <div className="relative overflow-hidden">
        {/* Decorative Background Pattern */}
        <div className="absolute inset-0 opacity-10 pointer-events-none">
          <div
            className="absolute inset-0"
            style={{
              backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
            }}
          />
        </div>

        {/* Decorative gradient blobs */}
        <div className="absolute inset-0 overflow-hidden pointer-events-none">
          <div className="absolute -top-24 -left-24 w-96 h-96 bg-orange-400/20 rounded-full blur-3xl" />
          <div className="absolute -bottom-24 -right-24 w-96 h-96 bg-emerald-400/20 rounded-full blur-3xl" />
          <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-rose-400/10 rounded-full blur-3xl" />
        </div>

        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 md:py-24">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">

            {/* ── Left Content ───────────────────────────────────────────── */}
            <div className="text-center lg:text-left">
              <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-white/20 backdrop-blur-sm border border-white/30 mb-6">
                <Sparkles className="h-4 w-4 text-white" />
                <span className="text-sm text-white">Connecting Sri Lankans Worldwide</span>
              </div>

              <h1 className="text-4xl md:text-5xl lg:text-6xl text-white mb-6">
                One Country,
                <br />
                <span className="text-white drop-shadow-lg">One Community</span>
              </h1>

              <p className="text-lg text-white/95 mb-8 max-w-xl mx-auto lg:mx-0">
                Join the largest Sri Lankan community platform. Discover events, connect
                with businesses, engage in discussions, and celebrate our rich culture
                together.
              </p>

              {/* Mobile — View All Events CTA */}
              <div className="lg:hidden flex justify-center lg:justify-start">
                <a href="/events" className="inline-flex items-center gap-2 px-6 py-3 bg-white text-orange-600 hover:bg-orange-50 shadow-lg rounded-lg font-semibold transition-all">
                  <Calendar className="h-4 w-4" />
                  View All Events
                  <ArrowRight className="h-4 w-4" />
                </a>
              </div>
            </div>

            {/* ── Right — Featured Events Cards (desktop only) ────────────── */}
            <div className="relative hidden lg:block">
              {eventsLoading || (isAnonymous && locationLoading) ? (
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-4">
                    {[...Array(2)].map((_, i) => (
                      <div key={i} className="relative h-40 rounded-2xl shadow-lg overflow-hidden animate-pulse bg-white/10 ring-2 ring-white/20">
                        <div className="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent" />
                        <div className="absolute bottom-4 left-4 right-4">
                          <div className="h-4 bg-white/30 rounded w-3/4 mb-2" />
                          <div className="h-3 bg-white/20 rounded w-1/2" />
                        </div>
                      </div>
                    ))}
                  </div>
                  <div className="space-y-4 mt-8">
                    {[...Array(2)].map((_, i) => (
                      <div key={i} className="relative h-40 rounded-2xl shadow-lg overflow-hidden animate-pulse bg-white/10 ring-2 ring-white/20">
                        <div className="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent" />
                        <div className="absolute bottom-4 left-4 right-4">
                          <div className="h-4 bg-white/30 rounded w-3/4 mb-2" />
                          <div className="h-3 bg-white/20 rounded w-1/2" />
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ) : eventsError || !featuredEvents || featuredEvents.length === 0 ? (
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-4">
                    {[
                      { emoji: '🎉', title: 'No Events Yet', sub: 'Check back soon', grad: 'from-orange-600 via-rose-600 to-amber-500' },
                      { emoji: '📅', title: 'Coming Soon', sub: 'New events weekly', grad: 'from-emerald-600 via-teal-600 to-cyan-500' },
                    ].map(({ emoji, title, sub, grad }) => (
                      <div key={title} className={`group relative h-40 rounded-2xl shadow-lg overflow-hidden bg-gradient-to-br ${grad} ring-2 ring-white/40`}>
                        <div className="absolute inset-0 flex items-center justify-center"><span className="text-6xl opacity-30">{emoji}</span></div>
                        <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent" />
                        <div className="absolute inset-0 p-4 flex flex-col justify-end">
                          <h3 className="text-white font-bold text-base drop-shadow-lg mb-1">{title}</h3>
                          <div className="text-white/90 text-sm">{sub}</div>
                        </div>
                      </div>
                    ))}
                  </div>
                  <div className="space-y-4 mt-8">
                    {[
                      { emoji: '🎭', title: 'Cultural Events', sub: 'Stay tuned', grad: 'from-rose-600 via-pink-600 to-purple-500' },
                      { emoji: '🌟', title: 'Join Community', sub: 'Connect with us', grad: 'from-indigo-600 via-blue-600 to-cyan-500' },
                    ].map(({ emoji, title, sub, grad }) => (
                      <div key={title} className={`group relative h-40 rounded-2xl shadow-lg overflow-hidden bg-gradient-to-br ${grad} ring-2 ring-white/40`}>
                        <div className="absolute inset-0 flex items-center justify-center"><span className="text-6xl opacity-30">{emoji}</span></div>
                        <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent" />
                        <div className="absolute inset-0 p-4 flex flex-col justify-end">
                          <h3 className="text-white font-bold text-base drop-shadow-lg mb-1">{title}</h3>
                          <div className="text-white/90 text-sm">{sub}</div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ) : (
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-4">
                    {featuredEvents.slice(0, 2).map((event, index) => {
                      const primaryImage = event.images?.find(img => img.isPrimary) || event.images?.[0];
                      const hasImage = primaryImage?.imageUrl;
                      const gradients = ['from-orange-600 via-rose-600 to-amber-500', 'from-emerald-600 via-teal-600 to-cyan-500'];
                      const fallbackIcons = ['🎉', '📅'];
                      return (
                        <div
                          key={event.id}
                          className="group relative h-40 rounded-2xl shadow-lg hover:shadow-2xl transition-all hover:-translate-y-1 hover:scale-[1.02] cursor-pointer overflow-hidden ring-2 ring-white/40 hover:ring-white/70"
                          onClick={() => window.location.href = `/events/${event.id}`}
                        >
                          {hasImage ? (
                            <img src={primaryImage.imageUrl} alt={event.title} className="absolute inset-0 w-full h-full object-cover transition-transform duration-500 group-hover:scale-110" />
                          ) : (
                            <div className={`absolute inset-0 bg-gradient-to-br ${gradients[index % 2]} flex items-center justify-center`}>
                              <span className="text-6xl opacity-30">{fallbackIcons[index % 2]}</span>
                            </div>
                          )}
                          <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent" />
                          <div className="absolute inset-0 p-4 flex flex-col justify-end">
                            <h3 className="text-white font-bold text-base leading-tight line-clamp-2 drop-shadow-lg mb-1">{event.title}</h3>
                            <div className="flex items-center gap-2 text-white/90 text-sm">
                              <Calendar className="h-3.5 w-3.5" />
                              <span>{new Date(event.startDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} at {new Date(event.startDate).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}</span>
                            </div>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                  <div className="space-y-4 mt-8">
                    {featuredEvents.slice(2, 4).map((event, index) => {
                      const primaryImage = event.images?.find(img => img.isPrimary) || event.images?.[0];
                      const hasImage = primaryImage?.imageUrl;
                      const gradients = ['from-rose-600 via-pink-600 to-purple-500', 'from-indigo-600 via-blue-600 to-cyan-500'];
                      const fallbackIcons = ['🎭', '🌟'];
                      return (
                        <div
                          key={event.id}
                          className="group relative h-40 rounded-2xl shadow-lg hover:shadow-2xl transition-all hover:-translate-y-1 hover:scale-[1.02] cursor-pointer overflow-hidden ring-2 ring-white/40 hover:ring-white/70"
                          onClick={() => window.location.href = `/events/${event.id}`}
                        >
                          {hasImage ? (
                            <img src={primaryImage.imageUrl} alt={event.title} className="absolute inset-0 w-full h-full object-cover transition-transform duration-500 group-hover:scale-110" />
                          ) : (
                            <div className={`absolute inset-0 bg-gradient-to-br ${gradients[index % 2]} flex items-center justify-center`}>
                              <span className="text-6xl opacity-30">{fallbackIcons[index % 2]}</span>
                            </div>
                          )}
                          <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent" />
                          <div className="absolute inset-0 p-4 flex flex-col justify-end">
                            <h3 className="text-white font-bold text-base leading-tight line-clamp-2 drop-shadow-lg mb-1">{event.title}</h3>
                            <div className="flex items-center gap-2 text-white/90 text-sm">
                              <Calendar className="h-3.5 w-3.5" />
                              <span>{new Date(event.startDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} at {new Date(event.startDate).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}</span>
                            </div>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}

              {/* View All Events button */}
              <div className="mt-6 flex justify-center">
                <a href="/events" className="inline-flex items-center justify-center px-8 py-3 bg-white text-orange-600 hover:bg-orange-50 shadow-lg rounded-lg font-semibold transition-all">
                  <Calendar className="mr-2 h-5 w-5" />
                  View All Events
                </a>
              </div>
            </div>

          </div>
        </div>
      </div>

      {/* ── Featured Newsletters — full-width below hero, on same gradient ── */}
      <FeaturedNewslettersCarousel />

      {/* ── Footer — transparent, inherits gradient ──────────────────────── */}
      <Footer />
    </div>
  );
}
