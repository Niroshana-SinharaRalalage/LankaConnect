import * as React from 'react';
import { Users, MessageCircle, Calendar } from 'lucide-react';
import { StatCard, TrendIndicator } from '@/presentation/components/ui/StatCard';
import { cn } from '@/presentation/lib/utils';

export interface CommunityStatsData {
  activeUsers: number;
  activeUsersTrend?: TrendIndicator;
  recentPosts: number;
  recentPostsTrend?: TrendIndicator;
  upcomingEvents: number;
  upcomingEventsTrend?: TrendIndicator;
}

export interface CommunityStatsProps extends React.HTMLAttributes<HTMLDivElement> {
  stats: CommunityStatsData;
  isLoading?: boolean;
  error?: string;
}

const formatNumber = (num: number): string => {
  return num.toLocaleString('en-US');
};

const LoadingSkeleton = () => (
  <div className="space-y-4">
    <h2 className="text-lg font-semibold">Community Statistics</h2>
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
      {[1, 2, 3].map((i) => (
        <div
          key={i}
          className="h-32 bg-gray-200 rounded-lg animate-pulse"
        />
      ))}
    </div>
    <p className="text-sm text-muted-foreground text-center">Loading statistics...</p>
  </div>
);

/**
 * CommunityStats Component
 * Displays real-time community statistics using StatCard components
 * Shows active users, recent posts, and upcoming events with trends
 */
export const CommunityStats = React.forwardRef<HTMLDivElement, CommunityStatsProps>(
  ({ className, stats, isLoading, error, ...props }, ref) => {
    if (isLoading) {
      return (
        <div ref={ref} className={cn('', className)} {...props}>
          <LoadingSkeleton />
        </div>
      );
    }

    if (error) {
      return (
        <div ref={ref} className={cn('', className)} {...props}>
          <div className="space-y-4">
            <h2 className="text-lg font-semibold">Community Statistics</h2>
            <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
              <p className="text-sm text-red-600">{error}</p>
            </div>
          </div>
        </div>
      );
    }

    return (
      <div ref={ref} className={cn('', className)} {...props}>
        <div
          className="rounded-xl overflow-hidden"
          style={{
            background: 'white',
            boxShadow: '0 4px 6px rgba(0, 0, 0, 0.05)'
          }}
        >
          {/* Widget Header */}
          <div
            className="px-5 py-4 font-semibold border-b"
            style={{
              background: 'linear-gradient(135deg, rgba(255,121,0,0.1) 0%, rgba(139,21,56,0.1) 100%)',
              borderBottom: '1px solid #e2e8f0',
              color: '#8B1538'
            }}
          >
            📊 Community Stats
          </div>

          {/* Widget Content */}
          <div className="p-5">
            <div className="space-y-4">
              {/* Active Today */}
              <div className="flex justify-between items-center">
                <span style={{ color: '#718096' }}>Active Today</span>
                <strong style={{ color: '#2d3748' }}>{formatNumber(stats.activeUsers)}</strong>
              </div>

              {/* Events This Week */}
              <div className="flex justify-between items-center">
                <span style={{ color: '#718096' }}>Events This Week</span>
                <strong style={{ color: '#2d3748' }}>{formatNumber(stats.recentPosts)}</strong>
              </div>

              {/* New Businesses */}
              <div className="flex justify-between items-center">
                <span style={{ color: '#718096' }}>New Businesses</span>
                <strong style={{ color: '#2d3748' }}>{formatNumber(23)}</strong>
              </div>

              {/* Forum Discussions */}
              <div className="flex justify-between items-center">
                <span style={{ color: '#718096' }}>Forum Discussions</span>
                <strong style={{ color: '#2d3748' }}>{formatNumber(stats.upcomingEvents)}</strong>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }
);

CommunityStats.displayName = 'CommunityStats';
