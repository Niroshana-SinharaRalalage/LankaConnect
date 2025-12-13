/**
 * Badges React Query Hooks
 * Phase 6A.25: Badge Management System
 * Phase 6A.27: Added forManagement and forAssignment parameters
 *
 * Provides React Query hooks for Badges API integration
 * Implements caching, optimistic updates, and proper error handling
 *
 * @requires @tanstack/react-query
 * @requires badgesRepository from infrastructure/repositories/badges.repository
 * @requires Badge types from infrastructure/api/types/badges.types
 */

import {
  useQuery,
  useMutation,
  useQueryClient,
  UseQueryOptions,
} from '@tanstack/react-query';

import { badgesRepository } from '@/infrastructure/api/repositories/badges.repository';
import type {
  BadgeDto,
  UpdateBadgeDto,
  EventBadgeDto,
  BadgePosition,
} from '@/infrastructure/api/types/badges.types';
import { ApiError } from '@/infrastructure/api/client/api-errors';

/**
 * Query Keys for Badges
 * Centralized query key management for cache invalidation
 * Phase 6A.27: Updated list key to include forManagement and forAssignment
 */
export const badgeKeys = {
  all: ['badges'] as const,
  lists: () => [...badgeKeys.all, 'list'] as const,
  list: (activeOnly: boolean, forManagement: boolean = false, forAssignment: boolean = false) =>
    [...badgeKeys.lists(), { activeOnly, forManagement, forAssignment }] as const,
  details: () => [...badgeKeys.all, 'detail'] as const,
  detail: (id: string) => [...badgeKeys.details(), id] as const,
  eventBadges: () => [...badgeKeys.all, 'event'] as const,
  eventBadge: (eventId: string) => [...badgeKeys.eventBadges(), eventId] as const,
};

/**
 * useBadges Hook
 *
 * Fetches all badges with optional filters
 * Phase 6A.27: Added forManagement and forAssignment parameters
 *
 * @param activeOnly - If true, returns only active badges (default). If false, returns all.
 * @param forManagement - If true, filters for Badge Management UI (Admin: all, EventOrganizer: own custom)
 * @param forAssignment - If true, filters for Badge Assignment UI (excludes expired badges)
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: badges, isLoading } = useBadges();
 * const { data: allBadges } = useBadges(false); // Include inactive
 * const { data: managementBadges } = useBadges(false, true, false); // For badge management
 * const { data: assignmentBadges } = useBadges(true, false, true); // For badge assignment
 * ```
 */
export function useBadges(
  activeOnly: boolean = true,
  forManagement: boolean = false,
  forAssignment: boolean = false,
  options?: Omit<UseQueryOptions<BadgeDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: badgeKeys.list(activeOnly, forManagement, forAssignment),
    queryFn: () => badgesRepository.getBadges(activeOnly, forManagement, forAssignment),
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: false,
    ...options,
  });
}

/**
 * useBadge Hook
 *
 * Fetches a single badge by ID
 *
 * @param badgeId - Badge ID to fetch
 * @param options - Additional React Query options
 */
export function useBadge(
  badgeId: string,
  options?: Omit<UseQueryOptions<BadgeDto, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: badgeKeys.detail(badgeId),
    queryFn: () => badgesRepository.getBadgeById(badgeId),
    enabled: !!badgeId,
    staleTime: 5 * 60 * 1000,
    ...options,
  });
}

/**
 * useEventBadges Hook
 *
 * Fetches badges assigned to a specific event
 *
 * @param eventId - Event ID to fetch badges for
 * @param options - Additional React Query options
 */
export function useEventBadges(
  eventId: string,
  options?: Omit<UseQueryOptions<EventBadgeDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: badgeKeys.eventBadge(eventId),
    queryFn: () => badgesRepository.getEventBadges(eventId),
    enabled: !!eventId,
    staleTime: 2 * 60 * 1000, // 2 minutes
    ...options,
  });
}

/**
 * useCreateBadge Hook
 *
 * Mutation hook for creating a new badge
 * Phase 6A.27: Added optional expiresAt parameter
 * Phase 6A.28: Changed to defaultDurationDays (duration-based expiration)
 *
 * @example
 * ```tsx
 * const createBadge = useCreateBadge();
 *
 * await createBadge.mutateAsync({
 *   name: 'New Event',
 *   position: BadgePosition.TopRight,
 *   imageFile: file,
 *   defaultDurationDays: 30 // optional, null = never expire
 * });
 * ```
 */
export function useCreateBadge() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      name,
      position,
      imageFile,
      defaultDurationDays,
    }: {
      name: string;
      position: BadgePosition;
      imageFile: File;
      defaultDurationDays?: number | null;
    }) => badgesRepository.createBadge(name, position, imageFile, defaultDurationDays),
    onSuccess: () => {
      // Invalidate badge lists to refetch
      queryClient.invalidateQueries({ queryKey: badgeKeys.lists() });
    },
  });
}

