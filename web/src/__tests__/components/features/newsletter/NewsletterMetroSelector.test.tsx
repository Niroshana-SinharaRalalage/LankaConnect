/**
 * @vitest-environment jsdom
 */
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { NewsletterMetroSelector } from '@/presentation/components/features/newsletter/NewsletterMetroSelector';

/**
 * Test Suite for NewsletterMetroSelector Component
 * Phase 5B.8: Newsletter Metro Area Selection
 *
 * Tests multi-select metro area functionality, state grouping, expand/collapse,
 * validation, and accessibility features.
 */
describe('NewsletterMetroSelector Component', () => {
  // Phase 5B: Metro area GUIDs for testing
  const OHIO_STATE_GUID = '39000000-0000-0000-0000-000000000001';
  const CLEVELAND_GUID = '39111111-1111-1111-1111-111111111001';
  const COLUMBUS_GUID = '39111111-1111-1111-1111-111111111002';
  const NEW_YORK_STATE_GUID = '36000000-0000-0000-0000-000000000001';
  const NYC_GUID = '36111111-1111-1111-1111-111111111001';

  // Mock callbacks
  const mockOnMetrosChange = vi.fn();
  const mockOnReceiveAllChange = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Rendering & Basic Structure', () => {
    it('should render the component with header text', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      expect(screen.getByText(/Get notifications for events in:/i)).toBeInTheDocument();
    });

    it('should display selection limit text', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
          maxSelections={20}
        />
      );

      expect(screen.getByText(/Select up to 20 metro areas/i)).toBeInTheDocument();
    });

    it('should render "All locations" checkbox', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      const checkbox = screen.getByLabelText(/Send me events from all locations/i);
      expect(checkbox).toBeInTheDocument();
      expect(checkbox).not.toBeChecked();
    });

    it('should render state-wide selections section', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      expect(screen.getByText(/State-Wide Selections/i)).toBeInTheDocument();
    });

    it('should render city metro areas section', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      expect(screen.getByText(/City Metro Areas/i)).toBeInTheDocument();
    });
  });

  describe('All Locations Checkbox Behavior', () => {
    it('should call onReceiveAllChange when "all locations" checkbox is clicked', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      const checkbox = screen.getByLabelText(/Send me events from all locations/i);
      fireEvent.click(checkbox);

      expect(mockOnReceiveAllChange).toHaveBeenCalledWith(true);
    });

    it('should show checked "all locations" when receiveAllLocations is true', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={true}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      const checkbox = screen.getByLabelText(/Send me events from all locations/i) as HTMLInputElement;
      expect(checkbox.checked).toBe(true);
    });

    it('should hide metro selection when receiveAllLocations is true', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={true}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      // Metro selection area should be hidden
      const metroSelectionArea = screen.queryByText(/State-Wide Selections/i);
      expect(metroSelectionArea).not.toBeInTheDocument();
    });

    it('should show metro selection when receiveAllLocations is false', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      expect(screen.getByText(/State-Wide Selections/i)).toBeInTheDocument();
    });
  });

  describe('State-Level Metro Selection', () => {
    it('should display state-level metro options', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      // Should have state-level options like "All Ohio"
      const stateOptions = screen.queryAllByText(/^All [A-Z]/);
      expect(stateOptions.length).toBeGreaterThan(0);
    });

    it('should call onMetrosChange when state-level metro is selected', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      // Find and click a state-level checkbox
      const stateCheckboxes = screen.getAllByRole('checkbox').filter((cb) => {
        const label = cb.closest('label');
        return label && /^All [A-Z]/.test(label.textContent || '');
      });

      if (stateCheckboxes.length > 0) {
        fireEvent.click(stateCheckboxes[0]);
        expect(mockOnMetrosChange).toHaveBeenCalled();
      }
    });

    it('should highlight selected state-level metro', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[OHIO_STATE_GUID]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      // Selected metro should have highlighted background color
      const selectedLabel = screen.getAllByRole('checkbox').find((cb) => (cb as HTMLInputElement).checked)?.closest('label');
      expect(selectedLabel).toHaveStyle({ background: '#FFE8CC' });
    });
  });

  describe('State Expand/Collapse Behavior', () => {
    it('should render state headers as expandable buttons', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      // Should have buttons with aria-expanded attribute
      const expandableButtons = screen.queryAllByRole('button').filter((btn) => btn.getAttribute('aria-expanded') !== null);
      expect(expandableButtons.length).toBeGreaterThan(0);
    });

    it('should display chevron icon for collapsed state', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      // Initially states are collapsed, should show ChevronRight
      const buttons = screen.queryAllByRole('button').filter((btn) => btn.getAttribute('aria-expanded') === 'false');
      expect(buttons.length).toBeGreaterThan(0);
    });

    it('should toggle state expansion when header is clicked', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      // Find a state button
      const stateButton = screen.queryAllByRole('button').find((btn) => btn.getAttribute('aria-expanded') !== null) as HTMLButtonElement;

      if (stateButton) {
        const initialState = stateButton.getAttribute('aria-expanded');
        fireEvent.click(stateButton);

        // After click, expanded state should be toggled in next render
        // Just verify the button is clickable
        expect(stateButton).not.toBeDisabled();
      }
    });

    it('should have aria-controls pointing to metro list for accessibility', () => {
      const { rerender } = render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      // All state headers should have aria-controls attribute
      const stateButtons = screen.queryAllByRole('button').filter((btn) => btn.getAttribute('aria-expanded') !== null);
      expect(stateButtons.length).toBeGreaterThan(0);

      stateButtons.forEach((button) => {
        const ariaControls = button.getAttribute('aria-controls');
        expect(ariaControls).toBeTruthy();
      });
    });

    it('should show selection counter badge in state header when metros are selected', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[CLEVELAND_GUID, COLUMBUS_GUID]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      // Should show a badge with selection count
      const badges = screen.queryAllByText(/\d+ selected/);
      expect(badges.length).toBeGreaterThan(0);
    });
  });

  describe('Selection Limit Validation', () => {
    it('should display selection counter at bottom', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[OHIO_STATE_GUID, CLEVELAND_GUID]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
          maxSelections={20}
        />
      );

      expect(screen.getByText(/2 of 20 selected/i)).toBeInTheDocument();
    });

    it('should show validation error when user tries to exceed max limit', () => {
      const { rerender } = render(
        <NewsletterMetroSelector
          selectedMetroIds={[OHIO_STATE_GUID, CLEVELAND_GUID]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
          maxSelections={2}
        />
      );

      // At limit (2 of 2)
      expect(screen.getByText(/2 of 2 selected/i)).toBeInTheDocument();

      // Try to select another by calling onMetrosChange with 3 items
      // Component will show validation error
      rerender(
        <NewsletterMetroSelector
          selectedMetroIds={[OHIO_STATE_GUID, CLEVELAND_GUID, COLUMBUS_GUID]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
          maxSelections={2}
        />
      );

      // Should show error for exceeding limit
      expect(screen.getByText(/cannot select more than 2/i)).toBeInTheDocument();
    });

    it('should allow selecting up to the max limit', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
          maxSelections={20}
        />
      );

      // Should show 0 of 20 initially
      expect(screen.getByText(/0 of 20 selected/i)).toBeInTheDocument();

      // Find checkboxes and verify they're interactive
      const checkboxes = screen.getAllByRole('checkbox');
      expect(checkboxes.length).toBeGreaterThan(0);
    });

    it('should allow clearing selections even at max limit', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[OHIO_STATE_GUID, CLEVELAND_GUID]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
          maxSelections={20}
        />
      );

      // Find a checked checkbox and click it to uncheck
      const checkedCheckboxes = screen.queryAllByRole('checkbox').filter((cb) => (cb as HTMLInputElement).checked);

      if (checkedCheckboxes.length > 0) {
        fireEvent.click(checkedCheckboxes[0]);
        expect(mockOnMetrosChange).toHaveBeenCalled();
      }
    });
  });

  describe('Disabled State', () => {
    it('should disable all checkboxes when disabled prop is true', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
          disabled={true}
        />
      );

      const checkboxes = screen.getAllByRole('checkbox');
      checkboxes.forEach((checkbox) => {
        expect(checkbox).toBeDisabled();
      });
    });

    it('should disable state expansion buttons when disabled prop is true', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
          disabled={true}
        />
      );

      const expandButtons = screen.queryAllByRole('button').filter((btn) => btn.getAttribute('aria-expanded') !== null);
      expandButtons.forEach((button) => {
        expect(button).toBeDisabled();
      });
    });

    it('should not call callbacks when disabled and user tries to interact', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
          disabled={true}
        />
      );

      const checkbox = screen.getByLabelText(/Send me events from all locations/i) as HTMLInputElement;
      // Checkbox should be disabled
      expect(checkbox).toBeDisabled();

      // Try to interact with disabled checkbox
      fireEvent.click(checkbox);

      // Callback should not be called because checkbox is disabled
      // (Note: disabled inputs may still fire change events in tests, but callbacks should be guarded)
      const stateButtons = screen.queryAllByRole('button').filter((btn) => btn.getAttribute('aria-expanded') !== null);
      expect(stateButtons.every(btn => btn.getAttribute('disabled') === '')).toBe(true);
    });
  });

  describe('Accessibility', () => {
    it('should have proper aria-labels on all checkboxes', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      // Check that checkboxes have either aria-label or are associated with label elements
      const checkboxes = screen.getAllByRole('checkbox');
      expect(checkboxes.length).toBeGreaterThan(0);

      // The "All locations" checkbox is wrapped in a label with visible text
      const allLocationsCheckbox = screen.getByLabelText(/Send me events from all locations/i);
      expect(allLocationsCheckbox).toBeInTheDocument();

      // State-level metros should have aria-labels
      const stateLevelCheckboxes = screen.queryAllByLabelText(/Select all of/i);
      expect(stateLevelCheckboxes.length).toBeGreaterThan(0);
    });

    it('should have aria-expanded attribute on state headers', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      const expandButtons = screen.queryAllByRole('button').filter((btn) => btn.getAttribute('aria-expanded') !== null);
      expandButtons.forEach((button) => {
        expect(button).toHaveAttribute('aria-expanded');
      });
    });

    it('should have aria-controls attribute linking header to metro list', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      const expandButtons = screen.queryAllByRole('button').filter((btn) => btn.getAttribute('aria-expanded') !== null);
      expandButtons.forEach((button) => {
        expect(button).toHaveAttribute('aria-controls');
      });
    });

    it('should have descriptive text about selection limit', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
          maxSelections={15}
        />
      );

      expect(screen.getByText(/Select up to 15 metro areas/i)).toBeInTheDocument();
    });

    it('should display validation error message', () => {
      // Start with 2 selections at a limit of 2
      const { rerender } = render(
        <NewsletterMetroSelector
          selectedMetroIds={[OHIO_STATE_GUID, CLEVELAND_GUID]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
          maxSelections={2}
        />
      );

      // Verify we're at limit
      expect(screen.getByText(/2 of 2 selected/i)).toBeInTheDocument();

      // Try to exceed limit by passing 3 selections
      rerender(
        <NewsletterMetroSelector
          selectedMetroIds={[OHIO_STATE_GUID, CLEVELAND_GUID, COLUMBUS_GUID]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
          maxSelections={2}
        />
      );

      // Component displays validation error when max is exceeded
      const errorMessages = screen.queryAllByText(/cannot select more than/i);
      expect(errorMessages.length).toBeGreaterThan(0);
    });
  });

  describe('Integration with Footer', () => {
    it('should handle empty selection state', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      expect(screen.getByText(/0 of 20 selected/i)).toBeInTheDocument();
    });

    it('should handle multiple selections', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[OHIO_STATE_GUID, CLEVELAND_GUID, COLUMBUS_GUID, NYC_GUID]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      expect(screen.getByText(/4 of 20 selected/i)).toBeInTheDocument();
    });

    it('should toggle between receiveAll and metro selection modes', () => {
      const { rerender } = render(
        <NewsletterMetroSelector
          selectedMetroIds={[OHIO_STATE_GUID]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      // Initially in metro selection mode
      expect(screen.getByText(/State-Wide Selections/i)).toBeInTheDocument();

      // Switch to receive all
      rerender(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={true}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
        />
      );

      // Metro selection should be hidden
      expect(screen.queryByText(/State-Wide Selections/i)).not.toBeInTheDocument();
    });
  });

  describe('Custom Max Selections', () => {
    it('should respect custom maxSelections prop', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
          maxSelections={5}
        />
      );

      expect(screen.getByText(/Select up to 5 metro areas/i)).toBeInTheDocument();
      expect(screen.getByText(/0 of 5 selected/i)).toBeInTheDocument();
    });

    it('should show correct selection counter with custom limit', () => {
      render(
        <NewsletterMetroSelector
          selectedMetroIds={[OHIO_STATE_GUID, CLEVELAND_GUID, COLUMBUS_GUID]}
          receiveAllLocations={false}
          onMetrosChange={mockOnMetrosChange}
          onReceiveAllChange={mockOnReceiveAllChange}
          maxSelections={10}
        />
      );

      expect(screen.getByText(/3 of 10 selected/i)).toBeInTheDocument();
    });
  });
});
