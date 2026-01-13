'use client';

import { useRouter, useSearchParams } from 'next/navigation';
import { NewsletterForm } from '@/presentation/components/features/newsletters/NewsletterForm';
import { ArrowLeft } from 'lucide-react';

/**
 * Create Newsletter Page
 * Phase 6A.74 Part 6 - Route-based UI
 *
 * Features:
 * - Full-page newsletter creation (no modal)
 * - Pre-populate event if eventId query param exists
 * - Navigate to details page after creation
 * - Breadcrumb navigation
 */
export default function CreateNewsletterPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const eventId = searchParams.get('eventId');

  return (
    <div className="container mx-auto px-4 py-8 max-w-5xl">
      {/* Breadcrumb Navigation */}
      <div className="mb-6">
        <button
          onClick={() => router.back()}
          className="flex items-center gap-2 text-gray-600 hover:text-gray-900 transition-colors"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Back</span>
        </button>
      </div>

      {/* Page Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-[#8B1538] mb-2">Create Newsletter</h1>
        <p className="text-gray-600">
          Create and send newsletters to your email groups and subscribers
        </p>
      </div>

      {/* Newsletter Form */}
      <NewsletterForm
        initialEventId={eventId || undefined}
        onSuccess={(id) => {
          // Navigate to details page after creation
          router.push(`/dashboard/newsletters/${id}`);
        }}
        onCancel={() => router.back()}
      />
    </div>
  );
}
