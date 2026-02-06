/**
 * Phase 6A.89: Email Template Management Types
 * API types for admin email template management
 */

/**
 * Detailed email template for editing
 */
export interface EmailTemplateDetailDto {
  id: string;
  name: string;
  description: string;
  subject: string;
  textTemplate: string;
  htmlTemplate: string | null;
  category: string;
  type: string;
  isActive: boolean;
  tags: string | null;
  createdAt: string;
  updatedAt: string | null;
  requiredParameters: string[];
  optionalParameters: string[];
}

/**
 * Email template list item (from GetEmailTemplatesQuery)
 */
export interface EmailTemplateListItemDto {
  name: string;
  displayName: string;
  description: string;
  subject: string;
  htmlTemplate: string;
  plainTextTemplate: string;
  category: EmailTemplateCategory;
  requiredParameters: string[];
  optionalParameters: string[];
  isActive: boolean;
  createdAt: string;
  lastModified: string;
}

/**
 * Response from GetEmailTemplates API
 */
export interface GetEmailTemplatesResponse {
  templates: EmailTemplateListItemDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  categoryCounts: Record<string, number>;
}

/**
 * Request to get email templates with filters
 */
export interface GetEmailTemplatesRequest {
  category?: string;
  isActive?: boolean;
  search?: string;
  page?: number;
  pageSize?: number;
}

/**
 * Request to update an email template
 */
export interface UpdateEmailTemplateRequest {
  subject: string;
  textTemplate: string;
  htmlTemplate?: string;
  description?: string;
  tags?: string;
}

/**
 * Request to toggle template active status
 */
export interface ToggleActiveRequest {
  isActive: boolean;
}

/**
 * Request for template preview
 */
export interface TemplatePreviewRequest {
  parameters?: Record<string, string>;
}

/**
 * Response from template preview
 */
export interface TemplatePreviewResponse {
  subject: string;
  textBody: string;
  htmlBody: string | null;
  templateName: string;
}

/**
 * Email template categories (matches backend enum)
 */
export enum EmailTemplateCategory {
  Authentication = 'Authentication',
  Business = 'Business',
  Marketing = 'Marketing',
  Notification = 'Notification',
  System = 'System',
}
