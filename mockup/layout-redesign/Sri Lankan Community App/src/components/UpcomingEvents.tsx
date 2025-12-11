import { Calendar, MapPin, Users, Clock, ArrowRight } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Button } from './ui/button';
import { Badge } from './ui/badge';

export function UpcomingEvents() {
  const events = [
    {
      title: 'Sinhala & Tamil New Year Celebration',
      date: 'April 14, 2024',
      time: '6:00 PM - 11:00 PM',
      location: 'Sri Lankan Community Center, Toronto',
      attendees: 234,
      category: 'Cultural',
      color: 'bg-orange-600',
      bgColor: 'bg-orange-50',
      image: '🎉',
    },
    {
      title: 'Traditional Cooking Workshop',
      date: 'April 20, 2024',
      time: '2:00 PM - 5:00 PM',
      location: 'Lanka Kitchen, Vancouver',
      attendees: 45,
      category: 'Food & Culture',
      color: 'bg-amber-600',
      bgColor: 'bg-amber-50',
      image: '🍛',
    },
    {
      title: 'Kandyan Dance Performance',
      date: 'April 27, 2024',
      time: '7:30 PM - 9:30 PM',
      location: 'Royal Theatre, Melbourne',
      attendees: 189,
      category: 'Arts',
      color: 'bg-rose-600',
      bgColor: 'bg-rose-50',
      image: '💃',
    },
  ];

  return (
    <Card className="border-neutral-200 shadow-sm">
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle className="flex items-center gap-2 text-neutral-900">
          <Calendar className="h-6 w-6 text-orange-600" />
          Upcoming Events
        </CardTitle>
        <Button variant="ghost" size="sm" className="text-orange-600">
          View All
          <ArrowRight className="ml-2 h-4 w-4" />
        </Button>
      </CardHeader>
      <CardContent className="space-y-4">
        {events.map((event, index) => (
          <div
            key={index}
            className="group relative overflow-hidden rounded-xl border border-neutral-200 hover:border-orange-200 transition-all hover:shadow-md bg-white"
          >
            <div className="flex gap-4 p-4">
              {/* Event Icon/Image */}
              <div className={`flex-shrink-0 w-20 h-20 rounded-xl ${event.bgColor} flex items-center justify-center text-4xl`}>
                {event.image}
              </div>

              {/* Event Details */}
              <div className="flex-1 min-w-0">
                <div className="flex items-start justify-between gap-2 mb-2">
                  <h3 className="text-neutral-900 group-hover:text-orange-600 transition-colors line-clamp-1">
                    {event.title}
                  </h3>
                  <Badge className={`${event.color} text-white flex-shrink-0`}>
                    {event.category}
                  </Badge>
                </div>

                <div className="space-y-1.5 mb-3">
                  <div className="flex items-center gap-2 text-sm text-neutral-600">
                    <Calendar className="h-4 w-4 flex-shrink-0" />
                    <span>{event.date}</span>
                    <span className="text-neutral-400">•</span>
                    <Clock className="h-4 w-4 flex-shrink-0" />
                    <span>{event.time}</span>
                  </div>
                  <div className="flex items-center gap-2 text-sm text-neutral-600">
                    <MapPin className="h-4 w-4 flex-shrink-0" />
                    <span className="line-clamp-1">{event.location}</span>
                  </div>
                </div>

                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2 text-sm text-neutral-600">
                    <Users className="h-4 w-4" />
                    <span>{event.attendees} attending</span>
                  </div>
                  <Button size="sm" variant="outline" className="hover:bg-orange-50 hover:text-orange-600 hover:border-orange-600">
                    Register
                  </Button>
                </div>
              </div>
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
