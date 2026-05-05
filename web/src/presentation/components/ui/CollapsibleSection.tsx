'use client';

import { useState } from 'react';
import { ChevronDown } from 'lucide-react';

interface CollapsibleSectionProps {
  /** Title displayed in the collapsible header */
  title: string;
  /** Optional description below the title */
  description?: string;
  /** Optional icon component rendered before the title */
  icon?: React.ReactNode;
  /** Whether the section is expanded by default (uncontrolled mode only) */
  defaultOpen?: boolean;
  /** Controlled open state. When provided alongside onOpenChange, the parent owns state. */
  open?: boolean;
  /** Controlled change handler. Required when `open` is provided. */
  onOpenChange?: (open: boolean) => void;
  /** Section content */
  children: React.ReactNode;
  /** Optional className for the outer wrapper */
  className?: string;
  /** Optional badge/element displayed next to the title */
  badge?: React.ReactNode;
  /** Border color for the card (e.g. '#FF7900') */
  borderColor?: string;
  /** Optional preview rendered under the title ONLY when collapsed (e.g. "1 list • 2 items"). */
  summary?: React.ReactNode;
  /** Label for the expand pill when collapsed. Defaults to "Show details". */
  expandLabel?: string;
  /** Label for the expand pill when expanded. Defaults to "Hide details". */
  collapseLabel?: string;
}

/**
 * Reusable collapsible section with smooth CSS transition.
 * Uses CSS grid-template-rows for animation (preserves React state when collapsed).
 */
export function CollapsibleSection({
  title,
  description,
  icon,
  defaultOpen = true,
  open,
  onOpenChange,
  children,
  className = '',
  badge,
  borderColor,
  summary,
  expandLabel = 'Show details',
  collapseLabel = 'Hide details',
}: CollapsibleSectionProps) {
  const isControlled = open !== undefined;
  const [internalOpen, setInternalOpen] = useState(defaultOpen);
  const isOpen = isControlled ? open : internalOpen;

  const handleToggle = () => {
    const next = !isOpen;
    if (isControlled) {
      onOpenChange?.(next);
    } else {
      setInternalOpen(next);
      onOpenChange?.(next);
    }
  };

  return (
    <div
      className={`rounded-lg border-2 bg-white transition-shadow ${isOpen ? '' : 'hover:shadow-md'} ${className}`}
      style={borderColor ? { borderColor } : { borderColor: '#e5e7eb' }}
    >
      {/* Clickable Header */}
      <button
        type="button"
        onClick={handleToggle}
        aria-expanded={isOpen}
        className={`group flex w-full items-center justify-between gap-3 px-4 py-3 sm:px-6 sm:py-4 text-left cursor-pointer transition-colors rounded-t-lg ${
          isOpen
            ? 'hover:bg-neutral-50/50'
            : 'bg-neutral-50/40 hover:bg-neutral-100/70 rounded-b-lg'
        }`}
      >
        <div className="flex items-center gap-2 min-w-0 flex-1">
          {icon}
          <div className="min-w-0 flex-1">
            <h3 className="text-lg font-semibold text-neutral-900 truncate">{title}</h3>
            {description && (
              <p className="text-sm text-neutral-500 mt-0.5">{description}</p>
            )}
            {!isOpen && summary && (
              <div className="text-sm text-neutral-600 mt-1.5">{summary}</div>
            )}
          </div>
          {badge && <div className="ml-2 flex-shrink-0">{badge}</div>}
        </div>

        {/* Explicit affordance: neutral pill with text label + chevron.
            Kept neutral so it reads as a button without clashing with brand-colored cards. */}
        <span
          className="hidden sm:inline-flex items-center gap-1.5 flex-shrink-0 rounded-full border border-neutral-300 bg-white px-3 py-1 text-xs font-medium text-neutral-700 shadow-sm transition-colors group-hover:border-neutral-400 group-hover:bg-neutral-50"
          aria-hidden="true"
        >
          {isOpen ? collapseLabel : expandLabel}
          <ChevronDown
            className={`h-4 w-4 transition-transform duration-200 ${isOpen ? 'rotate-180' : ''}`}
          />
        </span>

        {/* Mobile fallback: chevron-only, bolder than the original dim gray so it reads as interactive */}
        <ChevronDown
          className={`sm:hidden h-5 w-5 flex-shrink-0 text-neutral-600 transition-transform duration-200 ${
            isOpen ? 'rotate-180' : ''
          }`}
          aria-hidden="true"
        />
      </button>

      {/* Animated Content — uses grid for smooth height transition */}
      <div
        className="grid transition-[grid-template-rows] duration-200 ease-in-out"
        style={{ gridTemplateRows: isOpen ? '1fr' : '0fr' }}
      >
        <div className="overflow-hidden">
          <div className="px-4 pb-4 sm:px-6 sm:pb-6">
            {children}
          </div>
        </div>
      </div>
    </div>
  );
}
