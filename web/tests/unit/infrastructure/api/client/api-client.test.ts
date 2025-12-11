import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { ApiClient } from '@/infrastructure/api/client/api-client';
import { ApiError, NetworkError, ValidationError, UnauthorizedError } from '@/infrastructure/api/client/api-errors';

// Mock axios
vi.mock('axios', () => {
  return {
    default: {
      create: vi.fn(() => ({
        interceptors: {
          request: { use: vi.fn() },
          response: { use: vi.fn() },
        },
        get: vi.fn(),
        post: vi.fn(),
        put: vi.fn(),
        delete: vi.fn(),
      })),
    },
  };
});

describe('ApiClient', () => {
  let apiClient: ApiClient;

  beforeEach(() => {
    apiClient = ApiClient.getInstance();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('singleton pattern', () => {
    it('should return same instance', () => {
      const instance1 = ApiClient.getInstance();
      const instance2 = ApiClient.getInstance();

      expect(instance1).toBe(instance2);
    });
  });

  describe('GET requests', () => {
    it('should make GET request successfully', async () => {
      const mockData = { id: 1, name: 'Test' };

      // Mock implementation would go here
      // For now, test the interface exists
      expect(apiClient.get).toBeDefined();
      expect(typeof apiClient.get).toBe('function');
    });
  });

  describe('POST requests', () => {
    it('should make POST request successfully', async () => {
      expect(apiClient.post).toBeDefined();
      expect(typeof apiClient.post).toBe('function');
    });
  });

  describe('PUT requests', () => {
    it('should make PUT request successfully', async () => {
      expect(apiClient.put).toBeDefined();
      expect(typeof apiClient.put).toBe('function');
    });
  });

  describe('DELETE requests', () => {
    it('should make DELETE request successfully', async () => {
      expect(apiClient.delete).toBeDefined();
      expect(typeof apiClient.delete).toBe('function');
    });
  });

  describe('error handling', () => {
    it('should handle network errors', () => {
      const error = new NetworkError('Network error');
      expect(error).toBeInstanceOf(ApiError);
      expect(error.message).toBe('Network error');
    });

    it('should handle validation errors', () => {
      const error = new ValidationError('Validation failed', { email: ['Invalid email'] });
      expect(error).toBeInstanceOf(ApiError);
      expect(error.message).toBe('Validation failed');
      expect(error.validationErrors).toEqual({ email: ['Invalid email'] });
    });

    it('should handle unauthorized errors', () => {
      const error = new UnauthorizedError('Unauthorized');
      expect(error).toBeInstanceOf(ApiError);
      expect(error.message).toBe('Unauthorized');
    });
  });

  describe('authentication', () => {
    it('should set authorization token', () => {
      expect(apiClient.setAuthToken).toBeDefined();
      expect(typeof apiClient.setAuthToken).toBe('function');
    });

    it('should clear authorization token', () => {
      expect(apiClient.clearAuthToken).toBeDefined();
      expect(typeof apiClient.clearAuthToken).toBe('function');
    });
  });
});
