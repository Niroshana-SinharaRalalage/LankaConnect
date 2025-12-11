import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { EventsList } from '@/presentation/components/features/dashboard/EventsList';
import { EventDto, EventStatus, EventCategory } from '@/infrastructure/api/types/events.types';

describe('EventsList', () => {
  const mockEvents: EventDto[] = [
    {
      id: '1',
      title: 'Vesak Day Celebration',
      description: 'Traditional Buddhist celebration',
      startDate: '2025-05-23T10:00:00Z',
      endDate: '2025-05-23T18:00:00Z',
      organizerId: 'org1',
      capacity: 100,
      currentRegistrations: 45,
      status: EventStatus.Published,
      category: EventCategory.Religious,
      createdAt: '2025-01-01T00:00:00Z',
      isFree: true,
      hasDualPricing: false,
      city: 'Toronto',
      state: 'ON',
      images: [],
      videos: [],
    },
    {
      id: '2',
      title: 'Sri Lankan Independence Day',
      description: 'National celebration',
      startDate: '2025-02-04T14:00:00Z',
      endDate: '2025-02-04T20:00:00Z',
      organizerId: 'org2',
      capacity: 200,
      currentRegistrations: 150,
      status: EventStatus.Active,
      category: EventCategory.Cultural,
      createdAt: '2025-01-02T00:00:00Z',
      isFree: false,
      hasDualPricing: false,
      ticketPriceAmount: 25,
      city: 'Vancouver',
      state: 'BC',
      images: [],
      videos: [],
    },
  ];

  it('should render event titles', () => {
    render(<EventsList events={mockEvents} />);

    expect(screen.getByText('Vesak Day Celebration')).toBeInTheDocument();
    expect(screen.getByText('Sri Lankan Independence Day')).toBeInTheDocument();
  });

  it('should display event dates in readable format', () => {
    render(<EventsList events={mockEvents} />);

    // Should display formatted dates
    expect(screen.getByText(/May 23, 2025/i)).toBeInTheDocument();
    expect(screen.getByText(/Feb 04, 2025/i)).toBeInTheDocument();
  });

  it('should display event locations', () => {
    render(<EventsList events={mockEvents} />);

    expect(screen.getByText(/Toronto, ON/i)).toBeInTheDocument();
    expect(screen.getByText(/Vancouver, BC/i)).toBeInTheDocument();
  });

  it('should display "Free" badge for free events', () => {
    render(<EventsList events={mockEvents} />);

    expect(screen.getByText('Free')).toBeInTheDocument();
  });

  it('should display event capacity information', () => {
    render(<EventsList events={mockEvents} />);

    expect(screen.getByText(/45 \/ 100/i)).toBeInTheDocument();
    expect(screen.getByText(/150 \/ 200/i)).toBeInTheDocument();
  });

  it('should show empty state when no events', () => {
    render(<EventsList events={[]} emptyMessage="No events found" />);

    expect(screen.getByText('No events found')).toBeInTheDocument();
  });

  it('should show loading state', () => {
    render(<EventsList events={[]} isLoading={true} />);

    expect(screen.getByText(/Loading/i)).toBeInTheDocument();
  });

  it('should display event status badges', () => {
    render(<EventsList events={mockEvents} />);

    expect(screen.getByText('Published')).toBeInTheDocument();
    expect(screen.getByText('Active')).toBeInTheDocument();
  });

  it('should apply category-specific colors', () => {
    render(<EventsList events={mockEvents} />);

    const religiousBadge = screen.getByText('Religious');
    const culturalBadge = screen.getByText('Cultural');

    expect(religiousBadge).toBeInTheDocument();
    expect(culturalBadge).toBeInTheDocument();
  });

  describe('Cancel Registration', () => {
    it('should render cancel button for each event when onCancelClick prop is provided', () => {
      const mockCancelClick = vi.fn();
      render(<EventsList events={mockEvents} onCancelClick={mockCancelClick} />);

      const cancelButtons = screen.getAllByRole('button', { name: /cancel registration/i });
      expect(cancelButtons).toHaveLength(2);
    });

    it('should not render cancel button when onCancelClick prop is not provided', () => {
      render(<EventsList events={mockEvents} />);

      const cancelButtons = screen.queryAllByRole('button', { name: /cancel registration/i });
      expect(cancelButtons).toHaveLength(0);
    });

    it('should call onCancelClick with correct eventId when cancel button is clicked', async () => {
      const mockCancelClick = vi.fn().mockResolvedValue(undefined);
      render(<EventsList events={mockEvents} onCancelClick={mockCancelClick} />);

      const cancelButtons = screen.getAllByRole('button', { name: /cancel registration/i });
      fireEvent.click(cancelButtons[0]);

      await waitFor(() => {
        expect(mockCancelClick).toHaveBeenCalledWith('1');
        expect(mockCancelClick).toHaveBeenCalledTimes(1);
      });
    });

    it('should show loading state on cancel button while cancellation is in progress', async () => {
      const mockCancelClick = vi.fn().mockImplementation(
        () => new Promise(resolve => setTimeout(resolve, 100))
      );
      render(<EventsList events={mockEvents} onCancelClick={mockCancelClick} />);

      const cancelButtons = screen.getAllByRole('button', { name: /cancel registration/i });
      fireEvent.click(cancelButtons[0]);

      // Button should be disabled during cancellation
      await waitFor(() => {
        expect(cancelButtons[0]).toBeDisabled();
      });
    });

    it('should not trigger onEventClick when cancel button is clicked', async () => {
      const mockEventClick = vi.fn();
      const mockCancelClick = vi.fn().mockResolvedValue(undefined);
      render(
        <EventsList
          events={mockEvents}
          onEventClick={mockEventClick}
          onCancelClick={mockCancelClick}
        />
      );

      const cancelButtons = screen.getAllByRole('button', { name: /cancel registration/i });
      fireEvent.click(cancelButtons[0]);

      await waitFor(() => {
        expect(mockCancelClick).toHaveBeenCalledTimes(1);
        expect(mockEventClick).not.toHaveBeenCalled();
      });
    });

    it('should disable only the clicked cancel button, not all buttons', async () => {
      const mockCancelClick = vi.fn().mockImplementation(
        () => new Promise(resolve => setTimeout(resolve, 100))
      );
      render(<EventsList events={mockEvents} onCancelClick={mockCancelClick} />);

      const cancelButtons = screen.getAllByRole('button', { name: /cancel registration/i });
      fireEvent.click(cancelButtons[0]);

      await waitFor(() => {
        expect(cancelButtons[0]).toBeDisabled();
        expect(cancelButtons[1]).not.toBeDisabled();
      });
    });
  });
});
