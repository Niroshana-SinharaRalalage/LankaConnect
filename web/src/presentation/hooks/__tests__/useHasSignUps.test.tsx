import { renderHook } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { useHasSignUps } from '../useHasSignUps';
import { SignUpKind } from '@/infrastructure/api/types/events.types';

// Mock the underlying useEventSignUps hook
const useEventSignUpsMock = vi.fn();
vi.mock('../useEventSignUps', () => ({
  useEventSignUps: (...args: unknown[]) => useEventSignUpsMock(...args),
}));

describe('useHasSignUps (Phase 8YB.4)', () => {
  beforeEach(() => {
    useEventSignUpsMock.mockReset();
  });

  it('returns hasSignUps=false + isFetched=false while the underlying query is in-flight', () => {
    useEventSignUpsMock.mockReturnValue({ data: undefined, isFetched: false });

    const { result } = renderHook(() =>
      useHasSignUps('evt-1', SignUpKind.Items),
    );

    expect(result.current.hasSignUps).toBe(false);
    expect(result.current.isFetched).toBe(false);
  });

  it('returns hasSignUps=false when the fetch resolves with an empty list', () => {
    useEventSignUpsMock.mockReturnValue({ data: [], isFetched: true });

    const { result } = renderHook(() =>
      useHasSignUps('evt-1', SignUpKind.Items),
    );

    expect(result.current.hasSignUps).toBe(false);
    expect(result.current.isFetched).toBe(true);
  });

  it('returns hasSignUps=true when the fetch resolves with one or more lists', () => {
    useEventSignUpsMock.mockReturnValue({
      data: [{ id: 'list-1' }, { id: 'list-2' }],
      isFetched: true,
    });

    const { result } = renderHook(() =>
      useHasSignUps('evt-1', SignUpKind.Items),
    );

    expect(result.current.hasSignUps).toBe(true);
    expect(result.current.isFetched).toBe(true);
  });

  it('passes the kind through to useEventSignUps so cache keys stay distinct', () => {
    useEventSignUpsMock.mockReturnValue({ data: [], isFetched: true });

    renderHook(() => useHasSignUps('evt-7', SignUpKind.Volunteers));

    expect(useEventSignUpsMock).toHaveBeenCalledWith('evt-7', SignUpKind.Volunteers);
  });
});
