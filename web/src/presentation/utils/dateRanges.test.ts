import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import {
  getDateRangeForOption,
  getDateRangeLabel,
  getThisWeekRange,
  getNextWeekRange,
  getNextMonthRange,
  getUpcomingRange,
} from './dateRanges';

describe('dateRanges utilities', () => {
  beforeEach(() => {
    // Mock the current date to 2024-11-25 (Monday) for consistent testing
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2024-11-25T10:00:00Z'));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe('getUpcomingRange', () => {
    it('should return date range starting from now', () => {
      const result = getUpcomingRange();

      expect(result.startDateFrom).toBe('2024-11-25T10:00:00.000Z');
      expect(result.startDateTo).toBeUndefined();
    });
  });

  describe('getThisWeekRange', () => {
    it('should return date range from today to end of current week', () => {
      const result = getThisWeekRange();

      // Should start from beginning of today (2024-11-25)
      expect(result.startDateFrom).toContain('2024-11-25');
      // Should end at end of Saturday (2024-11-30)
      expect(result.startDateTo).toContain('2024-11-30');
    });
  });

  describe('getNextWeekRange', () => {
    it('should return date range for next week', () => {
      const result = getNextWeekRange();

      // Next week starts on Sunday 2024-12-01
      expect(result.startDateFrom).toContain('2024-12-01');
      // Next week ends on Saturday 2024-12-07
      expect(result.startDateTo).toContain('2024-12-07');
    });
  });

  describe('getNextMonthRange', () => {
    it('should return date range for next month', () => {
      const result = getNextMonthRange();

      // Next month is December 2024
      expect(result.startDateFrom).toContain('2024-12-01');
      expect(result.startDateTo).toContain('2024-12-31');
    });
  });

  describe('getDateRangeForOption', () => {
    it('should return empty object for "all" option', () => {
      const result = getDateRangeForOption('all');
      expect(result).toEqual({});
    });

    it('should return upcoming range for "upcoming" option', () => {
      const result = getDateRangeForOption('upcoming');
      expect(result.startDateFrom).toBeDefined();
      expect(result.startDateTo).toBeUndefined();
    });

    it('should return this week range for "thisWeek" option', () => {
      const result = getDateRangeForOption('thisWeek');
      expect(result.startDateFrom).toBeDefined();
      expect(result.startDateTo).toBeDefined();
    });

    it('should return next week range for "nextWeek" option', () => {
      const result = getDateRangeForOption('nextWeek');
      expect(result.startDateFrom).toBeDefined();
      expect(result.startDateTo).toBeDefined();
    });

    it('should return next month range for "nextMonth" option', () => {
      const result = getDateRangeForOption('nextMonth');
      expect(result.startDateFrom).toBeDefined();
      expect(result.startDateTo).toBeDefined();
    });

    it('should default to upcoming for unknown option', () => {
      const result = getDateRangeForOption('unknown' as any);
      expect(result.startDateFrom).toBeDefined();
      expect(result.startDateTo).toBeUndefined();
    });
  });

  describe('getDateRangeLabel', () => {
    it('should return correct labels for all options', () => {
      expect(getDateRangeLabel('all')).toBe('All Events');
      expect(getDateRangeLabel('upcoming')).toBe('Upcoming Events');
      expect(getDateRangeLabel('thisWeek')).toBe('This Week');
      expect(getDateRangeLabel('nextWeek')).toBe('Next Week');
      expect(getDateRangeLabel('nextMonth')).toBe('Next Month');
    });

    it('should default to "Upcoming Events" for unknown option', () => {
      expect(getDateRangeLabel('unknown' as any)).toBe('Upcoming Events');
    });
  });
});
