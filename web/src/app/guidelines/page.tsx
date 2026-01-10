'use client';

import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { FileText } from 'lucide-react';

/**
 * Community Guidelines Page - Placeholder
 * Phase 6A.73: Created to resolve 404 errors in footer navigation
 */
export default function GuidelinesPage() {
  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Page Header */}
      <div className="bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 py-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center gap-4">
            <FileText className="h-12 w-12 text-white" />
            <div>
              <h1 className="text-4xl font-bold text-white mb-2">
                Community Guidelines
              </h1>
              <p className="text-lg text-white/90">
                Building a respectful and inclusive community
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <Card>
          <CardHeader>
            <CardTitle>Our Community Standards</CardTitle>
          </CardHeader>
          <CardContent className="prose max-w-none">
            <p className="text-neutral-600 mb-6">
              LankaConnect is built on respect, inclusivity, and authentic connections.
              Our guidelines help maintain a positive environment for all community members.
            </p>

            <h3 className="text-xl font-semibold text-neutral-900 mb-4">
              Coming Soon
            </h3>
            <p className="text-neutral-600">
              We're developing comprehensive community guidelines that reflect our values
              and commitment to creating a welcoming space for everyone. Stay tuned!
            </p>

            <div className="mt-8 p-6 bg-neutral-50 rounded-lg">
              <p className="text-sm text-neutral-700">
                Questions about our guidelines? Reach out to us at{' '}
                <a href="mailto:community@lankaconnect.com" className="text-orange-600 hover:text-orange-700 font-medium">
                  community@lankaconnect.com
                </a>
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      <Footer />
    </div>
  );
}
