/**
 * Phase 6A.90: Admin User Management Hooks
 * React Query hooks for admin user management
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { adminUsersRepository } from '@/infrastructure/api/repositories/admin-users.repository';
import type {
  GetAdminUsersRequest,
  LockUserRequest,
  AdminUserDto,
  PagedResultDto,
} from '@/infrastructure/api/types/admin-users.types';

// Query key factory for cache management
export const adminUserKeys = {
  all: ['adminUsers'] as const,
  lists: () => [...adminUserKeys.all, 'list'] as const,
  list: (filters: GetAdminUsersRequest) => [...adminUserKeys.lists(), filters] as const,
  details: () => [...adminUserKeys.all, 'detail'] as const,
  detail: (id: string) => [...adminUserKeys.details(), id] as const,
  statistics: () => [...adminUserKeys.all, 'statistics'] as const,
};

/**
 * Hook to fetch paginated users with filters
 */
export function useAdminUsers(filters?: GetAdminUsersRequest) {
  return useQuery({
    queryKey: adminUserKeys.list(filters || {}),
    queryFn: () => adminUsersRepository.getUsers(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: false,
  });
}

/**
 * Hook to fetch a single user's details
 */
export function useAdminUserDetails(userId: string) {
  return useQuery({
    queryKey: adminUserKeys.detail(userId),
    queryFn: () => adminUsersRepository.getUserById(userId),
    enabled: !!userId,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

/**
 * Hook to fetch user statistics
 */
export function useAdminUserStatistics() {
  return useQuery({
    queryKey: adminUserKeys.statistics(),
    queryFn: () => adminUsersRepository.getStatistics(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

/**
 * Hook to deactivate a user
 */
export function useDeactivateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (userId: string) => adminUsersRepository.deactivateUser(userId),
    onSuccess: () => {
      // Invalidate user lists and statistics
      queryClient.invalidateQueries({ queryKey: adminUserKeys.lists() });
      queryClient.invalidateQueries({ queryKey: adminUserKeys.statistics() });
    },
  });
}

/**
 * Hook to activate a user
 */
export function useActivateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (userId: string) => adminUsersRepository.activateUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminUserKeys.lists() });
      queryClient.invalidateQueries({ queryKey: adminUserKeys.statistics() });
    },
  });
}

/**
 * Hook to lock a user account
 */
export function useLockUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ userId, request }: { userId: string; request: LockUserRequest }) =>
      adminUsersRepository.lockUser(userId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminUserKeys.lists() });
      queryClient.invalidateQueries({ queryKey: adminUserKeys.statistics() });
    },
  });
}

/**
 * Hook to unlock a user account
 */
export function useUnlockUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (userId: string) => adminUsersRepository.unlockUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminUserKeys.lists() });
      queryClient.invalidateQueries({ queryKey: adminUserKeys.statistics() });
    },
  });
}

/**
 * Hook to resend email verification
 */
export function useResendVerification() {
  return useMutation({
    mutationFn: (userId: string) => adminUsersRepository.resendVerification(userId),
  });
}

/**
 * Hook to force password reset
 */
export function useForcePasswordReset() {
  return useMutation({
    mutationFn: (userId: string) => adminUsersRepository.forcePasswordReset(userId),
  });
}
