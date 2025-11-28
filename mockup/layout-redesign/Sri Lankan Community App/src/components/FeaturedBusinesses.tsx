import { Star, TrendingUp } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Badge } from './ui/badge';

export function FeaturedBusinesses() {
  const businesses = [
    {
      name: 'Lanka Kitchen',
      category: 'Authentic Sri Lankan Restaurant',
      rating: 4.8,
      initials: 'LK',
      color: 'bg-orange-600',
    },
    {
      name: 'Spice Temple',
      category: 'Grocery & Spices',
      rating: 4.9,
      initials: 'ST',
      color: 'bg-rose-600',
    },
    {
      name: 'Dr. Lanka Immigration',
      category: 'Legal Services',
      rating: 4.7,
      initials: 'DL',
      color: 'bg-emerald-600',
    },
  ];

  return (
    <Card className="border-neutral-200 shadow-sm">
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-neutral-900">
          <TrendingUp className="h-5 w-5 text-orange-600" />
          Featured Businesses
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {businesses.map((business) => (
          <div
            key={business.name}
            className="flex gap-3 p-3 rounded-lg hover:bg-neutral-50 transition-colors cursor-pointer"
          >
            <div className={`${business.color} text-white rounded-lg p-3 flex items-center justify-center min-w-[50px] h-[50px]`}>
              <span className="text-lg">{business.initials}</span>
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-start justify-between gap-2">
                <div className="text-neutral-900 truncate">{business.name}</div>
                <div className="flex items-center gap-1 flex-shrink-0">
                  <Star className="h-4 w-4 fill-amber-400 text-amber-400" />
                  <span className="text-neutral-700 text-sm">{business.rating}</span>
                </div>
              </div>
              <div className="text-neutral-500 text-sm truncate">{business.category}</div>
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
