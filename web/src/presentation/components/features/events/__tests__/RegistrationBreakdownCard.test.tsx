import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { RegistrationBreakdownCard, formatPair } from '../RegistrationBreakdownCard';
import {
  RegistrationMode,
  type RegistrationBreakdownDto,
  type BreakdownPairDto,
} from '@/infrastructure/api/types/events.types';

/**
 * Phase 7F-E.2 — RegistrationBreakdownCard RTL coverage.
 *
 * Architect rule: "N/A" rendering must be a property of the data
 * (`captured == false`). Each test asserts the visible text matches the
 * user-mandated format from the master-TODO spec.
 */

const captured = (left: number, right: number, leftLabel: string, rightLabel: string): BreakdownPairDto =>
  ({ captured: true, left, right, leftLabel, rightLabel });

const notCaptured = (leftLabel: string, rightLabel: string): BreakdownPairDto =>
  ({ captured: false, left: 0, right: 0, leftLabel, rightLabel });

describe('RegistrationBreakdownCard — Phase 7F-E.2', () => {
  it('B1 non-tiered: shows count + N/A for both age and gender', () => {
    const bd: RegistrationBreakdownDto = {
      mode: RegistrationMode.HeadCountOnly,
      isTiered: false,
      totalAttendees: 4,
      rows: [{
        tierName: null,
        count: 4,
        age: notCaptured('Adult', 'Child'),
        gender: notCaptured('Male', 'Female'),
      }],
    };

    render(<RegistrationBreakdownCard breakdown={bd} leadAttendeeName="Alice" />);

    expect(screen.getByText('Lead Attendee:')).toBeInTheDocument();
    expect(screen.getByText('Alice')).toBeInTheDocument();
    expect(screen.getByText('Total attendees:')).toBeInTheDocument();
    // Single row; no Tier line for non-tiered.
    expect(screen.queryByText('Tier:')).not.toBeInTheDocument();
    expect(screen.getByText('Adult/Child:')).toBeInTheDocument();
    expect(screen.getByText('Male/Female:')).toBeInTheDocument();
    // Two N/A values (one per axis).
    expect(screen.getAllByText('N/A')).toHaveLength(2);
  });

  it('B2 multi-tier: per-tier rows with Adult/Child captured, Male/Female N/A', () => {
    const bd: RegistrationBreakdownDto = {
      mode: RegistrationMode.HeadCountByAge,
      isTiered: true,
      totalAttendees: 5,
      rows: [
        {
          tierName: 'VIP',
          count: 3,
          age: captured(2, 1, 'Adult', 'Child'),
          gender: notCaptured('Male', 'Female'),
        },
        {
          tierName: 'General',
          count: 2,
          age: captured(2, 0, 'Adult', 'Child'),
          gender: notCaptured('Male', 'Female'),
        },
      ],
    };

    render(<RegistrationBreakdownCard breakdown={bd} />);

    // Two tier rows.
    expect(screen.getAllByText('Tier:')).toHaveLength(2);
    expect(screen.getByText('VIP')).toBeInTheDocument();
    expect(screen.getByText('General')).toBeInTheDocument();
    // Age captured per tier.
    expect(screen.getByText('2/1')).toBeInTheDocument();   // VIP age
    expect(screen.getByText('2/0')).toBeInTheDocument();   // General age
    // Gender N/A on both rows = 2 N/A elements.
    expect(screen.getAllByText('N/A')).toHaveLength(2);
  });

  it('B3 non-tiered: gender captured, age N/A', () => {
    const bd: RegistrationBreakdownDto = {
      mode: RegistrationMode.HeadCountByGender,
      isTiered: false,
      totalAttendees: 3,
      rows: [{
        tierName: null,
        count: 3,
        age: notCaptured('Adult', 'Child'),
        gender: captured(2, 1, 'Male', 'Female'),
      }],
    };

    render(<RegistrationBreakdownCard breakdown={bd} />);

    expect(screen.getByText('N/A')).toBeInTheDocument(); // age only
    expect(screen.getByText('2/1')).toBeInTheDocument(); // gender captured
  });

  it('B4 non-tiered: both age and gender captured', () => {
    const bd: RegistrationBreakdownDto = {
      mode: RegistrationMode.HeadCountByAgeAndGender,
      isTiered: false,
      totalAttendees: 4,
      rows: [{
        tierName: null,
        count: 4,
        age: captured(2, 2, 'Adult', 'Child'),
        gender: captured(2, 2, 'Male', 'Female'),
      }],
    };

    render(<RegistrationBreakdownCard breakdown={bd} />);

    expect(screen.queryByText('N/A')).not.toBeInTheDocument();
    expect(screen.getAllByText('2/2')).toHaveLength(2); // age + gender both 2/2
  });

  it('Mode A multi-tier: shows per-tier breakdown derived from attendees', () => {
    const bd: RegistrationBreakdownDto = {
      mode: RegistrationMode.DetailedAttendees,
      isTiered: true,
      totalAttendees: 4,
      rows: [
        {
          tierName: 'VIP',
          count: 3,
          age: captured(2, 1, 'Adult', 'Child'),
          gender: captured(1, 2, 'Male', 'Female'),
        },
        {
          tierName: 'General',
          count: 1,
          age: captured(1, 0, 'Adult', 'Child'),
          gender: captured(1, 0, 'Male', 'Female'),
        },
      ],
    };

    render(<RegistrationBreakdownCard breakdown={bd} />);

    expect(screen.getAllByText('Tier:')).toHaveLength(2);
    expect(screen.getByText('VIP')).toBeInTheDocument();
    expect(screen.getByText('2/1')).toBeInTheDocument();   // VIP age
    expect(screen.getByText('1/2')).toBeInTheDocument();   // VIP gender
    // General row has age=1/0 AND gender=1/0 → 2 occurrences expected.
    expect(screen.getAllByText('1/0')).toHaveLength(2);
  });

  it('returns null when breakdown has empty rows (defensive)', () => {
    const bd: RegistrationBreakdownDto = {
      mode: RegistrationMode.DetailedAttendees,
      isTiered: false,
      totalAttendees: 0,
      rows: [],
    };
    const { container } = render(<RegistrationBreakdownCard breakdown={bd} />);
    expect(container.firstChild).toBeNull();
  });

  describe('formatPair pure helper', () => {
    it('returns "N/A" when not captured', () => {
      expect(formatPair(notCaptured('Adult', 'Child'))).toBe('N/A');
    });
    it('returns "left/right" when captured', () => {
      expect(formatPair(captured(2, 1, 'Adult', 'Child'))).toBe('2/1');
    });
    it('returns "0/0" when captured but both zero (vs N/A)', () => {
      expect(formatPair(captured(0, 0, 'Adult', 'Child'))).toBe('0/0');
    });
  });
});
