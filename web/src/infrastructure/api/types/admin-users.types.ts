/**
 * Phase 6A.90: Admin User Management Types
 * DTOs for admin user management endpoints
 */

export interface AdminUserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  role: string;
  identityProvider: string;
  isActive: boolean;
  isEmailVerified: boolean;
  accountLockedUntil: string | null;
  lastLoginAt: string | null;
  createdAt: string;
  phone: string | null;
  location: UserLocationDto | null;
}

export interface UserLocationDto {
  metroAreaId: string | null;
  metroAreaName: string | null;
  state: string | null;
  city: string | null;
}

export interface AdminUserDetailsDto extends AdminUserDto {
  updatedAt: string | null;
}

export interface AdminUserStatisticsDto {
  totalUsers: number;
  activeUsers: number;
  lockedAccounts: number;
  countsByRole: Record<string, number>;
}

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface GetAdminUsersRequest {
  page?: number;
  pageSize?: number;
  searchTerm?: string;
  roleFilter?: string;
  isActiveFilter?: boolean | null;
}

export interface LockUserRequest {
  lockUntil: string;
  reason?: string;
}

export type UserRole =
  | 'Member'
  | 'EventOrganizer'
  | 'Admin'
  | 'AdminManager';

export const USER_ROLES: Record<UserRole, string> = {
  Member: 'Member',
  EventOrganizer: 'Event Organizer',
  Admin: 'Admin',
  AdminManager: 'Admin Manager',
};

export const ROLE_BADGE_COLORS: Record<string, { bg: string; text: string }> = {
  Member: { bg: '#E5E7EB', text: '#374151' },
  EventOrganizer: { bg: '#DBEAFE', text: '#1D4ED8' },
  Admin: { bg: '#FEE2E2', text: '#991B1B' },
  AdminManager: { bg: '#8B1538', text: '#FFFFFF' },
};

export const LOCK_DURATION_OPTIONS = [
  { label: '1 Hour', value: 1 },
  { label: '24 Hours', value: 24 },
  { label: '7 Days', value: 168 },
  { label: '30 Days', value: 720 },
  { label: 'Custom', value: -1 },
];
