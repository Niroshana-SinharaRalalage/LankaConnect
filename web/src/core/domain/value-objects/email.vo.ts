import { ValueObject } from '../shared/value-object';
import { Result } from '../shared/result';

/**
 * Email Value Object
 * Ensures email format validity and normalization
 */
export class Email extends ValueObject<string> {
  private static readonly EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

  private constructor(value: string) {
    super(value);
  }

  public static create(email: string): Result<Email> {
    // Validate not empty
    if (!email || email.trim().length === 0) {
      return Result.fail('Email cannot be empty');
    }

    // Normalize to lowercase
    const normalizedEmail = email.trim().toLowerCase();

    // Validate email format
    if (!this.EMAIL_REGEX.test(normalizedEmail)) {
      return Result.fail('Email must be a valid email address');
    }

    // Additional validation: check for @ symbol
    if (!normalizedEmail.includes('@')) {
      return Result.fail('Email must contain @ symbol');
    }

    // Validate local part and domain exist
    const [localPart, domain] = normalizedEmail.split('@');
    if (!localPart || localPart.length === 0) {
      return Result.fail('Email must have a local part before @');
    }
    if (!domain || domain.length === 0) {
      return Result.fail('Email must have a domain after @');
    }

    // Validate domain has at least one dot
    if (!domain.includes('.')) {
      return Result.fail('Email domain must contain at least one dot');
    }

    return Result.ok(new Email(normalizedEmail));
  }

  public getValue(): string {
    return this._value;
  }
}
