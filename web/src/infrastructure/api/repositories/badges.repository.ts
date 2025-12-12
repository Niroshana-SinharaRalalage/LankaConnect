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
 */
export class BadgesRepository {
  private readonly basePath = '/badges';

  /**
   * Get all badges (optionally filtered by active status)
   * @param activeOnly If true, returns only active badges (default). If false, returns all.
   */
  async getBadges(activeOnly: boolean = true): Promise<BadgeDto[]> {
    const response = await apiClient.get<BadgeDto[]>(
      `${this.basePath}?activeOnly=${activeOnly}`
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
   * @param name Badge name
   * @param position Badge position on event images
   * @param imageFile Badge image file (PNG recommended)
   */
  async createBadge(
    name: string,
    position: BadgePosition,
    imageFile: File
  ): Promise<BadgeDto> {
    const formData = new FormData();
    formData.append('name', name);
    formData.append('position', position.toString());
    formData.append('file', imageFile);

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
