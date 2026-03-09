'use client';

import * as React from 'react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Badge } from '@/presentation/components/ui/Badge';
import { Sparkles, ArrowRight, Calendar, Users, Clock, Store, MessageSquare, Newspaper, ShoppingBag, Monitor } from 'lucide-react';
import { MarketplaceItemCard } from '@/presentation/components/widgets/MarketplaceItemCard';
import { MARKETPLACE_ITEMS } from '@/config/marketplaceItems';
import { useFeaturedEvents } from '@/presentation/hooks/useEvents';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useGeolocation } from '@/presentation/hooks/useGeolocation';
import { useCommunityStats } from '@/presentation/hooks/useStats';
import { usePublishedNewsletters } from '@/presentation/hooks/useNewsletters';

export default function Home() {
  const { user } = useAuthStore();

  // For anonymous users, detect location via IP/browser geolocation
  const isAnonymous = !user?.userId;
  const { latitude, longitude, loading: locationLoading } = useGeolocation(isAnonymous);

  // Fetch featured events with location-based sorting
  const { data: featuredEvents, isLoading: eventsLoading, error: eventsError } = useFeaturedEvents(
    user?.userId,
    isAnonymous ? latitude ?? undefined : undefined,
    isAnonymous ? longitude ?? undefined : undefined
  );

  // Phase 6A.69: Fetch real-time community statistics
  const { data: stats, isLoading: statsLoading } = useCommunityStats();

  // Phase 6A.74: Fetch published newsletters for News & Updates section
  const { data: newsletters, isLoading: newslettersLoading } = usePublishedNewsletters();

  // Format number for display (1234 → "1.2K+", 25678 → "25.6K+")
  const formatCount = (count: number): string => {
    if (count >= 1000) {
      const k = Math.floor(count / 1000);
      const remainder = count % 1000;
      if (remainder >= 100) {
        return `${k}.${Math.floor(remainder / 100)}K+`;
      }
      return `${k}K+`;
    }
    return count.toString();
  };

  // Phase 6A.74: Strip HTML tags and get excerpt for newsletter preview
  const getNewsletterExcerpt = (html: string, maxLength: number = 100): string => {
    const text = html.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
    return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
  };

  // Phase 6A.74: Format relative time for newsletter display
  const getRelativeTime = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffHours < 1) return 'Just now';
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  };

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Hero Section - Exact Figma Design */}
      <div className="relative overflow-hidden bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800">
        {/* Decorative Background Pattern */}
        <div className="absolute inset-0 opacity-10">
          <div
            className="absolute inset-0"
            style={{
              backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
            }}
          ></div>
        </div>

        {/* Decorative gradient blobs */}
        <div className="absolute inset-0 overflow-hidden">
          <div className="absolute -top-24 -left-24 w-96 h-96 bg-orange-400/20 rounded-full blur-3xl"></div>
          <div className="absolute -bottom-24 -right-24 w-96 h-96 bg-emerald-400/20 rounded-full blur-3xl"></div>
          <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-rose-400/10 rounded-full blur-3xl"></div>
        </div>

        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 md:py-24">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">
            {/* Left Content */}
            <div className="text-center lg:text-left">
              {/* Badge with Icon */}
              <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-white/20 backdrop-blur-sm border border-white/30 mb-6">
                <Sparkles className="h-4 w-4 text-white" />
                <span className="text-sm text-white">Connecting Sri Lankans Worldwide</span>
              </div>

              {/* Heading */}
              <h1 className="text-4xl md:text-5xl lg:text-6xl text-white mb-6">
                One Country,
                <br />
                <span className="text-white drop-shadow-lg">One Community</span>
              </h1>

              {/* Description */}
              <p className="text-lg text-white/95 mb-8 max-w-xl mx-auto lg:mx-0">
                Join the largest Sri Lankan community platform. Discover events, connect
                with businesses, engage in discussions, and celebrate our rich culture
                together.
              </p>

              {/* Removed News & Updates button per user request */}

              {/* Phase 6A.69: Real-time Community Statistics */}
              {statsLoading ? (
                <div className="grid grid-cols-3 gap-6 mt-12 pt-12 border-t border-white/20">
                  {[...Array(3)].map((_, i) => (
                    <div key={i}>
                      <div className="h-9 w-20 bg-white/20 rounded animate-pulse mb-1"></div>
                      <div className="h-4 w-16 bg-white/10 rounded animate-pulse"></div>
                    </div>
                  ))}
                </div>
              ) : stats && (stats.totalUsers > 0 || stats.totalEvents > 0 || stats.totalBusinesses > 0) ? (
                <div className="grid grid-cols-3 gap-6 mt-12 pt-12 border-t border-white/20">
                  {stats.totalUsers > 0 && (
                    <div>
                      <div className="text-3xl text-white mb-1">{formatCount(stats.totalUsers)}</div>
                      <div className="text-sm text-white/90">Members</div>
                    </div>
                  )}
                  {stats.totalEvents > 0 && (
                    <div>
                      <div className="text-3xl text-white mb-1">{formatCount(stats.totalEvents)}</div>
                      <div className="text-sm text-white/90">Events</div>
                    </div>
                  )}
                  {stats.totalBusinesses > 0 && (
                    <div>
                      <div className="text-3xl text-white mb-1">{formatCount(stats.totalBusinesses)}</div>
                      <div className="text-sm text-white/90">Businesses</div>
                    </div>
                  )}
                </div>
              ) : null}
            </div>

            {/* Right - Featured Events Cards (from Database) */}
            <div className="relative hidden lg:block">
              {eventsLoading || (isAnonymous && locationLoading) ? (
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-4">
                    {[...Array(2)].map((_, i) => (
                      <div key={i} className="relative h-40 rounded-2xl shadow-lg overflow-hidden animate-pulse bg-gradient-to-br from-neutral-200 to-neutral-300 ring-2 ring-white/40">
                        <div className="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent" />
                        <div className="absolute bottom-4 left-4 right-4">
                          <div className="h-4 bg-white/30 rounded w-3/4 mb-2"></div>
                          <div className="h-3 bg-white/20 rounded w-1/2"></div>
                        </div>
                      </div>
                    ))}
                  </div>
                  <div className="space-y-4 mt-8">
                    {[...Array(2)].map((_, i) => (
                      <div key={i} className="relative h-40 rounded-2xl shadow-lg overflow-hidden animate-pulse bg-gradient-to-br from-neutral-200 to-neutral-300 ring-2 ring-white/40">
                        <div className="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent" />
                        <div className="absolute bottom-4 left-4 right-4">
                          <div className="h-4 bg-white/30 rounded w-3/4 mb-2"></div>
                          <div className="h-3 bg-white/20 rounded w-1/2"></div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ) : eventsError || !featuredEvents || featuredEvents.length === 0 ? (
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-4">
                    <div className="group relative h-40 rounded-2xl shadow-lg overflow-hidden bg-gradient-to-br from-orange-600 via-rose-600 to-amber-500 ring-2 ring-white/40">
                      <div className="absolute inset-0 flex items-center justify-center">
                        <span className="text-6xl opacity-30">🎉</span>
                      </div>
                      <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent" />
                      <div className="absolute inset-0 p-4 flex flex-col justify-end">
                        <h3 className="text-white font-bold text-base drop-shadow-lg mb-1">No Events Yet</h3>
                        <div className="text-white/90 text-sm">Check back soon</div>
                      </div>
                    </div>
                    <div className="group relative h-40 rounded-2xl shadow-lg overflow-hidden bg-gradient-to-br from-emerald-600 via-teal-600 to-cyan-500 ring-2 ring-white/40">
                      <div className="absolute inset-0 flex items-center justify-center">
                        <span className="text-6xl opacity-30">📅</span>
                      </div>
                      <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent" />
                      <div className="absolute inset-0 p-4 flex flex-col justify-end">
                        <h3 className="text-white font-bold text-base drop-shadow-lg mb-1">Coming Soon</h3>
                        <div className="text-white/90 text-sm">New events weekly</div>
                      </div>
                    </div>
                  </div>
                  <div className="space-y-4 mt-8">
                    <div className="group relative h-40 rounded-2xl shadow-lg overflow-hidden bg-gradient-to-br from-rose-600 via-pink-600 to-purple-500 ring-2 ring-white/40">
                      <div className="absolute inset-0 flex items-center justify-center">
                        <span className="text-6xl opacity-30">🎭</span>
                      </div>
                      <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent" />
                      <div className="absolute inset-0 p-4 flex flex-col justify-end">
                        <h3 className="text-white font-bold text-base drop-shadow-lg mb-1">Cultural Events</h3>
                        <div className="text-white/90 text-sm">Stay tuned</div>
                      </div>
                    </div>
                    <div className="group relative h-40 rounded-2xl shadow-lg overflow-hidden bg-gradient-to-br from-indigo-600 via-blue-600 to-cyan-500 ring-2 ring-white/40">
                      <div className="absolute inset-0 flex items-center justify-center">
                        <span className="text-6xl opacity-30">🌟</span>
                      </div>
                      <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent" />
                      <div className="absolute inset-0 p-4 flex flex-col justify-end">
                        <h3 className="text-white font-bold text-base drop-shadow-lg mb-1">Join Community</h3>
                        <div className="text-white/90 text-sm">Connect with us</div>
                      </div>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-4">
                    {featuredEvents.slice(0, 2).map((event, index) => {
                      const primaryImage = event.images?.find(img => img.isPrimary) || event.images?.[0];
                      const hasImage = primaryImage?.imageUrl;
                      const gradients = [
                        'from-orange-600 via-rose-600 to-amber-500',
                        'from-emerald-600 via-teal-600 to-cyan-500',
                      ];
                      const fallbackIcons = ['🎉', '📅'];

                      return (
                        <div
                          key={event.id}
                          className="group relative h-40 rounded-2xl shadow-lg hover:shadow-2xl transition-all hover:-translate-y-1 hover:scale-[1.02] cursor-pointer overflow-hidden ring-2 ring-white/40 hover:ring-white/70"
                          onClick={() => window.location.href = `/events/${event.id}`}
                        >
                          {/* Background Image or Gradient Fallback */}
                          {hasImage ? (
                            <img
                              src={primaryImage.imageUrl}
                              alt={event.title}
                              className="absolute inset-0 w-full h-full object-cover transition-transform duration-500 group-hover:scale-110"
                            />
                          ) : (
                            <div className={`absolute inset-0 bg-gradient-to-br ${gradients[index % 2]} flex items-center justify-center`}>
                              <span className="text-6xl opacity-30">{fallbackIcons[index % 2]}</span>
                            </div>
                          )}

                          {/* Dark Gradient Overlay for Text Readability */}
                          <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent" />

                          {/* Content Overlay */}
                          <div className="absolute inset-0 p-4 flex flex-col justify-end">
                            <h3 className="text-white font-bold text-base leading-tight line-clamp-2 drop-shadow-lg mb-1">
                              {event.title}
                            </h3>
                            <div className="flex items-center gap-2 text-white/90 text-sm">
                              <Calendar className="h-3.5 w-3.5" />
                              <span>
                                {new Date(event.startDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} at {new Date(event.startDate).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}
                              </span>
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
                      const gradients = [
                        'from-rose-600 via-pink-600 to-purple-500',
                        'from-indigo-600 via-blue-600 to-cyan-500',
                      ];
                      const fallbackIcons = ['🎭', '🌟'];

                      return (
                        <div
                          key={event.id}
                          className="group relative h-40 rounded-2xl shadow-lg hover:shadow-2xl transition-all hover:-translate-y-1 hover:scale-[1.02] cursor-pointer overflow-hidden ring-2 ring-white/40 hover:ring-white/70"
                          onClick={() => window.location.href = `/events/${event.id}`}
                        >
                          {/* Background Image or Gradient Fallback */}
                          {hasImage ? (
                            <img
                              src={primaryImage.imageUrl}
                              alt={event.title}
                              className="absolute inset-0 w-full h-full object-cover transition-transform duration-500 group-hover:scale-110"
                            />
                          ) : (
                            <div className={`absolute inset-0 bg-gradient-to-br ${gradients[index % 2]} flex items-center justify-center`}>
                              <span className="text-6xl opacity-30">{fallbackIcons[index % 2]}</span>
                            </div>
                          )}

                          {/* Dark Gradient Overlay for Text Readability */}
                          <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent" />

                          {/* Content Overlay */}
                          <div className="absolute inset-0 p-4 flex flex-col justify-end">
                            <h3 className="text-white font-bold text-base leading-tight line-clamp-2 drop-shadow-lg mb-1">
                              {event.title}
                            </h3>
                            <div className="flex items-center gap-2 text-white/90 text-sm">
                              <Calendar className="h-3.5 w-3.5" />
                              <span>
                                {new Date(event.startDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} at {new Date(event.startDate).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}
                              </span>
                            </div>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}

              {/* View All Events Button - Below feature cards */}
              <div className="mt-6 flex justify-center">
                <a href="/events" className="inline-flex items-center justify-center px-8 py-3 bg-white text-orange-600 hover:bg-neutral-100 shadow-lg rounded-lg font-semibold transition-all">
                  <Calendar className="mr-2 h-5 w-5" />
                  View All Events
                </a>
              </div>
            </div>
          </div>
        </div>
      </div>


      {/* Preview New Landing Page Banner */}
      <div className="bg-gradient-to-r from-gray-900 to-gray-800 py-3">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex items-center justify-center gap-3">
          <Monitor className="h-4 w-4 text-amber-400" />
          <span className="text-white/80 text-sm">Check out our new cinematic landing page design!</span>
          <a
            href="/landing2"
            className="inline-flex items-center gap-1 px-4 py-1.5 bg-amber-500 hover:bg-amber-400 text-gray-900 text-sm font-semibold rounded-full transition-colors"
          >
            Preview New Design
            <ArrowRight className="h-3.5 w-3.5" />
          </a>
        </div>
      </div>

      {/* Main Content */}
      {/* Phase 6A.X Issue #44: Use max-w-7xl for consistent width with header */}
      <section className="py-16 bg-neutral-50">
        <div className="max-w-7xl mx-auto px-6 lg:px-8">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
            {/* Left Column - Forum Highlights + News (stacked) then Business */}
            <div className="lg:col-span-2 space-y-8">
              {/* Forum Highlights and News & Updates - Side by side */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                {/* Forum Highlights */}
                <Card className="border-neutral-200 shadow-sm">
                  <CardHeader className="flex flex-row items-center justify-between px-6 py-4 border-b border-neutral-100">
                    <CardTitle className="flex items-center gap-2 text-neutral-900 text-lg font-semibold">
                      <MessageSquare className="h-5 w-5 text-rose-600" />
                      Forum Highlights
                    </CardTitle>
                    <button className="text-rose-600 hover:text-rose-700">
                      <ArrowRight className="h-5 w-5" />
                    </button>
                  </CardHeader>

                  <CardContent className="p-6 space-y-4">
                    <div className="text-center py-6 text-neutral-500">
                      <MessageSquare className="h-10 w-10 mx-auto mb-2 text-neutral-300" />
                      <p className="text-sm">No forum discussions yet</p>
                      <p className="text-xs text-neutral-400 mt-1">Coming soon</p>
                    </div>
                  </CardContent>
                </Card>

                {/* Latest News & Updates - Phase 6A.74 Part 10: Dynamic newsletters from database */}
                <Card className="border-neutral-200 shadow-sm">
                  <CardHeader className="flex flex-row items-center justify-between px-6 py-4 border-b border-neutral-100">
                    <CardTitle className="flex items-center gap-2 text-neutral-900 text-lg font-semibold">
                      <Newspaper className="h-5 w-5 text-amber-600" />
                      Latest News & Updates
                    </CardTitle>
                    <a href="/newsletters" className="text-amber-600 hover:text-amber-700">
                      <ArrowRight className="h-5 w-5" />
                    </a>
                  </CardHeader>

                  <CardContent className="p-6 space-y-4">
                    {newslettersLoading ? (
                      // Loading skeleton
                      <>
                        {[...Array(2)].map((_, i) => (
                          <div key={i} className="rounded-xl border border-neutral-200 bg-white p-4 animate-pulse">
                            <div className="h-5 w-20 bg-neutral-200 rounded mb-3"></div>
                            <div className="h-5 w-3/4 bg-neutral-200 rounded mb-2"></div>
                            <div className="h-4 w-full bg-neutral-100 rounded mb-2"></div>
                            <div className="h-3 w-16 bg-neutral-100 rounded"></div>
                          </div>
                        ))}
                      </>
                    ) : newsletters && newsletters.length > 0 ? (
                      // Real newsletter data
                      newsletters.slice(0, 2).map((newsletter) => (
                        <div
                          key={newsletter.id}
                          onClick={() => window.location.href = `/newsletters/${newsletter.id}`}
                          className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-amber-200 transition-all hover:shadow-md bg-white p-4 cursor-pointer"
                        >
                          <Badge variant={newsletter.eventId ? 'community' : 'business'}>
                            {newsletter.eventId ? 'Event Update' : 'News'}
                          </Badge>
                          <h3 className="font-semibold text-neutral-900 mt-3 mb-2 leading-snug group-hover:text-amber-600 transition-colors line-clamp-2">
                            {newsletter.title}
                          </h3>
                          <p className="text-sm text-neutral-600 mb-2 line-clamp-2">
                            {getNewsletterExcerpt(newsletter.description)}
                          </p>
                          <div className="flex items-center gap-1 text-xs text-neutral-500">
                            <Clock className="h-3 w-3" />
                            <span>{getRelativeTime(newsletter.publishedAt || newsletter.createdAt)}</span>
                          </div>
                        </div>
                      ))
                    ) : (
                      // Empty state
                      <div className="text-center py-6 text-neutral-500">
                        <Newspaper className="h-10 w-10 mx-auto mb-2 text-neutral-300" />
                        <p className="text-sm">No news available</p>
                      </div>
                    )}
                  </CardContent>
                </Card>
              </div>

              {/* Business Section */}
              <Card className="border-neutral-200 shadow-sm">
                <CardHeader className="flex flex-row items-center justify-between px-6 py-4 border-b border-neutral-100">
                  <CardTitle className="flex items-center gap-2 text-neutral-900 text-lg font-semibold">
                    <Store className="h-5 w-5 text-emerald-600" />
                    Business
                  </CardTitle>
                  <button className="text-emerald-600 hover:text-emerald-700 font-semibold flex items-center gap-1 text-sm">
                    Browse All
                    <ArrowRight className="h-4 w-4" />
                  </button>
                </CardHeader>

                <CardContent className="p-6">
                  <div className="text-center py-6 text-neutral-500">
                    <Store className="h-10 w-10 mx-auto mb-2 text-neutral-300" />
                    <p className="text-sm">No businesses listed yet</p>
                    <p className="text-xs text-neutral-400 mt-1">Coming soon</p>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Right Sidebar - Marketplace */}
            <div>
              <Card className="border-neutral-200 shadow-sm">
                <CardHeader className="flex flex-row items-center justify-between px-6 py-4 border-b border-neutral-100">
                  <CardTitle className="flex items-center gap-2 text-neutral-900 text-lg font-semibold">
                    <ShoppingBag className="h-5 w-5 text-emerald-600" />
                    Marketplace
                  </CardTitle>
                  <a href="/marketplace" className="text-emerald-600 hover:text-emerald-700">
                    <ArrowRight className="h-5 w-5" />
                  </a>
                </CardHeader>

                <CardContent className="p-3">
                  <div className="grid grid-cols-2 gap-2">
                    {MARKETPLACE_ITEMS.map((item) => (
                      <MarketplaceItemCard key={item.id} item={item} compact />
                    ))}
                  </div>
                  <a
                    href="/marketplace"
                    className="block text-center text-sm text-emerald-600 hover:text-emerald-700 font-medium py-3 mt-3 border-t border-neutral-100"
                  >
                    View All Details & Order
                  </a>
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </section>

      <Footer />
    </div>
  );
}
