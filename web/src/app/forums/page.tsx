'use client';

import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import { Construction } from 'lucide-react';

/**
 * Phase 6A.64: Forums Coming Soon Page
 * Placeholder for community forums feature
 */
export default function ForumsPage() {
  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
        <Card className="text-center py-20">
          <CardContent>
            <Construction className="h-24 w-24 mx-auto mb-6 text-orange-600" />
            <h1 className="text-4xl font-bold mb-4" style={{ color: '#8B1538' }}>
              Community Forums Coming Soon
            </h1>
            <p className="text-xl text-neutral-600 mb-8">
              This feature is coming soon. Check back later!
            </p>
            <a
              href="/"
              className="inline-flex items-center px-6 py-3 rounded-lg text-white font-semibold hover:opacity-90 transition-opacity"
              style={{ background: '#FF7900' }}
            >
              Return to Home
            </a>
          </CardContent>
        </Card>
      </div>

      <Footer />
    </div>
  );
}
