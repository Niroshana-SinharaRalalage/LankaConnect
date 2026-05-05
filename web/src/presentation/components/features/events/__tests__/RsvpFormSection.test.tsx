import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { RsvpFormSection } from '../RsvpFormSection';
import { RegistrationMode, type EventDto } from '@/infrastructure/api/types/events.types';

/**
 * Phase 7E paid-B-mode gate (review iteration 1, 2026-04-28) — RsvpFormSection dispatcher
 * test suite. Mocks the heavy form children so we can assert which branch the dispatcher
 * picks based on event.registrationMode + event.registrationModeStatus without dragging in
 * react-hook-form / Zustand stores.
 */

vi.mock('../EventRegistrationForm', () => ({
  EventRegistrationForm: () => <div data-testid="mock-event-registration-form" />,
}));

vi.mock('../HeadCountRsvpForm', () => ({
  HeadCountRsvpForm: () => <div data-testid="mock-headcount-rsvp-form" />,
}));

function buildEvent(overrides: Partial<EventDto> = {}): EventDto {
  // Minimal shape — RsvpFormSection only inspects a handful of fields.
  return {
    id: 'evt-test',
    title: 'Test Event',
    description: '',
    startDate: '2030-01-01T00:00:00Z',
    endDate: '2030-01-01T02:00:00Z',
    organizerId: 'user-1',
    capacity: 100,
    currentRegistrations: 0,
    maxAttendeesPerRegistration: 10,
    registrationMode: RegistrationMode.DetailedAttendees,
    registrationModeStatus: 'active',
    isFree: true,
    status: 0 as any,
    category: 0 as any,
    createdAt: '2026-04-28T00:00:00Z',
    ...overrides,
  } as EventDto;
}

describe('RsvpFormSection — paid-B-mode gate dispatcher', () => {
  const noop = () => Promise.resolve();

  describe('registrationModeStatus = "deferred" (paid + B-mode legacy event)', () => {
    it('renders the "coming soon" panel and not the form', () => {
      const event = buildEvent({
        registrationMode: RegistrationMode.HeadCountByAge,
        registrationModeStatus: 'deferred',
      });

      render(<RsvpFormSection event={event} spotsLeft={50} isProcessing={false} onSubmit={noop} />);

      expect(screen.getByText('Registration coming soon')).toBeInTheDocument();
      expect(screen.queryByTestId('mock-headcount-rsvp-form')).not.toBeInTheDocument();
      expect(screen.queryByTestId('mock-event-registration-form')).not.toBeInTheDocument();
    });

    it('points the user at the organiser-contact section', () => {
      const event = buildEvent({
        registrationMode: RegistrationMode.HeadCountOnly,
        registrationModeStatus: 'deferred',
      });

      render(<RsvpFormSection event={event} spotsLeft={50} isProcessing={false} onSubmit={noop} />);

      expect(
        screen.getByText(/contact the organiser directly/i),
      ).toBeInTheDocument();
    });
  });

  describe('registrationModeStatus = "active"', () => {
    it('renders HeadCountRsvpForm for free Mode B (HeadCountByAge)', () => {
      const event = buildEvent({
        registrationMode: RegistrationMode.HeadCountByAge,
        registrationModeStatus: 'active',
        isFree: true,
      });

      render(<RsvpFormSection event={event} spotsLeft={50} isProcessing={false} onSubmit={noop} />);

      expect(screen.getByTestId('mock-headcount-rsvp-form')).toBeInTheDocument();
      expect(screen.queryByText('Registration coming soon')).not.toBeInTheDocument();
    });

    it('renders HeadCountRsvpForm for paid Mode B after Phase 7E.3b shipped', () => {
      // Phase 7E.3b lifted the PaidHeadCountDeferred gate. Paid + Mode B events with
      // registrationModeStatus = "active" must render the form; the form's submit
      // path returns a Stripe Checkout URL which the page redirects to.
      const event = buildEvent({
        registrationMode: RegistrationMode.HeadCountByAge,
        registrationModeStatus: 'active',
        isFree: false,
      });

      render(<RsvpFormSection event={event} spotsLeft={50} isProcessing={false} onSubmit={noop} />);

      expect(screen.getByTestId('mock-headcount-rsvp-form')).toBeInTheDocument();
      expect(screen.queryByText('Registration coming soon')).not.toBeInTheDocument();
    });

    it('renders the "no registration required" notice for Mode C', () => {
      const event = buildEvent({
        registrationMode: RegistrationMode.NoRegistration,
        registrationModeStatus: 'active',
      });

      render(<RsvpFormSection event={event} spotsLeft={50} isProcessing={false} onSubmit={noop} />);

      expect(screen.getByText('No registration required')).toBeInTheDocument();
      expect(screen.queryByTestId('mock-headcount-rsvp-form')).not.toBeInTheDocument();
      expect(screen.queryByTestId('mock-event-registration-form')).not.toBeInTheDocument();
    });

    it('renders EventRegistrationForm for Mode A (DetailedAttendees)', () => {
      const event = buildEvent({
        registrationMode: RegistrationMode.DetailedAttendees,
        registrationModeStatus: 'active',
      });

      render(<RsvpFormSection event={event} spotsLeft={50} isProcessing={false} onSubmit={noop} />);

      expect(screen.getByTestId('mock-event-registration-form')).toBeInTheDocument();
      expect(screen.queryByText('Registration coming soon')).not.toBeInTheDocument();
      expect(screen.queryByTestId('mock-headcount-rsvp-form')).not.toBeInTheDocument();
    });
  });

  describe('legacy cached payload (registrationModeStatus undefined)', () => {
    it('defaults to "active" so legacy events keep working', () => {
      // A pre-fix React Query cached payload won't have the new field. Defaulting to
      // "active" client-side preserves the existing dispatch path for those events; the
      // server-side default is "deferred" (fail-safe) but the FE side is "active" because
      // the only events that reach this code are events the FE already knew about and was
      // rendering forms for.
      const event = buildEvent({
        registrationMode: RegistrationMode.DetailedAttendees,
      });
      delete (event as any).registrationModeStatus;

      render(<RsvpFormSection event={event} spotsLeft={50} isProcessing={false} onSubmit={noop} />);

      expect(screen.getByTestId('mock-event-registration-form')).toBeInTheDocument();
    });
  });
});
