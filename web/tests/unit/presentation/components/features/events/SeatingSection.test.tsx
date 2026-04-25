import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { SeatingSection } from '@/presentation/components/features/events/SeatingSection';
import { SeatingMode, TicketingMode } from '@/infrastructure/api/types/events.types';

/**
 * Seating Redesign Slice 1: Component tests for SeatingSection.
 *
 * Key invariants the rest of the system relies on:
 *  - Renders null when ticketingMode !== Tiered.
 *  - Toggling fires onChange with the flipped enum value.
 *  - Placeholder copy appears when AssignedSeating is active.
 *  - isSaving / disabled / errorMessage render the appropriate affordances.
 */
describe('SeatingSection', () => {
  const baseProps = {
    ticketingMode: TicketingMode.Tiered,
    value: SeatingMode.GeneralAdmission,
    onChange: vi.fn(),
  };

  describe('visibility gate', () => {
    it('renders null when ticketing mode is SingleTier', () => {
      const { container } = render(
        <SeatingSection
          {...baseProps}
          ticketingMode={TicketingMode.SingleTier}
        />
      );
      expect(container.firstChild).toBeNull();
      expect(screen.queryByTestId('seating-section')).not.toBeInTheDocument();
    });

    it('renders when ticketing mode is Tiered', () => {
      render(<SeatingSection {...baseProps} />);
      expect(screen.getByTestId('seating-section')).toBeInTheDocument();
    });
  });

  describe('toggle behavior', () => {
    it('reflects GeneralAdmission value with toggle unchecked', () => {
      render(<SeatingSection {...baseProps} />);
      const toggle = screen.getByTestId('seating-toggle') as HTMLInputElement;
      expect(toggle.checked).toBe(false);
    });

    it('reflects AssignedSeating value with toggle checked', () => {
      render(
        <SeatingSection {...baseProps} value={SeatingMode.AssignedSeating} />
      );
      const toggle = screen.getByTestId('seating-toggle') as HTMLInputElement;
      expect(toggle.checked).toBe(true);
    });

    it('fires onChange with AssignedSeating when toggled on', () => {
      const onChange = vi.fn();
      render(<SeatingSection {...baseProps} onChange={onChange} />);

      fireEvent.click(screen.getByTestId('seating-toggle'));

      expect(onChange).toHaveBeenCalledWith(SeatingMode.AssignedSeating);
    });

    it('fires onChange with GeneralAdmission when toggled off', () => {
      const onChange = vi.fn();
      render(
        <SeatingSection
          {...baseProps}
          value={SeatingMode.AssignedSeating}
          onChange={onChange}
        />
      );

      fireEvent.click(screen.getByTestId('seating-toggle'));

      expect(onChange).toHaveBeenCalledWith(SeatingMode.GeneralAdmission);
    });
  });

  describe('placeholder panel', () => {
    // Slice 6 S6.9: when no eventId is supplied (event-creation flow) we show
    // a "save first" hint; when eventId is supplied (edit flow) the
    // SeatingLayoutPicker replaces the placeholder entirely.
    it('shows the save-first placeholder when AssignedSeating is active and eventId is absent', () => {
      render(
        <SeatingSection {...baseProps} value={SeatingMode.AssignedSeating} />
      );
      expect(
        screen.getByTestId('seating-layout-placeholder')
      ).toBeInTheDocument();
      expect(
        screen.getByText(/Save the event first, then pick a seating layout/i)
      ).toBeInTheDocument();
      // Picker is not rendered without eventId.
      expect(
        screen.queryByTestId('seating-layout-picker')
      ).not.toBeInTheDocument();
    });

    it('hides the placeholder when GeneralAdmission is active', () => {
      render(<SeatingSection {...baseProps} />);
      expect(
        screen.queryByTestId('seating-layout-placeholder')
      ).not.toBeInTheDocument();
      expect(
        screen.queryByTestId('seating-layout-picker')
      ).not.toBeInTheDocument();
    });
  });

  describe('feedback states', () => {
    it('renders a saving spinner when isSaving is true', () => {
      render(<SeatingSection {...baseProps} isSaving />);
      expect(screen.getByTestId('seating-saving')).toBeInTheDocument();
    });

    it('renders an error message when errorMessage is provided', () => {
      render(
        <SeatingSection {...baseProps} errorMessage="Cannot change after registrations exist" />
      );
      expect(screen.getByTestId('seating-error')).toHaveTextContent(
        /Cannot change after registrations exist/
      );
    });

    it('does not fire onChange when disabled', () => {
      const onChange = vi.fn();
      render(
        <SeatingSection
          {...baseProps}
          onChange={onChange}
          disabled
          disabledReason="Event has confirmed registrations"
        />
      );

      fireEvent.click(screen.getByTestId('seating-toggle'));

      expect(onChange).not.toHaveBeenCalled();
      expect(
        screen.getByText(/Event has confirmed registrations/)
      ).toBeInTheDocument();
    });

    it('does not fire onChange while saving', () => {
      const onChange = vi.fn();
      render(<SeatingSection {...baseProps} onChange={onChange} isSaving />);

      fireEvent.click(screen.getByTestId('seating-toggle'));

      expect(onChange).not.toHaveBeenCalled();
    });
  });
});
