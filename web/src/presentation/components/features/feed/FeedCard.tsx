'use client';

import React from 'react';
import { FeedItem, isEventMetadata, isBusinessMetadata, isForumMetadata, isCultureMetadata } from '@/domain/models/FeedItem';
import { FEED_TYPE_COLORS } from '@/domain/constants/feedTypes.constants';
import { Card } from '@/presentation/components/ui/Card';
import {
  Calendar,
  MapPin,
  Heart,
  MessageCircle,
  Star,
  ThumbsUp,
  Share2,
  BookOpen,
  Building2
} from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';

/**
 * Props for FeedCard component
 */
export interface FeedCardProps {
  /** Feed item data from domain model */
  item: FeedItem;
  /** Optional click handler for the entire card */
  onClick?: (item: FeedItem) => void;
  /** Optional className for custom styling */
  className?: string;
}

/**
 * FeedCard Component
 *
 * Displays individual feed items with type-specific styling and actions.
 * Follows the domain model structure and uses constants for consistent theming.
 *
 * @example
 * ```tsx
 * <FeedCard
 *   item={feedItem}
 *   onClick={(item) => router.push(`/feed/${item.id}`)}
 * />
 * ```
 */
export function FeedCard({ item, onClick, className = '' }: FeedCardProps) {
  const colors = FEED_TYPE_COLORS[item.type];

  /**
   * Format timestamp to relative time (e.g., "2 hours ago")
   */
  const formatTimestamp = (date: Date): string => {
    return formatDistanceToNow(date, { addSuffix: true });
  };

  /**
   * Render type-specific badge
   */
  const renderTypeBadge = () => {
    const labels = {
      event: 'Event',
      business: 'Business',
      forum: 'Discussion',
      culture: 'Culture'
    };

    return (
      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${colors.badge}`}>
        {labels[item.type]}
      </span>
    );
  };

  /**
   * Render metadata-specific info
   */
  const renderMetadata = () => {
    if (isEventMetadata(item.metadata)) {
      return (
        <div className="flex items-center gap-1 text-sm text-gray-600">
          <Calendar className="w-4 h-4" />
          <span>{item.metadata.date}</span>
        </div>
      );
    }

    if (isBusinessMetadata(item.metadata)) {
      return (
        <div className="flex items-center gap-2 text-sm text-gray-600">
          <Building2 className="w-4 h-4" />
          <span>{item.metadata.category}</span>
          <span className="text-[#FF7900] font-medium">★ {item.metadata.rating.toFixed(1)}</span>
        </div>
      );
    }

    if (isForumMetadata(item.metadata)) {
      return (
        <div className="flex items-center gap-1 text-sm text-gray-600">
          <MessageCircle className="w-4 h-4" />
          <span>{item.metadata.forumName}</span>
        </div>
      );
    }

    if (isCultureMetadata(item.metadata)) {
      return (
        <div className="flex items-center gap-1 text-sm text-gray-600">
          <BookOpen className="w-4 h-4" />
          <span>{item.metadata.category}</span>
        </div>
      );
    }

    return null;
  };

  /**
   * Render action buttons based on feed type
   */
  const renderActions = () => {
    // Map action icons
    const iconMap: Record<string, React.ReactElement> = {
      '👍': <Star className="w-4 h-4" />,
      '💬': <MessageCircle className="w-4 h-4" />,
      '❤️': <Heart className="w-4 h-4" />,
      '👏': <ThumbsUp className="w-4 h-4" />,
      '📚': <BookOpen className="w-4 h-4" />
    };

    return (
      <div className="flex items-center gap-4 pt-3 border-t border-gray-100">
        {item.actions.map((action, index) => {
          const icon = iconMap[action.icon] || <Star className="w-4 h-4" />;

          return (
            <button
              key={index}
              className="flex items-center gap-1.5 text-gray-600 hover:text-[#FF7900] transition-colors group"
              onClick={(e) => {
                e.stopPropagation();
                // Handle action click
              }}
            >
              <span className="group-hover:scale-110 transition-transform">
                {icon}
              </span>
              <span className="text-sm font-medium">{action.label}</span>
              {action.count !== undefined && action.count > 0 && (
                <span className="text-sm text-gray-500">({action.count})</span>
              )}
            </button>
          );
        })}
        <button
          className="ml-auto flex items-center gap-1.5 text-gray-600 hover:text-[#FF7900] transition-colors"
          onClick={(e) => {
            e.stopPropagation();
            // Handle share
          }}
        >
          <Share2 className="w-4 h-4" />
        </button>
      </div>
    );
  };

  return (
    <Card
      className={`hover:bg-[#fff9f5] transition-all duration-200 cursor-pointer border-l-4 ${colors.border} ${className}`}
      onClick={() => onClick?.(item)}
    >
      <div className="p-6">
        {/* Header with author info */}
        <div className="flex items-start gap-3 mb-4">
          {/* Avatar with gradient */}
          <div
            className="w-10 h-10 rounded-full flex items-center justify-center text-white font-semibold flex-shrink-0"
            style={{ background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)' }}
          >
            {item.author.initials}
          </div>

          {/* Author details */}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <h3 className="font-semibold text-gray-900">{item.author.name}</h3>
              {renderTypeBadge()}
            </div>
            <div className="flex items-center gap-3 mt-1 text-sm text-gray-500">
              <span>{formatTimestamp(item.timestamp)}</span>
              {item.location && (
                <>
                  <span>•</span>
                  <div className="flex items-center gap-1">
                    <MapPin className="w-3.5 h-3.5" />
                    <span>{item.location}</span>
                  </div>
                </>
              )}
            </div>
          </div>
        </div>

        {/* Metadata info */}
        <div className="mb-3">
          {renderMetadata()}
        </div>

        {/* Content */}
        <div className="mb-4">
          <h2 className="text-lg font-semibold text-gray-900 mb-2">
            {item.title}
          </h2>
          <p className="text-gray-700 text-sm leading-relaxed line-clamp-2">
            {item.description}
          </p>
        </div>

        {/* Actions */}
        {renderActions()}
      </div>
    </Card>
  );
}
