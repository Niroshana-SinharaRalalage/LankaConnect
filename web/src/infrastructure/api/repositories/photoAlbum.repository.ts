import { apiClient } from '../client/api-client';
import type {
  PhotoAlbumDto,
  AlbumPhotoDto,
  PaginatedAlbumPhotosResponse,
  CreatePhotoAlbumRequest,
  UpdateAlbumDetailsRequest,
} from '../types/events.types';

/**
 * PhotoAlbumRepository
 * Multi-album API integration following the repository pattern.
 *
 * Backend endpoints from PhotoAlbumsController.cs:
 * - GET    /api/events/{eventId}/albums                      - List all albums
 * - POST   /api/events/{eventId}/albums                      - Create album
 * - PUT    /api/events/{eventId}/albums/{albumId}             - Update album details
 * - DELETE /api/events/{eventId}/albums/{albumId}             - Delete draft album
 * - POST   /api/events/{eventId}/albums/{albumId}/publish     - Publish album
 * - POST   /api/events/{eventId}/albums/{albumId}/notify      - Send email notification
 * - GET    /api/events/{eventId}/albums/{albumId}/photos      - Get photos (paginated)
 * - POST   /api/events/{eventId}/albums/{albumId}/photos      - Upload photo
 * - DELETE /api/events/{eventId}/albums/{albumId}/photos/{id} - Delete photo
 * - PUT    /api/events/{eventId}/albums/{albumId}/cover/{id}  - Set cover photo
 * - GET    /api/events/{eventId}/albums/{albumId}/download    - Download ZIP
 */
export class PhotoAlbumRepository {
  private basePath(eventId: string): string {
    return `/events/${eventId}/albums`;
  }

  private albumPath(eventId: string, albumId: string): string {
    return `${this.basePath(eventId)}/${albumId}`;
  }

  // ==================== ALBUM MANAGEMENT ====================

  /**
   * Get all albums for an event
   */
  async getAlbums(eventId: string): Promise<PhotoAlbumDto[]> {
    return await apiClient.get<PhotoAlbumDto[]>(this.basePath(eventId));
  }

  /**
   * Create a new photo album for an event
   */
  async createAlbum(eventId: string, request: CreatePhotoAlbumRequest): Promise<PhotoAlbumDto> {
    return await apiClient.post<PhotoAlbumDto>(this.basePath(eventId), request);
  }

  /**
   * Update album details (name and description)
   */
  async updateAlbumDetails(
    eventId: string,
    albumId: string,
    request: UpdateAlbumDetailsRequest,
  ): Promise<void> {
    await apiClient.put(`${this.albumPath(eventId, albumId)}`, request);
  }

  /**
   * Delete a draft album and clean up blob storage
   */
  async deleteAlbum(eventId: string, albumId: string): Promise<void> {
    await apiClient.delete(`${this.albumPath(eventId, albumId)}`);
  }

  /**
   * Publish an album, making it visible to attendees
   */
  async publishAlbum(eventId: string, albumId: string): Promise<void> {
    await apiClient.post(`${this.albumPath(eventId, albumId)}/publish`);
  }

  /**
   * Send email notification to registered attendees about a published album
   */
  async sendNotification(eventId: string, albumId: string): Promise<void> {
    await apiClient.post(`${this.albumPath(eventId, albumId)}/notify`);
  }

  // ==================== PHOTO OPERATIONS ====================

  /**
   * Get photos for an album with cursor-based pagination
   */
  async getPhotos(
    eventId: string,
    albumId: string,
    pageSize?: number,
    cursor?: string,
  ): Promise<PaginatedAlbumPhotosResponse> {
    const params: Record<string, string | number> = {};
    if (pageSize) params.pageSize = pageSize;
    if (cursor) params.cursor = cursor;

    return await apiClient.get<PaginatedAlbumPhotosResponse>(
      `${this.albumPath(eventId, albumId)}/photos`,
      { params },
    );
  }

  /**
   * Upload a photo to an album
   */
  async uploadPhoto(
    eventId: string,
    albumId: string,
    file: File,
    caption?: string,
  ): Promise<AlbumPhotoDto> {
    const formData = new FormData();
    formData.append('image', file);
    if (caption) formData.append('caption', caption);

    return await apiClient.post<AlbumPhotoDto>(
      `${this.albumPath(eventId, albumId)}/photos`,
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    );
  }

  /**
   * Upload a video to an album with a thumbnail image
   */
  async uploadVideo(
    eventId: string,
    albumId: string,
    videoFile: File,
    thumbnailFile: File,
    caption?: string,
    durationSeconds?: number,
  ): Promise<AlbumPhotoDto> {
    const formData = new FormData();
    formData.append('video', videoFile);
    formData.append('thumbnail', thumbnailFile);
    if (caption) formData.append('caption', caption);
    if (durationSeconds != null) formData.append('durationSeconds', String(durationSeconds));

    return await apiClient.post<AlbumPhotoDto>(
      `${this.albumPath(eventId, albumId)}/videos`,
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } },
    );
  }

  /**
   * Delete a photo from an album
   */
  async deletePhoto(eventId: string, albumId: string, photoId: string): Promise<void> {
    await apiClient.delete(`${this.albumPath(eventId, albumId)}/photos/${photoId}`);
  }

  /**
   * Bulk delete photos from an album (organizer only, max 50)
   */
  async bulkDeletePhotos(
    eventId: string,
    albumId: string,
    photoIds: string[],
  ): Promise<number> {
    return await apiClient.post<number>(
      `${this.albumPath(eventId, albumId)}/photos/bulk-delete`,
      { photoIds },
    );
  }

  /**
   * Set a photo as the album cover
   */
  async setCoverPhoto(eventId: string, albumId: string, photoId: string): Promise<void> {
    await apiClient.put(`${this.albumPath(eventId, albumId)}/cover/${photoId}`);
  }

  /**
   * Download all album photos as a ZIP file
   */
  async downloadZip(eventId: string, albumId: string): Promise<Blob> {
    return await apiClient.get<Blob>(
      `${this.albumPath(eventId, albumId)}/download`,
      { responseType: 'blob' },
    );
  }
}

/**
 * Singleton instance of PhotoAlbumRepository
 */
export const photoAlbumRepository = new PhotoAlbumRepository();
