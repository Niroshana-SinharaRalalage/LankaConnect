'use client';

import { Header } from '@/presentation/components/layout/Header';
import Footer from '@/presentation/components/layout/Footer';
import { Card, CardContent } from '@/presentation/components/ui/Card';
import { MessageCircle, Clock } from 'lucide-react';
import { MarketplaceItemCard } from '@/presentation/components/widgets/MarketplaceItemCard';
import { MARKETPLACE_ITEMS, MARKETPLACE_COMMON_DESCRIPTION } from '@/config/marketplaceItems';

const FB_PAGE_URL = 'https://www.facebook.com/LankaConnectUSA/';

/**
 * Marketplace Page - Displays batik family kit items from Facebook posts.
 * Each item card links to its specific Facebook post for ordering.
 */
export default function MarketplacePage() {
  const desc = MARKETPLACE_COMMON_DESCRIPTION;

  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-50 to-white">
      <Header />

      <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
        {/* Common Description Banner */}
        <Card className="border-emerald-200 bg-emerald-50/50 shadow-sm mb-8">
          <CardContent className="p-6">
            <h2 className="text-xl font-bold text-neutral-900 mb-1">{desc.title}</h2>

            <div className="flex flex-wrap items-center gap-4 text-sm text-neutral-600 mt-3 mb-4">
              <span className="inline-flex items-center gap-1.5 text-orange-600 font-medium">
                <Clock className="h-4 w-4" />
                Pre-orders closing {desc.preOrderDeadline}
              </span>
              <span className="inline-flex items-center gap-1.5">
                <MessageCircle className="h-4 w-4 text-green-600" />
                WhatsApp {desc.contact.whatsapp}
              </span>
              <a
                href={FB_PAGE_URL}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-1.5 text-blue-600 hover:underline"
              >
                <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
                  <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z" />
                </svg>
                Message on Facebook
              </a>
            </div>

            {desc.details.map((detail, i) => (
              <p key={i} className="text-sm text-neutral-600">{detail}</p>
            ))}
          </CardContent>
        </Card>

        {/* Items Grid */}
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4 sm:gap-6">
          {MARKETPLACE_ITEMS.map((item) => (
            <MarketplaceItemCard key={item.id} item={item} />
          ))}
        </div>

        {/* Bottom CTA */}
        <div className="text-center mt-10">
          <p className="text-sm text-neutral-500 mb-3">
            Want to order? Message us on Facebook or WhatsApp {desc.contact.whatsapp}
          </p>
          <a
            href={FB_PAGE_URL}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 px-6 py-3 bg-blue-600 text-white font-semibold rounded-lg hover:bg-blue-700 transition-colors"
          >
            <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
              <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z" />
            </svg>
            Visit LankaConnect on Facebook
          </a>
        </div>
      </div>

      <Footer />
    </div>
  );
}
