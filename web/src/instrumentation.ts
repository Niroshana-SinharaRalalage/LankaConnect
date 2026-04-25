/**
 * Next.js Instrumentation Hook
 *
 * Runs once when the Next.js server starts. Used for environment validation
 * logging so operators can immediately see configuration issues in container logs.
 *
 * Design choice: Logs FATAL errors but does NOT throw.
 * - Throwing = crash loop = can't debug, can't hit /api/health to see what's wrong.
 * - Logging + returning 500 from /api/health = Azure probes fail = no traffic routed,
 *   but container stays alive for diagnosis.
 *
 * Post-Incident Fix (2026-04-17): Added after a production outage where missing
 * env vars caused silent routing to the staging backend.
 */
export async function register() {
  // Only validate on server side (not edge runtime)
  if (process.env.NEXT_RUNTIME === 'nodejs') {
    const { getEnvValidation } = await import('@/lib/env-validation');
    const result = getEnvValidation();

    if (!result.isValid) {
      console.error('='.repeat(80));
      console.error('FATAL CONFIGURATION ERROR');
      console.error('='.repeat(80));
      console.error('Required environment variables are missing:');
      result.errors.forEach((e) => console.error(`  - ${e}`));
      console.error('');
      console.error('The health endpoint will return HTTP 500.');
      console.error('Azure Container Apps will NOT route traffic to this revision.');
      console.error('The previous healthy revision will continue serving.');
      console.error('');
      console.error('To fix: set the missing env vars on the Container App:');
      console.error(
        '  az containerapp update --name <app> --resource-group <rg> --set-env-vars "BACKEND_API_URL=https://..."'
      );
      console.error('='.repeat(80));
    }

    if (result.warnings.length > 0) {
      console.warn('[Startup] Environment warnings:');
      result.warnings.forEach((w) => console.warn(`  - ${w}`));
    }

    if (result.isValid) {
      console.log(
        `[Startup] Environment validation passed. Backend URL: ${result.backendUrl}`
      );
    }
  }
}
