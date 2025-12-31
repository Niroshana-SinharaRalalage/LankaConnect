import { useQuery } from '@tanstack/react-query';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import { businessesRepository } from '@/infrastructure/api/repositories/businesses.repository';
import type { PagedResult, PaginatedList } from '@/infrastructure/api/types/common.types';
import type { EventSearchResultDto } from '@/infrastructure/api/types/events.types';
import type { BusinessDto } from '@/infrastructure/api/types/business.types';

/**
 * Unified Search Hook
 * Phase 6A.59: Consolidates search across Events and Business
 *
 * Usage:
 * ```tsx
 * const { data, isLoading, error } = useUnifiedSearch('yoga', 'events', 1);
 * ```
 *
 * @param searchTerm - The search query
 * @param type - Search category ('events' | 'business' | 'forums' | 'marketplace')
 * @param page - Current page number (default: 1)
 * @param pageSize - Results per page (default: 20)
 */
export function useUnifiedSearch(
  searchTerm: string,
  type: 'events' | 'business' | 'forums' | 'marketplace',
  page: number = 1,
  pageSize: number = 20
) {
  return useQuery({
    queryKey: ['unified-search', searchTerm, type, page, pageSize],
    queryFn: async (): Promise<UnifiedSearchResult> => {
      if (!searchTerm.trim()) {
        // Return empty result if no search term
        return createEmptyResult(page, pageSize);
      }

      if (type === 'events') {
        // Call Events search API
        const result = await eventsRepository.searchEvents({
          searchTerm,
          page,
          pageSize,
        });

        // Convert PagedResult to unified format
        return {
          items: result.items,
          pageNumber: result.page,
          totalPages: result.totalPages,
          totalCount: result.totalCount,
          hasPreviousPage: result.hasPreviousPage,
          hasNextPage: result.hasNextPage,
        };
      } else if (type === 'business') {
        // Call Business search API
        const result = await businessesRepository.search({
          searchTerm,
          pageNumber: page,
          pageSize,
        });

        // Business API returns PaginatedList format (already matches unified format)
        return result;
      } else {
        // Forums and Marketplace not implemented yet
        return createEmptyResult(page, pageSize);
      }
    },
    enabled: !!searchTerm.trim(), // Only run query if search term exists
    staleTime: 30000, // Cache results for 30 seconds
  });
}

/**
 * Unified Search Result Type
 * Normalizes different API response formats into a single structure
 */
export type UnifiedSearchResult = {
  items: readonly (EventSearchResultDto | BusinessDto)[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

/**
 * Helper: Create empty result for unsupported search types or empty queries
 */
function createEmptyResult(page: number, pageSize: number): UnifiedSearchResult {
  return {
    items: [],
    pageNumber: page,
    totalPages: 0,
    totalCount: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  };
}
