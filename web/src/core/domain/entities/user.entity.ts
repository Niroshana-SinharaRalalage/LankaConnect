import { Entity } from '../shared/entity';
import { Result } from '../shared/result';
import { Email } from '../value-objects/email.vo';
import { Password } from '../value-objects/password.vo';
import { v4 as uuidv4 } from 'uuid';

export interface UserProps {
  email: Email;
  password: Password;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  isVerified?: boolean;
  createdAt?: Date;
  updatedAt?: Date;
}

/**
 * User Entity
 * Represents a user in the system
 */
export class User extends Entity<UserProps> {
  private constructor(props: UserProps, id: string) {
    super(props, id);
  }

  public static create(props: UserProps, id?: string): Result<User> {
    // Validate required fields
    if (!props.email) {
      return Result.fail('User must have an email');
    }

    if (!props.password) {
      return Result.fail('User must have a password');
    }

    if (!props.firstName || props.firstName.trim().length === 0) {
      return Result.fail('User must have a valid first name');
    }

    if (!props.lastName || props.lastName.trim().length === 0) {
      return Result.fail('User must have a valid last name');
    }

    // Set default values
    const userProps: UserProps = {
      ...props,
      isVerified: props.isVerified ?? false,
      createdAt: props.createdAt ?? new Date(),
      updatedAt: props.updatedAt ?? new Date(),
    };

    const userId = id ?? uuidv4();

    return Result.ok(new User(userProps, userId));
  }

  // Getters
  public get email(): Email {
    return this.props.email;
  }

  public get password(): Password {
    return this.props.password;
  }

  public get firstName(): string {
    return this.props.firstName;
  }

  public get lastName(): string {
    return this.props.lastName;
  }

  public get fullName(): string {
    return `${this.props.firstName} ${this.props.lastName}`;
  }

  public get phoneNumber(): string | undefined {
    return this.props.phoneNumber;
  }

  public get isVerified(): boolean {
    return this.props.isVerified ?? false;
  }

  public get createdAt(): Date {
    return this.props.createdAt ?? new Date();
  }

  public get updatedAt(): Date {
    return this.props.updatedAt ?? new Date();
  }

  // Behavior methods
  public async verifyPassword(plainPassword: string): Promise<boolean> {
    return this.props.password.compare(plainPassword);
  }

  public updateProfile(updates: Partial<Pick<UserProps, 'firstName' | 'lastName' | 'phoneNumber'>>): Result<void> {
    if (updates.firstName !== undefined) {
      if (updates.firstName.trim().length === 0) {
        return Result.fail('First name cannot be empty');
      }
      (this.props as any).firstName = updates.firstName;
    }

    if (updates.lastName !== undefined) {
      if (updates.lastName.trim().length === 0) {
        return Result.fail('Last name cannot be empty');
      }
      (this.props as any).lastName = updates.lastName;
    }

    if (updates.phoneNumber !== undefined) {
      (this.props as any).phoneNumber = updates.phoneNumber;
    }

    (this.props as any).updatedAt = new Date();

    return Result.ok(undefined);
  }

  public verify(): Result<void> {
    (this.props as any).isVerified = true;
    (this.props as any).updatedAt = new Date();
    return Result.ok(undefined);
  }
}
