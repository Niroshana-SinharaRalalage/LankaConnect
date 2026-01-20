/**
 * Newsletters API Type Definitions
 * Phase 6A.74: Newsletter/News Alert System
 * DTOs matching backend API contracts (LankaConnect.Application.Communications.Common)
 */

// ==================== Enums ====================

/**
 * Newsletter status enum matching backend LankaConnect.Domain.Communications.Enums.NewsletterStatus
 * Phase 6A.74 Part 11 Issue #4 Fix: Backend stores as string, not int
 * Database column: VARCHAR(20) with values "Draft", "Active", "Inactive", "Sent"
 */
export enum NewsletterStatus {
  Draft = 'Draft',
  Active = 'Active',
  Inactive = 'Inactive',
  Sent = 'Sent',
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
  // Phase 6A.74 Part 13+: Detailed recipient breakdown from NewsletterEmailHistory
  totalRecipientCount?: number | null;
  newsletterEmailGroupCount?: number | null;
  eventEmailGroupCount?: number | null;
  subscriberCount?: number | null;
  eventRegistrationCount?: number | null;
  successfulSends?: number | null;
  failedSends?: number | null;
  // Legacy fields for backwards compatibility
  emailGroupRecipientCount?: number | null;
  subscriberRecipientCount?: number | null;
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
 * Phase 6A.74 Parts 10 & 11: Extended with public newsletter list filters
 */
export interface GetNewslettersFilters {
  status?: NewsletterStatus;
  eventId?: string;
  // Phase 6A.74 Parts 10/11: Public newsletter list filters
  publishedFrom?: Date;
  publishedTo?: Date;
  state?: string;
  metroAreaIds?: string[];
  searchTerm?: string;
  userId?: string;
  latitude?: number;
  longitude?: number;
}
