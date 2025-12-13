import { apiClient } from '../client/api-client';
import type {
  BadgeDto,
  UpdateBadgeDto,
  EventBadgeDto,
  BadgePosition,
} from '../types/badges.types';

/**
 * BadgesRepository
 * Handles all badge-related API calls
 * Phase 6A.25: Badge Management System
 * Phase 6A.27: Added forManagement and forAssignment parameters
 */
export class BadgesRepository {
  private readonly basePath = '/badges';

  /**
   * Get all badges with optional filters
   * Phase 6A.27: Added forManagement and forAssignment parameters for role-based filtering
   * @param activeOnly If true, returns only active badges (default). If false, returns all.
   * @param forManagement If true, filters for Badge Management UI (Admin: all, EventOrganizer: own custom)
   * @param forAssignment If true, filters for Badge Assignment UI (excludes expired badges)
   */
  async getBadges(
    activeOnly: boolean = true,
    forManagement: boolean = false,
    forAssignment: boolean = false
  ): Promise<BadgeDto[]> {
    const params = new URLSearchParams();
    params.append('activeOnly', String(activeOnly));
    if (forManagement) params.append('forManagement', 'true');
    if (forAssignment) params.append('forAssignment', 'true');

    const response = await apiClient.get<BadgeDto[]>(
      `${this.basePath}?${params.toString()}`
    );
    return response;
  }

  /**
   * Get a badge by ID
   */
  async getBadgeById(badgeId: string): Promise<BadgeDto> {
    const response = await apiClient.get<BadgeDto>(
      `${this.basePath}/${badgeId}`
    );
    return response;
  }

  /**
   * Create a new badge with image upload
   * Phase 6A.27: Added optional expiresAt parameter
   * @param name Badge name
   * @param position Badge position on event images
   * @param imageFile Badge image file (PNG recommended)
   * @param expiresAt Optional expiry date (ISO string)
   */
  async createBadge(
    name: string,
    position: BadgePosition,
    imageFile: File,
    expiresAt?: string
  ): Promise<BadgeDto> {
    const formData = new FormData();
    formData.append('name', name);
    formData.append('position', position.toString());
    formData.append('file', imageFile);
    if (expiresAt) {
      formData.append('expiresAt', expiresAt);
    }

    const response = await apiClient.post<BadgeDto>(
      this.basePath,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );
    return response;
  }

  /**
   * Update a badge's details
   */
  async updateBadge(badgeId: string, dto: UpdateBadgeDto): Promise<BadgeDto> {
    const response = await apiClient.put<BadgeDto>(
      `${this.basePath}/${badgeId}`,
      dto
    );
    return response;
  }

  /**
   * Update a badge's image
   */
  async updateBadgeImage(badgeId: string, imageFile: File): Promise<BadgeDto> {
    const formData = new FormData();
    formData.append('file', imageFile);

    const response = await apiClient.put<BadgeDto>(
      `${this.basePath}/${badgeId}/image`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );
    return response;
  }

  /**
   * Delete a badge (system badges are only deactivated)
   */
  async deleteBadge(badgeId: string): Promise<void> {
    await apiClient.delete(`${this.basePath}/${badgeId}`);
  }

  /**
   * Get badges assigned to an event
   */
  async getEventBadges(eventId: string): Promise<EventBadgeDto[]> {
    const response = await apiClient.get<EventBadgeDto[]>(
      `${this.basePath}/events/${eventId}`
    );
    return response;
  }

  /**
   * Assign a badge to an event
   */
  async assignBadgeToEvent(
    eventId: string,
    badgeId: string
  ): Promise<EventBadgeDto> {
    const response = await apiClient.post<EventBadgeDto>(
      `${this.basePath}/events/${eventId}/badges/${badgeId}`
    );
    return response;
  }

  /**
   * Remove a badge from an event
   */
  async removeBadgeFromEvent(eventId: string, badgeId: string): Promise<void> {
    await apiClient.delete(
      `${this.basePath}/events/${eventId}/badges/${badgeId}`
    );
  }
}

// Export singleton instance
export const badgesRepository = new BadgesRepository();
