import { create } from 'zustand';
import { devtools, persist, createJSONStorage } from 'zustand/middleware';
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
  _hasHydrated: boolean;
  isHydrated: boolean; // Public getter for _hasHydrated

  // Actions
  setAuth: (user: UserDto, tokens: AuthTokens) => void;
  updateAccessToken: (accessToken: string) => void;
  clearAuth: () => void;
  setLoading: (loading: boolean) => void;
  updateUser: (user: Partial<UserDto>) => void;
  setHasHydrated: (state: boolean) => void;
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
        _hasHydrated: false,

        // Public getter for hydration state
        get isHydrated() {
          return get()._hasHydrated;
        },

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
            _hasHydrated: true,
          });
        },

        // Update only access token (for token refresh)
        // Does not touch refreshToken since it's in HttpOnly cookie
        updateAccessToken: (accessToken) => {
          // Store new access token in localStorage
          LocalStorageService.setAccessToken(accessToken);

          // Update auth token in API client
          apiClient.setAuthToken(accessToken);

          // Update state - preserve existing refreshToken
          set({
            accessToken,
            _hasHydrated: true,
          });
        },

        // Clear authentication (logout)
        clearAuth: () => {
          console.log('🔍 [AUTH STORE] clearAuth() called');

          // Clear localStorage
          LocalStorageService.clearAuth();

          // Clear auth token from API client
          apiClient.clearAuthToken();

          set({
            user: null,
            accessToken: null,
            refreshToken: null,
            isAuthenticated: false,
            isLoading: false,
          });
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

        // Set hydration state
        setHasHydrated: (state) => {
          set({ _hasHydrated: state });
        },
      }),
      {
        name: 'auth-storage',
        storage: createJSONStorage(() => localStorage),
        partialize: (state) => ({
          user: state.user,
          accessToken: state.accessToken,
          refreshToken: state.refreshToken,
          isAuthenticated: state.isAuthenticated,
        }),
        onRehydrateStorage: () => (state) => {
          console.log('🔄 [AUTH STORE] Rehydration complete');

          // Restore auth token to API client
          if (state?.accessToken) {
            console.log('✅ [AUTH STORE] Restoring auth token to API client');
            apiClient.setAuthToken(state.accessToken);
          }

          // Mark as hydrated
          state?.setHasHydrated(true);
        },
      }
    ),
    { name: 'AuthStore' }
  )
);

// Selector to check if store has hydrated
export const useHasHydrated = () => useAuthStore((state) => state._hasHydrated);
