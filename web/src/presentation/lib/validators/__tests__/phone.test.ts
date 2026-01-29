/**
 * Phone Number Validation Tests
 * GitHub Issue #30: Test-Driven Development for phone validation
 *
 * These tests verify the phone validation utility correctly validates
 * phone numbers with various formats and rejects invalid inputs.
 */

import { validatePhoneNumber, isValidPhoneNumber, phoneNumberSchema } from '../phone';

describe('validatePhoneNumber', () => {
  describe('valid phone numbers', () => {
    test.each([
      ['+1-234-567-8901', 'US format with dashes'],
      ['(123) 456-7890', 'US format with parentheses'],
      ['+94 77 123 4567', 'Sri Lankan format with spaces'],
      ['1234567', 'minimum 7 digits'],
      ['123456789012345', 'maximum 15 digits'],
      ['+1 (555) 123-4567', 'mixed formatting'],
      ['0771234567', 'Sri Lankan local format'],
      ['+94771234567', 'Sri Lankan international format'],
      ['555-1234', '7 digits with dash'],
      ['5551234567', '10 digits no formatting'],
    ])('should accept %s (%s)', (phone) => {
      const result = validatePhoneNumber(phone);
      expect(result.isValid).toBe(true);
      expect(result.error).toBeUndefined();
    });
  });

  describe('invalid phone numbers - empty or whitespace', () => {
    test('should reject empty string', () => {
      const result = validatePhoneNumber('');
      expect(result.isValid).toBe(false);
      expect(result.error).toBe('Phone number is required');
    });

    test('should reject null/undefined input', () => {
      const result = validatePhoneNumber(null as unknown as string);
      expect(result.isValid).toBe(false);
      expect(result.error).toBe('Phone number is required');
    });

    test('should reject only whitespace', () => {
      const result = validatePhoneNumber('   ');
      expect(result.isValid).toBe(false);
      expect(result.error).toBe('Phone number is required');
    });
  });

  describe('invalid phone numbers - too few digits', () => {
    test.each([
      ['123456', '6 digits'],
      ['12345', '5 digits'],
      ['()', 'no digits, only parentheses'],
      ['---', 'no digits, only dashes'],
      ['+', 'only plus sign'],
      ['+ - ()', 'formatting characters only'],
    ])('should reject %s (%s)', (phone, description) => {
      const result = validatePhoneNumber(phone);
      expect(result.isValid).toBe(false);
      expect(result.error).toBe('Phone number must have at least 7 digits');
    });
  });

  describe('invalid phone numbers - too many digits', () => {
    test('should reject 16 digits', () => {
      const result = validatePhoneNumber('1234567890123456');
      expect(result.isValid).toBe(false);
      expect(result.error).toBe('Phone number cannot exceed 15 digits');
    });

    test('should reject 20 digits', () => {
      const result = validatePhoneNumber('12345678901234567890');
      expect(result.isValid).toBe(false);
      expect(result.error).toBe('Phone number cannot exceed 15 digits');
    });
  });

  describe('invalid phone numbers - invalid characters', () => {
    test.each([
      ['abc1234567', 'contains letters'],
      ['123-abc-4567', 'letters in middle'],
      ['123.456.7890', 'contains dots'],
      ['123#456#7890', 'contains hash'],
      ['123@456@7890', 'contains at sign'],
    ])('should reject %s (%s)', (phone) => {
      const result = validatePhoneNumber(phone);
      expect(result.isValid).toBe(false);
      expect(result.error).toBe('Phone number contains invalid characters');
    });
  });

  describe('invalid phone numbers - plus sign position', () => {
    test('should reject plus sign in middle', () => {
      const result = validatePhoneNumber('123+4567890');
      expect(result.isValid).toBe(false);
      expect(result.error).toBe('Plus sign (+) must be at the start of phone number');
    });

    test('should reject plus sign at end', () => {
      const result = validatePhoneNumber('1234567890+');
      expect(result.isValid).toBe(false);
      expect(result.error).toBe('Plus sign (+) must be at the start of phone number');
    });

    test('should reject multiple plus signs', () => {
      const result = validatePhoneNumber('++1234567890');
      expect(result.isValid).toBe(false);
      expect(result.error).toBe('Phone number can only contain one plus sign (+)');
    });
  });
});

describe('isValidPhoneNumber', () => {
  test('should return true for valid phone', () => {
    expect(isValidPhoneNumber('+1-234-567-8901')).toBe(true);
  });

  test('should return false for invalid phone', () => {
    expect(isValidPhoneNumber('()')).toBe(false);
  });

  test('should return false for empty string', () => {
    expect(isValidPhoneNumber('')).toBe(false);
  });
});

describe('phoneNumberSchema (Zod)', () => {
  test('should parse valid phone number', () => {
    const result = phoneNumberSchema.safeParse('+1-234-567-8901');
    expect(result.success).toBe(true);
  });

  test('should fail for invalid phone number', () => {
    const result = phoneNumberSchema.safeParse('()');
    expect(result.success).toBe(false);
  });

  test('should fail for empty string', () => {
    const result = phoneNumberSchema.safeParse('');
    expect(result.success).toBe(false);
  });

  test('should provide error message for too few digits', () => {
    const result = phoneNumberSchema.safeParse('123456');
    expect(result.success).toBe(false);
    if (!result.success) {
      expect(result.error.issues[0].message).toContain('at least 7 digits');
    }
  });
});
