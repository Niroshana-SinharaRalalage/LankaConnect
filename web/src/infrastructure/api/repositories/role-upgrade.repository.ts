import { apiClient } from '../client/api-client';
import type { RequestRoleUpgradeRequest, RoleUpgradeResponse } from '../types/role-upgrade.types';

/**
 * Phase 6A.7: User Upgrade Workflow - API Repository
 * Repository for role upgrade API calls
 */
export class RoleUpgradeRepository {
  private readonly basePath = '/users/me';

  /**
   * Request a role upgrade to Event Organizer
   */
  async requestUpgrade(request: RequestRoleUpgradeRequest): Promise<RoleUpgradeResponse> {
    await apiClient.post(`${this.basePath}/request-upgrade`, request);
    return {
      success: true,
      message: 'Role upgrade request submitted successfully'
    };
  }

  /**
   * Cancel a pending role upgrade request
   */
  async cancelUpgrade(): Promise<RoleUpgradeResponse> {
    await apiClient.post(`${this.basePath}/cancel-upgrade`, {});
    return {
      success: true,
      message: 'Role upgrade request canceled successfully'
    };
  }
}

// Export singleton instance
export const roleUpgradeRepository = new RoleUpgradeRepository();
