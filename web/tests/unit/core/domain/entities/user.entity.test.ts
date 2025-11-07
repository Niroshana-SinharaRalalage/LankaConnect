import { describe, it, expect } from 'vitest';
import { User, UserProps } from '@/core/domain/entities/user.entity';
import { Email } from '@/core/domain/value-objects/email.vo';
import { Password } from '@/core/domain/value-objects/password.vo';

describe('User Entity', () => {
  describe('creation', () => {
    it('should create user with valid data', () => {
      const emailResult = Email.create('test@example.com');
      const passwordResult = Password.create('Pass123!');

      expect(emailResult.isSuccess).toBe(true);
      expect(passwordResult.isSuccess).toBe(true);

      if (emailResult.isSuccess && passwordResult.isSuccess) {
        const userResult = User.create({
          email: emailResult.value,
          password: passwordResult.value,
          firstName: 'John',
          lastName: 'Doe',
        });

        expect(userResult.isSuccess).toBe(true);

        if (userResult.isSuccess) {
          const user = userResult.value;
          expect(user.id).toBeDefined();
          expect(user.email.getValue()).toBe('test@example.com');
          expect(user.firstName).toBe('John');
          expect(user.lastName).toBe('Doe');
          expect(user.fullName).toBe('John Doe');
        }
      }
    });

    it('should create user with existing id', () => {
      const emailResult = Email.create('test@example.com');
      const passwordResult = Password.create('Pass123!');

      expect(emailResult.isSuccess).toBe(true);
      expect(passwordResult.isSuccess).toBe(true);

      if (emailResult.isSuccess && passwordResult.isSuccess) {
        const existingId = 'user-123';

        const userResult = User.create(
          {
            email: emailResult.value,
            password: passwordResult.value,
            firstName: 'John',
            lastName: 'Doe',
          },
          existingId
        );

        expect(userResult.isSuccess).toBe(true);

        if (userResult.isSuccess) {
          expect(userResult.value.id).toBe(existingId);
        }
      }
    });

    it('should reject user without email', () => {
      const passwordResult = Password.create('Pass123!');

      expect(passwordResult.isSuccess).toBe(true);

      if (passwordResult.isSuccess) {
        const userResult = User.create({
          email: null as any,
          password: passwordResult.value,
          firstName: 'John',
          lastName: 'Doe',
        });

        expect(userResult.isFailure).toBe(true);

        if (userResult.isFailure) {
          expect(userResult.error).toContain('email');
        }
      }
    });

    it('should reject user without password', () => {
      const emailResult = Email.create('test@example.com');

      expect(emailResult.isSuccess).toBe(true);

      if (emailResult.isSuccess) {
        const userResult = User.create({
          email: emailResult.value,
          password: null as any,
          firstName: 'John',
          lastName: 'Doe',
        });

        expect(userResult.isFailure).toBe(true);

        if (userResult.isFailure) {
          expect(userResult.error).toContain('password');
        }
      }
    });

    it('should reject user with empty first name', () => {
      const emailResult = Email.create('test@example.com');
      const passwordResult = Password.create('Pass123!');

      expect(emailResult.isSuccess).toBe(true);
      expect(passwordResult.isSuccess).toBe(true);

      if (emailResult.isSuccess && passwordResult.isSuccess) {
        const userResult = User.create({
          email: emailResult.value,
          password: passwordResult.value,
          firstName: '',
          lastName: 'Doe',
        });

        expect(userResult.isFailure).toBe(true);

        if (userResult.isFailure) {
          expect(userResult.error).toContain('first name');
        }
      }
    });

    it('should reject user with empty last name', () => {
      const emailResult = Email.create('test@example.com');
      const passwordResult = Password.create('Pass123!');

      expect(emailResult.isSuccess).toBe(true);
      expect(passwordResult.isSuccess).toBe(true);

      if (emailResult.isSuccess && passwordResult.isSuccess) {
        const userResult = User.create({
          email: emailResult.value,
          password: passwordResult.value,
          firstName: 'John',
          lastName: '',
        });

        expect(userResult.isFailure).toBe(true);

        if (userResult.isFailure) {
          expect(userResult.error).toContain('last name');
        }
      }
    });

    it('should create user with optional fields', () => {
      const emailResult = Email.create('test@example.com');
      const passwordResult = Password.create('Pass123!');

      expect(emailResult.isSuccess).toBe(true);
      expect(passwordResult.isSuccess).toBe(true);

      if (emailResult.isSuccess && passwordResult.isSuccess) {
        const userResult = User.create({
          email: emailResult.value,
          password: passwordResult.value,
          firstName: 'John',
          lastName: 'Doe',
          phoneNumber: '+94712345678',
          isVerified: true,
        });

        expect(userResult.isSuccess).toBe(true);

        if (userResult.isSuccess) {
          const user = userResult.value;
          expect(user.phoneNumber).toBe('+94712345678');
          expect(user.isVerified).toBe(true);
        }
      }
    });
  });

  describe('equality', () => {
    it('should consider two users with same id as equal', () => {
      const emailResult = Email.create('test@example.com');
      const passwordResult = Password.create('Pass123!');

      expect(emailResult.isSuccess).toBe(true);
      expect(passwordResult.isSuccess).toBe(true);

      if (emailResult.isSuccess && passwordResult.isSuccess) {
        const userId = 'user-123';

        const user1Result = User.create(
          {
            email: emailResult.value,
            password: passwordResult.value,
            firstName: 'John',
            lastName: 'Doe',
          },
          userId
        );

        const user2Result = User.create(
          {
            email: emailResult.value,
            password: passwordResult.value,
            firstName: 'Jane',
            lastName: 'Smith',
          },
          userId
        );

        expect(user1Result.isSuccess).toBe(true);
        expect(user2Result.isSuccess).toBe(true);

        if (user1Result.isSuccess && user2Result.isSuccess) {
          expect(user1Result.value.equals(user2Result.value)).toBe(true);
        }
      }
    });

    it('should consider two users with different ids as not equal', () => {
      const emailResult = Email.create('test@example.com');
      const passwordResult = Password.create('Pass123!');

      expect(emailResult.isSuccess).toBe(true);
      expect(passwordResult.isSuccess).toBe(true);

      if (emailResult.isSuccess && passwordResult.isSuccess) {
        const user1Result = User.create({
          email: emailResult.value,
          password: passwordResult.value,
          firstName: 'John',
          lastName: 'Doe',
        });

        const user2Result = User.create({
          email: emailResult.value,
          password: passwordResult.value,
          firstName: 'John',
          lastName: 'Doe',
        });

        expect(user1Result.isSuccess).toBe(true);
        expect(user2Result.isSuccess).toBe(true);

        if (user1Result.isSuccess && user2Result.isSuccess) {
          expect(user1Result.value.equals(user2Result.value)).toBe(false);
        }
      }
    });
  });

  describe('behavior', () => {
    it('should verify password correctly', async () => {
      const emailResult = Email.create('test@example.com');
      const passwordResult = Password.create('Pass123!');

      expect(emailResult.isSuccess).toBe(true);
      expect(passwordResult.isSuccess).toBe(true);

      if (emailResult.isSuccess && passwordResult.isSuccess) {
        const userResult = User.create({
          email: emailResult.value,
          password: passwordResult.value,
          firstName: 'John',
          lastName: 'Doe',
        });

        expect(userResult.isSuccess).toBe(true);

        if (userResult.isSuccess) {
          const user = userResult.value;
          const isValid = await user.verifyPassword('Pass123!');
          expect(isValid).toBe(true);
        }
      }
    });

    it('should reject incorrect password', async () => {
      const emailResult = Email.create('test@example.com');
      const passwordResult = Password.create('Pass123!');

      expect(emailResult.isSuccess).toBe(true);
      expect(passwordResult.isSuccess).toBe(true);

      if (emailResult.isSuccess && passwordResult.isSuccess) {
        const userResult = User.create({
          email: emailResult.value,
          password: passwordResult.value,
          firstName: 'John',
          lastName: 'Doe',
        });

        expect(userResult.isSuccess).toBe(true);

        if (userResult.isSuccess) {
          const user = userResult.value;
          const isValid = await user.verifyPassword('WrongPass123!');
          expect(isValid).toBe(false);
        }
      }
    });

    it('should update user profile', () => {
      const emailResult = Email.create('test@example.com');
      const passwordResult = Password.create('Pass123!');

      expect(emailResult.isSuccess).toBe(true);
      expect(passwordResult.isSuccess).toBe(true);

      if (emailResult.isSuccess && passwordResult.isSuccess) {
        const userResult = User.create({
          email: emailResult.value,
          password: passwordResult.value,
          firstName: 'John',
          lastName: 'Doe',
        });

        expect(userResult.isSuccess).toBe(true);

        if (userResult.isSuccess) {
          const user = userResult.value;
          const updateResult = user.updateProfile({
            firstName: 'Jane',
            phoneNumber: '+94712345678',
          });

          expect(updateResult.isSuccess).toBe(true);

          if (updateResult.isSuccess) {
            expect(user.firstName).toBe('Jane');
            expect(user.lastName).toBe('Doe');
            expect(user.phoneNumber).toBe('+94712345678');
          }
        }
      }
    });
  });
});
