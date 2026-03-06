/**
 * Newsletter Type Utilities
 *
 * Derives newsletter type from existing DTO fields.
 * Two independent dimensions:
 * - Main type (from isAnnouncementOnly): Newsletter or Notification
 * - Event linkage (from eventId): linked or standalone
 *
 * No backend changes needed - all data already exists in NewsletterDto.
 */

import type { NewsletterDto } from '@/infrastructure/api/types/newsletters.types';

/**
 * Main newsletter type derived from isAnnouncementOnly flag.
 * - Newsletter: Public, requires publish, visible on /newsletters page
 * - Notification: Private, auto-activates, targets email groups only
 */
export enum NewsletterMainType {
  Newsletter = 'Newsletter',
  Notification = 'Notification',
}

/**
 * Get the main type of a newsletter from its DTO.
 * Based solely on isAnnouncementOnly flag.
 */
export function getNewsletterMainType(newsletter: Pick<NewsletterDto, 'isAnnouncementOnly'>): NewsletterMainType {
  return newsletter.isAnnouncementOnly
    ? NewsletterMainType.Notification
    : NewsletterMainType.Newsletter;
}

/**
 * Check if a newsletter is linked to an event.
 * Independent of the main type — both newsletters and notifications can link to events.
 */
export function isEventLinked(newsletter: Pick<NewsletterDto, 'eventId'>): boolean {
  return !!newsletter.eventId;
}

/**
 * Get the badge color for a newsletter main type.
 * - Newsletter: Orange (#FF7900) — LankaConnect primary brand
 * - Notification: Purple (#8B5CF6) — distinct from status badges
 */
export function getMainTypeColor(type: NewsletterMainType): string {
  switch (type) {
    case NewsletterMainType.Newsletter:
      return '#FF7900';
    case NewsletterMainType.Notification:
      return '#8B5CF6';
  }
}
