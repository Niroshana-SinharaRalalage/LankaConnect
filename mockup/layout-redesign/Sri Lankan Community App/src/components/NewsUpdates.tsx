import { Newspaper, ArrowRight, Clock } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Button } from './ui/button';

export function NewsUpdates() {
  const news = [
    {
      title: 'New Sri Lankan restaurant opens in downtown',
      excerpt: 'Authentic cuisine from Colombo arrives...',
      time: '3h ago',
      category: 'Business',
      color: 'text-emerald-600',
    },
    {
      title: 'Community raises $50K for Sri Lankan schools',
      excerpt: 'Successful fundraiser helps education...',
      time: '1d ago',
      category: 'Community',
      color: 'text-orange-600',
    },
    {
      title: 'Cultural dance competition winners announced',
      excerpt: 'Young performers showcase talent...',
      time: '2d ago',
      category: 'Culture',
      color: 'text-rose-600',
    },
  ];

  return (
    <Card className="border-neutral-200 shadow-sm">
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle className="flex items-center gap-2 text-neutral-900">
          <Newspaper className="h-5 w-5 text-amber-600" />
          News & Updates
        </CardTitle>
        <Button variant="ghost" size="sm" className="text-amber-600">
          <ArrowRight className="h-4 w-4" />
        </Button>
      </CardHeader>
      <CardContent className="space-y-4">
        {news.map((item, index) => (
          <div
            key={index}
            className="pb-4 border-b border-neutral-100 last:border-0 last:pb-0 hover:pl-2 transition-all cursor-pointer"
          >
            <div className={`text-xs mb-2 ${item.color}`}>{item.category}</div>
            <h4 className="text-neutral-900 text-sm mb-1 hover:text-amber-600 transition-colors">
              {item.title}
            </h4>
            <p className="text-neutral-600 text-xs mb-2">{item.excerpt}</p>
            <div className="flex items-center gap-1 text-xs text-neutral-500">
              <Clock className="h-3 w-3" />
              <span>{item.time}</span>
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
