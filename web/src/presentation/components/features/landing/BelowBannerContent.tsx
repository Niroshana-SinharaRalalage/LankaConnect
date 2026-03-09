'use client';

import * as React from 'react';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Badge } from '@/presentation/components/ui/Badge';
import { ArrowRight, Clock, Store, MessageSquare, Newspaper, ShoppingBag } from 'lucide-react';
import { MarketplaceItemCard } from '@/presentation/components/widgets/MarketplaceItemCard';
import { MARKETPLACE_ITEMS } from '@/config/marketplaceItems';

interface Newsletter {
  id: string;
  title: string;
  description: string;
  eventId?: string | null;
  publishedAt?: string | null;
  createdAt: string;
}

interface BelowBannerContentProps {
  newsletters: Newsletter[] | undefined;
  newslettersLoading: boolean;
}

function getNewsletterExcerpt(html: string, maxLength: number = 100): string {
  const text = html.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
  return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
}

function getRelativeTime(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffHours < 1) return 'Just now';
  if (diffHours < 24) return `${diffHours}h ago`;
  if (diffDays < 7) return `${diffDays}d ago`;
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

export function BelowBannerContent({ newsletters, newslettersLoading }: BelowBannerContentProps) {
  return (
    <section className="py-16 bg-neutral-50">
      <div className="max-w-7xl mx-auto px-6 lg:px-8">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <div className="lg:col-span-2 space-y-8">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
              {/* Forum Highlights */}
              <Card className="border-neutral-200 shadow-sm">
                <CardHeader className="flex flex-row items-center justify-between px-6 py-4 border-b border-neutral-100">
                  <CardTitle className="flex items-center gap-2 text-neutral-900 text-lg font-semibold">
                    <MessageSquare className="h-5 w-5 text-rose-600" />
                    Forum Highlights
                  </CardTitle>
                  <button className="text-rose-600 hover:text-rose-700">
                    <ArrowRight className="h-5 w-5" />
                  </button>
                </CardHeader>
                <CardContent className="p-6 space-y-4">
                  <div className="text-center py-6 text-neutral-500">
                    <MessageSquare className="h-10 w-10 mx-auto mb-2 text-neutral-300" />
                    <p className="text-sm">No forum discussions yet</p>
                    <p className="text-xs text-neutral-400 mt-1">Coming soon</p>
                  </div>
                </CardContent>
              </Card>

              {/* Latest News & Updates */}
              <Card className="border-neutral-200 shadow-sm">
                <CardHeader className="flex flex-row items-center justify-between px-6 py-4 border-b border-neutral-100">
                  <CardTitle className="flex items-center gap-2 text-neutral-900 text-lg font-semibold">
                    <Newspaper className="h-5 w-5 text-amber-600" />
                    Latest News & Updates
                  </CardTitle>
                  <a href="/newsletters" className="text-amber-600 hover:text-amber-700">
                    <ArrowRight className="h-5 w-5" />
                  </a>
                </CardHeader>
                <CardContent className="p-6 space-y-4">
                  {newslettersLoading ? (
                    <>
                      {[...Array(2)].map((_, i) => (
                        <div key={i} className="rounded-xl border border-neutral-200 bg-white p-4 animate-pulse">
                          <div className="h-5 w-20 bg-neutral-200 rounded mb-3"></div>
                          <div className="h-5 w-3/4 bg-neutral-200 rounded mb-2"></div>
                          <div className="h-4 w-full bg-neutral-100 rounded mb-2"></div>
                          <div className="h-3 w-16 bg-neutral-100 rounded"></div>
                        </div>
                      ))}
                    </>
                  ) : newsletters && newsletters.length > 0 ? (
                    newsletters.slice(0, 2).map((newsletter) => (
                      <div
                        key={newsletter.id}
                        onClick={() => (window.location.href = `/newsletters/${newsletter.id}`)}
                        className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-amber-200 transition-all hover:shadow-md bg-white p-4 cursor-pointer"
                      >
                        <Badge variant={newsletter.eventId ? 'community' : 'business'}>
                          {newsletter.eventId ? 'Event Update' : 'News'}
                        </Badge>
                        <h3 className="font-semibold text-neutral-900 mt-3 mb-2 leading-snug group-hover:text-amber-600 transition-colors line-clamp-2">
                          {newsletter.title}
                        </h3>
                        <p className="text-sm text-neutral-600 mb-2 line-clamp-2">
                          {getNewsletterExcerpt(newsletter.description)}
                        </p>
                        <div className="flex items-center gap-1 text-xs text-neutral-500">
                          <Clock className="h-3 w-3" />
                          <span>{getRelativeTime(newsletter.publishedAt || newsletter.createdAt)}</span>
                        </div>
                      </div>
                    ))
                  ) : (
                    <div className="text-center py-6 text-neutral-500">
                      <Newspaper className="h-10 w-10 mx-auto mb-2 text-neutral-300" />
                      <p className="text-sm">No news available</p>
                    </div>
                  )}
                </CardContent>
              </Card>
            </div>

            {/* Business Section */}
            <Card className="border-neutral-200 shadow-sm">
              <CardHeader className="flex flex-row items-center justify-between px-6 py-4 border-b border-neutral-100">
                <CardTitle className="flex items-center gap-2 text-neutral-900 text-lg font-semibold">
                  <Store className="h-5 w-5 text-emerald-600" />
                  Business
                </CardTitle>
                <button className="text-emerald-600 hover:text-emerald-700 font-semibold flex items-center gap-1 text-sm">
                  Browse All
                  <ArrowRight className="h-4 w-4" />
                </button>
              </CardHeader>
              <CardContent className="p-6">
                <div className="text-center py-6 text-neutral-500">
                  <Store className="h-10 w-10 mx-auto mb-2 text-neutral-300" />
                  <p className="text-sm">No businesses listed yet</p>
                  <p className="text-xs text-neutral-400 mt-1">Coming soon</p>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Right Sidebar - Marketplace */}
          <div>
            <Card className="border-neutral-200 shadow-sm">
              <CardHeader className="flex flex-row items-center justify-between px-6 py-4 border-b border-neutral-100">
                <CardTitle className="flex items-center gap-2 text-neutral-900 text-lg font-semibold">
                  <ShoppingBag className="h-5 w-5 text-emerald-600" />
                  Marketplace
                </CardTitle>
                <a href="/marketplace" className="text-emerald-600 hover:text-emerald-700">
                  <ArrowRight className="h-5 w-5" />
                </a>
              </CardHeader>
              <CardContent className="p-3">
                <div className="grid grid-cols-2 gap-2">
                  {MARKETPLACE_ITEMS.map((item) => (
                    <MarketplaceItemCard key={item.id} item={item} compact />
                  ))}
                </div>
                <a
                  href="/marketplace"
                  className="block text-center text-sm text-emerald-600 hover:text-emerald-700 font-medium py-3 mt-3 border-t border-neutral-100"
                >
                  View All Details & Order
                </a>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </section>
  );
}
