'use client';

import { useEffect } from 'react';
import { Button } from '@/presentation/components/ui/Button';

export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    // Log the error to console for debugging
    console.error('Application Error:', error);
  }, [error]);

  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-gray-50 p-4">
      <div className="max-w-md w-full bg-white rounded-lg shadow-lg p-6 space-y-4">
        <h2 className="text-2xl font-bold text-red-600">Something went wrong!</h2>
        <div className="space-y-2">
          <p className="text-sm text-gray-600">Error message:</p>
          <pre className="text-xs bg-gray-100 p-3 rounded overflow-auto max-h-40">
            {error.message}
          </pre>
          {error.stack && (
            <>
              <p className="text-sm text-gray-600 mt-4">Stack trace:</p>
              <pre className="text-xs bg-gray-100 p-3 rounded overflow-auto max-h-60">
                {error.stack}
              </pre>
            </>
          )}
        </div>
        <Button
          onClick={() => reset()}
          className="w-full"
        >
          Try again
        </Button>
      </div>
    </div>
  );
}
