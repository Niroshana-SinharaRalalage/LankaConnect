import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { ActivityFeed } from '@/presentation/components/features/feed/ActivityFeed';
import { FeedItem } from '@/domain/models/FeedItem';

/**
 * Test Suite for ActivityFeed Component
 * Tests feed rendering, loading states, empty states, pagination, and item interactions
 */
describe('ActivityFeed Component', () => {
  const mockOnItemClick = vi.fn();
  const mockOnLoadMore = vi.fn();

  const mockFeedItems: FeedItem[] = [
    {
      id: 'item-1',
      type: 'event',
      title: 'Community Gathering',
      description: 'Join us for a community event',
      author: { id: 'user-1', name: 'John Doe', initials: 'JD' },
      timestamp: new Date('2025-01-01T10:00:00Z'),
      location: 'Los Angeles, CA',
      metadata: { type: 'event', date: 'Jan 1', time: '10:00 AM', interestedCount: 10, commentCount: 5 },
      actions: [{ icon: '👍', label: 'Interested', count: 10 }],
    },
    {
      id: 'item-2',
      type: 'business',
      title: 'Ceylon Spice Shop',
      description: 'Authentic spices from Sri Lanka',
      author: { id: 'user-2', name: 'Jane Smith', initials: 'JS' },
      timestamp: new Date('2025-01-02T14:00:00Z'),
      location: 'New York, NY',
      metadata: { type: 'business', category: 'Grocery', rating: 4.5, likesCount: 20, commentsCount: 5 },
      actions: [{ icon: '❤️', label: 'Like', count: 20 }],
    },
    {
      id: 'item-3',
      type: 'forum',
      title: 'Best restaurants?',
      description: 'Looking for Sri Lankan restaurant recommendations',
      author: { id: 'user-3', name: 'Mike Johnson', initials: 'MJ' },
      timestamp: new Date('2025-01-03T16:00:00Z'),
      location: 'Chicago, IL',
      metadata: { type: 'forum', forumName: 'Food Discussion', repliesCount: 15, helpfulCount: 10 },
      actions: [{ icon: '💬', label: 'Reply', count: 15 }],
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Render Feed Items', () => {
    it('should render all feed items', () => {
      render(<ActivityFeed items={mockFeedItems} />);
      expect(screen.getByText('Community Gathering')).toBeInTheDocument();
      expect(screen.getByText('Ceylon Spice Shop')).toBeInTheDocument();
      expect(screen.getByText('Best restaurants?')).toBeInTheDocument();
    });

    it('should render feed items in correct order', () => {
      render(<ActivityFeed items={mockFeedItems} />);
      const titles = screen.getAllByRole('heading', { level: 2 });
      expect(titles[0]).toHaveTextContent('Community Gathering');
      expect(titles[1]).toHaveTextContent('Ceylon Spice Shop');
      expect(titles[2]).toHaveTextContent('Best restaurants?');
    });

    it('should pass onClick handler to FeedCard components', () => {
      render(<ActivityFeed items={mockFeedItems} onItemClick={mockOnItemClick} />);
      const firstCard = screen.getByText('Community Gathering').closest('div')?.parentElement?.parentElement;
      fireEvent.click(firstCard!);
      expect(mockOnItemClick).toHaveBeenCalledWith(mockFeedItems[0]);
    });

    it('should render items with spacing', () => {
      const { container } = render(<ActivityFeed items={mockFeedItems} />);
      const itemsContainer = container.querySelector('.space-y-4');
      expect(itemsContainer).toBeInTheDocument();
    });
  });

  describe('Loading State', () => {
    it('should render skeleton loaders when loading', () => {
      const { container } = render(<ActivityFeed items={[]} loading={true} />);
      const skeletons = container.querySelectorAll('.animate-pulse');
      expect(skeletons.length).toBeGreaterThan(0);
    });

    it('should render 3 skeleton cards when loading', () => {
      const { container } = render(<ActivityFeed items={[]} loading={true} />);
      const skeletons = container.querySelectorAll('.animate-pulse');
      expect(skeletons).toHaveLength(3);
    });

    it('should not render feed items when loading', () => {
      render(<ActivityFeed items={mockFeedItems} loading={true} />);
      expect(screen.queryByText('Community Gathering')).not.toBeInTheDocument();
    });

    it('should not render empty state when loading', () => {
      render(<ActivityFeed items={[]} loading={true} />);
      expect(screen.queryByText('No posts yet')).not.toBeInTheDocument();
    });
  });

  describe('Empty State', () => {
    it('should render empty state when no items', () => {
      render(<ActivityFeed items={[]} />);
      expect(screen.getByText('No posts yet')).toBeInTheDocument();
    });

    it('should display custom empty message', () => {
      const customMessage = 'Start creating your first post!';
      render(<ActivityFeed items={[]} emptyMessage={customMessage} />);
      expect(screen.getByText(customMessage)).toBeInTheDocument();
    });

    it('should render empty state icon', () => {
      const { container } = render(<ActivityFeed items={[]} />);
      const icon = container.querySelector('.w-16.h-16');
      expect(icon).toBeInTheDocument();
    });

    it('should render "Create Your First Post" button in empty state', () => {
      render(<ActivityFeed items={[]} />);
      expect(screen.getByText('Create Your First Post')).toBeInTheDocument();
    });

    it('should not render empty state when items exist', () => {
      render(<ActivityFeed items={mockFeedItems} />);
      expect(screen.queryByText('No posts yet')).not.toBeInTheDocument();
    });

    it('should not render empty state when loading', () => {
      render(<ActivityFeed items={[]} loading={true} />);
      expect(screen.queryByText('No posts yet')).not.toBeInTheDocument();
    });
  });

  describe('Pagination - Local', () => {
    const manyItems = Array.from({ length: 25 }, (_, i) => ({
      id: `item-${i}`,
      type: 'event' as const,
      title: `Event ${i}`,
      description: `Description ${i}`,
      author: { id: `user-${i}`, name: `User ${i}`, initials: 'U' },
      timestamp: new Date(),
      location: 'Test Location',
      metadata: { type: 'event' as const, date: 'Today', time: '10:00 AM', interestedCount: 0, commentCount: 0 },
      actions: [],
    }));

    it('should initially display itemsPerPage items (default 10)', () => {
      render(<ActivityFeed items={manyItems} />);
      const titles = screen.getAllByRole('heading', { level: 2 });
      expect(titles).toHaveLength(10);
    });

    it('should display custom itemsPerPage', () => {
      render(<ActivityFeed items={manyItems} itemsPerPage={5} />);
      const titles = screen.getAllByRole('heading', { level: 2 });
      expect(titles).toHaveLength(5);
    });

    it('should show "Load More Posts" button when more items available', () => {
      render(<ActivityFeed items={manyItems} />);
      expect(screen.getByText('Load More Posts')).toBeInTheDocument();
    });

    it('should load more items when button is clicked', () => {
      render(<ActivityFeed items={manyItems} itemsPerPage={10} />);
      const loadMoreButton = screen.getByText('Load More Posts');
      fireEvent.click(loadMoreButton);
      const titles = screen.getAllByRole('heading', { level: 2 });
      expect(titles).toHaveLength(20); // 10 + 10
    });

    it('should hide "Load More" button when all items displayed', () => {
      render(<ActivityFeed items={mockFeedItems} itemsPerPage={10} />);
      expect(screen.queryByText('Load More Posts')).not.toBeInTheDocument();
    });
  });

  describe('Pagination - External', () => {
    it('should call onLoadMore when provided and button clicked', () => {
      render(<ActivityFeed items={mockFeedItems} hasMore={true} onLoadMore={mockOnLoadMore} />);
      const loadMoreButton = screen.getByText('Load More Posts');
      fireEvent.click(loadMoreButton);
      expect(mockOnLoadMore).toHaveBeenCalled();
    });

    it('should show "Load More" button when hasMore is true', () => {
      render(<ActivityFeed items={mockFeedItems} hasMore={true} onLoadMore={mockOnLoadMore} />);
      expect(screen.getByText('Load More Posts')).toBeInTheDocument();
    });

    it('should hide "Load More" button when hasMore is false', () => {
      render(<ActivityFeed items={mockFeedItems} hasMore={false} onLoadMore={mockOnLoadMore} />);
      expect(screen.queryByText('Load More Posts')).not.toBeInTheDocument();
    });

    it('should disable button when loadingMore is true', () => {
      render(<ActivityFeed items={mockFeedItems} hasMore={true} onLoadMore={mockOnLoadMore} loadingMore={true} />);
      const loadMoreButton = screen.getByText('Loading...');
      expect(loadMoreButton).toBeDisabled();
    });

    it('should show loading spinner when loadingMore is true', () => {
      render(<ActivityFeed items={mockFeedItems} hasMore={true} onLoadMore={mockOnLoadMore} loadingMore={true} />);
      expect(screen.getByText('Loading...')).toBeInTheDocument();
    });

    it('should render loading skeletons when loadingMore is true', () => {
      const { container } = render(
        <ActivityFeed items={mockFeedItems} hasMore={true} onLoadMore={mockOnLoadMore} loadingMore={true} />
      );
      const skeletons = container.querySelectorAll('.animate-pulse');
      expect(skeletons.length).toBeGreaterThanOrEqual(2);
    });
  });

  describe('Item Click Handler', () => {
    it('should call onItemClick with correct item when clicked', () => {
      render(<ActivityFeed items={mockFeedItems} onItemClick={mockOnItemClick} />);
      const secondCard = screen.getByText('Ceylon Spice Shop').closest('div')?.parentElement?.parentElement;
      fireEvent.click(secondCard!);
      expect(mockOnItemClick).toHaveBeenCalledWith(mockFeedItems[1]);
    });

    it('should not throw error when onItemClick is not provided', () => {
      render(<ActivityFeed items={mockFeedItems} />);
      const firstCard = screen.getByText('Community Gathering').closest('div')?.parentElement?.parentElement;
      expect(() => fireEvent.click(firstCard!)).not.toThrow();
    });
  });

  describe('Custom Styling', () => {
    it('should accept and apply custom className', () => {
      const { container } = render(<ActivityFeed items={mockFeedItems} className="custom-class" />);
      const rootDiv = container.firstChild;
      expect(rootDiv).toHaveClass('custom-class');
    });

    it('should apply custom className to empty state', () => {
      const { container } = render(<ActivityFeed items={[]} className="custom-empty" />);
      const rootDiv = container.firstChild;
      expect(rootDiv).toHaveClass('custom-empty');
    });

    it('should apply custom className to loading state', () => {
      const { container } = render(<ActivityFeed items={[]} loading={true} className="custom-loading" />);
      const rootDiv = container.firstChild;
      expect(rootDiv).toHaveClass('custom-loading');
    });
  });

  describe('Load More Button Styling', () => {
    it('should have proper styling on load more button', () => {
      render(<ActivityFeed items={Array(15).fill(mockFeedItems[0])} />);
      const button = screen.getByText('Load More Posts');
      expect(button).toHaveClass('min-w-[200px]');
    });

    it('should center load more button', () => {
      const { container } = render(<ActivityFeed items={Array(15).fill(mockFeedItems[0])} />);
      const buttonContainer = container.querySelector('.flex.justify-center');
      expect(buttonContainer).toBeInTheDocument();
    });
  });
});
