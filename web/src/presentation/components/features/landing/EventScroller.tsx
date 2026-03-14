'use client';

import * as React from 'react';
import { Calendar } from 'lucide-react';
import { EventDto } from '@/infrastructure/api/types/events.types';

interface EventScrollerProps {
  events: EventDto[];
  isLoading: boolean;
}

function EventCard({ event, index }: { event: EventDto; index: number }) {
  const primaryImage = event.images?.find((img) => img.isPrimary) || event.images?.[0];
  const hasImage = primaryImage?.imageUrl;
  const gradients = [
    'from-orange-600 via-rose-600 to-amber-500',
    'from-emerald-600 via-teal-600 to-cyan-500',
    'from-rose-600 via-pink-600 to-purple-500',
    'from-indigo-600 via-blue-600 to-cyan-500',
  ];

  return (
    <div
      className="group relative min-w-[170px] w-[170px] h-32 rounded-2xl shadow-lg hover:shadow-2xl transition-all hover:-translate-y-1 hover:scale-[1.02] cursor-pointer overflow-hidden ring-2 ring-white/40 hover:ring-white/70 flex-shrink-0"
      onClick={() => (window.location.href = `/events/${event.id}`)}
    >
      {hasImage ? (
        <img
          src={primaryImage.imageUrl}
          alt={event.title}
          className="absolute inset-0 w-full h-full object-cover transition-transform duration-500 group-hover:scale-110"
        />
      ) : (
        <div
          className={`absolute inset-0 bg-gradient-to-br ${gradients[index % 4]} flex items-center justify-center`}
        >
          <span className="text-5xl opacity-30">🎉</span>
        </div>
      )}
      <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-transparent" />
      <div className="absolute inset-0 p-3 flex flex-col justify-end">
        <h3 className="text-white font-bold text-sm leading-tight line-clamp-2 drop-shadow-lg mb-1">
          {event.title}
        </h3>
        <div className="flex items-center gap-1.5 text-white/90 text-xs">
          <Calendar className="h-3 w-3" />
          <span>
            {new Date(event.startDate).toLocaleDateString('en-US', {
              month: 'short',
              day: 'numeric',
            })}{' '}
            at{' '}
            {new Date(event.startDate).toLocaleTimeString('en-US', {
              hour: 'numeric',
              minute: '2-digit',
            })}
          </span>
        </div>
      </div>
    </div>
  );
}

export function EventScroller({ events, isLoading }: EventScrollerProps) {
  return (
    <div className="mt-1">
      <div className="mb-2">
        <span className="text-white/70 text-xs font-medium uppercase tracking-wider">
          Featured Events
        </span>
      </div>

      {isLoading ? (
        <div className="flex gap-3 overflow-hidden">
          {[...Array(4)].map((_, i) => (
            <div
              key={i}
              className="min-w-[170px] h-32 rounded-2xl animate-pulse bg-white/10 ring-2 ring-white/20"
            />
          ))}
        </div>
      ) : events.length === 0 ? (
        <div className="flex items-center justify-center h-32 rounded-2xl bg-white/10 ring-2 ring-white/20">
          <p className="text-white/60 text-sm">No events to display</p>
        </div>
      ) : (
        <div className="scroller-container overflow-hidden relative">
          {/* Left fade edge */}
          <div className="absolute left-0 top-0 bottom-0 w-12 bg-gradient-to-r from-black/40 to-transparent z-10 pointer-events-none rounded-l-2xl" />
          {/* Right fade edge */}
          <div className="absolute right-0 top-0 bottom-0 w-12 bg-gradient-to-l from-black/40 to-transparent z-10 pointer-events-none rounded-r-2xl" />

          <div className="flex gap-3 animate-marquee-fast">
            {[...events, ...events, ...events].map((event, index) => (
              <EventCard key={`${event.id}-${index}`} event={event} index={index % events.length} />
            ))}
          </div>
        </div>
      )}

      <style jsx>{`
        @keyframes marquee-fast {
          0% {
            transform: translateX(0);
          }
          100% {
            transform: translateX(-33.33%);
          }
        }
        .animate-marquee-fast {
          animation: marquee-fast 8s linear infinite;
        }
        .animate-marquee-fast:hover {
          animation-play-state: paused;
        }
      `}</style>
    </div>
  );
}
