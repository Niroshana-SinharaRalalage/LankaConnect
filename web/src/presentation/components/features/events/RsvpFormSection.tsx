'use client';

import { Info } from 'lucide-react';
import {
  RegistrationMode,
  type EventDto,
  type AttendeeDto,
  type AnonymousRegistrationRequest,
  type RsvpRequest,
  type DonationConfigurationDto,
  type AddOnConfigurationDto,
  type CollectionConfigurationDto,
  type SponsorConfigurationDto,
  type GroupPricingTierDto,
  type TicketTierDto,
  type SeatingMode,
  type TicketingMode,
} from '@/infrastructure/api/types/events.types';
import { EventRegistrationForm } from './EventRegistrationForm';
import { HeadCountRsvpForm } from './HeadCountRsvpForm';

/**
 * Phase 7E.6: Dispatches the right RSVP UI based on `event.registrationMode`.
 *
 * - DetailedAttendees → existing `EventRegistrationForm` (Mode A — unchanged behaviour).
 * - HeadCountOnly / HeadCountByAge / HeadCountByGender / HeadCountByAgeAndGender →
 *   new `HeadCountRsvpForm` (Mode B — lead name + demographic spinners).
 * - NoRegistration → "No registration required" notice (Mode C — drop-in event).
 *
 * Defensive read of `event.registrationMode ?? DetailedAttendees` (architect §6) tolerates
 * stale React Query cached payloads from before the field shipped on the wire.
 *
 * The wrapper exists to keep the event detail page tidy — five existing
 * EventRegistrationForm renderings (different conditional paths: first-RSVP, after-cancel,
 * expired-session, etc.) can each be replaced by one `<RsvpFormSection event={event} ...>`
 * call without duplicating the dispatch logic.
 */
interface RsvpFormSectionProps {
  event: EventDto;
  spotsLeft: number;
  isProcessing: boolean;
  onSubmit: (data: RsvpRequest | AnonymousRegistrationRequest) => Promise<void>;
  error?: string | null;

  // Mode-A only props — passed through to EventRegistrationForm. Ignored by HeadCountRsvpForm
  // and the Mode-C notice (kept optional so callers can still pass them safely).
  ticketTiers?: TicketTierDto[];
  groupPricingTiers?: GroupPricingTierDto[];
  donationConfig?: DonationConfigurationDto | null;
  addOnConfig?: AddOnConfigurationDto | null;
  collectionConfig?: CollectionConfigurationDto | null;
  sponsorConfig?: SponsorConfigurationDto | null;
}

export function RsvpFormSection(props: RsvpFormSectionProps) {
  const mode = props.event.registrationMode ?? RegistrationMode.DetailedAttendees;

  // Phase 7E paid-B-mode gate (review iteration 1, 2026-04-28): if the server says the
  // configured mode is currently unimplemented, render a read-only "coming soon" panel
  // instead of a fillable form. Today this fires for paid + B-mode; lifts when 7E.3b ships.
  // Defaults to 'active' for legacy cached payloads from before this field shipped — those
  // events must keep working through the normal mode-dispatch path below.
  const modeStatus = props.event.registrationModeStatus ?? 'active';
  if (modeStatus === 'deferred') {
    return (
      <div className="rounded-lg border border-amber-200 bg-amber-50 p-4">
        <div className="flex items-start gap-2">
          <Info className="h-5 w-5 text-amber-600 mt-0.5 flex-shrink-0" />
          <div>
            <p className="text-sm font-medium text-amber-900">Registration coming soon</p>
            <p className="text-sm text-amber-800 mt-1">
              The organiser configured this event for head-count registration, but that flow
              is still being built (Phase 7E.3b). Please contact the organiser directly to
              register — see the Event Organiser Contacts section below.
            </p>
          </div>
        </div>
      </div>
    );
  }

  // Mode C: no registration form. Standalone donate / sponsor / add-on / collection actions
  // remain accessible elsewhere on the event detail page (per architect §5).
  if (mode === RegistrationMode.NoRegistration) {
    return (
      <div className="rounded-lg border border-blue-200 bg-blue-50 p-4">
        <div className="flex items-start gap-2">
          <Info className="h-5 w-5 text-blue-600 mt-0.5 flex-shrink-0" />
          <div>
            <p className="text-sm font-medium text-blue-900">No registration required</p>
            <p className="text-sm text-blue-800 mt-1">
              This is a drop-in event — just show up. Donations, sponsorships, and other
              contributions are still welcome via the actions on this page.
            </p>
          </div>
        </div>
      </div>
    );
  }

  // Mode B (B1-B4): head-count form.
  if (
    mode === RegistrationMode.HeadCountOnly ||
    mode === RegistrationMode.HeadCountByAge ||
    mode === RegistrationMode.HeadCountByGender ||
    mode === RegistrationMode.HeadCountByAgeAndGender
  ) {
    return (
      <HeadCountRsvpForm
        eventId={props.event.id}
        registrationMode={mode}
        isFree={props.event.isFree}
        maxAttendeesPerRegistration={props.event.maxAttendeesPerRegistration}
        spotsLeft={props.spotsLeft}
        isProcessing={props.isProcessing}
        onSubmit={props.onSubmit}
        error={props.error}
      />
    );
  }

  // Mode A: detailed attendees — existing form, untouched behaviour.
  return (
    <EventRegistrationForm
      eventId={props.event.id}
      spotsLeft={props.spotsLeft}
      isFree={props.event.isFree}
      ticketPrice={props.event.ticketPriceAmount ?? undefined}
      hasDualPricing={props.event.hasDualPricing}
      adultPrice={props.event.adultPriceAmount ?? undefined}
      childPrice={props.event.childPriceAmount ?? undefined}
      childAgeLimit={props.event.childAgeLimit ?? undefined}
      hasGroupPricing={props.event.hasGroupPricing}
      groupPricingTiers={props.groupPricingTiers ?? props.event.groupPricingTiers}
      seatingMode={props.event.seatingMode}
      ticketingMode={props.event.ticketingMode}
      ticketTiers={props.ticketTiers ?? props.event.ticketTiers}
      maxAttendeesPerRegistration={props.event.maxAttendeesPerRegistration}
      donationConfig={props.donationConfig ?? props.event.donationConfig}
      addOnConfig={props.addOnConfig ?? props.event.addOnConfig}
      collectionConfig={props.collectionConfig ?? props.event.collectionConfig}
      sponsorConfig={props.sponsorConfig ?? props.event.sponsorConfig}
      isProcessing={props.isProcessing}
      onSubmit={props.onSubmit}
      error={props.error}
    />
  );
}
