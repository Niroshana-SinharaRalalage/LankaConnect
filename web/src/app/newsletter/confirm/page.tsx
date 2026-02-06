'use client';

import { Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import Link from 'next/link';
import { ArrowLeft, CheckCircle, XCircle } from 'lucide-react';
import { OfficialLogo } from '@/presentation/components/atoms/OfficialLogo';

/**
 * NewsletterConfirmationContent Component
 * Inner component that uses useSearchParams to get the status and message
 */
function NewsletterConfirmationContent() {
  const searchParams = useSearchParams();
  const status = searchParams.get('status');
  const message = searchParams.get('message');

  const isSuccess = status === 'success';
  const displayMessage = message
    ? decodeURIComponent(message)
    : isSuccess
      ? 'Your newsletter subscription has been confirmed successfully!'
      : 'Unable to confirm your newsletter subscription.';

  return (
    <Card className="w-full max-w-md">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          {isSuccess ? (
            <>
              <CheckCircle className="w-6 h-6 text-green-600" />
              Subscription Confirmed
            </>
          ) : (
            <>
              <XCircle className="w-6 h-6 text-red-600" />
              Confirmation Failed
            </>
          )}
        </CardTitle>
        <CardDescription>
          {isSuccess ? 'Thank you for subscribing to our newsletter!' : 'There was an issue confirming your subscription'}
        </CardDescription>
      </CardHeader>

      <CardContent className="space-y-4">
        {isSuccess ? (
          <div className="space-y-4">
            <div className="p-4 bg-green-50 border border-green-200 rounded-md">
              <div className="flex items-start">
                <svg
                  className="w-5 h-5 text-green-600 mt-0.5 mr-3 flex-shrink-0"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M5 13l4 4L19 7"
                  />
                </svg>
                <div>
                  <p className="text-sm font-medium text-green-800">{displayMessage}</p>
                  <p className="text-sm text-green-600 mt-2">
                    You'll start receiving updates about events and news in your selected metro areas.
                  </p>
                </div>
              </div>
            </div>

            <div className="p-4 bg-blue-50 border border-blue-200 rounded-md">
              <p className="text-sm text-blue-800">
                <strong>What's next?</strong>
              </p>
              <ul className="text-sm text-blue-600 mt-2 space-y-1 list-disc list-inside">
                <li>Check your inbox for our latest newsletter</li>
                <li>Explore upcoming events in your area</li>
                <li>Connect with the Sri Lankan community</li>
              </ul>
            </div>
          </div>
        ) : (
          <div className="p-4 bg-red-50 border border-red-200 rounded-md">
            <div className="flex items-start">
              <svg
                className="w-5 h-5 text-red-600 mt-0.5 mr-3 flex-shrink-0"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M6 18L18 6M6 6l12 12"
                />
              </svg>
              <div>
                <p className="text-sm font-medium text-red-800">{displayMessage}</p>
                <p className="text-sm text-red-600 mt-2">
                  The confirmation link may have expired or is invalid. Please try subscribing again or contact support if the issue persists.
                </p>
              </div>
            </div>
          </div>
        )}
      </CardContent>

      <CardFooter className="flex flex-col space-y-2">
        <Link href="/" className="w-full">
          <Button className="w-full">
            {isSuccess ? 'Go to Homepage' : 'Back to Homepage'}
          </Button>
        </Link>
        {!isSuccess && (
          <p className="text-sm text-center text-gray-600">
            Need help?{' '}
            <Link href="/help" className="text-primary hover:underline">
              Contact Support
            </Link>
          </p>
        )}
      </CardFooter>
    </Card>
  );
}

/**
 * NewsletterConfirmPage Component
 * Page for confirming newsletter subscription
 */
export default function NewsletterConfirmPage() {
  return (
    <div
      className="min-h-screen flex items-center justify-center p-5"
      style={{
        background: 'linear-gradient(to-r, #FF7900, #8B1538, #006400)'
      }}
    >
      {/* Split Panel Container */}
      <div className="relative z-10 w-full max-w-[1000px] grid grid-cols-1 md:grid-cols-2 bg-white rounded-[20px] overflow-hidden shadow-[0_20px_60px_rgba(0,0,0,0.3)]">
        {/* Left Panel - Branding */}
        <div className="hidden md:flex flex-col justify-center text-white px-10 py-[60px] relative overflow-hidden bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800">
          {/* Decorative Background Pattern */}
          <div className="absolute inset-0 opacity-10">
            <div
              className="absolute inset-0"
              style={{
                backgroundImage: `url("data:image/svg+xml,%3Csvg width='60' height='60' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
              }}
            ></div>
          </div>

          {/* Decorative gradient blobs */}
          <div className="absolute inset-0 overflow-hidden">
            <div className="absolute -top-24 -left-24 w-96 h-96 bg-orange-400/20 rounded-full blur-3xl"></div>
            <div className="absolute -bottom-24 -right-24 w-96 h-96 bg-emerald-400/20 rounded-full blur-3xl"></div>
            <div className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-rose-400/10 rounded-full blur-3xl"></div>
          </div>

          {/* Logo Section */}
          <div className="mb-8 relative z-10">
            <OfficialLogo size="md" textColor="text-white" subtitleColor="text-white/90" linkTo="/" />
          </div>

          {/* Welcome Text */}
          <div className="relative z-10">
            <h1 className="text-[1.75rem] font-semibold mb-4 drop-shadow-[2px_2px_4px_rgba(0,0,0,0.2)]">
              Newsletter Subscription
            </h1>
            <p className="text-base opacity-95 leading-relaxed mb-6">
              Stay connected with the Sri Lankan American community! Get updates about upcoming events, news, and activities in your area.
            </p>
          </div>

          {/* Features */}
          <div className="relative z-10">
            <div className="flex items-center mb-[15px] p-[15px] bg-white/10 backdrop-blur-[10px] rounded-[10px]">
              <div className="w-10 h-10 rounded-[10px] flex items-center justify-center text-[1.3rem] mr-[15px] flex-shrink-0" style={{ background: '#FFD700' }}>
                📧
              </div>
              <div>
                <strong className="block">Event Updates</strong>
                <div className="text-[0.9rem] opacity-90">Get notified about events near you</div>
              </div>
            </div>
            <div className="flex items-center mb-[15px] p-[15px] bg-white/10 backdrop-blur-[10px] rounded-[10px]">
              <div className="w-10 h-10 rounded-[10px] flex items-center justify-center text-[1.3rem] mr-[15px] flex-shrink-0" style={{ background: '#FFD700' }}>
                📰
              </div>
              <div>
                <strong className="block">Community News</strong>
                <div className="text-[0.9rem] opacity-90">Stay informed about community happenings</div>
              </div>
            </div>
            <div className="flex items-center mb-[15px] p-[15px] bg-white/10 backdrop-blur-[10px] rounded-[10px]">
              <div className="w-10 h-10 rounded-[10px] flex items-center justify-center text-[1.3rem] mr-[15px] flex-shrink-0" style={{ background: '#FFD700' }}>
                🎯
              </div>
              <div>
                <strong className="block">Personalized Content</strong>
                <div className="text-[0.9rem] opacity-90">Receive updates for your metro areas</div>
              </div>
            </div>
          </div>
        </div>

        {/* Right Panel - Confirmation Status */}
        <div className="flex flex-col justify-center px-[50px] py-[60px]" style={{ background: 'linear-gradient(to bottom, #ffffff 0%, #fef9f5 100%)' }}>
          {/* Back to Home Link */}
          <Link
            href="/"
            className="inline-flex items-center text-sm text-gray-600 hover:text-[#FF7900] transition-colors mb-4"
          >
            <ArrowLeft className="w-4 h-4 mr-1" />
            Back to Home
          </Link>

          {/* Mobile Logo */}
          <div className="mb-6 md:hidden text-center">
            <OfficialLogo size="sm" linkTo="/" />
          </div>

          <Suspense
            fallback={
              <Card className="w-full">
                <CardHeader>
                  <CardTitle>Loading...</CardTitle>
                  <CardDescription>Please wait while we confirm your subscription.</CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="flex justify-center py-8">
                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
                  </div>
                </CardContent>
              </Card>
            }
          >
            <NewsletterConfirmationContent />
          </Suspense>
        </div>
      </div>
    </div>
  );
}
