'use client';

import React, { useState } from 'react';
import {
  Users,
  Ticket,
  Heart,
  Wallet,
  Award,
  ShoppingBag,
} from 'lucide-react';

import { AttendeeManagementTab } from '@/presentation/components/features/events/AttendeeManagementTab';
import { DonationsManagementTab } from '@/presentation/components/features/events/DonationsManagementTab';
import { CollectionsManagementTab } from '@/presentation/components/features/events/CollectionsManagementTab';
import { SponsorsManagementTab } from '@/presentation/components/features/events/SponsorsManagementTab';
import { AddOnsManagementTab } from '@/presentation/components/features/events/AddOnsManagementTab';
import { TicketRevenueView } from '@/presentation/components/features/events/TicketRevenueView';

import type { EventDto } from '@/infrastructure/api/types/events.types';

type SubTab = 'attendees' | 'tickets' | 'donations' | 'collections' | 'sponsors' | 'addons';

interface SubTabConfig {
  id: SubTab;
  label: string;
  icon: React.ElementType;
}

const SUB_TABS: SubTabConfig[] = [
  { id: 'attendees', label: 'Attendees', icon: Users },
  { id: 'tickets', label: 'Tickets', icon: Ticket },
  { id: 'donations', label: 'Donations', icon: Heart },
  { id: 'collections', label: 'Collections', icon: Wallet },
  { id: 'sponsors', label: 'Sponsors', icon: Award },
  { id: 'addons', label: 'Add-Ons', icon: ShoppingBag },
];

interface AttendeesAndFinanceTabProps {
  eventId: string;
  event: EventDto;
}

/**
 * Consolidated Attendees & Finance tab with 6 sub-tabs.
 * Follows the PhotoAlbumManagementTab sub-tab pattern (useState + button row).
 */
export function AttendeesAndFinanceTab({ eventId, event }: AttendeesAndFinanceTabProps) {
  const [activeSubTab, setActiveSubTab] = useState<SubTab>('attendees');

  const renderSubTabContent = () => {
    switch (activeSubTab) {
      case 'attendees':
        return <AttendeeManagementTab eventId={eventId} />;
      case 'tickets':
        return <TicketRevenueView eventId={eventId} event={event} />;
      case 'donations':
        return <DonationsManagementTab eventId={eventId} donationConfig={event.donationConfig ?? null} />;
      case 'collections':
        return <CollectionsManagementTab eventId={eventId} collectionConfig={event.collectionConfig ?? null} />;
      case 'sponsors':
        return <SponsorsManagementTab eventId={eventId} sponsorConfig={event.sponsorConfig ?? null} />;
      case 'addons':
        return <AddOnsManagementTab eventId={eventId} addOnConfig={event.addOnConfig ?? null} />;
      default:
        return null;
    }
  };

  return (
    <div className="space-y-6">
      {/* Sub-Tab Navigation */}
      <div className="flex items-center gap-2 overflow-x-auto pb-2 border-b border-neutral-200">
        {SUB_TABS.map((tab) => {
          const Icon = tab.icon;
          const isActive = activeSubTab === tab.id;
          return (
            <button
              key={tab.id}
              type="button"
              onClick={() => setActiveSubTab(tab.id)}
              className={`flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-t-lg whitespace-nowrap transition-colors ${
                isActive
                  ? 'bg-white border border-b-0 border-neutral-200 text-neutral-900'
                  : 'text-neutral-500 hover:text-neutral-700 hover:bg-neutral-50'
              }`}
            >
              <Icon className="h-4 w-4" />
              {tab.label}
            </button>
          );
        })}
      </div>

      {/* Sub-Tab Content */}
      {renderSubTabContent()}
    </div>
  );
}
