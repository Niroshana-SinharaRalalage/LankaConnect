'use client';

import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { HelpCircle } from 'lucide-react';

/**
 * Help Center Page - Placeholder
 * Phase 6A.73: Created to resolve 404 errors in footer navigation
 */
export default function HelpPage() {
  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Page Header */}
      <div className="bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 py-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center gap-4">
            <HelpCircle className="h-12 w-12 text-white" />
            <div>
              <h1 className="text-4xl font-bold text-white mb-2">
                Help Center
              </h1>
              <p className="text-lg text-white/90">
                Find answers and get support
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <Card>
          <CardHeader>
            <CardTitle>How Can We Help You?</CardTitle>
          </CardHeader>
          <CardContent className="prose max-w-none">
            <p className="text-neutral-600 mb-6">
              Welcome to the LankaConnect Help Center. We're here to assist you with any questions
              or issues you may have.
            </p>

            <h3 className="text-xl font-semibold text-neutral-900 mb-4">
              Coming Soon
            </h3>
            <p className="text-neutral-600">
              We're building a comprehensive help center with FAQs, tutorials, and guides.
              This resource will be available soon to help you make the most of LankaConnect.
            </p>

            <div className="mt-8 p-6 bg-neutral-50 rounded-lg">
              <p className="text-sm text-neutral-700">
                Need help right away? Contact our support team at{' '}
                <a href="mailto:support@lankaconnect.com" className="text-orange-600 hover:text-orange-700 font-medium">
                  support@lankaconnect.com
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
