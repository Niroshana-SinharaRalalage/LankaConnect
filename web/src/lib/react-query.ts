/**
 * React Query Configuration
 *
 * This file exports a factory function for creating QueryClient instances.
 * DO NOT add 'use client' directive - this is a utility module imported by both
 * Server and Client Components.
 *
 * CRITICAL NEXT.JS 16 + REACT 19 PATTERN:
 * - The QueryClient must be created using useState in the Providers component
 * - Module-level singletons break during SSR/hydration with React 19's automatic batching
 * - See: https://tanstack.com/query/latest/docs/framework/react/guides/nextjs
 */

import { QueryClient } from '@tanstack/react-query';

/**
 * Creates a new QueryClient instance with recommended defaults for Next.js 16 + React 19
 *
 * Configuration:
 * - staleTime: 60s (avoid immediate refetch on client after SSR)
 * - refetchOnWindowFocus: true (keep data fresh when user returns)
 * - retry: 1 (single retry on failure)
 *
 * @returns A new QueryClient instance
 */
export function makeQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        // With SSR, we usually want to set some default staleTime
        // above 0 to avoid refetching immediately on the client
        staleTime: 60 * 1000,

        // Refetch on window focus for data freshness
        refetchOnWindowFocus: true,

        // Only retry once on error (prevents infinite loops)
        retry: 1,
      },
    },
  });
}
