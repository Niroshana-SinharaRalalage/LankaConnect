import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { FeedCard } from '@/presentation/components/features/feed/FeedCard';
import { FeedItem } from '@/domain/models/FeedItem';

/**
 * Test Suite for FeedCard Component
 * Tests rendering for different feed types, author display, actions, and type badges
 */
describe('FeedCard Component', () => {
  const mockOnClick = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Event Feed Type', () => {
    const eventItem: FeedItem = {
      id: 'event-1',
      type: 'event',
      title: 'Sri Lankan New Year Celebration',
      description: 'Join us for a traditional New Year celebration',
      author: {
        id: 'user-1',
        name: 'John Doe',
        initials: 'JD',
      },
      timestamp: new Date('2025-01-01T10:00:00Z'),
      location: 'Los Angeles, CA',
      metadata: {
        type: 'event',
        date: 'April 14, 2025',
        time: '2:00 PM',
        interestedCount: 42,
        commentCount: 5,
      },
      actions: [
        { icon: '👍', label: 'Interested', count: 42 },
        { icon: '💬', label: 'Comments', count: 5 },
      ],
    };

    it('should render event feed card with correct title', () => {
      render(<FeedCard item={eventItem} />);
      expect(screen.getByText('Sri Lankan New Year Celebration')).toBeInTheDocument();
    });

    it('should render Event type badge', () => {
      render(<FeedCard item={eventItem} />);
      expect(screen.getByText('Event')).toBeInTheDocument();
    });

    it('should display event date with calendar icon', () => {
      render(<FeedCard item={eventItem} />);
      expect(screen.getByText('April 14, 2025')).toBeInTheDocument();
    });

    it('should render event actions with correct counts', () => {
      render(<FeedCard item={eventItem} />);
      expect(screen.getByText('Interested')).toBeInTheDocument();
      expect(screen.getByText('(42)')).toBeInTheDocument();
      expect(screen.getByText('Comments')).toBeInTheDocument();
      expect(screen.getByText('(5)')).toBeInTheDocument();
    });
  });

  describe('Business Feed Type', () => {
    const businessItem: FeedItem = {
      id: 'business-1',
      type: 'business',
      title: 'Ceylon Spice Market',
      description: 'Authentic Sri Lankan spices and groceries',
      author: {
        id: 'user-2',
        name: 'Jane Smith',
        initials: 'JS',
      },
      timestamp: new Date('2025-01-02T14:00:00Z'),
      location: 'New York, NY',
      metadata: {
        type: 'business',
        category: 'Grocery Store',
        rating: 4.5,
        likesCount: 78,
        commentsCount: 12,
      },
      actions: [
        { icon: '❤️', label: 'Like', count: 78 },
        { icon: '💬', label: 'Reviews', count: 12 },
      ],
    };

    it('should render business feed card with correct title', () => {
      render(<FeedCard item={businessItem} />);
      expect(screen.getByText('Ceylon Spice Market')).toBeInTheDocument();
    });

    it('should render Business type badge', () => {
      render(<FeedCard item={businessItem} />);
      expect(screen.getByText('Business')).toBeInTheDocument();
    });

    it('should display business category and rating', () => {
      render(<FeedCard item={businessItem} />);
      expect(screen.getByText('Grocery Store')).toBeInTheDocument();
      expect(screen.getByText('★ 4.5')).toBeInTheDocument();
    });

    it('should apply correct border color for business type', () => {
      const { container } = render(<FeedCard item={businessItem} />);
      const card = container.querySelector('.border-l-4');
      expect(card).toHaveClass('border-[#10B981]');
    });
  });

  describe('Forum Feed Type', () => {
    const forumItem: FeedItem = {
      id: 'forum-1',
      type: 'forum',
      title: 'Best places to find Sri Lankan food?',
      description: 'Looking for recommendations for authentic Sri Lankan restaurants',
      author: {
        id: 'user-3',
        name: 'Mike Johnson',
        initials: 'MJ',
      },
      timestamp: new Date('2025-01-03T16:00:00Z'),
      location: 'Chicago, IL',
      metadata: {
        type: 'forum',
        forumName: 'Food & Dining',
        repliesCount: 15,
        helpfulCount: 23,
      },
      actions: [
        { icon: '👍', label: 'Upvote', count: 23 },
        { icon: '💬', label: 'Reply', count: 15 },
      ],
    };

    it('should render forum feed card with correct title', () => {
      render(<FeedCard item={forumItem} />);
      expect(screen.getByText('Best places to find Sri Lankan food?')).toBeInTheDocument();
    });

    it('should render Discussion type badge', () => {
      render(<FeedCard item={forumItem} />);
      expect(screen.getByText('Discussion')).toBeInTheDocument();
    });

    it('should display forum name', () => {
      render(<FeedCard item={forumItem} />);
      expect(screen.getByText('Food & Dining')).toBeInTheDocument();
    });

    it('should apply correct border color for forum type', () => {
      const { container } = render(<FeedCard item={forumItem} />);
      const card = container.querySelector('.border-l-4');
      expect(card).toHaveClass('border-[#3B82F6]');
    });
  });

  describe('Culture Feed Type', () => {
    const cultureItem: FeedItem = {
      id: 'culture-1',
      type: 'culture',
      title: 'Traditional Kandyan Dance Performance',
      description: 'Learn about the history and significance of Kandyan dance',
      author: {
        id: 'user-4',
        name: 'Sarah Williams',
        initials: 'SW',
      },
      timestamp: new Date('2025-01-04T18:00:00Z'),
      location: 'San Francisco, CA',
      metadata: {
        type: 'culture',
        category: 'Dance & Music',
        resourcesCount: 34,
        repliesCount: 89,
      },
      actions: [
        { icon: '❤️', label: 'Love', count: 89 },
        { icon: '📚', label: 'Learn More', count: 34 },
      ],
    };

    it('should render culture feed card with correct title', () => {
      render(<FeedCard item={cultureItem} />);
      expect(screen.getByText('Traditional Kandyan Dance Performance')).toBeInTheDocument();
    });

    it('should render Culture type badge', () => {
      render(<FeedCard item={cultureItem} />);
      expect(screen.getByText('Culture')).toBeInTheDocument();
    });

    it('should display culture category', () => {
      render(<FeedCard item={cultureItem} />);
      expect(screen.getByText('Dance & Music')).toBeInTheDocument();
    });

    it('should apply correct border color for culture type', () => {
      const { container } = render(<FeedCard item={cultureItem} />);
      const card = container.querySelector('.border-l-4');
      expect(card).toHaveClass('border-[#8B5CF6]');
    });
  });

  describe('Author Display', () => {
    const testItem: FeedItem = {
      id: 'test-1',
      type: 'event',
      title: 'Test Event',
      description: 'Test Description',
      author: {
        id: 'user-1',
        name: 'Alice Brown',
        initials: 'AB',
      },
      timestamp: new Date('2025-01-01T12:00:00Z'),
      location: 'Test Location',
      metadata: { type: 'event', date: 'Jan 1', time: '12:00 PM', interestedCount: 0, commentCount: 0 },
      actions: [],
    };

    it('should display author name', () => {
      render(<FeedCard item={testItem} />);
      expect(screen.getByText('Alice Brown')).toBeInTheDocument();
    });

    it('should display author initials in avatar', () => {
      render(<FeedCard item={testItem} />);
      const avatar = screen.getByText('AB');
      expect(avatar).toBeInTheDocument();
      expect(avatar).toHaveStyle({ background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)' });
    });

    it('should display location when provided', () => {
      const itemWithLocation = { ...testItem, location: 'Boston, MA' };
      render(<FeedCard item={itemWithLocation} />);
      expect(screen.getByText('Boston, MA')).toBeInTheDocument();
    });
  });

  describe('Timestamp Formatting', () => {
    it('should format recent timestamp as relative time', () => {
      const recentItem: FeedItem = {
        id: 'recent-1',
        type: 'event',
        title: 'Recent Event',
        description: 'Test',
        author: { id: 'user-1', name: 'Test', initials: 'T' },
        timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000), // 2 hours ago
        location: 'Test Location',
        metadata: { type: 'event', date: 'Today', time: '10:00 AM', interestedCount: 0, commentCount: 0 },
        actions: [],
      };

      render(<FeedCard item={recentItem} />);
      expect(screen.getByText(/2 hours ago/i)).toBeInTheDocument();
    });
  });

  describe('Card Interactions', () => {
    const testItem: FeedItem = {
      id: 'test-1',
      type: 'event',
      title: 'Test Event',
      description: 'Test Description',
      author: { id: 'user-1', name: 'Test', initials: 'T' },
      timestamp: new Date(),
      location: 'Test Location',
      metadata: { type: 'event', date: 'Today', time: '10:00 AM', interestedCount: 0, commentCount: 0 },
      actions: [{ icon: '👍', label: 'Like', count: 10 }],
    };

    it('should call onClick when card is clicked', () => {
      render(<FeedCard item={testItem} onClick={mockOnClick} />);
      const card = screen.getByText('Test Event').closest('div')?.parentElement?.parentElement;
      fireEvent.click(card!);
      expect(mockOnClick).toHaveBeenCalledWith(testItem);
    });

    it('should apply hover styles', () => {
      const { container } = render(<FeedCard item={testItem} />);
      const card = container.querySelector('.hover\\:bg-\\[\\#fff9f5\\]');
      expect(card).toBeInTheDocument();
    });

    it('should have cursor-pointer class', () => {
      const { container } = render(<FeedCard item={testItem} />);
      const card = container.querySelector('.cursor-pointer');
      expect(card).toBeInTheDocument();
    });
  });
});
