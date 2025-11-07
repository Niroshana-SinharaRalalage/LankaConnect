'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { authRepository } from '@/infrastructure/api/repositories/auth.repository';
import { Button } from '@/presentation/components/ui/Button';
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from '@/presentation/components/ui/Card';
import { ApiError } from '@/infrastructure/api/client/api-errors';

/**
 * EmailVerification Component
 * Component for verifying email address with a token
 */
export interface EmailVerificationProps {
  token: string;
}

type VerificationState = 'verifying' | 'success' | 'error';

export function EmailVerification({ token }: EmailVerificationProps) {
  const router = useRouter();
  const [state, setState] = useState<VerificationState>('verifying');
  const [message, setMessage] = useState<string>('');

  useEffect(() => {
    const verifyEmail = async () => {
      try {
        const response = await authRepository.verifyEmail(token);
        setState('success');
        setMessage(response.message || 'Your email has been verified successfully!');

        // Redirect to login after 3 seconds
        setTimeout(() => {
          router.push('/login');
        }, 3000);
      } catch (error) {
        setState('error');
        if (error instanceof ApiError) {
          setMessage(error.message);
        } else {
          setMessage('An unexpected error occurred while verifying your email.');
        }
      }
    };

    if (token) {
      verifyEmail();
    } else {
      setState('error');
      setMessage('Invalid verification link. No token provided.');
    }
  }, [token, router]);

  return (
    <Card className="w-full max-w-md">
      <CardHeader>
        <CardTitle>Email Verification</CardTitle>
        <CardDescription>
          {state === 'verifying' && 'Verifying your email address...'}
          {state === 'success' && 'Email verified successfully!'}
          {state === 'error' && 'Verification failed'}
        </CardDescription>
      </CardHeader>

      <CardContent className="space-y-4">
        {/* Verifying State */}
        {state === 'verifying' && (
          <div className="flex flex-col items-center justify-center py-8">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mb-4"></div>
            <p className="text-sm text-gray-600">Please wait while we verify your email...</p>
          </div>
        )}

        {/* Success State */}
        {state === 'success' && (
          <div className="space-y-4">
            <div className="p-4 bg-green-50 border border-green-200 rounded-md">
              <div className="flex items-start">
                <svg
                  className="w-5 h-5 text-green-600 mt-0.5 mr-3"
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
                  <p className="text-sm font-medium text-green-800">{message}</p>
                  <p className="text-sm text-green-600 mt-2">
                    You will be redirected to the login page shortly...
                  </p>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Error State */}
        {state === 'error' && (
          <div className="space-y-4">
            <div className="p-4 bg-red-50 border border-red-200 rounded-md">
              <div className="flex items-start">
                <svg
                  className="w-5 h-5 text-red-600 mt-0.5 mr-3"
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
                  <p className="text-sm font-medium text-red-800">{message}</p>
                  <p className="text-sm text-red-600 mt-2">
                    The verification link may have expired or is invalid.
                  </p>
                </div>
              </div>
            </div>
          </div>
        )}
      </CardContent>

      <CardFooter className="flex flex-col space-y-2">
        {state === 'success' && (
          <Button
            onClick={() => router.push('/login')}
            className="w-full"
          >
            Go to Login
          </Button>
        )}

        {state === 'error' && (
          <div className="w-full space-y-2">
            <Link href="/login" className="block w-full">
              <Button className="w-full">
                Back to Login
              </Button>
            </Link>
            <p className="text-sm text-center text-gray-600">
              Need help?{' '}
              <Link href="/contact" className="text-primary hover:underline">
                Contact Support
              </Link>
            </p>
          </div>
        )}
      </CardFooter>
    </Card>
  );
}
