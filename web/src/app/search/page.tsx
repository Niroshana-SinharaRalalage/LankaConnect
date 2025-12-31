'use client';

import { useState, useMemo, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import { Calendar, MapPin, Users, DollarSign, Search as SearchIcon, Building2 } from 'lucide-react';
import { useUnifiedSearch } from '@/presentation/hooks/useUnifiedSearch';
import { EventDto, EventCategory, EventSearchResultDto } from '@/infrastructure/api/types/events.types';
import { BusinessDto } from '@/infrastructure/api/types/business.types';
import { Badge } from '@/presentation/components/ui/Badge';
import { BadgeOverlayGroup } from '@/presentation/components/features/badges';
import { RegistrationBadge } from '@/presentation/components/features/events/RegistrationBadge';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useUserRsvps } from '@/presentation/hooks/useEvents';
import { useEventCategories } from '@/infrastructure/api/hooks/useReferenceData';

/**
 * Search Results Page
 * Phase 6A.59: Unified search across Events and Business
 *
 * Features:
 * - Tab-based interface (Events, Business, Forums, Marketplace)
 * - URL-based state management (q, type, page params)
 * - Pagination per tab
 * - Coming Soon placeholders for Forums/Marketplace
 * - Reuses existing EventCard and BusinessCard components
 *
 * URL Format: /search?q=yoga&type=events&page=1
 */
function SearchPageContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { user } = useAuthStore();

  // Extract URL params
  const query = searchParams?.get('q') || '';
  const type = (searchParams?.get('type') || 'events') as 'events' | 'business' | 'forums' | 'marketplace';
  const page = parseInt(searchParams?.get('page') || '1');

  // Fetch search results
  const { data, isLoading, error } = useUnifiedSearch(query, type, page);

  // Phase 6A.46: Fetch user RSVPs for registration badges (events only)
  const { data: userRsvps } = useUserRsvps({ enabled: !!user && type === 'events' });

  // Phase 6A.46: Create Set of registered event IDs
  const registeredEventIds = useMemo(
    () => new Set(userRsvps?.map(e => e.id) || []),
    [userRsvps]
  );

  // Fetch event categories for labels
  const { data: categories } = useEventCategories();
  const categoryLabels = useMemo(() => {
    if (!categories) return {} as Record<EventCategory, string>;
    return categories.reduce((acc, cat) => {
      if (cat.intValue !== null) {
        acc[cat.intValue as EventCategory] = cat.name;
      }
      return acc;
    }, {} as Record<EventCategory, string>);
  }, [categories]);

  // Handle tab change
  const handleTabChange = (newType: 'events' | 'business' | 'forums' | 'marketplace') => {
    router.push(`/search?q=${encodeURIComponent(query)}&type=${newType}`);
  };

  // Handle page change
  const handlePageChange = (newPage: number) => {
    router.push(`/search?q=${encodeURIComponent(query)}&type=${type}&page=${newPage}`);
  };

  // Tab configuration
  const tabs = [
    { id: 'events', label: 'Events', comingSoon: false },
    { id: 'business', label: 'Business', comingSoon: false },
    { id: 'forums', label: 'Forums', comingSoon: true },
    { id: 'marketplace', label: 'Marketplace', comingSoon: true },
  ] as const;

  return (
    <div className="min-h-screen flex flex-col bg-neutral-50">
      <Header />

      {/* Search Header */}
      <div className="bg-white shadow-sm border-b border-neutral-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <div className="flex items-center gap-3 mb-2">
            <SearchIcon className="h-6 w-6 text-orange-500" />
            <h1 className="text-2xl font-bold text-neutral-900">
              Search Results
            </h1>
          </div>
          <p className="text-neutral-600">
            {query ? `Showing results for "${query}"` : 'Enter a search term to get started'}
          </p>
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="bg-white border-b border-neutral-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex gap-1">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                onClick={() => !tab.comingSoon && handleTabChange(tab.id as typeof type)}
                disabled={tab.comingSoon}
                className={`
                  px-6 py-4 font-medium text-sm border-b-2 transition-colors
                  ${type === tab.id
                    ? 'border-orange-500 text-orange-600'
                    : 'border-transparent text-neutral-600 hover:text-neutral-900 hover:border-neutral-300'
                  }
                  ${tab.comingSoon ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}
                `}
              >
                {tab.label}
                {tab.comingSoon && (
                  <span className="ml-2 text-xs text-neutral-400">(Coming Soon)</span>
                )}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Search Results */}
      <div className="flex-1 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 w-full">
        {!query ? (
          <Card>
            <CardContent className="p-12 text-center">
              <SearchIcon className="h-16 w-16 mx-auto mb-4 text-neutral-400" />
              <h3 className="text-xl font-semibold text-neutral-900 mb-2">
                No Search Query
              </h3>
              <p className="text-neutral-500">
                Use the search box in the header to search for events, forums, businesses, or marketplace items.
              </p>
            </CardContent>
          </Card>
        ) : type === 'forums' || type === 'marketplace' ? (
          <ComingSoonTab feature={type === 'forums' ? 'Forums' : 'Marketplace'} />
        ) : isLoading ? (
          <LoadingGrid type={type} />
        ) : error ? (
          <ErrorState />
        ) : !data || data.items.length === 0 ? (
          <EmptyState query={query} type={type} />
        ) : (
          <>
            {/* Results Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
              {type === 'events' && (
                (data.items as readonly EventSearchResultDto[]).map((event) => (
                  <EventCard
                    key={event.id}
                    event={event}
                    categoryLabels={categoryLabels}
                    isRegistered={user ? registeredEventIds.has(event.id) : false}
                  />
                ))
              )}
              {type === 'business' && (
                (data.items as readonly BusinessDto[]).map((business) => (
                  <BusinessCard key={business.id} business={business} />
                ))
              )}
            </div>

            {/* Pagination */}
            {data.totalPages > 1 && (
              <Pagination
                currentPage={data.pageNumber}
                totalPages={data.totalPages}
                onPageChange={handlePageChange}
              />
            )}
          </>
        )}
      </div>

      <Footer />
    </div>
  );
}

