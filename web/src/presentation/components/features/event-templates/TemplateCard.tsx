'use client';

import React from 'react';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/presentation/components/ui/Card';
import { cn } from '@/presentation/lib/utils';
import type { EventTemplateDto } from '@/infrastructure/api/types/event-template.types';
import { Sparkles } from 'lucide-react';

/**
 * Phase 6A.8: Event Template System
 * Props for TemplateCard component
 */
export interface TemplateCardProps {
  /** Event template data from API */
  template: EventTemplateDto;
  /** Optional click handler for template selection */
  onSelect?: (template: EventTemplateDto) => void;
  /** Optional className for custom styling */
  className?: string;
  /** Whether the template is currently selected */
  isSelected?: boolean;
}

/**
 * Category color mapping using Sri Lankan flag colors
 */
const CATEGORY_COLORS: Record<string, { bg: string; text: string; border: string }> = {
  Religious: { bg: 'bg-[#8B1538]/10', text: 'text-[#8B1538]', border: 'border-[#8B1538]/20' },
  Cultural: { bg: 'bg-[#FF7900]/10', text: 'text-[#FF7900]', border: 'border-[#FF7900]/20' },
  Community: { bg: 'bg-green-500/10', text: 'text-green-700', border: 'border-green-500/20' },
  Educational: { bg: 'bg-blue-500/10', text: 'text-blue-700', border: 'border-blue-500/20' },
  Social: { bg: 'bg-purple-500/10', text: 'text-purple-700', border: 'border-purple-500/20' },
  Business: { bg: 'bg-gray-500/10', text: 'text-gray-700', border: 'border-gray-500/20' },
  Charity: { bg: 'bg-pink-500/10', text: 'text-pink-700', border: 'border-pink-500/20' },
  Entertainment: { bg: 'bg-indigo-500/10', text: 'text-indigo-700', border: 'border-indigo-500/20' },
};

/**
 * TemplateCard Component
 *
 * Displays an event template with SVG thumbnail preview, name, description, and category.
 * Follows UI/UX best practices with hover states, proper spacing, and accessibility.
 *
 * Features:
 * - SVG thumbnail rendering from backend
 * - Category badge with color coding
 * - Hover effects for better interactivity
 * - Click to select template
 * - Selected state visual indicator
 * - Responsive design
 *
 * @example
 * ```tsx
 * <TemplateCard
 *   template={vesakkTemplate}
 *   onSelect={(template) => handleTemplateSelect(template)}
 *   isSelected={selectedTemplate?.id === template.id}
 * />
 * ```
 */
export function TemplateCard({
  template,
  onSelect,
  className = '',
  isSelected = false
}: TemplateCardProps) {
  const categoryColors = CATEGORY_COLORS[template.category] || CATEGORY_COLORS.Community;

  const handleClick = () => {
    if (onSelect) {
      onSelect(template);
    }
  };

  return (
    <Card
      className={cn(
        'cursor-pointer transition-all duration-200 hover:shadow-lg hover:scale-105',
        isSelected && 'ring-2 ring-[#FF7900] shadow-lg',
        className
      )}
      onClick={handleClick}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          handleClick();
        }
      }}
      aria-label={`Select ${template.name} template`}
    >
      {/* SVG Thumbnail Preview */}
      <CardHeader className="p-0">
        <div
          className={cn(
            'w-full aspect-square rounded-t-lg flex items-center justify-center',
            'bg-gradient-to-br from-gray-50 to-gray-100',
            isSelected && 'from-[#FF7900]/5 to-[#8B1538]/5'
          )}
          dangerouslySetInnerHTML={{ __html: template.thumbnailSvg }}
          aria-hidden="true"
        />
      </CardHeader>

      <CardContent className="p-4">
        {/* Category Badge */}
        <div className="mb-2">
          <span
            className={cn(
              'inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium border',
              categoryColors.bg,
              categoryColors.text,
              categoryColors.border
            )}
          >
            {template.category}
          </span>
        </div>

        {/* Template Name */}
        <CardTitle className="text-lg mb-2 line-clamp-2">
          {template.name}
        </CardTitle>

        {/* Template Description */}
        <CardDescription className="line-clamp-3 text-sm">
          {template.description}
        </CardDescription>
      </CardContent>

      {/* Selection Indicator */}
      {isSelected && (
        <CardFooter className="p-4 pt-0">
          <div className="flex items-center gap-2 text-sm font-medium text-[#FF7900]">
            <Sparkles className="w-4 h-4" />
            <span>Selected</span>
          </div>
        </CardFooter>
      )}
    </Card>
  );
}

/**
 * TemplateCardSkeleton Component
 *
 * Loading skeleton for TemplateCard while data is being fetched
 */
export function TemplateCardSkeleton() {
  return (
    <Card className="overflow-hidden">
      <div className="w-full aspect-square bg-gray-200 animate-pulse" />
      <CardContent className="p-4">
        <div className="mb-2">
          <div className="h-5 w-20 bg-gray-200 rounded-full animate-pulse" />
        </div>
        <div className="h-6 w-3/4 bg-gray-200 rounded animate-pulse mb-2" />
        <div className="space-y-2">
          <div className="h-4 w-full bg-gray-200 rounded animate-pulse" />
          <div className="h-4 w-5/6 bg-gray-200 rounded animate-pulse" />
          <div className="h-4 w-4/6 bg-gray-200 rounded animate-pulse" />
        </div>
      </CardContent>
    </Card>
  );
}
