/**
 * Newsletters React Query Hooks
 *
 * Provides React Query hooks for Newsletters API integration
 * Implements caching, optimistic updates, and proper error handling
 *
 * Phase 6A.74: Newsletter/News Alert System
 *
 * @requires @tanstack/react-query
 * @requires newslettersRepository from infrastructure/repositories/newsletters.repository
 * @requires Newsletter types from infrastructure/api/types/newsletters.types
 */

import {
  useQuery,
  useMutation,
  useQueryClient,
  UseQueryOptions,
  UseMutationOptions,
} from '@tanstack/react-query';

import { newslettersRepository } from '@/infrastructure/api/repositories/newsletters.repository';
import type {
  NewsletterDto,
  CreateNewsletterRequest,
  UpdateNewsletterRequest,
  RecipientPreviewDto,
  GetNewslettersFilters,
} from '@/infrastructure/api/types/newsletters.types';

import { ApiError } from '@/infrastructure/api/client/api-errors';

/**
 * Query Keys for Newsletters
 * Centralized query key management for cache invalidation
 */
export const newsletterKeys = {
  all: ['newsletters'] as const,
  lists: () => [...newsletterKeys.all, 'list'] as const,
  myNewsletters: () => [...newsletterKeys.lists(), 'my-newsletters'] as const,
  published: () => [...newsletterKeys.lists(), 'published'] as const,
  details: () => [...newsletterKeys.all, 'detail'] as const,
  detail: (id: string) => [...newsletterKeys.details(), id] as const,
  byEvent: (eventId: string) => [...newsletterKeys.all, 'by-event', eventId] as const,
  recipientPreview: (id: string) => [...newsletterKeys.all, 'recipient-preview', id] as const,
};

/**
 * useMyNewsletters Hook
 *
 * Fetches all newsletters created by the current user
 *
 * Features:
 * - Automatic caching with 2-minute stale time
 * - Refetch on window focus
 * - Proper error handling with ApiError types
 *
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: newsletters, isLoading, error } = useMyNewsletters();
 * ```
 */
export function useMyNewsletters(
  options?: Omit<UseQueryOptions<NewsletterDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: newsletterKeys.myNewsletters(),
    queryFn: () => newslettersRepository.getMyNewsletters(),
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchOnWindowFocus: true,
    retry: 1,
    ...options,
  });
}

/**
 * useNewsletterById Hook
 *
 * Fetches a single newsletter by ID
 *
 * Features:
 * - Caches individual newsletter details
 * - Longer stale time (5 minutes) for detail pages
 * - Automatic refetch on window focus
 * - Enabled only when ID is provided
 *
 * @param id - Newsletter ID (GUID)
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: newsletter, isLoading } = useNewsletterById(newsletterId);
 * ```
 */
export function useNewsletterById(
  id: string | undefined,
  options?: Omit<UseQueryOptions<NewsletterDto, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: newsletterKeys.detail(id || ''),
    queryFn: () => newslettersRepository.getNewsletterById(id!),
    enabled: !!id, // Only fetch when ID is provided
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true,
    ...options,
  });
}

/**
 * useNewslettersByEvent Hook
 *
 * Fetches all newsletters associated with a specific event
 * Phase 6A.74 Enhancement 3: Used in Event Management Communications tab
 *
 * Features:
 * - Separate cache for event-specific newsletters
 * - 3-minute stale time
 * - Only enabled when eventId is provided
 *
 * @param eventId - Event ID (GUID)
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: newsletters } = useNewslettersByEvent(eventId);
 * ```
 */
export function useNewslettersByEvent(
  eventId: string | undefined,
  options?: Omit<UseQueryOptions<NewsletterDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: newsletterKeys.byEvent(eventId || ''),
    queryFn: () => newslettersRepository.getNewslettersByEvent(eventId!),
    enabled: !!eventId, // Only fetch when eventId is provided
    staleTime: 3 * 60 * 1000, // 3 minutes
    refetchOnWindowFocus: true,
    retry: 1,
    ...options,
  });
}

