/**
 * LocalStorage Utility
 * Type-safe wrapper for localStorage with error handling
 */

const STORAGE_KEYS = {
  ACCESS_TOKEN: 'lankaconnect_access_token',
  REFRESH_TOKEN: 'lankaconnect_refresh_token',
  USER: 'lankaconnect_user',
} as const;

export class LocalStorageService {
  /**
   * Check if localStorage is available
   */
  private static isAvailable(): boolean {
    try {
      const test = '__localStorage_test__';
      localStorage.setItem(test, test);
      localStorage.removeItem(test);
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Get item from localStorage
   */
  static getItem<T>(key: string): T | null {
    if (!this.isAvailable()) {
      console.warn('localStorage is not available');
      return null;
    }

    try {
      const item = localStorage.getItem(key);
      if (!item) return null;
      return JSON.parse(item) as T;
    } catch (error) {
      console.error(`Error reading from localStorage (${key}):`, error);
      return null;
    }
  }

  /**
   * Set item in localStorage
   */
  static setItem<T>(key: string, value: T): boolean {
    if (!this.isAvailable()) {
      console.warn('localStorage is not available');
      return false;
    }

    try {
      localStorage.setItem(key, JSON.stringify(value));
      return true;
    } catch (error) {
      console.error(`Error writing to localStorage (${key}):`, error);
      return false;
    }
  }

  /**
   * Remove item from localStorage
   */
  static removeItem(key: string): void {
    if (!this.isAvailable()) {
      console.warn('localStorage is not available');
      return;
    }

    try {
      localStorage.removeItem(key);
    } catch (error) {
      console.error(`Error removing from localStorage (${key}):`, error);
    }
  }

  /**
   * Clear all items from localStorage
   */
  static clear(): void {
    if (!this.isAvailable()) {
      console.warn('localStorage is not available');
      return;
    }

    try {
      localStorage.clear();
    } catch (error) {
      console.error('Error clearing localStorage:', error);
    }
  }

  // Auth-specific methods
  static getAccessToken(): string | null {
    return this.getItem<string>(STORAGE_KEYS.ACCESS_TOKEN);
  }

  static setAccessToken(token: string): boolean {
    return this.setItem(STORAGE_KEYS.ACCESS_TOKEN, token);
  }

  static getRefreshToken(): string | null {
    return this.getItem<string>(STORAGE_KEYS.REFRESH_TOKEN);
  }

  static setRefreshToken(token: string): boolean {
    return this.setItem(STORAGE_KEYS.REFRESH_TOKEN, token);
  }

  static getUser<T>(): T | null {
    return this.getItem<T>(STORAGE_KEYS.USER);
  }

  static setUser<T>(user: T): boolean {
    return this.setItem(STORAGE_KEYS.USER, user);
  }

  static clearAuth(): void {
    this.removeItem(STORAGE_KEYS.ACCESS_TOKEN);
    this.removeItem(STORAGE_KEYS.REFRESH_TOKEN);
    this.removeItem(STORAGE_KEYS.USER);
  }
}
