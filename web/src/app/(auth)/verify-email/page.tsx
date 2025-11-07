'use client';

import { Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import { EmailVerification } from '@/presentation/components/features/auth/EmailVerification';
import { Logo } from '@/presentation/components/atoms/Logo';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import Link from 'next/link';

/**
 * EmailVerificationContent Component
 * Inner component that uses useSearchParams to get the token
 */
function EmailVerificationContent() {
  const searchParams = useSearchParams();
  const token = searchParams.get('token');

  if (!token) {
    return (
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Invalid Verification Link</CardTitle>
          <CardDescription>
            The email verification link is invalid or missing the required token.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="p-4 bg-red-50 border border-red-200 rounded-md">
            <p className="text-sm text-red-800">
              This email verification link appears to be invalid. Please check your email for the correct link
              or contact support if you continue to have issues.
            </p>
          </div>
          <div className="flex flex-col space-y-2">
            <Link href="/login" className="text-primary hover:underline text-sm text-center">
              Back to Login
            </Link>
            <Link href="/contact" className="text-primary hover:underline text-sm text-center">
              Contact Support
            </Link>
          </div>
        </CardContent>
      </Card>
    );
  }

  return <EmailVerification token={token} />;
}

/**
 * VerifyEmailPage Component
 * Page for verifying email address with a token from email
 */
export default function VerifyEmailPage() {
  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-gradient-sri-lanka p-4">
      <div className="mb-8">
        <Logo size="lg" showText />
      </div>
      <Suspense
        fallback={
          <Card className="w-full max-w-md">
            <CardHeader>
              <CardTitle>Loading...</CardTitle>
            </CardHeader>
          </Card>
        }
      >
        <EmailVerificationContent />
      </Suspense>
    </div>
  );
}
