import { describe, it, expect } from 'vitest';
import { updatePreferencesSchema } from '@/presentation/lib/validators/whatsapp.schemas';

describe('updatePreferencesSchema', () => {
  const baseBooleans = {
    eventRegistration: true,
    eventReminder: true,
    eventCancellation: true,
    eventUpdate: true,
    signupCommitment: true,
    refund: true,
    newsletter: true,
    newEvent: true,
    payment: true,
    respectCulturalTiming: true,
  };

  describe('empty-string normalization (fixes PUT /api/whatsapp/preferences 400)', () => {
    it('normalizes empty quietHoursStart string to null', () => {
      const result = updatePreferencesSchema.parse({
        ...baseBooleans,
        quietHoursStart: '',
        quietHoursEnd: '22:00',
        preferredLanguage: 'en',
      });

      expect(result.quietHoursStart).toBeNull();
    });

    it('normalizes empty quietHoursEnd string to null', () => {
      const result = updatePreferencesSchema.parse({
        ...baseBooleans,
        quietHoursStart: '08:00',
        quietHoursEnd: '',
        preferredLanguage: 'en',
      });

      expect(result.quietHoursEnd).toBeNull();
    });

    it('normalizes empty preferredLanguage string to null', () => {
      const result = updatePreferencesSchema.parse({
        ...baseBooleans,
        quietHoursStart: null,
        quietHoursEnd: null,
        preferredLanguage: '',
      });

      expect(result.preferredLanguage).toBeNull();
    });

    it('normalizes all three empty strings to null in a single submission', () => {
      const result = updatePreferencesSchema.parse({
        ...baseBooleans,
        quietHoursStart: '',
        quietHoursEnd: '',
        preferredLanguage: '',
      });

      expect(result.quietHoursStart).toBeNull();
      expect(result.quietHoursEnd).toBeNull();
      expect(result.preferredLanguage).toBeNull();
    });
  });

  describe('valid non-empty values pass through unchanged', () => {
    it('preserves a populated quiet-hours window', () => {
      const result = updatePreferencesSchema.parse({
        ...baseBooleans,
        quietHoursStart: '22:00',
        quietHoursEnd: '07:00',
        preferredLanguage: 'si',
      });

      expect(result.quietHoursStart).toBe('22:00');
      expect(result.quietHoursEnd).toBe('07:00');
      expect(result.preferredLanguage).toBe('si');
    });

    it('accepts explicit null for optional nullable fields', () => {
      const result = updatePreferencesSchema.parse({
        ...baseBooleans,
        quietHoursStart: null,
        quietHoursEnd: null,
        preferredLanguage: null,
      });

      expect(result.quietHoursStart).toBeNull();
      expect(result.quietHoursEnd).toBeNull();
      expect(result.preferredLanguage).toBeNull();
    });

    it('normalizes omitted optional fields to null', () => {
      const result = updatePreferencesSchema.parse(baseBooleans);

      // Omitted (undefined) values are also normalized to null so the API
      // receives consistent JSON regardless of whether the key was absent,
      // explicitly null, or an empty string.
      expect(result.quietHoursStart).toBeNull();
      expect(result.quietHoursEnd).toBeNull();
      expect(result.preferredLanguage).toBeNull();
    });
  });
});
