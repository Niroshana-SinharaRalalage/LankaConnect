/**
 * Slice 7 Chunk S7.4 — SeatPickerView tests.
 *
 * Focus: the container's session/hold/release/timer lifecycle + click
 * plumbing. The underlying SeatPicker is mocked so these tests don't
 * re-cover rendering (that's in SeatPicker.test.tsx).
 */

import React from 'react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, act, waitFor, fireEvent } from '@testing-library/react';

// react-hot-toast is pulled in by SeatPickerView — shim to avoid jsdom issues.
vi.mock('react-hot-toast', () => ({
  __esModule: true,
  default: { error: vi.fn(), success: vi.fn() },
}));

// Mock the SeatPicker renderer: expose a minimal UI that lets us invoke
// onSeatClick from a test and inspect the props the container passes in.
const seatPickerPropsSpy: Array<Record<string, unknown>> = [];
vi.mock('@/presentation/components/features/events/SeatPicker', () => ({
  __esModule: true,
  SeatPicker: (props: Record<string, unknown>) => {
    seatPickerPropsSpy.push(props);
    const onSeatClick = props.onSeatClick as ((id: string) => void) | undefined;
    return (
      <div data-testid="mock-seat-picker">
        <button
          type="button"
          data-testid="fake-seat-s1"
          onClick={() => onSeatClick?.('s1')}
        >
          s1
        </button>
        <button
          type="button"
          data-testid="fake-seat-s2"
          onClick={() => onSeatClick?.('s2')}
        >
          s2
        </button>
        <button
          type="button"
          data-testid="fake-seat-s3"
          onClick={() => onSeatClick?.('s3')}
        >
          s3
        </button>
      </div>
    );
  },
}));

// Stub hooks so the container sees "layout loaded, no availability yet" by default.
const holdMutate = vi.fn();
const releaseMutate = vi.fn();

vi.mock('@/presentation/hooks/useVenueLayouts', () => ({
  __esModule: true,
  useVenueLayoutByEvent: vi.fn(() => ({
    data: {
      id: 'layout-1',
      name: 'Test Layout',
      eventId: 'event-1',
      layoutType: 'Theater',
      isTemplate: false,
      createdByUserId: 'user-1',
      totalCapacity: 10,
      zones: [
        {
          id: 'z1',
          name: 'VIP',
          color: '#3b82f6',
          sortOrder: 0,
          enabledSeatCount: 3,
          totalSeatCount: 3,
          seats: [
            { id: 's1', row: 'A', number: 1, label: 'A1', sortOrder: 0, isEnabled: true, isAccessible: false },
            { id: 's2', row: 'A', number: 2, label: 'A2', sortOrder: 1, isEnabled: true, isAccessible: false },
            { id: 's3', row: 'A', number: 3, label: 'A3', sortOrder: 2, isEnabled: true, isAccessible: false },
          ],
          shape: 'Rect',
          geometry: '{"x":0,"y":0,"width":400,"height":200}',
          ticketTierIds: ['tier-vip'],
        },
      ],
      tables: [],
      decorations: [],
      canvas: { width: 1200, height: 800, scale: 1, backgroundColor: '#fff' },
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: null,
      rowVersion: 1,
    },
    isLoading: false,
  })),
  useSeatAvailability: vi.fn(() => ({
    data: [
      { id: 's1', label: 'A1', row: 'A', number: 1, isEnabled: true, isAccessible: false, status: 'Available', zoneId: 'z1', zoneName: 'VIP', zoneColor: '#3b82f6' },
      { id: 's2', label: 'A2', row: 'A', number: 2, isEnabled: true, isAccessible: false, status: 'Available', zoneId: 'z1', zoneName: 'VIP', zoneColor: '#3b82f6' },
      { id: 's3', label: 'A3', row: 'A', number: 3, isEnabled: true, isAccessible: false, status: 'Available', zoneId: 'z1', zoneName: 'VIP', zoneColor: '#3b82f6' },
    ],
    isLoading: false,
  })),
  useHoldSeats: vi.fn(() => ({ mutate: holdMutate })),
  useReleaseSeats: vi.fn(() => ({ mutate: releaseMutate })),
}));

import { SeatPickerView } from '@/presentation/components/features/events/SeatPickerView';

beforeEach(() => {
  seatPickerPropsSpy.length = 0;
  holdMutate.mockReset();
  releaseMutate.mockReset();
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
});

function defaultProps(overrides: Partial<Parameters<typeof SeatPickerView>[0]> = {}) {
  return {
    eventId: 'event-1',
    userId: 'user-1',
    maxSeats: 2,
    onSeatsConfirmed: vi.fn(),
    onCancel: vi.fn(),
    ...overrides,
  };
}

