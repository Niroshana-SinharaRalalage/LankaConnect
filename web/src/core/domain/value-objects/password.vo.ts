import { ValueObject } from '../shared/value-object';
import { Result } from '../shared/result';

/**
 * Password Value Object
 * Ensures password strength requirements and handles hashing
 */
export class Password extends ValueObject<string> {
  private static readonly MIN_LENGTH = 8;
  private static readonly UPPERCASE_REGEX = /[A-Z]/;
  private static readonly LOWERCASE_REGEX = /[a-z]/;
  private static readonly NUMBER_REGEX = /[0-9]/;
  private static readonly SPECIAL_CHAR_REGEX = /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/;

  private readonly isHashed: boolean;

  private constructor(value: string, isHashed: boolean = false) {
    super(value);
    this.isHashed = isHashed;
  }

  public static create(plainPassword: string): Result<Password> {
    // Validate not empty
    if (!plainPassword || plainPassword.trim().length === 0) {
      return Result.fail('Password cannot be empty');
    }

    // Validate minimum length
    if (plainPassword.length < this.MIN_LENGTH) {
      return Result.fail(`Password must be at least ${this.MIN_LENGTH} characters long`);
    }

    // Validate contains uppercase letter
    if (!this.UPPERCASE_REGEX.test(plainPassword)) {
      return Result.fail('Password must contain at least one uppercase letter');
    }

    // Validate contains lowercase letter
    if (!this.LOWERCASE_REGEX.test(plainPassword)) {
      return Result.fail('Password must contain at least one lowercase letter');
    }

    // Validate contains number
    if (!this.NUMBER_REGEX.test(plainPassword)) {
      return Result.fail('Password must contain at least one number');
    }

    // Validate contains special character
    if (!this.SPECIAL_CHAR_REGEX.test(plainPassword)) {
      return Result.fail('Password must contain at least one special character');
    }

    return Result.ok(new Password(plainPassword, false));
  }

  public static fromHash(hashedPassword: string): Result<Password> {
    if (!hashedPassword || hashedPassword.trim().length === 0) {
      return Result.fail('Hashed password cannot be empty');
    }

    return Result.ok(new Password(hashedPassword, true));
  }

  /**
   * Get hashed value of password
   * In a real implementation, this would use bcrypt or similar
   * For now, we'll use a simple hash simulation
   */
  public async getHashedValue(): Promise<string> {
    if (this.isHashed) {
      return this._value;
    }

    // Simulate bcrypt hashing
    // In production, use: import bcrypt from 'bcryptjs'; return bcrypt.hash(this._value, 10);
    return this.simulateHash(this._value);
  }

  /**
   * Compare plain password with stored hash
   */
  public async compare(plainPassword: string): Promise<boolean> {
    if (!this.isHashed) {
      // If this is not a hashed password, do simple comparison
      return this._value === plainPassword;
    }

    // Simulate bcrypt comparison
    // In production, use: import bcrypt from 'bcryptjs'; return bcrypt.compare(plainPassword, this._value);
    const hashedPlain = await this.simulateHash(plainPassword);
    // Note: This is not how real bcrypt works (each hash has a unique salt)
    // This is just for testing purposes
    return this._value.startsWith(hashedPlain.substring(0, 20));
  }

  /**
   * Simulate password hashing
   * In production, replace with bcrypt
   */
  private async simulateHash(password: string): Promise<string> {
    // Simple hash simulation using built-in crypto
    const encoder = new TextEncoder();
    const data = encoder.encode(password + Math.random().toString(36)); // Add random salt
    const hashBuffer = await crypto.subtle.digest('SHA-256', data);
    const hashArray = Array.from(new Uint8Array(hashBuffer));
    const hashHex = hashArray.map((b) => b.toString(16).padStart(2, '0')).join('');
    return `$2a$10$${hashHex}`; // Simulate bcrypt format
  }

  /**
   * Do not allow direct access to password value
   */
  public getValue(): never {
    throw new Error('Cannot get raw password value for security reasons. Use getHashedValue() instead.');
  }
}
