'use client';

import { useState } from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'react-hot-toast';
import { makeQueryClient } from '@/lib/react-query';
import { AuthProvider } from '@/presentation/providers/AuthProvider';
import { NumberInputScrollPrevention } from '@/presentation/components/utils/NumberInputScrollPrevention';

/**
 * Providers Component
 *
 * CRITICAL PATTERN for Next.js 16 + React 19 + React Query v5:
 *
 * We MUST use useState with an initialization function to create the QueryClient.
 * This ensures the client is created ONCE on component mount and survives React 19's
 * automatic batching during hydration.
 *
 * DO NOT use module-level singletons or external functions - this breaks during
 * SSR/hydration and causes queries to never execute.
 *
 * See: https://tanstack.com/query/latest/docs/framework/react/guides/nextjs
 *
 * AuthProvider: Sets up global 401 error handling for automatic logout on token expiration
 */
export function Providers({ children }: { children: React.ReactNode }) {
  // ✅ CORRECT: Use useState with initialization function
  // This creates the QueryClient ONCE on client mount
  // and ensures it survives React 19's automatic batching during hydration
  const [queryClient] = useState(() => makeQueryClient());

  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        {children}
        {/* Phase 6A.X Issue #27/#28: Global Toaster for newsletter and other notifications */}
        <Toaster
          position="top-center"
          toastOptions={{
            duration: 5000,
            success: { duration: 6000 },
            error: { duration: 6000 },
          }}
        />
        {/* Phase 6A.X Issue #20: Prevent scroll-to-change on number inputs globally */}
        <NumberInputScrollPrevention />
      </AuthProvider>
    </QueryClientProvider>
  );
}
