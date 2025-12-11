import { useMutation, useQueryClient } from '@tanstack/react-query';
import { roleUpgradeRepository } from '@/infrastructure/api/repositories/role-upgrade.repository';
import type { RequestRoleUpgradeRequest } from '@/infrastructure/api/types/role-upgrade.types';

/**
 * Phase 6A.7: User Upgrade Workflow - React Query Hooks
 * Custom hooks for role upgrade operations with optimistic updates
 */

/**
 * Hook to request a role upgrade
 * Invalidates user query on success to reflect pending status
 */
export function useRequestRoleUpgrade() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: RequestRoleUpgradeRequest) =>
      roleUpgradeRepository.requestUpgrade(request),
    onSuccess: () => {
      // Invalidate user query to refresh user data with pending upgrade status
      queryClient.invalidateQueries({ queryKey: ['user'] });
    },
  });
}

/**
 * Hook to cancel a pending role upgrade request
 * Invalidates user query on success to remove pending status
 */
export function useCancelRoleUpgrade() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => roleUpgradeRepository.cancelUpgrade(),
    onSuccess: () => {
      // Invalidate user query to refresh user data
      queryClient.invalidateQueries({ queryKey: ['user'] });
    },
  });
}
