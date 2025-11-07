/**
 * Domain Models for User Profile
 *
 * These models represent the core profile data structures that match
 * the backend API contracts defined in UsersController.cs
 */

/**
 * Proficiency levels for language preferences
 * Matches backend ProficiencyLevel enum
 */
export type ProficiencyLevel = 'Basic' | 'Intermediate' | 'Advanced' | 'Native';

/**
 * User location information (nullable - privacy choice)
 * Matches backend UserLocation value object
 */
export interface Location {
  city?: string | null;
  state?: string | null;
  zipCode?: string | null;
  country?: string | null;
}

/**
 * Language preference with proficiency level
 * Matches backend LanguagePreference entity
 */
export interface Language {
  languageCode: string;
  proficiencyLevel: ProficiencyLevel;
}

/**
 * Basic user information (editable profile fields)
 */
export interface BasicInfo {
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
  bio?: string | null;
}

/**
 * Complete user profile
 * Matches backend UserDto from UsersController.cs
 */
export interface UserProfile {
  id: string; // GUID
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
  bio?: string | null;
  profilePhotoUrl?: string | null;
  location?: Location | null;
  culturalInterests?: string[];
  languages?: Language[];
}

/**
 * Request models for API updates
 */

export interface UpdateLocationRequest {
  city?: string | null;
  state?: string | null;
  zipCode?: string | null;
  country?: string | null;
}

export interface UpdateCulturalInterestsRequest {
  culturalInterests: string[]; // 0-10 items
}

export interface UpdateLanguagesRequest {
  languages: Language[]; // 1-5 items with proficiency
}

export interface UpdateBasicInfoRequest {
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
  bio?: string | null;
}

/**
 * Photo upload types
 */
export interface PhotoUploadResponse {
  profilePhotoUrl: string;
  message?: string;
}
