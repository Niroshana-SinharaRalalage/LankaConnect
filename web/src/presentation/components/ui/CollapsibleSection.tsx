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
  /** Whether the section is expanded by default */
  defaultOpen?: boolean;
  /** Section content */
  children: React.ReactNode;
  /** Optional className for the outer wrapper */
  className?: string;
  /** Optional badge/element displayed next to the title */
  badge?: React.ReactNode;
  /** Border color for the card (e.g. '#FF7900') */
  borderColor?: string;
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
  children,
  className = '',
  badge,
  borderColor,
}: CollapsibleSectionProps) {
  const [isOpen, setIsOpen] = useState(defaultOpen);

  return (
    <div
      className={`rounded-lg border-2 bg-white ${className}`}
      style={borderColor ? { borderColor } : { borderColor: '#e5e7eb' }}
    >
      {/* Clickable Header */}
      <button
        type="button"
        onClick={() => setIsOpen((prev) => !prev)}
        aria-expanded={isOpen}
        className="flex w-full items-center justify-between px-4 py-3 sm:px-6 sm:py-4 text-left cursor-pointer hover:bg-neutral-50/50 transition-colors rounded-t-lg"
      >
        <div className="flex items-center gap-2 min-w-0">
          {icon}
          <div className="min-w-0">
            <h3 className="text-lg font-semibold text-neutral-900 truncate">{title}</h3>
            {description && (
              <p className="text-sm text-neutral-500 mt-0.5">{description}</p>
            )}
          </div>
          {badge && <div className="ml-2 flex-shrink-0">{badge}</div>}
        </div>
        <ChevronDown
          className={`h-5 w-5 text-neutral-400 flex-shrink-0 ml-2 transition-transform duration-200 ${
            isOpen ? 'rotate-180' : ''
          }`}
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
