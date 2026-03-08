import { apiClient } from '../client/api-client';
import type {
  PhotoAlbumDto,
  AlbumPhotoDto,
  PaginatedAlbumPhotosResponse,
  CreatePhotoAlbumRequest,
  UpdateAlbumSettingsRequest,
} from '../types/events.types';

/**
 * PhotoAlbumRepository
 * Handles all photo album-related API calls following the repository pattern
 *
 * Backend endpoints from PhotoAlbumController.cs:
 * - GET    /api/events/{eventId}/album              - Get album metadata
 * - POST   /api/events/{eventId}/album              - Create album
 * - PUT    /api/events/{eventId}/album/settings      - Update album settings
 * - POST   /api/events/{eventId}/album/publish       - Publish album
 * - POST   /api/events/{eventId}/album/close         - Close album
 * - GET    /api/events/{eventId}/album/photos        - Get photos (paginated)
 * - POST   /api/events/{eventId}/album/photos        - Upload photo
 * - DELETE /api/events/{eventId}/album/photos/{id}   - Delete photo
 * - POST   /api/events/{eventId}/album/photos/{id}/approve  - Approve photo
 * - POST   /api/events/{eventId}/album/photos/{id}/reject   - Reject photo
 * - POST   /api/events/{eventId}/album/photos/{id}/cover    - Set cover photo
 * - GET    /api/events/{eventId}/album/photos/pending       - Get pending photos
 */
export class PhotoAlbumRepository {
  /**
   * Build the base album path for a given event
   */
  private albumPath(eventId: string): string {
    return `/events/${eventId}/album`;
  }

  // ==================== ALBUM MANAGEMENT ====================

  /**
   * Get album metadata for an event
   * Returns null if the event does not have an album (404)
   *
   * @param eventId - Event ID
   * @returns Album DTO or null if not found
   */
  async getAlbum(eventId: string): Promise<PhotoAlbumDto | null> {
    try {
      return await apiClient.get<PhotoAlbumDto>(this.albumPath(eventId));
    } catch (error: any) {
      if (error?.response?.status === 404) return null;
      throw error;
    }
  }

  /**
   * Create a new photo album for an event
   *
   * @param eventId - Event ID
   * @param request - Optional creation parameters (description)
   * @returns Created album DTO
   */
  async createAlbum(eventId: string, request?: CreatePhotoAlbumRequest): Promise<PhotoAlbumDto> {
    return await apiClient.post<PhotoAlbumDto>(
      this.albumPath(eventId),
      request ?? {},
    );
  }

  /**
   * Update album settings (upload permission, moderation mode, description)
   *
   * @param eventId - Event ID
   * @param request - Settings to update
   */
  async updateSettings(eventId: string, request: UpdateAlbumSettingsRequest): Promise<void> {
    await apiClient.put(`${this.albumPath(eventId)}/settings`, request);
  }

  /**
   * Publish an album, making it visible to attendees
   *
   * @param eventId - Event ID
   */
  async publishAlbum(eventId: string): Promise<void> {
    await apiClient.post(`${this.albumPath(eventId)}/publish`);
  }

  /**
   * Close an album, preventing further uploads
   *
   * @param eventId - Event ID
   */
  async closeAlbum(eventId: string): Promise<void> {
    await apiClient.post(`${this.albumPath(eventId)}/close`);
  }

  // ==================== PHOTO OPERATIONS ====================

  /**
   * Get photos for an album with cursor-based pagination
   *
   * @param eventId - Event ID
   * @param pageSize - Number of photos per page (default: 20)
   * @param cursor - Pagination cursor for next page
   * @returns Paginated photos response
   */
  async getPhotos(
    eventId: string,
    pageSize?: number,
    cursor?: string,
  ): Promise<PaginatedAlbumPhotosResponse> {
    const params: Record<string, string | number> = {};
    if (pageSize) params.pageSize = pageSize;
    if (cursor) params.cursor = cursor;

    return await apiClient.get<PaginatedAlbumPhotosResponse>(
      `${this.albumPath(eventId)}/photos`,
      { params },
    );
  }

  /**
   * Upload a photo to an album
   * Uses FormData for multipart file upload
   *
   * @param eventId - Event ID
   * @param file - Image file to upload
   * @param caption - Optional photo caption
   * @returns Uploaded photo DTO
   */
  async uploadPhoto(eventId: string, file: File, caption?: string): Promise<AlbumPhotoDto> {
    const formData = new FormData();
    formData.append('image', file);
    if (caption) formData.append('caption', caption);

    return await apiClient.post<AlbumPhotoDto>(
      `${this.albumPath(eventId)}/photos`,
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    );
  }

  /**
   * Delete a photo from an album
   *
   * @param eventId - Event ID
   * @param photoId - Photo ID to delete
   */
  async deletePhoto(eventId: string, photoId: string): Promise<void> {
    await apiClient.delete(`${this.albumPath(eventId)}/photos/${photoId}`);
  }

  /**
   * Approve a photo (moderation action)
   *
   * @param eventId - Event ID
   * @param photoId - Photo ID to approve
   */
  async approvePhoto(eventId: string, photoId: string): Promise<void> {
    await apiClient.post(`${this.albumPath(eventId)}/photos/${photoId}/approve`);
  }

  /**
   * Reject a photo (moderation action)
   *
   * @param eventId - Event ID
   * @param photoId - Photo ID to reject
   */
  async rejectPhoto(eventId: string, photoId: string): Promise<void> {
    await apiClient.post(`${this.albumPath(eventId)}/photos/${photoId}/reject`);
  }

  /**
   * Set a photo as the album cover
   *
   * @param eventId - Event ID
   * @param photoId - Photo ID to set as cover
   */
  async setCoverPhoto(eventId: string, photoId: string): Promise<void> {
    await apiClient.post(`${this.albumPath(eventId)}/photos/${photoId}/cover`);
  }

  // ==================== MODERATION ====================

  /**
   * Get photos pending moderation approval
   *
   * @param eventId - Event ID
   * @returns Array of photos awaiting approval
   */
  async getPendingPhotos(eventId: string): Promise<AlbumPhotoDto[]> {
    return await apiClient.get<AlbumPhotoDto[]>(
      `${this.albumPath(eventId)}/photos/pending`,
    );
  }
}

/**
 * Singleton instance of PhotoAlbumRepository
 */
export const photoAlbumRepository = new PhotoAlbumRepository();
