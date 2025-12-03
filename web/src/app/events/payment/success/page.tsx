'use client';

import { use, useEffect, useState, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { CheckCircle2, ArrowRight, Home } from 'lucide-react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { useEventById } from '@/presentation/hooks/useEvents';

/**
 * Payment Success Callback Page
 * Session 23 (Phase 4): Stripe payment redirect page
 * Shown after successful payment completion on Stripe Checkout
 *
 * Flow:
 * 1. User completes payment on Stripe
 * 2. Stripe redirects to this page with eventId query param
 * 3. Webhook (Phase 2B) has already completed the registration
 * 4. This page shows confirmation and next steps
 */
function PaymentSuccessContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const eventId = searchParams?.get('eventId');
  const [isRedirecting, setIsRedirecting] = useState(false);

  // Fetch event details to show confirmation
  const { data: event, isLoading, error } = useEventById(eventId || undefined);

  useEffect(() => {
    // If no eventId, redirect to events list after 3 seconds
    if (!eventId && !isLoading) {
      const timer = setTimeout(() => {
        router.push('/events');
      }, 3000);
      return () => clearTimeout(timer);
    }
  }, [eventId, isLoading, router]);

  const handleViewEvent = () => {
    if (eventId) {
      setIsRedirecting(true);
      router.push(`/events/${eventId}`);
    }
  };

  const handleGoHome = () => {
    setIsRedirecting(true);
    router.push('/');
  };

  // Loading state
  if (isLoading) {
    return (
      <div className="min-h-screen flex flex-col bg-background">
        <Header />
        <main className="flex-1 container mx-auto px-4 py-8">
          <div className="max-w-2xl mx-auto text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto"></div>
            <p className="mt-4 text-muted-foreground">Loading...</p>
          </div>
        </main>
        <Footer />
      </div>
    );
  }

  return (
    <div className="min-h-screen flex flex-col bg-background">
      <Header />

      <main className="flex-1 container mx-auto px-4 py-8">
        <div className="max-w-2xl mx-auto">
          <Card className="border-2 border-green-500">
            <CardHeader className="text-center pb-4">
              <div className="flex justify-center mb-4">
                <CheckCircle2 className="h-16 w-16 text-green-500" />
              </div>
              <CardTitle className="text-3xl font-bold text-green-600">
                Payment Successful!
              </CardTitle>
              <CardDescription className="text-lg mt-2">
                Your event registration has been confirmed
              </CardDescription>
            </CardHeader>

            <CardContent className="space-y-6">
              {event && (
                <div className="bg-muted/50 rounded-lg p-6 space-y-3">
                  <h3 className="font-semibold text-lg">Registration Details</h3>
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Event:</span>
                      <span className="font-medium">{event.title}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Date:</span>
                      <span className="font-medium">
                        {new Date(event.startDate).toLocaleDateString('en-US', {
                          weekday: 'long',
                          year: 'numeric',
                          month: 'long',
                          day: 'numeric',
                        })}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-muted-foreground">Time:</span>
                      <span className="font-medium">
                        {new Date(event.startDate).toLocaleTimeString('en-US', {
                          hour: 'numeric',
                          minute: '2-digit',
                        })}
                      </span>
                    </div>
                    {event.ticketPriceAmount && (
                      <div className="flex justify-between border-t pt-2 mt-2">
                        <span className="text-muted-foreground font-semibold">Amount Paid:</span>
                        <span className="font-bold text-green-600">
                          {event.ticketPriceCurrency} {event.ticketPriceAmount.toFixed(2)}
                        </span>
                      </div>
                    )}
                  </div>
                </div>
              )}

              <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
                <h4 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">What's Next?</h4>
                <ul className="space-y-2 text-sm text-blue-800 dark:text-blue-200">
                  <li className="flex items-start">
                    <CheckCircle2 className="h-4 w-4 mr-2 mt-0.5 flex-shrink-0" />
                    <span>A confirmation email has been sent to your email address</span>
                  </li>
                  <li className="flex items-start">
                    <CheckCircle2 className="h-4 w-4 mr-2 mt-0.5 flex-shrink-0" />
                    <span>You can view your registration details in your account</span>
                  </li>
                  <li className="flex items-start">
                    <CheckCircle2 className="h-4 w-4 mr-2 mt-0.5 flex-shrink-0" />
                    <span>You'll receive event reminders before the event date</span>
                  </li>
                </ul>
              </div>
            </CardContent>

            <CardFooter className="flex flex-col sm:flex-row gap-3 justify-center">
              {eventId && (
                <Button
                  onClick={handleViewEvent}
                  disabled={isRedirecting}
                  className="w-full sm:w-auto"
                >
                  View Event Details
                  <ArrowRight className="ml-2 h-4 w-4" />
                </Button>
              )}
              <Button
                variant="outline"
                onClick={handleGoHome}
                disabled={isRedirecting}
                className="w-full sm:w-auto"
              >
                <Home className="mr-2 h-4 w-4" />
                Go to Homepage
              </Button>
            </CardFooter>
          </Card>
        </div>
      </main>

      <Footer />
    </div>
  );
}

export default function PaymentSuccessPage() {
  return (
    <Suspense fallback={
      <div className="min-h-screen flex flex-col bg-background">
        <Header />
        <main className="flex-1 container mx-auto px-4 py-8">
          <div className="max-w-2xl mx-auto text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto"></div>
            <p className="mt-4 text-muted-foreground">Loading...</p>
          </div>
        </main>
        <Footer />
      </div>
    }>
      <PaymentSuccessContent />
    </Suspense>
  );
}
