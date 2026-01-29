/**
 * Phone Number Validation Utility
 * GitHub Issue #30: Centralized phone validation
 *
 * Requirements:
 * - Minimum 7 digits (shortest valid phone)
 * - Maximum 15 digits (E.164 standard)
 * - Allow formatting: +, spaces, hyphens, parentheses
 * - Plus sign only allowed at start
 * - Only one plus sign allowed
 */

import { z } from 'zod';

export interface PhoneValidationResult {
  isValid: boolean;
  error?: string;
}

/**
 * Validates phone number format
 * Allows: +1-234-567-8901, (123) 456-7890, +94 77 123 4567
 * Rejects: empty, only formatting chars, too few/many digits
 *
 * @param phone - The phone number string to validate
 * @returns PhoneValidationResult with isValid and optional error message
 */
export function validatePhoneNumber(phone: string): PhoneValidationResult {
  // Handle null/undefined/empty
  if (!phone || !phone.trim()) {
    return { isValid: false, error: 'Phone number is required' };
  }

  const trimmed = phone.trim();

  // Check only one + at most (must check before other validations)
  const plusCount = (trimmed.match(/\+/g) || []).length;
  if (plusCount > 1) {
    return { isValid: false, error: 'Phone number can only contain one plus sign (+)' };
  }

  // Check + is only at start (if present)
  if (trimmed.includes('+') && !trimmed.startsWith('+')) {
    return { isValid: false, error: 'Plus sign (+) must be at the start of phone number' };
  }

  // Check for valid characters only (digits, +, spaces, hyphens, parentheses)
  const validCharsPattern = /^[\d\s\-+()]+$/;
  if (!validCharsPattern.test(trimmed)) {
    return { isValid: false, error: 'Phone number contains invalid characters' };
  }

  // Extract only digits (ignore formatting)
  const digitsOnly = trimmed.replace(/[^\d]/g, '');

  // Check digit count (7-15 digits allowed)
  if (digitsOnly.length < 7) {
    return { isValid: false, error: 'Phone number must have at least 7 digits' };
  }

  if (digitsOnly.length > 15) {
    return { isValid: false, error: 'Phone number cannot exceed 15 digits' };
  }

  return { isValid: true };
}

/**
 * Quick validation check - returns true/false
 *
 * @param phone - The phone number string to validate
 * @returns boolean indicating if phone is valid
 */
export function isValidPhoneNumber(phone: string): boolean {
  return validatePhoneNumber(phone).isValid;
}

/**
 * Zod schema for phone number validation
 * Can be used in form schemas for consistent validation
 *
 * @example
 * const formSchema = z.object({
 *   phoneNumber: phoneNumberSchema,
 * });
 */
export const phoneNumberSchema = z
  .string()
  .min(1, 'Phone number is required')
  .refine(
    (val) => {
      // Check only one + at most
      const plusCount = (val.match(/\+/g) || []).length;
      return plusCount <= 1;
    },
    { message: 'Phone number can only contain one plus sign (+)' }
  )
  .refine(
    (val) => {
      // Check + is only at start
      const trimmed = val.trim();
      return !trimmed.includes('+') || trimmed.startsWith('+');
    },
    { message: 'Plus sign (+) must be at the start of phone number' }
  )
  .refine(
    (val) => /^[\d\s\-+()]+$/.test(val.trim()),
    { message: 'Phone number contains invalid characters' }
  )
  .refine(
    (val) => {
      const digitsOnly = val.replace(/[^\d]/g, '');
      return digitsOnly.length >= 7;
    },
    { message: 'Phone number must have at least 7 digits' }
  )
  .refine(
    (val) => {
      const digitsOnly = val.replace(/[^\d]/g, '');
      return digitsOnly.length <= 15;
    },
    { message: 'Phone number cannot exceed 15 digits' }
  );
