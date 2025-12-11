import { describe, it, expect } from 'vitest';
import {
  locationSchema,
  culturalInterestsSchema,
  languageSchema,
  languagesSchema,
  basicInfoSchema,
  userProfileSchema,
} from '@/domain/validation/profileSchemas';
import type { ZodError } from 'zod';

describe('Profile Validation Schemas', () => {
  describe('locationSchema', () => {
    it('should accept valid location data with all fields', () => {
      const valid = {
        city: 'New York',
        state: 'NY',
        zipCode: '10001',
        country: 'USA',
      };
      expect(locationSchema.parse(valid)).toEqual(valid);
    });

    it('should accept location with only city', () => {
      const valid = { city: 'New York', state: null, zipCode: null, country: null };
      expect(locationSchema.parse(valid)).toEqual(valid);
    });

    it('should accept location with undefined fields', () => {
      const valid = { city: 'New York' };
      expect(locationSchema.parse(valid)).toEqual(valid);
    });

    it('should accept empty location object', () => {
      const valid = {};
      expect(locationSchema.parse(valid)).toEqual(valid);
    });

    it('should reject city longer than 100 characters', () => {
      const invalid = { city: 'A'.repeat(101) };
      expect(() => locationSchema.parse(invalid)).toThrow();
    });

    it('should reject state longer than 100 characters', () => {
      const invalid = { state: 'A'.repeat(101) };
      expect(() => locationSchema.parse(invalid)).toThrow();
    });

    it('should reject zipCode longer than 20 characters', () => {
      const invalid = { zipCode: '1'.repeat(21) };
      expect(() => locationSchema.parse(invalid)).toThrow();
    });

    it('should reject country longer than 100 characters', () => {
      const invalid = { country: 'A'.repeat(101) };
      expect(() => locationSchema.parse(invalid)).toThrow();
    });

    it('should accept null values for all fields', () => {
      const valid = { city: null, state: null, zipCode: null, country: null };
      expect(locationSchema.parse(valid)).toEqual(valid);
    });

    it('should reject non-string values', () => {
      const invalid = { city: 123 };
      expect(() => locationSchema.parse(invalid)).toThrow();
    });
  });

  describe('culturalInterestsSchema', () => {
    it('should accept empty array', () => {
      const valid: string[] = [];
      expect(culturalInterestsSchema.parse(valid)).toEqual(valid);
    });

    it('should accept valid array of interests', () => {
      const valid = ['Art', 'Music', 'Dance'];
      expect(culturalInterestsSchema.parse(valid)).toEqual(valid);
    });

    it('should accept exactly 10 interests (maximum)', () => {
      const valid = Array(10).fill('Interest');
      expect(culturalInterestsSchema.parse(valid)).toEqual(valid);
    });

    it('should reject more than 10 interests', () => {
      const invalid = Array(11).fill('Interest');
      expect(() => culturalInterestsSchema.parse(invalid)).toThrow('Maximum 10 interests allowed');
    });

    it('should reject interest longer than 50 characters', () => {
      const invalid = ['A'.repeat(51)];
      expect(() => culturalInterestsSchema.parse(invalid)).toThrow();
    });

    it('should accept undefined', () => {
      expect(culturalInterestsSchema.parse(undefined)).toBeUndefined();
    });

    it('should reject null', () => {
      expect(() => culturalInterestsSchema.parse(null)).toThrow();
    });

    it('should reject non-string elements', () => {
      const invalid = [123, 'Art'];
      expect(() => culturalInterestsSchema.parse(invalid)).toThrow();
    });

    it('should reject empty strings', () => {
      const invalid = [''];
      expect(() => culturalInterestsSchema.parse(invalid)).toThrow();
    });
  });

  describe('languageSchema', () => {
    it('should accept valid language with all proficiency levels', () => {
      const proficiencyLevels = ['Basic', 'Intermediate', 'Advanced', 'Native'] as const;

      proficiencyLevels.forEach((level) => {
        const valid = { languageCode: 'en', proficiencyLevel: level };
        expect(languageSchema.parse(valid)).toEqual(valid);
      });
    });

    it('should accept two-letter language code', () => {
      const valid = { languageCode: 'en', proficiencyLevel: 'Native' };
      expect(languageSchema.parse(valid)).toEqual(valid);
    });

    it('should accept longer language codes (e.g., zh-Hans)', () => {
      const valid = { languageCode: 'zh-Hans', proficiencyLevel: 'Advanced' };
      expect(languageSchema.parse(valid)).toEqual(valid);
    });

    it('should reject language code with 1 character', () => {
      const invalid = { languageCode: 'e', proficiencyLevel: 'Native' };
      expect(() => languageSchema.parse(invalid)).toThrow();
    });

    it('should reject language code longer than 10 characters', () => {
      const invalid = { languageCode: 'a'.repeat(11), proficiencyLevel: 'Native' };
      expect(() => languageSchema.parse(invalid)).toThrow();
    });

    it('should reject invalid proficiency level', () => {
      const invalid = { languageCode: 'en', proficiencyLevel: 'Expert' };
      expect(() => languageSchema.parse(invalid)).toThrow();
    });

    it('should reject missing languageCode', () => {
      const invalid = { proficiencyLevel: 'Native' };
      expect(() => languageSchema.parse(invalid)).toThrow();
    });

    it('should reject missing proficiencyLevel', () => {
      const invalid = { languageCode: 'en' };
      expect(() => languageSchema.parse(invalid)).toThrow();
    });
  });

  describe('languagesSchema', () => {
    it('should accept empty array', () => {
      const valid: any[] = [];
      expect(languagesSchema.parse(valid)).toEqual(valid);
    });

    it('should accept single language', () => {
      const valid = [{ languageCode: 'en', proficiencyLevel: 'Native' }];
      expect(languagesSchema.parse(valid)).toEqual(valid);
    });

    it('should accept multiple languages', () => {
      const valid = [
        { languageCode: 'en', proficiencyLevel: 'Native' },
        { languageCode: 'si', proficiencyLevel: 'Advanced' },
        { languageCode: 'ta', proficiencyLevel: 'Intermediate' },
      ];
      expect(languagesSchema.parse(valid)).toEqual(valid);
    });

    it('should accept undefined', () => {
      expect(languagesSchema.parse(undefined)).toBeUndefined();
    });

    it('should reject invalid language objects', () => {
      const invalid = [{ languageCode: 'e', proficiencyLevel: 'Native' }];
      expect(() => languagesSchema.parse(invalid)).toThrow();
    });
  });

  describe('basicInfoSchema', () => {
    it('should accept valid basic info with all fields', () => {
      const valid = {
        firstName: 'John',
        lastName: 'Doe',
        phoneNumber: '+1234567890',
        bio: 'Software developer passionate about technology',
      };
      expect(basicInfoSchema.parse(valid)).toEqual(valid);
    });

    it('should accept basic info with only required fields', () => {
      const valid = {
        firstName: 'John',
        lastName: 'Doe',
      };
      expect(basicInfoSchema.parse(valid)).toEqual(valid);
    });

    it('should reject firstName longer than 50 characters', () => {
      const invalid = {
        firstName: 'A'.repeat(51),
        lastName: 'Doe',
      };
      expect(() => basicInfoSchema.parse(invalid)).toThrow();
    });

    it('should reject lastName longer than 50 characters', () => {
      const invalid = {
        firstName: 'John',
        lastName: 'A'.repeat(51),
      };
      expect(() => basicInfoSchema.parse(invalid)).toThrow();
    });

    it('should reject empty firstName', () => {
      const invalid = {
        firstName: '',
        lastName: 'Doe',
      };
      expect(() => basicInfoSchema.parse(invalid)).toThrow();
    });

    it('should reject empty lastName', () => {
      const invalid = {
        firstName: 'John',
        lastName: '',
      };
      expect(() => basicInfoSchema.parse(invalid)).toThrow();
    });

    it('should reject phoneNumber longer than 20 characters', () => {
      const invalid = {
        firstName: 'John',
        lastName: 'Doe',
        phoneNumber: '1'.repeat(21),
      };
      expect(() => basicInfoSchema.parse(invalid)).toThrow();
    });

    it('should reject bio longer than 500 characters', () => {
      const invalid = {
        firstName: 'John',
        lastName: 'Doe',
        bio: 'A'.repeat(501),
      };
      expect(() => basicInfoSchema.parse(invalid)).toThrow();
    });

    it('should accept null optional fields', () => {
      const valid = {
        firstName: 'John',
        lastName: 'Doe',
        phoneNumber: null,
        bio: null,
      };
      expect(basicInfoSchema.parse(valid)).toEqual(valid);
    });

    it('should accept undefined optional fields', () => {
      const valid = {
        firstName: 'John',
        lastName: 'Doe',
      };
      expect(basicInfoSchema.parse(valid)).toEqual(valid);
    });
  });

  describe('userProfileSchema', () => {
    it('should accept complete valid profile', () => {
      const valid = {
        id: '123e4567-e89b-12d3-a456-426614174000',
        email: 'test@example.com',
        firstName: 'John',
        lastName: 'Doe',
        phoneNumber: '+1234567890',
        bio: 'Software developer',
        profilePhotoUrl: 'https://example.com/photo.jpg',
        location: {
          city: 'New York',
          state: 'NY',
          zipCode: '10001',
          country: 'USA',
        },
        culturalInterests: ['Art', 'Music'],
        languages: [
          { languageCode: 'en', proficiencyLevel: 'Native' },
          { languageCode: 'si', proficiencyLevel: 'Intermediate' },
        ],
      };
      expect(userProfileSchema.parse(valid)).toEqual(valid);
    });

    it('should accept minimal profile with only required fields', () => {
      const valid = {
        id: '123e4567-e89b-12d3-a456-426614174000',
        email: 'test@example.com',
        firstName: 'John',
        lastName: 'Doe',
      };
      expect(userProfileSchema.parse(valid)).toEqual(valid);
    });

    it('should reject invalid UUID format', () => {
      const invalid = {
        id: 'not-a-uuid',
        email: 'test@example.com',
        firstName: 'John',
        lastName: 'Doe',
      };
      expect(() => userProfileSchema.parse(invalid)).toThrow();
    });

    it('should reject invalid email format', () => {
      const invalid = {
        id: '123e4567-e89b-12d3-a456-426614174000',
        email: 'not-an-email',
        firstName: 'John',
        lastName: 'Doe',
      };
      expect(() => userProfileSchema.parse(invalid)).toThrow();
    });

    it('should reject invalid nested location', () => {
      const invalid = {
        id: '123e4567-e89b-12d3-a456-426614174000',
        email: 'test@example.com',
        firstName: 'John',
        lastName: 'Doe',
        location: {
          city: 'A'.repeat(101), // Too long
        },
      };
      expect(() => userProfileSchema.parse(invalid)).toThrow();
    });

    it('should reject invalid nested cultural interests', () => {
      const invalid = {
        id: '123e4567-e89b-12d3-a456-426614174000',
        email: 'test@example.com',
        firstName: 'John',
        lastName: 'Doe',
        culturalInterests: Array(11).fill('Interest'), // Too many
      };
      expect(() => userProfileSchema.parse(invalid)).toThrow();
    });

    it('should reject invalid nested languages', () => {
      const invalid = {
        id: '123e4567-e89b-12d3-a456-426614174000',
        email: 'test@example.com',
        firstName: 'John',
        lastName: 'Doe',
        languages: [
          { languageCode: 'e', proficiencyLevel: 'Native' }, // Code too short
        ],
      };
      expect(() => userProfileSchema.parse(invalid)).toThrow();
    });
  });
});
