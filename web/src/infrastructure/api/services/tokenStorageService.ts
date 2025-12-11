/**
 * Token Storage Service
 * Environment-based storage strategy for refresh tokens
 *
 * Purpose:
 * - Development: Use localStorage (less secure but dev is already insecure - HTTP, no domain isolation)
 * - Production: Use HttpOnly cookies (secure, immune to XSS)
 *
 * Why this solves the problem:
 * - Localhost (HTTP) cannot use Secure cookies from HTTPS backend
 * - localStorage works on localhost and allows token refresh to function
 * - Production uses proper HttpOnly cookies for security
 * - Environment variable controls the behavior
 */

class TokenStorageService {
  private readonly STORAGE_KEY = 'refreshToken';

  /**
   * Determine if we should use localStorage based on environment
   * Returns true for development (localhost), false for production
   */
  public shouldUseLocalStorage(): boolean {
    const useLocalStorage = process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH;
    return useLocalStorage === 'true';
  }

  /**
   * Get current storage mode for debugging
   */
  public getStorageMode(): 'localStorage' | 'httpOnlyCookie' {
    return this.shouldUseLocalStorage() ? 'localStorage' : 'httpOnlyCookie';
  }

  /**
   * Store refresh token using environment-appropriate method
   *
   * @param token - The refresh token to store
   */
  public setRefreshToken(token: string): void {
    if (!token) {
      console.warn('[TOKEN STORAGE] Attempted to store empty/null token');
      return;
    }

    if (this.shouldUseLocalStorage()) {
      // Development: Store in localStorage
      console.log('[TOKEN STORAGE] Storing refresh token in localStorage (development mode)');
      localStorage.setItem(this.STORAGE_KEY, token);
    } else {
      // Production: Backend sets HttpOnly cookie
      console.warn('[TOKEN STORAGE] Cookie mode: Backend will set HttpOnly cookie, frontend cannot access it');
    }
  }

  /**
   * Retrieve refresh token using environment-appropriate method
   *
   * @returns The refresh token, or null if not found/not accessible
   */
  public getRefreshToken(): string | null {
    if (this.shouldUseLocalStorage()) {
      // Development: Read from localStorage
      const token = localStorage.getItem(this.STORAGE_KEY);
      console.log('[TOKEN STORAGE] Retrieved refresh token from localStorage:', token ? 'FOUND' : 'NOT FOUND');
      return token;
    } else {
      // Production: Cookie is sent automatically by browser
      console.log('[TOKEN STORAGE] Cookie mode: Browser sends HttpOnly cookie automatically, frontend cannot read it');
      return null; // Cannot access HttpOnly cookie from JavaScript
    }
  }

  /**
   * Clear refresh token using environment-appropriate method
   */
  public clearRefreshToken(): void {
    if (this.shouldUseLocalStorage()) {
      // Development: Remove from localStorage
      console.log('[TOKEN STORAGE] Clearing refresh token from localStorage');
      localStorage.removeItem(this.STORAGE_KEY);
    } else {
      // Production: Backend clears cookie via /Auth/logout endpoint
      console.warn('[TOKEN STORAGE] Cookie mode: Call /Auth/logout to clear HttpOnly cookie');
    }
  }
}

// Export singleton instance
export const tokenStorageService = new TokenStorageService();
