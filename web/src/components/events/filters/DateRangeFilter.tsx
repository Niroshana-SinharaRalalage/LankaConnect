'use client';

import { Calendar } from 'lucide-react';

export type DateRangePreset = 'upcoming' | 'this-week' | 'next-week' | 'next-month' | 'all';

interface DateRangeFilterProps {
  value: DateRangePreset;
  onChange: (preset: DateRangePreset) => void;
  className?: string;
}

const dateRangeOptions: Array<{ value: DateRangePreset; label: string }> = [
  { value: 'all', label: 'All Dates' },
  { value: 'upcoming', label: 'Upcoming' },
  { value: 'this-week', label: 'This Week' },
  { value: 'next-week', label: 'Next Week' },
  { value: 'next-month', label: 'Next Month' },
];

/**
 * Utility to convert preset to actual date range
 */
export function getDateRangeFromPreset(preset: DateRangePreset): {
  startDateFrom?: string;
  startDateTo?: string;
} {
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());

  switch (preset) {
    case 'upcoming':
      return {
        startDateFrom: today.toISOString(),
      };
    case 'this-week': {
      const endOfWeek = new Date(today);
      endOfWeek.setDate(today.getDate() + (7 - today.getDay()));
      return {
        startDateFrom: today.toISOString(),
        startDateTo: endOfWeek.toISOString(),
      };
    }
    case 'next-week': {
      const startOfNextWeek = new Date(today);
      startOfNextWeek.setDate(today.getDate() + (7 - today.getDay()) + 1);
      const endOfNextWeek = new Date(startOfNextWeek);
      endOfNextWeek.setDate(startOfNextWeek.getDate() + 6);
      return {
        startDateFrom: startOfNextWeek.toISOString(),
        startDateTo: endOfNextWeek.toISOString(),
      };
    }
    case 'next-month': {
      const startOfNextMonth = new Date(today.getFullYear(), today.getMonth() + 1, 1);
      const endOfNextMonth = new Date(today.getFullYear(), today.getMonth() + 2, 0);
      return {
        startDateFrom: startOfNextMonth.toISOString(),
        startDateTo: endOfNextMonth.toISOString(),
      };
    }
    case 'all':
    default:
      return {};
  }
}

/**
 * DateRangeFilter component for event filtering
 * Phase 6A.47: Filter events by date range presets
 *
 * Features:
 * - Preset date ranges (Upcoming, This Week, Next Week, Next Month, All)
 * - Calendar icon indicator
 * - Converts presets to actual date ranges for API
 */
export function DateRangeFilter({ value, onChange, className = '' }: DateRangeFilterProps) {
  return (
    <div className={`relative ${className}`}>
      <label htmlFor="date-range-filter" className="sr-only">
        Filter by date range
      </label>
      <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
        <Calendar className="h-5 w-5 text-gray-400" aria-hidden="true" />
      </div>
      <select
        id="date-range-filter"
        value={value}
        onChange={(e) => onChange(e.target.value as DateRangePreset)}
        className="block w-full pl-10 pr-10 py-2.5 border border-gray-300 rounded-lg
                   focus:ring-2 focus:ring-blue-500 focus:border-blue-500
                   text-sm
                   transition-colors duration-200
                   appearance-none bg-white cursor-pointer"
      >
        {dateRangeOptions.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      <div className="absolute inset-y-0 right-0 flex items-center pr-3 pointer-events-none">
        <svg
          className="h-5 w-5 text-gray-400"
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 20 20"
          fill="currentColor"
          aria-hidden="true"
        >
          <path
            fillRule="evenodd"
            d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z"
            clipRule="evenodd"
          />
        </svg>
      </div>
    </div>
  );
}
