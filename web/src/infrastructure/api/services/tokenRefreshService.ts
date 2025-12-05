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
    console.log('🔍 [TOKEN REFRESH] refreshAccessToken() called');

    // If refresh is already in progress, queue this request
    if (this.isRefreshing) {
      console.log('🔍 [TOKEN REFRESH] Refresh already in progress, queueing this request');
      return new Promise<string | null>((resolve) => {
        this.subscribeTokenRefresh((token: string) => {
          console.log('🔍 [TOKEN REFRESH] Queued request received token:', token ? 'YES' : 'NO');
          resolve(token);
        });
      });
    }

    console.log('🔍 [TOKEN REFRESH] Starting new refresh operation');
    this.isRefreshing = true;

    try {
      console.log('🔄 [TOKEN REFRESH] Calling POST /Auth/refresh...');

      // Call the refresh endpoint
      // Note: Refresh token is in HttpOnly cookie, backend reads it automatically
      const response = await apiClient.post<{
        accessToken: string;
        tokenExpiresAt: string;
      }>('/Auth/refresh', {});

      console.log('🔍 [TOKEN REFRESH] Response received:', {
        hasAccessToken: !!response?.accessToken,
        tokenExpiresAt: response?.tokenExpiresAt,
      });

      const { accessToken, tokenExpiresAt } = response;

      console.log('✅ [TOKEN REFRESH] Token refreshed successfully', {
        expiresAt: tokenExpiresAt,
        tokenLength: accessToken?.length,
      });

      // Update auth store with new token
      const { setAuth, user } = useAuthStore.getState();
      console.log('🔍 [TOKEN REFRESH] Auth store state:', { hasUser: !!user, userId: user?.id });

      if (user) {
        console.log('🔍 [TOKEN REFRESH] Updating auth store with new token');
        setAuth(user, {
          accessToken,
          refreshToken: '', // Refresh token is in HttpOnly cookie (not in localStorage)
          expiresIn: 1800, // 30 minutes (matches backend config)
        });
      } else {
        console.warn('⚠️ [TOKEN REFRESH] No user in auth store, cannot update token');
      }

      // Notify all queued requests
      console.log('🔍 [TOKEN REFRESH] Notifying queued requests, count:', this.refreshSubscribers.length);
      this.onTokenRefreshed(accessToken);

      this.isRefreshing = false;
      console.log('🔍 [TOKEN REFRESH] Refresh complete, returning new token');
      return accessToken;
    } catch (error: any) {
      console.error('❌ [TOKEN REFRESH] Token refresh failed with error:', {
        message: error?.message,
        status: error?.statusCode || error?.response?.status,
        response: error?.response?.data,
      });

      // Clear auth and notify subscribers
      this.isRefreshing = false;
      console.log('🔍 [TOKEN REFRESH] Notifying queued requests with empty token');
      this.onTokenRefreshed('');

      // Clear auth state - user needs to login again
      console.log('🔍 [TOKEN REFRESH] Clearing auth state via clearAuth()');
      const { clearAuth } = useAuthStore.getState();
      clearAuth();

      console.log('🔍 [TOKEN REFRESH] Returning null');
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
