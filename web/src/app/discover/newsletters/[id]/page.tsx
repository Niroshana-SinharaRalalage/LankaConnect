'use client';

import { use } from 'react';
import { useRouter } from 'next/navigation';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Badge } from '@/presentation/components/ui/Badge';
import { Button } from '@/presentation/components/ui/Button';
import { Calendar, MapPin, Mail, ArrowLeft, ExternalLink } from 'lucide-react';
import { useNewsletterById } from '@/presentation/hooks/useNewsletters';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { NewsletterStatus } from '@/infrastructure/api/types/newsletters.types';

/**
 * Public Newsletter Details Page
 * Phase 6A.74 Parts 10 & 11
 * Route: /discover/newsletters/[id]
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

  const getStatusBadgeColor = (status: NewsletterStatus) => {
    if (status === NewsletterStatus.Active) return 'bg-green-100 text-green-800';
    if (status === NewsletterStatus.Sent) return 'bg-blue-100 text-blue-800';
    return 'bg-gray-100 text-gray-800';
  };

  const getStatusLabel = (status: NewsletterStatus) => {
    if (status === NewsletterStatus.Active) return 'Active';
    if (status === NewsletterStatus.Sent) return 'Sent';
    return 'Active';
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <Header />

      <main className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <Button
          variant="outline"
          onClick={() => router.push('/discover/newsletters')}
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
              <Button onClick={() => router.push('/discover/newsletters')}>
                View All Newsletters
              </Button>
            </CardContent>
          </Card>
        ) : newsletter ? (
          <div className="space-y-6">
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

            {newsletter.eventId && (
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg flex items-center">
                    <Calendar className="w-5 h-5 mr-2 text-orange-600" />
                    Related Event
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <p className="text-gray-600 mb-3">
                    This newsletter is linked to an event
                  </p>
                  <Button
                    variant="outline"
                    onClick={() => router.push(`/events/${newsletter.eventId}`)}
                  >
                    View Event Details
                    <ExternalLink className="w-4 h-4 ml-2" />
                  </Button>
                </CardContent>
              </Card>
            )}

            <Card>
              <CardHeader>
                <CardTitle>Newsletter Content</CardTitle>
              </CardHeader>
              <CardContent>
                <div
                  className="prose prose-sm sm:prose max-w-none"
                  dangerouslySetInnerHTML={{ __html: newsletter.description }}
                />
              </CardContent>
            </Card>

            {(newsletter.targetAllLocations || newsletter.metroAreas.length > 0) && (
              <Card>
                <CardHeader>
                  <CardTitle className="text-lg flex items-center">
                    <MapPin className="w-5 h-5 mr-2 text-orange-600" />
                    Location Targeting
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  {newsletter.targetAllLocations ? (
                    <p className="text-gray-600">All Locations</p>
                  ) : (
                    <div className="flex flex-wrap gap-2">
                      {newsletter.metroAreas.map((metro) => (
                        <Badge key={metro.id}>
                          {metro.name}, {metro.state}
                        </Badge>
                      ))}
                    </div>
                  )}
                </CardContent>
              </Card>
            )}

            <Card className="bg-orange-50 border-orange-200">
              <CardContent className="py-6">
                <div className="text-center">
                  <Mail className="w-12 h-12 text-orange-600 mx-auto mb-3" />
                  <h3 className="text-lg font-semibold text-gray-900 mb-2">
                    Stay Updated
                  </h3>
                  <p className="text-gray-600 mb-4">
                    Subscribe to our newsletter to receive updates like this directly in your inbox
                  </p>
                  <Button
                    onClick={() => router.push(user ? '/dashboard/profile' : '/login')}
                    className="bg-orange-600 hover:bg-orange-700"
                  >
                    {user ? 'Manage Newsletter Preferences' : 'Sign In to Subscribe'}
                  </Button>
                </div>
              </CardContent>
            </Card>
          </div>
        ) : null}
      </main>

      <Footer />
    </div>
  );
}
