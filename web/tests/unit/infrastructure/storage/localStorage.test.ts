import { describe, it, expect, beforeEach, vi } from 'vitest';
import { LocalStorageService } from '@/infrastructure/storage/localStorage';

// Mock localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {};

  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => {
      store[key] = value;
    },
    removeItem: (key: string) => {
      delete store[key];
    },
    clear: () => {
      store = {};
    },
  };
})();

Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
  writable: true,
});

describe('LocalStorageService', () => {
  beforeEach(() => {
    localStorageMock.clear();
  });

  describe('basic operations', () => {
    it('should store and retrieve string values', () => {
      LocalStorageService.setItem('test', 'value');
      const result = LocalStorageService.getItem<string>('test');

      expect(result).toBe('value');
    });

    it('should store and retrieve object values', () => {
      const obj = { name: 'John', age: 30 };
      LocalStorageService.setItem('user', obj);
      const result = LocalStorageService.getItem<typeof obj>('user');

      expect(result).toEqual(obj);
    });

    it('should return null for non-existent keys', () => {
      const result = LocalStorageService.getItem('nonexistent');

      expect(result).toBeNull();
    });

    it('should remove items', () => {
      LocalStorageService.setItem('test', 'value');
      LocalStorageService.removeItem('test');
      const result = LocalStorageService.getItem('test');

      expect(result).toBeNull();
    });

    it('should clear all items', () => {
      LocalStorageService.setItem('key1', 'value1');
      LocalStorageService.setItem('key2', 'value2');
      LocalStorageService.clear();

      expect(LocalStorageService.getItem('key1')).toBeNull();
      expect(LocalStorageService.getItem('key2')).toBeNull();
    });
  });

  describe('auth-specific methods', () => {
    it('should store and retrieve access token', () => {
      const token = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...';
      LocalStorageService.setAccessToken(token);
      const result = LocalStorageService.getAccessToken();

      expect(result).toBe(token);
    });

    it('should store and retrieve refresh token', () => {
      const token = 'refresh_token_xyz';
      LocalStorageService.setRefreshToken(token);
      const result = LocalStorageService.getRefreshToken();

      expect(result).toBe(token);
    });

    it('should store and retrieve user object', () => {
      const user = {
        userId: '123',
        email: 'test@example.com',
        firstName: 'John',
        lastName: 'Doe',
      };
      LocalStorageService.setUser(user);
      const result = LocalStorageService.getUser<typeof user>();

      expect(result).toEqual(user);
    });

    it('should clear all auth data', () => {
      LocalStorageService.setAccessToken('access_token');
      LocalStorageService.setRefreshToken('refresh_token');
      LocalStorageService.setUser({ userId: '123' });

      LocalStorageService.clearAuth();

      expect(LocalStorageService.getAccessToken()).toBeNull();
      expect(LocalStorageService.getRefreshToken()).toBeNull();
      expect(LocalStorageService.getUser()).toBeNull();
    });
  });

  describe('error handling', () => {
    it('should handle invalid JSON gracefully', () => {
      // Manually set invalid JSON
      localStorageMock.setItem('invalid', '{invalid json}');
      const result = LocalStorageService.getItem('invalid');

      expect(result).toBeNull();
    });

    it('should return false when setItem fails', () => {
      // Mock setItem to throw
      const originalSetItem = localStorageMock.setItem;
      localStorageMock.setItem = () => {
        throw new Error('QuotaExceededError');
      };

      const result = LocalStorageService.setItem('test', 'value');
      expect(result).toBe(false);

      // Restore
      localStorageMock.setItem = originalSetItem;
    });
  });
});
