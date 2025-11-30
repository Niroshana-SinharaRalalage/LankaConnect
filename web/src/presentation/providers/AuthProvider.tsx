'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { apiClient } from '@/infrastructure/api/client/api-client';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useTokenRefresh } from '@/presentation/hooks/useTokenRefresh';

/**
 * AuthProvider Component
 * Sets up global authentication features:
 *
 * 1. Global 401 error handling for automatic logout on token expiration
 * 2. Proactive token refresh - automatically refreshes token before expiration
 *
 * This component:
 * - Registers a callback with the API client to handle 401 Unauthorized errors
 * - Sets up proactive token refresh timer (refreshes 5 minutes before expiry)
 * - When a 401 occurs (token expired), it clears auth and redirects to login
 * - Prevents multiple redirects with a flag
 */
export function AuthProvider({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const clearAuth = useAuthStore((state) => state.clearAuth);

  // Phase AUTH-IMPROVEMENT: Proactive token refresh
  // Automatically refreshes token 5 minutes before expiration
  useTokenRefresh();

  useEffect(() => {
    let isHandling401 = false;

    // Set up 401 error handler
    apiClient.setUnauthorizedCallback(() => {
      // Prevent multiple simultaneous logout/redirect calls
      if (isHandling401) return;
      isHandling401 = true;

      // Clear authentication state
      clearAuth();

      // Redirect to login page
      router.push('/login');

      // Reset flag after redirect
      setTimeout(() => {
        isHandling401 = false;
      }, 1000);
    });
  }, [router, clearAuth]);

  return <>{children}</>;
}
