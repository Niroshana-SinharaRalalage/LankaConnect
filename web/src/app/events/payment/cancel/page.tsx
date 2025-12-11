'use client';

import { useEffect, useState, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { XCircle, ArrowLeft, Home, RefreshCw } from 'lucide-react';
import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { useEventById } from '@/presentation/hooks/useEvents';

/**
 * Payment Cancel Callback Page
 * Session 23 (Phase 4): Stripe payment redirect page
 * Shown when user cancels payment or payment fails on Stripe Checkout
 *
 * Flow:
 * 1. User cancels payment on Stripe or payment fails
 * 2. Stripe redirects to this page with eventId query param
 * 3. Registration remains in PaymentPending status (will be cleaned up later)
 * 4. This page shows cancellation message and options to retry or return
 */
function PaymentCancelContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const eventId = searchParams?.get('eventId');
  const [isRedirecting, setIsRedirecting] = useState(false);

  // Fetch event details to show context
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

  const handleRetryPayment = () => {
    if (eventId) {
      setIsRedirecting(true);
      // Go back to event detail page to retry registration
      router.push(`/events/${eventId}`);
    }
  };

  const handleBrowseEvents = () => {
    setIsRedirecting(true);
    router.push('/events');
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
          <Card className="border-2 border-orange-500">
            <CardHeader className="text-center pb-4">
              <div className="flex justify-center mb-4">
                <XCircle className="h-16 w-16 text-orange-500" />
              </div>
              <CardTitle className="text-3xl font-bold text-orange-600">
                Payment Cancelled
              </CardTitle>
              <CardDescription className="text-lg mt-2">
                Your event registration was not completed
              </CardDescription>
            </CardHeader>

            <CardContent className="space-y-6">
              {event && (
                <div className="bg-muted/50 rounded-lg p-6 space-y-3">
                  <h3 className="font-semibold text-lg">Event Details</h3>
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
                        <span className="text-muted-foreground">Ticket Price:</span>
                        <span className="font-semibold">
                          {event.ticketPriceCurrency} {event.ticketPriceAmount.toFixed(2)}
                        </span>
                      </div>
                    )}
                  </div>
                </div>
              )}

              <div className="bg-orange-50 dark:bg-orange-900/20 border border-orange-200 dark:border-orange-800 rounded-lg p-4">
                <h4 className="font-semibold text-orange-900 dark:text-orange-100 mb-2">What Happened?</h4>
                <p className="text-sm text-orange-800 dark:text-orange-200 mb-3">
                  Your payment was cancelled or could not be processed. No charges were made to your account.
                </p>
                <h4 className="font-semibold text-orange-900 dark:text-orange-100 mb-2 mt-4">What Can You Do?</h4>
                <ul className="space-y-2 text-sm text-orange-800 dark:text-orange-200">
                  <li className="flex items-start">
                    <RefreshCw className="h-4 w-4 mr-2 mt-0.5 flex-shrink-0" />
                    <span>Try registering again with a different payment method</span>
                  </li>
                  <li className="flex items-start">
                    <ArrowLeft className="h-4 w-4 mr-2 mt-0.5 flex-shrink-0" />
                    <span>Browse other events you might be interested in</span>
                  </li>
                  <li className="flex items-start">
                    <Home className="h-4 w-4 mr-2 mt-0.5 flex-shrink-0" />
                    <span>Contact support if you continue to experience issues</span>
                  </li>
                </ul>
              </div>

              {event && event.capacity && event.currentRegistrations >= event.capacity && (
                <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
                  <p className="text-sm text-red-800 dark:text-red-200 font-semibold">
                    ⚠️ Note: This event is currently at full capacity. Your spot may no longer be available.
                  </p>
                </div>
              )}
            </CardContent>

            <CardFooter className="flex flex-col sm:flex-row gap-3 justify-center">
              {eventId && (
                <Button
                  onClick={handleRetryPayment}
                  disabled={isRedirecting}
                  className="w-full sm:w-auto"
                >
                  <RefreshCw className="mr-2 h-4 w-4" />
                  Try Again
                </Button>
              )}
              <Button
                variant="outline"
                onClick={handleBrowseEvents}
                disabled={isRedirecting}
                className="w-full sm:w-auto"
              >
                <ArrowLeft className="mr-2 h-4 w-4" />
                Browse Events
              </Button>
              <Button
                variant="ghost"
                onClick={handleGoHome}
                disabled={isRedirecting}
                className="w-full sm:w-auto"
              >
                <Home className="mr-2 h-4 w-4" />
                Go Home
              </Button>
            </CardFooter>
          </Card>
        </div>
      </main>

      <Footer />
    </div>
  );
}

export default function PaymentCancelPage() {
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
      <PaymentCancelContent />
    </Suspense>
  );
}
