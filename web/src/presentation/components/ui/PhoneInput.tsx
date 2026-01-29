/**
 * PhoneInput Component
 * GitHub Issue #30: Restricted phone input that prevents invalid characters
 *
 * Features:
 * - Only allows digits, +, -, spaces, parentheses
 * - + only allowed at the start
 * - Maximum 15 digits (E.164 standard)
 * - Maximum 25 total characters (including formatting)
 * - Prevents typing invalid characters (not just validation)
 */

import * as React from 'react';
import { cn } from '@/presentation/lib/utils';

export interface PhoneInputProps
  extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type' | 'onChange'> {
  error?: boolean;
  value: string;
  onChange: (value: string) => void;
  maxDigits?: number;
  maxLength?: number;
}

// Characters allowed in phone number: digits, +, -, space, parentheses
const ALLOWED_CHARS = /^[\d\s\-+()]*$/;
const MAX_DIGITS_DEFAULT = 15; // E.164 standard
const MAX_LENGTH_DEFAULT = 25; // Total characters including formatting

/**
 * PhoneInput - Restricted input for phone numbers
 *
 * Prevents users from typing invalid characters (letters, special chars)
 * Only allows: 0-9, +, -, space, ( )
 *
 * @example
 * <PhoneInput
 *   value={phoneNumber}
 *   onChange={setPhoneNumber}
 *   placeholder="+1-234-567-8901"
 *   error={!!errors.phoneNumber}
 * />
 */
const PhoneInput = React.forwardRef<HTMLInputElement, PhoneInputProps>(
  (
    {
      className,
      error,
      value,
      onChange,
      maxDigits = MAX_DIGITS_DEFAULT,
      maxLength = MAX_LENGTH_DEFAULT,
      onKeyDown,
      onPaste,
      ...props
    },
    ref
  ) => {
    // Count digits in current value
    const countDigits = (str: string): number => {
      return (str.match(/\d/g) || []).length;
    };

    // Check if new value is valid
    const isValidInput = (newValue: string): boolean => {
      // Check allowed characters
      if (!ALLOWED_CHARS.test(newValue)) {
        return false;
      }

      // Check total length
      if (newValue.length > maxLength) {
        return false;
      }

      // Check digit count
      if (countDigits(newValue) > maxDigits) {
        return false;
      }

      // Check + is only at start and appears only once
      const plusCount = (newValue.match(/\+/g) || []).length;
      if (plusCount > 1) {
        return false;
      }
      if (plusCount === 1 && !newValue.startsWith('+')) {
        return false;
      }

      return true;
    };

    // Handle input change
    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      const newValue = e.target.value;

      // Allow empty value
      if (newValue === '') {
        onChange('');
        return;
      }

      // Only update if valid
      if (isValidInput(newValue)) {
        onChange(newValue);
      }
      // If invalid, don't update (keeps previous value)
    };

    // Handle key down to prevent invalid characters
    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
      // Allow control keys
      if (
        e.key === 'Backspace' ||
        e.key === 'Delete' ||
        e.key === 'Tab' ||
        e.key === 'Escape' ||
        e.key === 'Enter' ||
        e.key === 'ArrowLeft' ||
        e.key === 'ArrowRight' ||
        e.key === 'ArrowUp' ||
        e.key === 'ArrowDown' ||
        e.key === 'Home' ||
        e.key === 'End' ||
        (e.ctrlKey && (e.key === 'a' || e.key === 'c' || e.key === 'v' || e.key === 'x'))
      ) {
        onKeyDown?.(e);
        return;
      }

      // Check if character is allowed
      const char = e.key;
      if (!/^[\d\s\-+()]$/.test(char)) {
        e.preventDefault();
        return;
      }

      // Prevent + if not at start or already exists
      if (char === '+') {
        const input = e.currentTarget;
        const cursorPos = input.selectionStart || 0;
        if (cursorPos !== 0 || value.includes('+')) {
          e.preventDefault();
          return;
        }
      }

      // Prevent digit if at max
      if (/\d/.test(char) && countDigits(value) >= maxDigits) {
        e.preventDefault();
        return;
      }

      // Prevent if at max length (no selection = not replacing text)
      const currentInput = e.currentTarget;
      if (value.length >= maxLength && currentInput.selectionStart === currentInput.selectionEnd) {
        e.preventDefault();
        return;
      }

      onKeyDown?.(e);
    };

    // Handle paste - filter out invalid characters
    const handlePaste = (e: React.ClipboardEvent<HTMLInputElement>) => {
      e.preventDefault();

      const pastedText = e.clipboardData.getData('text');
      const input = e.currentTarget;
      const cursorStart = input.selectionStart || 0;
      const cursorEnd = input.selectionEnd || 0;

      // Filter pasted text to only allowed characters
      let filteredText = pastedText.replace(/[^\d\s\-+()]/g, '');

      // If pasting in middle, remove + from pasted text
      if (cursorStart > 0) {
        filteredText = filteredText.replace(/\+/g, '');
      }

      // Build new value
      const beforeCursor = value.slice(0, cursorStart);
      const afterCursor = value.slice(cursorEnd);
      let newValue = beforeCursor + filteredText + afterCursor;

      // Trim to max length
      if (newValue.length > maxLength) {
        newValue = newValue.slice(0, maxLength);
      }

      // Check digit count and trim if needed
      while (countDigits(newValue) > maxDigits && newValue.length > 0) {
        // Remove last digit
        const lastDigitIndex = newValue.search(/\d(?=[^\d]*$)/);
        if (lastDigitIndex >= 0) {
          newValue = newValue.slice(0, lastDigitIndex) + newValue.slice(lastDigitIndex + 1);
        } else {
          break;
        }
      }

      if (isValidInput(newValue)) {
        onChange(newValue);
      }

      onPaste?.(e);
    };

    return (
      <input
        type="tel"
        inputMode="tel"
        className={cn(
          'flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50',
          error && 'border-destructive focus-visible:ring-destructive',
          className
        )}
        ref={ref}
        value={value}
        onChange={handleChange}
        onKeyDown={handleKeyDown}
        onPaste={handlePaste}
        aria-invalid={error ? 'true' : undefined}
        {...props}
      />
    );
  }
);

PhoneInput.displayName = 'PhoneInput';

export { PhoneInput };
