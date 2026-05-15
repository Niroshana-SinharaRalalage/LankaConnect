'use client';

import { useState } from 'react';
import {
  Award,
  Download,
  DollarSign,
  Users,
  Package,
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
import { useEventSponsors } from '@/presentation/hooks/useSponsors';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import type { SponsorDto, SponsorConfigurationDto } from '@/infrastructure/api/types/events.types';

interface SponsorsManagementTabProps {
  eventId: string;
  sponsorConfig?: SponsorConfigurationDto | null;
}

function getSponsorStatusColor(status: string): string {
  switch (status) {
    case 'Completed':
      return '#10B981';
    case 'Pending':
      return '#F59E0B';
    case 'Failed':
      return '#EF4444';
    case 'Refunded':
      return '#8B5CF6';
    case 'RecordedItem':
      return '#6366F1';
    case 'Abandoned':
      return '#6B7280';
    default:
      return '#6B7280';
  }
}

/**
 * Sponsors Management Tab for event organizers.
 * Dual-mode: Money sponsors (via Stripe) and Item sponsors (recorded, no payment).
 * Shows sponsor summary, separate tables for money/item sponsors, and export button.
 * Follows DonationsManagementTab pattern.
 */
export function SponsorsManagementTab({ eventId, sponsorConfig }: SponsorsManagementTabProps) {
  const [isExporting, setIsExporting] = useState(false);

  const isEnabled = sponsorConfig?.isEnabled === true;
  const { data: sponsorsData, isLoading, error, refetch } = useEventSponsors(eventId, isEnabled);

  // Show enable prompt when feature is not configured
  if (!isEnabled) {
    return (
      <div className="flex flex-col items-center justify-center py-16 px-4">
        <div className="w-16 h-16 rounded-full bg-indigo-100 flex items-center justify-center mb-4">
          <Award className="h-8 w-8 text-indigo-500" />
        </div>
        <h3 className="text-lg font-semibold text-neutral-800 mb-2">Sponsors Not Enabled</h3>
        <p className="text-sm text-neutral-500 text-center max-w-md mb-4">
          Enable the Sponsors feature to accept monetary sponsorships via Stripe or record
          item-based sponsorships for your event.
        </p>
        <p className="text-xs text-neutral-400 text-center max-w-sm">
          To enable, edit your event and turn on Sponsors in the financial features section,
          or use the API to configure sponsor settings.
        </p>
      </div>
    );
  }

  const handleExport = async (format: 'excel' | 'csv') => {
    try {
      setIsExporting(true);
      const blob = await eventsRepository.exportSponsors(eventId, format);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `sponsors-${eventId}.${format === 'excel' ? 'xlsx' : 'csv'}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err) {
      console.error('Failed to export sponsors:', err);
    } finally {
      setIsExporting(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <RefreshCw className="h-6 w-6 animate-spin text-neutral-400" />
        <span className="ml-2 text-neutral-500">Loading sponsors...</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-sm text-red-600">
        Failed to load sponsors. Please try again.
      </div>
    );
  }

  const summary = sponsorsData?.summary;
  const sponsors = sponsorsData?.sponsors || [];
  const moneySponsors = sponsors.filter((s: SponsorDto) => s.sponsorType === 'Money');
  const itemSponsors = sponsors.filter((s: SponsorDto) => s.sponsorType === 'Item');

  return (
    <div className="space-y-6">
      {/* Sponsor Configuration */}
      {sponsorConfig && (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-base">
              <Settings className="h-4 w-4 text-neutral-500" />
              Sponsor Settings
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 text-sm">
              <div className="flex items-center gap-2">
                {sponsorConfig.isEnabled ? (
                  <CheckCircle className="h-4 w-4 text-emerald-500" />
                ) : (
                  <XCircle className="h-4 w-4 text-neutral-400" />
                )}
                <span className="text-neutral-700">
                  {sponsorConfig.isEnabled ? 'Sponsorships enabled' : 'Sponsorships disabled'}
                </span>
              </div>
              {sponsorConfig.isEnabled && (
                <>
                  <div className="flex items-center gap-2">
                    {sponsorConfig.acceptMoneySponsors ? (
                      <CheckCircle className="h-4 w-4 text-emerald-500" />
                    ) : (
                      <XCircle className="h-4 w-4 text-neutral-400" />
                    )}
                    <span className="text-neutral-700">Money sponsors</span>
                  </div>
                  <div className="flex items-center gap-2">
                    {sponsorConfig.acceptItemSponsors ? (
                      <CheckCircle className="h-4 w-4 text-emerald-500" />
                    ) : (
                      <XCircle className="h-4 w-4 text-neutral-400" />
                    )}
                    <span className="text-neutral-700">Item sponsors</span>
                  </div>
                  {sponsorConfig.minSponsorAmount != null && sponsorConfig.minSponsorAmount > 0 && (
                    <div>
                      <span className="text-neutral-500">Min amount:</span>{' '}
                      <span className="font-medium text-neutral-700">
                        ${sponsorConfig.minSponsorAmount.toFixed(2)}
                      </span>
                    </div>
                  )}
                  <div className="flex items-center gap-2">
                    {sponsorConfig.showSponsorList ? (
                      <CheckCircle className="h-4 w-4 text-emerald-500" />
                    ) : (
                      <XCircle className="h-4 w-4 text-neutral-400" />
                    )}
                    <span className="text-neutral-700">Show sponsor list publicly</span>
                  </div>
                  {sponsorConfig.sponsorMessage && (
                    <div className="sm:col-span-2 lg:col-span-3">
                      <span className="text-neutral-500">Message:</span>{' '}
                      <span className="text-neutral-700 italic">&ldquo;{sponsorConfig.sponsorMessage}&rdquo;</span>
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
                <div className="p-2 rounded-lg bg-indigo-50">
                  <Award className="h-5 w-5 text-indigo-500" />
                </div>
                <div>
                  <p className="text-xs text-neutral-500">Total Sponsors</p>
                  <p className="text-lg font-bold text-neutral-800">{summary.totalSponsors}</p>
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
                  <p className="text-xs text-neutral-500">Money Sponsors</p>
                  <p className="text-lg font-bold text-neutral-800">{summary.completedMoneySponsors}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-4 pb-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-amber-50">
                  <Package className="h-5 w-5 text-amber-600" />
                </div>
                <div>
                  <p className="text-xs text-neutral-500">Item Sponsors</p>
                  <p className="text-lg font-bold text-neutral-800">{summary.itemSponsorCount}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-4 pb-4">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-green-50">
                  <DollarSign className="h-5 w-5 text-green-600" />
                </div>
                <div>
                  <p className="text-xs text-neutral-500">Total Amount</p>
                  <p className="text-lg font-bold text-neutral-800">
                    ${summary.totalMoneyAmount.toFixed(2)}
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Payout Breakdown Card (for money sponsors only) */}
      {summary && summary.completedMoneySponsors > 0 && summary.totalMoneyAmount > 0 && (
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-neutral-600">Your Payout</p>
                <p className="text-3xl font-bold text-green-700">
                  ${summary.totalOrganizerPayout.toFixed(2)}
                </p>
                <p className="text-xs text-neutral-500 mt-1">
                  After Stripe fees &amp; platform commission
                </p>
              </div>
              <DollarSign className="h-10 w-10 text-orange-600" />
            </div>

            <div className="mt-3 pt-3 border-t border-neutral-200">
              <div className="flex justify-between text-xs text-neutral-600 mb-1">
                <span>Gross Revenue:</span>
                <span className="font-medium">${summary.totalMoneyAmount.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-xs text-red-600 mb-1">
                <span>Stripe Fees (2.9% + $0.30):</span>
                <span className="font-medium">-${summary.totalStripeFees.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-xs text-red-600 mb-1">
                <span>Platform Commission (2%):</span>
                <span className="font-medium">-${summary.totalPlatformCommission.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-xs text-green-700 font-semibold pt-1 border-t border-neutral-200">
                <span>Your Payout:</span>
                <span>${summary.totalOrganizerPayout.toFixed(2)}</span>
              </div>
              <p className="text-[10px] text-neutral-400 mt-2">
                * Stripe &amp; LankaConnect fees shown separately.
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Actions Row */}
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-neutral-700">
          All Sponsors ({sponsors.length})
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
            disabled={isExporting || sponsors.length === 0}
          >
            <Download className="h-4 w-4 mr-1" />
            {isExporting ? 'Exporting...' : 'Export Excel'}
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => handleExport('csv')}
            disabled={isExporting || sponsors.length === 0}
          >
            <Download className="h-4 w-4 mr-1" />
            CSV
          </Button>
        </div>
      </div>

      {/* Empty State */}
      {sponsors.length === 0 && (
        <div className="text-center py-12 text-neutral-500">
          <Award className="h-10 w-10 mx-auto mb-3 text-neutral-300" />
          <p className="text-sm">No sponsors yet.</p>
          <p className="text-xs text-neutral-400 mt-1">Sponsors will appear here once someone contributes.</p>
        </div>
      )}

      {/* Money Sponsors Table */}
      {moneySponsors.length > 0 && (
        <>
          <h4 className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
            <DollarSign className="h-4 w-4 text-emerald-600" />
            Money Sponsors ({moneySponsors.length})
          </h4>
          <div className="overflow-x-auto rounded-lg border border-neutral-200">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-neutral-50 border-b border-neutral-200">
                  <th className="text-left px-4 py-3 font-medium text-neutral-600">Sponsor</th>
                  <th className="text-left px-4 py-3 font-medium text-neutral-600">Organization</th>
                  <th className="text-left px-4 py-3 font-medium text-neutral-600">Contact</th>
                  <th className="text-right px-4 py-3 font-medium text-neutral-600">Amount</th>
                  <th className="text-center px-4 py-3 font-medium text-neutral-600">Status</th>
                  <th className="text-left px-4 py-3 font-medium text-neutral-600">Date</th>
                </tr>
              </thead>
              <tbody>
                {moneySponsors.map((sponsor: SponsorDto) => (
                  <tr key={sponsor.id} className="border-b border-neutral-100 hover:bg-neutral-50">
                    <td className="px-4 py-3">
                      <span className="font-medium text-neutral-800">{sponsor.sponsorName}</span>
                    </td>
                    <td className="px-4 py-3 text-neutral-600 text-xs">
                      {sponsor.sponsorOrganization || '—'}
                    </td>
                    <td className="px-4 py-3">
                      <div className="space-y-0.5">
                        <div className="flex items-center gap-1 text-xs text-neutral-600">
                          <Mail className="h-3 w-3" />
                          <span>{sponsor.sponsorEmail}</span>
                        </div>
                        {sponsor.sponsorPhone && (
                          <div className="flex items-center gap-1 text-xs text-neutral-500">
                            <Phone className="h-3 w-3" />
                            <span>{sponsor.sponsorPhone}</span>
                          </div>
                        )}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-right">
                      <span className="font-semibold text-neutral-800">
                        ${sponsor.amount != null ? sponsor.amount.toFixed(2) : '0.00'}
                      </span>
                      {sponsor.currency && sponsor.currency !== 'USD' && (
                        <span className="text-xs text-neutral-500 ml-1">{sponsor.currency}</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-center">
                      <Badge style={{ backgroundColor: getSponsorStatusColor(sponsor.status) }}>
                        {sponsor.status}
                      </Badge>
                    </td>
                    <td className="px-4 py-3 text-neutral-600">
                      <div className="flex items-center gap-1 text-xs">
                        <Calendar className="h-3 w-3" />
                        <span>
                          {sponsor.paymentCompletedAt
                            ? new Date(sponsor.paymentCompletedAt).toLocaleDateString()
                            : new Date(sponsor.createdAt).toLocaleDateString()}
                        </span>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      {/* Item Sponsors Table */}
      {itemSponsors.length > 0 && (
        <>
          <h4 className="text-sm font-semibold text-neutral-700 flex items-center gap-2">
            <Package className="h-4 w-4 text-amber-600" />
            Item Sponsors ({itemSponsors.length})
          </h4>
          <div className="overflow-x-auto rounded-lg border border-neutral-200">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-neutral-50 border-b border-neutral-200">
                  <th className="text-left px-4 py-3 font-medium text-neutral-600">Sponsor</th>
                  <th className="text-left px-4 py-3 font-medium text-neutral-600">Organization</th>
                  <th className="text-left px-4 py-3 font-medium text-neutral-600">Item Name</th>
                  <th className="text-left px-4 py-3 font-medium text-neutral-600">Description</th>
                  <th className="text-right px-4 py-3 font-medium text-neutral-600">Est. Value</th>
                  <th className="text-left px-4 py-3 font-medium text-neutral-600">Date</th>
                </tr>
              </thead>
              <tbody>
                {itemSponsors.map((sponsor: SponsorDto) => (
                  <tr key={sponsor.id} className="border-b border-neutral-100 hover:bg-neutral-50">
                    <td className="px-4 py-3">
                      <span className="font-medium text-neutral-800">{sponsor.sponsorName}</span>
                    </td>
                    <td className="px-4 py-3 text-neutral-600 text-xs">
                      {sponsor.sponsorOrganization || '—'}
                    </td>
                    <td className="px-4 py-3 text-neutral-800 text-xs font-medium">
                      {sponsor.itemName || '—'}
                    </td>
                    <td className="px-4 py-3 text-xs text-neutral-500 max-w-[200px] truncate">
                      {sponsor.itemDescription || '—'}
                    </td>
                    <td className="px-4 py-3 text-right">
                      {sponsor.estimatedValue != null ? (
                        <span className="font-semibold text-neutral-800">
                          ${sponsor.estimatedValue.toFixed(2)}
                        </span>
                      ) : (
                        <span className="text-neutral-400">—</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-neutral-600">
                      <div className="flex items-center gap-1 text-xs">
                        <Calendar className="h-3 w-3" />
                        <span>{new Date(sponsor.createdAt).toLocaleDateString()}</span>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  );
}
