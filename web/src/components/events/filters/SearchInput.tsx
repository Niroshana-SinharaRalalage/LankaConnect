'use client';

import React from 'react';
import { Search, X } from 'lucide-react';

interface SearchInputProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  className?: string;
}

/**
 * SearchInput component for event filtering
 * Phase 6A.47: Text-based search across event titles and descriptions
 *
 * Features:
 * - Search icon indicator
 * - Clear button when text is present
 * - Accessible with proper labels
 * - Debouncing handled by parent component
 */
export function SearchInput({
  value,
  onChange,
  placeholder = 'Search events...',
  className = '',
}: SearchInputProps) {
  const handleClear = () => {
    onChange('');
  };

  return (
    <div className={`relative ${className}`}>
      <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
        <Search className="h-5 w-5 text-gray-400" aria-hidden="true" />
      </div>
      <input
        type="text"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="block w-full pl-10 pr-10 py-2.5 border border-gray-300 rounded-lg
                   focus:ring-2 focus:ring-blue-500 focus:border-blue-500
                   text-sm placeholder-gray-400
                   transition-colors duration-200"
        aria-label="Search events"
      />
      {value && (
        <button
          type="button"
          onClick={handleClear}
          className="absolute inset-y-0 right-0 flex items-center pr-3
                     text-gray-400 hover:text-gray-600
                     transition-colors duration-200"
          aria-label="Clear search"
        >
          <X className="h-5 w-5" />
        </button>
      )}
    </div>
  );
}