'use client';

import { Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import { ResetPasswordForm } from '@/presentation/components/features/auth/ResetPasswordForm';
import { Logo } from '@/presentation/components/atoms/Logo';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/presentation/components/ui/Card';
import Link from 'next/link';

/**
 * ResetPasswordContent Component
 * Inner component that uses useSearchParams to get the token
 */
function ResetPasswordContent() {
  const searchParams = useSearchParams();
  const token = searchParams.get('token');

  if (!token) {
    return (
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>Invalid Reset Link</CardTitle>
          <CardDescription>
            The password reset link is invalid or missing the required token.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="p-4 bg-red-50 border border-red-200 rounded-md">
            <p className="text-sm text-red-800">
              This password reset link appears to be invalid. Please request a new password reset.
            </p>
          </div>
          <div className="flex flex-col space-y-2">
            <Link href="/forgot-password" className="text-primary hover:underline text-sm text-center">
              Request New Password Reset
            </Link>
            <Link href="/login" className="text-primary hover:underline text-sm text-center">
              Back to Login
            </Link>
          </div>
        </CardContent>
      </Card>
    );
  }

  return <ResetPasswordForm token={token} />;
}

/**
 * ResetPasswordPage Component
 * Page for resetting password with a token from email
 */
export default function ResetPasswordPage() {
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
        <ResetPasswordContent />
      </Suspense>
    </div>
  );
}
