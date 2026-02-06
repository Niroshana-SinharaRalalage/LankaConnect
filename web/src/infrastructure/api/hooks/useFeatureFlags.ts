/**
 * Phase 6A.95: React Query hooks for Feature Flags
 * Fetches feature flag configuration from backend
 */

import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../client/api-client';

/**
 * Feature flags response from /api/configuration/features
 */
export interface FeatureFlagsDto {
  /** Whether sales tax collection is enabled */
  salesTaxEnabled: boolean;
}

/**
 * Commission settings with feature flags from /api/configuration/commission-settings
 */
export interface CommissionSettingsWithFlagsDto {
  platformCommissionRate: number;
  stripeFeeRate: number;
  stripeFeeFixed: number;
  salesTaxEnabled: boolean;
}

/**
 * Query key factory for feature flags
 */
export const featureFlagsKeys = {
  all: ['featureFlags'] as const,
  features: () => [...featureFlagsKeys.all, 'features'] as const,
  commissionSettings: () => [...featureFlagsKeys.all, 'commissionSettings'] as const,
};

/**
 * Fetch feature flags from backend
 */
async function getFeatureFlags(): Promise<FeatureFlagsDto> {
  const data = await apiClient.get<FeatureFlagsDto>('/configuration/features');
  return data;
}

/**
 * Fetch commission settings with feature flags from backend
 * Phase 6A.95: Includes salesTaxEnabled flag
 */
async function getCommissionSettingsWithFlags(): Promise<CommissionSettingsWithFlagsDto> {
  const data = await apiClient.get<CommissionSettingsWithFlagsDto>('/configuration/commission-settings');
  return data;
}

/**
 * Hook to fetch feature flags configuration
 * Phase 6A.95: Returns feature flags from backend
 *
 * @example
 * const { data: flags, isLoading } = useFeatureFlags();
 * if (flags?.salesTaxEnabled) { ... }
 */
export function useFeatureFlags() {
  return useQuery<FeatureFlagsDto, Error>({
    queryKey: featureFlagsKeys.features(),
    queryFn: getFeatureFlags,
    staleTime: 1000 * 60, // 1 minute (feature flags should be fairly fresh)
    gcTime: 1000 * 60 * 5, // 5 minutes cache
  });
}

/**
 * Hook to fetch commission settings with feature flags
 * Phase 6A.95: Includes salesTaxEnabled flag for revenue preview
 *
 * @example
 * const { data: settings, isLoading } = useCommissionSettingsWithFlags();
 */
export function useCommissionSettingsWithFlags() {
  return useQuery<CommissionSettingsWithFlagsDto, Error>({
    queryKey: featureFlagsKeys.commissionSettings(),
    queryFn: getCommissionSettingsWithFlags,
    staleTime: 1000 * 60 * 5, // 5 minutes (commission settings don't change often)
    gcTime: 1000 * 60 * 60, // 1 hour cache
  });
}

/**
 * Convenience hook that returns just the salesTaxEnabled flag
 * Phase 6A.95: Use this for simple boolean checks
 *
 * @example
 * const salesTaxEnabled = useSalesTaxEnabled();
 * if (salesTaxEnabled) { ... }
 */
export function useSalesTaxEnabled(): boolean {
  const { data } = useFeatureFlags();
  return data?.salesTaxEnabled ?? false;
}