/**
 * Main Search Page Component
 * Wrapped in Suspense for useSearchParams
 */
export default function SearchPage() {
  return (
    <Suspense fallback={<LoadingFallback />}>
      <SearchPageContent />
    </Suspense>
  );
}

/**
 * Loading fallback for Suspense
 */
function LoadingFallback() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-neutral-50">
      <div className="text-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-orange-500 mx-auto mb-4"></div>
        <p className="text-neutral-600">Loading search...</p>
      </div>
    </div>
  );
}

/**
 * Coming Soon Tab Placeholder
 */
function ComingSoonTab({ feature }: { feature: string }) {
  return (
    <Card>
      <CardContent className="p-16 text-center">
        <div className="text-6xl mb-4">🚧</div>
        <h3 className="text-2xl font-semibold text-neutral-900 mb-3">
          {feature} Search Coming Soon
        </h3>
        <p className="text-neutral-600 text-lg">
          We're working hard to bring you {feature.toLowerCase()} search functionality.
        </p>
        <p className="text-neutral-500 mt-2">
          Check back soon for updates!
        </p>
      </CardContent>
    </Card>
  );
}

/**
 * Loading skeleton grid
 */
function LoadingGrid({ type }: { type: string }) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {[...Array(6)].map((_, i) => (
        <Card key={i} className="animate-pulse">
          <CardContent className="p-6">
            <div className="w-full h-48 bg-neutral-200 rounded-lg mb-4"></div>
            <div className="h-6 bg-neutral-200 rounded w-3/4 mb-2"></div>
            <div className="h-4 bg-neutral-200 rounded w-1/2 mb-4"></div>
            <div className="h-4 bg-neutral-200 rounded w-full"></div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

/**
 * Error state
 */
function ErrorState() {
  return (
    <Card>
      <CardContent className="p-12 text-center">
        <div className="text-6xl mb-4">⚠️</div>
        <h3 className="text-xl font-semibold text-neutral-900 mb-2">
          Search Failed
        </h3>
        <p className="text-neutral-500">
          We encountered an error while searching. Please try again later.
        </p>
      </CardContent>
    </Card>
  );
}

/**
 * Empty state
 */
function EmptyState({ query, type }: { query: string; type: string }) {
  return (
    <Card>
      <CardContent className="p-12 text-center">
        <SearchIcon className="h-16 w-16 mx-auto mb-4 text-neutral-400" />
        <h3 className="text-xl font-semibold text-neutral-900 mb-2">
          No {type === 'events' ? 'Events' : 'Businesses'} Found
        </h3>
        <p className="text-neutral-500">
          We couldn't find any {type} matching "{query}". Try different keywords or check another tab.
        </p>
      </CardContent>
    </Card>
  );
}

/**
 * Pagination Component
 */
function Pagination({
  currentPage,
  totalPages,
  onPageChange,
}: {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
}) {
  const pages = [];
  const maxVisible = 5;
  let startPage = Math.max(1, currentPage - Math.floor(maxVisible / 2));
  let endPage = Math.min(totalPages, startPage + maxVisible - 1);

  if (endPage - startPage < maxVisible - 1) {
    startPage = Math.max(1, endPage - maxVisible + 1);
  }

  for (let i = startPage; i <= endPage; i++) {
    pages.push(i);
  }

  return (
    <div className="flex justify-center items-center gap-2">
      <button
        onClick={() => onPageChange(currentPage - 1)}
        disabled={currentPage === 1}
        className="px-4 py-2 border border-neutral-300 rounded-lg hover:bg-neutral-100 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        Previous
      </button>

      {startPage > 1 && (
        <>
          <button
            onClick={() => onPageChange(1)}
            className="px-4 py-2 border border-neutral-300 rounded-lg hover:bg-neutral-100"
          >
            1
          </button>
          {startPage > 2 && <span className="px-2">...</span>}
        </>
      )}

      {pages.map((page) => (
        <button
          key={page}
          onClick={() => onPageChange(page)}
          className={`px-4 py-2 border rounded-lg ${
            page === currentPage
              ? 'bg-orange-500 text-white border-orange-500'
              : 'border-neutral-300 hover:bg-neutral-100'
          }`}
        >
          {page}
        </button>
      ))}

      {endPage < totalPages && (
        <>
          {endPage < totalPages - 1 && <span className="px-2">...</span>}
          <button
            onClick={() => onPageChange(totalPages)}
            className="px-4 py-2 border border-neutral-300 rounded-lg hover:bg-neutral-100"
          >
            {totalPages}
          </button>
        </>
      )}

      <button
        onClick={() => onPageChange(currentPage + 1)}
        disabled={currentPage === totalPages}
        className="px-4 py-2 border border-neutral-300 rounded-lg hover:bg-neutral-100 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        Next
      </button>
    </div>
  );
}

/**
 * Event Card Component (from events page)
 * Phase 6A.46: Displays registration badge and lifecycle label
 * Phase 6A.59: Accepts EventSearchResultDto for search results
 */
function EventCard({
  event,
  categoryLabels,
  isRegistered,
}: {
  event: EventDto | EventSearchResultDto;
  categoryLabels: Record<EventCategory, string>;
  isRegistered: boolean;
}) {
  const startDate = new Date(event.startDate);
  const formattedDate = startDate.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
  const formattedTime = startDate.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
  });

  return (
    <Card
      className="hover:shadow-xl transition-all hover:-translate-y-1 cursor-pointer overflow-hidden"
      onClick={() => window.location.href = `/events/${event.id}`}
    >
      {/* Event Image */}
      <div className="relative h-48 bg-gradient-to-br from-orange-500 to-rose-500">
        {event.images && event.images.length > 0 ? (
          <img
            src={(event.images.find(img => img.isPrimary) || event.images[0]).imageUrl}
            alt={event.title}
            className="w-full h-full object-cover"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-6xl text-white">
            🎉
          </div>
        )}

        {/* Badge Overlays */}
        {event.badges && event.badges.length > 0 && (
          <BadgeOverlayGroup
            badges={event.badges.map(eb => eb.badge)}
            size={50}
            maxBadges={2}
          />
        )}

        {/* Category Badge */}
        <div className="absolute top-3 right-3">
          <Badge
            variant="default"
            className="text-white shadow-lg"
            style={{ background: '#8B1538' }}
          >
            {categoryLabels[event.category] || 'Event'}
          </Badge>
        </div>
      </div>

      <CardContent className="p-6">
        {/* Title */}
        <h3 className="text-lg font-semibold text-neutral-900 mb-3 line-clamp-2">
          {event.title}
        </h3>

        {/* Display Label and Registration Badge */}
        <div className="flex flex-wrap items-center gap-2 mb-3">
          <Badge
            variant="default"
            className="text-white font-semibold"
            style={{ backgroundColor: getStatusBadgeColor(event.displayLabel) }}
          >
            {event.displayLabel}
          </Badge>
          <RegistrationBadge isRegistered={isRegistered} compact={false} />
        </div>

        {/* Date & Time */}
        <div className="flex items-center gap-2 text-sm text-neutral-600 mb-2">
          <Calendar className="h-4 w-4" style={{ color: '#FF7900' }} />
          <span>{formattedDate} at {formattedTime}</span>
        </div>

        {/* Location */}
        {event.city && event.state && (
          <div className="flex items-center gap-2 text-sm text-neutral-600 mb-2">
            <MapPin className="h-4 w-4" style={{ color: '#FF7900' }} />
            <span>{event.city}, {event.state}</span>
          </div>
        )}

        {/* Capacity */}
        <div className="flex items-center gap-2 text-sm text-neutral-600 mb-3">
          <Users className="h-4 w-4" style={{ color: '#FF7900' }} />
          <span>
            {event.currentRegistrations} / {event.capacity} registered
          </span>
        </div>

        {/* Pricing */}
        <div className="flex items-center justify-between pt-3 border-t border-neutral-200">
          <div className="flex items-center gap-2">
            <DollarSign className="h-4 w-4" style={{ color: '#FF7900' }} />
            <span className="font-semibold text-neutral-900">
              {event.isFree ? 'Free' : `$${event.ticketPriceAmount || 0}`}
            </span>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

/**
 * Business Card Component
 * Phase 6A.59: Display business search results
 */
function BusinessCard({ business }: { business: BusinessDto }) {
  return (
    <Card
      className="hover:shadow-xl transition-all hover:-translate-y-1 cursor-pointer overflow-hidden"
      onClick={() => window.location.href = `/businesses/${business.id}`}
    >
      {/* Business Header */}
      <div className="relative h-48 bg-gradient-to-br from-blue-500 to-indigo-500 flex items-center justify-center">
        <Building2 className="h-20 w-20 text-white opacity-80" />

        {/* Verified Badge */}
        {business.isVerified && (
          <div className="absolute top-3 right-3">
            <Badge
              variant="default"
              className="text-white shadow-lg"
              style={{ background: '#10B981' }}
            >
              ✓ Verified
            </Badge>
          </div>
        )}
      </div>

      <CardContent className="p-6">
        {/* Title */}
        <h3 className="text-lg font-semibold text-neutral-900 mb-3 line-clamp-2">
          {business.name}
        </h3>

        {/* Description */}
        <p className="text-sm text-neutral-600 mb-3 line-clamp-2">
          {business.description}
        </p>

        {/* Location */}
        {business.city && business.province && (
          <div className="flex items-center gap-2 text-sm text-neutral-600 mb-2">
            <MapPin className="h-4 w-4 text-blue-500" />
            <span>{business.city}, {business.province}</span>
          </div>
        )}

        {/* Rating */}
        {business.rating && (
          <div className="flex items-center gap-2 text-sm text-neutral-600 mb-3">
            <span className="text-yellow-500">⭐</span>
            <span className="font-semibold">{business.rating.toFixed(1)}</span>
            <span className="text-neutral-400">({business.reviewCount} reviews)</span>
          </div>
        )}

        {/* Contact Info */}
        <div className="pt-3 border-t border-neutral-200">
          {business.contactPhone && (
            <p className="text-xs text-neutral-500">
              📞 {business.contactPhone}
            </p>
          )}
        </div>
      </CardContent>
    </Card>
  );
}

/**
 * Get badge color based on event lifecycle label
 */
function getStatusBadgeColor(label: string): string {
  switch (label) {
    case 'New':
      return '#10B981';
    case 'Upcoming':
      return '#FF7900';
    case 'Published':
    case 'Active':
      return '#6366F1';
    case 'Cancelled':
      return '#EF4444';
    case 'Completed':
      return '#6B7280';
    case 'Inactive':
      return '#9CA3AF';
    case 'Draft':
      return '#F59E0B';
    case 'Postponed':
      return '#F97316';
    case 'UnderReview':
      return '#8B5CF6';
    default:
      return '#8B1538';
  }
}
