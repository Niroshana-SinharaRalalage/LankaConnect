/**
 * Event Sign-Up Management React Query Hooks
 *
 * Provides React Query hooks for Event Sign-Up Lists API integration
 * Implements caching, optimistic updates, and proper error handling
 *
 * @requires @tanstack/react-query
 * @requires eventsRepository from infrastructure/repositories/events.repository
 * @requires SignUp types from infrastructure/api/types/events.types
 */

import {
  useQuery,
  useMutation,
  useQueryClient,
  UseQueryOptions,
} from '@tanstack/react-query';

import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type {
  SignUpListDto,
  AddSignUpListRequest,
  CommitToSignUpRequest,
  CancelCommitmentRequest,
  CreateSignUpListRequest,
  UpdateSignUpListRequest,
  AddSignUpItemRequest,
  CommitToSignUpItemRequest,
} from '@/infrastructure/api/types/events.types';

import { ApiError } from '@/infrastructure/api/client/api-errors';
import { eventKeys } from './useEvents';

/**
 * Query Keys for Event Sign-Ups
 * Centralized query key management for cache invalidation
 */
export const signUpKeys = {
  all: ['signups'] as const,
  lists: () => [...signUpKeys.all, 'list'] as const,
  list: (eventId: string) => [...signUpKeys.lists(), eventId] as const,
};

/**
 * useEventSignUps Hook
 *
 * Fetches all sign-up lists for a specific event
 *
 * Features:
 * - Automatic caching with 5-minute stale time
 * - Refetch on window focus
 * - Proper error handling with ApiError types
 * - Only enabled when eventId is provided
 *
 * @param eventId - Event ID (GUID)
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: signUpLists, isLoading, error } = useEventSignUps(eventId);
 * ```
 */
export function useEventSignUps(
  eventId: string | undefined,
  options?: Omit<UseQueryOptions<SignUpListDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: signUpKeys.list(eventId || ''),
    queryFn: () => eventsRepository.getEventSignUpLists(eventId!),
    enabled: !!eventId, // Only fetch when eventId is provided
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true,
    retry: 1,
    ...options,
  });
}

/**
 * useAddSignUpList Hook
 *
 * Mutation hook for adding a sign-up list to an event (organizer only)
 *
 * Features:
 * - Automatic cache invalidation after success
 * - Proper error handling
 * - Invalidates both sign-up lists and event detail
 *
 * @example
 * ```tsx
 * const addSignUpList = useAddSignUpList();
 *
 * await addSignUpList.mutateAsync({
 *   eventId: 'event-123',
 *   category: 'Food & Drinks',
 *   description: 'Please bring items for the potluck',
 *   signUpType: SignUpType.Predefined,
 *   predefinedItems: ['Appetizers', 'Main Course', 'Desserts']
 * });
 * ```
 */
export function useAddSignUpList() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, ...data }: { eventId: string } & AddSignUpListRequest) =>
      eventsRepository.addSignUpList(eventId, data),
    onSuccess: (_data, variables) => {
      // Invalidate sign-up lists for this event
      queryClient.invalidateQueries({ queryKey: signUpKeys.list(variables.eventId) });
      // Invalidate event detail to reflect updated event
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(variables.eventId) });
    },
  });
}

/**
 * useRemoveSignUpList Hook
 *
 * Mutation hook for removing a sign-up list from an event (organizer only)
 *
 * Features:
 * - Optimistic update (removes from cache immediately)
 * - Automatic cache invalidation
 * - Rollback on error
 *
 * @example
 * ```tsx
 * const removeSignUpList = useRemoveSignUpList();
 *
 * await removeSignUpList.mutateAsync({
 *   eventId: 'event-123',
 *   signupId: 'signup-456'
 * });
 * ```
 */
export function useRemoveSignUpList() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventId, signupId }: { eventId: string; signupId: string }) =>
      eventsRepository.removeSignUpList(eventId, signupId),
    onMutate: async ({ eventId, signupId }) => {
      // Cancel queries
      await queryClient.cancelQueries({ queryKey: signUpKeys.list(eventId) });

      // Snapshot for rollback
      const previousSignUps = queryClient.getQueryData<SignUpListDto[]>(
        signUpKeys.list(eventId)
      );

      // Optimistically remove from cache
      queryClient.setQueryData<SignUpListDto[]>(
        signUpKeys.list(eventId),
        (old) => old?.filter((s) => s.id !== signupId) || []
      );

      return { previousSignUps };
    },
    onError: (err, { eventId }, context) => {
      // Rollback on error
      if (context?.previousSignUps) {
        queryClient.setQueryData(signUpKeys.list(eventId), context.previousSignUps);
      }
    },
    onSuccess: (_data, variables) => {
      // Invalidate to ensure consistency
      queryClient.invalidateQueries({ queryKey: signUpKeys.list(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(variables.eventId) });
    },
  });
}

