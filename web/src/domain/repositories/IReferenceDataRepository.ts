/**
 * Reference Data Repository Interface
 *
 * Abstraction for retrieving cultural interests, languages, and proficiency levels.
 * Currently implemented with static data, but designed for future API migration.
 */

import type {
  CulturalInterestOption,
  LanguageOption,
  ProficiencyLevelOption,
} from '@/infrastructure/data/staticReferenceData';

export interface IReferenceDataRepository {
  /**
   * Get all available cultural interests
   * @returns Promise resolving to array of cultural interest options
   */
  getCulturalInterests(): Promise<CulturalInterestOption[]>;

  /**
   * Get all available languages
   * @returns Promise resolving to array of language options
   */
  getLanguages(): Promise<LanguageOption[]>;

  /**
   * Get all proficiency levels
   * @returns Promise resolving to array of proficiency level options
   */
  getProficiencyLevels(): Promise<ProficiencyLevelOption[]>;

  /**
   * Search cultural interests by query string
   * @param query Search query
   * @returns Promise resolving to filtered cultural interests
   */
  searchCulturalInterests(query: string): Promise<CulturalInterestOption[]>;

  /**
   * Search languages by query string
   * @param query Search query
   * @returns Promise resolving to filtered languages
   */
  searchLanguages(query: string): Promise<LanguageOption[]>;
}
