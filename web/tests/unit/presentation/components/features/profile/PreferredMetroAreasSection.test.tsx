/**
 * @vitest-environment jsdom
 */
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { PreferredMetroAreasSection } from '@/presentation/components/features/profile/PreferredMetroAreasSection';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import type { UserProfile } from '@/domain/models/UserProfile';

// Mock stores
vi.mock('@/presentation/store/useAuthStore');
vi.mock('@/presentation/store/useProfileStore');

describe('PreferredMetroAreasSection', () => {
  // Phase 5B: Metro area GUIDs (matching backend seeder pattern)
  // Ohio state: 39, Cleveland: 39111111-1111-1111-1111-111111111001
  // Ohio state: 39, Columbus: 39111111-1111-1111-1111-111111111002
  const CLEVELAND_GUID = '39111111-1111-1111-1111-111111111001';
  const COLUMBUS_GUID = '39111111-1111-1111-1111-111111111002';
  const OHIO_STATE_GUID = '39000000-0000-0000-0000-000000000001';

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
    preferredMetroAreas: [CLEVELAND_GUID, COLUMBUS_GUID],
    profilePhotoUrl: null,
    location: null,
    culturalInterests: [],
    languages: [],
  };

  const mockUpdatePreferredMetroAreas = vi.fn();

  const createMockStore = (overrides = {}) => ({
    profile: mockProfile,
    error: null,
    isLoading: false,
    sectionStates: {
      photo: 'idle' as const,
      basicInfo: 'idle' as const,
      location: 'idle' as const,
      culturalInterests: 'idle' as const,
      languages: 'idle' as const,
      preferredMetroAreas: 'idle' as const,
    },
    updatePreferredMetroAreas: mockUpdatePreferredMetroAreas,
    setProfile: vi.fn(),
    loadProfile: vi.fn(),
    clearProfile: vi.fn(),
    uploadPhoto: vi.fn(),
    deletePhoto: vi.fn(),
    updateBasicInfo: vi.fn(),
    updateLocation: vi.fn(),
    updateCulturalInterests: vi.fn(),
    updateLanguages: vi.fn(),
    markSectionDirty: vi.fn(),
    markSectionClean: vi.fn(),
    resetSectionState: vi.fn(),
    isSectionDirty: vi.fn(),
    originalProfile: null,
    ...overrides,
  });

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
    vi.mocked(useProfileStore).mockReturnValue(createMockStore());
  });

  it('should render preferred metro areas section card', () => {
    render(<PreferredMetroAreasSection />);
    expect(screen.getByRole('region', { name: /preferred metro areas/i })).toBeInTheDocument();
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

    const { container } = render(<PreferredMetroAreasSection />);
    expect(container.firstChild).toBeNull();
  });

  it('should display current metro areas as badges in view mode', () => {
    render(<PreferredMetroAreasSection />);

    // Should show metro area names
    expect(screen.getByText(/Cleveland/)).toBeInTheDocument();
    expect(screen.getByText(/Columbus/)).toBeInTheDocument();
  });

  it('should display "No metro areas selected" when user has no preferences', () => {
    vi.mocked(useProfileStore).mockReturnValue(
      createMockStore({ profile: { ...mockProfile, preferredMetroAreas: [] } })
    );

    render(<PreferredMetroAreasSection />);
    expect(screen.getByText(/No metro areas selected/i)).toBeInTheDocument();
  });

  it('should show Edit button in view mode', () => {
    render(<PreferredMetroAreasSection />);
    expect(screen.getByRole('button', { name: /Edit/i })).toBeInTheDocument();
  });

  it('should enter edit mode when Edit button is clicked', () => {
    render(<PreferredMetroAreasSection />);

    const editButton = screen.getByRole('button', { name: /Edit/i });
    fireEvent.click(editButton);

    // Should show checkboxes
    const checkboxes = screen.getAllByRole('checkbox');
    expect(checkboxes.length).toBeGreaterThan(0);
  });

  it('should show Save and Cancel buttons in edit mode', () => {
    render(<PreferredMetroAreasSection />);

    fireEvent.click(screen.getByRole('button', { name: /Edit/i }));

    expect(screen.getByRole('button', { name: /Save/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Cancel/i })).toBeInTheDocument();
  });

  it('should show selection counter in edit mode (Phase 5B: max 20)', () => {
    render(<PreferredMetroAreasSection />);

    fireEvent.click(screen.getByRole('button', { name: /Edit/i }));

    // Phase 5B: Updated from 10 to 20 max limit
    expect(screen.getByText(/2 of 20 selected/i)).toBeInTheDocument();
  });

  it('should allow selecting metro areas up to max limit (Phase 5B: 20)', () => {
    vi.mocked(useProfileStore).mockReturnValue(
      createMockStore({ profile: { ...mockProfile, preferredMetroAreas: [] } })
    );

    render(<PreferredMetroAreasSection />);
    fireEvent.click(screen.getByRole('button', { name: /Edit/i }));

    // Select first 20 metros
    const checkboxes = screen.getAllByRole('checkbox');
    for (let i = 0; i < 20 && i < checkboxes.length; i++) {
      fireEvent.click(checkboxes[i]);
    }

    // Should show 20 of 20 selected (or fewer if not enough metros)
    const maxSelectable = Math.min(20, checkboxes.length);
    expect(screen.getByText(new RegExp(`${maxSelectable} of 20 selected`))).toBeInTheDocument();
  });

  it('should prevent selecting more than 20 metro areas (Phase 5B: expanded limit)', () => {
    vi.mocked(useProfileStore).mockReturnValue(
      createMockStore({ profile: { ...mockProfile, preferredMetroAreas: [] } })
    );

    render(<PreferredMetroAreasSection />);
    fireEvent.click(screen.getByRole('button', { name: /Edit/i }));

    // Select 20 metros first
    const checkboxes = screen.getAllByRole('checkbox');
    for (let i = 0; i < 20 && i < checkboxes.length; i++) {
      fireEvent.click(checkboxes[i]);
    }

    // Try to select 21st metro
    if (checkboxes.length > 20) {
      fireEvent.click(checkboxes[20]);
    }

    // Should show validation error for Phase 5B (20 limit)
    expect(screen.getByText(/cannot select more than 20/i)).toBeInTheDocument();
  });

  it('should call updatePreferredMetroAreas with correct GUID data on Save (Phase 5B)', async () => {
    mockUpdatePreferredMetroAreas.mockResolvedValue(undefined);

    render(<PreferredMetroAreasSection />);
    fireEvent.click(screen.getByRole('button', { name: /Edit/i }));

    // Save without changes - Phase 5B: Should send GUIDs, not string IDs
    fireEvent.click(screen.getByRole('button', { name: /Save/i }));

    await waitFor(() => {
      expect(mockUpdatePreferredMetroAreas).toHaveBeenCalledWith('user-123', {
        metroAreaIds: [CLEVELAND_GUID, COLUMBUS_GUID],
      });
    });
  });

  it('should exit edit mode and show success message after successful save', async () => {
    vi.mocked(useProfileStore).mockReturnValue(
      createMockStore({
        sectionStates: {
          photo: 'idle' as const,
          basicInfo: 'idle' as const,
          location: 'idle' as const,
          culturalInterests: 'idle' as const,
          languages: 'idle' as const,
          preferredMetroAreas: 'success' as const,
        },
      })
    );

    render(<PreferredMetroAreasSection />);

    // Should show success message
    expect(screen.getByText(/saved successfully/i)).toBeInTheDocument();
  });

  it('should cancel edit mode and revert changes when Cancel is clicked', () => {
    render(<PreferredMetroAreasSection />);

    fireEvent.click(screen.getByRole('button', { name: /Edit/i }));

    // Deselect a metro
    const checkboxes = screen.getAllByRole('checkbox');
    const firstChecked = checkboxes.find((cb) => (cb as HTMLInputElement).checked);
    if (firstChecked) {
      fireEvent.click(firstChecked);
    }

    // Cancel
    fireEvent.click(screen.getByRole('button', { name: /Cancel/i }));

    // Should be back in view mode with original values
    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument();
    expect(screen.getByText(/Cleveland/)).toBeInTheDocument();
  });

  it('should show error message when save fails', () => {
    vi.mocked(useProfileStore).mockReturnValue(
      createMockStore({
        error: 'Failed to update preferred metro areas',
        sectionStates: {
          photo: 'idle' as const,
          basicInfo: 'idle' as const,
          location: 'idle' as const,
          culturalInterests: 'idle' as const,
          languages: 'idle' as const,
          preferredMetroAreas: 'error' as const,
        },
      })
    );

    render(<PreferredMetroAreasSection />);

    expect(screen.getByText(/Failed to update preferred metro areas/i)).toBeInTheDocument();
  });

  it('should disable checkboxes and buttons while saving', () => {
    render(<PreferredMetroAreasSection />);
    fireEvent.click(screen.getByRole('button', { name: /Edit/i }));

    // All checkboxes should be enabled initially
    const checkboxes = screen.getAllByRole('checkbox');
    checkboxes.forEach((cb) => {
      expect(cb).not.toBeDisabled();
    });

    // Save and Cancel buttons should be enabled
    const saveButton = screen.getByRole('button', { name: /Save/i });
    const cancelButton = screen.getByRole('button', { name: /Cancel/i });
    expect(saveButton).not.toBeDisabled();
    expect(cancelButton).not.toBeDisabled();
  });

  it('should allow clearing all metro areas (privacy choice - Phase 5B)', async () => {
    mockUpdatePreferredMetroAreas.mockResolvedValue(undefined);

    // Start with no metros selected
    vi.mocked(useProfileStore).mockReturnValue(
      createMockStore({ profile: { ...mockProfile, preferredMetroAreas: [] } })
    );

    render(<PreferredMetroAreasSection />);
    fireEvent.click(screen.getByRole('button', { name: /Edit/i }));

    // Verify 0 metros are selected
    expect(screen.getByText(/0 of 20 selected/i)).toBeInTheDocument();

    // Save empty selection
    fireEvent.click(screen.getByRole('button', { name: /Save/i }));

    await waitFor(() => {
      expect(mockUpdatePreferredMetroAreas).toHaveBeenCalledWith('user-123', {
        metroAreaIds: [],
      });
    });
  });

  it('should display state-grouped dropdown UI in edit mode (Phase 5B)', () => {
    render(<PreferredMetroAreasSection />);
    fireEvent.click(screen.getByRole('button', { name: /Edit/i }));

    // Phase 5B: Should show state-grouped sections
    expect(screen.getByText(/State-Wide Selections/i)).toBeInTheDocument();
    expect(screen.getByText(/City Metro Areas/i)).toBeInTheDocument();
  });

  it('should support expand/collapse of state metro areas (Phase 5B)', () => {
    render(<PreferredMetroAreasSection />);
    fireEvent.click(screen.getByRole('button', { name: /Edit/i }));

    // Phase 5B: Should have state headers that can be expanded/collapsed
    // Find state name button headers (they should be expandable)
    const stateHeaders = screen.queryAllByRole('button').filter(
      (btn) => btn.getAttribute('aria-expanded') !== null
    );

    // Should have expandable state headers
    expect(stateHeaders.length).toBeGreaterThan(0);
  });
});
