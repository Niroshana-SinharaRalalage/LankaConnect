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
// Phase 6A.74 Part 10 Issue #1 Fix: Import enum helpers for string/number comparison
import { isNewsletterActive, isNewsletterSent } from '@/lib/enum-utils';

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

  // Issue #5 Fix: Remove selectedState - only use selectedMetroIds like /events page
  const [selectedMetroIds, setSelectedMetroIds] = useState<string[]>([]);
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

  // Issue #5 Fix: Remove selectedState from filters - only use metroAreaIds
  const filters = useMemo(() => {
    return {
      searchTerm: debouncedSearchTerm || undefined,
      userId: user?.userId,
      latitude: isAnonymous ? latitude ?? undefined : undefined,
      longitude: isAnonymous ? longitude ?? undefined : undefined,
      metroAreaIds: stableMetroIds,
      ...dateRange,
    };
  }, [debouncedSearchTerm, user?.userId, isAnonymous, latitude, longitude, stableMetroIds, dateRange]);

  const { data: newsletters, isLoading: newslettersLoading, error: newslettersError } = usePublishedNewslettersWithFilters(filters);

  // Issue #5 Fix: Simplify location tree to match /events page pattern exactly
  const locationTree = useMemo((): TreeNode[] => {
    const nodes: TreeNode[] = [];

    US_STATES.forEach((state) => {
      const metrosForState = metroAreasByState.get(state.code) || [];
      if (metrosForState.length === 0) return;

      const stateMetro = metrosForState.find((m) => m.isStateLevelArea);
      const cityMetros = metrosForState.filter((m) => !m.isStateLevelArea);

      const children: TreeNode[] = [];

      if (stateMetro) {
        children.push({
          id: stateMetro.id,
          label: `All of ${state.name}`,
          checked: selectedMetroIds.includes(stateMetro.id),
        });
      }

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

  // Issue #5 Fix: Simplify handler to match /events page - just update selectedMetroIds
  const handleLocationChange = (newSelectedIds: string[]) => {
    console.log('[Newsletters] Location selection changed:', newSelectedIds);
    setSelectedMetroIds(newSelectedIds);
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

  // Phase 6A.74 Part 10 Issue #1 Fix: Use enum helpers for string/number comparison
  const getStatusBadgeColor = (status: NewsletterStatus | string) => {
    if (isNewsletterActive(status)) return 'bg-green-100 text-green-800';
    if (isNewsletterSent(status)) return 'bg-blue-100 text-blue-800';
    return 'bg-gray-100 text-gray-800';
  };

  const getStatusLabel = (status: NewsletterStatus | string) => {
    if (isNewsletterActive(status)) return 'Active';
    if (isNewsletterSent(status)) return 'Sent';
    return 'Published';
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

          {/* Issue #5 Fix: Use selectedMetroIds directly like /events page */}
          <div className="min-w-[300px]">
            <TreeDropdown
              nodes={locationTree}
              selectedIds={selectedMetroIds}
              onSelectionChange={handleLocationChange}
              placeholder="Select location"
              maxSelections={20}
              disabled={isLoading || metrosLoading}
              className="w-full"
            />
          </div>

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
          /* Issue #7: Changed from 3-column grid to single-column list (one newsletter per row) */
          <div className="space-y-4">
            {newsletters.map((newsletter) => (
              <Card
                key={newsletter.id}
                className="hover:shadow-md transition-shadow cursor-pointer"
                onClick={() => router.push(`/newsletters/${newsletter.id}`)}
              >
                <div className="flex items-start gap-4 p-4">
                  {/* Fix Issue #1: Removed status badge - public page should not show internal status */}

                  {/* Title and description */}
                  <div className="flex-1 min-w-0">
                    <h3 className="text-lg font-semibold text-gray-900 line-clamp-1 mb-1">
                      {newsletter.title}
                    </h3>
                    <div
                      className="text-sm text-gray-600 line-clamp-2"
                      dangerouslySetInnerHTML={{
                        __html: newsletter.description.replace(/<[^>]*>/g, ' ').substring(0, 200) + '...'
                      }}
                    />
                  </div>

                  {/* Right side: Date and event indicator */}
                  <div className="flex-shrink-0 text-right">
                    <div className="flex items-center text-sm text-gray-500 mb-1">
                      <Calendar className="w-4 h-4 mr-1" />
                      {formatDate(newsletter.publishedAt)}
                    </div>
                    {newsletter.eventId && (
                      <span className="inline-flex items-center text-xs text-blue-600 bg-blue-50 px-2 py-1 rounded">
                        <Calendar className="w-3 h-3 mr-1" />
                        Linked Event
                      </span>
                    )}
                  </div>
                </div>
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
