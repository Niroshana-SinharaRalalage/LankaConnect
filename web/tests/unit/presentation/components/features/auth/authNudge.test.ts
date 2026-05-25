import { describe, it, expect } from 'vitest';
import { shouldShowAuthNudge } from '@/presentation/components/features/auth/authNudgePolicy';

/**
 * Phase 6A.144 — shouldShowAuthNudge decision policy
 *
 * Centralized rule for whether the auth-encouragement gate should appear in
 * place of the RSVP form for a given (user, event, session) tuple.
 *
 * Truth table the event detail page is contracted to follow:
 *
 *   authed | free | guestAck | gate?
 *   ------ | ---- | -------- | -----
 *     yes  |  -   |    -     |  no
 *     no   | yes  |    -     |  no
 *     no   |  no  |   yes    |  no
 *     no   |  no  |   no     |  YES   ← only nudge case
 *
 * Keeping this as a pure helper makes the rule testable, refactor-proof,
 * and avoids re-asserting it inside a 2000-line page component.
 */

describe('shouldShowAuthNudge — Phase 6A.144', () => {
  it('returns false for authenticated users on any event', () => {
    expect(shouldShowAuthNudge({ isAuthenticated: true, isFree: false, guestAcknowledged: false })).toBe(false);
    expect(shouldShowAuthNudge({ isAuthenticated: true, isFree: true, guestAcknowledged: false })).toBe(false);
    expect(shouldShowAuthNudge({ isAuthenticated: true, isFree: false, guestAcknowledged: true })).toBe(false);
  });

  it('returns false for anonymous users on free events (no incentive to gate)', () => {
    expect(shouldShowAuthNudge({ isAuthenticated: false, isFree: true, guestAcknowledged: false })).toBe(false);
    expect(shouldShowAuthNudge({ isAuthenticated: false, isFree: true, guestAcknowledged: true })).toBe(false);
  });

  it('returns false for anonymous users on paid events once guest has been acknowledged', () => {
    expect(shouldShowAuthNudge({ isAuthenticated: false, isFree: false, guestAcknowledged: true })).toBe(false);
  });

  it('returns true ONLY for anonymous users on paid events with no guest acknowledgement', () => {
    expect(shouldShowAuthNudge({ isAuthenticated: false, isFree: false, guestAcknowledged: false })).toBe(true);
  });
});
