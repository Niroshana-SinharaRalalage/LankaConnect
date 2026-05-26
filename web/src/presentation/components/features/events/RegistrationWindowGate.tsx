'use client';

import { useState } from 'react';
import { Clock, Lock } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/presentation/components/ui/Dialog';
import type { EventDto } from '@/infrastructure/api/types/events.types';

interface RegistrationWindowGateProps {
  event: EventDto;
  /**
   * The original registration form to render when the window is open. Passed
   * as children so this gate is pure UX glue around the existing form — never
   * modifies the form's props or behaviour.
   */
  children: React.ReactNode;
}

/**
 * Phase 6A.153 — gating wrapper around the registration form.
 *
 * Decision tree:
 *   - `registrationAvailability === 'not-yet-open'`   → render the Register
 *     button (visually identical to the "open" state) + on-click open a modal
 *     showing the formatted local opens-at time. Matches the user's verbatim
 *     ask: "show Registration/RSVP button but once anybody click it, we will
 *     display 'registration will open soon for this event'."
 *
 *   - `registrationAvailability === 'closed-by-organizer'` → inline microcopy
 *     ("Registration is closed for this event") with the button disabled.
 *     Different from `not-yet-open` because closure isn't a "come back later"
 *     state — there's nothing actionable behind the click. (User-confirmed
 *     2026-05-25.)
 *
 *   - Anything else (`open`, `closed-event-started`, `undefined` for stale
 *     cache) → render `children` unchanged (the existing RsvpFormSection /
 *     waitlist / etc.). Fail-safe default preserves legacy pre-6A.153
 *     behaviour for events whose API payload doesn't yet include the field.
 *
 * Architectural note: this gate is intentionally PURE about the window
 * state. It does NOT re-implement capacity / already-registered / cancelled
 * / ExternalPaid checks — those still belong to the parent cascade in
 * `/events/[id]/page.tsx`. The gate runs as the innermost wrapper around the
 * RSVP form mount, after all higher-priority states (cancelled, ExternalPaid,
 * already-registered, hasStarted, isFull) have already been resolved.
 */
export function RegistrationWindowGate({ event, children }: RegistrationWindowGateProps) {
  const [opensSoonModalOpen, setOpensSoonModalOpen] = useState(false);
  const availability = event.registrationAvailability ?? 'open';

  if (availability === 'not-yet-open') {
    return (
      <>
        <NotYetOpenButton
          opensAt={event.registrationOpensAt}
          timeZoneId={event.timeZoneId}
          onClick={() => setOpensSoonModalOpen(true)}
        />
        <RegistrationOpensSoonModal
          open={opensSoonModalOpen}
          onOpenChange={setOpensSoonModalOpen}
          opensAt={event.registrationOpensAt}
          timeZoneId={event.timeZoneId}
        />
      </>
    );
  }

  if (availability === 'closed-by-organizer') {
    return <ClosedByOrganizerInlineCopy />;
  }

  return <>{children}</>;
}

/**
 * Visual button shown in the "registration not yet open" state. Reuses the
 * brand-orange palette + datetime icon so it reads as an active CTA — the
 * user's expectation is that the button looks normal until they click.
 */
function NotYetOpenButton({
  opensAt,
  timeZoneId,
  onClick,
}: {
  opensAt?: string | null;
  timeZoneId?: string | null;
  onClick: () => void;
}) {
  const opensLocal = formatOpensAt(opensAt, timeZoneId);

  return (
    <div className="space-y-3">
      <Button
        type="button"
        size="lg"
        className="w-full"
        onClick={onClick}
        data-testid="registration-opens-soon-cta"
      >
        Register / RSVP
      </Button>
      <p className="text-xs text-neutral-500 text-center" data-testid="registration-opens-soon-hint">
        <Clock className="inline h-3 w-3 mr-1" aria-hidden="true" />
        Registration opens {opensLocal}.
      </p>
    </div>
  );
}

/**
 * Modal that fires when the user clicks the Register button on a
 * not-yet-open event. Plain informational surface — no actionable affordances
 * in v1 (the architect's plan notes "leave room" for a future "notify me"
 * button but it's out of scope for this phase).
 */
function RegistrationOpensSoonModal({
  open,
  onOpenChange,
  opensAt,
  timeZoneId,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  opensAt?: string | null;
  timeZoneId?: string | null;
}) {
  const opensLocal = formatOpensAt(opensAt, timeZoneId);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent data-testid="registration-opens-soon-modal">
        <DialogHeader>
          <div className="mx-auto mb-2 flex h-12 w-12 items-center justify-center rounded-full bg-orange-100">
            <Clock className="h-6 w-6 text-orange-600" aria-hidden="true" />
          </div>
          <DialogTitle className="text-center">Registration opens soon</DialogTitle>
          <DialogDescription className="text-center">
            This event is publicly listed in advance. Registration will open <strong>{opensLocal}</strong>. Please check back then to sign up.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter className="justify-center">
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            Got it
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

/**
 * Closed-by-organizer state. Inline microcopy + disabled-look button.
 * Different from "not-yet-open" because there's nothing the user can do —
 * a modal would just be friction with no useful information.
 */
function ClosedByOrganizerInlineCopy() {
  return (
    <div
      className="rounded-lg border border-neutral-200 bg-neutral-50 p-4"
      data-testid="registration-closed-inline"
    >
      <div className="flex items-start gap-2">
        <Lock className="mt-0.5 h-4 w-4 flex-shrink-0 text-neutral-500" aria-hidden="true" />
        <div>
          <p className="text-sm font-medium text-neutral-800">Registration is closed</p>
          <p className="mt-1 text-xs text-neutral-600">
            The organizer has closed registration for this event. Reach out to them directly if you have questions.
          </p>
        </div>
      </div>
    </div>
  );
}

/**
 * Formats a UTC timestamp into the event's local timezone for display.
 * Falls back to the user's browser timezone when TimeZoneId is null
 * (TBD events haven't always had timezone derivation; defensive).
 *
 * Returns a string like "Friday, July 1, 2026 at 9:00 AM EDT" — verbose by
 * design because the modal is the only surface where this date appears and
 * users need to plan around it.
 */
function formatOpensAt(opensAt?: string | null, timeZoneId?: string | null): string {
  if (!opensAt) return 'soon';

  try {
    const date = new Date(opensAt);
    if (isNaN(date.getTime())) return 'soon';

    const options: Intl.DateTimeFormatOptions = {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      timeZoneName: 'short',
    };
    if (timeZoneId) {
      options.timeZone = timeZoneId;
    }
    return new Intl.DateTimeFormat('en-US', options).format(date);
  } catch (err) {
    // Intl.DateTimeFormat throws if TimeZoneId is an unrecognised IANA name.
    // Fall back to soon-ish copy rather than blank — the user is on the
    // "registration opens soon" path; they don't need a precise time.
    // eslint-disable-next-line no-console
    console.warn('[6A.153] formatOpensAt failed', err);
    return 'soon';
  }
}
