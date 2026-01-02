import { apiClient } from '../client/api-client';

/**
 * Phase 6A.69: DTO for community statistics from backend
 * Matches CommunityStatsDto from backend
 */
export interface CommunityStatsDto {
  totalUsers: number;
  totalEvents: number;
  totalBusinesses: number;
}

/**
 * Phase 6A.69: Repository for public statistics endpoints
 * Used for landing page hero numbers
 */
class StatsRepository {
  private readonly basePath = '/public';

  /**
   * Get real-time community statistics for landing page
   * Public endpoint - no authentication required
   * Backend caches for 5 minutes
   */
  async getCommunityStats(): Promise<CommunityStatsDto> {
    return await apiClient.get<CommunityStatsDto>(`${this.basePath}/stats`);
  }
}

export const statsRepository = new StatsRepository();
