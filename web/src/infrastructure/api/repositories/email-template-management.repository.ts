/**
 * Phase 6A.89: Email Template Management Repository
 * Repository for admin email template management API endpoints
 */

import { apiClient } from '../client/api-client';
import type {
  EmailTemplateDetailDto,
  GetEmailTemplatesRequest,
  GetEmailTemplatesResponse,
  UpdateEmailTemplateRequest,
  ToggleActiveRequest,
  TemplatePreviewRequest,
  TemplatePreviewResponse,
} from '../types/email-template-management.types';

export class EmailTemplateManagementRepository {
  private readonly basePath = '/admin/email-templates';

  /**
   * Get paginated list of email templates with optional filters
   */
  async getTemplates(request?: GetEmailTemplatesRequest): Promise<GetEmailTemplatesResponse> {
    const params = new URLSearchParams();

    if (request?.category) params.append('category', request.category);
    if (request?.isActive !== undefined) params.append('isActive', request.isActive.toString());
    if (request?.search) params.append('search', request.search);
    if (request?.page) params.append('page', request.page.toString());
    if (request?.pageSize) params.append('pageSize', request.pageSize.toString());

    const queryString = params.toString();
    const url = queryString ? `${this.basePath}?${queryString}` : this.basePath;

    const response = await apiClient.get<GetEmailTemplatesResponse>(url);
    return response;
  }

  /**
   * Get a single email template by ID
   */
  async getTemplateById(id: string): Promise<EmailTemplateDetailDto> {
    const response = await apiClient.get<EmailTemplateDetailDto>(`${this.basePath}/${id}`);
    return response;
  }

  /**
   * Get a single email template by name
   */
  async getTemplateByName(name: string): Promise<EmailTemplateDetailDto> {
    const response = await apiClient.get<EmailTemplateDetailDto>(
      `${this.basePath}/by-name/${encodeURIComponent(name)}`
    );
    return response;
  }

  /**
   * Update an email template
   */
  async updateTemplate(id: string, request: UpdateEmailTemplateRequest): Promise<EmailTemplateDetailDto> {
    const response = await apiClient.put<EmailTemplateDetailDto>(`${this.basePath}/${id}`, request);
    return response;
  }

  /**
   * Toggle email template active status
   */
  async toggleActive(id: string, request: ToggleActiveRequest): Promise<EmailTemplateDetailDto> {
    const response = await apiClient.patch<EmailTemplateDetailDto>(
      `${this.basePath}/${id}/toggle-active`,
      request
    );
    return response;
  }

  /**
   * Preview an email template with sample data
   */
  async previewTemplate(id: string, request: TemplatePreviewRequest): Promise<TemplatePreviewResponse> {
    const response = await apiClient.post<TemplatePreviewResponse>(
      `${this.basePath}/${id}/preview`,
      request
    );
    return response;
  }
}

// Export singleton instance
export const emailTemplateManagementRepository = new EmailTemplateManagementRepository();
