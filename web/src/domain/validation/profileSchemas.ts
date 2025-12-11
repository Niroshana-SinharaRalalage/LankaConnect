import { z } from 'zod';

/**
 * Validation Schemas for User Profile
 *
 * These schemas enforce the validation rules that match the backend API requirements
 * defined in UsersController.cs and domain entities
 */

/**
 * Proficiency levels for language preferences
 * Matches backend ProficiencyLevel enum
 */
export const proficiencyLevelEnum = z.enum(['Basic', 'Intermediate', 'Advanced', 'Native']);

/**
 * Location Schema
 * All fields optional/nullable (privacy-first design)
 * Matches backend UpdateLocationRequest validation
 */
export const locationSchema = z.object({
  city: z.string().max(100).optional().nullable(),
  state: z.string().max(100).optional().nullable(),
  zipCode: z.string().max(20).optional().nullable(),
  country: z.string().max(100).optional().nullable(),
});

/**
 * Cultural Interests Schema
 * 0-10 items, each max 50 characters
 * Matches backend UpdateCulturalInterestsRequest validation
 */
export const culturalInterestsSchema = z
  .array(z.string().min(1).max(50))
  .max(10, 'Maximum 10 interests allowed')
  .optional();

/**
 * Language Schema
 * Language code: 2-10 characters (supports codes like 'en', 'zh-Hans')
 * Proficiency level: enum of 4 levels
 * Matches backend LanguagePreference entity
 */
export const languageSchema = z.object({
  languageCode: z.string().min(2).max(10),
  proficiencyLevel: proficiencyLevelEnum,
});

/**
 * Languages Schema
 * Array of language preferences
 * Matches backend UpdateLanguagesRequest validation
 */
export const languagesSchema = z.array(languageSchema).optional();

/**
 * Basic Info Schema
 * First/Last name required (1-50 chars)
 * Phone and bio optional
 * Matches backend User entity field constraints
 */
export const basicInfoSchema = z.object({
  firstName: z.string().min(1).max(50),
  lastName: z.string().min(1).max(50),
  phoneNumber: z.string().max(20).optional().nullable(),
  bio: z.string().max(500).optional().nullable(),
});

/**
 * Complete User Profile Schema
 * Combines all profile sections
 * Matches backend UserDto structure
 */
export const userProfileSchema = z.object({
  id: z.string().uuid(),
  email: z.string().email(),
  firstName: z.string().min(1).max(50),
  lastName: z.string().min(1).max(50),
  phoneNumber: z.string().max(20).optional().nullable(),
  bio: z.string().max(500).optional().nullable(),
  profilePhotoUrl: z.string().url().optional().nullable(),
  location: locationSchema.optional().nullable(),
  culturalInterests: culturalInterestsSchema,
  languages: languagesSchema,
});

/**
 * Request Schemas for API Updates
 */

export const updateLocationRequestSchema = locationSchema;

export const updateCulturalInterestsRequestSchema = z.object({
  culturalInterests: z.array(z.string().min(1).max(50)).max(10),
});

export const updateLanguagesRequestSchema = z.object({
  languages: z.array(languageSchema).min(1).max(5),
});

export const updateBasicInfoRequestSchema = basicInfoSchema;

/**
 * Type inference from schemas
 */
export type LocationInput = z.infer<typeof locationSchema>;
export type CulturalInterestsInput = z.infer<typeof culturalInterestsSchema>;
export type LanguageInput = z.infer<typeof languageSchema>;
export type LanguagesInput = z.infer<typeof languagesSchema>;
export type BasicInfoInput = z.infer<typeof basicInfoSchema>;
export type UserProfileInput = z.infer<typeof userProfileSchema>;
export type UpdateLocationRequestInput = z.infer<typeof updateLocationRequestSchema>;
export type UpdateCulturalInterestsRequestInput = z.infer<typeof updateCulturalInterestsRequestSchema>;
export type UpdateLanguagesRequestInput = z.infer<typeof updateLanguagesRequestSchema>;
export type UpdateBasicInfoRequestInput = z.infer<typeof updateBasicInfoRequestSchema>;
