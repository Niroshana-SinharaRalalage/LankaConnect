import * as React from 'react';
import { Calendar } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { cn } from '@/presentation/lib/utils';

export interface CulturalEvent {
  id: string;
  name: string;
  date: string;
  category: 'national' | 'religious' | 'cultural' | 'holiday';
}

export interface CulturalCalendarProps extends React.HTMLAttributes<HTMLDivElement> {
  events: CulturalEvent[];
}

const categoryStyles: Record<CulturalEvent['category'], string> = {
  national: 'bg-blue-100 text-blue-800',
  religious: 'bg-purple-100 text-purple-800',
  cultural: 'bg-green-100 text-green-800',
  holiday: 'bg-orange-100 text-orange-800',
};

const formatDate = (dateString: string): { day: string; month: string } => {
  // Parse date as UTC to avoid timezone offset issues
  const [year, month, day] = dateString.split('-').map(Number);
  const date = new Date(Date.UTC(year, month - 1, day));
  return {
    day: day.toString(),
    month: date.toLocaleDateString('en-US', { month: 'short', timeZone: 'UTC' }).toUpperCase()
  };
};

/**
 * CulturalCalendar Component
 * Displays upcoming cultural and religious events in a card format
 * Shows event name, date, and category with color-coded badges
 */
export const CulturalCalendar = React.forwardRef<HTMLDivElement, CulturalCalendarProps>(
  ({ className, events, ...props }, ref) => {
    // Sort events by date
    const sortedEvents = React.useMemo(() => {
      return [...events].sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
    }, [events]);

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
            🗓️ Cultural Calendar
          </div>

          {/* Widget Content */}
          <div className="p-5">
            {sortedEvents.length === 0 ? (
              <p className="text-sm text-center py-4" style={{ color: '#718096' }}>
                No upcoming events at this time
              </p>
            ) : (
              <div className="space-y-0" role="list">
                {sortedEvents.map((event, index) => {
                  const dateInfo = formatDate(event.date);
                  return (
                    <div
                      key={event.id}
                      className="flex items-center py-3"
                      style={{
                        borderBottom: index === sortedEvents.length - 1 ? 'none' : '1px solid #f1f5f9'
                      }}
                      role="listitem"
                    >
                      {/* Event Date Box */}
                      <div
                        className="rounded-lg text-center flex-shrink-0 mr-4"
                        style={{
                          background: 'linear-gradient(135deg, #FF7900 0%, #8B1538 100%)',
                          color: 'white',
                          padding: '0.5rem',
                          minWidth: '60px'
                        }}
                      >
                        <div style={{ fontSize: '1.25rem', fontWeight: 'bold', lineHeight: '1' }}>
                          {dateInfo.day}
                        </div>
                        <div style={{ fontSize: '0.75rem', opacity: 0.9 }}>
                          {dateInfo.month}
                        </div>
                      </div>

                      {/* Event Info */}
                      <div className="flex-1 min-w-0">
                        <h4
                          data-testid="event-name"
                          className="font-medium truncate"
                          style={{ fontSize: '0.9rem', marginBottom: '0.25rem', color: '#2d3748' }}
                        >
                          {event.name}
                        </h4>
                        <p style={{ fontSize: '0.8rem', color: '#718096' }}>
                          {event.category === 'national' ? 'National holiday' :
                           event.category === 'religious' ? 'Religious celebration' :
                           event.category === 'cultural' ? 'Cultural event' : 'Public holiday'}
                        </p>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      </div>
    );
  }
);

CulturalCalendar.displayName = 'CulturalCalendar';
