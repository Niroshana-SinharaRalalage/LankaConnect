'use client';

import * as React from 'react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Badge } from '@/presentation/components/ui/Badge';
import {
  Sparkles,
  ArrowRight,
  Calendar,
  Clock,
  Store,
  MessageSquare,
  Newspaper,
  ShoppingBag,
  ChevronLeft,
  ChevronRight,
  Monitor,
} from 'lucide-react';
import { MarketplaceItemCard } from '@/presentation/components/widgets/MarketplaceItemCard';
import { MARKETPLACE_ITEMS } from '@/config/marketplaceItems';
import { useFeaturedEvents } from '@/presentation/hooks/useEvents';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useGeolocation } from '@/presentation/hooks/useGeolocation';
import { useCommunityStats } from '@/presentation/hooks/useStats';
import { usePublishedNewsletters } from '@/presentation/hooks/useNewsletters';

type AnimationMode = 'auto-scroll' | 'slide-in' | 'carousel';

export default function LandingPage2() {
  const { user } = useAuthStore();
  const isAnonymous = !user?.userId;
  const { latitude, longitude, loading: locationLoading } = useGeolocation(isAnonymous);

  const {
    data: featuredEvents,
    isLoading: eventsLoading,
    error: eventsError,
  } = useFeaturedEvents(
    user?.userId,
    isAnonymous ? latitude ?? undefined : undefined,
    isAnonymous ? longitude ?? undefined : undefined
  );

  const { data: stats, isLoading: statsLoading } = useCommunityStats();
  const { data: newsletters, isLoading: newslettersLoading } = usePublishedNewsletters();

  const [animationMode, setAnimationMode] = React.useState<AnimationMode>('auto-scroll');
  const [carouselIndex, setCarouselIndex] = React.useState(0);
  const [slideInIndex, setSlideInIndex] = React.useState(0);
  const [slideDirection, setSlideDirection] = React.useState<'enter' | 'exit'>('enter');

  const events = featuredEvents || [];

  // Auto-advance for carousel mode
  React.useEffect(() => {
    if (animationMode !== 'carousel' || events.length === 0) return;
    const interval = setInterval(() => {
      setCarouselIndex((prev) => (prev + 1) % events.length);
    }, 4000);
    return () => clearInterval(interval);
  }, [animationMode, events.length]);

  // Slide-in one by one
  React.useEffect(() => {
    if (animationMode !== 'slide-in' || events.length === 0) return;
    const interval = setInterval(() => {
      setSlideDirection('exit');
      setTimeout(() => {
        setSlideInIndex((prev) => (prev + 1) % events.length);
        setSlideDirection('enter');
      }, 500);
    }, 3500);
    return () => clearInterval(interval);
  }, [animationMode, events.length]);

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

  const getNewsletterExcerpt = (html: string, maxLength: number = 100): string => {
    const text = html.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
    return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
  };

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

  const renderEventCard = (event: typeof events[0], index: number) => {
    const primaryImage = event.images?.find((img) => img.isPrimary) || event.images?.[0];
    const hasImage = primaryImage?.imageUrl;
    const gradients = [
      'from-orange-600 via-rose-600 to-amber-500',
      'from-emerald-600 via-teal-600 to-cyan-500',
      'from-rose-600 via-pink-600 to-purple-500',
      'from-indigo-600 via-blue-600 to-cyan-500',
    ];

    return (
      <div
        key={event.id}
        className="group relative min-w-[220px] h-36 rounded-2xl shadow-lg hover:shadow-2xl transition-all hover:-translate-y-1 hover:scale-[1.02] cursor-pointer overflow-hidden ring-2 ring-white/40 hover:ring-white/70 flex-shrink-0"
        onClick={() => (window.location.href = `/events/${event.id}`)}
      >
        {hasImage ? (
          <img
            src={primaryImage.imageUrl}
            alt={event.title}
            className="absolute inset-0 w-full h-full object-cover transition-transform duration-500 group-hover:scale-110"
          />
        ) : (
          <div
            className={`absolute inset-0 bg-gradient-to-br ${gradients[index % 4]} flex items-center justify-center`}
          >
            <span className="text-5xl opacity-30">🎉</span>
          </div>
        )}
        <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent" />
        <div className="absolute inset-0 p-3 flex flex-col justify-end">
          <h3 className="text-white font-bold text-sm leading-tight line-clamp-2 drop-shadow-lg mb-1">
            {event.title}
          </h3>
          <div className="flex items-center gap-1.5 text-white/90 text-xs">
            <Calendar className="h-3 w-3" />
            <span>
              {new Date(event.startDate).toLocaleDateString('en-US', {
                month: 'short',
                day: 'numeric',
              })}{' '}
              at{' '}
              {new Date(event.startDate).toLocaleTimeString('en-US', {
                hour: 'numeric',
                minute: '2-digit',
              })}
            </span>
          </div>
        </div>
      </div>
    );
  };

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Hero Section - Cinematic Design */}
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

        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 md:py-20">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 items-start">
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

              {/* Community Statistics */}
              {statsLoading ? (
                <div className="grid grid-cols-3 gap-6 mt-8 pt-8 border-t border-white/20">
                  {[...Array(3)].map((_, i) => (
                    <div key={i}>
                      <div className="h-9 w-20 bg-white/20 rounded animate-pulse mb-1"></div>
                      <div className="h-4 w-16 bg-white/10 rounded animate-pulse"></div>
                    </div>
                  ))}
                </div>
              ) : stats &&
                (stats.totalUsers > 0 || stats.totalEvents > 0 || stats.totalBusinesses > 0) ? (
                <div className="grid grid-cols-3 gap-6 mt-8 pt-8 border-t border-white/20">
                  {stats.totalUsers > 0 && (
                    <div>
                      <div className="text-3xl text-white mb-1">
                        {formatCount(stats.totalUsers)}
                      </div>
                      <div className="text-sm text-white/90">Members</div>
                    </div>
                  )}
                  {stats.totalEvents > 0 && (
                    <div>
                      <div className="text-3xl text-white mb-1">
                        {formatCount(stats.totalEvents)}
                      </div>
                      <div className="text-sm text-white/90">Events</div>
                    </div>
                  )}
                  {stats.totalBusinesses > 0 && (
                    <div>
                      <div className="text-3xl text-white mb-1">
                        {formatCount(stats.totalBusinesses)}
                      </div>
                      <div className="text-sm text-white/90">Businesses</div>
                    </div>
                  )}
                </div>
              ) : null}
            </div>

            {/* Right - Cinematic Area */}
            <div className="relative hidden lg:flex flex-col gap-4">
              {/* Top-Right: Cinema Screen Mockup */}
              <div className="relative" style={{ perspective: '1200px' }}>
                <div
                  className="relative rounded-lg overflow-hidden shadow-2xl border-4 border-gray-800"
                  style={{
                    transform: 'rotateY(-8deg) rotateX(2deg)',
                    transformStyle: 'preserve-3d',
                  }}
                >
                  {/* Screen bezel */}
                  <div className="bg-gray-900 p-2">
                    {/* Top bezel with camera dot */}
                    <div className="flex items-center justify-center pb-1">
                      <div className="w-2 h-2 rounded-full bg-gray-600"></div>
                    </div>
                    {/* Screen content */}
                    <div className="relative w-full aspect-video bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900 rounded overflow-hidden">
                      {/* Animated gradient background */}
                      <div className="absolute inset-0 bg-gradient-to-r from-orange-600/30 via-rose-600/20 to-emerald-600/30 animate-pulse" />

                      {/* Film grain overlay */}
                      <div
                        className="absolute inset-0 opacity-5"
                        style={{
                          backgroundImage:
                            'url("data:image/svg+xml,%3Csvg viewBox=\'0 0 256 256\' xmlns=\'http://www.w3.org/2000/svg\'%3E%3Cfilter id=\'noise\'%3E%3CfeTurbulence type=\'fractalNoise\' baseFrequency=\'0.9\' numOctaves=\'4\' stitchTiles=\'stitch\'/%3E%3C/filter%3E%3Crect width=\'100%25\' height=\'100%25\' filter=\'url(%23noise)\'/%3E%3C/svg%3E")',
                        }}
                      />

                      {/* Screen content - placeholder */}
                      <div className="absolute inset-0 flex flex-col items-center justify-center text-white">
                        <Monitor className="w-12 h-12 mb-3 opacity-40" />
                        <p className="text-lg font-bold opacity-80 tracking-wider">
                          EVENT HIGHLIGHTS
                        </p>
                        <p className="text-sm opacity-50 mt-1">Coming Soon</p>
                        {/* Film countdown circles */}
                        <div className="flex gap-3 mt-4">
                          {[3, 2, 1].map((num) => (
                            <div
                              key={num}
                              className="w-8 h-8 rounded-full border-2 border-white/30 flex items-center justify-center text-white/40 text-sm font-mono"
                            >
                              {num}
                            </div>
                          ))}
                        </div>
                      </div>

                      {/* Scan lines effect */}
                      <div
                        className="absolute inset-0 pointer-events-none opacity-10"
                        style={{
                          backgroundImage:
                            'repeating-linear-gradient(0deg, transparent, transparent 2px, rgba(0,0,0,0.3) 2px, rgba(0,0,0,0.3) 4px)',
                        }}
                      />

                      {/* Screen reflection */}
                      <div className="absolute inset-0 bg-gradient-to-br from-white/5 via-transparent to-transparent pointer-events-none" />
                    </div>
                  </div>

                  {/* Monitor stand base */}
                  <div className="bg-gray-800 h-2 flex items-center justify-center">
                    <div className="w-16 h-1 bg-gray-600 rounded"></div>
                  </div>
                </div>
              </div>

              {/* Bottom-Right: Scrolling Event Cards */}
              <div className="mt-2">
                {/* Animation Mode Selector */}
                <div className="flex items-center justify-between mb-3">
                  <span className="text-white/70 text-xs font-medium uppercase tracking-wider">
                    Featured Events
                  </span>
                  <select
                    value={animationMode}
                    onChange={(e) => setAnimationMode(e.target.value as AnimationMode)}
                    className="text-xs bg-white/10 border border-white/20 text-white rounded-md px-2 py-1 backdrop-blur-sm focus:outline-none focus:ring-1 focus:ring-white/30"
                  >
                    <option value="auto-scroll" className="text-gray-900">
                      Auto Scroll
                    </option>
                    <option value="slide-in" className="text-gray-900">
                      Slide In
                    </option>
                    <option value="carousel" className="text-gray-900">
                      Carousel
                    </option>
                  </select>
                </div>

                {eventsLoading || (isAnonymous && locationLoading) ? (
                  <div className="flex gap-3 overflow-hidden">
                    {[...Array(3)].map((_, i) => (
                      <div
                        key={i}
                        className="min-w-[220px] h-36 rounded-2xl animate-pulse bg-white/10 ring-2 ring-white/20"
                      />
                    ))}
                  </div>
                ) : eventsError || events.length === 0 ? (
                  <div className="flex items-center justify-center h-36 rounded-2xl bg-white/10 ring-2 ring-white/20">
                    <p className="text-white/60 text-sm">No events to display</p>
                  </div>
                ) : animationMode === 'auto-scroll' ? (
                  /* Horizontal Auto-Scroll (Marquee) */
                  <div className="overflow-hidden relative">
                    <div className="flex gap-3 animate-marquee">
                      {/* Duplicate events for seamless loop */}
                      {[...events, ...events].map((event, index) =>
                        renderEventCard(event, index % events.length)
                      )}
                    </div>
                  </div>
                ) : animationMode === 'slide-in' ? (
                  /* Slide In One by One */
                  <div className="relative h-36 overflow-hidden">
                    {events.length > 0 && (
                      <div
                        className={`absolute inset-0 transition-all duration-500 ease-in-out ${
                          slideDirection === 'enter'
                            ? 'translate-x-0 opacity-100'
                            : 'translate-x-full opacity-0'
                        }`}
                      >
                        {renderEventCard(events[slideInIndex], slideInIndex)}
                      </div>
                    )}
                    {/* Slide indicators */}
                    <div className="absolute bottom-2 left-1/2 -translate-x-1/2 flex gap-1.5 z-10">
                      {events.map((_, i) => (
                        <div
                          key={i}
                          className={`w-1.5 h-1.5 rounded-full transition-colors ${
                            i === slideInIndex ? 'bg-white' : 'bg-white/30'
                          }`}
                        />
                      ))}
                    </div>
                  </div>
                ) : (
                  /* Carousel with Dots */
                  <div className="relative">
                    <div className="overflow-hidden">
                      <div
                        className="flex transition-transform duration-500 ease-in-out gap-3"
                        style={{
                          transform: `translateX(-${carouselIndex * (220 + 12)}px)`,
                        }}
                      >
                        {events.map((event, index) => renderEventCard(event, index))}
                      </div>
                    </div>

                    {/* Navigation arrows */}
                    {events.length > 1 && (
                      <>
                        <button
                          onClick={() =>
                            setCarouselIndex(
                              (prev) => (prev - 1 + events.length) % events.length
                            )
                          }
                          className="absolute -left-3 top-1/2 -translate-y-1/2 w-7 h-7 rounded-full bg-white/20 backdrop-blur-sm flex items-center justify-center text-white hover:bg-white/30 transition-colors"
                        >
                          <ChevronLeft className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() =>
                            setCarouselIndex((prev) => (prev + 1) % events.length)
                          }
                          className="absolute -right-3 top-1/2 -translate-y-1/2 w-7 h-7 rounded-full bg-white/20 backdrop-blur-sm flex items-center justify-center text-white hover:bg-white/30 transition-colors"
                        >
                          <ChevronRight className="w-4 h-4" />
                        </button>
                      </>
                    )}

                    {/* Dot indicators */}
                    <div className="flex justify-center gap-1.5 mt-3">
                      {events.map((_, i) => (
                        <button
                          key={i}
                          onClick={() => setCarouselIndex(i)}
                          className={`w-2 h-2 rounded-full transition-all ${
                            i === carouselIndex
                              ? 'bg-white w-4'
                              : 'bg-white/30 hover:bg-white/50'
                          }`}
                        />
                      ))}
                    </div>
                  </div>
                )}
              </div>

              {/* View All Events Button */}
              <div className="flex justify-center mt-2">
                <a
                  href="/events"
                  className="inline-flex items-center justify-center px-8 py-3 bg-white text-orange-600 hover:bg-neutral-100 shadow-lg rounded-lg font-semibold transition-all"
                >
                  <Calendar className="mr-2 h-5 w-5" />
                  View All Events
                </a>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
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

                {/* Latest News & Updates */}
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
                      <>
                        {[...Array(2)].map((_, i) => (
                          <div
                            key={i}
                            className="rounded-xl border border-neutral-200 bg-white p-4 animate-pulse"
                          >
                            <div className="h-5 w-20 bg-neutral-200 rounded mb-3"></div>
                            <div className="h-5 w-3/4 bg-neutral-200 rounded mb-2"></div>
                            <div className="h-4 w-full bg-neutral-100 rounded mb-2"></div>
                            <div className="h-3 w-16 bg-neutral-100 rounded"></div>
                          </div>
                        ))}
                      </>
                    ) : newsletters && newsletters.length > 0 ? (
                      newsletters.slice(0, 2).map((newsletter) => (
                        <div
                          key={newsletter.id}
                          onClick={() =>
                            (window.location.href = `/newsletters/${newsletter.id}`)
                          }
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
                            <span>
                              {getRelativeTime(
                                newsletter.publishedAt || newsletter.createdAt
                              )}
                            </span>
                          </div>
                        </div>
                      ))
                    ) : (
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

      {/* Custom CSS for marquee animation */}
      <style jsx>{`
        @keyframes marquee {
          0% {
            transform: translateX(0);
          }
          100% {
            transform: translateX(-50%);
          }
        }
        .animate-marquee {
          animation: marquee 20s linear infinite;
        }
        .animate-marquee:hover {
          animation-play-state: paused;
        }
      `}</style>
    </div>
  );
}
