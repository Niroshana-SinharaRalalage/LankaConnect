import { Users, Calendar, Store, MessageSquare } from 'lucide-react';

export function StatsBar() {
  const stats = [
    { icon: Users, label: 'Members', value: '12,500+', color: 'text-orange-600' },
    { icon: Calendar, label: 'Events', value: '450+', color: 'text-rose-600' },
    { icon: Store, label: 'Businesses', value: '2,200+', color: 'text-emerald-600' },
    { icon: MessageSquare, label: 'Discussions', value: '456', color: 'text-amber-600' },
  ];

  return (
    <div className="bg-white border-b border-neutral-200 -mt-8 relative z-10">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 md:gap-6">
          {stats.map((stat, index) => {
            const Icon = stat.icon;
            return (
              <div
                key={stat.label}
                className="bg-neutral-50 rounded-xl p-4 md:p-6 hover:shadow-md transition-shadow border border-neutral-100"
              >
                <div className="flex items-center gap-3">
                  <div className={`p-2 rounded-lg bg-white ${stat.color}`}>
                    <Icon className="h-5 w-5" />
                  </div>
                  <div>
                    <div className="text-neutral-500 text-sm">{stat.label}</div>
                    <div className="text-neutral-900 text-2xl">{stat.value}</div>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
