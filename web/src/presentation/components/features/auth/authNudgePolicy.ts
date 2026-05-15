/**
 * Phase 6A.144 — Auth-encouragement gate decision policy
 *
 * The single source of truth for "should we show the AuthEncouragementPrompt
 * in place of the RSVP form for this user on this event?". Pinned by the
 * tests in `tests/unit/presentation/components/features/auth/authNudge.test.ts`.
 *
 * Kept as a pure helper (no hooks, no DOM) so the rule is refactor-proof and
 * we don't re-assert it inside a 2000-line page component.
 */

export interface AuthNudgeInputs {
  /** Has Zustand reported a hydrated, authenticated user? */
  isAuthenticated: boolean;
  /** Is the event free (no payment / ticket / refund lifecycle)? */
  isFree: boolean;
  /** Did the user already click "Continue as Guest" this session for this event? */
  guestAcknowledged: boolean;
}

export function shouldShowAuthNudge({
  isAuthenticated,
  isFree,
  guestAcknowledged,
}: AuthNudgeInputs): boolean {
  if (isAuthenticated) return false;
  if (isFree) return false;
  if (guestAcknowledged) return false;
  return true;
}

/** sessionStorage key for the per-event guest acknowledgement flag. */
export const guestAckStorageKey = (eventId: string): string => `lc:guest-ack:${eventId}`;
