'use client';

import type { MarketplaceItem } from '@/config/marketplaceItems';

interface MarketplaceItemCardProps {
  item: MarketplaceItem;
  /** Compact mode for landing page sidebar */
  compact?: boolean;
}

/**
 * Displays a single marketplace item as a card linking to its Facebook post.
 * Supports compact mode (sidebar) and full mode (marketplace page).
 */
export function MarketplaceItemCard({ item, compact = false }: MarketplaceItemCardProps) {
  if (compact) {
    return (
      <a
        href={item.fbPostUrl}
        target="_blank"
        rel="noopener noreferrer"
        className="group block rounded-lg overflow-hidden hover:shadow-md transition-all border border-transparent hover:border-emerald-100"
      >
        <div className="relative aspect-[4/3] overflow-hidden bg-neutral-100">
          <img
            src={item.image}
            alt={item.productName}
            className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
          />
          <div className="absolute top-2 left-2">
            <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-medium bg-orange-500 text-white shadow-sm">
              Pre-Order
            </span>
          </div>
        </div>
        <div className="p-2.5 bg-white">
          <p className="text-xs font-medium text-neutral-900 leading-tight group-hover:text-emerald-700 transition-colors">
            {item.productName}
          </p>
        </div>
      </a>
    );
  }

  return (
    <a
      href={item.fbPostUrl}
      target="_blank"
      rel="noopener noreferrer"
      className="group block rounded-xl border border-neutral-200 bg-white overflow-hidden hover:shadow-lg hover:border-emerald-200 transition-all"
    >
      <div className="relative aspect-[4/3] overflow-hidden bg-neutral-100">
        <img
          src={item.image}
          alt={item.productName}
          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
        />
        <div className="absolute top-3 left-3">
          <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium bg-orange-500 text-white shadow-sm">
            Pre-Order
          </span>
        </div>
      </div>
      <div className="p-4">
        <h3 className="font-semibold text-neutral-900 group-hover:text-emerald-700 transition-colors">
          {item.productName}
        </h3>
        <div className="flex items-center justify-end mt-2">
          <span className="text-xs text-blue-600 flex items-center gap-1 group-hover:underline">
            <svg className="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 24 24">
              <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z" />
            </svg>
            View on Facebook
          </span>
        </div>
      </div>
    </a>
  );
}
