/**
 * Phase 6A.87: Email Metrics Dashboard Hooks
 * React Query hooks for email metrics API endpoints
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { emailMetricsRepository } from '@/infrastructure/api/repositories/email-metrics.repository';

// Query key factory for cache management
export const emailMetricsKeys = {
  all: ['emailMetrics'] as const,
  summary: () => [...emailMetricsKeys.all, 'summary'] as const,
  templateStats: () => [...emailMetricsKeys.all, 'templateStats'] as const,
  templateStatsByName: (name: string) => [...emailMetricsKeys.all, 'templateStats', name] as const,
  failures: () => [...emailMetricsKeys.all, 'failures'] as const,
  validationFailures: () => [...emailMetricsKeys.all, 'validationFailures'] as const,
  migrationProgress: () => [...emailMetricsKeys.all, 'migrationProgress'] as const,
};

/**
 * Hook to fetch email metrics summary
 */
export function useEmailMetricsSummary() {
  return useQuery({
    queryKey: emailMetricsKeys.summary(),
    queryFn: () => emailMetricsRepository.getSummary(),
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchOnWindowFocus: false,
  });
}

/**
 * Hook to fetch stats for all templates
 */
export function useEmailTemplateStats() {
  return useQuery({
    queryKey: emailMetricsKeys.templateStats(),
    queryFn: () => emailMetricsRepository.getTemplateStats(),
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchOnWindowFocus: false,
  });
}

/**
 * Hook to fetch stats for a specific template
 */
export function useEmailTemplateStatsByName(templateName: string) {
  return useQuery({
    queryKey: emailMetricsKeys.templateStatsByName(templateName),
    queryFn: () => emailMetricsRepository.getTemplateStatsByName(templateName),
    enabled: !!templateName,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

/**
 * Hook to fetch recent email failures
 * Phase 6A.99: Added optional unmasked param for SuperAdmin
 */
export function useEmailFailures(params?: { unmasked?: boolean }) {
  return useQuery({
    queryKey: [...emailMetricsKeys.failures(), params?.unmasked],
    queryFn: () => emailMetricsRepository.getFailures(params),
    staleTime: 1 * 60 * 1000, // 1 minute - failures need fresh data
    refetchOnWindowFocus: false,
  });
}

/**
 * Hook to fetch validation failures
 */
export function useEmailValidationFailures() {
  return useQuery({
    queryKey: emailMetricsKeys.validationFailures(),
    queryFn: () => emailMetricsRepository.getValidationFailures(),
    staleTime: 1 * 60 * 1000, // 1 minute
    refetchOnWindowFocus: false,
  });
}

/**
 * Hook to fetch migration progress
 */
export function useEmailMigrationProgress() {
  return useQuery({
    queryKey: emailMetricsKeys.migrationProgress(),
    queryFn: () => emailMetricsRepository.getMigrationProgress(),
    staleTime: 5 * 60 * 1000, // 5 minutes - migration status changes less frequently
    refetchOnWindowFocus: false,
  });
}

/**
 * Hook to reset metrics (Dev/Staging only)
 */
export function useResetEmailMetrics() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => emailMetricsRepository.resetMetrics(),
    onSuccess: () => {
      // Invalidate all email metrics queries
      queryClient.invalidateQueries({ queryKey: emailMetricsKeys.all });
    },
  });
}

/**
 * Hook to manually refresh all email metrics data
 */
export function useRefreshEmailMetrics() {
  const queryClient = useQueryClient();

  return () => {
    queryClient.invalidateQueries({ queryKey: emailMetricsKeys.all });
  };
}
