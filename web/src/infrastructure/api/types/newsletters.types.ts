/**
 * Newsletters API Type Definitions
 * Phase 6A.74: Newsletter/News Alert System
 * DTOs matching backend API contracts (LankaConnect.Application.Communications.Common)
 */

// ==================== Enums ====================

/**
 * Newsletter status enum matching backend LankaConnect.Domain.Communications.Enums.NewsletterStatus
 */
export enum NewsletterStatus {
  Draft = 0,
  Active = 2,
  Inactive = 3,
  Sent = 4,
}

// ==================== DTOs ====================

/**
 * Newsletter DTO matching backend NewsletterDto
 */
export interface NewsletterDto {
  id: string;
  title: string;
  description: string;
  createdByUserId: string;
  createdByUserName: string;
  eventId: string | null;
  eventTitle: string | null;
  status: NewsletterStatus;
  publishedAt: string | null;
  sentAt: string | null;
  expiresAt: string | null;
  includeNewsletterSubscribers: boolean;
  targetAllLocations: boolean;
  createdAt: string;
  updatedAt: string | null;
  emailGroupIds: string[];
  emailGroups: EmailGroupSummaryDto[];
  metroAreaIds: string[];
  metroAreas: MetroAreaSummaryDto[];
}

/**
 * Email group summary DTO (lightweight)
 */
export interface EmailGroupSummaryDto {
  id: string;
  name: string;
  isActive: boolean;
}

/**
 * Metro area summary DTO (lightweight)
 */
export interface MetroAreaSummaryDto {
  id: string;
  name: string;
  state: string;
}

/**
 * Recipient preview DTO matching backend RecipientPreviewDto
 */
export interface RecipientPreviewDto {
  totalRecipients: number;
  emailAddresses: string[];
  breakdown: RecipientBreakdownDto;
}

/**
 * Breakdown of newsletter recipient sources
 */
export interface RecipientBreakdownDto {
  emailGroupCount: number;
  metroAreaSubscribers: number;
  stateLevelSubscribers: number;
  allLocationsSubscribers: number;
  totalNewsletterSubscribers: number;
}

// ==================== Request DTOs ====================

/**
 * Request DTO for creating a newsletter
 */
export interface CreateNewsletterRequest {
  title: string;
  description: string;
  emailGroupIds?: string[];
  includeNewsletterSubscribers: boolean;
  eventId?: string;
  targetAllLocations: boolean;
  metroAreaIds?: string[];
}

/**
 * Request DTO for updating a draft newsletter
 */
export interface UpdateNewsletterRequest {
  title: string;
  description: string;
  emailGroupIds?: string[];
  includeNewsletterSubscribers: boolean;
  eventId?: string;
  targetAllLocations: boolean;
  metroAreaIds?: string[];
}

/**
 * Filters for querying newsletters
 */
export interface GetNewslettersFilters {
  status?: NewsletterStatus;
  eventId?: string;
}