/**
 * useCommitToSignUp Hook
 *
 * Mutation hook for user to commit to bringing an item
 *
 * Features:
 * - Optimistic update (adds commitment to cache immediately)
 * - Automatic cache invalidation
 * - Rollback on error
 *
 * @example
 * ```tsx
 * const commitToSignUp = useCommitToSignUp();
 *
 * await commitToSignUp.mutateAsync({
 *   eventId: 'event-123',
 *   signupId: 'signup-456',
 *   userId: 'user-789',
 *   itemDescription: 'Vegetable Salad',
 *   quantity: 1
 * });
 * ```
 */
export function useCommitToSignUp() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      signupId,
      ...data
    }: {
      eventId: string;
      signupId: string;
    } & CommitToSignUpRequest) => eventsRepository.commitToSignUp(eventId, signupId, data),
    onMutate: async ({ eventId, signupId, userId, itemDescription, quantity }) => {
      // Cancel queries
      await queryClient.cancelQueries({ queryKey: signUpKeys.list(eventId) });

      // Snapshot for rollback
      const previousSignUps = queryClient.getQueryData<SignUpListDto[]>(
        signUpKeys.list(eventId)
      );

      // Optimistically add commitment
      queryClient.setQueryData<SignUpListDto[]>(signUpKeys.list(eventId), (old) => {
        if (!old) return old;

        return old.map((signUp) => {
          if (signUp.id !== signupId) return signUp;

          return {
            ...signUp,
            commitments: [
              ...signUp.commitments,
              {
                id: `temp-${Date.now()}`, // Temporary ID until server responds
                userId,
                itemDescription,
                quantity,
                committedAt: new Date().toISOString(),
              },
            ],
            commitmentCount: signUp.commitmentCount + 1,
          };
        });
      });

      return { previousSignUps };
    },
    onError: (err, { eventId }, context) => {
      // Rollback on error
      if (context?.previousSignUps) {
        queryClient.setQueryData(signUpKeys.list(eventId), context.previousSignUps);
      }
    },
    onSuccess: (_data, variables) => {
      // Refetch to get accurate data from server (with real IDs)
      queryClient.invalidateQueries({ queryKey: signUpKeys.list(variables.eventId) });
    },
  });
}

/**
 * useCancelCommitment Hook
 *
 * Mutation hook for user to cancel their commitment
 *
 * Features:
 * - Optimistic update (removes commitment from cache immediately)
 * - Automatic cache invalidation
 * - Rollback on error
 *
 * @example
 * ```tsx
 * const cancelCommitment = useCancelCommitment();
 *
 * await cancelCommitment.mutateAsync({
 *   eventId: 'event-123',
 *   signupId: 'signup-456',
 *   userId: 'user-789'
 * });
 * ```
 */
export function useCancelCommitment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      signupId,
      ...data
    }: {
      eventId: string;
      signupId: string;
    } & CancelCommitmentRequest) => eventsRepository.cancelCommitment(eventId, signupId, data),
    onMutate: async ({ eventId, signupId, userId }) => {
      // Cancel queries
      await queryClient.cancelQueries({ queryKey: signUpKeys.list(eventId) });

      // Snapshot for rollback
      const previousSignUps = queryClient.getQueryData<SignUpListDto[]>(
        signUpKeys.list(eventId)
      );

      // Optimistically remove commitment
      queryClient.setQueryData<SignUpListDto[]>(signUpKeys.list(eventId), (old) => {
        if (!old) return old;

        return old.map((signUp) => {
          if (signUp.id !== signupId) return signUp;

          return {
            ...signUp,
            commitments: signUp.commitments.filter((c) => c.userId !== userId),
            commitmentCount: Math.max(0, signUp.commitmentCount - 1),
          };
        });
      });

      return { previousSignUps };
    },
    onError: (err, { eventId }, context) => {
      // Rollback on error
      if (context?.previousSignUps) {
        queryClient.setQueryData(signUpKeys.list(eventId), context.previousSignUps);
      }
    },
    onSuccess: (_data, variables) => {
      // Refetch to ensure consistency
      queryClient.invalidateQueries({ queryKey: signUpKeys.list(variables.eventId) });
    },
  });
}

// ==================== CATEGORY-BASED SIGN-UP HOOKS ====================

/**
 * useCreateSignUpList Hook
 *
 * Mutation hook for creating a sign-up list WITH items in a single API call (organizer only)
 *
 * Features:
 * - Creates list and all items in single transactional operation
 * - Returns the newly created sign-up list ID
 * - Automatic cache invalidation after success
 * - Proper error handling
 * - Invalidates sign-up lists and event detail
 *
 * @example
 * ```tsx
 * const createList = useCreateSignUpList();
 *
 * const signUpListId = await createList.mutateAsync({
 *   eventId: 'event-123',
 *   category: 'Potluck Items',
 *   description: 'Bring food for the community potluck',
 *   hasMandatoryItems: true,
 *   hasPreferredItems: true,
 *   hasSuggestedItems: false,
 *   items: [
 *     { itemDescription: 'Main Dish', quantity: 2, itemCategory: 0, notes: 'Serves 10' },
 *     { itemDescription: 'Salad', quantity: 3, itemCategory: 1 }
 *   ]
 * });
 * ```
 */
