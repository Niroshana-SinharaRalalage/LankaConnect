/**
 * Event Mapper Utility
 *
 * Converts EventDto from the backend API to FeedItem domain model
 * for display in the unified landing page feed.
 *
 * @module presentation/utils/eventMapper
 */

import { EventDto } from '@/infrastructure/api/types/events.types';
import {
  FeedItem,
  FeedAuthor,
  FeedAction,
  EventMetadata,
  createFeedItem,
} from '@/domain/models/FeedItem';

/**
 * Default placeholder image for events without images
 */
const DEFAULT_EVENT_IMAGE = '/images/placeholder-event.jpg';

/**
 * Maps EventDto to FeedItem for unified feed display
 *
 * @param event - EventDto from backend API
 * @returns FeedItem domain model
 *
 * @example
 * ```typescript
 * const eventDto = await eventsApi.getEvent(id);
 * const feedItem = mapEventToFeedItem(eventDto);
 * ```
 */
export function mapEventToFeedItem(event: EventDto): FeedItem {
  // Map author information
  const author: FeedAuthor = mapEventAuthor(event);

  // Map location from event address fields
  const location = formatEventLocation(event);

  // Map actions (Like, Comment, Share)
  const actions: FeedAction[] = createEventActions(event);

  // Map event-specific metadata
  const metadata: EventMetadata = {
    type: 'event',
    date: event.startDate,
    time: extractTimeFromDate(event.startDate),
    venue: formatVenue(event),
    interestedCount: event.currentRegistrations || 0,
    commentCount: 0, // TODO: Add when comments feature is implemented
  };

  // Create FeedItem using factory function with validation
  return createFeedItem({
    id: event.id,
    type: 'event',
    author,
    timestamp: new Date(event.createdAt),
    location,
    title: event.title,
    description: event.description,
    actions,
    metadata,
  });
}

/**
 * Maps a list of EventDto to FeedItem array
 *
 * @param events - Array of EventDto from backend
 * @returns Array of FeedItem domain models
 *
 * @example
 * ```typescript
 * const events = await eventsApi.getEvents();
 * const feedItems = mapEventListToFeedItems(events);
 * ```
 */
export function mapEventListToFeedItems(events: EventDto[]): FeedItem[] {
  return events.map(mapEventToFeedItem);
}

/**
 * Maps event organizer to FeedAuthor
 * Creates a placeholder author since EventDto doesn't include full organizer details
 *
 * @param event - EventDto
 * @returns FeedAuthor value object
 */
function mapEventAuthor(event: EventDto): FeedAuthor {
  // TODO: Fetch actual organizer details when user/organizer API is available
  // For now, create a basic author representation
  return {
    id: event.organizerId,
    name: 'Event Organizer', // TODO: Replace with actual organizer name
    avatar: undefined, // TODO: Add organizer avatar when available
    initials: 'EO', // TODO: Generate from actual organizer name
  };
}

/**
 * Formats event location from address components
 *
 * Priority:
 * 1. City + State (e.g., "Colombo, Western Province")
 * 2. City only
 * 3. State only
 * 4. "Online" if no physical location
 *
 * @param event - EventDto
 * @returns Formatted location string
 */
function formatEventLocation(event: EventDto): string {
  const { city, state, country } = event;

  // Build location string with available components
  const locationParts: string[] = [];

  if (city) {
    locationParts.push(city);
  }

  if (state) {
    locationParts.push(state);
  }

  // If we have location components, join them
  if (locationParts.length > 0) {
    return locationParts.join(', ');
  }

  // No location data - check if it's an online event or TBD
  if (country) {
    return country;
  }

  // Default to "Online" for events without location
  return 'Online';
}

/**
 * Formats venue information for event metadata
 * Includes full address if available
 *
 * @param event - EventDto
 * @returns Formatted venue string or undefined
 */
function formatVenue(event: EventDto): string | undefined {
  const { address, city, state } = event;

  if (!address && !city) {
    return undefined;
  }

  const venueParts: string[] = [];

  if (address) {
    venueParts.push(address);
  }

  if (city) {
    venueParts.push(city);
  }

  if (state) {
    venueParts.push(state);
  }

  return venueParts.length > 0 ? venueParts.join(', ') : undefined;
}

/**
 * Extracts time portion from ISO 8601 date-time string
 *
 * @param isoDateTime - ISO 8601 date-time string (e.g., "2024-03-15T18:30:00Z")
 * @returns Formatted time string (e.g., "6:30 PM") or undefined
 */
