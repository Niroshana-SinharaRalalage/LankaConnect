/**
 * Event Mapper Unit Tests
 *
 * Tests for EventDto to FeedItem conversion utility
 */

import { describe, it, expect } from 'vitest';
import {
  mapEventToFeedItem,
  mapEventListToFeedItems,
  getEventThumbnail,
  formatEventPrice,
  isEventFull,
  getAvailableSpots,
  formatEventDateRange,
} from '@/presentation/utils/eventMapper';
import {
  EventDto,
  EventStatus,
  EventCategory,
  Currency,
} from '@/infrastructure/api/types/events.types';
import { FeedItem } from '@/domain/models/FeedItem';

// ==================== Test Fixtures ====================

/**
 * Creates a mock EventDto for testing
 */
function createMockEvent(overrides: Partial<EventDto> = {}): EventDto {
  return {
    id: 'event-123',
    title: 'Sri Lankan New Year Festival',
    description: 'Join us for a traditional celebration of Sinhala and Tamil New Year',
    startDate: '2024-04-13T10:00:00Z',
    endDate: '2024-04-13T18:00:00Z',
    organizerId: 'org-456',
    capacity: 100,
    currentRegistrations: 45,
    status: EventStatus.Published,
    category: EventCategory.Cultural,
    createdAt: '2024-03-01T12:00:00Z',
    updatedAt: '2024-03-15T14:30:00Z',
    address: '123 Main Street',
    city: 'Colombo',
    state: 'Western Province',
    zipCode: '00100',
    country: 'Sri Lanka',
    latitude: 6.9271,
    longitude: 79.8612,
    ticketPriceAmount: 10.0,
    ticketPriceCurrency: Currency.USD,
    isFree: false,
    images: [
      {
        id: 'img-1',
        imageUrl: 'https://example.com/event.jpg',
        displayOrder: 1,
        uploadedAt: '2024-03-01T12:00:00Z',
      },
    ],
    videos: [],
    ...overrides,
  };
}

/**
 * Creates a free event mock
 */
function createFreeEvent(): EventDto {
  return createMockEvent({
    isFree: true,
    ticketPriceAmount: null,
    ticketPriceCurrency: null,
  });
}

/**
 * Creates an online event mock (no physical location)
 */
function createOnlineEvent(): EventDto {
  return createMockEvent({
    address: null,
    city: null,
    state: null,
    zipCode: null,
    country: null,
    latitude: null,
    longitude: null,
  });
}

/**
 * Creates an event without images
 */
function createEventWithoutImages(): EventDto {
  return createMockEvent({
    images: [],
  });
}

// ==================== Core Mapping Tests ====================

