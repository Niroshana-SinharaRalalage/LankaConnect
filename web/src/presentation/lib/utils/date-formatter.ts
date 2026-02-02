/**
 * Phase 6A.97: Date/Time formatting utility for consistent timezone display
 *
 * This utility handles timezone-aware date formatting for events.
 * All event dates are stored in UTC in the database and converted to
 * the event's local timezone for display.
 *
 * Usage:
 * - formatEventDate(startDate, 'America/New_York') => "January 15, 2026"
 * - formatEventTime(startDate, 'America/New_York') => "6:00 PM EST"
 * - formatEventDateTime(startDate, 'America/New_York') => "January 15, 2026 at 6:00 PM EST"
 */

/**
 * Default timezone when not specified (Eastern Time - most Sri Lankan communities in USA are on East Coast)
 */
export const DEFAULT_TIMEZONE_ID = 'America/New_York';

/**
 * Standard timezone abbreviations mapping with DST support
 */
const TIMEZONE_ABBREVIATIONS: Record<string, { standard: string; daylight: string }> = {
  'America/New_York': { standard: 'EST', daylight: 'EDT' },
  'America/Chicago': { standard: 'CST', daylight: 'CDT' },
  'America/Denver': { standard: 'MST', daylight: 'MDT' },
  'America/Phoenix': { standard: 'MST', daylight: 'MST' }, // Arizona doesn't observe DST
  'America/Los_Angeles': { standard: 'PST', daylight: 'PDT' },
  'America/Anchorage': { standard: 'AKST', daylight: 'AKDT' },
  'Pacific/Honolulu': { standard: 'HST', daylight: 'HST' }, // Hawaii doesn't observe DST
};

/**
 * Converts a UTC date string to the specified timezone
 */
export function toLocalTime(utcDateString: string, timeZoneId?: string | null): Date {
  const tzId = timeZoneId || DEFAULT_TIMEZONE_ID;
  const utcDate = new Date(utcDateString);

  try {
    // Format in the specified timezone to get the local time string
    const localDateStr = utcDate.toLocaleString('en-US', {
      timeZone: tzId,
      year: 'numeric',
      month: 'numeric',
      day: 'numeric',
      hour: 'numeric',
      minute: 'numeric',
      second: 'numeric',
      hour12: false,
    });

    // Parse the local time string back to a Date (this Date object will represent the local time)
    return new Date(localDateStr);
  } catch {
    // If timezone is invalid, use default
    const localDateStr = utcDate.toLocaleString('en-US', {
      timeZone: DEFAULT_TIMEZONE_ID,
      year: 'numeric',
      month: 'numeric',
      day: 'numeric',
      hour: 'numeric',
      minute: 'numeric',
      second: 'numeric',
      hour12: false,
    });
    return new Date(localDateStr);
  }
}

/**
 * Gets the timezone abbreviation (e.g., "EST", "PDT") for display
 * Accounts for Daylight Saving Time
 */
export function getTimezoneAbbreviation(timeZoneId?: string | null, utcDateString?: string): string {
  const tzId = timeZoneId || DEFAULT_TIMEZONE_ID;

  try {
    // Use Intl.DateTimeFormat to detect if DST is in effect
    const date = utcDateString ? new Date(utcDateString) : new Date();

    // Get timezone offset to determine if DST
    const winterDate = new Date(date.getFullYear(), 0, 1); // January 1
    const summerDate = new Date(date.getFullYear(), 6, 1); // July 1

    const winterOffset = new Date(winterDate.toLocaleString('en-US', { timeZone: tzId })).getTime() - winterDate.getTime();
    const summerOffset = new Date(summerDate.toLocaleString('en-US', { timeZone: tzId })).getTime() - summerDate.getTime();
    const currentOffset = new Date(date.toLocaleString('en-US', { timeZone: tzId })).getTime() - date.getTime();

    // If current offset matches summer offset and differs from winter, it's DST
    const isDST = winterOffset !== summerOffset && currentOffset === summerOffset;

    const abbr = TIMEZONE_ABBREVIATIONS[tzId];
    if (abbr) {
      return isDST ? abbr.daylight : abbr.standard;
    }

    // Fallback: try to extract from Intl API
    const formatter = new Intl.DateTimeFormat('en-US', {
      timeZone: tzId,
      timeZoneName: 'short',
    });
    const parts = formatter.formatToParts(date);
    const tzPart = parts.find(p => p.type === 'timeZoneName');
    return tzPart?.value || 'EST';
  } catch {
    return 'EST'; // Safe fallback
  }
}

