/**
 * Slice 8 S8.6 — useEditorHistory tests.
 *
 * Covers both the pure reducers (commit / undo / redo) and the React hook
 * wrapper. The pure reducer tests stay synchronous and hermetic; the hook
 * tests use renderHook + act to exercise the state transitions.
 */

import { describe, it, expect } from 'vitest';
import { act, renderHook } from '@testing-library/react';

import {
  EDITOR_HISTORY_LIMIT,
  commitReducer,
  undoReducer,
  redoReducer,
  useEditorHistory,
} from '../useEditorHistory';

interface S {
  v: number;
}
const seed = (v: number): { present: { v: number }; past: { v: number }[]; future: { v: number }[] } => ({
  present: { v },
  past: [],
  future: [],
});

describe('commitReducer', () => {
  it('pushes the previous present into past and clears future', () => {
    const s1 = seed(1);
    const s2 = commitReducer(s1, { v: 2 });
    expect(s2.present).toEqual({ v: 2 });
    expect(s2.past).toEqual([{ v: 1 }]);
    expect(s2.future).toEqual([]);
  });

  it('no-ops when the next value is identity-equal to present', () => {
    const s1 = seed(1);
    const ret = commitReducer(s1, s1.present);
    expect(ret).toBe(s1);
  });

  it('respects the history limit by dropping oldest snapshots', () => {
    let state = seed(0);
    for (let i = 1; i <= EDITOR_HISTORY_LIMIT + 5; i += 1) {
      state = commitReducer(state, { v: i });
    }
    // past cap = limit; oldest entries dropped. The 5 earliest are gone.
    expect(state.past).toHaveLength(EDITOR_HISTORY_LIMIT);
    expect(state.past[0]).toEqual({ v: 5 });
    expect(state.present).toEqual({ v: EDITOR_HISTORY_LIMIT + 5 });
  });

  it('passes a custom limit through', () => {
    let state = seed(0);
    for (let i = 1; i <= 12; i += 1) {
      state = commitReducer(state, { v: i }, 5);
    }
    expect(state.past).toHaveLength(5);
    expect(state.past[0]).toEqual({ v: 7 });
  });
});

describe('undoReducer', () => {
  it('pops past into present and pushes current present into future', () => {
    const after = commitReducer(commitReducer(seed(1), { v: 2 }), { v: 3 });
    const un = undoReducer(after);
    expect(un.present).toEqual({ v: 2 });
    expect(un.past).toEqual([{ v: 1 }]);
    expect(un.future).toEqual([{ v: 3 }]);
  });

  it('no-ops when past is empty', () => {
    const s1 = seed(1);
    expect(undoReducer(s1)).toBe(s1);
  });
});

describe('redoReducer', () => {
  it('pops future into present and pushes current present into past', () => {
    const after = commitReducer(commitReducer(seed(1), { v: 2 }), { v: 3 });
    const un = undoReducer(after);
    const re = redoReducer(un);
    expect(re.present).toEqual({ v: 3 });
    expect(re.past).toEqual([{ v: 1 }, { v: 2 }]);
    expect(re.future).toEqual([]);
  });

  it('no-ops when future is empty', () => {
    const s = seed(1);
    expect(redoReducer(s)).toBe(s);
  });
});

describe('useEditorHistory — hook behavior', () => {
  it('returns initial state with empty history', () => {
    const { result } = renderHook(() => useEditorHistory<S>({ v: 0 }));
    expect(result.current.present).toEqual({ v: 0 });
    expect(result.current.canUndo).toBe(false);
    expect(result.current.canRedo).toBe(false);
  });

  it('commit pushes new state and enables undo', () => {
    const { result } = renderHook(() => useEditorHistory<S>({ v: 0 }));
    act(() => result.current.commit({ v: 1 }));
    expect(result.current.present).toEqual({ v: 1 });
    expect(result.current.canUndo).toBe(true);
    expect(result.current.canRedo).toBe(false);
  });

  it('commit with updater function sees the latest present', () => {
    const { result } = renderHook(() => useEditorHistory<S>({ v: 0 }));
    act(() => result.current.commit((p) => ({ v: p.v + 1 })));
    act(() => result.current.commit((p) => ({ v: p.v + 1 })));
    expect(result.current.present).toEqual({ v: 2 });
    expect(result.current.past).toEqual([{ v: 0 }, { v: 1 }]);
  });

  it('undo + redo round-trips to the same present', () => {
    const { result } = renderHook(() => useEditorHistory<S>({ v: 0 }));
    act(() => result.current.commit({ v: 1 }));
    act(() => result.current.commit({ v: 2 }));
    act(() => result.current.undo());
    expect(result.current.present).toEqual({ v: 1 });
    expect(result.current.canRedo).toBe(true);
    act(() => result.current.redo());
    expect(result.current.present).toEqual({ v: 2 });
    expect(result.current.canRedo).toBe(false);
  });

  it('commit after undo discards the future (redo no longer possible)', () => {
    const { result } = renderHook(() => useEditorHistory<S>({ v: 0 }));
    act(() => result.current.commit({ v: 1 }));
    act(() => result.current.commit({ v: 2 }));
    act(() => result.current.undo());
    expect(result.current.canRedo).toBe(true);
    act(() => result.current.commit({ v: 99 }));
    expect(result.current.canRedo).toBe(false);
    expect(result.current.present).toEqual({ v: 99 });
  });

  it('undo / redo are no-ops when stacks are empty', () => {
    const { result } = renderHook(() => useEditorHistory<S>({ v: 0 }));
    act(() => result.current.undo());
    expect(result.current.present).toEqual({ v: 0 });
    act(() => result.current.redo());
    expect(result.current.present).toEqual({ v: 0 });
  });

  it('reset replaces present and clears both stacks', () => {
    const { result } = renderHook(() => useEditorHistory<S>({ v: 0 }));
    act(() => result.current.commit({ v: 1 }));
    act(() => result.current.commit({ v: 2 }));
    act(() => result.current.reset({ v: 99 }));
    expect(result.current.present).toEqual({ v: 99 });
    expect(result.current.canUndo).toBe(false);
    expect(result.current.canRedo).toBe(false);
  });

  it('committing the same identity value does not enable undo', () => {
    const { result } = renderHook(() => useEditorHistory<S>({ v: 0 }));
    const sameRef = result.current.present;
    act(() => result.current.commit(sameRef));
    expect(result.current.canUndo).toBe(false);
  });
});
