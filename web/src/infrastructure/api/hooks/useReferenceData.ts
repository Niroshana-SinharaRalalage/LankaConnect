/**
 * React Query hooks for Reference Data
 * Phase 6A.47: Database-driven reference data with React Query caching
 */

import { useQuery } from '@tanstack/react-query';
import { getEventInterests, getReferenceDataByTypes } from '../services/referenceData.service';
import type { EventInterestOption, ReferenceValue } from '../services/referenceData.service';

/**
 * Query key factory for reference data
 */
export const referenceDataKeys = {
  all: ['referenceData'] as const,
  byTypes: (types: string[], activeOnly?: boolean) =>
    [...referenceDataKeys.all, 'byTypes', types, activeOnly] as const,
  eventInterests: () => [...referenceDataKeys.all, 'eventInterests'] as const,
};

/**
 * Hook to fetch reference data by enum types
 * @param types - Array of enum types to fetch
 * @param activeOnly - Return only active values (default: true)
 *
 * @example
 * const { data: refData } = useReferenceData(['EventCategory', 'EventStatus']);
 */
export function useReferenceData(
  types: string[],
  activeOnly: boolean = true
) {
  return useQuery<ReferenceValue[], Error>({
    queryKey: referenceDataKeys.byTypes(types, activeOnly),
    queryFn: () => getReferenceDataByTypes(types, activeOnly),
    staleTime: 1000 * 60 * 60, // 1 hour (matches backend cache)
    gcTime: 1000 * 60 * 60 * 24, // 24 hours (React Query v5: gcTime replaces cacheTime)
  });
}

/**
 * Hook to fetch Event Interests (EventCategory)
 * Replaces hardcoded CULTURAL_INTERESTS constant
 *
 * @example
 * const { data: eventInterests, isLoading } = useEventInterests();
 */
export function useEventInterests() {
  return useQuery<EventInterestOption[], Error>({
    queryKey: referenceDataKeys.eventInterests(),
    queryFn: getEventInterests,
    staleTime: 1000 * 60 * 60, // 1 hour (matches backend cache)
    gcTime: 1000 * 60 * 60 * 24, // 24 hours (React Query v5: gcTime replaces cacheTime)
  });
}