/**
 * Formats a UTC date for display (date only)
 * Example: "January 15, 2026"
 */
export function formatEventDate(utcDateString: string, timeZoneId?: string | null): string {
  const tzId = timeZoneId || DEFAULT_TIMEZONE_ID;
  const date = new Date(utcDateString);

  try {
    return date.toLocaleDateString('en-US', {
      timeZone: tzId,
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  } catch {
    return date.toLocaleDateString('en-US', {
      timeZone: DEFAULT_TIMEZONE_ID,
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  }
}

/**
 * Formats a UTC time for display (time only, with timezone abbreviation)
 * Example: "6:00 PM EST"
 */
export function formatEventTime(utcDateString: string, timeZoneId?: string | null): string {
  const tzId = timeZoneId || DEFAULT_TIMEZONE_ID;
  const date = new Date(utcDateString);

  try {
    const timeStr = date.toLocaleTimeString('en-US', {
      timeZone: tzId,
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
    const tzAbbr = getTimezoneAbbreviation(tzId, utcDateString);
    return `${timeStr} ${tzAbbr}`;
  } catch {
    const timeStr = date.toLocaleTimeString('en-US', {
      timeZone: DEFAULT_TIMEZONE_ID,
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
    const tzAbbr = getTimezoneAbbreviation(DEFAULT_TIMEZONE_ID, utcDateString);
    return `${timeStr} ${tzAbbr}`;
  }
}

/**
 * Formats a UTC time for display (time only, without timezone)
 * Example: "6:00 PM"
 */
export function formatEventTimeOnly(utcDateString: string, timeZoneId?: string | null): string {
  const tzId = timeZoneId || DEFAULT_TIMEZONE_ID;
  const date = new Date(utcDateString);

  try {
    return date.toLocaleTimeString('en-US', {
      timeZone: tzId,
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  } catch {
    return date.toLocaleTimeString('en-US', {
      timeZone: DEFAULT_TIMEZONE_ID,
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  }
}

/**
 * Formats a UTC datetime for display (date and time with timezone)
 * Example: "January 15, 2026 at 6:00 PM EST"
 */
export function formatEventDateTime(utcDateString: string, timeZoneId?: string | null): string {
  const dateStr = formatEventDate(utcDateString, timeZoneId);
  const timeStr = formatEventTime(utcDateString, timeZoneId);
  return `${dateStr} at ${timeStr}`;
}

/**
 * Formats a date range for display
 * Example: "January 15, 2026 6:00 PM - 9:00 PM EST"
 * Or if different days: "January 15, 2026 6:00 PM - January 16, 2026 2:00 PM EST"
 */
export function formatEventDateRange(
  startDateString: string,
  endDateString: string,
  timeZoneId?: string | null
): string {
  const tzId = timeZoneId || DEFAULT_TIMEZONE_ID;
  const startDate = new Date(startDateString);
  const endDate = new Date(endDateString);

  // Check if same day (in the event's timezone)
  const startDateStr = formatEventDate(startDateString, tzId);
  const endDateStr = formatEventDate(endDateString, tzId);

  if (startDateStr === endDateStr) {
    // Same day - show date once with time range
    const startTime = formatEventTimeOnly(startDateString, tzId);
    const endTime = formatEventTime(endDateString, tzId); // End time includes timezone
    return `${startDateStr} ${startTime} - ${endTime}`;
  } else {
    // Different days - show both dates
    const startDateTime = formatEventDateTime(startDateString, tzId);
    const endDateTime = formatEventDateTime(endDateString, tzId);
    return `${startDateTime} - ${endDateTime}`;
  }
}

/**
 * Formats a short date for display (compact format)
 * Example: "Jan 15" or "Jan 15, 2026" (if different year)
 */
export function formatEventDateShort(utcDateString: string, timeZoneId?: string | null): string {
  const tzId = timeZoneId || DEFAULT_TIMEZONE_ID;
  const date = new Date(utcDateString);
  const now = new Date();

  try {
    const dateOptions: Intl.DateTimeFormatOptions = {
      timeZone: tzId,
      month: 'short',
      day: 'numeric',
    };

    // Add year if different from current year
    if (date.getFullYear() !== now.getFullYear()) {
      dateOptions.year = 'numeric';
    }

    return date.toLocaleDateString('en-US', dateOptions);
  } catch {
    return date.toLocaleDateString('en-US', {
      timeZone: DEFAULT_TIMEZONE_ID,
      month: 'short',
      day: 'numeric',
    });
  }
}