/**
 * useRecipientPreview Hook
 *
 * Fetches recipient preview for a newsletter
 * Shows deduplicated recipient count with location-based breakdown
 *
 * Features:
 * - Preview recipients before sending
 * - 1-minute stale time (shorter for accuracy)
 * - Only enabled when ID is provided
 *
 * @param id - Newsletter ID (GUID)
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: preview, isLoading } = useRecipientPreview(newsletterId);
 * if (preview) {
 *   console.log(`Total recipients: ${preview.totalRecipients}`);
 * }
 * ```
 */
export function useRecipientPreview(
  id: string | undefined,
  options?: Omit<UseQueryOptions<RecipientPreviewDto, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: newsletterKeys.recipientPreview(id || ''),
    queryFn: () => newslettersRepository.getRecipientPreview(id!),
    enabled: !!id, // Only fetch when ID is provided
    staleTime: 1 * 60 * 1000, // 1 minute
    refetchOnWindowFocus: false, // Don't auto-refetch preview
    ...options,
  });
}

/**
 * usePublishedNewsletters Hook
 *
 * Fetches all published (Active) newsletters for public landing page
 * Phase 6A.74 Part 5B: Public newsletter display
 *
 * Features:
 * - Public endpoint (no authentication required)
 * - 5-minute stale time for landing page performance
 * - Returns only Active newsletters sorted by publishedAt desc
 * - Refetch on window focus for fresh content
 *
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: newsletters, isLoading } = usePublishedNewsletters();
 * ```
 */
export function usePublishedNewsletters(
  options?: Omit<UseQueryOptions<NewsletterDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: newsletterKeys.published(),
    queryFn: () => newslettersRepository.getPublishedNewsletters(),
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true,
    retry: 1,
    ...options,
  });
}

/**
 * usePublishedNewslettersWithFilters Hook
 *
 * Fetches published newsletters with advanced filtering support
 * Phase 6A.74 Parts 10 & 11: Public newsletter list page
 *
 * Features:
 * - Location-based filtering (metro areas, state)
 * - Search term filtering (title + description)
 * - Date range filtering (publishedFrom, publishedTo)
 * - Public endpoint (no authentication required)
 * - 2-minute stale time for list page performance
 * - Dynamic cache keys based on filter parameters
 *
 * @param filters - Filter parameters for newsletters
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: newsletters, isLoading } = usePublishedNewslettersWithFilters({
 *   searchTerm: 'christmas',
 *   metroAreaIds: ['metro-id-1', 'metro-id-2']
 * });
 * ```
 */
export function usePublishedNewslettersWithFilters(
  filters?: GetNewslettersFilters,
  options?: Omit<UseQueryOptions<NewsletterDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: [...newsletterKeys.published(), 'filtered', filters] as const,
    queryFn: () => newslettersRepository.getPublishedNewslettersWithFilters(filters),
    staleTime: 2 * 60 * 1000, // 2 minutes (shorter than simple list for freshness)
    refetchOnWindowFocus: true,
    retry: 1,
    ...options,
  });
}

/**
 * useCreateNewsletter Hook
 *
 * Mutation hook for creating a new newsletter in Draft status
 *
 * Features:
 * - Automatic cache invalidation after success
 * - Proper error handling
 * - Success/error callbacks
 *
 * @param options - Additional React Query mutation options
 *
 * @example
 * ```tsx
 * const createNewsletter = useCreateNewsletter();
 *
 * const newsletterId = await createNewsletter.mutateAsync({
 *   title: 'Important Update',
 *   description: 'Newsletter content here...',
 *   includeNewsletterSubscribers: true,
 *   targetAllLocations: false,
 *   metroAreaIds: ['colombo-id']
 * });
 * ```
 */
