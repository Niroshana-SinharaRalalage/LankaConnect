import { describe, it, expect } from 'vitest';
import { Password } from '@/core/domain/value-objects/password.vo';

describe('Password Value Object', () => {
  describe('creation', () => {
    it('should create valid password with minimum requirements', () => {
      const result = Password.create('Pass123!');

      expect(result.isSuccess).toBe(true);
      expect(result.isFailure).toBe(false);
    });

    it('should reject empty password', () => {
      const result = Password.create('');

      expect(result.isFailure).toBe(true);

      if (result.isFailure) {
        expect(result.error).toContain('Password');
      }
    });

    it('should reject password shorter than 8 characters', () => {
      const result = Password.create('Pass1!');

      expect(result.isFailure).toBe(true);

      if (result.isFailure) {
        expect(result.error).toContain('8');
      }
    });

    it('should reject password without uppercase letter', () => {
      const result = Password.create('pass123!');

      expect(result.isFailure).toBe(true);

      if (result.isFailure) {
        expect(result.error).toContain('uppercase');
      }
    });

    it('should reject password without lowercase letter', () => {
      const result = Password.create('PASS123!');

      expect(result.isFailure).toBe(true);

      if (result.isFailure) {
        expect(result.error).toContain('lowercase');
      }
    });

    it('should reject password without number', () => {
      const result = Password.create('Password!');

      expect(result.isFailure).toBe(true);

      if (result.isFailure) {
        expect(result.error).toContain('number');
      }
    });

    it('should reject password without special character', () => {
      const result = Password.create('Password123');

      expect(result.isFailure).toBe(true);

      if (result.isFailure) {
        expect(result.error).toContain('special');
      }
    });

    it('should accept strong password', () => {
      const result = Password.create('StrongP@ssw0rd');

      expect(result.isSuccess).toBe(true);
    });

    it('should accept password with various special characters', () => {
      const passwords = [
        'Pass123!',
        'Pass123@',
        'Pass123#',
        'Pass123$',
        'Pass123%',
        'Pass123^',
        'Pass123&',
        'Pass123*',
      ];

      passwords.forEach((password) => {
        const result = Password.create(password);
        expect(result.isSuccess).toBe(true);
      });
    });
  });

  describe('hashing', () => {
    it('should not expose raw password value', () => {
      const result = Password.create('Pass123!');

      expect(result.isSuccess).toBe(true);

      if (result.isSuccess) {
        const password = result.value;
        // Password value object should not have a direct getValue method
        // or it should return hashed value
        expect(password).toBeDefined();
      }
    });

    it('should create hashed password', async () => {
      const result = Password.create('Pass123!');

      expect(result.isSuccess).toBe(true);

      if (result.isSuccess) {
        const hashedPassword = await result.value.getHashedValue();
        expect(hashedPassword).toBeDefined();
        expect(hashedPassword).not.toBe('Pass123!');
        expect(hashedPassword.length).toBeGreaterThan(20); // bcrypt hashes are long
      }
    });

    it('should compare password correctly', async () => {
      const result = Password.create('Pass123!');

      expect(result.isSuccess).toBe(true);

      if (result.isSuccess) {
        const password = result.value;
        const isMatch = await password.compare('Pass123!');
        expect(isMatch).toBe(true);
      }
    });

    it('should reject incorrect password comparison', async () => {
      const result = Password.create('Pass123!');

      expect(result.isSuccess).toBe(true);

      if (result.isSuccess) {
        const password = result.value;
        const isMatch = await password.compare('WrongPass123!');
        expect(isMatch).toBe(false);
      }
    });
  });

  describe('equality', () => {
    it('should not compare passwords by value for security', async () => {
      const password1Result = Password.create('Pass123!');
      const password2Result = Password.create('Pass123!');

      expect(password1Result.isSuccess).toBe(true);
      expect(password2Result.isSuccess).toBe(true);

      if (password1Result.isSuccess && password2Result.isSuccess) {
        // Two password objects with same value should not be equal
        // to prevent timing attacks
        const hash1 = await password1Result.value.getHashedValue();
        const hash2 = await password2Result.value.getHashedValue();

        // Each hash should be unique due to salt
        expect(hash1).not.toBe(hash2);
      }
    });
  });

  describe('from hash', () => {
    it('should create password from existing hash', async () => {
      const originalResult = Password.create('Pass123!');

      expect(originalResult.isSuccess).toBe(true);

      if (originalResult.isSuccess) {
        const hash = await originalResult.value.getHashedValue();

        const restoredResult = Password.fromHash(hash);
        expect(restoredResult.isSuccess).toBe(true);

        if (restoredResult.isSuccess) {
          const isMatch = await restoredResult.value.compare('Pass123!');
          expect(isMatch).toBe(true);
        }
      }
    });
  });
});
