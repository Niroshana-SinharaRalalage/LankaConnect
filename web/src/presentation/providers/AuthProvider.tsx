'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { apiClient } from '@/infrastructure/api/client/api-client';
import { useAuthStore } from '@/presentation/store/useAuthStore';

/**
 * AuthProvider Component
 * Sets up global 401 error handling for automatic logout on token expiration
 *
 * This component:
 * 1. Registers a callback with the API client to handle 401 Unauthorized errors
 * 2. When a 401 occurs (token expired), it:
 *    - Clears authentication state
 *    - Redirects to login page
 * 3. Prevents multiple redirects with a flag
 */
export function AuthProvider({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const clearAuth = useAuthStore((state) => state.clearAuth);

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
