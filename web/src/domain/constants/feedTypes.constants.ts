/**
 * Feed Types Constants
 *
 * Defines visual representations and metadata for different feed types.
 * These constants are used across the presentation layer for consistent styling.
 */

import { FeedType } from '../models/FeedItem';

/**
 * Feed type color mappings
 * Using Tailwind CSS color classes for consistency
 */
export const FEED_TYPE_COLORS: Record<FeedType, {
  bg: string;
  text: string;
  border: string;
  hover: string;
  badge: string;
}> = {
  event: {
    bg: 'bg-blue-50',
    text: 'text-blue-700',
    border: 'border-blue-200',
    hover: 'hover:bg-blue-100',
    badge: 'bg-blue-100 text-blue-800',
  },
  business: {
    bg: 'bg-green-50',
    text: 'text-green-700',
    border: 'border-green-200',
    hover: 'hover:bg-green-100',
    badge: 'bg-green-100 text-green-800',
  },
  forum: {
    bg: 'bg-purple-50',
    text: 'text-purple-700',
    border: 'border-purple-200',
    hover: 'hover:bg-purple-100',
    badge: 'bg-purple-100 text-purple-800',
  },
  culture: {
    bg: 'bg-orange-50',
    text: 'text-orange-700',
    border: 'border-orange-200',
    hover: 'hover:bg-orange-100',
    badge: 'bg-orange-100 text-orange-800',
  },
} as const;

/**
 * Feed type icon mappings
 * Using Heroicons v2 icon names
 */
export const FEED_TYPE_ICONS: Record<FeedType, string> = {
  event: 'CalendarDaysIcon',
  business: 'BuildingStorefrontIcon',
  forum: 'ChatBubbleLeftRightIcon',
  culture: 'GlobeAsiaAustraliaIcon',
} as const;

/**
 * Feed type display labels
 */
export const FEED_TYPE_LABELS: Record<FeedType, {
  singular: string;
  plural: string;
  description: string;
}> = {
  event: {
    singular: 'Event',
    plural: 'Events',
    description: 'Community events and gatherings',
  },
  business: {
    singular: 'Business',
    plural: 'Businesses',
    description: 'Local Sri Lankan businesses and services',
  },
  forum: {
    singular: 'Discussion',
    plural: 'Discussions',
    description: 'Community forums and conversations',
  },
  culture: {
    singular: 'Culture',
    plural: 'Cultural Content',
    description: 'Sri Lankan culture, language, and heritage',
  },
} as const;

/**
 * Feed type action icons
 */
export const FEED_ACTION_ICONS = {
  like: 'HeartIcon',
  comment: 'ChatBubbleLeftIcon',
  share: 'ShareIcon',
  interested: 'StarIcon',
  helpful: 'HandThumbUpIcon',
  reply: 'ArrowUturnLeftIcon',
  bookmark: 'BookmarkIcon',
  report: 'FlagIcon',
  edit: 'PencilIcon',
  delete: 'TrashIcon',
  view: 'EyeIcon',
  download: 'ArrowDownTrayIcon',
} as const;

/**
 * Feed type ordering for filters
 */
export const FEED_TYPE_ORDER: readonly FeedType[] = [
  'event',
  'business',
  'forum',
  'culture',
] as const;

/**
 * Default feed type (when no filter is applied)
 */
export const DEFAULT_FEED_TYPE: FeedType | null = null;

/**
 * Maximum number of items to load per page
 */
export const FEED_ITEMS_PER_PAGE = 20;

/**
 * Feed refresh interval in milliseconds (5 minutes)
 */
export const FEED_REFRESH_INTERVAL = 5 * 60 * 1000;

/**
 * Feed item timestamp formats
 */
export const TIMESTAMP_FORMATS = {
  recent: 'relative', // "2 hours ago"
  today: 'time', // "3:45 PM"
  thisWeek: 'dayTime', // "Monday 3:45 PM"
  older: 'date', // "Jan 15, 2025"
} as const;

/**
 * Helper function to get feed type color
 */
export function getFeedTypeColor(type: FeedType): typeof FEED_TYPE_COLORS[FeedType] {
  return FEED_TYPE_COLORS[type];
}

/**
 * Helper function to get feed type icon
 */
export function getFeedTypeIcon(type: FeedType): string {
  return FEED_TYPE_ICONS[type];
}

/**
 * Helper function to get feed type label
 */
export function getFeedTypeLabel(type: FeedType, plural = false): string {
  return plural
    ? FEED_TYPE_LABELS[type].plural
    : FEED_TYPE_LABELS[type].singular;
}

/**
 * Helper function to get feed type description
 */
export function getFeedTypeDescription(type: FeedType): string {
  return FEED_TYPE_LABELS[type].description;
}

/**
 * Helper function to validate feed type
 */
export function isValidFeedType(type: string): type is FeedType {
  return FEED_TYPE_ORDER.includes(type as FeedType);
}

/**
 * Feed filter options
 */
export interface FeedFilterOption {
  readonly value: FeedType | 'all';
  readonly label: string;
  readonly icon: string;
  readonly description: string;
}

/**
 * All feed filter options including "All"
 */
export const FEED_FILTER_OPTIONS: readonly FeedFilterOption[] = [
  {
    value: 'all',
    label: 'All Posts',
    icon: 'Squares2X2Icon',
    description: 'Show all content types',
  },
  ...FEED_TYPE_ORDER.map(type => ({
    value: type,
    label: FEED_TYPE_LABELS[type].plural,
    icon: FEED_TYPE_ICONS[type],
    description: FEED_TYPE_LABELS[type].description,
  })),
] as const;
