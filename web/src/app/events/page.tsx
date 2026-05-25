'use client';

import { useState, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { LankaEventsHeader } from '@/presentation/components/layout/LankaEventsHeader';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import { Badge } from '@/presentation/components/ui/Badge';
import { Button } from '@/presentation/components/ui/Button';
import { TreeDropdown, type TreeNode } from '@/presentation/components/ui/TreeDropdown';
// Phase 6A.149: per-section filters live inside a CollapsibleSection (defaultOpen={false}).
import { CollapsibleSection } from '@/presentation/components/ui/CollapsibleSection';
import { Calendar, MapPin, Users, DollarSign, Filter, Plus } from 'lucide-react';
import { useEvents, useUserRsvps } from '@/presentation/hooks/useEvents';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useGeolocation } from '@/presentation/hooks/useGeolocation';
import { useMetroAreas } from '@/presentation/hooks/useMetroAreas';
// Phase 6A.152: EventStatus import removed. The Completed section is now
// date-based on the backend (StartDate < now), so the client no longer needs
// to filter by Status. EventStatusFilterLabels was already dropped in 6A.149.
import { EventCategory, EventDto, EventPaymentMode, EventStatusFilter } from '@/infrastructure/api/types/events.types';
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

  // Phase 6A.149: Per-section filter state. Upcoming and Completed each maintain
  // their OWN search / category / location / date so a search inside the
  // Completed section doesn't disturb the Upcoming view (and vice versa). Date
  // is only meaningful for Upcoming — Completed is implicitly past-only so the
  // Completed filter card omits the Date dropdown.
  const [upcomingSearchInput, setUpcomingSearchInput] = useState<string>('');
  const [upcomingCategory, setUpcomingCategory] = useState<EventCategory | undefined>(undefined);
  const [upcomingMetroIds, setUpcomingMetroIds] = useState<string[]>([]);
  const [upcomingDateRangeOption, setUpcomingDateRangeOption] = useState<DateRangeOption>('upcoming');

  const [completedSearchInput, setCompletedSearchInput] = useState<string>('');
  const [completedCategory, setCompletedCategory] = useState<EventCategory | undefined>(undefined);
  const [completedMetroIds, setCompletedMetroIds] = useState<string[]>([]);

  // 500ms debounce keeps typing smooth (Phase 6A.72 pattern).
  const debouncedUpcomingSearch = useDebounce(upcomingSearchInput, 500);
  const debouncedCompletedSearch = useDebounce(completedSearchInput, 500);

  const upcomingDateRange = useMemo(
    () => getDateRangeForOption(upcomingDateRangeOption),
    [upcomingDateRangeOption],
  );

  // Stabilize metro id arrays so React Query keys don't churn.
  const stableUpcomingMetros = useMemo(
    () => (upcomingMetroIds.length > 0 ? upcomingMetroIds : undefined),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [upcomingMetroIds.length, ...upcomingMetroIds],
  );
  const stableCompletedMetros = useMemo(
    () => (completedMetroIds.length > 0 ? completedMetroIds : undefined),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [completedMetroIds.length, ...completedMetroIds],
  );

  const upcomingFilters = useMemo(() => ({
    searchTerm: debouncedUpcomingSearch || undefined,
    category: upcomingCategory,
    statusFilter: EventStatusFilter.Active,  // Upcoming → Active group
    userId: user?.userId,
    latitude: isAnonymous ? latitude ?? undefined : undefined,
    longitude: isAnonymous ? longitude ?? undefined : undefined,
    metroAreaIds: stableUpcomingMetros,
    ...upcomingDateRange,
  }), [debouncedUpcomingSearch, upcomingCategory, user?.userId, isAnonymous, latitude, longitude, stableUpcomingMetros, upcomingDateRange]);

  const completedFilters = useMemo(() => ({
    searchTerm: debouncedCompletedSearch || undefined,
    category: completedCategory,
    statusFilter: EventStatusFilter.Inactive,  // Inactive group includes Completed
    userId: user?.userId,
    latitude: isAnonymous ? latitude ?? undefined : undefined,
    longitude: isAnonymous ? longitude ?? undefined : undefined,
    metroAreaIds: stableCompletedMetros,
    // No startDateFrom/To — Completed is implicitly past; date gating would
    // zero the section (pinned by Phase 1 RED test
    // "does NOT pass date range filters into the Inactive (Completed) call").
  }), [debouncedCompletedSearch, completedCategory, user?.userId, isAnonymous, latitude, longitude, stableCompletedMetros]);

  // Two independent useEvents calls — one per section. React Query caches by
  // filter args so repeat navigation hits cache when filters are unchanged.
  const { data: upcomingEvents, isLoading: upcomingLoading, error: upcomingError } = useEvents(upcomingFilters);
  const { data: completedRawEvents, isLoading: completedLoading } = useEvents(completedFilters);

  // Phase 6A.152: Backend EventStatusFilter.Inactive now returns events where
  // StartDate < now (date-based), regardless of Status. No client-side Status
  // filter is needed — the API response is already the correct set for the
  // public Completed section. Cancelled events are excluded server-side and
  // never reach this list.
  const completedEvents = useMemo(
    () => completedRawEvents ?? [],
    [completedRawEvents],
  );

  // Phase 6A.46: Bulk fetch user RSVPs for registration status (Issue #2: Not needed anymore - status is on EventDto)
  // Removed: registeredEventIds Set logic - now using event.userRegistrationStatus

  // Phase 6A.149: build tree-node structures per section because each section
  // tracks its own selected metro ids. Same shape, different `checked` flags.
  const buildLocationTree = (selectedIds: string[]): TreeNode[] => {
    const nodes: TreeNode[] = [];
    US_STATES.forEach((state) => {
      const metrosForState = metroAreasByState.get(state.code) || [];
      if (metrosForState.length === 0) return;
      const cityMetros = metrosForState.filter((m) => !m.isStateLevelArea);
      if (cityMetros.length === 0) return;
      nodes.push({
        id: state.code,
        label: state.name,
        checked: false,
        children: cityMetros.map((metro) => ({
          id: metro.id,
          label: metro.name,
          checked: selectedIds.includes(metro.id),
        })),
      });
    });
    return nodes;
  };
  const upcomingLocationTreeNodes = useMemo<TreeNode[]>(
    () => buildLocationTree(upcomingMetroIds),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [metroAreasByState, upcomingMetroIds],
  );
  const completedLocationTreeNodes = useMemo<TreeNode[]>(
    () => buildLocationTree(completedMetroIds),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [metroAreasByState, completedMetroIds],
  );

  // Phase 6A.149: per-section clear handlers. Each section's "Clear All" pill
  // only resets that section's state — opening Completed filters and clearing
  // them does not disturb Upcoming and vice versa.
  const clearUpcomingFilters = () => {
    setUpcomingSearchInput('');
    setUpcomingCategory(undefined);
    setUpcomingMetroIds([]);
    setUpcomingDateRangeOption('upcoming');
  };
  const clearCompletedFilters = () => {
    setCompletedSearchInput('');
    setCompletedCategory(undefined);
    setCompletedMetroIds([]);
  };

  const hasUpcomingActiveFilters =
    upcomingSearchInput !== '' ||
    upcomingCategory !== undefined ||
    upcomingMetroIds.length > 0 ||
    upcomingDateRangeOption !== 'upcoming';
  const hasCompletedActiveFilters =
    completedSearchInput !== '' ||
    completedCategory !== undefined ||
    completedMetroIds.length > 0;

  const isLoading = upcomingLoading || (isAnonymous && locationLoading) || metrosLoading;

  // Phase 6A.47: Convert reference data to dropdown options
  const categoryOptions = useMemo(() => toDropdownOptions(categories), [categories]);

  // Create category labels map from reference data
  // Phase 6A.X: Support BOTH numeric and string category keys for API compatibility
  // Backend serializes enums as strings (JsonStringEnumConverter), but we future-proof
  // by supporting numeric keys (0-11) AND string keys ("Religious", "Festival", etc.)
  const categoryLabels = useMemo(() => {
    if (!categories) return {} as Record<string, string>;
    const labels: Record<string, string> = {};
    categories.forEach(cat => {
      if (cat.intValue !== null && cat.intValue !== undefined) {
        // Add numeric string key (e.g., "9" → "Festival") for numeric category values
        labels[cat.intValue.toString()] = cat.name;
      }
      // Add name string key (e.g., "Festival" → "Festival") for string category values from API
      labels[cat.code] = cat.name;
    });
    return labels;
  }, [categories]);

  // Phase 6A.63: Remove Create Event button from /events page for all users
  // Users should create events only from Dashboard
  const canUserCreateEvents = false;

  // Phase 6A.149: shared filter form. Each section passes its own state and
  // handlers — keeps the form markup in one place while preserving independent
  // state per section. `showDateFilter` is false for the Completed section
  // because Completed implies past.
  const renderFilterForm = (opts: {
    searchInput: string;
    onSearchChange: (v: string) => void;
    category: EventCategory | undefined;
    onCategoryChange: (v: EventCategory | undefined) => void;
    showDateFilter: boolean;
    dateRangeOption?: DateRangeOption;
    onDateRangeChange?: (v: DateRangeOption) => void;
    metroIds: string[];
    onMetroIdsChange: (ids: string[]) => void;
    treeNodes: TreeNode[];
    placeholderPrefix: string;
  }) => (
    <>
      <div className="mb-4">
        <label className="block text-sm font-medium text-neutral-700 mb-2">Search Events</label>
        <input
          type="text"
          value={opts.searchInput}
          onChange={(e) => opts.onSearchChange(e.target.value)}
          placeholder={`Search ${opts.placeholderPrefix} by event name, description...`}
          className="w-full px-4 py-2 border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500"
          disabled={isLoading}
        />
      </div>
      <div className={`grid grid-cols-1 md:grid-cols-2 ${opts.showDateFilter ? 'lg:grid-cols-3' : 'lg:grid-cols-2'} gap-4`}>
        <div>
          <label className="block text-sm font-medium text-neutral-700 mb-2">Event Type</label>
          <select
            value={opts.category ?? ''}
            onChange={(e) => {
              const v = e.target.value;
              opts.onCategoryChange(v === '' ? undefined : (Number(v) as EventCategory));
            }}
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
        {opts.showDateFilter && opts.dateRangeOption !== undefined && opts.onDateRangeChange && (
          <div>
            <label className="block text-sm font-medium text-neutral-700 mb-2">Event Date</label>
            <select
              value={opts.dateRangeOption}
              onChange={(e) => opts.onDateRangeChange!(e.target.value as DateRangeOption)}
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
        )}
        <div>
          <label className="block text-sm font-medium text-neutral-700 mb-2">
            Location (State/Metro Area)
          </label>
          <TreeDropdown
            nodes={opts.treeNodes}
            selectedIds={opts.metroIds}
            onSelectionChange={opts.onMetroIdsChange}
            placeholder="Select location"
            maxSelections={20}
            disabled={isLoading || metrosLoading}
            className="w-full"
          />
        </div>
      </div>
    </>
  );

  // Loading skeleton shared by both sections.
  const renderSkeleton = () => (
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

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <LankaEventsHeader />

      {/* Phase 6A.149: gradient "Discover Events" banner removed entirely
          (~12rem reclaimed above the fold). Page identity comes from the nav
          and the explicit section headers below. */}

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-12">
        {/* ============================ Upcoming Events ============================ */}
        <section>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-2xl font-bold" style={{ color: '#8B1538' }}>
              Upcoming Events
            </h2>
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

          <CollapsibleSection
            title="Filters"
            defaultOpen={false}
            icon={<Filter className="h-5 w-5" style={{ color: '#FF7900' }} />}
            borderColor="#8B1538"
            badge={
              hasUpcomingActiveFilters ? (
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    clearUpcomingFilters();
                  }}
                  className="text-sm font-medium hover:underline"
                  style={{ color: '#FF7900' }}
                >
                  Clear All
                </button>
              ) : undefined
            }
            className="mb-4"
          >
            {renderFilterForm({
              searchInput: upcomingSearchInput,
              onSearchChange: setUpcomingSearchInput,
              category: upcomingCategory,
              onCategoryChange: setUpcomingCategory,
              showDateFilter: true,
              dateRangeOption: upcomingDateRangeOption,
              onDateRangeChange: setUpcomingDateRangeOption,
              metroIds: upcomingMetroIds,
              onMetroIdsChange: setUpcomingMetroIds,
              treeNodes: upcomingLocationTreeNodes,
              placeholderPrefix: 'upcoming events',
            })}
          </CollapsibleSection>

          {/* Scroll-bounded grid (max-h-[1500px] ≈ 3 rows × ~480px card height) */}
          <div className="relative">
            <div
              data-section="upcoming-grid-scroll"
              className="max-h-[1500px] overflow-y-auto pr-1"
            >
              {isLoading ? (
                renderSkeleton()
              ) : !upcomingEvents || upcomingEvents.length === 0 ? (
                upcomingError ? (
                  <Card>
                    <CardContent className="p-12 text-center">
                      <p className="text-destructive text-lg">
                        Failed to load events. Please try again later.
                      </p>
                    </CardContent>
                  </Card>
                ) : (
                  <Card>
                    <CardContent className="p-12 text-center">
                      <Calendar className="h-16 w-16 mx-auto mb-4 text-neutral-400" />
                      <h3 className="text-xl font-semibold text-neutral-900 mb-2">
                        No Events Found
                      </h3>
                      <p className="text-neutral-500">
                        {hasUpcomingActiveFilters
                          ? 'Try adjusting your filters to see more events.'
                          : 'Check back soon for new events!'}
                      </p>
                    </CardContent>
                  </Card>
                )
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                  {upcomingEvents.map((event) => (
                    <EventCard key={event.id} event={event} categoryLabels={categoryLabels} />
                  ))}
                </div>
              )}
            </div>
            {/* Fade-mask telegraphing "more below" — purely decorative + pointer-events-none */}
            <div
              data-section="upcoming-grid-fade"
              className="pointer-events-none absolute bottom-0 left-0 right-0 h-16 bg-gradient-to-t from-white to-transparent"
              aria-hidden="true"
            />
          </div>
        </section>

        {/* ============================ Completed Events ============================
            Phase 6A.152: heading and filter card always render so the feature is
            discoverable even when the list is empty (e.g. an event organizer's
            first month). Empty-state card sits inside the grid when there are
            no past events to show. Replaces the 6A.149 self-gating behaviour. */}
        <section>
          <h2 className="text-2xl font-bold mb-4" style={{ color: '#8B1538' }}>
            Completed Events
          </h2>

          <CollapsibleSection
            title="Filters"
            defaultOpen={false}
            icon={<Filter className="h-5 w-5" style={{ color: '#FF7900' }} />}
            borderColor="#8B1538"
            badge={
              hasCompletedActiveFilters ? (
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    clearCompletedFilters();
                  }}
                  className="text-sm font-medium hover:underline"
                  style={{ color: '#FF7900' }}
                >
                  Clear All
                </button>
              ) : undefined
            }
            className="mb-4"
          >
            {renderFilterForm({
              searchInput: completedSearchInput,
              onSearchChange: setCompletedSearchInput,
              category: completedCategory,
              onCategoryChange: setCompletedCategory,
              showDateFilter: false,
              metroIds: completedMetroIds,
              onMetroIdsChange: setCompletedMetroIds,
              treeNodes: completedLocationTreeNodes,
              placeholderPrefix: 'completed events',
            })}
          </CollapsibleSection>

          <div className="relative">
            <div
              data-section="completed-grid-scroll"
              className="max-h-[1500px] overflow-y-auto pr-1"
            >
              {completedLoading ? (
                renderSkeleton()
              ) : completedEvents.length === 0 ? (
                /* Phase 6A.152: empty-state replaces "section hidden". Keeps the
                   feature discoverable for organizers whose first event hasn't
                   ended yet, and for visitors filtering by criteria that match
                   nothing in the past bucket. */
                <div
                  data-testid="completed-empty-state"
                  className="text-center py-12 px-4 bg-neutral-50 rounded-lg border border-neutral-200"
                >
                  <Calendar className="mx-auto h-10 w-10 text-neutral-400 mb-3" aria-hidden="true" />
                  <p className="text-base font-medium text-neutral-700">No completed events yet</p>
                  <p className="text-sm text-neutral-500 mt-1">
                    {hasCompletedActiveFilters
                      ? 'Try adjusting your filters above.'
                      : 'Past events will appear here once they conclude.'}
                  </p>
                </div>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                  {completedEvents.map((event) => (
                    <EventCard key={event.id} event={event} categoryLabels={categoryLabels} />
                  ))}
                </div>
              )}
            </div>
            <div
              data-section="completed-grid-fade"
              className="pointer-events-none absolute bottom-0 left-0 right-0 h-16 bg-gradient-to-t from-white to-transparent"
              aria-hidden="true"
            />
          </div>
        </section>
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
 * Issue #2: Fixed to use registration status instead of boolean flag
 */
