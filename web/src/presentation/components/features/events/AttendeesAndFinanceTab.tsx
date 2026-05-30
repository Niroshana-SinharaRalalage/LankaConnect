'use client';

import React, { useState } from 'react';
import {
  Users,
  Ticket,
  Heart,
  Wallet,
  Award,
  ShoppingBag,
  Download,
} from 'lucide-react';

import { AttendeeManagementTab } from '@/presentation/components/features/events/AttendeeManagementTab';
import { DonationsManagementTab } from '@/presentation/components/features/events/DonationsManagementTab';
import { CollectionsManagementTab } from '@/presentation/components/features/events/CollectionsManagementTab';
import { SponsorsManagementTab } from '@/presentation/components/features/events/SponsorsManagementTab';
import { SponsorshipPackagesManagementSection } from '@/presentation/components/features/events/SponsorshipPackagesManagementSection'; // Phase 6A.156
import { AddOnsManagementTab } from '@/presentation/components/features/events/AddOnsManagementTab';
import { TicketRevenueView } from '@/presentation/components/features/events/TicketRevenueView';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';

import type { EventDto } from '@/infrastructure/api/types/events.types';

type SubTab = 'attendees' | 'tickets' | 'donations' | 'collections' | 'sponsors' | 'packages' | 'addons';

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
  { id: 'packages', label: 'Packages', icon: Award }, // Phase 6A.156: sponsorship packages
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
  const [isExportingAll, setIsExportingAll] = useState(false);

  const handleExportAll = async (format: 'excel' | 'csv') => {
    try {
      setIsExportingAll(true);
      const blob = await eventsRepository.exportAllFinancials(eventId, format);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `all-financials-${eventId}.${format === 'excel' ? 'xlsx' : 'zip'}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err) {
      console.error('Failed to export all financials:', err);
    } finally {
      setIsExportingAll(false);
    }
  };

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
      case 'packages':
        // Phase 6A.156 — sponsorship packages live behind the EnablePackages flag in
        // SponsorConfig. The management section itself shows a guided empty state
        // when the flag is off, so we always render it and let it self-gate.
        return (
          <SponsorshipPackagesManagementSection
            eventId={eventId}
            enabled={event.sponsorConfig?.enablePackages === true}
          />
        );
      case 'addons':
        return <AddOnsManagementTab eventId={eventId} addOnConfig={event.addOnConfig ?? null} />;
      default:
        return null;
    }
  };

  return (
    <div className="space-y-6">
      {/* Header with Export All buttons */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2 overflow-x-auto pb-2 border-b border-neutral-200 flex-1">
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

        {/* Export All buttons */}
        <div className="flex items-center gap-2 ml-4 pb-2 shrink-0">
          <button
            type="button"
            onClick={() => handleExportAll('csv')}
            disabled={isExportingAll}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-md border border-neutral-300 text-neutral-700 hover:bg-neutral-50 disabled:opacity-50"
          >
            <Download className="h-3.5 w-3.5" />
            Export All (CSV)
          </button>
          <button
            type="button"
            onClick={() => handleExportAll('excel')}
            disabled={isExportingAll}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-md text-white disabled:opacity-50"
            style={{ background: '#FF7900' }}
          >
            <Download className="h-3.5 w-3.5" />
            Export All (Excel)
          </button>
        </div>
      </div>

      {/* Sub-Tab Content */}
      {renderSubTabContent()}
    </div>
  );
}
