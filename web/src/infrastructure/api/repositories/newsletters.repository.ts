import { apiClient } from '../client/api-client';
import type {
  NewsletterDto,
  CreateNewsletterRequest,
  UpdateNewsletterRequest,
  RecipientPreviewDto,
  GetNewslettersFilters,
} from '../types/newsletters.types';

/**
 * NewslettersRepository
 * Phase 6A.74: Newsletter/News Alert API integration
 * Handles all newsletter-related API calls following the repository pattern
 *
 * Backend endpoints from NewslettersController.cs:
 * - POST /api/newsletters - Create newsletter
 * - PUT /api/newsletters/{id} - Update draft
 * - DELETE /api/newsletters/{id} - Delete draft
 * - POST /api/newsletters/{id}/publish - Publish newsletter
 * - POST /api/newsletters/{id}/send - Queue email send job
 * - GET /api/newsletters/{id} - Get newsletter by ID
 * - GET /api/newsletters/my-newsletters - Get current user's newsletters
 * - GET /api/newsletters/event/{eventId} - Get newsletters for specific event
 * - GET /api/newsletters/{id}/recipient-preview - Preview recipients
 */
export class NewslettersRepository {
  private readonly basePath = '/newsletters';

  // ==================== QUERIES ====================

  /**
   * Get all newsletters created by current user
   * Maps to backend GetNewslettersByCreatorQuery
   */
  async getMyNewsletters(): Promise<NewsletterDto[]> {
    return await apiClient.get<NewsletterDto[]>(`${this.basePath}/my-newsletters`);
  }

  /**
   * Get newsletter by ID
   * Maps to backend GetNewsletterByIdQuery
   *
   * @param id - Newsletter ID (GUID)
   * @throws ApiError if not found or unauthorized
   */
  async getNewsletterById(id: string): Promise<NewsletterDto> {
    return await apiClient.get<NewsletterDto>(`${this.basePath}/${id}`);
  }

  /**
   * Get all newsletters for a specific event
   * Maps to backend GetNewslettersByEventQuery
   * Phase 6A.74 Enhancement 3: Used in Event Management Communications tab
   *
   * @param eventId - Event ID (GUID)
   */
  async getNewslettersByEvent(eventId: string): Promise<NewsletterDto[]> {
    return await apiClient.get<NewsletterDto[]>(`${this.basePath}/event/${eventId}`);
  }

  /**
   * Preview recipients before sending newsletter
   * Maps to backend GetRecipientPreviewQuery
   * Shows deduplicated recipient count with location-based breakdown
   *
   * @param id - Newsletter ID (GUID)
   */
  async getRecipientPreview(id: string): Promise<RecipientPreviewDto> {
    return await apiClient.get<RecipientPreviewDto>(`${this.basePath}/${id}/recipient-preview`);
  }

  /**
   * Get all published (Active) newsletters for public landing page
   * Maps to backend GetPublishedNewslettersQuery
   * Phase 6A.74 Part 5B: Public newsletter display
   *
   * Returns only Active newsletters, sorted by publishedAt desc
   * No authentication required (public endpoint)
   */
  async getPublishedNewsletters(): Promise<NewsletterDto[]> {
    return await apiClient.get<NewsletterDto[]>(`${this.basePath}/published`);
  }

  // ==================== MUTATIONS ====================

  /**
   * Create a new newsletter in Draft status
   * Maps to backend CreateNewsletterCommand
   *
   * @param data - Newsletter creation data
   * @returns Newsletter ID (GUID)
   */
  async createNewsletter(data: CreateNewsletterRequest): Promise<string> {
    return await apiClient.post<string>(this.basePath, data);
  }

  /**
   * Update a draft newsletter
   * Maps to backend UpdateNewsletterCommand
   * Only Draft newsletters can be updated
   *
   * @param id - Newsletter ID (GUID)
   * @param data - Newsletter update data
   */
  async updateNewsletter(id: string, data: UpdateNewsletterRequest): Promise<void> {
    await apiClient.put(`${this.basePath}/${id}`, data);
  }

  /**
   * Delete a draft newsletter
   * Maps to backend DeleteNewsletterCommand
   * Only Draft newsletters can be deleted
   *
   * @param id - Newsletter ID (GUID)
   */
  async deleteNewsletter(id: string): Promise<void> {
    await apiClient.delete(`${this.basePath}/${id}`);
  }

  /**
   * Publish a newsletter (Draft → Active)
   * Maps to backend PublishNewsletterCommand
   * Sets 7-day expiration and makes newsletter visible
   *
   * @param id - Newsletter ID (GUID)
   */
  async publishNewsletter(id: string): Promise<void> {
    await apiClient.post(`${this.basePath}/${id}/publish`);
  }

  /**
   * Queue newsletter email sending via Hangfire background job
   * Maps to backend SendNewsletterCommand
   * Only Active newsletters (not already sent) can be sent
   *
   * @param id - Newsletter ID (GUID)
   * @returns 202 Accepted (job queued)
   */
  async sendNewsletter(id: string): Promise<void> {
    await apiClient.post(`${this.basePath}/${id}/send`);
  }
}

/**
 * Singleton instance for use across the application
 */
export const newslettersRepository = new NewslettersRepository();
