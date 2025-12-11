import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { CulturalCalendar } from '@/presentation/components/features/dashboard/CulturalCalendar';

describe('CulturalCalendar Component', () => {
  const mockEvents = [
    {
      id: '1',
      name: 'Sinhala and Tamil New Year',
      date: '2025-04-14',
      category: 'national' as const,
    },
    {
      id: '2',
      name: 'Vesak Full Moon Poya Day',
      date: '2025-05-23',
      category: 'religious' as const,
    },
    {
      id: '3',
      name: 'Poson Full Moon Poya Day',
      date: '2025-06-21',
      category: 'religious' as const,
    },
  ];

  describe('rendering', () => {
    it('should render cultural calendar with title', () => {
      render(<CulturalCalendar events={mockEvents} />);

      expect(screen.getByText('Cultural Calendar')).toBeInTheDocument();
    });

    it('should render list of events with names', () => {
      render(<CulturalCalendar events={mockEvents} />);

      expect(screen.getByText('Sinhala and Tamil New Year')).toBeInTheDocument();
      expect(screen.getByText('Vesak Full Moon Poya Day')).toBeInTheDocument();
      expect(screen.getByText('Poson Full Moon Poya Day')).toBeInTheDocument();
    });

    it('should render event dates', () => {
      render(<CulturalCalendar events={mockEvents} />);

      expect(screen.getByText('Apr 14, 2025')).toBeInTheDocument();
      expect(screen.getByText('May 23, 2025')).toBeInTheDocument();
      expect(screen.getByText('Jun 21, 2025')).toBeInTheDocument();
    });

    it('should render event categories', () => {
      render(<CulturalCalendar events={mockEvents} />);

      expect(screen.getByText('national')).toBeInTheDocument();
      expect(screen.getAllByText('religious')).toHaveLength(2);
    });

    it('should render calendar icon', () => {
      const { container } = render(<CulturalCalendar events={mockEvents} />);

      const icon = container.querySelector('[data-icon="calendar"]');
      expect(icon).toBeInTheDocument();
    });
  });

  describe('empty state', () => {
    it('should render message when no events provided', () => {
      render(<CulturalCalendar events={[]} />);

      expect(screen.getByText(/no upcoming events/i)).toBeInTheDocument();
    });

    it('should render title even when no events', () => {
      render(<CulturalCalendar events={[]} />);

      expect(screen.getByText('Cultural Calendar')).toBeInTheDocument();
    });
  });

  describe('event ordering', () => {
    it('should display events in chronological order', () => {
      const unorderedEvents = [
        {
          id: '1',
          name: 'Event C',
          date: '2025-06-01',
          category: 'religious' as const,
        },
        {
          id: '2',
          name: 'Event A',
          date: '2025-04-01',
          category: 'national' as const,
        },
        {
          id: '3',
          name: 'Event B',
          date: '2025-05-01',
          category: 'cultural' as const,
        },
      ];

      render(<CulturalCalendar events={unorderedEvents} />);

      const eventNames = screen.getAllByTestId('event-name');
      expect(eventNames[0]).toHaveTextContent('Event A');
      expect(eventNames[1]).toHaveTextContent('Event B');
      expect(eventNames[2]).toHaveTextContent('Event C');
    });
  });

  describe('styling', () => {
    it('should use Card component structure', () => {
      const { container } = render(<CulturalCalendar events={mockEvents} />);

      const card = container.querySelector('.rounded-lg.border.bg-card');
      expect(card).toBeInTheDocument();
    });

    it('should accept custom className', () => {
      const { container } = render(
        <CulturalCalendar events={mockEvents} className="custom-class" />
      );

      const wrapper = container.firstChild as HTMLElement;
      expect(wrapper).toHaveClass('custom-class');
    });
  });

  describe('accessibility', () => {
    it('should have proper semantic structure with list', () => {
      render(<CulturalCalendar events={mockEvents} />);

      const list = screen.getByRole('list');
      expect(list).toBeInTheDocument();
    });

    it('should have proper list items for each event', () => {
      render(<CulturalCalendar events={mockEvents} />);

      const listItems = screen.getAllByRole('listitem');
      expect(listItems).toHaveLength(3);
    });

    it('should support aria-label', () => {
      render(
        <CulturalCalendar
          events={mockEvents}
          aria-label="Upcoming cultural and religious events"
        />
      );

      const calendar = screen.getByLabelText(/upcoming cultural and religious events/i);
      expect(calendar).toBeInTheDocument();
    });
  });

  describe('date formatting', () => {
    it('should format dates in readable format', () => {
      const events = [
        {
          id: '1',
          name: 'Test Event',
          date: '2025-12-25',
          category: 'holiday' as const,
        },
      ];

      render(<CulturalCalendar events={events} />);

      expect(screen.getByText('Dec 25, 2025')).toBeInTheDocument();
    });
  });

  describe('category badges', () => {
    it('should display national category with appropriate styling', () => {
      const events = [
        {
          id: '1',
          name: 'National Day',
          date: '2025-02-04',
          category: 'national' as const,
        },
      ];

      render(<CulturalCalendar events={events} />);

      const badge = screen.getByText('national');
      expect(badge).toHaveClass('bg-blue-100', 'text-blue-800');
    });

    it('should display religious category with appropriate styling', () => {
      const events = [
        {
          id: '1',
          name: 'Vesak',
          date: '2025-05-23',
          category: 'religious' as const,
        },
      ];

      render(<CulturalCalendar events={events} />);

      const badge = screen.getByText('religious');
      expect(badge).toHaveClass('bg-purple-100', 'text-purple-800');
    });

    it('should display cultural category with appropriate styling', () => {
      const events = [
        {
          id: '1',
          name: 'Cultural Festival',
          date: '2025-03-15',
          category: 'cultural' as const,
        },
      ];

      render(<CulturalCalendar events={events} />);

      const badge = screen.getByText('cultural');
      expect(badge).toHaveClass('bg-green-100', 'text-green-800');
    });
  });
});
