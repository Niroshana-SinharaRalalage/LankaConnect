/**
 * Unit Tests for Environment Validation
 *
 * Tests for validateEnv() — the fail-closed safety system that prevents
 * the production UI from ever silently routing to the staging backend.
 *
 * Post-Incident Fix: On 2026-04-17, a partial YAML update wiped all env vars
 * from the production UI container. Because the proxy had a hardcoded staging
 * fallback URL, production users saw staging data for ~20 minutes.
 *
 * This validation module ensures:
 * - Production: BACKEND_API_URL required, no staging fallback
 * - Development: staging fallback allowed for developer convenience
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { validateEnv, getEnvValidation, _resetEnvValidationCache } from '../env-validation';

describe('Environment Validation', () => {
  beforeEach(() => {
    _resetEnvValidationCache();
  });

  describe('Local Development (NODE_ENV unset or "development")', () => {
    it('should be valid with no env vars set at all', () => {
      const result = validateEnv({});
      expect(result.isValid).toBe(true);
      expect(result.isDeployedEnvironment).toBe(false);
      expect(result.errors).toHaveLength(0);
    });

    it('should be valid when NODE_ENV is "development"', () => {
      const result = validateEnv({ NODE_ENV: 'development' });
      expect(result.isValid).toBe(true);
      expect(result.isDeployedEnvironment).toBe(false);
    });

    it('should use staging fallback URL when BACKEND_API_URL is missing', () => {
      const result = validateEnv({ NODE_ENV: 'development' });
      expect(result.backendUrl).toContain('lankaconnect-api-staging');
    });

    it('should use staging fallback URL when NODE_ENV is unset', () => {
      const result = validateEnv({});
      expect(result.backendUrl).toContain('lankaconnect-api-staging');
    });

    it('should use configured URL when BACKEND_API_URL is set in development', () => {
      const result = validateEnv({
        NODE_ENV: 'development',
        BACKEND_API_URL: 'http://localhost:5000/api',
      });
      expect(result.backendUrl).toBe('http://localhost:5000/api');
    });

    it('should have no errors or warnings in development with no vars', () => {
      const result = validateEnv({});
      expect(result.errors).toHaveLength(0);
      expect(result.warnings).toHaveLength(0);
    });
  });

  describe('Deployed Environment (NODE_ENV="production")', () => {
    it('should FAIL when BACKEND_API_URL is missing', () => {
      const result = validateEnv({ NODE_ENV: 'production' });
      expect(result.isValid).toBe(false);
      expect(result.isDeployedEnvironment).toBe(true);
      expect(result.errors.length).toBeGreaterThan(0);
      expect(result.errors[0]).toContain('BACKEND_API_URL');
    });

    it('should FAIL when BACKEND_API_URL is empty string', () => {
      const result = validateEnv({ NODE_ENV: 'production', BACKEND_API_URL: '' });
      expect(result.isValid).toBe(false);
    });

    it('should FAIL when BACKEND_API_URL is whitespace only', () => {
      const result = validateEnv({ NODE_ENV: 'production', BACKEND_API_URL: '   ' });
      expect(result.isValid).toBe(false);
    });

    it('should return null backendUrl when validation fails (fail-closed)', () => {
      const result = validateEnv({ NODE_ENV: 'production' });
      expect(result.backendUrl).toBeNull();
    });

    it('should NEVER fall back to staging URL in production', () => {
      const result = validateEnv({ NODE_ENV: 'production' });
      expect(result.backendUrl).toBeNull();
      // Null means fail-closed — no staging fallback, no proxy requests served
    });

    it('should PASS when BACKEND_API_URL is set', () => {
      const result = validateEnv({
        NODE_ENV: 'production',
        BACKEND_API_URL: 'https://lankaconnect-api-prod.example.com/api',
      });
      expect(result.isValid).toBe(true);
      expect(result.errors).toHaveLength(0);
      expect(result.backendUrl).toBe('https://lankaconnect-api-prod.example.com/api');
    });

    it('should warn about missing NEXT_PUBLIC_ENV', () => {
      const result = validateEnv({
        NODE_ENV: 'production',
        BACKEND_API_URL: 'https://prod-api.example.com/api',
      });
      expect(result.warnings).toContain('NEXT_PUBLIC_ENV is not set');
    });

    it('should warn about missing NEXT_PUBLIC_API_URL', () => {
      const result = validateEnv({
        NODE_ENV: 'production',
        BACKEND_API_URL: 'https://prod-api.example.com/api',
      });
      expect(result.warnings).toContain('NEXT_PUBLIC_API_URL is not set');
    });

    it('should have no warnings when all recommended vars are set', () => {
      const result = validateEnv({
        NODE_ENV: 'production',
        BACKEND_API_URL: 'https://prod-api.example.com/api',
        NEXT_PUBLIC_ENV: 'production',
        NEXT_PUBLIC_API_URL: '/api/proxy',
      });
      expect(result.warnings).toHaveLength(0);
    });
  });

  describe('getEnvValidation() caching', () => {
    it('should return the same result on repeated calls', () => {
      // Reset cache first
      _resetEnvValidationCache();

      const result1 = getEnvValidation();
      const result2 = getEnvValidation();
      expect(result1).toBe(result2); // same reference (cached)
    });

    it('should return fresh result after cache reset', () => {
      const result1 = getEnvValidation();
      _resetEnvValidationCache();
      const result2 = getEnvValidation();
      expect(result1).not.toBe(result2); // different reference
    });
  });

  describe('Edge Cases', () => {
    it('should treat NODE_ENV="test" as non-deployed (allow fallback)', () => {
      const result = validateEnv({ NODE_ENV: 'test' });
      expect(result.isValid).toBe(true);
      expect(result.isDeployedEnvironment).toBe(false);
      expect(result.backendUrl).toContain('staging');
    });

    it('should handle undefined env gracefully', () => {
      const result = validateEnv({ NODE_ENV: undefined, BACKEND_API_URL: undefined });
      expect(result.isValid).toBe(true);
    });

    it('should trim BACKEND_API_URL value', () => {
      const result = validateEnv({
        NODE_ENV: 'production',
        BACKEND_API_URL: '  https://prod-api.example.com/api  ',
      });
      expect(result.isValid).toBe(true);
      expect(result.backendUrl).toBe('https://prod-api.example.com/api');
    });
  });
});
