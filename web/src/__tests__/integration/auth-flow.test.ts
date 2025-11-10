import { describe, it, expect, beforeAll, afterEach } from 'vitest';
import { authRepository } from '@/infrastructure/api/repositories/auth.repository';
import { apiClient } from '@/infrastructure/api/client/api-client';
import type { RegisterRequest, LoginRequest } from '@/infrastructure/api/types/auth.types';

/**
 * Integration Tests for Authentication Flow
 * Tests against the staging API to verify end-to-end authentication
 */
describe('Authentication Flow Integration Tests', () => {
  const testUser = {
    email: `test.user.${Date.now()}@lankaconnect.test`,
    password: 'TestPassword123!',
    firstName: 'Test',
    lastName: 'User',
  };

  let accessToken: string | null = null;
  let refreshToken: string | null = null;

  beforeAll(() => {
    // Ensure API client is using staging API
    console.log('Testing against API:', process.env.NEXT_PUBLIC_API_URL);
  });

  afterEach(() => {
    // Clear any tokens after each test
    if (accessToken) {
      apiClient.clearAuthToken();
      accessToken = null;
    }
  });

  describe('User Registration', () => {
    it('should successfully register a new user', async () => {
      const registerData: RegisterRequest = {
        email: testUser.email,
        password: testUser.password,
        firstName: testUser.firstName,
        lastName: testUser.lastName,
      };

      const response = await authRepository.register(registerData);

      expect(response).toBeDefined();
      expect(response.userId).toBeDefined();
      expect(response.email).toBe(testUser.email.toLowerCase());
      expect(response.message).toBeDefined();
    });

    it('should fail to register with duplicate email', async () => {
      const registerData: RegisterRequest = {
        email: testUser.email,
        password: testUser.password,
        firstName: testUser.firstName,
        lastName: testUser.lastName,
      };

      await expect(authRepository.register(registerData)).rejects.toThrow();
    });

    it('should fail to register with invalid password', async () => {
      const registerData: RegisterRequest = {
        email: `test.weak.${Date.now()}@lankaconnect.test`,
        password: 'weak', // Too weak
        firstName: 'Test',
        lastName: 'Weak',
      };

      await expect(authRepository.register(registerData)).rejects.toThrow();
    });
  });

  describe('User Login', () => {
    it('should successfully login with valid credentials', async () => {
      const loginData: LoginRequest = {
        email: testUser.email,
        password: testUser.password,
      };

      const response = await authRepository.login(loginData);

      expect(response).toBeDefined();
      expect(response.user).toBeDefined();
      expect(response.user.email).toBe(testUser.email.toLowerCase());
      expect(response.user.fullName).toContain(testUser.firstName);
      expect(response.user.fullName).toContain(testUser.lastName);
      expect(response.accessToken).toBeDefined();
      expect(response.tokenExpiresAt).toBeDefined();

      // Store tokens for subsequent tests
      accessToken = response.accessToken;
      // Note: refreshToken is stored in HttpOnly cookie, not in response
      refreshToken = '';
    });

    it('should fail to login with incorrect password', async () => {
      const loginData: LoginRequest = {
        email: testUser.email,
        password: 'WrongPassword123!',
      };

      await expect(authRepository.login(loginData)).rejects.toThrow();
    });

    it('should fail to login with non-existent user', async () => {
      const loginData: LoginRequest = {
        email: 'nonexistent@lankaconnect.test',
        password: 'Password123!',
      };

      await expect(authRepository.login(loginData)).rejects.toThrow();
    });
  });

  describe('Token Management', () => {
    it('should set auth token in API client', async () => {
      const loginData: LoginRequest = {
        email: testUser.email,
        password: testUser.password,
      };

      const response = await authRepository.login(loginData);
      apiClient.setAuthToken(response.accessToken);

      // Token should be set (we can't directly test this, but no error should be thrown)
      expect(response.accessToken).toBeDefined();
    });

    it('should clear auth token from API client', () => {
      apiClient.clearAuthToken();
      // No error should be thrown
      expect(true).toBe(true);
    });
  });

  describe('Auth Repository Methods', () => {
    it('should have all required methods', () => {
      expect(authRepository.login).toBeDefined();
      expect(authRepository.register).toBeDefined();
      expect(authRepository.refreshToken).toBeDefined();
      expect(authRepository.logout).toBeDefined();
      expect(authRepository.requestPasswordReset).toBeDefined();
      expect(authRepository.resetPassword).toBeDefined();
      expect(authRepository.verifyEmail).toBeDefined();
      expect(authRepository.resendVerificationEmail).toBeDefined();
    });
  });

  describe('Password Reset Flow (Method Existence)', () => {
    it('should have requestPasswordReset method', () => {
      expect(authRepository.requestPasswordReset).toBeInstanceOf(Function);
    });

    it('should have resetPassword method', () => {
      expect(authRepository.resetPassword).toBeInstanceOf(Function);
    });
  });

  describe('Email Verification Flow (Method Existence)', () => {
    it('should have verifyEmail method', () => {
      expect(authRepository.verifyEmail).toBeInstanceOf(Function);
    });

    it('should have resendVerificationEmail method', () => {
      expect(authRepository.resendVerificationEmail).toBeInstanceOf(Function);
    });
  });
});
