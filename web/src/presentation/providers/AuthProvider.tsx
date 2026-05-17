'use client';

import { useEffect } from 'react';
import toast from 'react-hot-toast';
import { apiClient } from '@/infrastructure/api/client/api-client';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useTokenRefresh } from '@/presentation/hooks/useTokenRefresh';

/**
 * AuthProvider Component
 * Sets up global authentication features:
 *
 * 1. Global 401 error handling for authenticated-user session expiry
 * 2. Proactive token refresh - automatically refreshes token before expiration
 *
 * **Phase 6A.150 update (Layer 3, defense-in-depth)**:
 * - The unauthorized callback is invoked ONLY when the original request
 *   carried an access token (the `hadAuthAtRequestTime` flag in
 *   `api-client.ts`'s response interceptor). Anonymous-user 401s
 *   short-circuit BEFORE reaching this callback (Layer 2), so we never
 *   redirect them to /login.
 * - The legacy `router.push('/login')` is replaced by `clearAuth()` plus
 *   a toast notification. The user stays on the current page (which may
 *   be a public page they were viewing while authed) and can choose to
 *   sign in from the header. This avoids the "hard bounce" UX where a
 *   user reading a public event suddenly lands on /login.
 * - `isHandling401` debounce is preserved so toast doesn't spam during
 *   concurrent 401s.
 */
export function AuthProvider({ children }: { children: React.ReactNode }) {
  const clearAuth = useAuthStore((state) => state.clearAuth);

  // Phase AUTH-IMPROVEMENT: Proactive token refresh
  // Automatically refreshes token 5 minutes before expiration
  useTokenRefresh();

  useEffect(() => {
    let isHandling401 = false;

    // Set up 401 error handler. Only invoked by api-client when a request
    // that carried an access token gets 401 AND the refresh attempt fails.
    // Anonymous 401s never reach here after Phase 6A.150 Layer 2.
    apiClient.setUnauthorizedCallback(() => {
      console.log('[AUTH PROVIDER] onUnauthorized callback triggered (session expired for authed user)');

      // Prevent multiple simultaneous toast/clear calls during concurrent 401s.
      if (isHandling401) {
        console.log('[AUTH PROVIDER] Already handling 401, skipping');
        return;
      }
      isHandling401 = true;

      try {
        console.log('[AUTH PROVIDER] Calling clearAuth() (no forced redirect — Phase 6A.150 Layer 3)');
        clearAuth();

        // Soft, non-blocking notification. The user stays on the current
        // page; they can sign in via the header when ready.
        toast(
          'Your session expired. Please sign in again to access personalized features.',
          {
            duration: 6000,
            id: 'session-expired',  // de-dupe key for concurrent triggers
          },
        );
      } catch (err) {
        console.error('[AUTH PROVIDER] Failed to handle 401 callback:', err);
      } finally {
        // Reset flag after the toast's idle window so a later session-death
        // can re-trigger the notification.
        setTimeout(() => {
          isHandling401 = false;
        }, 1000);
      }
    });
  }, [clearAuth]);

  return <>{children}</>;
}
