import { apiClient } from '../client/api-client';
import type {
  EmailGroupDto,
  CreateEmailGroupRequest,
  UpdateEmailGroupRequest,
} from '../types/email-groups.types';

/**
 * EmailGroupsRepository
 * Handles all email group-related API calls
 * Phase 6A.25: Email Groups Management
 */
export class EmailGroupsRepository {
  private readonly basePath = '/emailgroups';

  /**
   * Get email groups for the current user
   * For admins with includeAll=true, returns all groups across platform
   * @param includeAll If true and user is admin, returns all groups
   */
  async getEmailGroups(includeAll: boolean = false): Promise<EmailGroupDto[]> {
    const response = await apiClient.get<EmailGroupDto[]>(
      `${this.basePath}?includeAll=${includeAll}`
    );
    return response;
  }

  /**
   * Get an email group by ID
   */
  async getEmailGroupById(id: string): Promise<EmailGroupDto> {
    const response = await apiClient.get<EmailGroupDto>(
      `${this.basePath}/${id}`
    );
    return response;
  }

  /**
   * Create a new email group
   */
  async createEmailGroup(
    request: CreateEmailGroupRequest
  ): Promise<EmailGroupDto> {
    const response = await apiClient.post<EmailGroupDto>(
      this.basePath,
      request
    );
    return response;
  }

  /**
   * Update an email group
   */
  async updateEmailGroup(
    id: string,
    request: UpdateEmailGroupRequest
  ): Promise<EmailGroupDto> {
    const response = await apiClient.put<EmailGroupDto>(
      `${this.basePath}/${id}`,
      request
    );
    return response;
  }

  /**
   * Delete an email group (soft delete)
   */
  async deleteEmailGroup(id: string): Promise<void> {
    await apiClient.delete(`${this.basePath}/${id}`);
  }
}

// Export singleton instance
export const emailGroupsRepository = new EmailGroupsRepository();
