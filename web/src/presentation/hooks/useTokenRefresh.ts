/**
 * useTokenRefresh Hook
 * Proactively refreshes JWT token before it expires
 *
 * Usage: Call this hook in your root layout or app component
 * Example: useTokenRefresh();
 *
 * Features:
 * - Automatically refreshes token 5 minutes before expiration
 * - Clears timer on logout
 * - Handles edge cases (invalid tokens, no token, etc.)
 */

import { useEffect, useRef } from 'react';
import { useAuthStore } from '../store/useAuthStore';
import { getTimeUntilExpiration, getRefreshTime } from '@/infrastructure/utils/jwtDecoder';
import { tokenRefreshService } from '@/infrastructure/api/services/tokenRefreshService';

export function useTokenRefresh() {
  const { accessToken, isAuthenticated } = useAuthStore();
  const timerRef = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    // Clear any existing timer
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }

    // Only set up refresh if user is authenticated and has a token
    if (!isAuthenticated || !accessToken) {
      console.log('⏸️ Token refresh timer: Not authenticated or no token');
      return;
    }

    const refreshTime = getRefreshTime(accessToken);

    if (!refreshTime) {
      console.warn('⚠️ Unable to determine token refresh time - token may be invalid');
      return;
    }

    const now = Date.now();
    const timeUntilRefresh = refreshTime - now;

    // If token should already be refreshed, refresh immediately
    if (timeUntilRefresh <= 0) {
      console.log('⚡ Token refresh overdue - refreshing immediately');
      tokenRefreshService.refreshAccessToken().catch((error) => {
        console.error('❌ Immediate token refresh failed:', error);
      });
      return;
    }

    console.log('⏰ Token refresh scheduled in:', {
      minutes: Math.round(timeUntilRefresh / 60000),
      seconds: Math.round(timeUntilRefresh / 1000),
    });

    // Set timer to refresh token
    timerRef.current = setTimeout(() => {
      console.log('🔄 Proactive token refresh triggered');
      tokenRefreshService.refreshAccessToken().catch((error) => {
        console.error('❌ Proactive token refresh failed:', error);
      });
    }, timeUntilRefresh);

    // Cleanup function
    return () => {
      if (timerRef.current) {
        console.log('🧹 Clearing token refresh timer');
        clearTimeout(timerRef.current);
        timerRef.current = null;
      }
    };
  }, [accessToken, isAuthenticated]);

  // Also check token expiration on mount and every minute
  useEffect(() => {
    if (!isAuthenticated || !accessToken) {
      return;
    }

    const checkInterval = setInterval(() => {
      const timeRemaining = getTimeUntilExpiration(accessToken);

      if (timeRemaining === null) {
        console.warn('⚠️ Invalid token detected in periodic check');
        return;
      }

      // If less than 5 minutes remaining, refresh now
      if (timeRemaining < 5 * 60 * 1000 && timeRemaining > 0) {
        console.log('⚡ Less than 5 minutes remaining - refreshing token');
        tokenRefreshService.refreshAccessToken().catch((error) => {
          console.error('❌ Periodic token refresh failed:', error);
        });
      }
    }, 60 * 1000); // Check every minute

    return () => clearInterval(checkInterval);
  }, [accessToken, isAuthenticated]);
}
