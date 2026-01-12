'use client';

import { Newspaper, ArrowRight, Calendar, Clock } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { usePublishedNewsletters } from '@/presentation/hooks/useNewsletters';

/**
 * LandingPageNewsletters Component
 * Phase 6A.74 Part 5B: Public newsletter display on homepage
 *
 * Displays the 3 most recent Active newsletters on the landing page
 * - Title
 * - Excerpt (first 200 chars of HTML content, stripped)
 * - Published date
 * - "Read More" button → /newsletters/[id]
 * - "View All Newsletters" button → /newsletters
 *
 * Features:
 * - Responsive card layout
 * - Loading skeleton
 * - Empty state (hidden if no newsletters)
 * - Brand colors (#FF7900, #8B1538)
 */
export function LandingPageNewsletters() {
  const { data: newsletters, isLoading, error } = usePublishedNewsletters();

  // Helper: Strip HTML tags and get first 200 characters
  const getExcerpt = (html: string, maxLength: number = 200): string => {
    const text = html.replace(/<[^>]*>/g, ''); // Remove HTML tags
    return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
  };

  // Helper: Format date
  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  // Don't render section if no newsletters (after loading)
  if (!isLoading && (!newsletters || newsletters.length === 0)) {
    return null;
  }

  // Show loading skeleton
  if (isLoading) {
    return (
      <section className="py-16 bg-white">
        <div className="container mx-auto px-6 lg:px-8">
          {/* Header */}
          <div className="flex items-center justify-between mb-8">
            <div className="flex items-center gap-3">
              <Newspaper className="h-8 w-8" style={{ color: '#FF7900' }} />
              <h2 className="text-3xl font-bold" style={{ color: '#8B1538' }}>
                Latest News & Updates
              </h2>
            </div>
            <div className="h-10 w-40 bg-neutral-200 rounded-lg animate-pulse"></div>
          </div>

          {/* Loading Skeleton */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {[...Array(3)].map((_, i) => (
              <div
                key={i}
                className="relative overflow-hidden rounded-xl border border-neutral-200 bg-white p-6 animate-pulse"
              >
                <div className="h-6 w-3/4 bg-neutral-200 rounded mb-4"></div>
                <div className="space-y-2 mb-4">
                  <div className="h-4 w-full bg-neutral-100 rounded"></div>
                  <div className="h-4 w-full bg-neutral-100 rounded"></div>
                  <div className="h-4 w-2/3 bg-neutral-100 rounded"></div>
                </div>
                <div className="flex items-center justify-between mt-6">
                  <div className="h-4 w-24 bg-neutral-200 rounded"></div>
                  <div className="h-9 w-24 bg-neutral-200 rounded"></div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>
    );
  }

  // Show error state (optional, could also hide section)
  if (error) {
    return null;
  }

  // Limit to 3 most recent newsletters
  const recentNewsletters = newsletters?.slice(0, 3) || [];

  return (
    <section className="py-16 bg-white">
      <div className="container mx-auto px-6 lg:px-8">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center gap-3">
            <Newspaper className="h-8 w-8" style={{ color: '#FF7900' }} />
            <h2 className="text-3xl font-bold" style={{ color: '#8B1538' }}>
              Latest News & Updates
            </h2>
          </div>
          <a
            href="/newsletters"
            className="inline-flex items-center gap-2 px-6 py-3 rounded-lg font-semibold text-sm transition-all hover:shadow-md"
            style={{
              backgroundColor: '#FF7900',
              color: 'white',
            }}
          >
            View All
            <ArrowRight className="h-4 w-4" />
          </a>
        </div>

        {/* Newsletter Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {recentNewsletters.map((newsletter) => (
            <Card
              key={newsletter.id}
              className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-orange-200 transition-all hover:shadow-lg cursor-pointer"
              onClick={() => (window.location.href = `/newsletters/${newsletter.id}`)}
            >
              <CardContent className="p-6">
                {/* Title */}
                <h3
                  className="text-xl font-bold mb-3 line-clamp-2 group-hover:text-orange-600 transition-colors"
                  style={{ color: '#1F2937' }}
                >
                  {newsletter.title}
                </h3>

                {/* Excerpt */}
                <p className="text-sm text-neutral-600 mb-4 line-clamp-3">
                  {getExcerpt(newsletter.description)}
                </p>

                {/* Footer: Date + Read More */}
                <div className="flex items-center justify-between mt-6 pt-4 border-t border-neutral-100">
                  <div className="flex items-center gap-2 text-xs text-neutral-500">
                    <Calendar className="h-3.5 w-3.5" />
                    <span>{formatDate(newsletter.publishedAt!)}</span>
                  </div>
                  <button
                    className="inline-flex items-center gap-1 text-sm font-semibold group-hover:gap-2 transition-all"
                    style={{ color: '#FF7900' }}
                  >
                    Read More
                    <ArrowRight className="h-4 w-4" />
                  </button>
                </div>

                {/* Optional: Event Linkage Badge */}
                {newsletter.eventId && newsletter.eventTitle && (
                  <div className="absolute top-4 right-4">
                    <div className="px-3 py-1 rounded-full text-xs font-semibold bg-blue-100 text-blue-700">
                      Event: {newsletter.eventTitle}
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    </section>
  );
}
