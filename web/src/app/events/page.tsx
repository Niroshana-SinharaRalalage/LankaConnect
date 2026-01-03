'use client';

import { useState, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Badge } from '@/presentation/components/ui/Badge';
import { Button } from '@/presentation/components/ui/Button';
import { TreeDropdown, type TreeNode } from '@/presentation/components/ui/TreeDropdown';
import { Calendar, MapPin, Users, DollarSign, Filter, Plus } from 'lucide-react';
import { useEvents, useUserRsvps } from '@/presentation/hooks/useEvents';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useGeolocation } from '@/presentation/hooks/useGeolocation';
import { useMetroAreas } from '@/presentation/hooks/useMetroAreas';
import { EventCategory, EventDto } from '@/infrastructure/api/types/events.types';
import { BadgeOverlayGroup } from '@/presentation/components/features/badges';
import { RegistrationBadge } from '@/presentation/components/features/events/RegistrationBadge';
import { US_STATES } from '@/domain/constants/metroAreas.constants';
import { getDateRangeForOption, type DateRangeOption } from '@/presentation/utils/dateRanges';
import { UserRole } from '@/infrastructure/api/types/auth.types';
import { useEventCategories } from '@/infrastructure/api/hooks/useReferenceData';
import { toDropdownOptions } from '@/infrastructure/api/utils/enum-mappers';
import { useDebounce } from '@/hooks/useDebounce';

/**
 * Events Listing Page
 * Phase 6B: View All Events Feature
 * Phase 6A.46: Event status labels and registration badges
 *
 * Features:
 * - Location-based sorting (same logic as featured events)
 * - Three filtering options: Event Type, Event Date, Location
 * - Displays unlimited events with scrolling
 * - Works for authenticated and anonymous users
 * - Phase 6A.46: Shows "You are registered" badge for registered events
 * - Phase 6A.46: Displays computed lifecycle labels (New, Upcoming, etc.)
 */
