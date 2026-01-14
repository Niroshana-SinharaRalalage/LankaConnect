'use client';

import { useState, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Badge } from '@/presentation/components/ui/Badge';
import { TreeDropdown, type TreeNode } from '@/presentation/components/ui/TreeDropdown';
import { Calendar, MapPin, Mail, Search } from 'lucide-react';
import { usePublishedNewslettersWithFilters } from '@/presentation/hooks/useNewsletters';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useGeolocation } from '@/presentation/hooks/useGeolocation';
import { useMetroAreas } from '@/presentation/hooks/useMetroAreas';
import { NewsletterDto, NewsletterStatus } from '@/infrastructure/api/types/newsletters.types';
import { US_STATES } from '@/domain/constants/metroAreas.constants';
import { useDebounce } from '@/hooks/useDebounce';

/**
 * Public Newsletters Discovery Page
 * Phase 6A.74 Parts 10 & 11
 * Route: /newsletters
 */
export default function DiscoverNewslettersPage() {
  const router = useRouter();
  const { user } = useAuthStore();

  const isAnonymous = !user?.userId;
  const { latitude, longitude, loading: locationLoading } = useGeolocation(isAnonymous);

  const {
    metroAreasByState,
    stateLevelMetros,
    isLoading: metrosLoading,
  } = useMetroAreas();

  const [selectedMetroIds, setSelectedMetroIds] = useState<string[]>([]);
  const [selectedState, setSelectedState] = useState<string | undefined>(undefined);
  const [dateFilter, setDateFilter] = useState<'all' | 'past-week' | 'past-month'>('all');
  const [searchInput, setSearchInput] = useState<string>('');

  const debouncedSearchTerm = useDebounce(searchInput, 500);

  const dateRange = useMemo(() => {
    const now = new Date();
    if (dateFilter === 'past-week') {
      const weekAgo = new Date(now);
      weekAgo.setDate(weekAgo.getDate() - 7);
      return { publishedFrom: weekAgo, publishedTo: now };
    }
    if (dateFilter === 'past-month') {
      const monthAgo = new Date(now);
      monthAgo.setMonth(monthAgo.getMonth() - 1);
      return { publishedFrom: monthAgo, publishedTo: now };
    }
    return {};
  }, [dateFilter]);

  const stableMetroIds = useMemo(() =>
    selectedMetroIds.length > 0 ? selectedMetroIds : undefined,
    [selectedMetroIds.length, ...selectedMetroIds]
  );

  const filters = useMemo(() => {
    return {
      searchTerm: debouncedSearchTerm || undefined,
      userId: user?.userId,
      latitude: isAnonymous ? latitude ?? undefined : undefined,
      longitude: isAnonymous ? longitude ?? undefined : undefined,
      metroAreaIds: stableMetroIds,
      state: selectedState,
      ...dateRange,
    };
  }, [debouncedSearchTerm, user?.userId, isAnonymous, latitude, longitude, stableMetroIds, selectedState, dateRange]);

  const { data: newsletters, isLoading: newslettersLoading, error: newslettersError } = usePublishedNewslettersWithFilters(filters);

  const locationTree = useMemo((): TreeNode[] => {
    const stateNodes = US_STATES.map(state => {
      const stateMetros = metroAreasByState.get(state.code) || [];
      const stateLevelMetro = stateLevelMetros.find(m => m.state === state.code);
      const stateId = stateLevelMetro?.id || state.code;

      return {
        id: stateId,
        label: `All ${state.name}`,
        checked: selectedMetroIds.includes(stateId),
        children: stateMetros.map(metro => ({
          id: metro.id,
          label: `${metro.name}, ${metro.state}`,
          checked: selectedMetroIds.includes(metro.id),
        })),
      };
    });

    return [
      { id: 'all-locations', label: 'All Locations', checked: selectedMetroIds.length === 0 },
      ...stateNodes,
    ];
  }, [metroAreasByState, stateLevelMetros, selectedMetroIds]);

  const handleLocationChange = (selectedIds: string[]) => {
    if (selectedIds.includes('all-locations')) {
      setSelectedMetroIds([]);
      setSelectedState(undefined);
      return;
    }

    // Check if any selected ID is a state-level metro
    const selectedStateLevelMetro = selectedIds.find(id =>
      stateLevelMetros.some(metro => metro.id === id)
    );

    if (selectedStateLevelMetro) {
      const metro = stateLevelMetros.find(m => m.id === selectedStateLevelMetro);
      setSelectedState(metro?.state);
      setSelectedMetroIds([]);
    } else {
      setSelectedState(undefined);
      setSelectedMetroIds(selectedIds);
    }
  };

  const isLoading = newslettersLoading || locationLoading || metrosLoading;

  const formatDate = (dateString: string | null) => {
    if (!dateString) return 'Not published';
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  const getStatusBadgeColor = (status: NewsletterStatus) => {
    if (status === NewsletterStatus.Active) return 'bg-green-100 text-green-800';
    return 'bg-gray-100 text-gray-800';
  };

  const getStatusLabel = (status: NewsletterStatus) => {
    if (status === NewsletterStatus.Active) return 'Active';
    if (status === NewsletterStatus.Sent) return 'Sent';
    return 'Active';
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <Header />

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">
            Published Newsletters
          </h1>
          <p className="text-gray-600">
            Discover the latest news and updates from our community
          </p>
        </div>

        <div className="mb-6 flex flex-col sm:flex-row gap-4">
          <div className="sm:flex-1 sm:max-w-md">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-5 h-5" />
              <input
                type="text"
                placeholder="Search newsletters..."
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-orange-500 focus:border-transparent"
              />
            </div>
          </div>

          <TreeDropdown
            nodes={locationTree}
            selectedIds={
              selectedState
                ? [selectedState]
                : selectedMetroIds.length > 0
                ? selectedMetroIds
                : ['all-locations']
            }
            onSelectionChange={handleLocationChange}
            placeholder="Location"
          />

          <select
            value={dateFilter}
            onChange={(e) => setDateFilter(e.target.value as any)}
            className="px-4 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-orange-500 focus:border-transparent"
          >
            <option value="all">All Time</option>
            <option value="past-week">Past Week</option>
            <option value="past-month">Past Month</option>
          </select>
        </div>

        {isLoading ? (
          <div className="text-center py-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-orange-600 mx-auto"></div>
            <p className="mt-4 text-gray-600">Loading newsletters...</p>
          </div>
        ) : newslettersError ? (
          <Card>
            <CardContent className="py-12 text-center">
              <p className="text-red-600">Error loading newsletters. Please try again later.</p>
            </CardContent>
          </Card>
        ) : !newsletters || newsletters.length === 0 ? (
          <Card>
            <CardContent className="py-12 text-center">
              <Mail className="w-16 h-16 text-gray-400 mx-auto mb-4" />
              <p className="text-gray-600 text-lg mb-2">No newsletters found</p>
              <p className="text-gray-500 text-sm">
                {debouncedSearchTerm || selectedMetroIds.length > 0
                  ? 'Try adjusting your filters'
                  : 'Check back later for new newsletters'}
              </p>
            </CardContent>
          </Card>
        ) : (
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {newsletters.map((newsletter) => (
              <Card
                key={newsletter.id}
                className="hover:shadow-lg transition-shadow cursor-pointer"
                onClick={() => router.push(`/newsletters/${newsletter.id}`)}
              >
                <CardHeader>
                  <div className="flex justify-between items-start mb-2">
                    <Badge className={getStatusBadgeColor(newsletter.status)}>
                      {getStatusLabel(newsletter.status)}
                    </Badge>
                    <Calendar className="w-4 h-4 text-gray-400" />
                  </div>
                  <CardTitle className="text-lg line-clamp-2">
                    {newsletter.title}
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div
                    className="text-sm text-gray-600 line-clamp-3 mb-4"
                    dangerouslySetInnerHTML={{
                      __html: newsletter.description.replace(/<[^>]*>/g, ' ').substring(0, 150) + '...'
                    }}
                  />
                  <div className="flex items-center justify-between text-xs text-gray-500">
                    <span>{formatDate(newsletter.publishedAt)}</span>
                    {newsletter.eventId && (
                      <span className="flex items-center">
                        <Calendar className="w-3 h-3 mr-1" />
                        Linked Event
                      </span>
                    )}
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}

        {newsletters && newsletters.length > 0 && (
          <div className="mt-6 text-center text-sm text-gray-600">
            Showing {newsletters.length} newsletter{newsletters.length !== 1 ? 's' : ''}
          </div>
        )}
      </main>

      <Footer />
    </div>
  );
}