export function useCreateNewsletter() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateNewsletterRequest) => newslettersRepository.createNewsletter(data),
    onSuccess: () => {
      // Invalidate my newsletters list to refetch with new newsletter
      queryClient.invalidateQueries({ queryKey: newsletterKeys.myNewsletters() });
    },
  });
}

/**
 * useUpdateNewsletter Hook
 *
 * Mutation hook for updating a draft newsletter
 * Only Draft newsletters can be updated
 *
 * Features:
 * - Optimistic updates
 * - Automatic cache invalidation
 * - Rollback on error
 *
 * @param options - Additional React Query mutation options
 *
 * @example
 * ```tsx
 * const updateNewsletter = useUpdateNewsletter();
 *
 * await updateNewsletter.mutateAsync({
 *   id: 'newsletter-123',
 *   title: 'Updated Title',
 *   description: 'Updated content',
 *   includeNewsletterSubscribers: true,
 *   targetAllLocations: true
 * });
 * ```
 */
export function useUpdateNewsletter() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, ...data }: { id: string } & UpdateNewsletterRequest) =>
      newslettersRepository.updateNewsletter(id, data),
    onMutate: async ({ id, ...newData }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: newsletterKeys.detail(id) });

      // Snapshot previous value for rollback
      const previousNewsletter = queryClient.getQueryData(newsletterKeys.detail(id));

      // Optimistically update
      queryClient.setQueryData(newsletterKeys.detail(id), (old: NewsletterDto | undefined) => {
        if (!old) return old;
        return {
          ...old,
          ...newData,
          updatedAt: new Date().toISOString(),
        };
      });

      return { previousNewsletter };
    },
    onError: (err, { id }, context) => {
      // Rollback on error
      if (context?.previousNewsletter) {
        queryClient.setQueryData(newsletterKeys.detail(id), context.previousNewsletter);
      }
    },
    onSuccess: (_data, variables) => {
      // Invalidate affected queries
      queryClient.invalidateQueries({ queryKey: newsletterKeys.detail(variables.id) });
      queryClient.invalidateQueries({ queryKey: newsletterKeys.myNewsletters() });
    },
  });
}

/**
 * useDeleteNewsletter Hook
 *
 * Mutation hook for deleting a draft newsletter
 * Only Draft newsletters can be deleted
 *
 * Features:
 * - Immediate cache removal
 * - Automatic list invalidation
 * - Rollback on error
 *
 * @param options - Additional React Query mutation options
 *
 * @example
 * ```tsx
 * const deleteNewsletter = useDeleteNewsletter();
 *
 * await deleteNewsletter.mutateAsync('newsletter-123');
 * ```
 */
export function useDeleteNewsletter() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => newslettersRepository.deleteNewsletter(id),
    onMutate: async (id) => {
      // Cancel queries
      await queryClient.cancelQueries({ queryKey: newsletterKeys.detail(id) });

      // Snapshot for rollback
      const previousNewsletter = queryClient.getQueryData(newsletterKeys.detail(id));

      // Remove from cache immediately
      queryClient.removeQueries({ queryKey: newsletterKeys.detail(id) });

      return { previousNewsletter };
    },
    onError: (err, id, context) => {
      // Restore on error
      if (context?.previousNewsletter) {
        queryClient.setQueryData(newsletterKeys.detail(id), context.previousNewsletter);
      }
    },
    onSuccess: () => {
      // Invalidate lists
      queryClient.invalidateQueries({ queryKey: newsletterKeys.myNewsletters() });
    },
  });
}

/**
 * usePublishNewsletter Hook
 *
 * Mutation hook for publishing a newsletter (Draft → Active)
 * Sets 7-day expiration and makes newsletter visible
 *
 * Features:
 * - Automatic cache invalidation
 * - Proper error handling
 * - Updates newsletter status in cache
 *
 * @param options - Additional React Query mutation options
 *
 * @example
 * ```tsx
 * const publishNewsletter = usePublishNewsletter();
 *
 * await publishNewsletter.mutateAsync('newsletter-123');
 * ```
 */
