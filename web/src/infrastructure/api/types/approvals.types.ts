/**
 * Approvals API Types
 * Phase 6A.5: Admin Approval Workflow
 */

export interface PendingRoleUpgradeDto {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  currentRole: string;
  requestedRole: string;
  requestedAt: string; // ISO date string
}

export interface RejectRoleUpgradeRequest {
  reason?: string;
}
