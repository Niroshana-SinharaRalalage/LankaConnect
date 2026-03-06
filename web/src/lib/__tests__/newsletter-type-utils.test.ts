/**
 * Unit Tests for Newsletter Type Utilities
 *
 * Tests for getNewsletterMainType(), isEventLinked(), and getMainTypeColor()
 * Following TDD Red-Green-Refactor methodology
 *
 * Two independent dimensions:
 * - Main type (from isAnnouncementOnly): Newsletter or Notification
 * - Event linkage (from eventId): linked or standalone
 */

import { getNewsletterMainType, isEventLinked, getMainTypeColor, NewsletterMainType } from '../newsletter-type-utils';
import type { NewsletterDto } from '@/infrastructure/api/types/newsletters.types';

// Helper to create a minimal NewsletterDto for testing
function createMockDto(overrides: Partial<NewsletterDto> = {}): NewsletterDto {
  return {
    id: 'test-id',
    title: 'Test Newsletter',
    description: 'Test description',
    createdByUserId: 'user-1',
    createdByUserName: 'Test User',
    eventId: null,
    eventTitle: null,
    status: 'Draft' as any,
    publishedAt: null,
    sentAt: null,
    expiresAt: null,
    includeNewsletterSubscribers: true,
    targetAllLocations: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
    emailGroupIds: [],
    emailGroups: [],
    metroAreaIds: [],
    metroAreas: [],
    isAnnouncementOnly: false,
    ...overrides,
  };
}

describe('Newsletter Type Utils', () => {
  describe('getNewsletterMainType', () => {
    it('should return Newsletter when isAnnouncementOnly is false', () => {
      const dto = createMockDto({ isAnnouncementOnly: false });
      expect(getNewsletterMainType(dto)).toBe(NewsletterMainType.Newsletter);
    });

    it('should return Notification when isAnnouncementOnly is true', () => {
      const dto = createMockDto({ isAnnouncementOnly: true });
      expect(getNewsletterMainType(dto)).toBe(NewsletterMainType.Notification);
    });

    it('should return Newsletter when isAnnouncementOnly is false with eventId set', () => {
      const dto = createMockDto({ isAnnouncementOnly: false, eventId: 'event-123' });
      expect(getNewsletterMainType(dto)).toBe(NewsletterMainType.Newsletter);
    });

    it('should return Notification when isAnnouncementOnly is true with eventId set', () => {
      const dto = createMockDto({ isAnnouncementOnly: true, eventId: 'event-123' });
      expect(getNewsletterMainType(dto)).toBe(NewsletterMainType.Notification);
    });
  });

  describe('isEventLinked', () => {
    it('should return false when eventId is null', () => {
      const dto = createMockDto({ eventId: null });
      expect(isEventLinked(dto)).toBe(false);
    });

    it('should return false when eventId is undefined', () => {
      const dto = createMockDto({ eventId: undefined as any });
      expect(isEventLinked(dto)).toBe(false);
    });

    it('should return false when eventId is empty string', () => {
      const dto = createMockDto({ eventId: '' as any });
      expect(isEventLinked(dto)).toBe(false);
    });

    it('should return true when eventId is a valid string', () => {
      const dto = createMockDto({ eventId: 'event-123' });
      expect(isEventLinked(dto)).toBe(true);
    });

    it('should return true for notification with eventId (event-linked notification)', () => {
      const dto = createMockDto({ isAnnouncementOnly: true, eventId: 'event-456' });
      expect(isEventLinked(dto)).toBe(true);
    });

    it('should return true for newsletter with eventId (event-linked newsletter)', () => {
      const dto = createMockDto({ isAnnouncementOnly: false, eventId: 'event-789' });
      expect(isEventLinked(dto)).toBe(true);
    });
  });

  describe('getMainTypeColor', () => {
    it('should return orange for Newsletter type', () => {
      const color = getMainTypeColor(NewsletterMainType.Newsletter);
      expect(color).toBe('#FF7900');
    });

    it('should return purple for Notification type', () => {
      const color = getMainTypeColor(NewsletterMainType.Notification);
      expect(color).toBe('#8B5CF6');
    });
  });
});
