/**
 * Static Reference Data Repository Implementation
 *
 * Returns static reference data for cultural interests, languages, and proficiency levels.
 * Designed with Promise-based API for future migration to backend endpoints.
 */

import type { IReferenceDataRepository } from '@/domain/repositories/IReferenceDataRepository';
import {
  culturalInterests,
  languages,
  proficiencyLevels,
  type CulturalInterestOption,
  type LanguageOption,
  type ProficiencyLevelOption,
} from '@/infrastructure/data/staticReferenceData';

class StaticReferenceDataRepository implements IReferenceDataRepository {
  /**
   * Get all cultural interests
   */
  async getCulturalInterests(): Promise<CulturalInterestOption[]> {
    // Simulate async operation for consistent API
    return Promise.resolve([...culturalInterests]);
  }

  /**
   * Get all languages
   */
  async getLanguages(): Promise<LanguageOption[]> {
    // Simulate async operation for consistent API
    return Promise.resolve([...languages]);
  }

  /**
   * Get all proficiency levels
   */
  async getProficiencyLevels(): Promise<ProficiencyLevelOption[]> {
    // Simulate async operation for consistent API
    return Promise.resolve([...proficiencyLevels]);
  }

  /**
   * Search cultural interests by query
   * Searches in both label and description fields (case-insensitive)
   */
  async searchCulturalInterests(query: string): Promise<CulturalInterestOption[]> {
    const lowerQuery = query.toLowerCase().trim();

    if (!lowerQuery) {
      return this.getCulturalInterests();
    }

    const filtered = culturalInterests.filter((interest) => {
      const labelMatch = interest.label.toLowerCase().includes(lowerQuery);
      const descMatch = interest.description?.toLowerCase().includes(lowerQuery);
      return labelMatch || descMatch;
    });

    return Promise.resolve([...filtered]);
  }

  /**
   * Search languages by query
   * Searches in code, name, and native name fields (case-insensitive)
   */
  async searchLanguages(query: string): Promise<LanguageOption[]> {
    const lowerQuery = query.toLowerCase().trim();

    if (!lowerQuery) {
      return this.getLanguages();
    }

    const filtered = languages.filter((language) => {
      const codeMatch = language.code.toLowerCase().includes(lowerQuery);
      const nameMatch = language.name.toLowerCase().includes(lowerQuery);
      const nativeNameMatch = language.nativeName?.toLowerCase().includes(lowerQuery);
      return codeMatch || nameMatch || nativeNameMatch;
    });

    return Promise.resolve([...filtered]);
  }
}

/**
 * Singleton instance of the reference data repository
 */
export const referenceDataRepository: IReferenceDataRepository =
  new StaticReferenceDataRepository();
