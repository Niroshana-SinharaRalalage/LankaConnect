import { Calendar, Store, Heart, MessageSquare, MapPin, Clock } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from './ui/tabs';
import { Badge } from './ui/badge';
import { Avatar, AvatarFallback } from './ui/avatar';
import { Button } from './ui/button';

interface CommunityActivityProps {
  activeTab: string;
  setActiveTab: (tab: string) => void;
}

export function CommunityActivity({ activeTab, setActiveTab }: CommunityActivityProps) {
  const activities = [
    {
      type: 'event',
      user: 'Event Organizer',
      userInitials: 'EO',
      time: '2 days ago',
      location: 'Westlake, Ohio',
      title: 'Cultural Dance Workshop',
      description: 'Join us for a traditional Sri Lankan dance workshop featuring Kandyan and low-country styles.',
      badge: 'Event',
      badgeColor: 'bg-orange-100 text-orange-700',
    },
    {
      type: 'event',
      user: 'Event Organizer',
      userInitials: 'EO',
      time: '3 days ago',
      location: 'Columbus, Ohio',
      title: 'Sinhala Language Classes',
      description: 'Learn to read, write and speak Sinhala with our experienced instructors.',
      badge: 'Event',
      badgeColor: 'bg-orange-100 text-orange-700',
    },
  ];

  return (
    <Card className="border-neutral-200 shadow-sm">
      <CardHeader>
        <CardTitle className="text-neutral-900">Community Activity</CardTitle>
      </CardHeader>
      <CardContent>
        <Tabs defaultValue="all" className="w-full">
          <TabsList className="grid w-full grid-cols-5 mb-6">
            <TabsTrigger value="all" className="gap-2">
              <Heart className="h-4 w-4" />
              <span className="hidden sm:inline">All Posts</span>
            </TabsTrigger>
            <TabsTrigger value="events" className="gap-2">
              <Calendar className="h-4 w-4" />
              <span className="hidden sm:inline">Events</span>
            </TabsTrigger>
            <TabsTrigger value="businesses" className="gap-2">
              <Store className="h-4 w-4" />
              <span className="hidden sm:inline">Businesses</span>
            </TabsTrigger>
            <TabsTrigger value="culture" className="gap-2">
              <Heart className="h-4 w-4" />
              <span className="hidden sm:inline">Culture</span>
            </TabsTrigger>
            <TabsTrigger value="discussions" className="gap-2">
              <MessageSquare className="h-4 w-4" />
              <span className="hidden sm:inline">Discussions</span>
            </TabsTrigger>
          </TabsList>

          <TabsContent value="all" className="space-y-4">
            {activities.map((activity, index) => (
              <div
                key={index}
                className="p-4 md:p-6 rounded-lg border border-neutral-200 hover:border-neutral-300 transition-all hover:shadow-sm bg-white"
              >
                <div className="flex gap-4">
                  <Avatar className="h-12 w-12">
                    <AvatarFallback className="bg-gradient-to-br from-orange-500 to-orange-600 text-white">
                      {activity.userInitials}
                    </AvatarFallback>
                  </Avatar>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between gap-2 mb-2">
                      <div>
                        <div className="text-neutral-900 mb-1">{activity.user}</div>
                        <div className="flex flex-wrap items-center gap-2 text-neutral-500 text-sm">
                          <span className="flex items-center gap-1">
                            <Clock className="h-3.5 w-3.5" />
                            {activity.time}
                          </span>
                          <span>•</span>
                          <span className="flex items-center gap-1">
                            <MapPin className="h-3.5 w-3.5" />
                            {activity.location}
                          </span>
                        </div>
                      </div>
                      <Badge className={activity.badgeColor}>{activity.badge}</Badge>
                    </div>
                    <div className="text-neutral-900 text-lg mb-2">{activity.title}</div>
                    <p className="text-neutral-600 mb-4">{activity.description}</p>
                    <div className="flex gap-2">
                      <Button size="sm" className="bg-orange-600 hover:bg-orange-700">
                        Learn More
                      </Button>
                      <Button size="sm" variant="outline">
                        Share
                      </Button>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </TabsContent>

          <TabsContent value="events">
            <div className="text-center py-12 text-neutral-500">
              Events content will be displayed here
            </div>
          </TabsContent>

          <TabsContent value="businesses">
            <div className="text-center py-12 text-neutral-500">
              Businesses content will be displayed here
            </div>
          </TabsContent>

          <TabsContent value="culture">
            <div className="text-center py-12 text-neutral-500">
              Culture content will be displayed here
            </div>
          </TabsContent>

          <TabsContent value="discussions">
            <div className="text-center py-12 text-neutral-500">
              Discussions content will be displayed here
            </div>
          </TabsContent>
        </Tabs>
      </CardContent>
    </Card>
  );
}