function extractTimeFromDate(isoDateTime: string): string | undefined {
  try {
    const date = new Date(isoDateTime);

    // Check if date is valid
    if (isNaN(date.getTime())) {
      return undefined;
    }

    // Format time in 12-hour format
    return date.toLocaleTimeString('en-US', {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  } catch {
    return undefined;
  }
}

/**
 * Creates action buttons for event feed item
 *
 * Actions:
 * - Like/Interested
 * - Comment
 * - Share
 *
 * @param event - EventDto
 * @returns Array of FeedAction value objects
 */
function createEventActions(event: EventDto): FeedAction[] {
  return [
    {
      type: 'interested',
      icon: 'Heart',
      label: 'Interested',
      count: event.currentRegistrations || 0,
      active: false, // TODO: Set based on user's RSVP status
    },
    {
      type: 'comment',
      icon: 'MessageCircle',
      label: 'Comment',
      count: 0, // TODO: Add when comments feature is implemented
      active: false,
    },
    {
      type: 'share',
      icon: 'Share2',
      label: 'Share',
      count: 0, // TODO: Add share count when feature is implemented
      active: false,
    },
  ];
}

/**
 * Gets event thumbnail image URL
 * Returns first image from event images array or default placeholder
 *
 * @param event - EventDto
 * @returns Image URL string
 */
export function getEventThumbnail(event: EventDto): string {
  if (event.images && event.images.length > 0) {
    // Get first image sorted by display order
    const sortedImages = [...event.images].sort(
      (a, b) => a.displayOrder - b.displayOrder
    );
    return sortedImages[0].imageUrl;
  }

  return DEFAULT_EVENT_IMAGE;
}

/**
 * Formats event price for display
 *
 * @param event - EventDto
 * @returns Formatted price string (e.g., "Free", "$10.00", "LKR 500")
 *
 * @example
 * ```typescript
 * formatEventPrice(freeEvent) // "Free"
 * formatEventPrice(paidEvent) // "$25.00"
 * ```
 */
export function formatEventPrice(event: EventDto): string {
  if (event.isFree || !event.ticketPriceAmount) {
    return 'Free';
  }

  // Map Currency enum to currency code
  const currencyMap: Record<number, string> = {
    1: 'USD',
    2: 'LKR',
    3: 'GBP',
    4: 'EUR',
    5: 'CAD',
    6: 'AUD',
  };

  const currency = typeof event.ticketPriceCurrency === 'number'
    ? (currencyMap[event.ticketPriceCurrency] || 'USD')
    : (event.ticketPriceCurrency || 'USD');
  const amount = event.ticketPriceAmount;

  // Format with currency symbol
  try {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
    }).format(amount);
  } catch {
    // Fallback if currency is not supported
    return `${currency} ${amount.toFixed(2)}`;
  }
}

/**
 * Checks if event registration is full
 *
 * @param event - EventDto
 * @returns True if event is at capacity
 */
export function isEventFull(event: EventDto): boolean {
  return event.currentRegistrations >= event.capacity;
}

/**
 * Gets available spots count for event
 *
 * @param event - EventDto
 * @returns Number of available spots
 */
export function getAvailableSpots(event: EventDto): number {
  const available = event.capacity - event.currentRegistrations;
  return Math.max(0, available);
}

/**
 * Formats event date range for display
 *
 * @param event - EventDto
 * @returns Formatted date range string
 *
 * @example
 * ```typescript
 * formatEventDateRange(event) // "Mar 15, 2024 - Mar 17, 2024"
 * formatEventDateRange(singleDay) // "Mar 15, 2024"
 * ```
 */
export function formatEventDateRange(event: EventDto): string {
  try {
    const startDate = new Date(event.startDate);
    const endDate = new Date(event.endDate);

    const dateOptions: Intl.DateTimeFormatOptions = {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    };

    const startFormatted = startDate.toLocaleDateString('en-US', dateOptions);

    // If same day event, return single date
    if (
      startDate.getFullYear() === endDate.getFullYear() &&
      startDate.getMonth() === endDate.getMonth() &&
      startDate.getDate() === endDate.getDate()
    ) {
      return startFormatted;
    }

    // Multi-day event
    const endFormatted = endDate.toLocaleDateString('en-US', dateOptions);
    return `${startFormatted} - ${endFormatted}`;
  } catch {
    return 'Date TBD';
  }
}