describe('eventMapper', () => {
  describe('mapEventToFeedItem', () => {
    it('should map EventDto to FeedItem with correct structure', () => {
      const event = createMockEvent();
      const feedItem = mapEventToFeedItem(event);

      expect(feedItem).toBeDefined();
      expect(feedItem.id).toBe(event.id);
      expect(feedItem.type).toBe('event');
      expect(feedItem.title).toBe(event.title);
      expect(feedItem.description).toBe(event.description);
    });

    it('should create EventMetadata with correct type', () => {
      const event = createMockEvent();
      const feedItem = mapEventToFeedItem(event);

      expect(feedItem.metadata.type).toBe('event');
      expect(feedItem.metadata).toHaveProperty('date');
      expect(feedItem.metadata).toHaveProperty('interestedCount');
      expect(feedItem.metadata).toHaveProperty('commentCount');
    });

    it('should map event dates to metadata', () => {
      const event = createMockEvent({
        startDate: '2024-04-13T10:00:00Z',
      });
      const feedItem = mapEventToFeedItem(event);

      expect(feedItem.metadata.type).toBe('event');
      if (feedItem.metadata.type === 'event') {
        expect(feedItem.metadata.date).toBe('2024-04-13T10:00:00Z');
        expect(feedItem.metadata.time).toBeDefined();
      }
    });

    it('should map current registrations to interestedCount', () => {
      const event = createMockEvent({
        currentRegistrations: 75,
      });
      const feedItem = mapEventToFeedItem(event);

      expect(feedItem.metadata.type).toBe('event');
      if (feedItem.metadata.type === 'event') {
        expect(feedItem.metadata.interestedCount).toBe(75);
      }
    });

    it('should create author from organizer ID', () => {
      const event = createMockEvent({
        organizerId: 'org-789',
      });
      const feedItem = mapEventToFeedItem(event);

      expect(feedItem.author).toBeDefined();
      expect(feedItem.author.id).toBe('org-789');
      expect(feedItem.author.name).toBeDefined();
      expect(feedItem.author.initials).toBeDefined();
    });

    it('should create timestamp from createdAt', () => {
      const event = createMockEvent({
        createdAt: '2024-03-01T12:00:00Z',
      });
      const feedItem = mapEventToFeedItem(event);

      expect(feedItem.timestamp).toBeInstanceOf(Date);
      expect(feedItem.timestamp.toISOString()).toBe('2024-03-01T12:00:00.000Z');
    });

    it('should create action buttons for events', () => {
      const event = createMockEvent();
      const feedItem = mapEventToFeedItem(event);

      expect(feedItem.actions).toHaveLength(3);
      expect(feedItem.actions[0]).toMatchObject({
        type: 'interested',
        icon: 'Heart',
        label: 'Interested',
      });
      expect(feedItem.actions[1]).toMatchObject({
        type: 'comment',
        icon: 'MessageCircle',
        label: 'Comment',
      });
      expect(feedItem.actions[2]).toMatchObject({
        type: 'share',
        icon: 'Share2',
        label: 'Share',
      });
    });
  });

  describe('mapEventListToFeedItems', () => {
    it('should map array of EventDto to FeedItem array', () => {
      const events = [createMockEvent(), createMockEvent({ id: 'event-456' })];
      const feedItems = mapEventListToFeedItems(events);

      expect(feedItems).toHaveLength(2);
      expect(feedItems[0].id).toBe('event-123');
      expect(feedItems[1].id).toBe('event-456');
    });

    it('should handle empty array', () => {
      const feedItems = mapEventListToFeedItems([]);
      expect(feedItems).toEqual([]);
    });
  });

  // ==================== Location Mapping Tests ====================

  describe('location mapping', () => {
    it('should format location from city and state', () => {
      const event = createMockEvent({
        city: 'Colombo',
        state: 'Western Province',
      });
      const feedItem = mapEventToFeedItem(event);

      expect(feedItem.location).toBe('Colombo, Western Province');
    });

    it('should handle city only', () => {
      const event = createMockEvent({
        city: 'Kandy',
        state: null,
      });
      const feedItem = mapEventToFeedItem(event);

      expect(feedItem.location).toBe('Kandy');
    });

    it('should handle state only', () => {
      const event = createMockEvent({
        city: null,
        state: 'Central Province',
      });
      const feedItem = mapEventToFeedItem(event);

      expect(feedItem.location).toBe('Central Province');
    });

    it('should use "Online" for events without location', () => {
      const event = createOnlineEvent();
      const feedItem = mapEventToFeedItem(event);

      expect(feedItem.location).toBe('Online');
    });

    it('should use country as fallback', () => {
      const event = createMockEvent({
        city: null,
        state: null,
        country: 'Sri Lanka',
      });
      const feedItem = mapEventToFeedItem(event);

      expect(feedItem.location).toBe('Sri Lanka');
    });
  });

  // ==================== Venue Mapping Tests ====================

  describe('venue mapping', () => {
    it('should include full address in venue', () => {
      const event = createMockEvent({
        address: '123 Main Street',
        city: 'Colombo',
        state: 'Western Province',
      });
      const feedItem = mapEventToFeedItem(event);

      expect(feedItem.metadata.type).toBe('event');
      if (feedItem.metadata.type === 'event') {
        expect(feedItem.metadata.venue).toBe(
          '123 Main Street, Colombo, Western Province'
        );
      }
    });

    it('should handle missing address', () => {
      const event = createMockEvent({
        address: null,
        city: 'Colombo',
      });
      const feedItem = mapEventToFeedItem(event);

      expect(feedItem.metadata.type).toBe('event');
      if (feedItem.metadata.type === 'event') {
        expect(feedItem.metadata.venue).toBe('Colombo, Western Province');
      }
    });

    it('should return undefined for online events', () => {
      const event = createOnlineEvent();
      const feedItem = mapEventToFeedItem(event);

      expect(feedItem.metadata.type).toBe('event');
      if (feedItem.metadata.type === 'event') {
        expect(feedItem.metadata.venue).toBeUndefined();
      }
    });
  });

  // ==================== Helper Function Tests ====================

  describe('getEventThumbnail', () => {
    it('should return first image URL when images exist', () => {
      const event = createMockEvent({
        images: [
          {
            id: 'img-1',
            imageUrl: 'https://example.com/image1.jpg',
            displayOrder: 2,
            uploadedAt: '2024-03-01T12:00:00Z',
          },
          {
            id: 'img-2',
            imageUrl: 'https://example.com/image2.jpg',
            displayOrder: 1,
            uploadedAt: '2024-03-01T12:00:00Z',
          },
        ],
      });

      const thumbnail = getEventThumbnail(event);
      expect(thumbnail).toBe('https://example.com/image2.jpg');
    });

    it('should return placeholder for events without images', () => {
      const event = createEventWithoutImages();
      const thumbnail = getEventThumbnail(event);

      expect(thumbnail).toBe('/images/placeholder-event.jpg');
    });
  });

  describe('formatEventPrice', () => {
    it('should return "Free" for free events', () => {
      const event = createFreeEvent();
      const price = formatEventPrice(event);

      expect(price).toBe('Free');
    });

    it('should format USD price correctly', () => {
      const event = createMockEvent({
        ticketPriceAmount: 25.0,
        ticketPriceCurrency: Currency.USD,
        isFree: false,
      });
      const price = formatEventPrice(event);

      expect(price).toMatch(/\$25/);
    });

    it('should format LKR price correctly', () => {
      const event = createMockEvent({
        ticketPriceAmount: 500,
        ticketPriceCurrency: Currency.LKR,
        isFree: false,
      });
      const price = formatEventPrice(event);

      expect(price).toContain('500');
    });

    it('should handle missing price as free', () => {
      const event = createMockEvent({
        ticketPriceAmount: null,
        isFree: true,
      });
      const price = formatEventPrice(event);

      expect(price).toBe('Free');
    });
  });

  describe('isEventFull', () => {
    it('should return true when event is at capacity', () => {
      const event = createMockEvent({
        capacity: 100,
        currentRegistrations: 100,
      });

      expect(isEventFull(event)).toBe(true);
    });

    it('should return false when event has available spots', () => {
      const event = createMockEvent({
        capacity: 100,
        currentRegistrations: 75,
      });

      expect(isEventFull(event)).toBe(false);
    });

    it('should return true when registrations exceed capacity', () => {
      const event = createMockEvent({
        capacity: 100,
        currentRegistrations: 105,
      });

      expect(isEventFull(event)).toBe(true);
    });
  });

  describe('getAvailableSpots', () => {
    it('should calculate available spots correctly', () => {
      const event = createMockEvent({
        capacity: 100,
        currentRegistrations: 75,
      });

      expect(getAvailableSpots(event)).toBe(25);
    });

    it('should return 0 when event is full', () => {
      const event = createMockEvent({
        capacity: 100,
        currentRegistrations: 100,
      });

      expect(getAvailableSpots(event)).toBe(0);
    });

    it('should return 0 when registrations exceed capacity', () => {
      const event = createMockEvent({
        capacity: 100,
        currentRegistrations: 105,
      });

      expect(getAvailableSpots(event)).toBe(0);
    });
  });

  describe('formatEventDateRange', () => {
    it('should format single-day event', () => {
      const event = createMockEvent({
        startDate: '2024-04-13T10:00:00Z',
        endDate: '2024-04-13T18:00:00Z',
      });

      const dateRange = formatEventDateRange(event);
      expect(dateRange).toContain('Apr');
      expect(dateRange).toContain('13');
      expect(dateRange).toContain('2024');
      expect(dateRange).not.toContain('-');
    });

    it('should format multi-day event with date range', () => {
      const event = createMockEvent({
        startDate: '2024-04-13T10:00:00Z',
        endDate: '2024-04-15T18:00:00Z',
      });

      const dateRange = formatEventDateRange(event);
      expect(dateRange).toContain('-');
      expect(dateRange).toContain('Apr 13');
      expect(dateRange).toContain('Apr 15');
    });

    it('should handle invalid dates gracefully', () => {
      const event = createMockEvent({
        startDate: 'invalid-date',
        endDate: 'invalid-date',
      });

      const dateRange = formatEventDateRange(event);
      expect(dateRange).toBe('Date TBD');
    });
  });
});