function EventCard({
  event,
  categoryLabels,
}: {
  event: EventDto;
  categoryLabels: Record<string, string>;
}) {
  // Phase 8YA.3: TBD events render "Date TBD" / "Time TBD" badges (Q1=A —
  // public listing of TBD events with a clear placeholder).
  const startDate = event.startDate ? new Date(event.startDate) : null;
  const formattedDate = startDate
    ? startDate.toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
      })
    : 'Date TBD';
  const formattedTime = startDate
    ? startDate.toLocaleTimeString('en-US', {
        hour: 'numeric',
        minute: '2-digit',
      })
    : 'Time TBD';

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

          {/* Phase 8YB.6 (2026-05-09): "Coming Soon" pill removed per product-owner
              rule overturn — TBD events are treated as regular events. The factual
              "Date TBD" / "Time TBD" text in the date field below is sufficient
              indicator that the schedule isn't confirmed yet. */}

          {/* Registration Badge - Issue #2: Only shows for Confirmed status */}
          <RegistrationBadge registrationStatus={event.userRegistrationStatus} compact={false} />
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
                : event.hasTicketTiers && event.ticketTiers && event.ticketTiers.length > 0
                  ? (() => {
                      const activeTiers = event.ticketTiers.filter(t => t.isActive && !t.isFree);
                      if (activeTiers.length === 0) return 'Free Tiers';
                      const prices = activeTiers.map(t => t.adultPriceAmount);
                      const minPrice = Math.min(...prices);
                      const maxPrice = Math.max(...prices);
                      return minPrice === maxPrice
                        ? `$${minPrice.toFixed(2)}`
                        : `$${minPrice.toFixed(2)}-$${maxPrice.toFixed(2)}`;
                    })()
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
          <div className="flex items-center gap-2">
            {/* Phase 8X.7+8: ExternalPaid badge so users know payment happens off-platform
                before they click through to the event page. */}
            {event.paymentMode === EventPaymentMode.ExternalPaid && (
              <span
                className="inline-flex items-center px-2 py-0.5 text-xs font-medium rounded-full bg-blue-100 text-blue-800"
                title="Payment + registration handled by an external site"
                data-testid="external-payment-badge"
              >
                External payment
              </span>
            )}
            <button
              className="px-4 py-2 rounded-lg text-sm font-medium text-white transition-colors"
              style={{ background: '#FF7900' }}
            >
              {event.paymentMode === EventPaymentMode.ExternalPaid
                // Phase 8YB.6 hotfix #3 (2026-05-09): "Buy on {Vendor}" copy implies a
                // direct path to the vendor — so it must only render when an
                // externalRegistrationUrl is actually set. Without a URL the card click
                // takes the user to the LankaConnect detail page (where they read the
                // organiser's plain-text instructions). Showing "Buy on XYZ →" with no
                // URL was misleading; fall through to the neutral "View Details" copy.
                ? (event.externalRegistrationUrl
                    ? (event.externalRegistrationVendorName
                        ? `Buy on ${event.externalRegistrationVendorName} →`
                        : 'Buy Ticket / Register →')
                    : 'View Details →')
                : event.isFree ? 'View Details / Register →' : 'View Details / Buy Tickets →'}
            </button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
