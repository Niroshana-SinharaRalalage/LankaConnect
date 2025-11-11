import { apiClient } from '../client/api-client';
import type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse,
} from '../types/auth.types';

/**
 * AuthRepository
 * Handles all authentication-related API calls
 * Repository pattern for auth operations
 */
export class AuthRepository {
  private readonly basePath = '/auth';

  /**
   * Login user
   */
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    const response = await apiClient.post<LoginResponse>(
      `${this.basePath}/login`,
      credentials
    );
    return response;
  }

  /**
   * Register new user
   */
  async register(userData: RegisterRequest): Promise<RegisterResponse> {
    const response = await apiClient.post<RegisterResponse>(
      `${this.basePath}/register`,
      userData
    );
    return response;
  }

  /**
   * Refresh access token
   */
  async refreshToken(refreshToken: string): Promise<{ accessToken: string; refreshToken: string }> {
    const response = await apiClient.post<{ accessToken: string; refreshToken: string }>(
      `${this.basePath}/refresh-token`,
      { refreshToken }
    );
    return response;
  }

  /**
   * Logout user
   */
  async logout(): Promise<void> {
    await apiClient.post(`${this.basePath}/logout`);
  }

  /**
   * Request password reset
   */
  async requestPasswordReset(email: string): Promise<{ message: string }> {
    const response = await apiClient.post<{ message: string }>(
      `${this.basePath}/forgot-password`,
      { email }
    );
    return response;
  }

  /**
   * Reset password with token
   */
  async resetPassword(token: string, newPassword: string): Promise<{ message: string }> {
    const response = await apiClient.post<{ message: string }>(
      `${this.basePath}/reset-password`,
      { token, newPassword }
    );
    return response;
  }

  /**
   * Verify email with token
   */
  async verifyEmail(token: string): Promise<{ message: string }> {
    const response = await apiClient.post<{ message: string }>(
      `${this.basePath}/verify-email`,
      { token }
    );
    return response;
  }

  /**
   * Resend verification email
   */
  async resendVerificationEmail(email: string): Promise<{ message: string }> {
    const response = await apiClient.post<{ message: string }>(
      `${this.basePath}/resend-verification`,
      { email }
    );
    return response;
  }

  /**
   * [TEST ONLY] Verify user email without token validation
   * Only available in Development environment for E2E testing
   */
  async testVerifyEmail(userId: string): Promise<{ message: string }> {
    const response = await apiClient.post<{ message: string }>(
      `${this.basePath}/test/verify-user/${userId}`,
      {}
    );
    return response;
  }
}

// Export singleton instance
export const authRepository = new AuthRepository();
