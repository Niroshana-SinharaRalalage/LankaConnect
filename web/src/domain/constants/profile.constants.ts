/**
 * Profile Constants
 *
 * Constants for cultural interests, languages, and proficiency levels
 * Matches backend CulturalInterest, LanguageCode, and ProficiencyLevel enums
 */

import type { ProficiencyLevel } from '../models/UserProfile';

/**
 * Cultural Interests (20 predefined interests)
 * Code matches backend CulturalInterest.Code
 */
export interface CulturalInterestOption {
  code: string;
  name: string;
}

export const CULTURAL_INTERESTS: readonly CulturalInterestOption[] = [
  { code: 'SL_CUISINE', name: 'Sri Lankan Cuisine' },
  { code: 'BUDDHIST_FEST', name: 'Buddhist Festivals & Traditions' },
  { code: 'HINDU_FEST', name: 'Hindu Festivals & Traditions' },
  { code: 'ISLAMIC_FEST', name: 'Islamic Festivals & Traditions' },
  { code: 'CHRISTIAN_FEST', name: 'Christian Festivals & Traditions' },
  { code: 'TRAD_DANCE', name: 'Traditional Dance (Kandyan, Sabaragamuwa, Low Country)' },
  { code: 'CRICKET', name: 'Cricket & Sports' },
  { code: 'AYURVEDA', name: 'Ayurvedic Medicine & Wellness' },
  { code: 'SINHALA_MUSIC', name: 'Sinhala Music & Arts' },
  { code: 'TAMIL_MUSIC', name: 'Tamil Music & Arts' },
  { code: 'VESAK', name: 'Vesak & Poson Celebrations' },
  { code: 'SINHALA_NY', name: 'Sinhala & Tamil New Year (Aluth Avurudda)' },
  { code: 'TEA_CULTURE', name: 'Ceylon Tea Culture' },
  { code: 'TRAD_ARTS', name: 'Traditional Arts & Crafts (Masks, Batik, Pottery)' },
  { code: 'SL_WEDDINGS', name: 'Sri Lankan Wedding Traditions' },
  { code: 'TEMPLE_ARCH', name: 'Temple Architecture & Heritage Sites' },
  { code: 'SL_LITERATURE', name: 'Sinhala/Tamil Literature & Poetry' },
  { code: 'TRAD_GAMES', name: 'Traditional Games (Elle, Ankeliya)' },
  { code: 'SL_FASHION', name: 'Traditional Dress & Fashion (Saree, Sarong)' },
  { code: 'DIASPORA_NET', name: 'Diaspora Community & Networking' },
] as const;

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
  location: {
    cityMaxLength: 100,
    stateMaxLength: 100,
    zipCodeMaxLength: 20,
    countryMaxLength: 100,
  },
  culturalInterests: {
    min: 0, // Optional
    max: 10, // Maximum 10 interests
  },
  languages: {
    min: 1, // At least 1 language required
    max: 5, // Maximum 5 languages
  },
  preferredMetroAreas: {
    min: 0, // Optional (privacy choice)
    max: 10, // Maximum 10 metro areas (matches backend validation)
  },
} as const;
