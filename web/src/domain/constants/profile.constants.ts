/**
 * Profile Constants
 *
 * Constants for cultural interests, languages, and proficiency levels
 * Matches backend CulturalInterest, LanguageCode, and ProficiencyLevel enums
 */

import type { ProficiencyLevel } from '../models/UserProfile';

/**
 * Event Interests (replaced with EventCategory from database)
 * Phase 6A.47: Removed hardcoded CULTURAL_INTERESTS constant
 *
 * Use useEventInterests() hook from @/infrastructure/api/hooks/useReferenceData
 * to fetch EventCategory from database via unified API endpoint:
 * GET /api/reference-data?types=EventCategory
 *
 * @deprecated Use useEventInterests() hook instead
 */

/**
 * Supported Languages (20 languages with ISO 639 codes)
 * Code matches backend LanguageCode.Code
 */
export interface LanguageOption {
  code: string;
  name: string;
  nativeName: string;
}

export const SUPPORTED_LANGUAGES: readonly LanguageOption[] = [
  // Sri Lankan languages first (priority)
  { code: 'si', name: 'Sinhala', nativeName: 'සිංහල' },
  { code: 'ta', name: 'Tamil', nativeName: 'தமிழ்' },
  { code: 'en', name: 'English', nativeName: 'English' },

  // Major South Asian languages
  { code: 'hi', name: 'Hindi', nativeName: 'हिन्दी' },
  { code: 'bn', name: 'Bengali', nativeName: 'বাংলা' },
  { code: 'ur', name: 'Urdu', nativeName: 'اردو' },
  { code: 'pa', name: 'Punjabi', nativeName: 'ਪੰਜਾਬੀ' },
  { code: 'gu', name: 'Gujarati', nativeName: 'ગુજરાતી' },
  { code: 'ml', name: 'Malayalam', nativeName: 'മലയാളം' },
  { code: 'kn', name: 'Kannada', nativeName: 'ಕನ್ನಡ' },
  { code: 'te', name: 'Telugu', nativeName: 'తెలుగు' },
  { code: 'mr', name: 'Marathi', nativeName: 'मराठी' },

  // International languages (for diaspora)
  { code: 'ar', name: 'Arabic', nativeName: 'العربية' },
  { code: 'fr', name: 'French', nativeName: 'Français' },
  { code: 'de', name: 'German', nativeName: 'Deutsch' },
  { code: 'es', name: 'Spanish', nativeName: 'Español' },
  { code: 'it', name: 'Italian', nativeName: 'Italiano' },
  { code: 'pt', name: 'Portuguese', nativeName: 'Português' },
  { code: 'nl', name: 'Dutch', nativeName: 'Nederlands' },
  { code: 'sv', name: 'Swedish', nativeName: 'Svenska' },
] as const;

/**
 * Proficiency Level Options
 * Matches backend ProficiencyLevel enum (4-level scale)
 */
export interface ProficiencyLevelOption {
  value: ProficiencyLevel;
  label: string;
  description: string;
}

export const PROFICIENCY_LEVELS: readonly ProficiencyLevelOption[] = [
  {
    value: 'Basic',
    label: 'Basic',
    description: 'Can understand and use familiar everyday expressions',
  },
  {
    value: 'Intermediate',
    label: 'Intermediate',
    description: 'Can handle routine work/social situations',
  },
  {
    value: 'Advanced',
    label: 'Advanced',
    description: 'Can express ideas fluently and spontaneously',
  },
  {
    value: 'Native',
    label: 'Native/Near-Native',
    description: 'Complete mastery of the language',
  },
] as const;

/**
 * Validation Constraints (matches backend validation)
 */
export const PROFILE_CONSTRAINTS = {
  basicInfo: {
    firstNameMinLength: 1,
    firstNameMaxLength: 50,
    lastNameMinLength: 1,
    lastNameMaxLength: 50,
    phoneNumberMaxLength: 20,
    bioMaxLength: 500,
  },
  location: {
    cityMaxLength: 100,
    stateMaxLength: 100,
    zipCodeMaxLength: 20,
    countryMaxLength: 100,
  },
  culturalInterests: {
    min: 0, // Optional
    // No max property - unlimited interests allowed
  },
  languages: {
    min: 1, // At least 1 language required
    max: 5, // Maximum 5 languages
  },
  preferredMetroAreas: {
    min: 0, // Optional (privacy choice)
    max: 20, // Maximum 20 metro areas (Phase 5B: Expanded from 10)
  },
} as const;
