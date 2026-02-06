'use client';

import { use } from 'react';
import { useRouter } from 'next/navigation';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Badge } from '@/presentation/components/ui/Badge';
import { Button } from '@/presentation/components/ui/Button';
import { Calendar, ArrowLeft, ExternalLink } from 'lucide-react';
import { useNewsletterById } from '@/presentation/hooks/useNewsletters';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { NewsletterStatus } from '@/infrastructure/api/types/newsletters.types';
// Phase 6A.74 Part 10 Issue #1 Fix: Import enum helpers for string/number comparison
import { isNewsletterActive, isNewsletterSent } from '@/lib/enum-utils';

/**
 * Public Newsletter Details Page
 * Phase 6A.74 Parts 10 & 11
 * Route: /newsletters/[id]
 *
 * Layout order:
 * 1. Title and publish date
 * 2. Newsletter content (main message)
 * 3. Related event button (if linked)
 * 4. Footer newsletter subscription (link to footer, not separate CTA)
 */
export default function NewsletterDetailsPage({ params }: { params: Promise<{ id: string }> }) {
  const resolvedParams = use(params);
  const router = useRouter();
  const { user } = useAuthStore();
  const { data: newsletter, isLoading, error } = useNewsletterById(resolvedParams.id);

  const formatDate = (dateString: string | null) => {
    if (!dateString) return 'Not published';
    return new Date(dateString).toLocaleDateString('en-US', {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
      year: 'numeric',
    });
  };

  // Phase 6A.74 Part 10 Issue #1 Fix: Use enum helpers for string/number comparison
  const getStatusBadgeColor = (status: NewsletterStatus | string) => {
    if (isNewsletterActive(status)) return 'bg-green-100 text-green-800';
    if (isNewsletterSent(status)) return 'bg-blue-100 text-blue-800';
    return 'bg-gray-100 text-gray-800';
  };

  const getStatusLabel = (status: NewsletterStatus | string) => {
    if (isNewsletterActive(status)) return 'Active';
    if (isNewsletterSent(status)) return 'Sent';
    return 'Published';
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <Header />

      <main className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <Button
          variant="outline"
          onClick={() => router.push('/newsletters')}
          className="mb-6"
        >
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Newsletters
        </Button>

        {isLoading ? (
          <div className="text-center py-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-orange-600 mx-auto"></div>
            <p className="mt-4 text-gray-600">Loading newsletter...</p>
          </div>
        ) : error ? (
          <Card>
            <CardContent className="py-12 text-center">
              <p className="text-red-600 mb-4">Newsletter not found or not available.</p>
              <Button onClick={() => router.push('/newsletters')}>
                View All Newsletters
              </Button>
            </CardContent>
          </Card>
        ) : newsletter ? (
          <div className="space-y-6">
            {/* 1. Title and Publish Date */}
            <Card>
              <CardHeader>
                <div className="flex justify-between items-start mb-4">
                  <Badge className={getStatusBadgeColor(newsletter.status)}>
                    {getStatusLabel(newsletter.status)}
                  </Badge>
                  <div className="flex items-center text-sm text-gray-500">
                    <Calendar className="w-4 h-4 mr-1" />
                    {formatDate(newsletter.publishedAt)}
                  </div>
                </div>
                <CardTitle className="text-3xl">
                  {newsletter.title}
                </CardTitle>
              </CardHeader>
            </Card>

            {/* 2. Newsletter Content (Main Message) */}
            <Card>
              <CardContent className="pt-6">
                <div
                  className="prose prose-sm sm:prose max-w-none"
                  dangerouslySetInnerHTML={{ __html: newsletter.description }}
                />
              </CardContent>
            </Card>

            {/* 3. Related Event Button (if linked) */}
            {newsletter.eventId && (
              <Card className="bg-blue-50 border-blue-200">
                <CardContent className="py-6">
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="font-semibold text-gray-900 mb-1">
                        Related Event
                      </h3>
                      <p className="text-sm text-gray-600">
                        This newsletter is linked to an event
                      </p>
                    </div>
                    <Button
                      variant="outline"
                      onClick={() => router.push(`/events/${newsletter.eventId}`)}
                    >
                      View Event Details
                      <ExternalLink className="w-4 h-4 ml-2" />
                    </Button>
                  </div>
                </CardContent>
              </Card>
            )}
          </div>
        ) : null}
      </main>

      <Footer />
    </div>
  );
}
