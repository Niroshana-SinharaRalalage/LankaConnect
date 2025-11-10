import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MetroAreaSelector } from '@/presentation/components/features/location/MetroAreaSelector';
import { MetroArea } from '@/domain/models/MetroArea';
import { UserLocation } from '@/domain/models/Location';

// Mock the distance calculation utility
vi.mock('@/presentation/utils/distance', () => ({
  calculateDistance: vi.fn((lat1, lon1, lat2, lon2) => {
    // Simple mock calculation - return distance based on latitude difference
    const latDiff = Math.abs(lat1 - lat2);
    return latDiff * 69; // Rough miles per degree latitude
  }),
}));

/**
 * Test Suite for MetroAreaSelector Component
 * Tests dropdown rendering, geolocation, permission handling, sorting, badges, and persistence
 */
describe('MetroAreaSelector Component', () => {
  const mockOnChange = vi.fn();
  const mockOnDetectLocation = vi.fn();

  const mockMetros: readonly MetroArea[] = [
    {
      id: 'metro-la',
      name: 'Los Angeles',
      state: 'CA',
      centerLat: 34.0522,
      centerLng: -118.2437,
      cities: ['Los Angeles', 'Long Beach', 'Glendale'],
      radiusMiles: 50,
    },
    {
      id: 'metro-nyc',
      name: 'New York',
      state: 'NY',
      centerLat: 40.7128,
      centerLng: -74.0060,
      cities: ['New York', 'Brooklyn', 'Queens'],
      radiusMiles: 50,
    },
    {
      id: 'metro-chi',
      name: 'Chicago',
      state: 'IL',
      centerLat: 41.8781,
      centerLng: -87.6298,
      cities: ['Chicago', 'Naperville', 'Joliet'],
      radiusMiles: 50,
    },
    {
      id: 'metro-sf',
      name: 'San Francisco',
      state: 'CA',
      centerLat: 37.7749,
      centerLng: -122.4194,
      cities: ['San Francisco', 'Oakland', 'San Jose'],
      radiusMiles: 50,
    },
  ] as const;

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Dropdown Rendering', () => {
    it('should render dropdown select element', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
        />
      );
      const select = screen.getByLabelText('Metro area selector');
      expect(select).toBeInTheDocument();
    });

    it('should render placeholder option', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
        />
      );
      expect(screen.getByText('Select your metro area')).toBeInTheDocument();
    });

    it('should render custom placeholder', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          placeholder="Choose a city"
        />
      );
      expect(screen.getByText('Choose a city')).toBeInTheDocument();
    });

    it('should render all metro areas', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
        />
      );
      expect(screen.getByText('Los Angeles, CA')).toBeInTheDocument();
      expect(screen.getByText('New York, NY')).toBeInTheDocument();
      expect(screen.getByText('Chicago, IL')).toBeInTheDocument();
      expect(screen.getByText('San Francisco, CA')).toBeInTheDocument();
    });

    it('should have sr-only label for accessibility', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
        />
      );
      const label = screen.getByLabelText('Select Metro Area');
      expect(label).toBeInTheDocument();
    });

    it('should render MapPin icon', () => {
      const { container } = render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
        />
      );
      const icon = container.querySelector('.w-5.h-5');
      expect(icon).toBeInTheDocument();
    });
  });

  describe('Selection Behavior', () => {
    it('should call onChange when metro is selected', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
        />
      );
      const select = screen.getByLabelText('Metro area selector');
      fireEvent.change(select, { target: { value: 'metro-la' } });
      expect(mockOnChange).toHaveBeenCalledWith('metro-la');
    });

    it('should display selected metro', () => {
      render(
        <MetroAreaSelector
          value="metro-nyc"
          metros={mockMetros}
          onChange={mockOnChange}
        />
      );
      expect(screen.getByText('New York, NY')).toBeInTheDocument();
    });

    it('should call onChange with null when placeholder is selected', () => {
      render(
        <MetroAreaSelector
          value="metro-la"
          metros={mockMetros}
          onChange={mockOnChange}
        />
      );
      const select = screen.getByLabelText('Metro area selector');
      fireEvent.change(select, { target: { value: '' } });
      expect(mockOnChange).toHaveBeenCalledWith(null);
    });

    it('should be disabled when disabled prop is true', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          disabled={true}
        />
      );
      const select = screen.getByLabelText('Metro area selector');
      expect(select).toBeDisabled();
    });
  });

  describe('Geolocation Detection', () => {
    it('should render "Detect My Location" button when onDetectLocation provided', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          onDetectLocation={mockOnDetectLocation}
        />
      );
      expect(screen.getByText('Detect My Location')).toBeInTheDocument();
    });

    it('should not render detect button when onDetectLocation not provided', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
        />
      );
      expect(screen.queryByText('Detect My Location')).not.toBeInTheDocument();
    });

    it('should call onDetectLocation when button is clicked', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          onDetectLocation={mockOnDetectLocation}
        />
      );
      const button = screen.getByText('Detect My Location');
      fireEvent.click(button);
      expect(mockOnDetectLocation).toHaveBeenCalled();
    });

    it('should show loading state during detection', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          onDetectLocation={mockOnDetectLocation}
          isDetecting={true}
        />
      );
      expect(screen.getByText('Detecting Location...')).toBeInTheDocument();
    });

    it('should disable button during detection', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          onDetectLocation={mockOnDetectLocation}
          isDetecting={true}
        />
      );
      const button = screen.getByText('Detecting Location...');
      expect(button).toBeDisabled();
    });

    it('should disable select during detection', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          onDetectLocation={mockOnDetectLocation}
          isDetecting={true}
        />
      );
      const select = screen.getByLabelText('Metro area selector');
      expect(select).toBeDisabled();
    });

    it('should not call onDetectLocation when already detecting', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          onDetectLocation={mockOnDetectLocation}
          isDetecting={true}
        />
      );
      const button = screen.getByText('Detecting Location...');
      fireEvent.click(button);
      expect(mockOnDetectLocation).not.toHaveBeenCalled();
    });
  });

  describe('Permission Denied Handling', () => {
    it('should display error message when detection fails', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          onDetectLocation={mockOnDetectLocation}
          detectionError="Location permission denied"
        />
      );
      expect(screen.getByText('Location permission denied')).toBeInTheDocument();
    });

    it('should have proper role="alert" on error message', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          onDetectLocation={mockOnDetectLocation}
          detectionError="Location permission denied"
        />
      );
      const errorElement = screen.getByRole('alert');
      expect(errorElement).toBeInTheDocument();
    });

    it('should link error to select with aria-describedby', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          onDetectLocation={mockOnDetectLocation}
          detectionError="Location permission denied"
        />
      );
      const select = screen.getByLabelText('Metro area selector');
      expect(select).toHaveAttribute('aria-describedby', 'location-error');
    });

    it('should not show error when detectionError is null', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          onDetectLocation={mockOnDetectLocation}
          detectionError={null}
        />
      );
      const errorElement = screen.queryByRole('alert');
      expect(errorElement).toBeNull();
    });
  });

  describe('Sorting by Distance', () => {
    const userLocation: UserLocation = {
      latitude: 34.0,
      longitude: -118.0,
      accuracy: 10,
      timestamp: new Date(),
    };

    it('should sort metros by distance when location provided', () => {
      const { container } = render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          userLocation={userLocation}
        />
      );
      const select = container.querySelector('select') as HTMLSelectElement;
      const options = Array.from(select.options).slice(1); // Skip placeholder

      // LA should be first (closest to user location)
      expect(options[0].text).toContain('Los Angeles');
    });

    it('should display distance in miles for each metro', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          userLocation={userLocation}
        />
      );
      // Check that distance indicators are present
      const optionsText = screen.getByLabelText('Metro area selector').innerHTML;
      expect(optionsText).toMatch(/\d+ mi/);
    });

    it('should not display distance when userLocation not provided', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
        />
      );
      const optionsText = screen.getByLabelText('Metro area selector').innerHTML;
      expect(optionsText).not.toMatch(/\d+ mi/);
    });

    it('should show location detected success message', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          onDetectLocation={mockOnDetectLocation}
          userLocation={userLocation}
        />
      );
      expect(screen.getByText('Location detected! Metros sorted by distance.')).toBeInTheDocument();
    });
  });

  describe('"Nearby" Badge', () => {
    const closeUserLocation: UserLocation = {
      latitude: 34.0522, // Very close to LA
      longitude: -118.2437,
      accuracy: 10,
      timestamp: new Date(),
    };

    it('should display "Nearby" badge for metros within 50 miles', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          userLocation={closeUserLocation}
        />
      );
      const optionsText = screen.getByLabelText('Metro area selector').innerHTML;
      expect(optionsText).toContain('Nearby');
    });

    it('should not display "Nearby" for distant metros', () => {
      const farLocation: UserLocation = {
        latitude: 25.0,
        longitude: -80.0,
        accuracy: 10,
        timestamp: new Date(),
      };
      const { container } = render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          userLocation={farLocation}
        />
      );
      const select = container.querySelector('select') as HTMLSelectElement;
      const lastOption = select.options[select.options.length - 1];
      expect(lastOption.text).not.toContain('Nearby');
    });
  });

  describe('Selected Metro Display', () => {
    it('should show selected metro details with MapPinned icon', () => {
      render(
        <MetroAreaSelector
          value="metro-nyc"
          metros={mockMetros}
          onChange={mockOnChange}
        />
      );
      expect(screen.getByText('New York, NY')).toBeInTheDocument();
    });

    it('should show distance for selected metro when location available', () => {
      const userLocation: UserLocation = {
        latitude: 40.0,
        longitude: -74.0,
        accuracy: 10,
        timestamp: new Date(),
      };
      render(
        <MetroAreaSelector
          value="metro-nyc"
          metros={mockMetros}
          onChange={mockOnChange}
          userLocation={userLocation}
        />
      );
      expect(screen.getByText(/\d+ miles away/)).toBeInTheDocument();
    });

    it('should not show selected metro display when no metro selected', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
        />
      );
      const { container } = render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
        />
      );
      const selectedDisplay = container.querySelector('.text-sm');
      expect(selectedDisplay).toBeNull();
    });
  });

  describe('Accessibility', () => {
    it('should have proper aria-label on select', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
        />
      );
      const select = screen.getByLabelText('Metro area selector');
      expect(select).toHaveAttribute('aria-label', 'Metro area selector');
    });

    it('should have proper aria-label on detect button', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          onDetectLocation={mockOnDetectLocation}
        />
      );
      const button = screen.getByLabelText('Detect my location');
      expect(button).toBeInTheDocument();
    });

    it('should update aria-label during detection', () => {
      render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          onDetectLocation={mockOnDetectLocation}
          isDetecting={true}
        />
      );
      const button = screen.getByLabelText('Detecting location...');
      expect(button).toBeInTheDocument();
    });

    it('should have focus styles on select', () => {
      const { container } = render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
        />
      );
      const select = container.querySelector('select');
      expect(select).toHaveClass('focus:border-[#FF7900]');
    });

    it('should have focus styles on detect button', () => {
      const { container } = render(
        <MetroAreaSelector
          value={null}
          metros={mockMetros}
          onChange={mockOnChange}
          onDetectLocation={mockOnDetectLocation}
        />
      );
      const button = screen.getByText('Detect My Location').closest('button');
      expect(button).toHaveClass('focus:ring-2');
    });
  });
});