/**
 * useUpdateBadge Hook
 *
 * Mutation hook for updating a badge's details
 */
export function useUpdateBadge() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      badgeId,
      dto,
    }: {
      badgeId: string;
      dto: UpdateBadgeDto;
    }) => badgesRepository.updateBadge(badgeId, dto),
    onSuccess: (data, variables) => {
      // Update the specific badge in cache
      queryClient.setQueryData(badgeKeys.detail(variables.badgeId), data);
      // Invalidate lists
      queryClient.invalidateQueries({ queryKey: badgeKeys.lists() });
    },
  });
}

/**
 * useUpdateBadgeImage Hook
 *
 * Mutation hook for updating a badge's image
 */
export function useUpdateBadgeImage() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      badgeId,
      imageFile,
    }: {
      badgeId: string;
      imageFile: File;
    }) => badgesRepository.updateBadgeImage(badgeId, imageFile),
    onSuccess: (data, variables) => {
      // Update the specific badge in cache
      queryClient.setQueryData(badgeKeys.detail(variables.badgeId), data);
      // Invalidate lists
      queryClient.invalidateQueries({ queryKey: badgeKeys.lists() });
      // Invalidate event badges to get updated image URLs
      queryClient.invalidateQueries({ queryKey: badgeKeys.eventBadges() });
    },
  });
}

/**
 * useDeleteBadge Hook
 *
 * Mutation hook for deleting a badge
 * Note: System badges are only deactivated, not deleted
 */
export function useDeleteBadge() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (badgeId: string) => badgesRepository.deleteBadge(badgeId),
    onSuccess: () => {
      // Invalidate all badge queries
      queryClient.invalidateQueries({ queryKey: badgeKeys.all });
    },
  });
}

/**
 * useAssignBadgeToEvent Hook
 *
 * Mutation hook for assigning a badge to an event
 * Phase 6A.28: Added optional durationDays for assignment-specific duration override
 */
export function useAssignBadgeToEvent() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      badgeId,
      durationDays,
    }: {
      eventId: string;
      badgeId: string;
      durationDays?: number | null;
    }) => badgesRepository.assignBadgeToEvent(eventId, badgeId, durationDays),
    onSuccess: (data, variables) => {
      // Invalidate event badges
      queryClient.invalidateQueries({
        queryKey: badgeKeys.eventBadge(variables.eventId),
      });
    },
  });
}

/**
 * useRemoveBadgeFromEvent Hook
 *
 * Mutation hook for removing a badge from an event
 */
export function useRemoveBadgeFromEvent() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      badgeId,
    }: {
      eventId: string;
      badgeId: string;
    }) => badgesRepository.removeBadgeFromEvent(eventId, badgeId),
    onMutate: async ({ eventId, badgeId }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({
        queryKey: badgeKeys.eventBadge(eventId),
      });

      // Snapshot for rollback
      const previousBadges = queryClient.getQueryData<EventBadgeDto[]>(
        badgeKeys.eventBadge(eventId)
      );

      // Optimistically remove the badge
      queryClient.setQueryData<EventBadgeDto[]>(
        badgeKeys.eventBadge(eventId),
        (old) => old?.filter((eb) => eb.badgeId !== badgeId) || []
      );

      return { previousBadges };
    },
    onError: (_err, variables, context) => {
      // Rollback on error
      if (context?.previousBadges) {
        queryClient.setQueryData(
          badgeKeys.eventBadge(variables.eventId),
          context.previousBadges
        );
      }
    },
    onSuccess: (_, variables) => {
      // Invalidate to ensure consistency
      queryClient.invalidateQueries({
        queryKey: badgeKeys.eventBadge(variables.eventId),
      });
    },
  });
}

/**
 * useInvalidateBadges Hook
 *
 * Utility hook to manually invalidate badge queries
 */
export function useInvalidateBadges() {
  const queryClient = useQueryClient();

  return {
    all: () => queryClient.invalidateQueries({ queryKey: badgeKeys.all }),
    lists: () => queryClient.invalidateQueries({ queryKey: badgeKeys.lists() }),
    detail: (id: string) =>
      queryClient.invalidateQueries({ queryKey: badgeKeys.detail(id) }),
    eventBadges: (eventId: string) =>
      queryClient.invalidateQueries({ queryKey: badgeKeys.eventBadge(eventId) }),
  };
}

/**
 * Export all hooks
 */
export default {
  useBadges,
  useBadge,
  useEventBadges,
  useCreateBadge,
  useUpdateBadge,
  useUpdateBadgeImage,
  useDeleteBadge,
  useAssignBadgeToEvent,
  useRemoveBadgeFromEvent,
  useInvalidateBadges,
};
