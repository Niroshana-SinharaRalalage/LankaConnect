import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { CommunityStats } from '@/presentation/components/features/dashboard/CommunityStats';

describe('CommunityStats Component', () => {
  const mockStats = {
    activeUsers: 1248,
    recentPosts: 342,
    upcomingEvents: 18,
  };

  describe('rendering', () => {
    it('should render community stats with title', () => {
      render(<CommunityStats stats={mockStats} />);

      expect(screen.getByText('Community Statistics')).toBeInTheDocument();
    });

    it('should render active users stat', () => {
      render(<CommunityStats stats={mockStats} />);

      expect(screen.getByText('Active Users')).toBeInTheDocument();
      expect(screen.getByText('1,248')).toBeInTheDocument();
    });

    it('should render recent posts stat', () => {
      render(<CommunityStats stats={mockStats} />);

      expect(screen.getByText('Recent Posts')).toBeInTheDocument();
      expect(screen.getByText('342')).toBeInTheDocument();
    });

    it('should render upcoming events stat', () => {
      render(<CommunityStats stats={mockStats} />);

      expect(screen.getByText('Upcoming Events')).toBeInTheDocument();
      expect(screen.getByText('18')).toBeInTheDocument();
    });
  });

  describe('StatCard integration', () => {
    it('should use StatCard components for each stat', () => {
      const { container } = render(<CommunityStats stats={mockStats} />);

      const statCards = container.querySelectorAll('.rounded-lg.shadow-sm');
      expect(statCards.length).toBeGreaterThanOrEqual(3);
    });

    it('should display icons for each stat', () => {
      const { container } = render(<CommunityStats stats={mockStats} />);

      const icons = container.querySelectorAll('[data-icon="users"], [data-icon="message-circle"], [data-icon="calendar"]');
      expect(icons.length).toBeGreaterThanOrEqual(3);
    });
  });

  describe('number formatting', () => {
    it('should format large numbers with commas', () => {
      const stats = {
        activeUsers: 123456,
        recentPosts: 7890,
        upcomingEvents: 123,
      };

      render(<CommunityStats stats={stats} />);

      expect(screen.getByText('123,456')).toBeInTheDocument();
      expect(screen.getByText('7,890')).toBeInTheDocument();
      expect(screen.getByText('123')).toBeInTheDocument();
    });

    it('should handle zero values', () => {
      const stats = {
        activeUsers: 0,
        recentPosts: 0,
        upcomingEvents: 0,
      };

      render(<CommunityStats stats={stats} />);

      expect(screen.getAllByText('0')).toHaveLength(3);
    });

    it('should handle single digit numbers', () => {
      const stats = {
        activeUsers: 5,
        recentPosts: 3,
        upcomingEvents: 8,
      };

      render(<CommunityStats stats={stats} />);

      expect(screen.getByText('5')).toBeInTheDocument();
      expect(screen.getByText('3')).toBeInTheDocument();
      expect(screen.getByText('8')).toBeInTheDocument();
    });
  });

  describe('trends', () => {
    it('should display positive trend for active users', () => {
      const statsWithTrend = {
        activeUsers: 1248,
        activeUsersTrend: { value: '+12%', direction: 'up' as const },
        recentPosts: 342,
        upcomingEvents: 18,
      };

      render(<CommunityStats stats={statsWithTrend} />);

      expect(screen.getByText('+12%')).toBeInTheDocument();
    });

    it('should display negative trend for recent posts', () => {
      const statsWithTrend = {
        activeUsers: 1248,
        recentPosts: 342,
        recentPostsTrend: { value: '-5%', direction: 'down' as const },
        upcomingEvents: 18,
      };

      render(<CommunityStats stats={statsWithTrend} />);

      expect(screen.getByText('-5%')).toBeInTheDocument();
    });

    it('should display neutral trend for upcoming events', () => {
      const statsWithTrend = {
        activeUsers: 1248,
        recentPosts: 342,
        upcomingEvents: 18,
        upcomingEventsTrend: { value: '0%', direction: 'neutral' as const },
      };

      render(<CommunityStats stats={statsWithTrend} />);

      expect(screen.getByText('0%')).toBeInTheDocument();
    });
  });

  describe('time periods', () => {
    it('should display "Online now" subtitle for active users', () => {
      render(<CommunityStats stats={mockStats} />);

      expect(screen.getByText('Online now')).toBeInTheDocument();
    });

    it('should display "This week" subtitle for recent posts', () => {
      render(<CommunityStats stats={mockStats} />);

      expect(screen.getByText('This week')).toBeInTheDocument();
    });

    it('should display "Next 30 days" subtitle for upcoming events', () => {
      render(<CommunityStats stats={mockStats} />);

      expect(screen.getByText('Next 30 days')).toBeInTheDocument();
    });
  });

  describe('layout', () => {
    it('should display stats in grid layout', () => {
      const { container } = render(<CommunityStats stats={mockStats} />);

      const grid = container.querySelector('.grid');
      expect(grid).toBeInTheDocument();
    });

    it('should be responsive with proper grid columns', () => {
      const { container } = render(<CommunityStats stats={mockStats} />);

      const grid = container.querySelector('.grid');
      expect(grid).toHaveClass('grid-cols-1', 'md:grid-cols-3');
    });
  });

  describe('styling', () => {
    it('should accept custom className', () => {
      const { container } = render(
        <CommunityStats stats={mockStats} className="custom-class" />
      );

      const wrapper = container.firstChild as HTMLElement;
      expect(wrapper).toHaveClass('custom-class');
    });

    it('should use consistent spacing between stat cards', () => {
      const { container } = render(<CommunityStats stats={mockStats} />);

      const grid = container.querySelector('.grid');
      expect(grid).toHaveClass('gap-4');
    });
  });

  describe('accessibility', () => {
    it('should have proper semantic structure', () => {
      render(<CommunityStats stats={mockStats} />);

      expect(screen.getByText('Community Statistics')).toBeInTheDocument();
    });

    it('should support aria-label', () => {
      render(
        <CommunityStats
          stats={mockStats}
          aria-label="Real-time community statistics"
        />
      );

      const section = screen.getByLabelText(/real-time community statistics/i);
      expect(section).toBeInTheDocument();
    });

    it('should have descriptive stat labels', () => {
      render(<CommunityStats stats={mockStats} />);

      expect(screen.getByText('Active Users')).toBeInTheDocument();
      expect(screen.getByText('Recent Posts')).toBeInTheDocument();
      expect(screen.getByText('Upcoming Events')).toBeInTheDocument();
    });
  });

  describe('icons', () => {
    it('should display Users icon for active users stat', () => {
      const { container } = render(<CommunityStats stats={mockStats} />);

      const usersIcon = container.querySelector('[data-icon="users"]');
      expect(usersIcon).toBeInTheDocument();
    });

    it('should display MessageCircle icon for recent posts stat', () => {
      const { container } = render(<CommunityStats stats={mockStats} />);

      const messageIcon = container.querySelector('[data-icon="message-circle"]');
      expect(messageIcon).toBeInTheDocument();
    });

    it('should display Calendar icon for upcoming events stat', () => {
      const { container } = render(<CommunityStats stats={mockStats} />);

      const calendarIcon = container.querySelector('[data-icon="calendar"]');
      expect(calendarIcon).toBeInTheDocument();
    });
  });

  describe('loading state', () => {
    it('should display loading state when isLoading is true', () => {
      render(<CommunityStats stats={mockStats} isLoading={true} />);

      expect(screen.getByText(/loading/i)).toBeInTheDocument();
    });

    it('should show skeleton loaders instead of values when loading', () => {
      const { container } = render(<CommunityStats stats={mockStats} isLoading={true} />);

      const skeletons = container.querySelectorAll('.animate-pulse');
      expect(skeletons.length).toBeGreaterThan(0);
    });
  });

  describe('error state', () => {
    it('should display error message when error prop is provided', () => {
      render(<CommunityStats stats={mockStats} error="Failed to load statistics" />);

      expect(screen.getByText(/failed to load statistics/i)).toBeInTheDocument();
    });

    it('should still show title when error occurs', () => {
      render(<CommunityStats stats={mockStats} error="Failed to load statistics" />);

      expect(screen.getByText('Community Statistics')).toBeInTheDocument();
    });
  });
});
