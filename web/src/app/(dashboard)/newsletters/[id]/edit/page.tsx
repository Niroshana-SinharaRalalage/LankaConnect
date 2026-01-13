'use client';

import { use } from 'react';
import { useRouter } from 'next/navigation';
import { NewsletterForm } from '@/presentation/components/features/newsletters/NewsletterForm';
import { ArrowLeft } from 'lucide-react';

/**
 * Edit Newsletter Page
 * Phase 6A.74 Part 6 - Route-based UI
 *
 * Features:
 * - Full-page newsletter editing
 * - Only available for Draft status newsletters
 * - Navigate to details page after save
 */
export default function EditNewsletterPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const router = useRouter();

  return (
    <div className="container mx-auto px-4 py-8 max-w-5xl">
      {/* Breadcrumb Navigation */}
      <div className="mb-6">
        <button
          onClick={() => router.push(`/dashboard/newsletters/${id}`)}
          className="flex items-center gap-2 text-gray-600 hover:text-gray-900 transition-colors"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Back to Newsletter</span>
        </button>
      </div>

      {/* Page Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-[#8B1538] mb-2">Edit Newsletter</h1>
        <p className="text-gray-600">
          Update your newsletter content and settings
        </p>
      </div>

      {/* Newsletter Form */}
      <NewsletterForm
        newsletterId={id}
        onSuccess={(newsletterId) => {
          // Navigate to details page after save
          router.push(`/dashboard/newsletters/${newsletterId || id}`);
        }}
        onCancel={() => router.push(`/dashboard/newsletters/${id}`)}
      />
    </div>
  );
}
