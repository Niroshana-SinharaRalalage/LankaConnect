import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import EventsPage from '../page';

// Mock all external dependencies
vi.mock('next/navigation', () => ({
  useRouter: vi.fn(() => ({ push: vi.fn() })),
}));

vi.mock('@/presentation/store/useAuthStore', () => ({
  useAuthStore: vi.fn(() => ({
    user: null,
    isAuthenticated: false,
  })),
}));

vi.mock('@/presentation/hooks/useGeolocation', () => ({
  useGeolocation: vi.fn(() => ({
    latitude: null,
    longitude: null,
    loading: false,
  })),
}));

vi.mock('@/presentation/hooks/useMetroAreas', () => ({
  useMetroAreas: vi.fn(() => ({
    metroAreasByState: new Map(),
    stateLevelMetros: [],
    isLoading: false,
  })),
}));

vi.mock('@/infrastructure/api/hooks/useReferenceData', () => ({
  useEventCategories: vi.fn(() => ({
    data: [],
    isLoading: false,
  })),
}));

vi.mock('@/presentation/hooks/useEvents', () => ({
  useEvents: vi.fn(),
  useUserRsvps: vi.fn(() => ({
    data: [],
    isLoading: false,
  })),
}));

import { useEvents } from '@/presentation/hooks/useEvents';

/**
 * Test Suite for Events Page - Error Handling Logic
 * Issue #79: Empty result scenarios should show "No Events Found", not error message
 *
 * This test suite specifically tests the error display logic to ensure:
 * 1. Empty results (no events) show "No Events Found" message
 * 2. Genuine API errors show error message
 * 3. Stale error states don't override empty result messages
 */
describe('EventsPage - Error Handling', () => {
  const mockUseEvents = useEvents as ReturnType<typeof vi.fn>;

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Empty Results Scenarios (Issue #79)', () => {
    it('should show "No Events Found" when API returns empty array with no error', () => {
      // Arrange: Mock successful API call with empty results
      mockUseEvents.mockReturnValue({
        data: [],
        isLoading: false,
        error: undefined,
      });

      // Act: Render the Events page
      render(<EventsPage />);

      // Assert: Should show "No Events Found" message
      expect(screen.getByText('No Events Found')).toBeInTheDocument();
      expect(screen.getByText(/Check back soon for new events!/i)).toBeInTheDocument();

      // Assert: Should NOT show error message
      expect(screen.queryByText(/Failed to load events/i)).not.toBeInTheDocument();
    });

    it('should show "No Events Found" even if there is a stale error state (Issue #79 - Main Bug)', () => {
      // Arrange: Mock empty array WITH a stale error (the bug scenario)
      // This simulates React Query not clearing error state when filters change
      mockUseEvents.mockReturnValue({
        data: [],
        isLoading: false,
        error: { message: 'Previous request failed' } as any,
      });

      // Act: Render the Events page
      render(<EventsPage />);

      // Assert: Should prioritize data check over error state
      // Should show "No Events Found" because we have a defined empty array
      expect(screen.getByText('No Events Found')).toBeInTheDocument();

      // Assert: Should NOT show error message when we have valid empty data
      expect(screen.queryByText(/Failed to load events/i)).not.toBeInTheDocument();
    });

    it('should show error message ONLY when there is no data AND an error', () => {
      // Arrange: Mock genuine error with no data
      mockUseEvents.mockReturnValue({
        data: undefined,
        isLoading: false,
        error: { message: 'Network error' } as any,
      });

      // Act: Render the Events page
      render(<EventsPage />);

      // Assert: Should show error message
      expect(screen.getByText(/Failed to load events/i)).toBeInTheDocument();

      // Assert: Should NOT show "No Events Found"
      expect(screen.queryByText('No Events Found')).not.toBeInTheDocument();
    });

    it('should show error message when data is null AND an error exists', () => {
      // Arrange: Mock error with null data
      mockUseEvents.mockReturnValue({
        data: null as any,
        isLoading: false,
        error: { message: 'API Error' } as any,
      });

      // Act: Render the Events page
      render(<EventsPage />);

      // Assert: Should show error message
      expect(screen.getByText(/Failed to load events/i)).toBeInTheDocument();
    });
  });

  describe('Loading State', () => {
    it('should show loading skeleton when isLoading is true', () => {
      // Arrange: Mock loading state
      mockUseEvents.mockReturnValue({
        data: undefined,
        isLoading: true,
        error: undefined,
      });

      // Act: Render the Events page
      render(<EventsPage />);

      // Assert: Should show loading skeletons (check for animate-pulse class)
      const loadingElements = screen.getAllByRole('generic').filter(
        el => el.className.includes('animate-pulse')
      );
      expect(loadingElements.length).toBeGreaterThan(0);

      // Assert: Should NOT show error or no events message
      expect(screen.queryByText(/Failed to load events/i)).not.toBeInTheDocument();
      expect(screen.queryByText('No Events Found')).not.toBeInTheDocument();
    });
  });

  describe('Success State with Events', () => {
    it('should display events grid when data is available', () => {
      // Arrange: Mock successful API call with events
      const mockEvents = [
        {
          id: '1',
          name: 'Test Event 1',
          description: 'Description 1',
          category: 0,
          startDate: new Date().toISOString(),
          endDate: new Date().toISOString(),
          isVirtual: false,
          registrationRequired: false,
          eventStatus: 1,
          organizer: { id: '1', fullName: 'Test Organizer', email: 'test@test.com' },
        },
      ];

      mockUseEvents.mockReturnValue({
        data: mockEvents,
        isLoading: false,
        error: undefined,
      });

      // Act: Render the Events page
      render(<EventsPage />);

      // Assert: Should display event card
      expect(screen.getByText('Test Event 1')).toBeInTheDocument();

      // Assert: Should NOT show error or no events message
      expect(screen.queryByText(/Failed to load events/i)).not.toBeInTheDocument();
      expect(screen.queryByText('No Events Found')).not.toBeInTheDocument();
    });
  });
});