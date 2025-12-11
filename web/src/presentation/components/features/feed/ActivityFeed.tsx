'use client';

import { FeedItem } from '@/domain/models/FeedItem';
import { FeedCard } from './FeedCard';
import { Button } from '@/presentation/components/ui/Button';
import { Loader2, Inbox } from 'lucide-react';
import { useState } from 'react';

/**
 * Props for ActivityFeed component
 */
export interface ActivityFeedProps {
  /** Array of feed items to display */
  items: FeedItem[];
  /** Loading state */
  loading?: boolean;
  /** Empty state message */
  emptyMessage?: string;
  /** Optional click handler for feed items */
  onItemClick?: (item: FeedItem) => void;
  /** Items per page for pagination */
  itemsPerPage?: number;
  /** Whether to show load more button */
  hasMore?: boolean;
  /** Callback when load more is clicked */
  onLoadMore?: () => void;
  /** Loading state for load more */
  loadingMore?: boolean;
  /** Optional className for custom styling */
  className?: string;
  /** Enable grid view (2 columns on large screens) */
  gridView?: boolean;
}

/**
 * Skeleton loader for feed card
 */
function FeedCardSkeleton() {
  return (
    <div className="border rounded-lg p-6 bg-white animate-pulse">
      <div className="flex items-start gap-3 mb-4">
        <div className="w-10 h-10 rounded-full bg-gray-200"></div>
        <div className="flex-1">
          <div className="h-4 bg-gray-200 rounded w-32 mb-2"></div>
          <div className="h-3 bg-gray-200 rounded w-48"></div>
        </div>
      </div>
      <div className="mb-3">
        <div className="h-3 bg-gray-200 rounded w-24"></div>
      </div>
      <div className="mb-4">
        <div className="h-5 bg-gray-200 rounded w-3/4 mb-2"></div>
        <div className="h-4 bg-gray-200 rounded w-full mb-1"></div>
        <div className="h-4 bg-gray-200 rounded w-5/6"></div>
      </div>
      <div className="flex gap-4 pt-3 border-t border-gray-100">
        <div className="h-8 bg-gray-200 rounded w-20"></div>
        <div className="h-8 bg-gray-200 rounded w-20"></div>
        <div className="h-8 bg-gray-200 rounded w-20"></div>
      </div>
    </div>
  );
}

/**
 * Empty state component
 */
function EmptyState({ message }: { message: string }) {
  return (
    <div className="flex flex-col items-center justify-center py-16 px-4">
      <div
        className="w-16 h-16 rounded-full flex items-center justify-center mb-4"
        style={{ background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)' }}
      >
        <Inbox className="w-8 h-8 text-white" />
      </div>
      <h3 className="text-lg font-semibold text-gray-900 mb-2">
        No posts yet
      </h3>
      <p className="text-gray-600 text-center max-w-md mb-6">
        {message}
      </p>
      <Button
        variant="default"
        style={{
          background: '#FF7900',
          color: 'white'
        }}
      >
        Create Your First Post
      </Button>
    </div>
  );
}

/**
 * ActivityFeed Component
 *
 * Container component for displaying a list of feed items with pagination,
 * loading states, and empty states.
 *
 * @example
 * ```tsx
 * <ActivityFeed
 *   items={feedItems}
 *   loading={isLoading}
 *   emptyMessage="Start by creating your first post or event!"
 *   onItemClick={(item) => router.push(`/feed/${item.id}`)}
 *   hasMore={hasMoreItems}
 *   onLoadMore={loadMoreItems}
 * />
 * ```
 */
export function ActivityFeed({
  items,
  loading = false,
  emptyMessage = 'Be the first to share something with the community!',
  onItemClick,
  itemsPerPage = 10,
  hasMore = false,
  onLoadMore,
  loadingMore = false,
  className = '',
  gridView = false
}: ActivityFeedProps) {
  const [visibleCount, setVisibleCount] = useState(itemsPerPage);

  /**
   * Handle load more action
   */
  const handleLoadMore = () => {
    if (onLoadMore) {
      onLoadMore();
    } else {
      // Local pagination
      setVisibleCount(prev => prev + itemsPerPage);
    }
  };

  /**
   * Render loading skeletons
   */
  if (loading) {
    return (
      <div className={gridView ? `grid grid-cols-1 lg:grid-cols-2 gap-4 p-4 ${className}` : `space-y-4 ${className}`}>
        {Array.from({ length: gridView ? 4 : 3 }).map((_, index) => (
          <FeedCardSkeleton key={index} />
        ))}
      </div>
    );
  }

  /**
   * Render empty state
   */
  if (!loading && items.length === 0) {
    return (
      <div className={className}>
        <EmptyState message={emptyMessage} />
      </div>
    );
  }

  /**
   * Determine items to display
   */
  const displayItems = onLoadMore ? items : items.slice(0, visibleCount);
  const showLoadMore = onLoadMore ? hasMore : visibleCount < items.length;

  return (
    <div className={className}>
      {/* Feed items grid */}
      <div className={gridView ? 'grid grid-cols-1 lg:grid-cols-2 gap-4 p-4' : 'space-y-4'}>
        {displayItems.map((item) => (
          <FeedCard
            key={item.id}
            item={item}
            onClick={onItemClick}
          />
        ))}
      </div>

      {/* Load more button */}
      {showLoadMore && (
        <div className="flex justify-center mt-8 pb-4">
          <Button
            onClick={handleLoadMore}
            disabled={loadingMore}
            variant="outline"
            className="min-w-[200px]"
            style={{
              borderColor: '#FF7900',
              color: '#FF7900'
            }}
          >
            {loadingMore ? (
              <>
                <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                Loading...
              </>
            ) : (
              'Load More Posts'
            )}
          </Button>
        </div>
      )}

      {/* Loading more indicator */}
      {loadingMore && (
        <div className={gridView ? 'grid grid-cols-1 lg:grid-cols-2 gap-4 p-4' : 'space-y-4 mt-4'}>
          {Array.from({ length: 2 }).map((_, index) => (
            <FeedCardSkeleton key={`loading-${index}`} />
          ))}
        </div>
      )}
    </div>
  );
}
