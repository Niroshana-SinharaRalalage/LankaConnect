/**
 * Phase 6A.90: Admin User Management Repository
 * Repository for admin user management API endpoints
 */

import { apiClient } from '../client/api-client';
import type {
  AdminUserDto,
  AdminUserDetailsDto,
  AdminUserStatisticsDto,
  PagedResultDto,
  GetAdminUsersRequest,
  LockUserRequest,
} from '../types/admin-users.types';

export class AdminUsersRepository {
  private readonly basePath = '/admin/users';

  /**
   * Get paginated list of users with optional filters
   */
  async getUsers(request?: GetAdminUsersRequest): Promise<PagedResultDto<AdminUserDto>> {
    const params = new URLSearchParams();

    if (request?.page) params.append('page', request.page.toString());
    if (request?.pageSize) params.append('pageSize', request.pageSize.toString());
    if (request?.searchTerm) params.append('searchTerm', request.searchTerm);
    if (request?.roleFilter) params.append('roleFilter', request.roleFilter);
    if (request?.isActiveFilter !== null && request?.isActiveFilter !== undefined) {
      params.append('isActiveFilter', request.isActiveFilter.toString());
    }

    const queryString = params.toString();
    const url = queryString ? `${this.basePath}?${queryString}` : this.basePath;

    const response = await apiClient.get<PagedResultDto<AdminUserDto>>(url);
    return response;
  }

  /**
   * Get detailed user information by ID
   */
  async getUserById(userId: string): Promise<AdminUserDetailsDto> {
    const response = await apiClient.get<AdminUserDetailsDto>(`${this.basePath}/${userId}`);
    return response;
  }

  /**
   * Get user statistics (counts by role, active users, etc.)
   */
  async getStatistics(): Promise<AdminUserStatisticsDto> {
    const response = await apiClient.get<AdminUserStatisticsDto>(`${this.basePath}/statistics`);
    return response;
  }

  /**
   * Deactivate a user account
   */
  async deactivateUser(userId: string): Promise<void> {
    await apiClient.post(`${this.basePath}/${userId}/deactivate`, {});
  }

  /**
   * Activate a user account
   */
  async activateUser(userId: string): Promise<void> {
    await apiClient.post(`${this.basePath}/${userId}/activate`, {});
  }

  /**
   * Lock a user account for a specified duration
   */
  async lockUser(userId: string, request: LockUserRequest): Promise<void> {
    await apiClient.post(`${this.basePath}/${userId}/lock`, request);
  }

  /**
   * Unlock a user account
   */
  async unlockUser(userId: string): Promise<void> {
    await apiClient.post(`${this.basePath}/${userId}/unlock`, {});
  }

  /**
   * Resend email verification to a user
   */
  async resendVerification(userId: string): Promise<void> {
    await apiClient.post(`${this.basePath}/${userId}/resend-verification`, {});
  }

  /**
   * Force password reset for a user (sends reset email)
   */
  async forcePasswordReset(userId: string): Promise<void> {
    await apiClient.post(`${this.basePath}/${userId}/force-password-reset`, {});
  }
}

// Singleton instance
export const adminUsersRepository = new AdminUsersRepository();
