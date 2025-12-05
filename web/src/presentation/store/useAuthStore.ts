import { create } from 'zustand';
import { devtools, persist } from 'zustand/middleware';
import type { UserDto, AuthTokens } from '@/infrastructure/api/types/auth.types';
import { LocalStorageService } from '@/infrastructure/storage/localStorage';
import { apiClient } from '@/infrastructure/api/client/api-client';

interface AuthState {
  // State
  user: UserDto | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;

  // Actions
  setAuth: (user: UserDto, tokens: AuthTokens) => void;
  clearAuth: () => void;
  setLoading: (loading: boolean) => void;
  updateUser: (user: Partial<UserDto>) => void;
}

/**
 * Auth Store
 * Global state management for authentication using Zustand
 */
export const useAuthStore = create<AuthState>()(
  devtools(
    persist(
      (set, get) => ({
        // Initial state
        user: null,
        accessToken: null,
        refreshToken: null,
        isAuthenticated: false,
        isLoading: false,

        // Set authentication (after login/register)
        setAuth: (user, tokens) => {
          // Store tokens in localStorage
          LocalStorageService.setAccessToken(tokens.accessToken);
          LocalStorageService.setRefreshToken(tokens.refreshToken);
          LocalStorageService.setUser(user);

          // Set auth token in API client
          apiClient.setAuthToken(tokens.accessToken);

          set({
            user,
            accessToken: tokens.accessToken,
            refreshToken: tokens.refreshToken,
            isAuthenticated: true,
            isLoading: false,
          });
        },

        // Clear authentication (logout)
        clearAuth: () => {
          console.log('🔍 [AUTH STORE] clearAuth() called');
          console.trace('🔍 [AUTH STORE] Stack trace:');

          // Clear localStorage
          console.log('🔍 [AUTH STORE] Clearing localStorage');
          LocalStorageService.clearAuth();

          // Clear auth token from API client
          console.log('🔍 [AUTH STORE] Clearing API client auth token');
          apiClient.clearAuthToken();

          console.log('🔍 [AUTH STORE] Setting state to unauthenticated');
          set({
            user: null,
            accessToken: null,
            refreshToken: null,
            isAuthenticated: false,
            isLoading: false,
          });

          console.log('🔍 [AUTH STORE] clearAuth() completed');
        },

        // Set loading state
        setLoading: (loading) => {
          set({ isLoading: loading });
        },

        // Update user data
        updateUser: (userData) => {
          const currentUser = get().user;
          if (!currentUser) return;

          const updatedUser = { ...currentUser, ...userData };
          LocalStorageService.setUser(updatedUser);

          set({ user: updatedUser });
        },
      }),
      {
        name: 'auth-storage',
        partialize: (state) => ({
          user: state.user,
          accessToken: state.accessToken,
          refreshToken: state.refreshToken,
          isAuthenticated: state.isAuthenticated,
        }),
        onRehydrateStorage: () => (state) => {
          // Restore auth token to API client on app load
          if (state?.accessToken) {
            apiClient.setAuthToken(state.accessToken);
          }
        },
      }
    ),
    { name: 'AuthStore' }
  )
);
