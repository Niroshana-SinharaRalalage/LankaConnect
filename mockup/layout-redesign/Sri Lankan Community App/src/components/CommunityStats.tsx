import { BarChart3, TrendingUp } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';

export function CommunityStats() {
  const stats = [
    { label: 'Active Today', value: '2,340', trend: '+12%', color: 'text-emerald-600' },
    { label: 'Events This Week', value: '127', trend: '+8%', color: 'text-orange-600' },
    { label: 'New Businesses', value: '23', trend: '+15%', color: 'text-rose-600' },
    { label: 'Forum Discussions', value: '456', trend: '+5%', color: 'text-amber-600' },
  ];

  return (
    <Card className="border-neutral-200 shadow-sm">
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-neutral-900">
          <BarChart3 className="h-5 w-5 text-orange-600" />
          Community Stats
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {stats.map((stat) => (
          <div key={stat.label} className="flex items-center justify-between p-3 rounded-lg hover:bg-neutral-50 transition-colors">
            <div>
              <div className="text-neutral-500 text-sm mb-1">{stat.label}</div>
              <div className="text-neutral-900 text-2xl">{stat.value}</div>
            </div>
            <div className={`flex items-center gap-1 ${stat.color}`}>
              <TrendingUp className="h-4 w-4" />
              <span className="text-sm">{stat.trend}</span>
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
