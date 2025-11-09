/**
 * Event Mapper Utility
 *
 * Maps EventDto from API to FeedItem domain model
 * Handles transformation between infrastructure layer DTOs and domain models
 */

import type { EventDto } from '@/infrastructure/api/types/events.types';
import type { FeedItem, EventMetadata } from '@/domain/models/FeedItem';
import { createFeedItem } from '@/domain/models/FeedItem';

/**
 * Map a single EventDto to FeedItem
 *
 * @param event - EventDto from API
 * @returns FeedItem for display in feed
 */
export function mapEventToFeedItem(event: EventDto): FeedItem {
  // Format date and time
  const startDate = new Date(event.startDate);
  const endDate = new Date(event.endDate);

  const dateOptions: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  };
  const timeOptions: Intl.DateTimeFormatOptions = {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  };

  const formattedDate = startDate.toLocaleDateString('en-US', dateOptions);
  const startTime = startDate.toLocaleTimeString('en-US', timeOptions);
  const endTime = endDate.toLocaleTimeString('en-US', timeOptions);
  const timeRange = `${startTime} - ${endTime}`;

  // Build location string from address components
  let location = 'Online Event';
  if (event.city && event.state) {
    location = `${event.city}, ${event.state}`;
  } else if (event.city) {
    location = event.city;
  } else if (event.state) {
    location = event.state;
  }

  // Build venue string
  let venue = 'Location TBA';
  if (event.address) {
    venue = event.address;
    if (event.city) {
      venue += `, ${event.city}`;
    }
  }

  // Extract organizer initials (simplified - you may want to fetch organizer details)
  const organizerInitials = 'EO'; // Event Organizer placeholder

  // Calculate engagement metrics
  // For now using currentRegistrations as proxy for interested count
  const interestedCount = event.currentRegistrations;
  const commentCount = 0; // TODO: Add comments when that feature is implemented

  // Create EventMetadata
  const metadata: EventMetadata = {
    type: 'event',
    date: formattedDate,
    time: timeRange,
    venue,
    interestedCount,
    commentCount,
  };

  // Create FeedItem using factory function
  return createFeedItem({
    id: event.id,
    type: 'event',
    author: {
      id: event.organizerId,
      name: 'Event Organizer', // TODO: Fetch organizer name from users API
      initials: organizerInitials,
    },
    timestamp: new Date(event.createdAt),
    location,
    title: event.title,
    description: event.description,
    actions: [
      {
        type: 'interested',
        icon: '👍',
        label: 'Interested',
        count: interestedCount,
        active: false // TODO: Check if current user has RSVP'd
      },
      {
        type: 'comment',
        icon: '💬',
        label: 'Comment',
        count: commentCount
      },
      {
        type: 'share',
        icon: '📤',
        label: 'Share',
        count: 0
      },
    ],
    metadata,
  });
}

/**
 * Map array of EventDto to array of FeedItem
 *
 * @param events - Array of EventDto from API
 * @returns Array of FeedItem for display in feed
 */
export function mapEventListToFeedItems(events: EventDto[]): FeedItem[] {
  return events.map(mapEventToFeedItem);
}

/**
 * Filter events by metro area (city/state matching)
 *
 * @param events - Array of EventDto
 * @param city - City name to filter by
 * @param state - State abbreviation to filter by
 * @returns Filtered array of EventDto
 */
export function filterEventsByLocation(
  events: EventDto[],
  city?: string,
  state?: string
): EventDto[] {
  if (!city && !state) {
    return events;
  }

  return events.filter((event) => {
    // If state-level filter, match any event in that state
    if (state && !city) {
      return event.state === state;
    }

    // If city-level filter, match exact city
    if (city) {
      return event.city === city;
    }

    return true;
  });
}

/**
 * Filter events by category
 *
 * @param events - Array of EventDto
 * @param category - Event category to filter by
 * @returns Filtered array of EventDto
 */
export function filterEventsByCategory(
  events: EventDto[],
  category: string
): EventDto[] {
  return events.filter((event) => event.category === category);
}

/**
 * Sort events by start date (ascending - soonest first)
 *
 * @param events - Array of EventDto
 * @returns Sorted array of EventDto
 */
export function sortEventsByDate(events: EventDto[]): EventDto[] {
  return [...events].sort((a, b) => {
    const dateA = new Date(a.startDate).getTime();
    const dateB = new Date(b.startDate).getTime();
    return dateA - dateB;
  });
}

/**
 * Get only upcoming events (start date in the future)
 *
 * @param events - Array of EventDto
 * @returns Array of upcoming EventDto
 */
export function getUpcomingEvents(events: EventDto[]): EventDto[] {
  const now = Date.now();
  return events.filter((event) => {
    const startDate = new Date(event.startDate).getTime();
    return startDate > now;
  });
}
