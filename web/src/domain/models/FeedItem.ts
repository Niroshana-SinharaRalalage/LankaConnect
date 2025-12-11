/**
 * Feed Item Domain Model
 *
 * Represents a unified feed item across different content types in the LankaConnect platform.
 * This is a core domain entity following DDD principles.
 */

/**
 * Discriminated union of feed types
 */
export type FeedType = 'event' | 'business' | 'forum' | 'culture';

/**
 * Value Object: Author information
 */
export interface FeedAuthor {
  readonly id?: string;
  readonly name: string;
  readonly avatar?: string;
  readonly initials: string;
}

/**
 * Value Object: Action button configuration
 */
export interface FeedAction {
  readonly type?: string;
  readonly icon: string;
  readonly label: string;
  readonly count?: number;
  readonly active?: boolean;
}

/**
 * Value Object: Event-specific metadata
 */
export interface EventMetadata {
  readonly type: 'event';
  readonly date: string;
  readonly time?: string;
  readonly venue?: string;
  readonly interestedCount: number;
  readonly commentCount: number;
}

/**
 * Value Object: Business listing metadata
 */
export interface BusinessMetadata {
  readonly type: 'business';
  readonly category: string;
  readonly rating: number;
  readonly reviewCount?: number;
  readonly priceRange?: string;
  readonly hours?: string;
  readonly likesCount: number;
  readonly commentsCount: number;
}

/**
 * Value Object: Forum post metadata
 */
export interface ForumMetadata {
  readonly type: 'forum';
  readonly forumName: string;
  readonly category?: string;
  readonly replies?: number;
  readonly views?: number;
  readonly lastActive?: Date;
  readonly repliesCount: number;
  readonly helpfulCount: number;
}

/**
 * Value Object: Cultural content metadata
 */
export interface CultureMetadata {
  readonly type: 'culture';
  readonly category: string;
  readonly images?: number;
  readonly videoLength?: string;
  readonly tags?: string[];
  readonly resourcesCount: number;
  readonly repliesCount: number;
}

/**
 * Type guard for EventMetadata
 */
export function isEventMetadata(metadata: FeedMetadata): metadata is EventMetadata {
  return metadata.type === 'event';
}

/**
 * Type guard for BusinessMetadata
 */
export function isBusinessMetadata(metadata: FeedMetadata): metadata is BusinessMetadata {
  return metadata.type === 'business';
}

/**
 * Type guard for ForumMetadata
 */
export function isForumMetadata(metadata: FeedMetadata): metadata is ForumMetadata {
  return metadata.type === 'forum';
}

/**
 * Type guard for CultureMetadata
 */
export function isCultureMetadata(metadata: FeedMetadata): metadata is CultureMetadata {
  return metadata.type === 'culture';
}

/**
 * Discriminated union of all metadata types
 */
export type FeedMetadata = EventMetadata | BusinessMetadata | ForumMetadata | CultureMetadata;

/**
 * Aggregate Root: Feed Item
 *
 * Represents a single item in the unified feed system.
 * This is an immutable entity with value objects.
 */
export interface FeedItem {
  readonly id: string;
  readonly type: FeedType;
  readonly author: FeedAuthor;
  readonly timestamp: Date;
  readonly location: string;
  readonly title: string;
  readonly description: string;
  readonly actions: readonly FeedAction[];
  readonly metadata: FeedMetadata;
}

/**
 * Factory function to create a FeedItem with validation
 */
export function createFeedItem(data: {
  id: string;
  type: FeedType;
  author: FeedAuthor;
  timestamp: Date;
  location: string;
  title: string;
  description: string;
  actions: FeedAction[];
  metadata: FeedMetadata;
}): FeedItem {
  // Validation
  if (!data.id.trim()) {
    throw new Error('Feed item ID cannot be empty');
  }
  if (!data.title.trim()) {
    throw new Error('Feed item title cannot be empty');
  }
  if (!data.author.name.trim()) {
    throw new Error('Feed item author name cannot be empty');
  }

  // Ensure metadata type matches feed type
  if (data.metadata.type !== data.type) {
    throw new Error(`Metadata type '${data.metadata.type}' does not match feed type '${data.type}'`);
  }

  return {
    id: data.id,
    type: data.type,
    author: { ...data.author },
    timestamp: new Date(data.timestamp),
    location: data.location,
    title: data.title,
    description: data.description,
    actions: [...data.actions],
    metadata: data.metadata,
  };
}

/**
 * Value Object: Feed Item Collection
 */
export interface FeedCollection {
  readonly items: readonly FeedItem[];
  readonly totalCount: number;
  readonly hasMore: boolean;
}
