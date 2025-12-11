import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { LocationSection } from '@/presentation/components/features/profile/LocationSection';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';

// Mock stores
vi.mock('@/presentation/store/useAuthStore');
vi.mock('@/presentation/store/useProfileStore');

describe('LocationSection', () => {
  const mockUser = {
    userId: 'test-user-id',
    email: 'test@example.com',
    fullName: 'Test User',
    role: 'User',
  };

  const mockProfile = {
    id: 'test-user-id',
    email: 'test@example.com',
    firstName: 'Test',
    lastName: 'User',
    location: {
      city: 'Toronto',
      state: 'Ontario',
      zipCode: 'M5H 2N2',
      country: 'Canada',
    },
  };

  const mockUpdateLocation = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();

    (useAuthStore as any).mockReturnValue({
      user: mockUser,
      isAuthenticated: true,
    });

    (useProfileStore as any).mockReturnValue({
      profile: mockProfile,
      error: null,
      sectionStates: { location: 'idle' },
      updateLocation: mockUpdateLocation,
    });
  });

  describe('Rendering', () => {
    it('should render location section card', () => {
      render(<LocationSection />);
      expect(screen.getByRole('region', { name: /location/i })).toBeInTheDocument();
    });

    it('should display current location in view mode', () => {
      render(<LocationSection />);
      expect(screen.getByText(/Toronto/)).toBeInTheDocument();
    });
  });
});
