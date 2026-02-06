'use client';

import { Activity } from 'lucide-react';
import { EventStatusFilter, EventStatusFilterLabels } from '@/infrastructure/api/types/events.types';

interface StatusFilterProps {
  value: EventStatusFilter | null;
  onChange: (statusFilter: EventStatusFilter | null) => void;
  /** When true, shows the "Unpublished Events" option (for organizers) */
  showUnpublished?: boolean;
  className?: string;
}

/**
 * StatusFilter component for event filtering
 * Issue #36: User-friendly status filter groups for event listing pages
 *
 * Features:
 * - Dropdown with status filter options (Active, Inactive, Cancelled, All)
 * - "Unpublished" option visible only when showUnpublished=true (organizer view)
 * - Status icon indicator
 *
 * Status Mappings:
 * - Active: Published + Active (upcoming/ongoing events)
 * - Inactive: Completed + Archived + Postponed (past/paused events)
 * - Cancelled: Cancelled only
 * - All: Everything except Draft/UnderReview (public view)
 * - Unpublished: Draft + UnderReview (organizer-only)
 */
export function StatusFilter({
  value,
  onChange,
  showUnpublished = false,
  className = '',
}: StatusFilterProps) {
  // Build the list of options based on showUnpublished prop
  const options = [
    { value: EventStatusFilter.All, label: EventStatusFilterLabels[EventStatusFilter.All] },
    { value: EventStatusFilter.Active, label: EventStatusFilterLabels[EventStatusFilter.Active] },
    { value: EventStatusFilter.Inactive, label: EventStatusFilterLabels[EventStatusFilter.Inactive] },
    { value: EventStatusFilter.Cancelled, label: EventStatusFilterLabels[EventStatusFilter.Cancelled] },
    ...(showUnpublished
      ? [{ value: EventStatusFilter.Unpublished, label: EventStatusFilterLabels[EventStatusFilter.Unpublished] }]
      : []),
  ];

  return (
    <div className={`relative ${className}`}>
      <label htmlFor="status-filter" className="sr-only">
        Filter by status
      </label>
      <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
        <Activity className="h-5 w-5 text-gray-400" aria-hidden="true" />
      </div>
      <select
        id="status-filter"
        value={value !== null ? value : ''}
        onChange={(e) =>
          onChange(e.target.value !== '' ? parseInt(e.target.value, 10) as EventStatusFilter : null)
        }
        className="block w-full pl-10 pr-10 py-2.5 border border-gray-300 rounded-lg
                   focus:ring-2 focus:ring-blue-500 focus:border-blue-500
                   text-sm
                   transition-colors duration-200
                   appearance-none bg-white cursor-pointer"
      >
        {options.map((option) => (
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
