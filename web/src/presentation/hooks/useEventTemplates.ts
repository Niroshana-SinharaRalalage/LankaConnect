/**
 * Phase 6A.8: Event Template System - React Query Hooks
 *
 * Provides React Query hooks for Event Templates API integration
 * Implements caching and proper error handling
 *
 * @requires @tanstack/react-query
 * @requires eventTemplatesRepository from infrastructure/repositories/event-templates.repository
 * @requires EventTemplateDto types from infrastructure/api/types/event-template.types
 */

import {
  useQuery,
  UseQueryOptions,
} from '@tanstack/react-query';

import { eventTemplatesRepository } from '@/infrastructure/api/repositories/event-templates.repository';
import type {
  EventTemplateDto,
  GetEventTemplatesParams,
} from '@/infrastructure/api/types/event-template.types';

import { ApiError } from '@/infrastructure/api/client/api-errors';

/**
 * Query Keys for Event Templates
 * Centralized query key management for cache invalidation
 */
export const eventTemplateKeys = {
  all: ['eventTemplates'] as const,
  lists: () => [...eventTemplateKeys.all, 'list'] as const,
  list: (filters: any) => [...eventTemplateKeys.lists(), filters] as const,
  details: () => [...eventTemplateKeys.all, 'detail'] as const,
  detail: (id: string) => [...eventTemplateKeys.details(), id] as const,
};

/**
 * useEventTemplates Hook
 *
 * Fetches a list of event templates with optional filters
 *
 * Features:
 * - Automatic caching with 10-minute stale time (templates change infrequently)
 * - Refetch on window focus disabled (templates are relatively static)
 * - Proper error handling with ApiError types
 * - Query key includes filters for granular cache control
 *
 * @param params - Optional parameters for filtering templates (category, isActive)
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * // Get all active templates
 * const { data, isLoading, error } = useEventTemplates();
 *
 * // Get templates for a specific category
 * const { data } = useEventTemplates({
 *   category: 'Cultural',
 *   isActive: true
 * });
 * ```
 */
export function useEventTemplates(
  params?: GetEventTemplatesParams,
  options?: Omit<UseQueryOptions<EventTemplateDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: eventTemplateKeys.list(params || {}),
    queryFn: async () => {
      const result = await eventTemplatesRepository.getEventTemplates(params);
      return result;
    },
    staleTime: 10 * 60 * 1000, // 10 minutes (templates don't change often)
    gcTime: 30 * 60 * 1000, // 30 minutes in cache
    refetchOnWindowFocus: false, // Templates are static, no need to refetch
    ...options,
  });
}

/**
 * useEventTemplate Hook
 *
 * Fetches a single event template by ID
 *
 * @param id - Event template ID
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: template, isLoading } = useEventTemplate(templateId);
 * ```
 */
export function useEventTemplate(
  id: string,
  options?: Omit<UseQueryOptions<EventTemplateDto, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: eventTemplateKeys.detail(id),
    queryFn: async () => {
      const result = await eventTemplatesRepository.getEventTemplateById(id);
      return result;
    },
    staleTime: 10 * 60 * 1000, // 10 minutes
    gcTime: 30 * 60 * 1000, // 30 minutes in cache
    refetchOnWindowFocus: false,
    enabled: !!id, // Only run query if id is provided
    ...options,
  });
}
