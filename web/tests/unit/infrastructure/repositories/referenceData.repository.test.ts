import { describe, it, expect } from 'vitest';
import { referenceDataRepository } from '@/infrastructure/repositories/referenceData.repository';

describe('ReferenceDataRepository', () => {
  describe('getCulturalInterests', () => {
    it('should return array of cultural interests', async () => {
      const interests = await referenceDataRepository.getCulturalInterests();

      expect(interests).toBeInstanceOf(Array);
      expect(interests.length).toBeGreaterThan(0);
    });

    it('should return interests with required fields', async () => {
      const interests = await referenceDataRepository.getCulturalInterests();

      interests.forEach((interest) => {
        expect(interest).toHaveProperty('id');
        expect(interest).toHaveProperty('label');
        expect(typeof interest.id).toBe('string');
        expect(typeof interest.label).toBe('string');
      });
    });

    it('should include specific cultural interests', async () => {
      const interests = await referenceDataRepository.getCulturalInterests();
      const labels = interests.map((i) => i.label);

      expect(labels).toContain('Art & Painting');
      expect(labels).toContain('Music');
      expect(labels).toContain('Dance');
      expect(labels).toContain('Cuisine');
    });

    it('should return immutable copy (not original array)', async () => {
      const interests1 = await referenceDataRepository.getCulturalInterests();
      const interests2 = await referenceDataRepository.getCulturalInterests();

      expect(interests1).not.toBe(interests2); // Different references
      expect(interests1).toEqual(interests2); // Same content
    });
  });

  describe('getLanguages', () => {
    it('should return array of languages', async () => {
      const languages = await referenceDataRepository.getLanguages();

      expect(languages).toBeInstanceOf(Array);
      expect(languages.length).toBeGreaterThan(0);
    });

    it('should return languages with required fields', async () => {
      const languages = await referenceDataRepository.getLanguages();

      languages.forEach((language) => {
        expect(language).toHaveProperty('code');
        expect(language).toHaveProperty('name');
        expect(typeof language.code).toBe('string');
        expect(typeof language.name).toBe('string');
      });
    });

    it('should include Sri Lankan languages', async () => {
      const languages = await referenceDataRepository.getLanguages();
      const codes = languages.map((l) => l.code);

      expect(codes).toContain('si'); // Sinhala
      expect(codes).toContain('ta'); // Tamil
      expect(codes).toContain('en'); // English
    });

    it('should include native names where applicable', async () => {
      const languages = await referenceDataRepository.getLanguages();
      const sinhala = languages.find((l) => l.code === 'si');
      const tamil = languages.find((l) => l.code === 'ta');

      expect(sinhala?.nativeName).toBe('සිංහල');
      expect(tamil?.nativeName).toBe('தமிழ்');
    });

    it('should return immutable copy (not original array)', async () => {
      const languages1 = await referenceDataRepository.getLanguages();
      const languages2 = await referenceDataRepository.getLanguages();

      expect(languages1).not.toBe(languages2); // Different references
      expect(languages1).toEqual(languages2); // Same content
    });
  });

  describe('getProficiencyLevels', () => {
    it('should return array of proficiency levels', async () => {
      const levels = await referenceDataRepository.getProficiencyLevels();

      expect(levels).toBeInstanceOf(Array);
      expect(levels.length).toBe(4); // Basic, Intermediate, Advanced, Native
    });

    it('should return proficiency levels with required fields', async () => {
      const levels = await referenceDataRepository.getProficiencyLevels();

      levels.forEach((level) => {
        expect(level).toHaveProperty('value');
        expect(level).toHaveProperty('label');
        expect(level).toHaveProperty('description');
        expect(typeof level.value).toBe('string');
        expect(typeof level.label).toBe('string');
        expect(typeof level.description).toBe('string');
      });
    });

    it('should include all four proficiency levels', async () => {
      const levels = await referenceDataRepository.getProficiencyLevels();
      const values = levels.map((l) => l.value);

      expect(values).toContain('Basic');
      expect(values).toContain('Intermediate');
      expect(values).toContain('Advanced');
      expect(values).toContain('Native');
    });

    it('should return levels in ascending order', async () => {
      const levels = await referenceDataRepository.getProficiencyLevels();
      const values = levels.map((l) => l.value);

      expect(values).toEqual(['Basic', 'Intermediate', 'Advanced', 'Native']);
    });
  });

  describe('searchCulturalInterests', () => {
    it('should return all interests when query is empty', async () => {
      const allInterests = await referenceDataRepository.getCulturalInterests();
      const searchResults = await referenceDataRepository.searchCulturalInterests('');

      expect(searchResults.length).toBe(allInterests.length);
    });

    it('should return all interests when query is whitespace', async () => {
      const allInterests = await referenceDataRepository.getCulturalInterests();
      const searchResults = await referenceDataRepository.searchCulturalInterests('   ');

      expect(searchResults.length).toBe(allInterests.length);
    });

    it('should filter interests by label (case-insensitive)', async () => {
      const results = await referenceDataRepository.searchCulturalInterests('music');

      expect(results.length).toBeGreaterThan(0);
      expect(results.some((r) => r.label.toLowerCase().includes('music'))).toBe(true);
    });

    it('should filter interests by description', async () => {
      const results = await referenceDataRepository.searchCulturalInterests('traditional');

      expect(results.length).toBeGreaterThan(0);
      expect(
        results.some((r) => r.description?.toLowerCase().includes('traditional'))
      ).toBe(true);
    });

    it('should be case-insensitive', async () => {
      const lowerResults = await referenceDataRepository.searchCulturalInterests('art');
      const upperResults = await referenceDataRepository.searchCulturalInterests('ART');
      const mixedResults = await referenceDataRepository.searchCulturalInterests('Art');

      expect(lowerResults).toEqual(upperResults);
      expect(lowerResults).toEqual(mixedResults);
    });

    it('should return empty array when no matches', async () => {
      const results = await referenceDataRepository.searchCulturalInterests(
        'zzznonexistentzzzz'
      );

      expect(results).toEqual([]);
    });

    it('should trim whitespace from query', async () => {
      const trimmedResults = await referenceDataRepository.searchCulturalInterests('music');
      const untrimmedResults = await referenceDataRepository.searchCulturalInterests(
        '  music  '
      );

      expect(untrimmedResults).toEqual(trimmedResults);
    });

    it('should return partial matches', async () => {
      const results = await referenceDataRepository.searchCulturalInterests('art');

      const labels = results.map((r) => r.label);
      expect(labels).toContain('Art & Painting');
      expect(labels).toContain('Martial Arts');
    });
  });

  describe('searchLanguages', () => {
    it('should return all languages when query is empty', async () => {
      const allLanguages = await referenceDataRepository.getLanguages();
      const searchResults = await referenceDataRepository.searchLanguages('');

      expect(searchResults.length).toBe(allLanguages.length);
    });

    it('should return all languages when query is whitespace', async () => {
      const allLanguages = await referenceDataRepository.getLanguages();
      const searchResults = await referenceDataRepository.searchLanguages('   ');

      expect(searchResults.length).toBe(allLanguages.length);
    });

    it('should filter languages by code', async () => {
      const results = await referenceDataRepository.searchLanguages('en');

      expect(results.length).toBeGreaterThan(0);
      expect(results.some((r) => r.code === 'en')).toBe(true);
    });

    it('should filter languages by name', async () => {
      const results = await referenceDataRepository.searchLanguages('english');

      expect(results.length).toBeGreaterThan(0);
      expect(results.some((r) => r.name.toLowerCase() === 'english')).toBe(true);
    });

    it('should filter languages by native name', async () => {
      const results = await referenceDataRepository.searchLanguages('සිංහල');

      expect(results.length).toBeGreaterThan(0);
      expect(results.some((r) => r.nativeName === 'සිංහල')).toBe(true);
    });

    it('should be case-insensitive', async () => {
      const lowerResults = await referenceDataRepository.searchLanguages('english');
      const upperResults = await referenceDataRepository.searchLanguages('ENGLISH');
      const mixedResults = await referenceDataRepository.searchLanguages('English');

      expect(lowerResults).toEqual(upperResults);
      expect(lowerResults).toEqual(mixedResults);
    });

    it('should return empty array when no matches', async () => {
      const results = await referenceDataRepository.searchLanguages('zzznonexistentzzzz');

      expect(results).toEqual([]);
    });

    it('should trim whitespace from query', async () => {
      const trimmedResults = await referenceDataRepository.searchLanguages('en');
      const untrimmedResults = await referenceDataRepository.searchLanguages('  en  ');

      expect(untrimmedResults).toEqual(trimmedResults);
    });

    it('should support multi-part language codes', async () => {
      const results = await referenceDataRepository.searchLanguages('zh-Hans');

      expect(results.length).toBeGreaterThan(0);
      expect(results.some((r) => r.code === 'zh-Hans')).toBe(true);
    });
  });
});
