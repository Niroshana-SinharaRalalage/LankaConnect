/**
 * @jest-environment jsdom
 */

import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { MetroAreasSelector } from '@/presentation/components/features/auth/MetroAreasSelector';
import * as useMetroAreasModule from '@/presentation/hooks/useMetroAreas';
import type { MetroAreaDto } from '@/infrastructure/api/repositories/metro-areas.repository';

// Mock the useMetroAreas hook
vi.mock('@/presentation/hooks/useMetroAreas');

describe('MetroAreasSelector', () => {
  const mockMetroAreas: MetroAreaDto[] = [
    {
      id: 'ca-la-1',
      name: 'Los Angeles',
      state: 'California',
      centerLatitude: 34.0522,
      centerLongitude: -118.2437,
      radiusMiles: 50,
      isStateLevelArea: false,
      isActive: true,
    },
    {
      id: 'ca-sf-1',
      name: 'San Francisco',
      state: 'California',
      centerLatitude: 37.7749,
      centerLongitude: -122.4194,
      radiusMiles: 50,
      isStateLevelArea: false,
      isActive: true,
    },
    {
      id: 'ny-nyc-1',
      name: 'New York City',
      state: 'New York',
      centerLatitude: 40.7128,
      centerLongitude: -74.0060,
      radiusMiles: 50,
      isStateLevelArea: false,
      isActive: true,
    },
  ];

  const mockOnChange = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Component Rendering', () => {
    it('should render with placeholder when no metros selected', () => {
      vi.mocked(useMetroAreasModule.useMetroAreas).mockReturnValue({
        metroAreas: mockMetroAreas,
        metroAreasByState: new Map(),
        stateLevelMetros: [],
        cityLevelMetros: mockMetroAreas,
        isLoading: false,
        error: null,
      });

      render(
        <MetroAreasSelector
          value={[]}
          onChange={mockOnChange}
        />
      );

      expect(screen.getByText('Preferred Metro Areas')).toBeInTheDocument();
      expect(screen.getByText(/select your preferred metro areas/i)).toBeInTheDocument();
    });

    it('should render required indicator when required=true', () => {
      vi.mocked(useMetroAreasModule.useMetroAreas).mockReturnValue({
        metroAreas: mockMetroAreas,
        metroAreasByState: new Map(),
        stateLevelMetros: [],
        cityLevelMetros: mockMetroAreas,
        isLoading: false,
        error: null,
      });

      render(
        <MetroAreasSelector
          value={[]}
          onChange={mockOnChange}
          required={true}
        />
      );

      // Check for asterisk (required indicator)
      expect(screen.getByText('*')).toBeInTheDocument();
    });

    it('should not render required indicator when required=false', () => {
      vi.mocked(useMetroAreasModule.useMetroAreas).mockReturnValue({
        metroAreas: mockMetroAreas,
        metroAreasByState: new Map(),
        stateLevelMetros: [],
        cityLevelMetros: mockMetroAreas,
        isLoading: false,
        error: null,
      });

      render(
        <MetroAreasSelector
          value={[]}
          onChange={mockOnChange}
          required={false}
        />
      );

      // Asterisk should not be present
      expect(screen.queryByText('*')).not.toBeInTheDocument();
    });

    it('should display helper text with correct min/max selection range', () => {
      vi.mocked(useMetroAreasModule.useMetroAreas).mockReturnValue({
        metroAreas: mockMetroAreas,
        metroAreasByState: new Map(),
        stateLevelMetros: [],
        cityLevelMetros: mockMetroAreas,
        isLoading: false,
        error: null,
      });

      render(
        <MetroAreasSelector
          value={[]}
          onChange={mockOnChange}
          minSelection={1}
          maxSelection={20}
        />
      );

      expect(screen.getByText(/select 1-20 metro areas/i)).toBeInTheDocument();
    });
  });

  describe('Loading State', () => {
    it('should show loading message while fetching metro data', () => {
      vi.mocked(useMetroAreasModule.useMetroAreas).mockReturnValue({
        metroAreas: [],
        metroAreasByState: new Map(),
        stateLevelMetros: [],
        cityLevelMetros: [],
        isLoading: true,
        error: null,
      });

      render(
        <MetroAreasSelector
          value={[]}
          onChange={mockOnChange}
        />
      );

      expect(screen.getByText(/loading metro areas/i)).toBeInTheDocument();
    });

    it('should disable TreeDropdown while loading', () => {
      vi.mocked(useMetroAreasModule.useMetroAreas).mockReturnValue({
        metroAreas: [],
        metroAreasByState: new Map(),
        stateLevelMetros: [],
        cityLevelMetros: [],
        isLoading: true,
        error: null,
      });

      render(
        <MetroAreasSelector
          value={[]}
          onChange={mockOnChange}
        />
      );

      // TreeDropdown button should be disabled
      const dropdownButton = screen.getByRole('button');
      expect(dropdownButton).toBeDisabled();
    });
  });

  describe('Error Handling', () => {
    it('should display error message when error prop provided', () => {
      vi.mocked(useMetroAreasModule.useMetroAreas).mockReturnValue({
        metroAreas: mockMetroAreas,
        metroAreasByState: new Map(),
        stateLevelMetros: [],
        cityLevelMetros: mockMetroAreas,
        isLoading: false,
        error: null,
      });

      const errorMessage = 'Please select at least one metro area';

      render(
        <MetroAreasSelector
          value={[]}
          onChange={mockOnChange}
          error={errorMessage}
        />
      );

      expect(screen.getByText(errorMessage)).toBeInTheDocument();
    });

    it('should display API error message when metro fetch fails', () => {
      const apiError = 'Failed to load metro areas from server';

      vi.mocked(useMetroAreasModule.useMetroAreas).mockReturnValue({
        metroAreas: [],
        metroAreasByState: new Map(),
        stateLevelMetros: [],
        cityLevelMetros: [],
        isLoading: false,
        error: apiError,
      });

      render(
        <MetroAreasSelector
          value={[]}
          onChange={mockOnChange}
        />
      );

      expect(screen.getByText(apiError)).toBeInTheDocument();
    });
  });

  describe('Metro Selection', () => {
    it('should call onChange with selected metro IDs', async () => {
      const metrosByState = new Map([
        ['California', mockMetroAreas.filter(m => m.state === 'California')],
      ]);

      vi.mocked(useMetroAreasModule.useMetroAreas).mockReturnValue({
        metroAreas: mockMetroAreas,
        metroAreasByState: metrosByState,
        stateLevelMetros: [],
        cityLevelMetros: mockMetroAreas,
        isLoading: false,
        error: null,
      });

      render(
        <MetroAreasSelector
          value={[]}
          onChange={mockOnChange}
        />
      );

      // Open dropdown
      const dropdownButton = screen.getByRole('button');
      fireEvent.click(dropdownButton);

      // Wait for tree to render
      await waitFor(() => {
        expect(screen.getByText('California')).toBeInTheDocument();
      });

      // Expand California node first to show children
      const expandButton = screen.getByLabelText('Expand');
      fireEvent.click(expandButton);

      // Wait for Los Angeles to appear
      await waitFor(() => {
        expect(screen.getByText('Los Angeles')).toBeInTheDocument();
      });

      // Select Los Angeles checkbox
      const laLabel = screen.getByText('Los Angeles').closest('label');
      const laCheckbox = laLabel?.querySelector('input[type="checkbox"]');
      expect(laCheckbox).toBeTruthy();
      fireEvent.click(laCheckbox!);

      // Verify onChange was called with correct ID
      await waitFor(() => {
        expect(mockOnChange).toHaveBeenCalledWith(['ca-la-1']);
      });
    });

    it('should display selected count in dropdown button', () => {
      vi.mocked(useMetroAreasModule.useMetroAreas).mockReturnValue({
        metroAreas: mockMetroAreas,
        metroAreasByState: new Map(),
        stateLevelMetros: [],
        cityLevelMetros: mockMetroAreas,
        isLoading: false,
        error: null,
      });

      render(
        <MetroAreasSelector
          value={['ca-la-1', 'ca-sf-1']}
          onChange={mockOnChange}
        />
      );

      // TreeDropdown should show "2 items selected"
      expect(screen.getByText(/2 items selected/i)).toBeInTheDocument();
    });
  });

  describe('Validation', () => {
    it('should respect maxSelection limit', async () => {
      vi.mocked(useMetroAreasModule.useMetroAreas).mockReturnValue({
        metroAreas: mockMetroAreas,
        metroAreasByState: new Map(),
        stateLevelMetros: [],
        cityLevelMetros: mockMetroAreas,
        isLoading: false,
        error: null,
      });

      render(
        <MetroAreasSelector
          value={[]}
          onChange={mockOnChange}
          maxSelection={1}
        />
      );

      // Open dropdown
      const dropdownButton = screen.getByRole('button');
      fireEvent.click(dropdownButton);

      // TreeDropdown should enforce max limit (tested in TreeDropdown component tests)
      // Here we just verify max is passed to TreeDropdown
      await waitFor(() => {
        expect(screen.getByText(/0 of 1 selected/i)).toBeInTheDocument();
      });
    });
  });

  describe('Tree Structure', () => {
    it('should group metro areas by state', () => {
      const metrosByState = new Map([
        ['California', mockMetroAreas.filter(m => m.state === 'California')],
        ['New York', mockMetroAreas.filter(m => m.state === 'New York')],
      ]);

      vi.mocked(useMetroAreasModule.useMetroAreas).mockReturnValue({
        metroAreas: mockMetroAreas,
        metroAreasByState: metrosByState,
        stateLevelMetros: [],
        cityLevelMetros: mockMetroAreas,
        isLoading: false,
        error: null,
      });

      render(
        <MetroAreasSelector
          value={[]}
          onChange={mockOnChange}
        />
      );

      // Open dropdown
      const dropdownButton = screen.getByRole('button');
      fireEvent.click(dropdownButton);

      // Verify states appear as parent nodes
      expect(screen.getByText('California')).toBeInTheDocument();
      expect(screen.getByText('New York')).toBeInTheDocument();
    });

    it('should sort states alphabetically', () => {
      const metrosByState = new Map([
        ['New York', mockMetroAreas.filter(m => m.state === 'New York')],
        ['California', mockMetroAreas.filter(m => m.state === 'California')],
      ]);

      vi.mocked(useMetroAreasModule.useMetroAreas).mockReturnValue({
        metroAreas: mockMetroAreas,
        metroAreasByState: metrosByState,
        stateLevelMetros: [],
        cityLevelMetros: mockMetroAreas,
        isLoading: false,
        error: null,
      });

      render(
        <MetroAreasSelector
          value={[]}
          onChange={mockOnChange}
        />
      );

      // Open dropdown
      const dropdownButton = screen.getByRole('button');
      fireEvent.click(dropdownButton);

      // Get all state labels
      const stateLabels = screen.getAllByText(/california|new york/i);

      // California should come before New York (alphabetical order)
      expect(stateLabels[0]).toHaveTextContent('California');
      expect(stateLabels[1]).toHaveTextContent('New York');
    });
  });

  describe('Accessibility', () => {
    it('should have proper ARIA labels', () => {
      vi.mocked(useMetroAreasModule.useMetroAreas).mockReturnValue({
        metroAreas: mockMetroAreas,
        metroAreasByState: new Map(),
        stateLevelMetros: [],
        cityLevelMetros: mockMetroAreas,
        isLoading: false,
        error: null,
      });

      render(
        <MetroAreasSelector
          value={[]}
          onChange={mockOnChange}
        />
      );

      // Check for proper label
      expect(screen.getByText('Preferred Metro Areas')).toBeInTheDocument();

      // TreeDropdown should have button role
      expect(screen.getByRole('button')).toBeInTheDocument();
    });
  });
});