export default function EventsPage() {
  const router = useRouter();
  const { user } = useAuthStore();

  // For anonymous users, detect location via IP/browser geolocation
  const isAnonymous = !user?.userId;
  const { latitude, longitude, loading: locationLoading } = useGeolocation(isAnonymous);

  // Metro areas data for location filter
  const {
    metroAreasByState,
    stateLevelMetros,
    isLoading: metrosLoading,
  } = useMetroAreas();

  // Phase 6A.47: Fetch EventCategory reference data from API
  const { data: categories } = useEventCategories();

  // Filter states
  const [selectedCategory, setSelectedCategory] = useState<EventCategory | undefined>(undefined);
  const [selectedMetroIds, setSelectedMetroIds] = useState<string[]>([]);
  const [selectedState, setSelectedState] = useState<string | undefined>(undefined);
  const [dateRangeOption, setDateRangeOption] = useState<DateRangeOption>('upcoming');
  const [searchInput, setSearchInput] = useState<string>(''); // Phase 6A.58: Immediate search input (local state)

  // Phase 6A.59: Debounce search term to avoid excessive API calls (matches dashboard behavior)
  const debouncedSearchTerm = useDebounce(searchInput, 300);

  // Build filters for useEvents hook
  const filters = useMemo(() => {
    const dateRange = getDateRangeForOption(dateRangeOption);
    return {
      searchTerm: debouncedSearchTerm || undefined, // Phase 6A.59: Use debounced search term
      category: selectedCategory,
      userId: user?.userId,
      latitude: isAnonymous ? latitude ?? undefined : undefined,
      longitude: isAnonymous ? longitude ?? undefined : undefined,
      metroAreaIds: selectedMetroIds.length > 0 ? selectedMetroIds : undefined,
      state: selectedState,
      ...dateRange, // Spread startDateFrom and startDateTo from date range
    };
  }, [debouncedSearchTerm, selectedCategory, user?.userId, isAnonymous, latitude, longitude, selectedMetroIds, selectedState, dateRangeOption]);

  // Fetch events with location-based sorting and filters
  const { data: events, isLoading: eventsLoading, error: eventsError } = useEvents(filters);

  // Phase 6A.46: Bulk fetch user RSVPs (1 API call) for registration badges
  const { data: userRsvps } = useUserRsvps({ enabled: !!user });

  // Phase 6A.46: Create Set of registered event IDs for O(1) lookups
  const registeredEventIds = useMemo(
    () => new Set(userRsvps?.map(e => e.id) || []),
    [userRsvps]
  );

  // Convert metro areas to tree structure for TreeDropdown
  const locationTreeNodes = useMemo<TreeNode[]>(() => {
    const nodes: TreeNode[] = [];

    US_STATES.forEach((state) => {
      const metrosForState = metroAreasByState.get(state.code) || [];

      if (metrosForState.length === 0) return;

      // Separate state-level and city-level metros
      const stateMetro = metrosForState.find((m) => m.isStateLevelArea);
      const cityMetros = metrosForState.filter((m) => !m.isStateLevelArea);

      const children: TreeNode[] = [];

      // Add state-level metro first if it exists
      if (stateMetro) {
        children.push({
          id: stateMetro.id,
          label: `All of ${state.name}`,
          checked: selectedMetroIds.includes(stateMetro.id),
        });
      }

      // Add city-level metros
      cityMetros.forEach((metro) => {
        children.push({
          id: metro.id,
          label: metro.name,
          checked: selectedMetroIds.includes(metro.id),
        });
      });

      nodes.push({
        id: state.code,
        label: state.name,
        checked: false,
        children,
      });
    });

    return nodes;
  }, [metroAreasByState, selectedMetroIds]);

  const handleLocationChange = (newSelectedIds: string[]) => {
    setSelectedMetroIds(newSelectedIds);
  };

  const handleCategoryChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const value = e.target.value;
    setSelectedCategory(value === '' ? undefined : Number(value) as EventCategory);
  };

  const handleDateRangeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setDateRangeOption(e.target.value as DateRangeOption);
  };

  const clearFilters = () => {
    setSearchInput(''); // Phase 6A.59: Clear search input (local state)
    setSelectedCategory(undefined);
    setSelectedMetroIds([]);
    setSelectedState(undefined);
    setDateRangeOption('upcoming');
  };

  const hasActiveFilters = searchInput !== '' || selectedCategory !== undefined || selectedMetroIds.length > 0 || selectedState !== undefined || dateRangeOption !== 'upcoming';

  const isLoading = eventsLoading || (isAnonymous && locationLoading) || metrosLoading;

  // Phase 6A.47: Convert reference data to dropdown options
  const categoryOptions = useMemo(() => toDropdownOptions(categories), [categories]);

  // Create category labels map from reference data
  const categoryLabels = useMemo(() => {
    if (!categories) return {} as Record<EventCategory, string>;
    const labels: Record<EventCategory, string> = {} as Record<EventCategory, string>;
    categories.forEach(cat => {
      labels[cat.intValue as EventCategory] = cat.name;
    });
    return labels;
  }, [categories]);

  // Phase 6A.63: Remove Create Event button from /events page for all users
  // Users should create events only from Dashboard
  const canUserCreateEvents = false;

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Page Header */}
      <div className="bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 py-12 relative overflow-hidden">
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

        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between">
            <div className="flex-1">
              <h1 className="text-4xl font-bold text-white mb-4">
                Discover Events
              </h1>
              <p className="text-lg text-white/90 max-w-2xl">
                Find cultural, community, and social events relevant to you
              </p>
            </div>
            {/* Create Event Button - Show for EventOrganizer, Admin, AdminManager */}
            {canUserCreateEvents && (
              <Button
                onClick={() => router.push('/events/create')}
                className="flex items-center gap-2"
                style={{ background: '#FF7900' }}
              >
                <Plus className="h-5 w-5" />
                Create Event
              </Button>
            )}
          </div>
        </div>
      </div>

      {/* Filters Section */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Filter className="h-5 w-5" style={{ color: '#FF7900' }} />
                <CardTitle style={{ color: '#8B1538' }}>Filters</CardTitle>
              </div>
              {hasActiveFilters && (
                <button
                  onClick={clearFilters}
                  className="text-sm font-medium hover:underline"
                  style={{ color: '#FF7900' }}
                >
                  Clear All
                </button>
              )}
            </div>
          </CardHeader>
          <CardContent>
            {/* Phase 6A.59: Search Input with debouncing (matches dashboard behavior) */}
            <div className="mb-4">
              <label className="block text-sm font-medium text-neutral-700 mb-2">
                Search Events
              </label>
              <input
                type="text"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                placeholder="Search by event name, description..."
                className="w-full px-4 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500"
                disabled={isLoading}
              />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {/* Event Type Filter */}
              <div>
                <label className="block text-sm font-medium text-neutral-700 mb-2">
                  Event Type
                </label>
                <select
                  value={selectedCategory ?? ''}
                  onChange={handleCategoryChange}
                  className="w-full px-4 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500"
                  disabled={isLoading}
                >
                  <option value="">All Types</option>
                  {categoryOptions.map((category) => (
                    <option key={category.value} value={category.value}>
                      {category.label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Event Date Filter */}
              <div>
                <label className="block text-sm font-medium text-neutral-700 mb-2">
                  Event Date
                </label>
                <select
                  value={dateRangeOption}
                  onChange={handleDateRangeChange}
                  className="w-full px-4 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500"
                  disabled={isLoading}
                >
                  <option value="upcoming">Upcoming Events</option>
                  <option value="thisWeek">This Week</option>
                  <option value="nextWeek">Next Week</option>
                  <option value="nextMonth">Next Month</option>
                  <option value="all">All Events</option>
                </select>
              </div>

              {/* Location Filter */}
              <div>
                <label className="block text-sm font-medium text-neutral-700 mb-2">
                  Location (State/Metro Area)
                </label>
                <TreeDropdown
                  nodes={locationTreeNodes}
                  selectedIds={selectedMetroIds}
                  onSelectionChange={handleLocationChange}
                  placeholder="Select location"
                  maxSelections={20}
                  disabled={isLoading || metrosLoading}
                  className="w-full"
                />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Events Grid */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-12">
        {isLoading ? (
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
        ) : eventsError ? (
          <Card>
            <CardContent className="p-12 text-center">
              <p className="text-destructive text-lg">
                Failed to load events. Please try again later.
              </p>
            </CardContent>
          </Card>
        ) : !events || events.length === 0 ? (
          <Card>
            <CardContent className="p-12 text-center">
              <Calendar className="h-16 w-16 mx-auto mb-4 text-neutral-400" />
              <h3 className="text-xl font-semibold text-neutral-900 mb-2">
                No Events Found
              </h3>
              <p className="text-neutral-500">
                {hasActiveFilters
                  ? 'Try adjusting your filters to see more events.'
                  : 'Check back soon for new events!'}
              </p>
            </CardContent>
          </Card>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {events.map((event) => (
              <EventCard
                key={event.id}
                event={event}
                categoryLabels={categoryLabels}
                isRegistered={user ? registeredEventIds.has(event.id) : false}
              />
            ))}
          </div>
        )}
      </div>

      <Footer />
    </div>
  );
}

/**
 * Phase 6A.46: Get badge color based on event lifecycle label
 * LankaConnect theme colors: Orange #FF7900, Rose #8B1538, Emerald #047857
 */
function getStatusBadgeColor(label: string): string {
  switch (label) {
    case 'New':
      return '#10B981'; // Emerald-500 - Fresh, exciting new events
    case 'Upcoming':
      return '#FF7900'; // LankaConnect Orange - Events starting soon
    case 'Published':
    case 'Active':
      return '#6366F1'; // Indigo-500 - Currently active events
    case 'Cancelled':
      return '#EF4444'; // Red-500 - Cancelled events
    case 'Completed':
      return '#6B7280'; // Gray-500 - Past events
    case 'Inactive':
      return '#9CA3AF'; // Gray-400 - Old inactive events
    case 'Draft':
      return '#F59E0B'; // Amber-500 - Draft events
    case 'Postponed':
      return '#F97316'; // Orange-500 - Postponed events
    case 'UnderReview':
      return '#8B5CF6'; // Violet-500 - Under admin review
    default:
      return '#8B1538'; // LankaConnect Rose - Default fallback
  }
}

/**
 * Event Card Component
 * Displays individual event with image, title, date, location, category, and pricing
 * Phase 6A.46: Displays registration badge and lifecycle label
 */
function EventCard({
  event,
  categoryLabels,
  isRegistered,
}: {
  event: EventDto;
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

        {/* Phase 6A.25: Badge Overlays */}
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
            {categoryLabels[event.category]}
          </Badge>
        </div>
      </div>

      <CardContent className="p-6">
        {/* Title */}
        <h3 className="text-lg font-semibold text-neutral-900 mb-3 line-clamp-2">
          {event.title}
        </h3>

        {/* Phase 6A.46: Display Label and Registration Badge */}
        <div className="flex flex-wrap items-center gap-2 mb-3">
          {/* Display Label (computed lifecycle label from backend) */}
          <Badge
            variant="default"
            className="text-white font-semibold"
            style={{ backgroundColor: getStatusBadgeColor(event.displayLabel) }}
          >
            {event.displayLabel}
          </Badge>

          {/* Registration Badge */}
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

        {/* Pricing - Session 33: Group and dual pricing support */}
        <div className="flex items-center justify-between pt-3 border-t border-neutral-200">
          <div className="flex items-center gap-2">
            <DollarSign className="h-4 w-4" style={{ color: '#FF7900' }} />
            <span className="text-sm font-semibold" style={{ color: '#8B1538' }}>
              {event.isFree
                ? 'Free Event'
                : event.hasGroupPricing && event.groupPricingTiers && event.groupPricingTiers.length > 0
                  ? (() => {
                      const prices = event.groupPricingTiers.map(t => t.pricePerPerson);
                      const minPrice = Math.min(...prices);
                      const maxPrice = Math.max(...prices);
                      return minPrice === maxPrice
                        ? `$${minPrice.toFixed(2)}`
                        : `$${minPrice.toFixed(2)}-$${maxPrice.toFixed(2)}`;
                    })()
                  : event.hasDualPricing
                    ? `$${event.adultPriceAmount?.toFixed(2)} / $${event.childPriceAmount?.toFixed(2)}`
                    : event.ticketPriceAmount != null
                      ? `$${event.ticketPriceAmount.toFixed(2)}`
                      : 'Paid Event'}
            </span>
          </div>
          <button
            className="px-4 py-2 rounded-lg text-sm font-medium text-white transition-colors"
            style={{ background: '#FF7900' }}
          >
            View Details
          </button>
        </div>
      </CardContent>
    </Card>
  );
}
