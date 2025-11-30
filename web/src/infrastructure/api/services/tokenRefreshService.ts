/**
 * Token Refresh Service
 * Handles automatic token refresh with retry queue to prevent duplicate refreshes
 *
 * Features:
 * - Automatic retry on 401 Unauthorized
 * - Prevents duplicate refresh requests using a queue
 * - Thread-safe refresh operation
 * - Transparent token refresh without user interaction
 */

import { apiClient } from '../client/api-client';
import { useAuthStore } from '@/presentation/store/useAuthStore';

class TokenRefreshService {
  private isRefreshing = false;
  private refreshSubscribers: Array<(token: string) => void> = [];

  /**
   * Check if token refresh is currently in progress
   */
  public get isRefreshInProgress(): boolean {
    return this.isRefreshing;
  }

  /**
   * Add subscriber to be notified when token refresh completes
   */
  private subscribeTokenRefresh(callback: (token: string) => void): void {
    this.refreshSubscribers.push(callback);
  }

  /**
   * Notify all subscribers that token refresh is complete
   */
  private onTokenRefreshed(token: string): void {
    this.refreshSubscribers.forEach((callback) => callback(token));
    this.refreshSubscribers = [];
  }

  /**
   * Attempt to refresh the access token
   * Returns new access token if successful, null otherwise
   */
  public async refreshAccessToken(): Promise<string | null> {
    // If refresh is already in progress, queue this request
    if (this.isRefreshing) {
      return new Promise<string | null>((resolve) => {
        this.subscribeTokenRefresh((token: string) => {
          resolve(token);
        });
      });
    }

    this.isRefreshing = true;

    try {
      console.log('🔄 Attempting to refresh access token...');

      // Call the refresh endpoint
      // Note: Refresh token is in HttpOnly cookie, backend reads it automatically
      const response = await apiClient.post<{
        accessToken: string;
        tokenExpiresAt: string;
      }>('/Auth/refresh', {});

      const { accessToken, tokenExpiresAt } = response;

      console.log('✅ Token refreshed successfully', {
        expiresAt: tokenExpiresAt,
      });

      // Update auth store with new token
      const { setAuth, user } = useAuthStore.getState();
      if (user) {
        setAuth(user, {
          accessToken,
          refreshToken: '', // Refresh token is in HttpOnly cookie (not in localStorage)
          expiresIn: 1800, // 30 minutes (matches backend config)
        });
      }

      // Notify all queued requests
      this.onTokenRefreshed(accessToken);

      this.isRefreshing = false;
      return accessToken;
    } catch (error) {
      console.error('❌ Token refresh failed:', error);

      // Clear auth and notify subscribers
      this.isRefreshing = false;
      this.onTokenRefreshed('');

      // Clear auth state - user needs to login again
      const { clearAuth } = useAuthStore.getState();
      clearAuth();

      return null;
    }
  }

  /**
   * Retry a failed request after refreshing the token
   */
  public async retryRequestAfterRefresh<T>(
    originalRequest: () => Promise<T>
  ): Promise<T | null> {
    const newToken = await this.refreshAccessToken();

    if (!newToken) {
      throw new Error('Token refresh failed - please login again');
    }

    // Retry the original request with the new token
    try {
      return await originalRequest();
    } catch (error) {
      console.error('❌ Retry request failed even after token refresh:', error);
      throw error;
    }
  }
}

// Export singleton instance
export const tokenRefreshService = new TokenRefreshService();
