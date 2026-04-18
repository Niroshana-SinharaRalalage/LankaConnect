/**
 * Environment Validation Module
 *
 * Fail-closed safety system that prevents the production UI from ever
 * silently routing API requests to the staging backend.
 *
 * Defense-in-depth layers:
 *   1. instrumentation.ts  — logs FATAL at startup (doesn't throw)
 *   2. /api/health          — returns 500, Azure probes fail, no traffic routed
 *   3. /api/proxy/*         — returns 503, never falls back to staging
 *
 * Post-Incident Fix (2026-04-17): A partial YAML update wiped all env vars
 * from the production UI container. The proxy's hardcoded staging fallback
 * caused production users to see staging data for ~20 minutes.
 *
 * This module ensures that can never happen again.
 */

export interface EnvValidationResult {
  /** Whether all required env vars are present */
  isValid: boolean;
  /** Whether we're running in a deployed environment (NODE_ENV=production) */
  isDeployedEnvironment: boolean;
  /** Hard failures — required vars missing in deployed environments */
  errors: string[];
  /** Non-critical issues — recommended vars missing */
  warnings: string[];
  /** The backend URL to use, or null if validation failed in a deployed env */
  backendUrl: string | null;
}

/** Env vars that MUST be present in deployed environments */
const REQUIRED_DEPLOYED_VARS = ['BACKEND_API_URL'] as const;

/** Env vars that SHOULD be present (warning only, not a hard failure) */
const RECOMMENDED_VARS = ['NEXT_PUBLIC_ENV', 'NEXT_PUBLIC_API_URL'] as const;

/**
 * Staging fallback URL — ONLY used in local development.
 * In production, if BACKEND_API_URL is missing, backendUrl is null (fail-closed).
 */
const STAGING_FALLBACK_URL =
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api';

/**
 * Validate environment variables.
 *
 * Pure function — accepts an explicit env record for testability.
 * Does NOT throw. Returns a result object describing what's valid/invalid.
 *
 * @param env - The environment variables to validate (defaults to process.env)
 */
export function validateEnv(
  env: Record<string, string | undefined> = process.env
): EnvValidationResult {
  const nodeEnv = env.NODE_ENV;
  const isDeployedEnvironment = nodeEnv === 'production';
  const errors: string[] = [];
  const warnings: string[] = [];

  // In deployed environments, validate required vars
  if (isDeployedEnvironment) {
    for (const varName of REQUIRED_DEPLOYED_VARS) {
      if (!env[varName]?.trim()) {
        errors.push(
          `${varName} is required in deployed environments (NODE_ENV=${nodeEnv})`
        );
      }
    }

    for (const varName of RECOMMENDED_VARS) {
      if (!env[varName]?.trim()) {
        warnings.push(`${varName} is not set`);
      }
    }
  }

  // Determine the backend URL (fail-closed in production)
  const configuredUrl = env.BACKEND_API_URL?.trim();
  let backendUrl: string | null;

  if (configuredUrl) {
    backendUrl = configuredUrl;
  } else if (!isDeployedEnvironment) {
    // Local development convenience — staging fallback
    backendUrl = STAGING_FALLBACK_URL;
  } else {
    // Deployed + missing = fail closed (null = no proxy requests served)
    backendUrl = null;
  }

  return {
    isValid: errors.length === 0,
    isDeployedEnvironment,
    errors,
    warnings,
    backendUrl,
  };
}

// ---------------------------------------------------------------------------
// Singleton cache — evaluated once per server lifecycle
// ---------------------------------------------------------------------------

let _cachedResult: EnvValidationResult | null = null;

/**
 * Get the cached validation result (evaluated once at first call).
 * Used by health endpoint and proxy route.
 */
export function getEnvValidation(): EnvValidationResult {
  if (!_cachedResult) {
    _cachedResult = validateEnv();
  }
  return _cachedResult;
}

/**
 * Reset the cached result. Only used in tests.
 * @internal
 */
export function _resetEnvValidationCache(): void {
  _cachedResult = null;
}
