/**
 * Slice 8 S8.6: bounded-history reducer for the canvas editor's draft state.
 *
 * Wraps the three draft slices (geometry overrides, additions, deletions) in
 * a past / present / future triad so the editor can undo the last N edits and
 * redo any edits that were undone. The ratio `N = 50` is the architect's
 * spec cap; larger histories grow memory without meaningful UX gain.
 *
 * This module is intentionally generic over the state shape — it only knows
 * "there's a present" plus "push snapshots before every change". That lets
 * S8.8 treat the history-aware commits the same as any other state update
 * when serializing for Save.
 *
 * The hook is pure: it never touches Konva, the repository, or storage.
 * The caller owns `initial` and the `apply` function that produces the
 * next state from the current one.
 */

import { useCallback, useMemo, useRef, useState } from 'react';

/** Architect-spec upper bound on undo depth. */
export const EDITOR_HISTORY_LIMIT = 50;

export interface EditorHistory<T> {
  /** The current state visible to the editor. */
  present: T;
  /** Snapshots taken *before* each successive commit, oldest first. */
  past: T[];
  /** Snapshots popped by undo, newest first (LIFO for redo). */
  future: T[];
}

export interface UseEditorHistoryReturn<T> {
  present: T;
  past: T[];
  future: T[];
  canUndo: boolean;
  canRedo: boolean;
  /**
   * Apply a new present. `updater` receives the current present and returns
   * the next one. A `setState`-style callback keeps this pure and safe for
   * React batching. Calling with a value equal to the current present is
   * a no-op so spurious re-renders don't inflate the history.
   */
  commit: (updater: T | ((prev: T) => T)) => void;
  /** Step one snapshot backward. No-op when `past` is empty. */
  undo: () => void;
  /** Step one snapshot forward. No-op when `future` is empty. */
  redo: () => void;
  /** Replace the present and discard past + future. Used for hard resets
   * (e.g., Save materialized the draft). */
  reset: (next: T) => void;
}

/**
 * Pure reducer for `commit`. Exported so S8.6 unit tests can run it without
 * a React host and so S8.8 can reuse the semantics at Save time.
 */
export function commitReducer<T>(
  state: EditorHistory<T>,
  next: T,
  limit: number = EDITOR_HISTORY_LIMIT,
): EditorHistory<T> {
  if (Object.is(next, state.present)) return state;
  const nextPast = [...state.past, state.present];
  // Drop the oldest snapshot once we exceed the cap.
  while (nextPast.length > limit) nextPast.shift();
  return { present: next, past: nextPast, future: [] };
}

/** Pure reducer for `undo`. */
export function undoReducer<T>(state: EditorHistory<T>): EditorHistory<T> {
  if (state.past.length === 0) return state;
  const previous = state.past[state.past.length - 1];
  const nextPast = state.past.slice(0, -1);
  const nextFuture = [state.present, ...state.future];
  return { present: previous, past: nextPast, future: nextFuture };
}

/** Pure reducer for `redo`. */
export function redoReducer<T>(state: EditorHistory<T>): EditorHistory<T> {
  if (state.future.length === 0) return state;
  const upcoming = state.future[0];
  const nextFuture = state.future.slice(1);
  const nextPast = [...state.past, state.present];
  return { present: upcoming, past: nextPast, future: nextFuture };
}

export function useEditorHistory<T>(initial: T): UseEditorHistoryReturn<T> {
  const [state, setState] = useState<EditorHistory<T>>(() => ({
    present: initial,
    past: [],
    future: [],
  }));

  // The `updater` form in commit needs a stable reference to the latest
  // present so callers can use it like React's setState without stale
  // closures.
  const latestPresentRef = useRef<T>(initial);
  latestPresentRef.current = state.present;

  const commit = useCallback((updater: T | ((prev: T) => T)) => {
    setState((prev) => {
      const next =
        typeof updater === 'function'
          ? (updater as (p: T) => T)(prev.present)
          : updater;
      return commitReducer(prev, next);
    });
  }, []);

  const undo = useCallback(() => {
    setState((prev) => undoReducer(prev));
  }, []);

  const redo = useCallback(() => {
    setState((prev) => redoReducer(prev));
  }, []);

  const reset = useCallback((next: T) => {
    setState({ present: next, past: [], future: [] });
  }, []);

  const result = useMemo<UseEditorHistoryReturn<T>>(
    () => ({
      present: state.present,
      past: state.past,
      future: state.future,
      canUndo: state.past.length > 0,
      canRedo: state.future.length > 0,
      commit,
      undo,
      redo,
      reset,
    }),
    [state.present, state.past, state.future, commit, undo, redo, reset],
  );

  return result;
}
