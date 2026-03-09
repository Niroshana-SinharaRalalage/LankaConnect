'use client';

import * as React from 'react';
import { Sparkles } from 'lucide-react';

interface CommunityStats {
  totalUsers: number;
  totalEvents: number;
  totalBusinesses: number;
}

interface HeroLeftContentProps {
  stats: CommunityStats | undefined;
  statsLoading: boolean;
}

function formatCount(count: number): string {
  if (count >= 1000) {
    const k = Math.floor(count / 1000);
    const remainder = count % 1000;
    if (remainder >= 100) {
      return `${k}.${Math.floor(remainder / 100)}K+`;
    }
    return `${k}K+`;
  }
  return count.toString();
}

export function HeroLeftContent({ stats, statsLoading }: HeroLeftContentProps) {
  return (
    <div className="text-center lg:text-left">
      <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-white/20 backdrop-blur-sm border border-white/30 mb-6">
        <Sparkles className="h-4 w-4 text-white" />
        <span className="text-sm text-white">Connecting Sri Lankans Worldwide</span>
      </div>

      <h1 className="text-4xl md:text-5xl lg:text-6xl text-white mb-6">
        One Country,
        <br />
        <span className="text-white drop-shadow-lg">One Community</span>
      </h1>

      <p className="text-lg text-white/95 mb-8 max-w-xl mx-auto lg:mx-0">
        Join the largest Sri Lankan community platform. Discover events, connect
        with businesses, engage in discussions, and celebrate our rich culture
        together.
      </p>

      {statsLoading ? (
        <div className="grid grid-cols-3 gap-6 mt-8 pt-8 border-t border-white/20">
          {[...Array(3)].map((_, i) => (
            <div key={i}>
              <div className="h-9 w-20 bg-white/20 rounded animate-pulse mb-1"></div>
              <div className="h-4 w-16 bg-white/10 rounded animate-pulse"></div>
            </div>
          ))}
        </div>
      ) : stats && (stats.totalUsers > 0 || stats.totalEvents > 0 || stats.totalBusinesses > 0) ? (
        <div className="grid grid-cols-3 gap-6 mt-8 pt-8 border-t border-white/20">
          {stats.totalUsers > 0 && (
            <div>
              <div className="text-3xl text-white mb-1">{formatCount(stats.totalUsers)}</div>
              <div className="text-sm text-white/90">Members</div>
            </div>
          )}
          {stats.totalEvents > 0 && (
            <div>
              <div className="text-3xl text-white mb-1">{formatCount(stats.totalEvents)}</div>
              <div className="text-sm text-white/90">Events</div>
            </div>
          )}
          {stats.totalBusinesses > 0 && (
            <div>
              <div className="text-3xl text-white mb-1">{formatCount(stats.totalBusinesses)}</div>
              <div className="text-sm text-white/90">Businesses</div>
            </div>
          )}
        </div>
      ) : null}
    </div>
  );
}
