/**
 * Email Groups React Query Hooks
 * Phase 6A.25: Email Groups Management
 *
 * Provides React Query hooks for Email Groups API integration
 * Implements caching, optimistic updates, and proper error handling
 *
 * @requires @tanstack/react-query
 * @requires emailGroupsRepository from infrastructure/repositories/email-groups.repository
 * @requires EmailGroup types from infrastructure/api/types/email-groups.types
 */

import {
  useQuery,
  useMutation,
  useQueryClient,
  UseQueryOptions,
} from '@tanstack/react-query';

import { emailGroupsRepository } from '@/infrastructure/api/repositories/email-groups.repository';
import type {
  EmailGroupDto,
  CreateEmailGroupRequest,
  UpdateEmailGroupRequest,
} from '@/infrastructure/api/types/email-groups.types';
import { ApiError } from '@/infrastructure/api/client/api-errors';

/**
 * Query Keys for Email Groups
 * Centralized query key management for cache invalidation
 */
export const emailGroupKeys = {
  all: ['emailGroups'] as const,
  lists: () => [...emailGroupKeys.all, 'list'] as const,
  list: (includeAll: boolean) =>
    [...emailGroupKeys.lists(), { includeAll }] as const,
  details: () => [...emailGroupKeys.all, 'detail'] as const,
  detail: (id: string) => [...emailGroupKeys.details(), id] as const,
};

/**
 * useEmailGroups Hook
 *
 * Fetches email groups for the current user (or all for admins)
 *
 * @param includeAll - If true and user is admin, returns all groups across platform
 * @param options - Additional React Query options
 *
 * @example
 * ```tsx
 * const { data: groups, isLoading } = useEmailGroups();
 * const { data: allGroups } = useEmailGroups(true); // Admin only: include all
 * ```
 */
export function useEmailGroups(
  includeAll: boolean = false,
  options?: Omit<
    UseQueryOptions<EmailGroupDto[], ApiError>,
    'queryKey' | 'queryFn'
  >
) {
  return useQuery({
    queryKey: emailGroupKeys.list(includeAll),
    queryFn: () => emailGroupsRepository.getEmailGroups(includeAll),
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: false,
    ...options,
  });
}

/**
 * useEmailGroup Hook
 *
 * Fetches a single email group by ID
 *
 * @param id - Email group ID to fetch
 * @param options - Additional React Query options
 */
export function useEmailGroup(
  id: string,
  options?: Omit<UseQueryOptions<EmailGroupDto, ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: emailGroupKeys.detail(id),
    queryFn: () => emailGroupsRepository.getEmailGroupById(id),
    enabled: !!id,
    staleTime: 5 * 60 * 1000,
    ...options,
  });
}

/**
 * useCreateEmailGroup Hook
 *
 * Mutation hook for creating a new email group
 *
 * @example
 * ```tsx
 * const createGroup = useCreateEmailGroup();
 *
 * await createGroup.mutateAsync({
 *   name: 'Marketing Team',
 *   description: 'Event marketing contacts',
 *   emailAddresses: 'john@example.com, jane@example.com'
 * });
 * ```
 */
export function useCreateEmailGroup() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateEmailGroupRequest) =>
      emailGroupsRepository.createEmailGroup(request),
    onSuccess: () => {
      // Invalidate email group lists to refetch
      queryClient.invalidateQueries({ queryKey: emailGroupKeys.lists() });
    },
  });
}

/**
 * useUpdateEmailGroup Hook
 *
 * Mutation hook for updating an email group
 *
 * @example
 * ```tsx
 * const updateGroup = useUpdateEmailGroup();
 *
 * await updateGroup.mutateAsync({
 *   id: 'group-id',
 *   request: {
 *     name: 'Updated Name',
 *     emailAddresses: 'updated@example.com'
 *   }
 * });
 * ```
 */
export function useUpdateEmailGroup() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      request,
    }: {
      id: string;
      request: UpdateEmailGroupRequest;
    }) => emailGroupsRepository.updateEmailGroup(id, request),
    onSuccess: (data, variables) => {
      // Update the specific email group in cache
      queryClient.setQueryData(emailGroupKeys.detail(variables.id), data);
      // Invalidate lists
      queryClient.invalidateQueries({ queryKey: emailGroupKeys.lists() });
    },
  });
}

/**
 * useDeleteEmailGroup Hook
 *
 * Mutation hook for deleting an email group (soft delete)
 *
 * @example
 * ```tsx
 * const deleteGroup = useDeleteEmailGroup();
 *
 * await deleteGroup.mutateAsync('group-id');
 * ```
 */
export function useDeleteEmailGroup() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => emailGroupsRepository.deleteEmailGroup(id),
    onMutate: async (id) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: emailGroupKeys.lists() });

      // Snapshot for rollback
      const previousGroups = queryClient.getQueriesData<EmailGroupDto[]>({
        queryKey: emailGroupKeys.lists(),
      });

      // Optimistically remove the group from all list caches
      queryClient.setQueriesData<EmailGroupDto[]>(
        { queryKey: emailGroupKeys.lists() },
        (old) => old?.filter((g) => g.id !== id) || []
      );

      return { previousGroups };
    },
    onError: (_err, _id, context) => {
      // Rollback on error
      if (context?.previousGroups) {
        context.previousGroups.forEach(([queryKey, data]) => {
          queryClient.setQueryData(queryKey, data);
        });
      }
    },
    onSuccess: () => {
      // Invalidate all email group queries
      queryClient.invalidateQueries({ queryKey: emailGroupKeys.all });
    },
  });
}

/**
 * useInvalidateEmailGroups Hook
 *
 * Utility hook to manually invalidate email group queries
 */
export function useInvalidateEmailGroups() {
  const queryClient = useQueryClient();

  return {
    all: () =>
      queryClient.invalidateQueries({ queryKey: emailGroupKeys.all }),
    lists: () =>
      queryClient.invalidateQueries({ queryKey: emailGroupKeys.lists() }),
    detail: (id: string) =>
      queryClient.invalidateQueries({ queryKey: emailGroupKeys.detail(id) }),
  };
}

/**
 * Export all hooks
 */
export default {
  useEmailGroups,
  useEmailGroup,
  useCreateEmailGroup,
  useUpdateEmailGroup,
  useDeleteEmailGroup,
  useInvalidateEmailGroups,
};
