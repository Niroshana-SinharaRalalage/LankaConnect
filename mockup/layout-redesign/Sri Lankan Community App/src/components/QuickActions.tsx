import { Calendar, Store, MessageSquare, Newspaper, Users, MapPin } from 'lucide-react';
import { Button } from './ui/button';

export function QuickActions() {
  const actions = [
    { icon: Calendar, label: 'Create Event', color: 'text-orange-600', bgColor: 'bg-orange-50' },
    { icon: Store, label: 'List Business', color: 'text-emerald-600', bgColor: 'bg-emerald-50' },
    { icon: MessageSquare, label: 'Start Discussion', color: 'text-rose-600', bgColor: 'bg-rose-50' },
    { icon: Newspaper, label: 'Share News', color: 'text-amber-600', bgColor: 'bg-amber-50' },
    { icon: Users, label: 'Find Members', color: 'text-blue-600', bgColor: 'bg-blue-50' },
    { icon: MapPin, label: 'Local Services', color: 'text-purple-600', bgColor: 'bg-purple-50' },
  ];

  return (
    <div className="bg-white border-y border-neutral-200">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
          {actions.map((action) => {
            const Icon = action.icon;
            return (
              <button
                key={action.label}
                className="flex flex-col items-center gap-3 p-4 rounded-xl hover:bg-neutral-50 transition-colors group"
              >
                <div className={`w-14 h-14 rounded-full ${action.bgColor} flex items-center justify-center group-hover:scale-110 transition-transform`}>
                  <Icon className={`h-6 w-6 ${action.color}`} />
                </div>
                <span className="text-sm text-neutral-700 text-center">{action.label}</span>
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
}
