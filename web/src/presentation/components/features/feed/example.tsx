/**
 * Feed Components Usage Example
 *
 * This file demonstrates how to use the feed display components
 * in a page or container component.
 *
 * NOTE: This is an example file for reference - not used in production.
 */

'use client';

import { useState, useMemo } from 'react';
import { FeedTabs, ActivityFeed, type FeedTabValue } from './';
import { FeedItem, createFeedItem } from '@/domain/models/FeedItem';

/**
 * Example usage of feed components
 */
export function FeedExample() {
  const [activeTab, setActiveTab] = useState<FeedTabValue>('all');
  const [loading, setLoading] = useState(false);

  /**
   * Mock feed data for demonstration
   */
  const mockFeedItems: FeedItem[] = useMemo(() => [
    createFeedItem({
      id: '1',
      type: 'event',
      author: {
        name: 'Priya Jayawardena',
        initials: 'PJ',
      },
      timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000), // 2 hours ago
      location: 'Colombo',
      title: 'Sinhala & Tamil New Year Celebration',
      description: 'Join us for a traditional New Year celebration with authentic Sri Lankan food, cultural performances, and games!',
      actions: [
        { icon: '👍', label: 'Interested', count: 24 },
        { icon: '💬', label: 'Comments', count: 8 },
      ],
      metadata: {
        type: 'event',
        date: 'April 14, 2025',
        interestedCount: 24,
        commentCount: 8,
      },
    }),
    createFeedItem({
      id: '2',
      type: 'business',
      author: {
        name: 'Ravi Silva',
        initials: 'RS',
      },
      timestamp: new Date(Date.now() - 5 * 60 * 60 * 1000), // 5 hours ago
      location: 'Los Angeles',
      title: 'Ceylon Spice House - Grand Opening!',
      description: 'Authentic Sri Lankan grocery store now open! Fresh spices, specialty ingredients, and traditional sweets.',
      actions: [
        { icon: '❤️', label: 'Like', count: 42 },
        { icon: '💬', label: 'Comments', count: 12 },
      ],
      metadata: {
        type: 'business',
        category: 'Grocery Store',
        rating: 4.8,
        likesCount: 42,
        commentsCount: 12,
      },
    }),
    createFeedItem({
      id: '3',
      type: 'forum',
      author: {
        name: 'Amara Fernando',
        initials: 'AF',
      },
      timestamp: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000), // 1 day ago
      location: 'Online',
      title: 'Tips for teaching Sinhala to kids abroad?',
      description: 'Looking for advice on teaching my children Sinhala. What resources, apps, or methods have worked for you?',
      actions: [
        { icon: '💬', label: 'Replies', count: 18 },
        { icon: '👏', label: 'Helpful', count: 15 },
      ],
      metadata: {
        type: 'forum',
        forumName: 'Language & Culture',
        repliesCount: 18,
        helpfulCount: 15,
      },
    }),
    createFeedItem({
      id: '4',
      type: 'culture',
      author: {
        name: 'Nadeesha Perera',
        initials: 'NP',
      },
      timestamp: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000), // 3 days ago
      location: 'Toronto',
      title: 'Traditional Kandyan Dance Lessons',
      description: 'Learn authentic Kandyan dance! Classes every Saturday for children and adults. Preserve our cultural heritage.',
      actions: [
        { icon: '📚', label: 'Resources', count: 5 },
        { icon: '💬', label: 'Replies', count: 9 },
      ],
      metadata: {
        type: 'culture',
        category: 'Dance & Arts',
        resourcesCount: 5,
        repliesCount: 9,
      },
    }),
  ], []);

  /**
   * Filter items based on active tab
   */
  const filteredItems = useMemo(() => {
    if (activeTab === 'all') {
      return mockFeedItems;
    }
    return mockFeedItems.filter(item => item.type === activeTab);
  }, [activeTab, mockFeedItems]);

  /**
   * Calculate counts for tab badges
   */
  const tabCounts = useMemo(() => {
    const counts: Partial<Record<FeedTabValue, number>> = {
      all: mockFeedItems.length,
    };

    mockFeedItems.forEach(item => {
      counts[item.type] = (counts[item.type] || 0) + 1;
    });

    return counts;
  }, [mockFeedItems]);

  /**
   * Handle feed item click
   */
  const handleItemClick = (item: FeedItem) => {
    console.log('Feed item clicked:', item);
    // In production: router.push(`/feed/${item.id}`)
  };

  /**
   * Simulate loading more items
   */
  const handleLoadMore = () => {
    setLoading(true);
    setTimeout(() => {
      setLoading(false);
      // In production: fetch more items from API
    }, 1000);
  };

  return (
    <div className="max-w-4xl mx-auto">
      {/* Page header */}
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">
          Community Feed
        </h1>
        <p className="text-gray-600">
          Stay connected with the Sri Lankan community
        </p>
      </div>

      {/* Feed tabs */}
      <div className="mb-6 bg-white rounded-lg shadow-sm overflow-hidden">
        <FeedTabs
          activeTab={activeTab}
          onTabChange={setActiveTab}
          counts={tabCounts}
        />
      </div>

      {/* Activity feed */}
      <ActivityFeed
        items={filteredItems}
        loading={loading}
        emptyMessage="No posts in this category yet. Be the first to share!"
        onItemClick={handleItemClick}
        itemsPerPage={10}
        hasMore={false}
        onLoadMore={handleLoadMore}
      />
    </div>
  );
}

/**
 * Example with API integration pattern
 */
export function FeedWithAPIExample() {
  const [activeTab, setActiveTab] = useState<FeedTabValue>('all');
  const [items, setItems] = useState<FeedItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [page, setPage] = useState(1);

  /**
   * Fetch feed items from API
   */
  const fetchFeedItems = async (pageNum: number, filter: FeedTabValue) => {
    try {
      const params = new URLSearchParams({
        page: pageNum.toString(),
        limit: '10',
        ...(filter !== 'all' && { type: filter }),
      });

      const response = await fetch(`/api/feed?${params}`);
      const data = await response.json();

      return {
        items: data.items,
        hasMore: data.hasMore,
      };
    } catch (error) {
      console.error('Failed to fetch feed items:', error);
      return { items: [], hasMore: false };
    }
  };

  /**
   * Load initial items
   */
  const loadInitialItems = async () => {
    setLoading(true);
    const { items: newItems, hasMore: more } = await fetchFeedItems(1, activeTab);
    setItems(newItems);
    setHasMore(more);
    setPage(1);
    setLoading(false);
  };

  /**
   * Load more items
   */
  const loadMoreItems = async () => {
    if (!hasMore || loadingMore) return;

    setLoadingMore(true);
    const nextPage = page + 1;
    const { items: newItems, hasMore: more } = await fetchFeedItems(nextPage, activeTab);
    setItems(prev => [...prev, ...newItems]);
    setHasMore(more);
    setPage(nextPage);
    setLoadingMore(false);
  };

  /**
   * Handle tab change
   */
  const handleTabChange = (tab: FeedTabValue) => {
    setActiveTab(tab);
    // Trigger reload with new filter
    loadInitialItems();
  };

  return (
    <div className="max-w-4xl mx-auto">
      <FeedTabs
        activeTab={activeTab}
        onTabChange={handleTabChange}
      />
      <ActivityFeed
        items={items}
        loading={loading}
        emptyMessage="No posts found. Start sharing with the community!"
        itemsPerPage={10}
        hasMore={hasMore}
        onLoadMore={loadMoreItems}
        loadingMore={loadingMore}
      />
    </div>
  );
}
