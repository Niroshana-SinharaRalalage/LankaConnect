'use client';

import React, { useState, useRef, useEffect } from 'react';
import { Check, ChevronDown, X } from 'lucide-react';

/**
 * Option for MultiSelect component
 */
export interface MultiSelectOption {
  id: string;
  label: string;
  disabled?: boolean;
}

/**
 * MultiSelect Component Props
 */
export interface MultiSelectProps {
  /** Available options to select from */
  options: MultiSelectOption[];
  /** Selected option IDs */
  value: string[];
  /** Callback when selection changes */
  onChange: (selectedIds: string[]) => void;
  /** Placeholder text when no selection */
  placeholder?: string;
  /** Whether the field has validation error */
  error?: boolean;
  /** Error message to display */
  errorMessage?: string;
  /** Loading state */
  isLoading?: boolean;
  /** Disabled state */
  disabled?: boolean;
  /** Helper text */
  helperText?: string;
  /** Maximum number of selections allowed */
  maxSelection?: number;
}

/**
 * MultiSelect Dropdown Component
 * Reusable component for selecting multiple items from a list
 *
 * Features:
 * - Checkbox-based multi-selection
 * - Keyboard navigation (Space/Enter to toggle)
 * - Loading and disabled states
 * - Validation error display
 * - Max selection limit
 * - Selected count badge
 * - Click outside to close
 *
 * Usage:
 * ```tsx
 * <MultiSelect
 *   options={emailGroups.map(g => ({ id: g.id, label: g.name }))}
 *   value={selectedGroupIds}
 *   onChange={setSelectedGroupIds}
 *   placeholder="Select email groups"
 *   error={!!errors.emailGroupIds}
 *   errorMessage={errors.emailGroupIds?.message}
 * />
 * ```
 */
export function MultiSelect({
  options,
  value,
  onChange,
  placeholder = 'Select options',
  error = false,
  errorMessage,
  isLoading = false,
  disabled = false,
  helperText,
  maxSelection,
}: MultiSelectProps) {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen]);

  // Get selected option labels for display
  const selectedOptions = options.filter(opt => value.includes(opt.id));
  const selectedLabels = selectedOptions.map(opt => opt.label).join(', ');

  // Toggle selection
  const toggleOption = (optionId: string) => {
    if (value.includes(optionId)) {
      // Deselect
      onChange(value.filter(id => id !== optionId));
    } else {
      // Check max selection limit
      if (maxSelection && value.length >= maxSelection) {
        return; // Don't add if max reached
      }
      // Select
      onChange([...value, optionId]);
    }
  };

  // Clear all selections
  const clearAll = () => {
    onChange([]);
  };

  return (
    <div className="w-full relative" ref={containerRef}>
      {/* Dropdown Trigger - Using div with role="combobox" to avoid nested button issue */}
      {/* Phase 6A.25 Fix: Changed from <button> to <div> to allow nested clear button */}
      <div
        role="combobox"
        aria-expanded={isOpen}
        aria-haspopup="listbox"
        tabIndex={disabled || isLoading ? -1 : 0}
        onClick={() => !disabled && !isLoading && setIsOpen(!isOpen)}
        onKeyDown={(e) => {
          if (!disabled && !isLoading && (e.key === 'Enter' || e.key === ' ')) {
            e.preventDefault();
            setIsOpen(!isOpen);
          }
        }}
        className={`
          w-full px-4 py-2 text-left border rounded-lg
          flex items-center justify-between gap-2
          transition-colors duration-150 cursor-pointer
          ${error ? 'border-red-500 focus:ring-red-500' : 'border-neutral-300 focus:ring-orange-500'}
          ${disabled || isLoading ? 'bg-neutral-100 cursor-not-allowed' : 'bg-white hover:border-neutral-400'}
          focus:outline-none focus:ring-2
        `}
      >
        <span className="flex-1 truncate text-sm">
          {isLoading ? (
            <span className="text-neutral-400">Loading...</span>
          ) : value.length === 0 ? (
            <span className="text-neutral-400">{placeholder}</span>
          ) : (
            <span className="text-neutral-700">
              {value.length} selected
              {selectedLabels && value.length <= 2 && (
                <span className="text-neutral-500"> ({selectedLabels})</span>
              )}
            </span>
          )}
        </span>

        <div className="flex items-center gap-1">
          {value.length > 0 && !disabled && !isLoading && (
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                clearAll();
              }}
              className="p-1 hover:bg-neutral-100 rounded transition-colors"
              title="Clear selection"
            >
              <X className="h-4 w-4 text-neutral-500" />
            </button>
          )}
          <ChevronDown
            className={`h-4 w-4 text-neutral-500 transition-transform ${
              isOpen ? 'transform rotate-180' : ''
            }`}
          />
        </div>
      </div>

      {/* Dropdown Menu */}
      {isOpen && !disabled && !isLoading && (
        <div className="absolute z-50 mt-1 w-full bg-white border border-neutral-300 rounded-lg shadow-lg overflow-y-auto" style={{ maxHeight: '16rem' }}>
          {options.length === 0 ? (
            <div className="px-4 py-3 text-sm text-neutral-500 text-center">
              No options available
            </div>
          ) : (
            <div className="py-1">
              {options.map((option) => {
                const isSelected = value.includes(option.id);
                const isDisabled = option.disabled || (maxSelection ? value.length >= maxSelection && !isSelected : false);

                return (
                  <button
                    key={option.id}
                    type="button"
                    onClick={() => !isDisabled && toggleOption(option.id)}
                    disabled={isDisabled}
                    className={`
                      w-full px-4 py-2 text-left text-sm
                      flex items-center gap-3
                      transition-colors duration-150
                      ${isDisabled ? 'cursor-not-allowed opacity-50' : 'hover:bg-neutral-50'}
                      ${isSelected ? 'bg-orange-50' : ''}
                    `}
                  >
                    {/* Checkbox */}
                    <div
                      className={`
                        w-5 h-5 border-2 rounded flex items-center justify-center flex-shrink-0
                        ${isSelected ? 'bg-orange-500 border-orange-500' : 'border-neutral-300 bg-white'}
                      `}
                    >
                      {isSelected && <Check className="h-4 w-4 text-white" />}
                    </div>

                    {/* Label */}
                    <span className={isSelected ? 'font-medium text-neutral-900' : 'text-neutral-700'}>
                      {option.label}
                    </span>
                  </button>
                );
              })}
            </div>
          )}
        </div>
      )}

      {/* Helper Text */}
      {helperText && !error && (
        <p className="mt-1 text-sm text-neutral-500">{helperText}</p>
      )}

      {/* Error Message */}
      {error && errorMessage && (
        <p className="mt-1 text-sm text-red-600">{errorMessage}</p>
      )}

      {/* Max Selection Warning */}
      {maxSelection && value.length >= maxSelection && !error && (
        <p className="mt-1 text-sm text-amber-600">
          Maximum {maxSelection} {maxSelection === 1 ? 'selection' : 'selections'} reached
        </p>
      )}
    </div>
  );
}
