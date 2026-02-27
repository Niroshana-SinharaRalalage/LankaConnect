'use client';

import { useState } from 'react';
import {
  Heart,
  Download,
  DollarSign,
  Users,
  TrendingUp,
  Mail,
  Phone,
  Calendar,
  RefreshCw,
  Settings,
  CheckCircle,
  XCircle,
} from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/presentation/components/ui/Card';
import { Button } from '@/presentation/components/ui/Button';
import { Badge } from '@/presentation/components/ui/Badge';
import { useEventDonations } from '@/presentation/hooks/useDonations';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type { DonationDto, DonationStatus, DonationConfigurationDto } from '@/infrastructure/api/types/events.types';

interface DonationsManagementTabProps {
  eventId: string;
  donationConfig?: DonationConfigurationDto | null;
}

function getDonationStatusColor(status: DonationStatus | string): string {
  switch (status) {
    case 'Completed':
      return '#10B981';
    case 'Pending':
      return '#F59E0B';
    case 'Failed':
      return '#EF4444';
    case 'Refunded':
      return '#6366F1';
    case 'Abandoned':
      return '#6B7280';
    default:
      return '#6B7280';
  }
}

/**
 * Donations Management Tab for event organizers.
 * Shows donation summary, donations table, and export button.
 * Follows AttendeeManagementTab pattern.
 */
