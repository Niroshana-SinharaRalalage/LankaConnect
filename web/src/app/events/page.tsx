'use client';

import { useState, useMemo } from 'react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Badge } from '@/presentation/components/ui/Badge';
import { TreeDropdown, type TreeNode } from '@/presentation/components/ui/TreeDropdown';
import { Calendar, MapPin, Users, DollarSign, Filter } from 'lucide-react';
import { useEvents } from '@/presentation/hooks/useEvents';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useGeolocation } from '@/presentation/hooks/useGeolocation';
import { useMetroAreas } from '@/presentation/hooks/useMetroAreas';
import { EventCategory, EventDto } from '@/infrastructure/api/types/events.types';
import { US_STATES } from '@/domain/constants/metroAreas.constants';

/**
 * Events Listing Page
 * Phase 6B: View All Events Feature
 *
 * Features:
 * - Location-based sorting (same logic as featured events)
 * - Three filtering options: Event Type, Event Date, Location
 * - Displays unlimited events with scrolling
 * - Works for authenticated and anonymous users
 */
export default function EventsPage() {
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

  // Filter states
  const [selectedCategory, setSelectedCategory] = useState<EventCategory | undefined>(undefined);
  const [selectedMetroIds, setSelectedMetroIds] = useState<string[]>([]);
  const [selectedState, setSelectedState] = useState<string | undefined>(undefined);
  const [sortByDate, setSortByDate] = useState<'upcoming' | 'all'>('upcoming');

  // Build filters for useEvents hook
  const filters = useMemo(() => {
    const now = new Date().toISOString();
    return {
      category: selectedCategory,
      userId: user?.userId,
      latitude: isAnonymous ? latitude ?? undefined : undefined,
      longitude: isAnonymous ? longitude ?? undefined : undefined,
      metroAreaIds: selectedMetroIds.length > 0 ? selectedMetroIds : undefined,
      state: selectedState,
      startDateFrom: sortByDate === 'upcoming' ? now : undefined,
    };
  }, [selectedCategory, user?.userId, isAnonymous, latitude, longitude, selectedMetroIds, selectedState, sortByDate]);

  // Fetch events with location-based sorting and filters
  const { data: events, isLoading: eventsLoading, error: eventsError } = useEvents(filters);

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

  const handleDateSortChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setSortByDate(e.target.value as 'upcoming' | 'all');
  };

  const clearFilters = () => {
    setSelectedCategory(undefined);
    setSelectedMetroIds([]);
    setSelectedState(undefined);
    setSortByDate('upcoming');
  };

  const hasActiveFilters = selectedCategory !== undefined || selectedMetroIds.length > 0 || selectedState !== undefined || sortByDate !== 'upcoming';

  // Category labels
  const categoryLabels: Record<EventCategory, string> = {
    [EventCategory.Religious]: 'Religious',
    [EventCategory.Cultural]: 'Cultural',
    [EventCategory.Community]: 'Community',
    [EventCategory.Educational]: 'Educational',
    [EventCategory.Social]: 'Social',
    [EventCategory.Business]: 'Business',
    [EventCategory.Charity]: 'Charity',
    [EventCategory.Entertainment]: 'Entertainment',
  };

  const isLoading = eventsLoading || (isAnonymous && locationLoading) || metrosLoading;

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Page Header */}
      <div className="bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 py-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center">
            <h1 className="text-4xl font-bold text-white mb-4">
              Discover Events
            </h1>
            <p className="text-lg text-white/90 max-w-2xl mx-auto">
              Find cultural, community, and social events relevant to you
            </p>
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
                  {Object.entries(categoryLabels).map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
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
                  value={sortByDate}
                  onChange={handleDateSortChange}
                  className="w-full px-4 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500"
                  disabled={isLoading}
                >
                  <option value="upcoming">Upcoming Events</option>
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
              <EventCard key={event.id} event={event} categoryLabels={categoryLabels} />
            ))}
          </div>
        )}
      </div>

      <Footer />
    </div>
  );
}

/**
 * Event Card Component
 * Displays individual event with image, title, date, location, category, and pricing
 */
function EventCard({
  event,
  categoryLabels,
}: {
  event: EventDto;
  categoryLabels: Record<EventCategory, string>;
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
    <Card className="hover:shadow-xl transition-all hover:-translate-y-1 cursor-pointer overflow-hidden">
      {/* Event Image */}
      <div className="relative h-48 bg-gradient-to-br from-orange-500 to-rose-500">
        {event.images && event.images.length > 0 ? (
          <img
            src={event.images[0].imageUrl}
            alt={event.title}
            className="w-full h-full object-cover"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-6xl text-white">
            🎉
          </div>
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
            <span className="text-sm font-semibold" style={{ color: '#8B1538' }}>
              {event.isFree ? 'Free Event' : `$${event.ticketPriceAmount?.toFixed(2)}`}
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
