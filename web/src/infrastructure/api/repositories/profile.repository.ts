import { apiClient } from '../client/api-client';
import type {
  UserProfile,
  UpdateLocationRequest,
  UpdateCulturalInterestsRequest,
  UpdateLanguagesRequest,
  UpdateBasicInfoRequest,
  UpdatePreferredMetroAreasRequest,
  PhotoUploadResponse,
} from '@/domain/models/UserProfile';

/**
 * ProfileRepository
 * Handles all profile-related API calls
 * Repository pattern for profile operations
 *
 * Endpoints from UsersController.cs:
 * - GET /api/users/{id} - Get user profile
 * - POST /api/users/{id}/profile-photo - Upload profile photo
 * - DELETE /api/users/{id}/profile-photo - Delete profile photo
 * - PUT /api/users/{id}/location - Update location
 * - PUT /api/users/{id}/cultural-interests - Update cultural interests
 * - PUT /api/users/{id}/languages - Update languages
 */
export class ProfileRepository {
  private readonly basePath = '/users';

  /**
   * Get user profile by ID
   * @param userId User GUID
   * @returns Promise resolving to UserProfile
   */
  async getProfile(userId: string): Promise<UserProfile> {
    const response = await apiClient.get<UserProfile>(`${this.basePath}/${userId}`);
    return response;
  }

  /**
   * Upload profile photo
   * @param userId User GUID
   * @param file Image file (max 5MB)
   * @returns Promise resolving to PhotoUploadResponse with new URL
   */
  async uploadProfilePhoto(userId: string, file: File): Promise<PhotoUploadResponse> {
    const formData = new FormData();
    formData.append('file', file);

    const response = await apiClient.postMultipart<PhotoUploadResponse>(
      `${this.basePath}/${userId}/profile-photo`,
      formData
    );
    return response;
  }

  /**
   * Delete profile photo
   * @param userId User GUID
   * @returns Promise resolving to success message
   */
  async deleteProfilePhoto(userId: string): Promise<{ message: string }> {
    const response = await apiClient.delete<{ message: string }>(
      `${this.basePath}/${userId}/profile-photo`
    );
    return response;
  }

  /**
   * Update user location
   * @param userId User GUID
   * @param location Location data (all fields optional)
   * @returns Promise resolving to updated UserProfile
   */
  async updateLocation(
    userId: string,
    location: UpdateLocationRequest
  ): Promise<UserProfile> {
    const response = await apiClient.put<UserProfile>(
      `${this.basePath}/${userId}/location`,
      location
    );
    return response;
  }

  /**
   * Update cultural interests
   * @param userId User GUID
   * @param interests Cultural interests (0-10 items)
   * @returns Promise resolving to updated UserProfile
   */
  async updateCulturalInterests(
    userId: string,
    interests: UpdateCulturalInterestsRequest
  ): Promise<UserProfile> {
    const response = await apiClient.put<UserProfile>(
      `${this.basePath}/${userId}/cultural-interests`,
      interests
    );
    return response;
  }

  /**
   * Update languages
   * @param userId User GUID
   * @param languages Languages with proficiency levels
   * @returns Promise resolving to updated UserProfile
   */
  async updateLanguages(
    userId: string,
    languages: UpdateLanguagesRequest
  ): Promise<UserProfile> {
    const response = await apiClient.put<UserProfile>(
      `${this.basePath}/${userId}/languages`,
      languages
    );
    return response;
  }

  /**
   * Update basic user information
   * @param userId User GUID
   * @param basicInfo First name, last name, phone, bio
   * @returns Promise resolving to updated UserProfile
   */
  async updateBasicInfo(
    userId: string,
    basicInfo: UpdateBasicInfoRequest
  ): Promise<UserProfile> {
    // Note: This endpoint doesn't exist in backend yet
    // Using PUT /api/users/{id} as a placeholder
    // Backend team should add dedicated endpoint for basic info updates
    const response = await apiClient.put<UserProfile>(
      `${this.basePath}/${userId}`,
      basicInfo
    );
    return response;
  }

  /**
   * Update user's preferred metro areas for location-based filtering
   * Phase 5B: User Preferred Metro Areas - Expanded to 20 max limit
   * Phase 6A.9 FIX: Backend returns 204 No Content, so we reload profile after update
   * @param userId User GUID
   * @param metroAreas Metro area IDs (0-20 items, GUIDs)
   * @returns Promise resolving to updated UserProfile
   */
  async updatePreferredMetroAreas(
    userId: string,
    request: UpdatePreferredMetroAreasRequest
  ): Promise<UserProfile> {
    // PUT request returns 204 No Content on success
    await apiClient.put<void>(
      `${this.basePath}/${userId}/preferred-metro-areas`,
      request
    );

    // Reload full profile to get updated preferredMetroAreas
    const updatedProfile = await this.getProfile(userId);
    return updatedProfile;
  }

  /**
   * Get user's preferred metro areas with full details
   * Phase 5B: User Preferred Metro Areas
   * @param userId User GUID
   * @returns Promise resolving to array of metro area GUIDs
   */
  async getPreferredMetroAreas(userId: string): Promise<string[]> {
    const response = await apiClient.get<string[]>(
      `${this.basePath}/${userId}/preferred-metro-areas`
    );
    return response;
  }
}

/**
 * Singleton instance of the profile repository
 */
export const profileRepository = new ProfileRepository();
