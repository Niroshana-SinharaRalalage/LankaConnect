import { create } from 'zustand';
import { devtools } from 'zustand/middleware';
import type {
  UserProfile,
  Location,
  Language,
  UpdateLocationRequest,
  UpdateCulturalInterestsRequest,
  UpdateLanguagesRequest,
  UpdateBasicInfoRequest,
  UpdatePreferredMetroAreasRequest,
} from '@/domain/models/UserProfile';
import { profileRepository } from '@/infrastructure/api/repositories/profile.repository';

/**
 * Save button states for different profile sections
 */
export type SaveButtonState = 'idle' | 'dirty' | 'saving' | 'success' | 'error';

interface ProfileSectionStates {
  photo: SaveButtonState;
  basicInfo: SaveButtonState;
  location: SaveButtonState;
  culturalInterests: SaveButtonState;
  languages: SaveButtonState;
  preferredMetroAreas: SaveButtonState; // Phase 5B
}

interface ProfileState {
  // State
  profile: UserProfile | null;
  originalProfile: UserProfile | null; // For dirty tracking
  isLoading: boolean;
  error: string | null;
  sectionStates: ProfileSectionStates;

  // Actions - Data Management
  setProfile: (profile: UserProfile) => void;
  loadProfile: (userId: string) => Promise<void>;
  clearProfile: () => void;

  // Actions - Photo
  uploadPhoto: (userId: string, file: File) => Promise<void>;
  deletePhoto: (userId: string) => Promise<void>;

  // Actions - Basic Info
  updateBasicInfo: (userId: string, basicInfo: UpdateBasicInfoRequest) => Promise<void>;

  // Actions - Location
  updateLocation: (userId: string, location: UpdateLocationRequest) => Promise<void>;

  // Actions - Cultural Interests
  updateCulturalInterests: (
    userId: string,
    interests: UpdateCulturalInterestsRequest
  ) => Promise<void>;

  // Actions - Languages
  updateLanguages: (userId: string, languages: UpdateLanguagesRequest) => Promise<void>;

  // Actions - Preferred Metro Areas (Phase 5B)
  updatePreferredMetroAreas: (
    userId: string,
    metroAreas: UpdatePreferredMetroAreasRequest
  ) => Promise<void>;

  // Actions - Dirty Tracking
  markSectionDirty: (section: keyof ProfileSectionStates) => void;
  markSectionClean: (section: keyof ProfileSectionStates) => void;
  resetSectionState: (section: keyof ProfileSectionStates) => void;
  isSectionDirty: (section: keyof ProfileSectionStates) => boolean;
}

const initialSectionStates: ProfileSectionStates = {
  photo: 'idle',
  basicInfo: 'idle',
  location: 'idle',
  culturalInterests: 'idle',
  languages: 'idle',
  preferredMetroAreas: 'idle', // Phase 5B
};

/**
 * Profile Store
 * Global state management for user profile using Zustand
 * Separate from auth store per ADR-002
 */