describe('SeatPickerView — session, hold, timer, confirm, cancel', () => {
  it('renders the mock seat picker and a disabled Confirm button when nothing is selected', () => {
    render(<SeatPickerView {...defaultProps()} />);

    expect(screen.getByTestId('mock-seat-picker')).toBeInTheDocument();
    expect(screen.getByTestId('seat-picker-confirm')).toBeDisabled();
    expect(screen.getByTestId('seat-picker-summary').textContent).toMatch(
      /No seats selected yet/,
    );
  });

  it('calls useHoldSeats.mutate with every currently selected seat id each click', () => {
    const props = defaultProps({ maxSeats: 2 });
    render(<SeatPickerView {...props} />);

    fireEvent.click(screen.getByTestId('fake-seat-s1'));
    expect(holdMutate).toHaveBeenCalledTimes(1);
    expect(holdMutate.mock.calls[0][0]).toMatchObject({
      seatIds: ['s1'],
    });

    fireEvent.click(screen.getByTestId('fake-seat-s2'));
    expect(holdMutate).toHaveBeenCalledTimes(2);
    const secondCall = holdMutate.mock.calls[1][0] as { seatIds: string[] };
    // Second call replaces the hold with the full set.
    expect(new Set(secondCall.seatIds)).toEqual(new Set(['s1', 's2']));
  });

  it('deselecting releases nothing explicitly (keeps session alive) but keeps the hold with the remaining set', () => {
    render(<SeatPickerView {...defaultProps({ maxSeats: 2 })} />);

    fireEvent.click(screen.getByTestId('fake-seat-s1'));
    fireEvent.click(screen.getByTestId('fake-seat-s2'));
    // Click s1 again → deselect.
    fireEvent.click(screen.getByTestId('fake-seat-s1'));

    // Third hold call sent, this time with only s2.
    const thirdCall = holdMutate.mock.calls[2][0] as { seatIds: string[] };
    expect(thirdCall.seatIds).toEqual(['s2']);
  });

  it('starts a 10-minute countdown when the hold mutation resolves successfully', () => {
    const in10min = new Date(Date.now() + 10 * 60 * 1000).toISOString();
    holdMutate.mockImplementation(
      (_args: unknown, opts: { onSuccess?: (r: { expiresAt: string }) => void }) => {
        opts.onSuccess?.({ expiresAt: in10min });
      },
    );

    render(<SeatPickerView {...defaultProps({ maxSeats: 1 })} />);
    fireEvent.click(screen.getByTestId('fake-seat-s1'));

    // Timer should appear after onSuccess fires.
    const timer = screen.getByTestId('seat-picker-timer');
    expect(timer.textContent).toMatch(/\d{2}:\d{2}/);
    // 10-minute hold just started → MM is 09 or 10 depending on the
    // current second boundary. Either way we expect > 8 minutes.
    const match = timer.textContent?.match(/(\d{2}):(\d{2})/);
    expect(match).toBeTruthy();
    const minutes = Number(match![1]);
    expect(minutes).toBeGreaterThanOrEqual(9);
    expect(minutes).toBeLessThanOrEqual(10);
  });

  it('fires onSeatsConfirmed with the selection and session id when Confirm is clicked at max', () => {
    const in10min = new Date(Date.now() + 10 * 60 * 1000).toISOString();
    holdMutate.mockImplementation(
      (_args: unknown, opts: { onSuccess?: (r: { expiresAt: string }) => void }) => {
        opts.onSuccess?.({ expiresAt: in10min });
      },
    );
    const onSeatsConfirmed = vi.fn();
    render(
      <SeatPickerView
        {...defaultProps({ maxSeats: 2, onSeatsConfirmed })}
      />,
    );

    fireEvent.click(screen.getByTestId('fake-seat-s1'));
    fireEvent.click(screen.getByTestId('fake-seat-s2'));

    const confirm = screen.getByTestId('seat-picker-confirm');
    expect(confirm).not.toBeDisabled();
    fireEvent.click(confirm);

    expect(onSeatsConfirmed).toHaveBeenCalledTimes(1);
    const [seatIds, sessionId] = onSeatsConfirmed.mock.calls[0];
    expect(new Set(seatIds)).toEqual(new Set(['s1', 's2']));
    // crypto.randomUUID() is stubbed in jsdom to produce valid UUIDs.
    expect(typeof sessionId).toBe('string');
    expect(sessionId.length).toBeGreaterThan(0);
  });

  it('cancel releases holds and resets selection', () => {
    const in10min = new Date(Date.now() + 10 * 60 * 1000).toISOString();
    holdMutate.mockImplementation(
      (_args: unknown, opts: { onSuccess?: (r: { expiresAt: string }) => void }) => {
        opts.onSuccess?.({ expiresAt: in10min });
      },
    );
    const onCancel = vi.fn();
    render(<SeatPickerView {...defaultProps({ onCancel })} />);

    fireEvent.click(screen.getByTestId('fake-seat-s1'));
    fireEvent.click(screen.getByRole('button', { name: /cancel/i }));

    expect(releaseMutate).toHaveBeenCalledTimes(1);
    expect(onCancel).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId('seat-picker-summary').textContent).toMatch(
      /No seats selected yet/,
    );
  });

  it('forwards eligibleSeatIds to the picker when tierId is supplied', () => {
    render(
      <SeatPickerView
        {...defaultProps({ maxSeats: 1, tierId: 'tier-vip' })}
      />,
    );

    const lastProps = seatPickerPropsSpy[seatPickerPropsSpy.length - 1];
    const eligible = lastProps.eligibleSeatIds as Set<string>;
    expect(eligible).toBeDefined();
    // Zone z1 is mapped to tier-vip → all its seats eligible.
    expect(eligible.has('s1')).toBe(true);
    expect(eligible.has('s2')).toBe(true);
  });

  it('leaves eligibleSeatIds undefined when tierId is not supplied (no filter)', () => {
    render(<SeatPickerView {...defaultProps()} />);
    const lastProps = seatPickerPropsSpy[seatPickerPropsSpy.length - 1];
    expect(lastProps.eligibleSeatIds).toBeUndefined();
  });
});