export function DonationsManagementTab({ eventId, donationConfig }: DonationsManagementTabProps) {
  const [isExporting, setIsExporting] = useState(false);

  const { data: donationsData, isLoading, error, refetch } = useEventDonations(eventId);

  const handleExport = async (format: 'excel' | 'csv') => {
    try {
      setIsExporting(true);
      const blob = await eventsRepository.exportDonations(eventId, format);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `donations-${eventId}.${format === 'excel' ? 'xlsx' : 'csv'}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err) {
      console.error('Failed to export donations:', err);
    } finally {
      setIsExporting(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <RefreshCw className="h-6 w-6 animate-spin text-neutral-400" />
        <span className="ml-2 text-neutral-500">Loading donations...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-sm text-red-600">
        Failed to load donations. Please try again.
      </div>
    );
  }

  const summary = donationsData?.summary;
  const donations = donationsData?.donations || [];

  return (
    <div className="space-y-6">
      {/* Donation Configuration */}
      {donationConfig && (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-base">
              <Settings className="h-4 w-4 text-neutral-500" />
              Donation Settings
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 text-sm">
              <div className="flex items-center gap-2">
                {donationConfig.isEnabled ? (
                  <CheckCircle className="h-4 w-4 text-emerald-500" />
                ) : (
                  <XCircle className="h-4 w-4 text-neutral-400" />
                )}
                <span className="text-neutral-700">
                  {donationConfig.isEnabled ? 'Donations enabled' : 'Donations disabled'}
                </span>
              </div>
              {donationConfig.isEnabled && (
                <>
                  {donationConfig.suggestedAmounts.length > 0 && (
                    <div>
                      <span className="text-neutral-500">Suggested amounts:</span>{' '}
                      <span className="font-medium text-neutral-700">
                        {donationConfig.suggestedAmounts.map((a) => `$${a.toFixed(2)}`).join(', ')}
                      </span>
                    </div>
                  )}
                  <div className="flex items-center gap-2">
                    {donationConfig.allowCustomAmount ? (
                      <CheckCircle className="h-4 w-4 text-emerald-500" />
                    ) : (
                      <XCircle className="h-4 w-4 text-neutral-400" />
                    )}
                    <span className="text-neutral-700">Custom amount</span>
                  </div>
                  {(donationConfig.minAmount || donationConfig.maxAmount) && (
                    <div>
                      <span className="text-neutral-500">Range:</span>{' '}
                      <span className="font-medium text-neutral-700">
                        {donationConfig.minAmount ? `$${donationConfig.minAmount.toFixed(2)}` : 'No min'}
                        {' — '}
                        {donationConfig.maxAmount ? `$${donationConfig.maxAmount.toFixed(2)}` : 'No max'}
                      </span>
                    </div>
                  )}
                  {donationConfig.donationMessage && (
                    <div className="sm:col-span-2 lg:col-span-3">
                      <span className="text-neutral-500">Message:</span>{' '}
                      <span className="text-neutral-700 italic">&ldquo;{donationConfig.donationMessage}&rdquo;</span>
                    </div>
                  )}
                </>
              )}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Summary Cards */}
      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <Card>
            <CardContent className="pt-4 pb-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-rose-50">
                  <Heart className="h-5 w-5 text-rose-500" />
                </div>
                <div>
                  <p className="text-xs text-neutral-500">Total Donations</p>
                  <p className="text-lg font-bold text-neutral-800">{summary.completedDonations}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-4 pb-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-emerald-50">
                  <DollarSign className="h-5 w-5 text-emerald-600" />
                </div>
                <div>
                  <p className="text-xs text-neutral-500">Total Amount</p>
                  <p className="text-lg font-bold text-neutral-800">${summary.totalAmount.toFixed(2)}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-4 pb-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-blue-50">
                  <TrendingUp className="h-5 w-5 text-blue-600" />
                </div>
                <div>
                  <p className="text-xs text-neutral-500">Average Donation</p>
                  <p className="text-lg font-bold text-neutral-800">${summary.averageDonation.toFixed(2)}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-4 pb-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-orange-50">
                  <Users className="h-5 w-5 text-orange-600" />
                </div>
                <div>
                  <p className="text-xs text-neutral-500">Net Raised</p>
                  <p className="text-lg font-bold text-neutral-800">${(summary.totalAmount - summary.totalStripeFees - summary.totalPlatformCommission).toFixed(2)}</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Actions Row */}
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-neutral-700">
          All Donations ({donations.length})
        </h3>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => refetch()}
            disabled={isLoading}
          >
            <RefreshCw className={`h-4 w-4 mr-1 ${isLoading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => handleExport('excel')}
            disabled={isExporting || donations.length === 0}
          >
            <Download className="h-4 w-4 mr-1" />
            {isExporting ? 'Exporting...' : 'Export Excel'}
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => handleExport('csv')}
            disabled={isExporting || donations.length === 0}
          >
            <Download className="h-4 w-4 mr-1" />
            CSV
          </Button>
        </div>
      </div>

      {/* Donations Table */}
      {donations.length === 0 ? (
        <div className="text-center py-12 text-neutral-500">
          <Heart className="h-10 w-10 mx-auto mb-3 text-neutral-300" />
          <p className="text-sm">No donations yet.</p>
          <p className="text-xs text-neutral-400 mt-1">Donations will appear here once someone contributes.</p>
        </div>
      ) : (
        <div className="overflow-x-auto rounded-lg border border-neutral-200">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-neutral-50 border-b border-neutral-200">
                <th className="text-left px-4 py-3 font-medium text-neutral-600">Donor</th>
                <th className="text-left px-4 py-3 font-medium text-neutral-600">Contact</th>
                <th className="text-right px-4 py-3 font-medium text-neutral-600">Amount</th>
                <th className="text-center px-4 py-3 font-medium text-neutral-600">Status</th>
                <th className="text-left px-4 py-3 font-medium text-neutral-600">Date</th>
                <th className="text-left px-4 py-3 font-medium text-neutral-600">Notes</th>
              </tr>
            </thead>
            <tbody>
              {donations.map((donation: DonationDto) => (
                <tr key={donation.id} className="border-b border-neutral-100 hover:bg-neutral-50">
                  <td className="px-4 py-3">
                    <span className="font-medium text-neutral-800">{donation.donorName}</span>
                  </td>
                  <td className="px-4 py-3">
                    <div className="space-y-0.5">
                      <div className="flex items-center gap-1 text-xs text-neutral-600">
                        <Mail className="h-3 w-3" />
                        <span>{donation.donorEmail}</span>
                      </div>
                      {donation.donorPhone && (
                        <div className="flex items-center gap-1 text-xs text-neutral-500">
                          <Phone className="h-3 w-3" />
                          <span>{donation.donorPhone}</span>
                        </div>
                      )}
                    </div>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <span className="font-semibold text-neutral-800">
                      ${donation.amount.toFixed(2)}
                    </span>
                    {donation.currency !== 'USD' && (
                      <span className="text-xs text-neutral-500 ml-1">{donation.currency}</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-center">
                    <Badge style={{ backgroundColor: getDonationStatusColor(donation.status) }}>
                      {donation.status}
                    </Badge>
                  </td>
                  <td className="px-4 py-3 text-neutral-600">
                    <div className="flex items-center gap-1 text-xs">
                      <Calendar className="h-3 w-3" />
                      <span>
                        {donation.paymentCompletedAt
                          ? new Date(donation.paymentCompletedAt).toLocaleDateString()
                          : new Date(donation.createdAt).toLocaleDateString()}
                      </span>
                    </div>
                  </td>
                  <td className="px-4 py-3 text-xs text-neutral-500 max-w-[200px] truncate">
                    {donation.donorNotes || '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