export const useProfileStore = create<ProfileState>()(
  devtools(
    (set, get) => ({
      // Initial state
      profile: null,
      originalProfile: null,
      isLoading: false,
      error: null,
      sectionStates: { ...initialSectionStates },

      // Set profile
      setProfile: (profile) => {
        set({
          profile,
          originalProfile: JSON.parse(JSON.stringify(profile)), // Deep clone
          error: null,
        });
      },

      // Load profile from API
      loadProfile: async (userId) => {
        set({ isLoading: true, error: null });
        try {
          const profile = await profileRepository.getProfile(userId);
          get().setProfile(profile);
        } catch (error) {
          const errorMessage =
            error instanceof Error ? error.message : 'Failed to load profile';
          set({ error: errorMessage, profile: null, originalProfile: null });
        } finally {
          set({ isLoading: false });
        }
      },

      // Clear profile
      clearProfile: () => {
        set({
          profile: null,
          originalProfile: null,
          isLoading: false,
          error: null,
          sectionStates: { ...initialSectionStates },
        });
      },

      // Upload profile photo
      uploadPhoto: async (userId, file) => {
        set((state) => ({
          sectionStates: { ...state.sectionStates, photo: 'saving' },
          error: null,
        }));

        try {
          const response = await profileRepository.uploadProfilePhoto(userId, file);

          // Update profile with new photo URL
          set((state) => ({
            profile: state.profile
              ? { ...state.profile, profilePhotoUrl: response.profilePhotoUrl }
              : null,
            originalProfile: state.originalProfile
              ? { ...state.originalProfile, profilePhotoUrl: response.profilePhotoUrl }
              : null,
            sectionStates: { ...state.sectionStates, photo: 'success' },
          }));

          // Reset to idle after 2 seconds
          setTimeout(() => {
            set((state) => ({
              sectionStates: { ...state.sectionStates, photo: 'idle' },
            }));
          }, 2000);
        } catch (error) {
          const errorMessage =
            error instanceof Error ? error.message : 'Failed to upload photo';
          set((state) => ({
            error: errorMessage,
            sectionStates: { ...state.sectionStates, photo: 'error' },
          }));
        }
      },

      // Delete profile photo
      deletePhoto: async (userId) => {
        set((state) => ({
          sectionStates: { ...state.sectionStates, photo: 'saving' },
          error: null,
        }));

        try {
          await profileRepository.deleteProfilePhoto(userId);

          // Update profile to remove photo URL
          set((state) => ({
            profile: state.profile ? { ...state.profile, profilePhotoUrl: null } : null,
            originalProfile: state.originalProfile
              ? { ...state.originalProfile, profilePhotoUrl: null }
              : null,
            sectionStates: { ...state.sectionStates, photo: 'success' },
          }));

          // Reset to idle after 2 seconds
          setTimeout(() => {
            set((state) => ({
              sectionStates: { ...state.sectionStates, photo: 'idle' },
            }));
          }, 2000);
        } catch (error) {
          const errorMessage =
            error instanceof Error ? error.message : 'Failed to delete photo';
          set((state) => ({
            error: errorMessage,
            sectionStates: { ...state.sectionStates, photo: 'error' },
          }));
        }
      },

      // Update basic info
      updateBasicInfo: async (userId, basicInfo) => {
        set((state) => ({
          sectionStates: { ...state.sectionStates, basicInfo: 'saving' },
          error: null,
        }));

        try {
          const updatedProfile = await profileRepository.updateBasicInfo(userId, basicInfo);
          get().setProfile(updatedProfile);

          set((state) => ({
            sectionStates: { ...state.sectionStates, basicInfo: 'success' },
          }));

          // Reset to idle after 2 seconds
          setTimeout(() => {
            set((state) => ({
              sectionStates: { ...state.sectionStates, basicInfo: 'idle' },
            }));
          }, 2000);
        } catch (error) {
          const errorMessage =
            error instanceof Error ? error.message : 'Failed to update basic info';
          set((state) => ({
            error: errorMessage,
            sectionStates: { ...state.sectionStates, basicInfo: 'error' },
          }));
        }
      },

      // Update location
      updateLocation: async (userId, location) => {
        set((state) => ({
          sectionStates: { ...state.sectionStates, location: 'saving' },
          error: null,
        }));

        try {
          const updatedProfile = await profileRepository.updateLocation(userId, location);
          get().setProfile(updatedProfile);

          set((state) => ({
            sectionStates: { ...state.sectionStates, location: 'success' },
          }));

          // Reset to idle after 2 seconds
          setTimeout(() => {
            set((state) => ({
              sectionStates: { ...state.sectionStates, location: 'idle' },
            }));
          }, 2000);
        } catch (error) {
          const errorMessage =
            error instanceof Error ? error.message : 'Failed to update location';
          set((state) => ({
            error: errorMessage,
            sectionStates: { ...state.sectionStates, location: 'error' },
          }));
        }
      },

      // Update cultural interests
      updateCulturalInterests: async (userId, interests) => {
        set((state) => ({
          sectionStates: { ...state.sectionStates, culturalInterests: 'saving' },
          error: null,
        }));

        try {
          const updatedProfile = await profileRepository.updateCulturalInterests(
            userId,
            interests
          );
          get().setProfile(updatedProfile);

          set((state) => ({
            sectionStates: { ...state.sectionStates, culturalInterests: 'success' },
          }));

          // Reset to idle after 2 seconds
          setTimeout(() => {
            set((state) => ({
              sectionStates: { ...state.sectionStates, culturalInterests: 'idle' },
            }));
          }, 2000);
        } catch (error) {
          const errorMessage =
            error instanceof Error ? error.message : 'Failed to update cultural interests';
          set((state) => ({
            error: errorMessage,
            sectionStates: { ...state.sectionStates, culturalInterests: 'error' },
          }));
        }
      },

      // Update languages
      updateLanguages: async (userId, languages) => {
        set((state) => ({
          sectionStates: { ...state.sectionStates, languages: 'saving' },
          error: null,
        }));

        try {
          const updatedProfile = await profileRepository.updateLanguages(userId, languages);
          get().setProfile(updatedProfile);

          set((state) => ({
            sectionStates: { ...state.sectionStates, languages: 'success' },
          }));

          // Reset to idle after 2 seconds
          setTimeout(() => {
            set((state) => ({
              sectionStates: { ...state.sectionStates, languages: 'idle' },
            }));
          }, 2000);
        } catch (error) {
          const errorMessage =
            error instanceof Error ? error.message : 'Failed to update languages';
          set((state) => ({
            error: errorMessage,
            sectionStates: { ...state.sectionStates, languages: 'error' },
          }));
        }
      },

      // Phase 5B: Update user's preferred metro areas for location-based filtering
      // Validates against max 20 limit (expanded from 10)
      updatePreferredMetroAreas: async (userId, metroAreas) => {
        // Frontend validation: Check max 20 limit
        // Phase 6A.9 FIX: Property name changed to PascalCase to match backend
        if (metroAreas.MetroAreaIds.length > 20) {
          set((state) => ({
            error: 'Cannot select more than 20 metro areas',
            sectionStates: { ...state.sectionStates, preferredMetroAreas: 'error' },
          }));
          return;
        }

        set((state) => ({
          sectionStates: { ...state.sectionStates, preferredMetroAreas: 'saving' },
          error: null,
        }));

        try {
          const updatedProfile = await profileRepository.updatePreferredMetroAreas(
            userId,
            metroAreas
          );
          get().setProfile(updatedProfile);

          set((state) => ({
            sectionStates: { ...state.sectionStates, preferredMetroAreas: 'success' },
          }));

          // Reset to idle after 2 seconds
          setTimeout(() => {
            set((state) => ({
              sectionStates: { ...state.sectionStates, preferredMetroAreas: 'idle' },
            }));
          }, 2000);
        } catch (error) {
          const errorMessage =
            error instanceof Error ? error.message : 'Failed to update preferred metro areas';
          set((state) => ({
            error: errorMessage,
            sectionStates: { ...state.sectionStates, preferredMetroAreas: 'error' },
          }));
        }
      },

      // Mark section as dirty (has unsaved changes)
      markSectionDirty: (section) => {
        set((state) => ({
          sectionStates: { ...state.sectionStates, [section]: 'dirty' },
        }));
      },

      // Mark section as clean (no unsaved changes)
      markSectionClean: (section) => {
        set((state) => ({
          sectionStates: { ...state.sectionStates, [section]: 'idle' },
        }));
      },

      // Reset section state to idle
      resetSectionState: (section) => {
        set((state) => ({
          sectionStates: { ...state.sectionStates, [section]: 'idle' },
        }));
      },

      // Check if section has unsaved changes
      isSectionDirty: (section) => {
        return get().sectionStates[section] === 'dirty';
      },
    }),
    { name: 'ProfileStore' }
  )
);
