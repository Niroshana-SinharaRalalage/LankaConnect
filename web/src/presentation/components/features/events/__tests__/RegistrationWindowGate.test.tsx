import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { RegistrationWindowGate } from '../RegistrationWindowGate';
import type { EventDto } from '@/infrastructure/api/types/events.types';
import { EventStatus, EventCategory } from '@/infrastructure/api/types/events.types';

/**
 * Phase 6A.153 — RegistrationWindowGate UX contract.
 *
 * The gate sits between the parent cascade (`/events/[id]/page.tsx`) and the
 * existing `<RsvpFormSection>` form. It interprets `registrationAvailability`
 * and either:
 *   1. Renders the existing form unchanged (open / closed-event-started /
 *      undefined for stale cache — fail-safe default).
 *   2. Replaces the form with a Register-button + click-through "opens soon"
 *      modal (not-yet-open).
 *   3. Replaces the form with an inline "Registration is closed" microcopy
 *      panel (closed-by-organizer).
 *
 * These tests pin the wire contract so a future refactor can't silently let
 * registrations through during a closed window.
 */

function makeEvent(overrides: Partial<EventDto> = {}): EventDto {
  return {
    id: `event-${Math.random().toString(36).slice(2, 8)}`,
    title: 'Phase 6A.153 gate test',
    description: 'gate component fixture',
    startDate: '2026-12-01T18:00:00Z',
    endDate: '2026-12-01T21:00:00Z',
    organizerId: 'organizer-id',
    capacity: 100,
    currentRegistrations: 0,
    status: EventStatus.Published,
    category: EventCategory.Community,
    createdAt: '2026-05-25T00:00:00Z',
    isFree: true,
    ...overrides,
  } as EventDto;
}

const FormStub = () => <div data-testid="rsvp-form-stub">RSVP form</div>;

describe('RegistrationWindowGate — Phase 6A.153', () => {
  describe('open state', () => {
    it('renders the child form unchanged when availability is "open"', () => {
      const event = makeEvent({ registrationAvailability: 'open' });

      render(
        <RegistrationWindowGate event={event}>
          <FormStub />
        </RegistrationWindowGate>,
      );

      expect(screen.getByTestId('rsvp-form-stub')).toBeInTheDocument();
      expect(screen.queryByTestId('registration-opens-soon-cta')).not.toBeInTheDocument();
      expect(screen.queryByTestId('registration-closed-inline')).not.toBeInTheDocument();
    });

    it('renders the child form unchanged when availability is undefined (stale cache)', () => {
      // Backward-compat: a React Query cache from before 6A.153 won't have
      // the field. The gate must default to "open" semantics so existing
      // events keep working immediately after deploy without a cache flush.
      const event = makeEvent();
      delete (event as { registrationAvailability?: string }).registrationAvailability;

      render(
        <RegistrationWindowGate event={event}>
          <FormStub />
        </RegistrationWindowGate>,
      );

      expect(screen.getByTestId('rsvp-form-stub')).toBeInTheDocument();
    });

    it('renders the child form unchanged when availability is "closed-event-started"', () => {
      // The parent cascade already shows the "Event has started" banner for
      // hasStarted events — the gate is a no-op so the parent's banner wins.
      const event = makeEvent({ registrationAvailability: 'closed-event-started' });

      render(
        <RegistrationWindowGate event={event}>
          <FormStub />
        </RegistrationWindowGate>,
      );

      expect(screen.getByTestId('rsvp-form-stub')).toBeInTheDocument();
    });
  });

  describe('not-yet-open state', () => {
    it('hides the child form and renders the Register CTA + hint', () => {
      const event = makeEvent({
        registrationAvailability: 'not-yet-open',
        registrationOpensAt: '2026-11-01T15:00:00Z',
        timeZoneId: 'America/New_York',
      });

      render(
        <RegistrationWindowGate event={event}>
          <FormStub />
        </RegistrationWindowGate>,
      );

      expect(screen.queryByTestId('rsvp-form-stub')).not.toBeInTheDocument();
      expect(screen.getByTestId('registration-opens-soon-cta')).toBeInTheDocument();
      expect(screen.getByTestId('registration-opens-soon-hint')).toBeInTheDocument();
    });

    it('opens the "registration opens soon" modal on Register click', () => {
      const event = makeEvent({
        registrationAvailability: 'not-yet-open',
        registrationOpensAt: '2026-11-01T15:00:00Z',
        timeZoneId: 'America/New_York',
      });

      render(
        <RegistrationWindowGate event={event}>
          <FormStub />
        </RegistrationWindowGate>,
      );

      // Modal absent before click — verbatim user requirement: "we will show
      // Registration/RSVP button but once anybody click it, we will display
      // 'registration will open soon for this event'."
      expect(screen.queryByTestId('registration-opens-soon-modal')).not.toBeInTheDocument();

      fireEvent.click(screen.getByTestId('registration-opens-soon-cta'));

      expect(screen.getByTestId('registration-opens-soon-modal')).toBeInTheDocument();
      // Modal copy includes the formatted opens-at — verify it's a real date,
      // not the literal "soon" fallback. November is robust against locale.
      expect(screen.getByTestId('registration-opens-soon-modal').textContent)
        .toMatch(/November/i);
    });

    it('falls back to "soon" copy when opensAt is missing', () => {
      // Defensive: an event flagged not-yet-open without opensAt is a
      // logical impossibility (the mapper sets the flag only when opensAt is
      // set), but the FE must not crash if the contract drifts. Pin the
      // graceful "soon" fallback in case a hostile payload arrives.
      const event = makeEvent({
        registrationAvailability: 'not-yet-open',
        registrationOpensAt: null,
        timeZoneId: 'America/New_York',
      });

      render(
        <RegistrationWindowGate event={event}>
          <FormStub />
        </RegistrationWindowGate>,
      );

      fireEvent.click(screen.getByTestId('registration-opens-soon-cta'));
      expect(screen.getByTestId('registration-opens-soon-modal').textContent)
        .toMatch(/soon/i);
    });
  });

  describe('closed-by-organizer state', () => {
    it('hides the child form and renders the inline closed microcopy', () => {
      const event = makeEvent({ registrationAvailability: 'closed-by-organizer' });

      render(
        <RegistrationWindowGate event={event}>
          <FormStub />
        </RegistrationWindowGate>,
      );

      expect(screen.queryByTestId('rsvp-form-stub')).not.toBeInTheDocument();
      expect(screen.getByTestId('registration-closed-inline')).toBeInTheDocument();
      // User-confirmed 2026-05-25: closed-by-organizer is inline, NOT a
      // modal. Clicking it does nothing — there's no clickable surface.
      expect(screen.queryByTestId('registration-opens-soon-modal')).not.toBeInTheDocument();
    });
  });
});
