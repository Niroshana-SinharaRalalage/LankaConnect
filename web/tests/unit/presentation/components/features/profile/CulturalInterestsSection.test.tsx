/**
 * @vitest-environment jsdom
 */
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { CulturalInterestsSection } from '@/presentation/components/features/profile/CulturalInterestsSection';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import type { UserProfile } from '@/domain/models/UserProfile';

// Mock stores
vi.mock('@/presentation/store/useAuthStore');
vi.mock('@/presentation/store/useProfileStore');

describe('CulturalInterestsSection', () => {
  const mockUser = {
    userId: 'user-123',
    email: 'test@example.com',
    fullName: 'Test User',
  };

  const mockProfile: UserProfile = {
    id: 'user-123',
    firstName: 'Test',
    lastName: 'User',
    email: 'test@example.com',
    culturalInterests: ['SL_CUISINE', 'BUDDHIST_FEST'],
    profilePhotoUrl: null,
    location: null,
    languages: [],
  };

  const mockUpdateCulturalInterests = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();

    // Mock useAuthStore
    vi.mocked(useAuthStore).mockReturnValue({
      user: mockUser,
      isAuthenticated: true,
      token: 'mock-token',
      setAuth: vi.fn(),
      clearAuth: vi.fn(),
      isLoading: false,
      error: null,
    });

    // Mock useProfileStore
    vi.mocked(useProfileStore).mockReturnValue({
      profile: mockProfile,
      error: null,
      isLoading: false,
      sectionStates: {
        photo: 'idle',
        basicInfo: 'idle',
        location: 'idle',
        culturalInterests: 'idle',
        languages: 'idle',
      },
      updateCulturalInterests: mockUpdateCulturalInterests,
      setProfile: vi.fn(),
      loadProfile: vi.fn(),
      clearProfile: vi.fn(),
      uploadPhoto: vi.fn(),
      deletePhoto: vi.fn(),
      updateBasicInfo: vi.fn(),
      updateLocation: vi.fn(),
      updateLanguages: vi.fn(),
      markSectionDirty: vi.fn(),
      markSectionClean: vi.fn(),
      resetSectionState: vi.fn(),
      isSectionDirty: vi.fn(),
      originalProfile: null,
    });
  });

  it('should render cultural interests section card', () => {
    render(<CulturalInterestsSection />);
    expect(screen.getByRole('region', { name: /cultural interests/i })).toBeInTheDocument();
  });

  it('should not render if user is not authenticated', () => {
    vi.mocked(useAuthStore).mockReturnValue({
      user: null,
      isAuthenticated: false,
      token: null,
      setAuth: vi.fn(),
      clearAuth: vi.fn(),
      isLoading: false,
      error: null,
    });

    const { container } = render(<CulturalInterestsSection />);
    expect(container.firstChild).toBeNull();
  });

  it('should display current interests as badges in view mode', () => {
    render(<CulturalInterestsSection />);

    expect(screen.getByText('Sri Lankan Cuisine')).toBeInTheDocument();
    expect(screen.getByText('Buddhist Festivals & Traditions')).toBeInTheDocument();
  });

  it('should show message when no interests are selected', () => {
    vi.mocked(useProfileStore).mockReturnValue({
      profile: { ...mockProfile, culturalInterests: [] },
      error: null,
      isLoading: false,
      sectionStates: {
        photo: 'idle',
        basicInfo: 'idle',
        location: 'idle',
        culturalInterests: 'idle',
        languages: 'idle',
      },
      updateCulturalInterests: mockUpdateCulturalInterests,
      setProfile: vi.fn(),
      loadProfile: vi.fn(),
      clearProfile: vi.fn(),
      uploadPhoto: vi.fn(),
      deletePhoto: vi.fn(),
      updateBasicInfo: vi.fn(),
      updateLocation: vi.fn(),
      updateLanguages: vi.fn(),
      markSectionDirty: vi.fn(),
      markSectionClean: vi.fn(),
      resetSectionState: vi.fn(),
      isSectionDirty: vi.fn(),
      originalProfile: null,
    });

    render(<CulturalInterestsSection />);
    expect(screen.getByText(/no interests selected/i)).toBeInTheDocument();
  });

  it('should enter edit mode when Edit button is clicked', () => {
    render(<CulturalInterestsSection />);

    const editButton = screen.getByRole('button', { name: /edit/i });
    fireEvent.click(editButton);

    // Should show checkboxes in edit mode
    expect(screen.getByLabelText('Sri Lankan Cuisine')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /save interests/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument();
  });

  it('should cancel edit mode when Cancel button is clicked', () => {
    render(<CulturalInterestsSection />);

    const editButton = screen.getByRole('button', { name: /edit/i });
    fireEvent.click(editButton);

    const cancelButton = screen.getByRole('button', { name: /cancel/i });
    fireEvent.click(cancelButton);

    // Should return to view mode
    expect(screen.queryByLabelText('Sri Lankan Cuisine')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: /edit/i })).toBeInTheDocument();
  });

  it('should show success message after successful save', () => {
    vi.mocked(useProfileStore).mockReturnValue({
      profile: mockProfile,
      error: null,
      isLoading: false,
      sectionStates: {
        photo: 'idle',
        basicInfo: 'idle',
        location: 'idle',
        culturalInterests: 'success',
        languages: 'idle',
      },
      updateCulturalInterests: mockUpdateCulturalInterests,
      setProfile: vi.fn(),
      loadProfile: vi.fn(),
      clearProfile: vi.fn(),
      uploadPhoto: vi.fn(),
      deletePhoto: vi.fn(),
      updateBasicInfo: vi.fn(),
      updateLocation: vi.fn(),
      updateLanguages: vi.fn(),
      markSectionDirty: vi.fn(),
      markSectionClean: vi.fn(),
      resetSectionState: vi.fn(),
      isSectionDirty: vi.fn(),
      originalProfile: null,
    });

    render(<CulturalInterestsSection />);
    expect(screen.getByText(/cultural interests saved successfully/i)).toBeInTheDocument();
  });

  it('should show error message when save fails', () => {
    const errorMessage = 'Failed to update interests';
    vi.mocked(useProfileStore).mockReturnValue({
      profile: mockProfile,
      error: errorMessage,
      isLoading: false,
      sectionStates: {
        photo: 'idle',
        basicInfo: 'idle',
        location: 'idle',
        culturalInterests: 'error',
        languages: 'idle',
      },
      updateCulturalInterests: mockUpdateCulturalInterests,
      setProfile: vi.fn(),
      loadProfile: vi.fn(),
      clearProfile: vi.fn(),
      uploadPhoto: vi.fn(),
      deletePhoto: vi.fn(),
      updateBasicInfo: vi.fn(),
      updateLocation: vi.fn(),
      updateLanguages: vi.fn(),
      markSectionDirty: vi.fn(),
      markSectionClean: vi.fn(),
      resetSectionState: vi.fn(),
      isSectionDirty: vi.fn(),
      originalProfile: null,
    });

    render(<CulturalInterestsSection />);
    expect(screen.getByText(errorMessage)).toBeInTheDocument();
  });
});
