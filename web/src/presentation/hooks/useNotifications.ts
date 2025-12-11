/**
 * Notifications React Query Hooks
 * Phase 6A.6: Notification System
 *
 * Provides React Query hooks for Notifications API integration
 * Implements caching, optimistic updates, and proper error handling
 *
 * @requires @tanstack/react-query
 * @requires notificationsRepository from infrastructure/repositories/notifications.repository
 * @requires Notification types from infrastructure/api/types/notifications.types
 */

import {
  useQuery,
  useMutation,
  useQueryClient,
  UseQueryOptions,
} from '@tanstack/react-query';

import { notificationsRepository } from '@/infrastructure/api/repositories/notifications.repository';
import type { NotificationDto } from '@/infrastructure/api/types/notifications.types';
import { ApiError } from '@/infrastructure/api/client/api-errors';

/**
 * Query Keys for Notifications
 * Centralized query key management for cache invalidation
 */
export const notificationKeys = {
  all: ['notifications'] as const,
  unread: () => [...notificationKeys.all, 'unread'] as const,
};

/**
 * useUnreadNotifications Hook
 *
 * Fetches unread notifications for the current user
 *
 * Features:
 * - Automatic caching with 1-minute stale time
 * - Refetch on window focus for real-time updates
 * - Proper error handling with ApiError types
 * - Auto-refetch every 30 seconds for notification updates
 *
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: notifications, isLoading, error } = useUnreadNotifications();
 * ```
 */
export function useUnreadNotifications(
  options?: Omit<UseQueryOptions<NotificationDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: notificationKeys.unread(),
    queryFn: async () => {
      const result = await notificationsRepository.getUnreadNotifications();
      return result;
    },
    staleTime: 1 * 60 * 1000, // 1 minute
    refetchInterval: 30 * 1000, // Refetch every 30 seconds
    refetchOnWindowFocus: true,
    retry: 1, // Only retry once
    ...options,
  });
}

/**
 * useMarkNotificationAsRead Hook
 *
 * Mutation hook for marking a notification as read
 *
 * Features:
 * - Optimistic updates
 * - Automatic cache invalidation after success
 * - Proper error handling
 * - Rollback on error
 *
 * @example
 * ```tsx
 * const markAsRead = useMarkNotificationAsRead();
 *
 * await markAsRead.mutateAsync(notificationId);
 * ```
 */
export function useMarkNotificationAsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (notificationId: string) =>
      notificationsRepository.markAsRead(notificationId),
    onMutate: async (notificationId) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: notificationKeys.unread() });

      // Snapshot previous value for rollback
      const previousNotifications = queryClient.getQueryData<NotificationDto[]>(
        notificationKeys.unread()
      );

      // Optimistically remove the notification from unread list
      queryClient.setQueryData<NotificationDto[]>(
        notificationKeys.unread(),
        (old) => old?.filter((n) => n.id !== notificationId) || []
      );

      return { previousNotifications };
    },
    onError: (_err, _variables, context) => {
      // Rollback on error
      if (context?.previousNotifications) {
        queryClient.setQueryData(
          notificationKeys.unread(),
          context.previousNotifications
        );
      }
    },
    onSuccess: () => {
      // Invalidate to ensure consistency with server
      queryClient.invalidateQueries({ queryKey: notificationKeys.unread() });
    },
  });
}

/**
 * useMarkAllNotificationsAsRead Hook
 *
 * Mutation hook for marking all notifications as read
 *
 * Features:
 * - Optimistic updates (clears all notifications)
 * - Automatic cache invalidation
 * - Rollback on error
 *
 * @example
 * ```tsx
 * const markAllAsRead = useMarkAllNotificationsAsRead();
 *
 * await markAllAsRead.mutateAsync();
 * ```
 */
export function useMarkAllNotificationsAsRead() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => notificationsRepository.markAllAsRead(),
    onMutate: async () => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: notificationKeys.unread() });

      // Snapshot previous value for rollback
      const previousNotifications = queryClient.getQueryData<NotificationDto[]>(
        notificationKeys.unread()
      );

      // Optimistically clear all notifications
      queryClient.setQueryData<NotificationDto[]>(notificationKeys.unread(), []);

      return { previousNotifications };
    },
    onError: (_err, _variables, context) => {
      // Rollback on error
      if (context?.previousNotifications) {
        queryClient.setQueryData(
          notificationKeys.unread(),
          context.previousNotifications
        );
      }
    },
    onSuccess: () => {
      // Invalidate to ensure consistency with server
      queryClient.invalidateQueries({ queryKey: notificationKeys.unread() });
    },
  });
}

/**
 * useInvalidateNotifications Hook
 *
 * Utility hook to manually invalidate notification queries
 * Useful for force refresh scenarios
 *
 * @example
 * ```tsx
 * const invalidateNotifications = useInvalidateNotifications();
 *
 * <button onClick={() => invalidateNotifications.all()}>
 *   Refresh Notifications
 * </button>
 * ```
 */
export function useInvalidateNotifications() {
  const queryClient = useQueryClient();

  return {
    all: () => queryClient.invalidateQueries({ queryKey: notificationKeys.all }),
    unread: () => queryClient.invalidateQueries({ queryKey: notificationKeys.unread() }),
  };
}

/**
 * Export all hooks
 */
export default {
  useUnreadNotifications,
  useMarkNotificationAsRead,
  useMarkAllNotificationsAsRead,
  useInvalidateNotifications,
};
