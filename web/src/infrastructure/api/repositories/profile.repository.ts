import { apiClient } from '../client/api-client';
import type {
  UserProfile,
  UpdateLocationRequest,
  UpdateCulturalInterestsRequest,
  UpdateLanguagesRequest,
  UpdateBasicInfoRequest,
  UpdatePreferredMetroAreasRequest,
  PhotoUploadResponse,
  UpdateEmailResponse,
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
   *
   * Note: Uses native fetch instead of Axios to avoid Content-Type header issues with multipart/form-data
   */
  async uploadProfilePhoto(userId: string, file: File): Promise<PhotoUploadResponse> {
    const formData = new FormData();
    formData.append('image', file); // Backend expects 'image' parameter (UsersController.cs line 112)

    // Use native fetch API for file uploads to avoid Axios header conflicts
    // Fetch automatically sets correct Content-Type with boundary for FormData
    const baseURL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';
    const url = `${baseURL}${this.basePath}/${userId}/profile-photo`;

    const token = apiClient['authToken']; // Access private token field

    const response = await fetch(url, {
      method: 'POST',
      body: formData,
      credentials: 'include',
      headers: {
        ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
      },
      // Don't set Content-Type - browser will add it with boundary automatically
    });

    if (!response.ok) {
      const errorText = await response.text();
      let errorMessage = `Upload failed with status ${response.status}`;
      try {
        const errorJson = JSON.parse(errorText);
        errorMessage = errorJson.message || errorJson.error || errorMessage;
      } catch {
        // Not JSON, use text
      }
      throw new Error(errorMessage);
    }

    return await response.json();
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
   * Phase 6A.9 FIX: Backend returns 204 No Content, so we reload profile after update
   * @param userId User GUID
   * @param interests Cultural interests (0-10 items)
   * @returns Promise resolving to updated UserProfile
   */
  async updateCulturalInterests(
    userId: string,
    interests: UpdateCulturalInterestsRequest
  ): Promise<UserProfile> {
    // PUT request returns 204 No Content on success (UsersController.cs line 295)
    await apiClient.put<void>(
      `${this.basePath}/${userId}/cultural-interests`,
      interests
    );

    // Reload full profile to get updated culturalInterests
    const updatedProfile = await this.getProfile(userId);
    return updatedProfile;
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
   * Update basic user information (First Name, Last Name, Phone, Bio)
   * Phase 6A.70: Profile Basic Info Section
   * @param userId User GUID
   * @param basicInfo First name, last name, phone, bio
   * @returns Promise resolving to updated UserProfile
   */
  async updateBasicInfo(
    userId: string,
    basicInfo: UpdateBasicInfoRequest
  ): Promise<UserProfile> {
    const response = await apiClient.put<UserProfile>(
      `${this.basePath}/${userId}/basic-info`,
      basicInfo
    );
    return response;
  }

  /**
   * Update user email address
   * Phase 6A.70: Profile Basic Info Section with Email Verification
   * Triggers email verification flow (reuses Phase 6A.53 infrastructure)
   * @param userId User GUID
   * @param email New email address
   * @returns Promise resolving to UpdateEmailResponse with verification details
   */
  async updateEmail(
    userId: string,
    email: string
  ): Promise<UpdateEmailResponse> {
    const response = await apiClient.put<UpdateEmailResponse>(
      `${this.basePath}/${userId}/email`,
      { newEmail: email }
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
