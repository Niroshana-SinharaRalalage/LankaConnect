import { MessageSquare, ArrowRight, ThumbsUp, MessageCircle } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Button } from './ui/button';
import { Avatar, AvatarFallback } from './ui/avatar';
import { Badge } from './ui/badge';

export function ForumHighlights() {
  const discussions = [
    {
      title: 'Best places to buy Sri Lankan groceries?',
      author: 'Saman P.',
      initials: 'SP',
      time: '2h ago',
      replies: 24,
      likes: 67,
      category: 'Food & Lifestyle',
      isHot: true,
    },
    {
      title: 'Teaching Sinhala to kids abroad',
      author: 'Nisha R.',
      initials: 'NR',
      time: '5h ago',
      replies: 18,
      likes: 45,
      category: 'Parenting',
      isHot: false,
    },
    {
      title: 'Planning a trip to Sri Lanka - tips?',
      author: 'Kasun M.',
      initials: 'KM',
      time: '1d ago',
      replies: 56,
      likes: 123,
      category: 'Travel',
      isHot: true,
    },
    {
      title: 'Sri Lankan business networking group',
      author: 'Amara S.',
      initials: 'AS',
      time: '2d ago',
      replies: 31,
      likes: 89,
      category: 'Business',
      isHot: false,
    },
  ];

  return (
    <Card className="border-neutral-200 shadow-sm">
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle className="flex items-center gap-2 text-neutral-900">
          <MessageSquare className="h-5 w-5 text-rose-600" />
          Forum Highlights
        </CardTitle>
        <Button variant="ghost" size="sm" className="text-rose-600">
          <ArrowRight className="h-4 w-4" />
        </Button>
      </CardHeader>
      <CardContent className="space-y-3">
        {discussions.map((discussion, index) => (
          <div
            key={index}
            className="p-3 rounded-lg border border-neutral-200 hover:border-rose-200 hover:bg-rose-50/30 transition-all cursor-pointer"
          >
            <div className="flex gap-3">
              <Avatar className="h-10 w-10 flex-shrink-0">
                <AvatarFallback className="bg-gradient-to-br from-rose-500 to-pink-600 text-white text-sm">
                  {discussion.initials}
                </AvatarFallback>
              </Avatar>

              <div className="flex-1 min-w-0">
                <div className="flex items-start justify-between gap-2 mb-1">
                  <h4 className="text-neutral-900 text-sm line-clamp-2">
                    {discussion.title}
                  </h4>
                  {discussion.isHot && (
                    <Badge className="bg-rose-100 text-rose-700 text-xs flex-shrink-0">
                      🔥 Hot
                    </Badge>
                  )}
                </div>

                <div className="text-xs text-neutral-500 mb-2">
                  {discussion.author} • {discussion.time}
                </div>

                <div className="flex items-center gap-3 text-xs text-neutral-600">
                  <span className="px-2 py-1 rounded bg-neutral-100">{discussion.category}</span>
                  <div className="flex items-center gap-1">
                    <MessageCircle className="h-3.5 w-3.5" />
                    <span>{discussion.replies}</span>
                  </div>
                  <div className="flex items-center gap-1">
                    <ThumbsUp className="h-3.5 w-3.5" />
                    <span>{discussion.likes}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
