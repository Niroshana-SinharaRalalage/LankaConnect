/**
 * useHasSignUps
 * Phase 8YB.4 — thin probe over `useEventSignUps` for "does this event have any
 * sign-up lists of `kind`?" without forcing every caller to repeat the
 * `isFetched && (data?.length ?? 0) > 0` boilerplate.
 *
 * Why a separate hook: the public event-details page now needs the same probe
 * for both `SignUpKind.Items` (regular signup lists) and `SignUpKind.Volunteers`
 * (volunteer roles) to gate its quick-nav pills + their sibling sections. Each
 * future `SignUpKind` we add gets the gate for free.
 *
 * Returns a stable `{ hasSignUps, isFetched }` shape so callers can defer
 * rendering until the probe has resolved (avoids the worse "pill flashes in
 * then disappears" failure mode on slow networks).
 */
import type { SignUpKind } from '@/infrastructure/api/types/events.types';
import { useEventSignUps } from './useEventSignUps';

export interface UseHasSignUpsResult {
  /** True when the underlying fetch has resolved AND returned at least one list. */
  hasSignUps: boolean;
  /** Pass-through of React Query's `isFetched` so callers can wait for the probe. */
  isFetched: boolean;
}

export function useHasSignUps(
  eventId: string | undefined,
  kind: SignUpKind,
): UseHasSignUpsResult {
  const { data, isFetched } = useEventSignUps(eventId, kind);
  const hasSignUps = isFetched && (data?.length ?? 0) > 0;
  return { hasSignUps, isFetched };
}