export function usePublishNewsletter() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => newslettersRepository.publishNewsletter(id),
    onSuccess: (_data, id) => {
      // Invalidate affected queries
      queryClient.invalidateQueries({ queryKey: newsletterKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: newsletterKeys.myNewsletters() });
    },
  });
}

/**
 * useSendNewsletter Hook
 *
 * Mutation hook for queuing newsletter email sending via Hangfire background job
 * Only Active newsletters (not already sent) can be sent
 *
 * Features:
 * - Queues background job (202 Accepted)
 * - Automatic cache invalidation
 * - Proper error handling
 *
 * @param options - Additional React Query mutation options
 *
 * @example
 * ```tsx
 * const sendNewsletter = useSendNewsletter();
 *
 * await sendNewsletter.mutateAsync('newsletter-123');
 * // Newsletter queued for sending
 * ```
 */
export function useSendNewsletter() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => newslettersRepository.sendNewsletter(id),
    onSuccess: (_data, id) => {
      // Invalidate affected queries
      queryClient.invalidateQueries({ queryKey: newsletterKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: newsletterKeys.myNewsletters() });
    },
  });
}

/**
 * useUnpublishNewsletter Hook
 * Phase 6A.74 Part 9A: Unpublish newsletters
 *
 * Mutation hook for unpublishing newsletters (Active → Draft)
 * Reverts newsletter to draft status
 * Only Active newsletters (not sent) can be unpublished
 *
 * Features:
 * - Changes status from Active to Draft
 * - Clears PublishedAt and ExpiresAt
 * - Automatic cache invalidation
 * - Proper error handling
 *
 * @param options - Additional React Query mutation options
 *
 * @example
 * ```tsx
 * const unpublishNewsletter = useUnpublishNewsletter();
 *
 * await unpublishNewsletter.mutateAsync('newsletter-123');
 * // Newsletter reverted to Draft status
 * ```
 */
export function useUnpublishNewsletter() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => newslettersRepository.unpublishNewsletter(id),
    onSuccess: (_data, id) => {
      // Invalidate affected queries
      queryClient.invalidateQueries({ queryKey: newsletterKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: newsletterKeys.myNewsletters() });
      queryClient.invalidateQueries({ queryKey: newsletterKeys.published() });
    },
  });
}

/**
 * useReactivateNewsletter Hook
 * Phase 6A.74 Hotfix: Reactivate inactive newsletters
 *
 * Mutation hook for reactivating inactive newsletters (Inactive → Active)
 * Extends newsletter visibility by 7 days
 * Only Inactive newsletters (not sent) can be reactivated
 *
 * Features:
 * - Changes status from Inactive to Active
 * - Extends ExpiresAt by 7 days
 * - Automatic cache invalidation
 * - Proper error handling
 *
 * @param options - Additional React Query mutation options
 *
 * @example
 * ```tsx
 * const reactivateNewsletter = useReactivateNewsletter();
 *
 * await reactivateNewsletter.mutateAsync('newsletter-123');
 * // Newsletter reactivated and visible for another week
 * ```
 */
export function useReactivateNewsletter() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => newslettersRepository.reactivateNewsletter(id),
    onSuccess: (_data, id) => {
      // Invalidate affected queries
      queryClient.invalidateQueries({ queryKey: newsletterKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: newsletterKeys.myNewsletters() });
      queryClient.invalidateQueries({ queryKey: newsletterKeys.published() });
    },
  });
}

/**
 * Export all hooks
 */
export default {
  useMyNewsletters,
  useNewsletterById,
  useNewslettersByEvent,
  usePublishedNewsletters,
  usePublishedNewslettersWithFilters,
  useRecipientPreview,
  useCreateNewsletter,
  useUpdateNewsletter,
  useDeleteNewsletter,
  usePublishNewsletter,
  useUnpublishNewsletter,
  useSendNewsletter,
  useReactivateNewsletter,
};
