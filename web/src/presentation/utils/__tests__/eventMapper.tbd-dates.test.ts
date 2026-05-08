import { describe, it, expect } from 'vitest';
import { mapEventToFeedItem, formatEventDateRange } from '@/presentation/utils/eventMapper';
import type { EventDto } from '@/infrastructure/api/types/events.types';
import {
  EventCategory,
  EventStatus,
  RegistrationMode,
  TicketingMode,
  SeatingMode,
  EventPaymentMode,
} from '@/infrastructure/api/types/events.types';

/**
 * Phase 8YA.3 — TBD-dates rendering in eventMapper utilities.
 *
 * The mapper sits between the API response and every display surface (feed cards,
 * detail-page metadata, dashboard listings). Pinning the "Date TBD" / "Time TBD"
 * fallbacks here means future formatter changes can't accidentally leak the
 * backend's null dates into the UI as "Invalid Date" / "1970".
 */

const baseEvent: EventDto = {
  id: 'evt-1',
  title: 'TBD community gathering',
  description: 'Date and venue to be decided',
  startDate: null,
  endDate: null,
  organizerId: 'org-1',
  capacity: 50,
  currentRegistrations: 0,
  maxAttendeesPerRegistration: 10,
  registrationMode: RegistrationMode.DetailedAttendees,
  registrationModeStatus: 'active',
  status: EventStatus.Planning,
  category: EventCategory.Community,
  createdAt: new Date().toISOString(),
  updatedAt: null,
  isFree: true,
  paymentMode: EventPaymentMode.Free,
  ticketingMode: TicketingMode.Single,
  seatingMode: SeatingMode.GeneralAdmission,
  // Optional fields the mapper does not require for the date paths.
  locationAddress: null,
  locationCity: null,
  locationState: null,
  locationZipCode: null,
  locationCountry: null,
  locationLatitude: null,
  locationLongitude: null,
  locationName: null,
  hasSecondaryLocation: false,
  timeZoneId: 'America/New_York',
  ticketPriceAmount: null,
  ticketPriceCurrency: null,
  hasDualPricing: false,
  hasGroupPricing: false,
  adultPriceAmount: null,
  adultPriceCurrency: null,
  childPriceAmount: null,
  childPriceCurrency: null,
  childAgeLimit: null,
  groupPricingTiers: null,
  emailGroupIds: null,
  publishOrganizerContact: false,
  organizerContacts: null,
  hasOrganizerContact: false,
} as unknown as EventDto;

describe('eventMapper — TBD dates (Phase 8YA.3)', () => {
  describe('formatEventDateRange', () => {
    it('returns "Date TBD" when both startDate and endDate are null', () => {
      expect(formatEventDateRange(baseEvent)).toBe('Date TBD');
    });

    it('returns "Date TBD" when startDate is null even with endDate set', () => {
      expect(formatEventDateRange({ ...baseEvent, endDate: new Date().toISOString() })).toBe('Date TBD');
    });

    it('returns a real range when both dates are set', () => {
      const start = new Date('2030-06-01T18:00:00Z').toISOString();
      const end = new Date('2030-06-01T21:00:00Z').toISOString();
      const result = formatEventDateRange({ ...baseEvent, startDate: start, endDate: end });

      // Don't assert exact format (timezone-dependent), only that we got something
      // other than the TBD fallback.
      expect(result).not.toBe('Date TBD');
      expect(result.length).toBeGreaterThan(0);
    });
  });

  describe('mapEventToFeedItem', () => {
    it('surfaces "Date TBD" / "Time TBD" in metadata when dates are null', () => {
      const feedItem = mapEventToFeedItem(baseEvent);

      expect(feedItem.metadata).toMatchObject({
        type: 'event',
        date: 'Date TBD',
        time: 'Time TBD',
      });
    });

    it('passes through real dates when they are set', () => {
      const start = new Date('2030-06-01T18:00:00Z').toISOString();
      const feedItem = mapEventToFeedItem({ ...baseEvent, startDate: start, endDate: start });

      expect(feedItem.metadata).toMatchObject({ type: 'event', date: start });
      // Time is timezone-dependent; assert it is non-empty and distinct from the TBD placeholder.
      const eventMetadata = feedItem.metadata as { time: string };
      expect(eventMetadata.time).not.toBe('Time TBD');
      expect(eventMetadata.time.length).toBeGreaterThan(0);
    });
  });
});
