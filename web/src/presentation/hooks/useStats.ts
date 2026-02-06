import { useQuery } from '@tanstack/react-query';
import { statsRepository } from '@/infrastructure/api/repositories/stats.repository';

/**
 * Phase 6A.69: Hook for fetching real-time community statistics
 * Used on landing page hero section to display member/event/business counts
 * Caches for 5 minutes (matches backend cache duration)
 */
export function useCommunityStats() {
  return useQuery({
    queryKey: ['community-stats'],
    queryFn: () => statsRepository.getCommunityStats(),
    staleTime: 5 * 60 * 1000, // 5 minutes - matches backend cache
    gcTime: 10 * 60 * 1000, // 10 minutes garbage collection
    refetchOnWindowFocus: false, // Don't refetch on window focus for stats
  });
}
