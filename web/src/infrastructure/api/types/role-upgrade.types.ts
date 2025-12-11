/**
 * Phase 6A.7: User Upgrade Workflow - API Types
 */

export type UserRole = 'GeneralUser' | 'EventOrganizer' | 'Admin';

export interface RequestRoleUpgradeRequest {
  targetRole: UserRole;
  reason: string;
}

export interface RoleUpgradeResponse {
  success: boolean;
  message?: string;
}
