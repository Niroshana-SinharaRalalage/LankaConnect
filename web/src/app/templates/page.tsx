'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useEventTemplates } from '@/presentation/hooks/useEventTemplates';
import { TemplateCard, TemplateCardSkeleton } from '@/presentation/components/features/event-templates/TemplateCard';
import { Button } from '@/presentation/components/ui/Button';
import { Logo } from '@/presentation/components/atoms/Logo';
import Footer from '@/presentation/components/layout/Footer';
import { EventCategory } from '@/infrastructure/api/types/events.types';
import type { EventTemplateDto } from '@/infrastructure/api/types/event-template.types';
import { ArrowLeft, Sparkles } from 'lucide-react';

/**
 * Phase 6A.8: Event Template System
 * Template Gallery Page - Browse and select event templates
 *
 * Features:
 * - Browse all available event templates
 * - Filter templates by category
 * - Responsive grid layout (1 col mobile, 2 cols tablet, 3-4 cols desktop)
 * - Loading skeletons for better UX
 * - Error handling with user-friendly messages
 * - Template selection and navigation to event creation
 * - Sri Lankan brand colors (#8B1538, #FF7900)
 * - Accessibility features (keyboard navigation, ARIA labels)
 */

// Category filter options with labels
const CATEGORY_FILTERS: Array<{ value: EventCategory | 'all'; label: string }> = [
  { value: 'all', label: 'All Templates' },
  { value: EventCategory.Religious, label: 'Religious' },
  { value: EventCategory.Cultural, label: 'Cultural' },
  { value: EventCategory.Community, label: 'Community' },
  { value: EventCategory.Educational, label: 'Educational' },
  { value: EventCategory.Social, label: 'Social' },
  { value: EventCategory.Business, label: 'Business' },
  { value: EventCategory.Charity, label: 'Charity' },
  { value: EventCategory.Entertainment, label: 'Entertainment' },
];

