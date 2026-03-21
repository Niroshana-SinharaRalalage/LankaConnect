'use client';

import React from 'react';
import {
  Ticket,
  DollarSign,
  Users,
  TrendingUp,
  AlertCircle,
} from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/presentation/components/ui/Card';
import { useEventAttendees } from '@/presentation/hooks/useEvents';

import type { EventDto } from '@/infrastructure/api/types/events.types';

interface TicketRevenueViewProps {
  eventId: string;
  event: EventDto;
}

/**
 * Ticket Revenue sub-tab within Attendees & Finance.
 * Displays ticket sales summary and revenue breakdown using existing attendee data.
 * No new backend endpoint needed — reuses useEventAttendees.
 */
export function TicketRevenueView({ eventId, event }: TicketRevenueViewProps) {
  const { data: attendeesData, isLoading, error } = useEventAttendees(eventId);

  if (isLoading) {
    return (
      <div className="animate-pulse space-y-4">
        <div className="h-32 bg-neutral-200 rounded-lg" />
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="h-24 bg-neutral-200 rounded-lg" />
          <div className="h-24 bg-neutral-200 rounded-lg" />
          <div className="h-24 bg-neutral-200 rounded-lg" />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center py-16 px-4">
        <div className="w-16 h-16 rounded-full bg-red-100 flex items-center justify-center mb-4">
          <AlertCircle className="h-8 w-8 text-red-500" />
        </div>
        <h3 className="text-lg font-semibold text-neutral-800 mb-2">Failed to Load Ticket Data</h3>
        <p className="text-sm text-neutral-500">
          {error instanceof Error ? error.message : 'An unexpected error occurred.'}
        </p>
      </div>
    );
  }

  const isFreeEvent = event.isFree || attendeesData?.isFreeEvent;

  // Free event state
  if (isFreeEvent) {
    return (
      <div className="flex flex-col items-center justify-center py-16 px-4">
        <div className="w-16 h-16 rounded-full bg-blue-100 flex items-center justify-center mb-4">
          <Ticket className="h-8 w-8 text-blue-500" />
        </div>
        <h3 className="text-lg font-semibold text-neutral-800 mb-2">Free Event</h3>
        <p className="text-sm text-neutral-500 text-center max-w-md">
          This is a free event — no ticket revenue to display.
          Visit the <strong>Attendees</strong> sub-tab to see registration details.
        </p>
        {attendeesData && (
          <div className="mt-6 flex gap-6">
            <div className="text-center">
              <p className="text-2xl font-bold text-neutral-900">{attendeesData.totalRegistrations}</p>
              <p className="text-xs text-neutral-500">Registrations</p>
            </div>
            <div className="text-center">
              <p className="text-2xl font-bold text-neutral-900">{attendeesData.totalAttendees}</p>
              <p className="text-xs text-neutral-500">Total Attendees</p>
            </div>
          </div>
        )}
      </div>
    );
  }

  // Paid event — show revenue breakdown
  const totalRegistrations = attendeesData?.totalRegistrations ?? 0;
  const totalAttendees = attendeesData?.totalAttendees ?? 0;
  const grossRevenue = attendeesData?.grossRevenue ?? 0;
  const hasBreakdown = attendeesData?.hasRevenueBreakdown ?? false;
  const totalSalesTax = attendeesData?.totalSalesTax ?? 0;
  const totalStripeFees = attendeesData?.totalStripeFees ?? 0;
  const totalPlatformCommission = attendeesData?.totalPlatformCommission ?? 0;
  const totalOrganizerPayout = attendeesData?.totalOrganizerPayout ?? 0;
  const averageTaxRate = attendeesData?.averageTaxRate ?? 0;
  const capacity = event.capacity;
  const capacityPercent = capacity > 0 ? Math.round((totalRegistrations / capacity) * 100) : 0;

  const formatCurrency = (amount: number) =>
    new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount);

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-blue-100 rounded-lg">
                <Ticket className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-sm text-neutral-500">Tickets Sold</p>
                <p className="text-2xl font-bold text-neutral-900">{totalRegistrations}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-green-100 rounded-lg">
                <Users className="h-5 w-5 text-green-600" />
              </div>
              <div>
                <p className="text-sm text-neutral-500">Total Attendees</p>
                <p className="text-2xl font-bold text-neutral-900">{totalAttendees}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-emerald-100 rounded-lg">
                <DollarSign className="h-5 w-5 text-emerald-600" />
              </div>
              <div>
                <p className="text-sm text-neutral-500">Gross Revenue</p>
                <p className="text-2xl font-bold text-neutral-900">{formatCurrency(grossRevenue)}</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-purple-100 rounded-lg">
                <TrendingUp className="h-5 w-5 text-purple-600" />
              </div>
              <div>
                <p className="text-sm text-neutral-500">Capacity</p>
                <p className="text-2xl font-bold text-neutral-900">
                  {totalRegistrations}/{capacity}
                  <span className="text-sm font-normal text-neutral-500 ml-1">({capacityPercent}%)</span>
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Revenue Breakdown */}
      {hasBreakdown && (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Revenue Breakdown</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              <div className="flex items-center justify-between py-2">
                <span className="text-sm text-neutral-600">Gross Revenue</span>
                <span className="text-sm font-semibold text-neutral-900">{formatCurrency(grossRevenue)}</span>
              </div>
              <div className="flex items-center justify-between py-2 border-t border-neutral-100">
                <span className="text-sm text-neutral-600">
                  Sales Tax
                  {averageTaxRate > 0 && (
                    <span className="text-xs text-neutral-400 ml-1">
                      (avg {(averageTaxRate * 100).toFixed(2)}%)
                    </span>
                  )}
                </span>
                <span className="text-sm text-neutral-700">- {formatCurrency(totalSalesTax)}</span>
              </div>
              <div className="flex items-center justify-between py-2 border-t border-neutral-100">
                <span className="text-sm text-neutral-600">Stripe Processing Fees</span>
                <span className="text-sm text-neutral-700">- {formatCurrency(totalStripeFees)}</span>
              </div>
              <div className="flex items-center justify-between py-2 border-t border-neutral-100">
                <span className="text-sm text-neutral-600">Platform Commission</span>
                <span className="text-sm text-neutral-700">- {formatCurrency(totalPlatformCommission)}</span>
              </div>
              <div className="flex items-center justify-between py-3 border-t-2 border-neutral-300 mt-2">
                <span className="text-base font-semibold text-neutral-900">Organizer Payout</span>
                <span className="text-base font-bold text-emerald-700">{formatCurrency(totalOrganizerPayout)}</span>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* No breakdown data yet */}
      {!hasBreakdown && grossRevenue > 0 && (
        <Card>
          <CardContent className="py-8 text-center">
            <p className="text-sm text-neutral-500">
              Detailed revenue breakdown is not available for legacy registrations.
            </p>
            <p className="text-lg font-semibold text-neutral-900 mt-2">
              Total Revenue: {formatCurrency(grossRevenue)}
            </p>
          </CardContent>
        </Card>
      )}

      {/* No registrations yet */}
      {totalRegistrations === 0 && (
        <Card>
          <CardContent className="py-12 text-center">
            <Ticket className="h-12 w-12 text-neutral-300 mx-auto mb-3" />
            <h3 className="text-lg font-semibold text-neutral-700 mb-1">No Ticket Sales Yet</h3>
            <p className="text-sm text-neutral-500">
              Ticket revenue will appear here once attendees register for this event.
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
