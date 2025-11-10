'use client';

import { FeedType } from '@/domain/models/FeedItem';
import { FEED_TYPE_LABELS } from '@/domain/constants/feedTypes.constants';
import {
  Calendar,
  Building2,
  MessageSquare,
  Globe,
  LayoutGrid
} from 'lucide-react';

/**
 * Tab value type - includes 'all' option
 */
export type FeedTabValue = FeedType | 'all';

/**
 * Tab configuration interface
 */
export interface FeedTab {
  /** Unique tab identifier */
  value: FeedTabValue;
  /** Display label */
  label: string;
  /** Lucide icon component */
  icon: React.ReactNode;
  /** Optional badge count */
  count?: number;
}

/**
 * Props for FeedTabs component
 */
export interface FeedTabsProps {
  /** Currently active tab */
  activeTab: FeedTabValue;
  /** Callback when tab changes */
  onTabChange: (tab: FeedTabValue) => void;
  /** Optional badge counts for each tab */
  counts?: Partial<Record<FeedTabValue, number>>;
  /** Optional className for custom styling */
  className?: string;
}

/**
 * FeedTabs Component
 *
 * Tab navigation for filtering feed content by type.
 * Provides horizontal scrolling on mobile and responsive layout.
 *
 * @example
 * ```tsx
 * <FeedTabs
 *   activeTab={activeTab}
 *   onTabChange={setActiveTab}
 *   counts={{ all: 42, event: 12, business: 8 }}
 * />
 * ```
 */
export function FeedTabs({
  activeTab,
  onTabChange,
  counts = {},
  className = ''
}: FeedTabsProps) {

  /**
   * Tab configuration with icons
   */
  const tabs: FeedTab[] = [
    {
      value: 'all',
      label: 'All Posts',
      icon: <LayoutGrid className="w-4 h-4" />,
      count: counts.all
    },
    {
      value: 'event',
      label: FEED_TYPE_LABELS.event.plural,
      icon: <Calendar className="w-4 h-4" />,
      count: counts.event
    },
    {
      value: 'business',
      label: FEED_TYPE_LABELS.business.plural,
      icon: <Building2 className="w-4 h-4" />,
      count: counts.business
    },
    {
      value: 'culture',
      label: FEED_TYPE_LABELS.culture.singular,
      icon: <Globe className="w-4 h-4" />,
      count: counts.culture
    },
    {
      value: 'forum',
      label: FEED_TYPE_LABELS.forum.plural,
      icon: <MessageSquare className="w-4 h-4" />,
      count: counts.forum
    }
  ];

  /**
   * Render individual tab button
   */
  const renderTab = (tab: FeedTab) => {
    const isActive = activeTab === tab.value;

    return (
      <button
        key={tab.value}
        onClick={() => onTabChange(tab.value)}
        className={`
          flex items-center gap-2 px-4 py-2 font-medium transition-all duration-200
          whitespace-nowrap border-b-2 flex-shrink-0
          ${isActive
            ? 'text-[#FF7900] border-[#FF7900]'
            : 'text-gray-600 border-transparent hover:text-[#FF7900] hover:border-gray-300'
          }
        `}
        aria-current={isActive ? 'page' : undefined}
      >
        <span className={isActive ? 'text-[#FF7900]' : 'text-gray-500'}>
          {tab.icon}
        </span>
        <span>{tab.label}</span>
        {tab.count !== undefined && tab.count > 0 && (
          <span
            className={`
              inline-flex items-center justify-center min-w-[20px] h-5 px-1.5 rounded-full text-xs font-semibold
              ${isActive
                ? 'bg-[#FF7900] text-white'
                : 'bg-gray-200 text-gray-700'
              }
            `}
          >
            {tab.count > 99 ? '99+' : tab.count}
          </span>
        )}
      </button>
    );
  };

  return (
    <nav
      className={`border-b border-gray-200 bg-white ${className}`}
      aria-label="Feed filter tabs"
    >
      <div className="overflow-x-auto scrollbar-hide">
        <div className="flex min-w-max">
          {tabs.map(renderTab)}
        </div>
      </div>

      {/* Custom scrollbar hiding styles */}
      <style jsx>{`
        .scrollbar-hide {
          -ms-overflow-style: none;
          scrollbar-width: none;
        }
        .scrollbar-hide::-webkit-scrollbar {
          display: none;
        }
      `}</style>
    </nav>
  );
}
