'use client';

import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Shield } from 'lucide-react';

/**
 * Safety Page - Placeholder
 * Phase 6A.73: Created to resolve 404 errors in footer navigation
 */
export default function SafetyPage() {
  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      {/* Page Header */}
      <div className="bg-gradient-to-r from-orange-600 via-rose-800 to-emerald-800 py-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center gap-4">
            <Shield className="h-12 w-12 text-white" />
            <div>
              <h1 className="text-4xl font-bold text-white mb-2">
                Safety & Security
              </h1>
              <p className="text-lg text-white/90">
                Your safety is our top priority
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <Card>
          <CardHeader>
            <CardTitle>Community Safety Guidelines</CardTitle>
          </CardHeader>
          <CardContent className="prose max-w-none">
            <p className="text-neutral-600 mb-6">
              LankaConnect is committed to providing a safe and secure platform for our community.
              Our safety guidelines help ensure positive experiences for all members.
            </p>

            <h3 className="text-xl font-semibold text-neutral-900 mb-4">
              Coming Soon
            </h3>
            <p className="text-neutral-600">
              This page is currently under development. We're working on comprehensive safety
              guidelines and resources for our community. Check back soon!
            </p>

            <div className="mt-8 p-6 bg-neutral-50 rounded-lg">
              <p className="text-sm text-neutral-700">
                If you have immediate safety concerns or need to report an issue, please contact us at{' '}
                <a href="mailto:safety@lankaconnect.com" className="text-orange-600 hover:text-orange-700 font-medium">
                  safety@lankaconnect.com
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
