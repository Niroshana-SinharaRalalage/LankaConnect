'use client';

import { Tag } from 'lucide-react';
import { EventCategory } from '@/infrastructure/api/types/events.types';
import { getEventCategoryOptions } from '@/lib/enum-utils';

interface CategoryFilterProps {
  value: EventCategory | null;
  onChange: (category: EventCategory | null) => void;
  className?: string;
}

/**
 * CategoryFilter component for event filtering
 * Phase 6A.47: Filter events by category type
 *
 * Features:
 * - Dropdown with all event categories from backend enum
 * - "All Categories" option to clear filter
 * - Category icon indicator
 * - Dynamically loads categories from EventCategory enum (no hardcoding)
 *
 * Design Note: Uses enum-utils to stay in sync with backend
 */
export function CategoryFilter({
  value,
  onChange,
  className = '',
}: CategoryFilterProps) {
  const categories = getEventCategoryOptions();

  return (
    <div className={`relative ${className}`}>
      <label htmlFor="category-filter" className="sr-only">
        Filter by category
      </label>
      <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
        <Tag className="h-5 w-5 text-gray-400" aria-hidden="true" />
      </div>
      <select
        id="category-filter"
        value={value !== null ? value : ''}
        onChange={(e) =>
          onChange(e.target.value !== '' ? parseInt(e.target.value, 10) as EventCategory : null)
        }
        className="block w-full pl-10 pr-10 py-2.5 border border-gray-300 rounded-lg
                   focus:ring-2 focus:ring-blue-500 focus:border-blue-500
                   text-sm
                   transition-colors duration-200
                   appearance-none bg-white cursor-pointer"
      >
        <option value="">All Categories</option>
        {categories.map((category) => (
          <option key={category.value} value={category.value}>
            {category.label}
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