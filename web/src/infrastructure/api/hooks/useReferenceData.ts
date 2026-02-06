/**
 * React Query hooks for Reference Data
 * Phase 6A.47: Database-driven reference data with React Query caching
 */

import { useQuery } from '@tanstack/react-query';
import { getEventInterests, getReferenceDataByTypes, getCommissionSettings } from '../services/referenceData.service';
import type { EventInterestOption, ReferenceValue, CommissionSettingsDto } from '../services/referenceData.service';

/**
 * Query key factory for reference data
 */
export const referenceDataKeys = {
  all: ['referenceData'] as const,
  byTypes: (types: string[], activeOnly?: boolean) =>
    [...referenceDataKeys.all, 'byTypes', types, activeOnly] as const,
  eventInterests: () => [...referenceDataKeys.all, 'eventInterests'] as const,
  commissionSettings: () => [...referenceDataKeys.all, 'commissionSettings'] as const,
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

/**
 * Hook to fetch Event Categories for dropdowns
 * Returns category options with code, name, and intValue
 *
 * @example
 * const { data: categories, isLoading } = useEventCategories();
 */
export function useEventCategories() {
  return useQuery<ReferenceValue[], Error>({
    queryKey: referenceDataKeys.byTypes(['EventCategory'], true),
    queryFn: () => getReferenceDataByTypes(['EventCategory'], true),
    staleTime: 1000 * 60 * 60, // 1 hour (matches backend cache)
    gcTime: 1000 * 60 * 60 * 24, // 24 hours (React Query v5: gcTime replaces cacheTime)
  });
}

/**
 * Hook to fetch Event Statuses for dropdowns
 * Returns status options with code, name, and intValue
 *
 * @example
 * const { data: statuses, isLoading } = useEventStatuses();
 */
export function useEventStatuses() {
  return useQuery<ReferenceValue[], Error>({
    queryKey: referenceDataKeys.byTypes(['EventStatus'], true),
    queryFn: () => getReferenceDataByTypes(['EventStatus'], true),
    staleTime: 1000 * 60 * 60, // 1 hour (matches backend cache)
    gcTime: 1000 * 60 * 60 * 24, // 24 hours (React Query v5: gcTime replaces cacheTime)
  });
}

/**
 * Hook to fetch Currency reference data for dropdowns
 * Returns currency options with code, name, and intValue
 * Phase 6A.47: Added to replace hardcoded Currency dropdowns
 *
 * @example
 * const { data: currencies, isLoading } = useCurrencies();
 */
export function useCurrencies() {
  return useQuery<ReferenceValue[], Error>({
    queryKey: referenceDataKeys.byTypes(['Currency'], true),
    queryFn: () => getReferenceDataByTypes(['Currency'], true),
    staleTime: 1000 * 60 * 60, // 1 hour (matches backend cache)
    gcTime: 1000 * 60 * 60 * 24, // 24 hours (React Query v5: gcTime replaces cacheTime)
  });
}

/**
 * Hook to fetch Commission Settings for revenue breakdown calculations
 * Phase 6A.X: Fee rates are configurable in appsettings.json
 *
 * @example
 * const { data: settings, isLoading } = useCommissionSettings();
 */
export function useCommissionSettings() {
  return useQuery<CommissionSettingsDto, Error>({
    queryKey: referenceDataKeys.commissionSettings(),
    queryFn: getCommissionSettings,
    staleTime: 1000 * 60 * 60, // 1 hour (matches backend cache)
    gcTime: 1000 * 60 * 60 * 24, // 24 hours
  });
}