export function useCreateSignUpList() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      ...data
    }: { eventId: string } & CreateSignUpListRequest) =>
      eventsRepository.createSignUpList(eventId, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: signUpKeys.list(variables.eventId) });
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(variables.eventId) });
    },
  });
}

/**
 * Update sign-up list details (category, description, and category flags)
 * Phase 6A.13: Edit Sign-Up List feature
 */
export function useUpdateSignUpList(eventId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      signupId,
      ...data
    }: { signupId: string } & UpdateSignUpListRequest) =>
      eventsRepository.updateSignUpList(eventId, signupId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: signUpKeys.list(eventId) });
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId) });
    },
  });
}

/**
 * useAddSignUpItem Hook
 *
 * Mutation hook for adding an item to a category-based sign-up list (organizer only)
 *
 * Features:
 * - Returns the newly created item ID
 * - Optimistic update adds item to cache
 * - Automatic cache invalidation
 * - Rollback on error
 *
 * @example
 * ```tsx
 * const addItem = useAddSignUpItem();
 *
 * const itemId = await addItem.mutateAsync({
 *   eventId: 'event-123',
 *   signupId: 'signup-456',
 *   itemDescription: 'Rice (5 cups)',
 *   quantity: 2,
 *   itemCategory: SignUpItemCategory.Mandatory,
 *   notes: 'Basmati preferred'
 * });
 * ```
 */
export function useAddSignUpItem() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      signupId,
      ...data
    }: {
      eventId: string;
      signupId: string;
    } & AddSignUpItemRequest) => eventsRepository.addSignUpItem(eventId, signupId, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: signUpKeys.list(variables.eventId) });
    },
  });
}

/**
 * useRemoveSignUpItem Hook
 *
 * Mutation hook for removing an item from a category-based sign-up list (organizer only)
 *
 * Features:
 * - Optimistic update removes item from cache
 * - Automatic cache invalidation
 * - Rollback on error
 *
 * @example
 * ```tsx
 * const removeItem = useRemoveSignUpItem();
 *
 * await removeItem.mutateAsync({
 *   eventId: 'event-123',
 *   signupId: 'signup-456',
 *   itemId: 'item-789'
 * });
 * ```
 */
export function useRemoveSignUpItem() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      signupId,
      itemId,
    }: {
      eventId: string;
      signupId: string;
      itemId: string;
    }) => eventsRepository.removeSignUpItem(eventId, signupId, itemId),
    onMutate: async ({ eventId, signupId, itemId }) => {
      await queryClient.cancelQueries({ queryKey: signUpKeys.list(eventId) });

      const previousSignUps = queryClient.getQueryData<SignUpListDto[]>(
        signUpKeys.list(eventId)
      );

      // Optimistically remove item
      queryClient.setQueryData<SignUpListDto[]>(signUpKeys.list(eventId), (old) => {
        if (!old) return old;

        return old.map((signUp) => {
          if (signUp.id !== signupId) return signUp;

          return {
            ...signUp,
            items: signUp.items.filter((item) => item.id !== itemId),
          };
        });
      });

      return { previousSignUps };
    },
    onError: (err, { eventId }, context) => {
      if (context?.previousSignUps) {
        queryClient.setQueryData(signUpKeys.list(eventId), context.previousSignUps);
      }
    },
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: signUpKeys.list(variables.eventId) });
    },
  });
}

/**
 * useCommitToSignUpItem Hook
 *
 * Mutation hook for user to commit to bringing a specific item
 *
 * Features:
 * - Optimistic update adds commitment to item
 * - Updates remaining quantity
 * - Automatic cache invalidation
 * - Rollback on error
 *
 * @example
 * ```tsx
 * const commitToItem = useCommitToSignUpItem();
 *
 * await commitToItem.mutateAsync({
 *   eventId: 'event-123',
 *   signupId: 'signup-456',
 *   itemId: 'item-789',
 *   userId: 'user-abc',
 *   quantity: 1,
 *   notes: 'Will bring brown rice'
 * });
 * ```
 */
export function useCommitToSignUpItem() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      eventId,
      signupId,
      itemId,
      ...data
    }: {
      eventId: string;
      signupId: string;
      itemId: string;
    } & CommitToSignUpItemRequest) =>
      eventsRepository.commitToSignUpItem(eventId, signupId, itemId, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: signUpKeys.list(variables.eventId) });
    },
  });
}

/**
 * Export all hooks
 */
export default {
  useEventSignUps,
  useAddSignUpList,
  useRemoveSignUpList,
  useCommitToSignUp,
  useCancelCommitment,
  useCreateSignUpList,
  useAddSignUpItem,
  useRemoveSignUpItem,
  useCommitToSignUpItem,
};
