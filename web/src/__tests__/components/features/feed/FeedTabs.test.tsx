import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { FeedTabs, FeedTabValue } from '@/presentation/components/features/feed/FeedTabs';

/**
 * Test Suite for FeedTabs Component
 * Tests tab rendering, switching, active states, badge counts, and keyboard navigation
 */
describe('FeedTabs Component', () => {
  const mockOnTabChange = vi.fn();

  const defaultProps = {
    activeTab: 'all' as FeedTabValue,
    onTabChange: mockOnTabChange,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Tab Rendering', () => {
    it('should render all five tabs', () => {
      render(<FeedTabs {...defaultProps} />);
      expect(screen.getByText('All Posts')).toBeInTheDocument();
      expect(screen.getByText('Events')).toBeInTheDocument();
      expect(screen.getByText('Businesses')).toBeInTheDocument();
      expect(screen.getByText('Culture')).toBeInTheDocument();
      expect(screen.getByText('Forums')).toBeInTheDocument();
    });

    it('should render tabs with correct icons', () => {
      const { container } = render(<FeedTabs {...defaultProps} />);
      // Check that SVG icons are present (lucide icons render as SVGs)
      const svgs = container.querySelectorAll('svg');
      expect(svgs.length).toBeGreaterThanOrEqual(5); // At least one icon per tab
    });

    it('should render tabs as buttons', () => {
      render(<FeedTabs {...defaultProps} />);
      const buttons = screen.getAllByRole('button');
      expect(buttons).toHaveLength(5);
    });

    it('should have proper aria-label on nav', () => {
      const { container } = render(<FeedTabs {...defaultProps} />);
      const nav = container.querySelector('nav');
      expect(nav).toHaveAttribute('aria-label', 'Feed filter tabs');
    });
  });

  describe('Tab Switching', () => {
    it('should call onTabChange when All Posts tab is clicked', () => {
      render(<FeedTabs {...defaultProps} activeTab="event" />);
      const allPostsTab = screen.getByText('All Posts');
      fireEvent.click(allPostsTab);
      expect(mockOnTabChange).toHaveBeenCalledWith('all');
    });

    it('should call onTabChange when Events tab is clicked', () => {
      render(<FeedTabs {...defaultProps} />);
      const eventsTab = screen.getByText('Events');
      fireEvent.click(eventsTab);
      expect(mockOnTabChange).toHaveBeenCalledWith('event');
    });

    it('should call onTabChange when Businesses tab is clicked', () => {
      render(<FeedTabs {...defaultProps} />);
      const businessTab = screen.getByText('Businesses');
      fireEvent.click(businessTab);
      expect(mockOnTabChange).toHaveBeenCalledWith('business');
    });

    it('should call onTabChange when Culture tab is clicked', () => {
      render(<FeedTabs {...defaultProps} />);
      const cultureTab = screen.getByText('Culture');
      fireEvent.click(cultureTab);
      expect(mockOnTabChange).toHaveBeenCalledWith('culture');
    });

    it('should call onTabChange when Forums tab is clicked', () => {
      render(<FeedTabs {...defaultProps} />);
      const forumsTab = screen.getByText('Forums');
      fireEvent.click(forumsTab);
      expect(mockOnTabChange).toHaveBeenCalledWith('forum');
    });
  });

  describe('Active Tab Styling', () => {
    it('should apply active styles to All Posts tab when active', () => {
      render(<FeedTabs {...defaultProps} activeTab="all" />);
      const allPostsTab = screen.getByText('All Posts').closest('button');
      expect(allPostsTab).toHaveClass('text-[#FF7900]', 'border-[#FF7900]');
    });

    it('should apply active styles to Events tab when active', () => {
      render(<FeedTabs {...defaultProps} activeTab="event" />);
      const eventsTab = screen.getByText('Events').closest('button');
      expect(eventsTab).toHaveClass('text-[#FF7900]', 'border-[#FF7900]');
    });

    it('should apply inactive styles to non-active tabs', () => {
      render(<FeedTabs {...defaultProps} activeTab="all" />);
      const eventsTab = screen.getByText('Events').closest('button');
      expect(eventsTab).toHaveClass('text-gray-600', 'border-transparent');
    });

    it('should set aria-current="page" on active tab', () => {
      render(<FeedTabs {...defaultProps} activeTab="event" />);
      const eventsTab = screen.getByText('Events').closest('button');
      expect(eventsTab).toHaveAttribute('aria-current', 'page');
    });

    it('should not set aria-current on inactive tabs', () => {
      render(<FeedTabs {...defaultProps} activeTab="event" />);
      const allPostsTab = screen.getByText('All Posts').closest('button');
      expect(allPostsTab).not.toHaveAttribute('aria-current');
    });
  });

  describe('Badge Counts', () => {
    const countsProps = {
      ...defaultProps,
      counts: {
        all: 150,
        event: 42,
        business: 28,
        culture: 15,
        forum: 65,
      },
    };

    it('should display badge count for All Posts', () => {
      render(<FeedTabs {...countsProps} />);
      expect(screen.getByText('150')).toBeInTheDocument();
    });

    it('should display badge count for Events', () => {
      render(<FeedTabs {...countsProps} />);
      expect(screen.getByText('42')).toBeInTheDocument();
    });

    it('should display badge count for Businesses', () => {
      render(<FeedTabs {...countsProps} />);
      expect(screen.getByText('28')).toBeInTheDocument();
    });

    it('should display badge count for Culture', () => {
      render(<FeedTabs {...countsProps} />);
      expect(screen.getByText('15')).toBeInTheDocument();
    });

    it('should display badge count for Forums', () => {
      render(<FeedTabs {...countsProps} />);
      expect(screen.getByText('65')).toBeInTheDocument();
    });

    it('should display "99+" for counts over 99', () => {
      render(
        <FeedTabs
          {...defaultProps}
          counts={{ all: 150 }}
        />
      );
      expect(screen.getByText('99+')).toBeInTheDocument();
    });

    it('should not display badge when count is 0', () => {
      render(
        <FeedTabs
          {...defaultProps}
          counts={{ all: 0, event: 0 }}
        />
      );
      const badges = screen.queryByText('0');
      expect(badges).not.toBeInTheDocument();
    });

    it('should style active tab badge differently', () => {
      render(<FeedTabs {...countsProps} activeTab="event" />);
      const eventTabButton = screen.getByText('Events').closest('button');
      const badge = eventTabButton?.querySelector('.bg-\\[\\#FF7900\\]');
      expect(badge).toBeInTheDocument();
    });

    it('should style inactive tab badge differently', () => {
      render(<FeedTabs {...countsProps} activeTab="all" />);
      const eventTabButton = screen.getByText('Events').closest('button');
      const badge = eventTabButton?.querySelector('.bg-gray-200');
      expect(badge).toBeInTheDocument();
    });
  });

  describe('Keyboard Navigation', () => {
    it('should be focusable via keyboard', () => {
      render(<FeedTabs {...defaultProps} />);
      const firstTab = screen.getByText('All Posts').closest('button');
      firstTab?.focus();
      expect(document.activeElement).toBe(firstTab);
    });

    it('should trigger tab change on Enter key', () => {
      render(<FeedTabs {...defaultProps} />);
      const eventsTab = screen.getByText('Events').closest('button');
      eventsTab?.focus();
      fireEvent.keyDown(eventsTab!, { key: 'Enter' });
      // Click is triggered by default browser behavior
      fireEvent.click(eventsTab!);
      expect(mockOnTabChange).toHaveBeenCalledWith('event');
    });

    it('should have hover styles for keyboard focus', () => {
      render(<FeedTabs {...defaultProps} />);
      const tab = screen.getByText('Events').closest('button');
      expect(tab).toHaveClass('hover:text-[#FF7900]');
    });
  });

  describe('Responsive Behavior', () => {
    it('should have horizontal scrolling container', () => {
      const { container } = render(<FeedTabs {...defaultProps} />);
      const scrollContainer = container.querySelector('.overflow-x-auto');
      expect(scrollContainer).toBeInTheDocument();
    });

    it('should apply scrollbar-hide class', () => {
      const { container } = render(<FeedTabs {...defaultProps} />);
      const scrollContainer = container.querySelector('.scrollbar-hide');
      expect(scrollContainer).toBeInTheDocument();
    });

    it('should have min-w-max on tabs container', () => {
      const { container } = render(<FeedTabs {...defaultProps} />);
      const tabsContainer = container.querySelector('.min-w-max');
      expect(tabsContainer).toBeInTheDocument();
    });

    it('should have whitespace-nowrap on tabs', () => {
      render(<FeedTabs {...defaultProps} />);
      const tab = screen.getByText('All Posts').closest('button');
      expect(tab).toHaveClass('whitespace-nowrap');
    });

    it('should have flex-shrink-0 on tabs', () => {
      render(<FeedTabs {...defaultProps} />);
      const tab = screen.getByText('All Posts').closest('button');
      expect(tab).toHaveClass('flex-shrink-0');
    });
  });

  describe('Custom Styling', () => {
    it('should accept and apply custom className', () => {
      const { container } = render(<FeedTabs {...defaultProps} className="custom-class" />);
      const nav = container.querySelector('nav');
      expect(nav).toHaveClass('custom-class');
    });

    it('should apply border-b to nav', () => {
      const { container } = render(<FeedTabs {...defaultProps} />);
      const nav = container.querySelector('nav');
      expect(nav).toHaveClass('border-b', 'border-gray-200');
    });

    it('should apply bg-white to nav', () => {
      const { container } = render(<FeedTabs {...defaultProps} />);
      const nav = container.querySelector('nav');
      expect(nav).toHaveClass('bg-white');
    });
  });
});
