/**
 * Token Storage Service Tests
 * Tests environment-based token storage strategy
 */

import { tokenStorageService } from '../tokenStorageService';

describe('TokenStorageService', () => {
  let originalEnv: string | undefined;

  beforeEach(() => {
    // Save original environment
    originalEnv = process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH;

    // Clear localStorage and cookies
    localStorage.clear();
    document.cookie.split(";").forEach((c) => {
      document.cookie = c
        .replace(/^ +/, "")
        .replace(/=.*/, "=;expires=" + new Date().toUTCString() + ";path=/");
    });
  });

  afterEach(() => {
    // Restore original environment
    if (originalEnv !== undefined) {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = originalEnv;
    } else {
      delete process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH;
    }
  });

  describe('shouldUseLocalStorage', () => {
    it('should return true when NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH is "true"', () => {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = 'true';
      expect(tokenStorageService.shouldUseLocalStorage()).toBe(true);
    });

    it('should return false when NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH is "false"', () => {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = 'false';
      expect(tokenStorageService.shouldUseLocalStorage()).toBe(false);
    });

    it('should return false when NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH is not set', () => {
      delete process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH;
      expect(tokenStorageService.shouldUseLocalStorage()).toBe(false);
    });

    it('should return false when NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH is empty string', () => {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = '';
      expect(tokenStorageService.shouldUseLocalStorage()).toBe(false);
    });
  });

  describe('setRefreshToken - localStorage mode', () => {
    beforeEach(() => {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = 'true';
    });

    it('should store refresh token in localStorage', () => {
      const token = 'test-refresh-token-123';
      tokenStorageService.setRefreshToken(token);

      expect(localStorage.getItem('refreshToken')).toBe(token);
    });

    it('should not set cookie when using localStorage', () => {
      const token = 'test-refresh-token-123';
      tokenStorageService.setRefreshToken(token);

      expect(document.cookie).not.toContain('refreshToken');
    });

    it('should overwrite existing token in localStorage', () => {
      localStorage.setItem('refreshToken', 'old-token');

      const newToken = 'new-refresh-token-456';
      tokenStorageService.setRefreshToken(newToken);

      expect(localStorage.getItem('refreshToken')).toBe(newToken);
    });
  });

  describe('setRefreshToken - cookie mode', () => {
    beforeEach(() => {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = 'false';
    });

    it('should NOT store refresh token in localStorage', () => {
      const token = 'test-refresh-token-123';
      tokenStorageService.setRefreshToken(token);

      expect(localStorage.getItem('refreshToken')).toBeNull();
    });

    it('should log warning that backend will handle cookie', () => {
      const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});

      const token = 'test-refresh-token-123';
      tokenStorageService.setRefreshToken(token);

      expect(consoleWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining('[TOKEN STORAGE] Cookie mode: Backend will set HttpOnly cookie')
      );

      consoleWarnSpy.mockRestore();
    });
  });

  describe('getRefreshToken - localStorage mode', () => {
    beforeEach(() => {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = 'true';
    });

    it('should retrieve refresh token from localStorage', () => {
      const token = 'test-refresh-token-123';
      localStorage.setItem('refreshToken', token);

      expect(tokenStorageService.getRefreshToken()).toBe(token);
    });

    it('should return null when no token in localStorage', () => {
      expect(tokenStorageService.getRefreshToken()).toBeNull();
    });
  });

  describe('getRefreshToken - cookie mode', () => {
    beforeEach(() => {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = 'false';
    });

    it('should return null (backend handles cookie)', () => {
      // Even if localStorage has a token, should return null in cookie mode
      localStorage.setItem('refreshToken', 'should-not-use-this');

      expect(tokenStorageService.getRefreshToken()).toBeNull();
    });

    it('should log info that backend sends cookie automatically', () => {
      const consoleLogSpy = vi.spyOn(console, 'log').mockImplementation(() => {});

      tokenStorageService.getRefreshToken();

      expect(consoleLogSpy).toHaveBeenCalledWith(
        expect.stringContaining('[TOKEN STORAGE] Cookie mode: Browser sends HttpOnly cookie automatically')
      );

      consoleLogSpy.mockRestore();
    });
  });

  describe('clearRefreshToken - localStorage mode', () => {
    beforeEach(() => {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = 'true';
    });

    it('should remove refresh token from localStorage', () => {
      localStorage.setItem('refreshToken', 'test-token');

      tokenStorageService.clearRefreshToken();

      expect(localStorage.getItem('refreshToken')).toBeNull();
    });

    it('should be idempotent (safe to call multiple times)', () => {
      localStorage.setItem('refreshToken', 'test-token');

      tokenStorageService.clearRefreshToken();
      tokenStorageService.clearRefreshToken();

      expect(localStorage.getItem('refreshToken')).toBeNull();
    });
  });

  describe('clearRefreshToken - cookie mode', () => {
    beforeEach(() => {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = 'false';
    });

    it('should not clear localStorage in cookie mode', () => {
      // If there's somehow a token in localStorage, don't touch it
      localStorage.setItem('refreshToken', 'some-token');

      tokenStorageService.clearRefreshToken();

      expect(localStorage.getItem('refreshToken')).toBe('some-token');
    });

    it('should log warning that logout endpoint will clear cookie', () => {
      const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});

      tokenStorageService.clearRefreshToken();

      expect(consoleWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining('[TOKEN STORAGE] Cookie mode: Call /Auth/logout to clear HttpOnly cookie')
      );

      consoleWarnSpy.mockRestore();
    });
  });

  describe('getStorageMode', () => {
    it('should return "localStorage" when using localStorage', () => {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = 'true';
      expect(tokenStorageService.getStorageMode()).toBe('localStorage');
    });

    it('should return "httpOnlyCookie" when using cookies', () => {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = 'false';
      expect(tokenStorageService.getStorageMode()).toBe('httpOnlyCookie');
    });
  });

  describe('Edge cases', () => {
    it('should handle undefined token gracefully', () => {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = 'true';

      expect(() => {
        tokenStorageService.setRefreshToken(undefined as any);
      }).not.toThrow();
    });

    it('should handle null token gracefully', () => {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = 'true';

      expect(() => {
        tokenStorageService.setRefreshToken(null as any);
      }).not.toThrow();
    });

    it('should not store empty string token', () => {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = 'true';

      const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});
      tokenStorageService.setRefreshToken('');

      // Should not store empty token
      expect(localStorage.getItem('refreshToken')).toBeNull();
      expect(consoleWarnSpy).toHaveBeenCalledWith(
        expect.stringContaining('[TOKEN STORAGE] Attempted to store empty/null token')
      );

      consoleWarnSpy.mockRestore();
    });
  });
});
