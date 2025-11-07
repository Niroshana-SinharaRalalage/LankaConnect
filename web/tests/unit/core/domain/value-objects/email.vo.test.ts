import { describe, it, expect } from 'vitest';
import { Email } from '@/core/domain/value-objects/email.vo';

describe('Email Value Object', () => {
  describe('creation', () => {
    it('should create valid email', () => {
      const result = Email.create('test@example.com');

      expect(result.isSuccess).toBe(true);
      expect(result.isFailure).toBe(false);

      if (result.isSuccess) {
        expect(result.value.getValue()).toBe('test@example.com');
      }
    });

    it('should reject empty email', () => {
      const result = Email.create('');

      expect(result.isFailure).toBe(true);
      expect(result.isSuccess).toBe(false);

      if (result.isFailure) {
        expect(result.error).toContain('Email');
      }
    });

    it('should reject email without @ symbol', () => {
      const result = Email.create('invalid-email');

      expect(result.isFailure).toBe(true);

      if (result.isFailure) {
        expect(result.error).toContain('valid email');
      }
    });

    it('should reject email without domain', () => {
      const result = Email.create('test@');

      expect(result.isFailure).toBe(true);
    });

    it('should reject email without local part', () => {
      const result = Email.create('@example.com');

      expect(result.isFailure).toBe(true);
    });

    it('should accept valid email with subdomain', () => {
      const result = Email.create('test@mail.example.com');

      expect(result.isSuccess).toBe(true);
    });

    it('should accept valid email with plus sign', () => {
      const result = Email.create('test+tag@example.com');

      expect(result.isSuccess).toBe(true);
    });

    it('should normalize email to lowercase', () => {
      const result = Email.create('Test@Example.COM');

      expect(result.isSuccess).toBe(true);

      if (result.isSuccess) {
        expect(result.value.getValue()).toBe('test@example.com');
      }
    });
  });

  describe('equality', () => {
    it('should consider two emails with same value as equal', () => {
      const email1Result = Email.create('test@example.com');
      const email2Result = Email.create('test@example.com');

      expect(email1Result.isSuccess).toBe(true);
      expect(email2Result.isSuccess).toBe(true);

      if (email1Result.isSuccess && email2Result.isSuccess) {
        expect(email1Result.value.equals(email2Result.value)).toBe(true);
      }
    });

    it('should consider two emails with different values as not equal', () => {
      const email1Result = Email.create('test1@example.com');
      const email2Result = Email.create('test2@example.com');

      expect(email1Result.isSuccess).toBe(true);
      expect(email2Result.isSuccess).toBe(true);

      if (email1Result.isSuccess && email2Result.isSuccess) {
        expect(email1Result.value.equals(email2Result.value)).toBe(false);
      }
    });

    it('should handle case-insensitive comparison', () => {
      const email1Result = Email.create('Test@Example.com');
      const email2Result = Email.create('test@example.com');

      expect(email1Result.isSuccess).toBe(true);
      expect(email2Result.isSuccess).toBe(true);

      if (email1Result.isSuccess && email2Result.isSuccess) {
        expect(email1Result.value.equals(email2Result.value)).toBe(true);
      }
    });
  });
});
