import { Calendar } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';

export function CulturalCalendar() {
  const events = [
    {
      date: '13',
      month: 'Apr',
      title: 'Sinhala New Year',
      description: 'Traditional celebrations nationwide',
      color: 'bg-orange-600',
    },
    {
      date: '23',
      month: 'May',
      title: 'Vesak Day',
      description: 'Buddhist celebration of enlightenment',
      color: 'bg-amber-600',
    },
    {
      date: '15',
      month: 'Aug',
      title: 'Independence Day',
      description: 'Sri Lankan independence celebrations',
      color: 'bg-emerald-600',
    },
  ];

  return (
    <Card className="border-neutral-200 shadow-sm">
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-neutral-900">
          <Calendar className="h-5 w-5 text-orange-600" />
          Cultural Calendar
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {events.map((event) => (
          <div
            key={event.title}
            className="flex gap-4 p-3 rounded-lg hover:bg-neutral-50 transition-colors cursor-pointer"
          >
            <div className={`${event.color} text-white rounded-lg p-3 flex flex-col items-center justify-center min-w-[60px]`}>
              <div className="text-2xl">{event.date}</div>
              <div className="text-xs uppercase">{event.month}</div>
            </div>
            <div className="flex-1">
              <div className="text-neutral-900 mb-1">{event.title}</div>
              <div className="text-neutral-500 text-sm">{event.description}</div>
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
