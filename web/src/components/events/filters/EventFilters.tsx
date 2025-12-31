'use client';

import { useState, useEffect, useRef } from 'react';
import { useDebounce } from '@/hooks/useDebounce';
import { SearchInput } from './SearchInput';
import { CategoryFilter } from './CategoryFilter';
import { DateRangeFilter, getDateRangeFromPreset, type DateRangePreset } from './DateRangeFilter';
import { LocationFilter } from './LocationFilter';
import { EventCategory } from '@/infrastructure/api/types/events.types';

export interface EventFiltersState {
  searchTerm: string;
  category: EventCategory | null;
  dateRange: DateRangePreset;
  metroAreaIds: string[];
  state?: string;
}

export interface EventFiltersProps {
  /** Current filter values */
  filters: EventFiltersState;
  /** Callback when filters change */
  onFiltersChange: (filters: EventFiltersState) => void;
  /** Show/hide specific filters */
  showSearch?: boolean;
  showCategory?: boolean;
  showDateRange?: boolean;
  showLocation?: boolean;
  /** Custom class name */
  className?: string;
}

/**
 * EventFilters - Reusable event filtering component
 * Phase 6A.47: Unified filter UI for all event listing pages
 *
 * Features:
 * - Search input with 300ms debounce
 * - Category dropdown (dynamic from enum)
 * - Date range presets
 * - Location TreeDropdown (State > Metro Areas)
 * - Configurable - show/hide individual filters
 * - Single source of truth for filter state
 *
 * Usage:
 * ```tsx
 * const [filters, setFilters] = useState<EventFiltersState>({
 *   searchTerm: '',
 *   category: null,
 *   dateRange: 'upcoming',
 *   metroAreaIds: [],
 * });
 *
 * <EventFilters
 *   filters={filters}
 *   onFiltersChange={setFilters}
 *   showSearch={true}
 *   showCategory={true}
 *   showDateRange={true}
 *   showLocation={true}
 * />
 * ```
 */
export function EventFilters({
  filters,
  onFiltersChange,
  showSearch = true,
  showCategory = true,
  showDateRange = true,
  showLocation = true,
  className = '',
}: EventFiltersProps) {
  // Local state for immediate search input updates (before debounce)
  const [searchInput, setSearchInput] = useState(filters.searchTerm);

  // Debounce search term to avoid excessive API calls
  const debouncedSearchTerm = useDebounce(searchInput, 300);

  // Ref to track previous search term to avoid unnecessary updates
  const prevSearchTermRef = useRef(filters.searchTerm);

  // Update parent when debounced search term changes
  // Phase 6A.58 FLICKERING FIX: Only depend on debouncedSearchTerm to avoid re-render loop
  useEffect(() => {
    // Only update if debounced term actually changed from what parent has
    if (debouncedSearchTerm !== prevSearchTermRef.current) {
      prevSearchTermRef.current = debouncedSearchTerm;
      onFiltersChange({ ...filters, searchTerm: debouncedSearchTerm });
    }
  }, [debouncedSearchTerm]); // ✅ ONLY depend on debouncedSearchTerm

  // Sync local search input with external filter changes
  // Phase 6A.58 FLICKERING FIX: Only update when external change differs from local state
  useEffect(() => {
    if (filters.searchTerm !== searchInput && filters.searchTerm !== debouncedSearchTerm) {
      setSearchInput(filters.searchTerm);
      prevSearchTermRef.current = filters.searchTerm;
    }
  }, [filters.searchTerm]); // ✅ ONLY depend on filters.searchTerm

  const handleSearchChange = (value: string) => {
    setSearchInput(value);
  };

  const handleCategoryChange = (category: EventCategory | null) => {
    onFiltersChange({ ...filters, category });
  };

  const handleDateRangeChange = (dateRange: DateRangePreset) => {
    onFiltersChange({ ...filters, dateRange });
  };

  const handleMetroAreasChange = (metroAreaIds: string[]) => {
    onFiltersChange({ ...filters, metroAreaIds });
  };

  return (
    <div className={`grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 ${className}`}>
      {/* Search Input */}
      {showSearch && (
        <div className="lg:col-span-1">
          <label htmlFor="search-input" className="block text-sm font-medium text-gray-700 mb-2">
            Search
          </label>
          <SearchInput
            value={searchInput}
            onChange={handleSearchChange}
            placeholder="Search events..."
          />
        </div>
      )}

      {/* Category Filter */}
      {showCategory && (
        <div className="lg:col-span-1">
          <label htmlFor="category-filter" className="block text-sm font-medium text-gray-700 mb-2">
            Category
          </label>
          <CategoryFilter value={filters.category} onChange={handleCategoryChange} />
        </div>
      )}

      {/* Date Range Filter */}
      {showDateRange && (
        <div className="lg:col-span-1">
          <label htmlFor="date-range-filter" className="block text-sm font-medium text-gray-700 mb-2">
            Date Range
          </label>
          <DateRangeFilter value={filters.dateRange} onChange={handleDateRangeChange} />
        </div>
      )}

      {/* Location Filter */}
      {showLocation && (
        <div className="lg:col-span-1">
          <label htmlFor="location-filter" className="block text-sm font-medium text-gray-700 mb-2">
            Location
          </label>
          <LocationFilter
            selectedMetroAreaIds={filters.metroAreaIds}
            onMetroAreasChange={handleMetroAreasChange}
          />
        </div>
      )}
    </div>
  );
}

/**
 * Utility function to convert EventFiltersState to API request parameters
 * Handles date range preset conversion to actual dates
 */
export function filtersToApiParams(filters: EventFiltersState) {
  const dateRange = getDateRangeFromPreset(filters.dateRange);

  return {
    searchTerm: filters.searchTerm || undefined,
    category: filters.category !== null ? filters.category : undefined,
    startDateFrom: dateRange.startDateFrom,
    startDateTo: dateRange.startDateTo,
    state: filters.state,
    metroAreaIds: filters.metroAreaIds.length > 0 ? filters.metroAreaIds : undefined,
  };
}