export default function TemplatesPage() {
  const router = useRouter();
  const [selectedCategory, setSelectedCategory] = useState<EventCategory | 'all'>('all');
  const [selectedTemplate, setSelectedTemplate] = useState<EventTemplateDto | null>(null);

  // Fetch templates with optional category filter
  const { data: templates, isLoading, error } = useEventTemplates(
    selectedCategory === 'all' ? undefined : { category: selectedCategory }
  );

  const handleTemplateSelect = (template: EventTemplateDto) => {
    setSelectedTemplate(template);
    // Navigate to event creation form with template pre-population
    // This will be implemented in Phase 6A.8.14
    router.push(`/events/create?templateId=${template.id}`);
  };

  const handleBackToDashboard = () => {
    router.push('/dashboard');
  };

  return (
    <div className="min-h-screen" style={{ background: '#f7fafc' }}>
      {/* Header - Matching Dashboard Page */}
      <header
        className="bg-white sticky top-0 z-40"
        style={{
          background: 'rgba(255, 255, 255, 0.95)',
          backdropFilter: 'blur(10px)',
          boxShadow: '0 2px 20px rgba(0,0,0,0.1)',
        }}
      >
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
          <div className="flex items-center justify-between">
            {/* Logo */}
            <div className="flex items-center">
              <Logo size="md" showText={false} />
              <span className="ml-3 text-2xl font-bold text-[#8B1538]">LankaConnect</span>
            </div>

            {/* Back to Dashboard Button */}
            <Button
              onClick={handleBackToDashboard}
              variant="outline"
              className="flex items-center gap-2"
              style={{
                borderColor: '#FF7900',
                color: '#FF7900',
              }}
            >
              <ArrowLeft className="w-4 h-4" />
              <span className="hidden sm:inline">Back to Dashboard</span>
              <span className="sm:hidden">Back</span>
            </Button>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Page Header */}
        <div className="mb-8">
          <div className="flex items-center gap-3 mb-3">
            <Sparkles className="w-8 h-8 text-[#FF7900]" />
            <h1 className="text-3xl sm:text-4xl font-bold text-[#8B1538]">Event Templates</h1>
          </div>
          <p className="text-lg text-gray-600 max-w-3xl">
            Choose from our pre-designed event templates to quickly create your next event. Each
            template comes with suggested settings that you can customize to your needs.
          </p>
        </div>

        {/* Category Filter Tabs */}
        <div className="mb-8 overflow-x-auto">
          <div className="flex gap-2 min-w-max pb-2">
            {CATEGORY_FILTERS.map((filter) => (
              <button
                key={filter.value}
                onClick={() => setSelectedCategory(filter.value)}
                className={`px-4 py-2 rounded-lg font-medium transition-all duration-200 whitespace-nowrap ${
                  selectedCategory === filter.value
                    ? 'text-white shadow-md'
                    : 'bg-white text-gray-700 hover:bg-gray-50 border border-gray-200'
                }`}
                style={
                  selectedCategory === filter.value
                    ? {
                        background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)',
                      }
                    : undefined
                }
                aria-pressed={selectedCategory === filter.value}
                aria-label={`Filter by ${filter.label}`}
              >
                {filter.label}
              </button>
            ))}
          </div>
        </div>

        {/* Templates Grid */}
        {isLoading && (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
            {[...Array(8)].map((_, i) => (
              <TemplateCardSkeleton key={i} />
            ))}
          </div>
        )}

        {error && (
          <div
            className="rounded-lg p-8 text-center"
            style={{
              background: 'white',
              boxShadow: '0 4px 6px rgba(0, 0, 0, 0.05)',
            }}
          >
            <p className="text-red-600 font-medium mb-4">
              Failed to load templates. Please try again later.
            </p>
            <Button
              onClick={() => window.location.reload()}
              style={{
                background: '#FF7900',
                color: 'white',
              }}
            >
              Retry
            </Button>
          </div>
        )}

        {!isLoading && !error && templates && templates.length === 0 && (
          <div
            className="rounded-lg p-12 text-center"
            style={{
              background: 'white',
              boxShadow: '0 4px 6px rgba(0, 0, 0, 0.05)',
            }}
          >
            <Sparkles className="w-16 h-16 text-gray-400 mx-auto mb-4" />
            <h2 className="text-xl font-semibold text-gray-700 mb-2">No Templates Found</h2>
            <p className="text-gray-600 mb-6">
              {selectedCategory === 'all'
                ? 'No templates are available at the moment.'
                : `No templates found in the ${CATEGORY_FILTERS.find((f) => f.value === selectedCategory)?.label} category.`}
            </p>
            {selectedCategory !== 'all' && (
              <Button
                onClick={() => setSelectedCategory('all')}
                variant="outline"
                style={{
                  borderColor: '#FF7900',
                  color: '#FF7900',
                }}
              >
                View All Templates
              </Button>
            )}
          </div>
        )}

        {!isLoading && !error && templates && templates.length > 0 && (
          <>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
              {templates.map((template) => (
                <TemplateCard
                  key={template.id}
                  template={template}
                  onSelect={handleTemplateSelect}
                  isSelected={selectedTemplate?.id === template.id}
                />
              ))}
            </div>

            {/* Templates Count */}
            <div className="mt-8 text-center text-gray-600">
              <p>
                Showing <span className="font-semibold text-[#8B1538]">{templates.length}</span>{' '}
                {templates.length === 1 ? 'template' : 'templates'}
                {selectedCategory !== 'all' &&
                  ` in ${CATEGORY_FILTERS.find((f) => f.value === selectedCategory)?.label}`}
              </p>
            </div>
          </>
        )}

        {/* Help Section */}
        <div
          className="mt-12 rounded-lg p-6"
          style={{
            background: 'linear-gradient(135deg, #FFF5E6 0%, #FFE8CC 100%)',
            border: '1px solid #FFD4A3',
          }}
        >
          <h3 className="text-lg font-semibold text-[#8B1538] mb-2">Need a custom template?</h3>
          <p className="text-gray-700 mb-4">
            Can't find the perfect template? You can always create an event from scratch or contact
            our support team to request a custom template for your specific needs.
          </p>
          <div className="flex flex-wrap gap-3">
            <Button
              onClick={() => router.push('/events/create')}
              style={{
                background: '#FF7900',
                color: 'white',
              }}
            >
              Create from Scratch
            </Button>
            <Button
              onClick={() => router.push('/support')}
              variant="outline"
              style={{
                borderColor: '#8B1538',
                color: '#8B1538',
              }}
            >
              Contact Support
            </Button>
          </div>
        </div>
      </main>

      {/* Footer */}
      <Footer />
    </div>
  );
}
