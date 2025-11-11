import { apiClient } from '../client/api-client';
import type { PendingRoleUpgradeDto, RejectRoleUpgradeRequest } from '../types/approvals.types';

/**
 * ApprovalsRepository
 * Handles all role upgrade approval-related API calls
 * Phase 6A.5: Admin Approval Workflow
 */
export class ApprovalsRepository {
  private readonly basePath = '/approvals';

  /**
   * Get all pending role upgrade requests (Admin only)
   */
  async getPendingApprovals(): Promise<PendingRoleUpgradeDto[]> {
    const response = await apiClient.get<PendingRoleUpgradeDto[]>(
      `${this.basePath}/pending`
    );
    return response;
  }

  /**
   * Approve a role upgrade request (Admin only)
   */
  async approveRoleUpgrade(userId: string): Promise<void> {
    await apiClient.post(`${this.basePath}/${userId}/approve`);
  }

  /**
   * Reject a role upgrade request with optional reason (Admin only)
   */
  async rejectRoleUpgrade(userId: string, reason?: string): Promise<void> {
    const request: RejectRoleUpgradeRequest = { reason };
    await apiClient.post(`${this.basePath}/${userId}/reject`, request);
  }
}

// Export singleton instance
export const approvalsRepository = new ApprovalsRepository();
