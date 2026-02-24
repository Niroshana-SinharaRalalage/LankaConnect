'use client';

import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import { ShoppingBag, ExternalLink } from 'lucide-react';
import { FacebookPageEmbed } from '@/presentation/components/widgets/FacebookPageEmbed';

const FB_PAGE_URL = 'https://www.facebook.com/LankaConnectUSA/';

/**
 * Marketplace Page - Shows Facebook page feed until native marketplace is built.
 * Links to the LankaConnectUSA Facebook page for marketplace listings.
 */
export default function MarketplacePage() {
  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
        {/* Page Header */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-emerald-50 mb-4">
            <ShoppingBag className="h-8 w-8 text-emerald-600" />
          </div>
          <h1 className="text-3xl font-bold text-neutral-900 mb-2">
            Marketplace
          </h1>
          <p className="text-neutral-600 max-w-lg mx-auto">
            Browse items from our community on Facebook. A full marketplace experience is coming soon!
          </p>
        </div>

        {/* Facebook Feed Embed */}
        <Card className="border-neutral-200 shadow-sm">
          <CardContent className="p-6">
            <FacebookPageEmbed
              pageUrl={FB_PAGE_URL}
              tabs="timeline"
              width={500}
              height={700}
              smallHeader={false}
              hideCover={false}
              showFacepile={true}
            />
          </CardContent>
        </Card>

        {/* CTA to Facebook Page */}
        <div className="text-center mt-6">
          <a
            href={FB_PAGE_URL}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 px-6 py-3 bg-blue-600 text-white font-semibold rounded-lg hover:bg-blue-700 transition-colors"
          >
            <ExternalLink className="h-5 w-5" />
            View All Listings on Facebook
          </a>
        </div>
      </div>

      <Footer />
    </div>
  );
}
