/**
 * Date Range Utilities
 * Provides helper functions for calculating date ranges used in filtering
 *
 * Phase: Events Page Enhancement
 * Feature: Advanced date filtering (this week, next week, next month)
 */

export type DateRangeOption = 'all' | 'upcoming' | 'thisWeek' | 'nextWeek' | 'nextMonth';

export interface DateRange {
  startDateFrom?: string;
  startDateTo?: string;
}

/**
 * Gets the start of today (midnight)
 */
function getStartOfToday(): Date {
  const now = new Date();
  now.setHours(0, 0, 0, 0);
  return now;
}

/**
 * Gets the end of today (23:59:59.999)
 */
function getEndOfToday(): Date {
  const now = new Date();
  now.setHours(23, 59, 59, 999);
  return now;
}

/**
 * Gets the start of the current week (Sunday)
 */
function getStartOfWeek(date: Date = new Date()): Date {
  const d = new Date(date);
  const day = d.getDay();
  const diff = d.getDate() - day;
  d.setDate(diff);
  d.setHours(0, 0, 0, 0);
  return d;
}

/**
 * Gets the end of the current week (Saturday)
 */
function getEndOfWeek(date: Date = new Date()): Date {
  const d = new Date(date);
  const day = d.getDay();
  const diff = d.getDate() + (6 - day);
  d.setDate(diff);
  d.setHours(23, 59, 59, 999);
  return d;
}

/**
 * Gets the start of the current month
 */
function getStartOfMonth(date: Date = new Date()): Date {
  const d = new Date(date);
  d.setDate(1);
  d.setHours(0, 0, 0, 0);
  return d;
}

/**
 * Gets the end of the current month
 */
function getEndOfMonth(date: Date = new Date()): Date {
  const d = new Date(date);
  d.setMonth(d.getMonth() + 1);
  d.setDate(0);
  d.setHours(23, 59, 59, 999);
  return d;
}

/**
 * Adds days to a date
 */
function addDays(date: Date, days: number): Date {
  const result = new Date(date);
  result.setDate(result.getDate() + days);
  return result;
}

/**
 * Adds months to a date
 */
function addMonths(date: Date, months: number): Date {
  const result = new Date(date);
  result.setMonth(result.getMonth() + months);
  return result;
}

/**
 * Gets date range for "This Week" (from today to end of current week)
 */
export function getThisWeekRange(): DateRange {
  const startDateFrom = getStartOfToday().toISOString();
  const startDateTo = getEndOfWeek().toISOString();

  return {
    startDateFrom,
    startDateTo,
  };
}

/**
 * Gets date range for "Next Week" (start of next week to end of next week)
 */
export function getNextWeekRange(): DateRange {
  const today = new Date();
  const nextWeekStart = addDays(getStartOfWeek(today), 7);
  const nextWeekEnd = addDays(getEndOfWeek(today), 7);

  return {
    startDateFrom: nextWeekStart.toISOString(),
    startDateTo: nextWeekEnd.toISOString(),
  };
}

/**
 * Gets date range for "Next Month" (start of next month to end of next month)
 */
export function getNextMonthRange(): DateRange {
  const today = new Date();
  const nextMonth = addMonths(today, 1);
  const nextMonthStart = getStartOfMonth(nextMonth);
  const nextMonthEnd = getEndOfMonth(nextMonth);

  return {
    startDateFrom: nextMonthStart.toISOString(),
    startDateTo: nextMonthEnd.toISOString(),
  };
}

/**
 * Gets date range for "Upcoming Events" (from now onwards)
 */
export function getUpcomingRange(): DateRange {
  return {
    startDateFrom: new Date().toISOString(),
  };
}

/**
 * Gets date range based on the selected option
 *
 * @param option - The date range option selected by user
 * @returns DateRange object with startDateFrom and/or startDateTo
 *
 * @example
 * ```tsx
 * const range = getDateRangeForOption('thisWeek');
 * // Returns { startDateFrom: '2024-11-25T00:00:00.000Z', startDateTo: '2024-12-01T23:59:59.999Z' }
 * ```
 */
export function getDateRangeForOption(option: DateRangeOption): DateRange {
  switch (option) {
    case 'all':
      return {}; // No date filtering
    case 'upcoming':
      return getUpcomingRange();
    case 'thisWeek':
      return getThisWeekRange();
    case 'nextWeek':
      return getNextWeekRange();
    case 'nextMonth':
      return getNextMonthRange();
    default:
      return getUpcomingRange(); // Default to upcoming
  }
}

/**
 * Gets a human-readable label for the date range option
 */
export function getDateRangeLabel(option: DateRangeOption): string {
  switch (option) {
    case 'all':
      return 'All Events';
    case 'upcoming':
      return 'Upcoming Events';
    case 'thisWeek':
      return 'This Week';
    case 'nextWeek':
      return 'Next Week';
    case 'nextMonth':
      return 'Next Month';
    default:
      return 'Upcoming Events';
  }
}
